using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using SampleFe.Options;

namespace SampleFe.Services;

public interface ITokenValidationService
{
    Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string[]? Roles { get; set; }
}

public class TokenValidationService : ITokenValidationService
{
    private readonly ILogger<TokenValidationService> _logger;
    private readonly TokenValidationOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public TokenValidationService(
        ILogger<TokenValidationService> logger,
        IOptions<TokenValidationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _tokenHandler = new JwtSecurityTokenHandler();

        var authority = $"{_options.Instance.TrimEnd('/')}/{_options.TenantId}";
        var metadataAddress = $"{_options.Instance.TrimEnd('/')}/common/v2.0/.well-known/openid-configuration";

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever())
        {
            // 自動リフレッシュの設定
            AutomaticRefreshInterval = TimeSpan.FromHours(24),
            RefreshInterval = TimeSpan.FromMinutes(30)
        };

        _logger.LogInformation("TokenValidationService initialized. Metadata URL: {MetadataAddress}, Validation Enabled: {ValidationEnabled}",
            metadataAddress, _options.EnableValidation);
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableValidation)
        {
            _logger.LogInformation("Token validation is disabled");
            return new TokenValidationResult { IsValid = true };
        }

        try
        {
            // 基本的なJWT構造チェック
            if (!_tokenHandler.CanReadToken(token))
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token is not a valid JWT"
                };
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // 有効期限チェック（クライアント側での簡易チェック）
            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Token has expired"
                };
            }

            // OpenID Connect設定を取得
            var openIdConfig = await _configurationManager.GetConfigurationAsync(cancellationToken);

            // トークン検証パラメータを設定
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                // commonエンドポイント使用時は、実際のテナントIDで動的に検証
                ValidIssuer = $"{_options.Instance.TrimEnd('/')}/{_options.TenantId}/v2.0",
                ValidateAudience = !string.IsNullOrEmpty(_options.ApiClientId),
                ValidAudience = _options.ApiClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = openIdConfig.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5), // 時計のずれを許容
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // トークンの署名検証
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // ロールの確認
            var roles = principal.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                .Select(c => c.Value)
                .ToArray();

            var hasRequiredRole = _options.RequiredRoles == null ||
                                  _options.RequiredRoles.Length == 0 ||
                                  _options.RequiredRoles.Any(reqRole => roles.Contains(reqRole));

            if (!hasRequiredRole)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Token does not contain required roles: {string.Join(", ", _options.RequiredRoles ?? Array.Empty<string>())}"
                };
            }

            _logger.LogInformation("Token validation successful. Roles: {Roles}", string.Join(", ", roles));

            return new TokenValidationResult
            {
                IsValid = true,
                Principal = principal,
                ExpiresOn = jwtToken.ValidTo,
                Roles = roles
            };
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Token has expired");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }
        catch (SecurityTokenInvalidIssuerException ex)
        {
            _logger.LogWarning(ex, "Token has invalid issuer");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has invalid issuer"
            };
        }
        catch (SecurityTokenInvalidAudienceException ex)
        {
            _logger.LogWarning(ex, "Token has invalid audience");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has invalid audience"
            };
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning(ex, "Token has invalid signature");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has invalid signature"
            };
        }
        catch (Exception ex)
        {
            // ネットワーク関連のエラーかどうかを判定
            if (ex.Message.Contains("IDX20803") || ex.Message.Contains("IDX20807"))
            {
                _logger.LogError(ex, "Network connectivity issue when accessing OpenID Connect configuration. " +
                    "This might be due to network policy restrictions, firewall rules, or DNS resolution issues. " +
                    "Consider temporarily disabling token validation using TokenValidation__EnableValidation=false environment variable.");

                return new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Network connectivity issue: Unable to validate token due to inability to access Azure AD metadata endpoint"
                };
            }

            _logger.LogError(ex, "Token validation failed with unexpected error");
            return new TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Token validation failed: {ex.Message}"
            };
        }
    }
}

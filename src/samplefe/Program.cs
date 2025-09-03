// <directives>
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleFe.Options;
using SampleFe.Infrastructure;
using SampleFe.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
// <directives>

namespace SampleFe;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 設定: appsettings.json + 環境変数（__ 区切り）
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                              .AddEnvironmentVariables();

        // ログ: 統一（SimpleConsole, SingleLine, Timestamp）
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
        });

        // DI 構成
        builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));
        builder.Services.Configure<SqlOptions>(builder.Configuration.GetSection("Sql"));
        builder.Services.Configure<TokenValidationOptions>(builder.Configuration.GetSection("TokenValidation"));
        // 既存の平坦な環境変数からのフォールバック（後方互換）
        builder.Services.PostConfigure<ApiOptions>(opt =>
        {
            opt.Endpoint ??= builder.Configuration["API_ENDPOINT"];
            opt.Scope ??= builder.Configuration["API_SCOPE"];
        });
        builder.Services.PostConfigure<SqlOptions>(opt =>
        {
            opt.Server ??= builder.Configuration["SQL_SERVER"];
            opt.Database ??= builder.Configuration["SQL_DATABASE"];
        });
        builder.Services.PostConfigure<TokenValidationOptions>(opt =>
        {
            opt.TenantId ??= builder.Configuration["AzureAd:TenantId"] ?? builder.Configuration["AZURE_TENANT_ID"];
            opt.ApiClientId ??= builder.Configuration["API_CLIENT_ID"];
            opt.Instance ??= builder.Configuration["AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
        });
        builder.Services.AddHttpClient("api");
        builder.Services.AddSingleton<TokenCredential>(_ => new WorkloadIdentityCredential());
        builder.Services.AddSingleton<ReadinessState>();
        builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();
        builder.Services.AddHealthChecks()
                        .AddCheck<ReadinessHealthCheck>("ready", tags: new[] { "ready" });
        builder.Services.AddHostedService<Worker>();

        // HealthChecks + ポート設定
        builder.WebHost.UseSetting(WebHostDefaults.ServerUrlsKey, "http://0.0.0.0:8080");

        var app = builder.Build();
        app.MapHealthChecks("/healthz");
        app.MapHealthChecks("/readyz", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });
        await app.RunAsync();
    }
}

internal sealed class Worker(
    ILogger<Worker> logger,
    Microsoft.Extensions.Options.IOptions<ApiOptions> apiOptions,
    Microsoft.Extensions.Options.IOptions<SqlOptions> sqlOptions,
    IHttpClientFactory httpClientFactory,
    TokenCredential credential,
    ReadinessState readiness,
    IServiceProvider serviceProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var http = httpClientFactory.CreateClient("api");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("samplefe start: utc={UtcNow}, node={Node}", DateTime.UtcNow, Environment.MachineName);

            // SQL 接続確認（任意設定時のみ）
            var sqlServer = sqlOptions.Value.Server ?? string.Empty;
            var sqlDatabase = sqlOptions.Value.Database ?? "master";
            if (!string.IsNullOrEmpty(sqlServer))
            {
                try
                {
                    var sqlToken = await credential.GetTokenAsync(
                        new TokenRequestContext(["https://database.windows.net/.default"]),
                        stoppingToken);

                    logger.LogInformation("SQL target: server={Server}, database={Database}", sqlServer, sqlDatabase);

                    var cs = $"Server={sqlServer}; Database={sqlDatabase}; Encrypt=True; TrustServerCertificate=False; Connection Timeout=30;";
                    using var connection = new SqlConnection(cs)
                    {
                        AccessToken = sqlToken.Token
                    };
                    await connection.OpenAsync(stoppingToken);
                    var command = new SqlCommand("SELECT @@VERSION", connection);
                    var version = await command.ExecuteScalarAsync(stoppingToken);
                    logger.LogInformation("SQL Database connected successfully. Version: {Version}", version?.ToString()?.Substring(0, 50) + "...");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to connect to SQL Database");
                }
            }

            // API 呼び出し
            var apiScope = apiOptions.Value.Scope ?? "api://YOUR-API-APP-ID/.default";
            var apiEndpoint = apiOptions.Value.Endpoint ?? "http://sampleapi/weatherforecast";

            try
            {
                var token = await credential.GetTokenAsync(new TokenRequestContext([apiScope]), stoppingToken);

                // トークン検証
                using var scope = serviceProvider.CreateScope();
                var tokenValidationService = scope.ServiceProvider.GetRequiredService<ITokenValidationService>();
                var validationResult = await tokenValidationService.ValidateTokenAsync(token.Token, stoppingToken);

                if (!validationResult.IsValid)
                {
                    logger.LogError("Token validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                    logger.LogInformation("Skipping API call due to invalid token");
                    continue; // 次のループへ
                }

                logger.LogInformation("Token validation successful. Token expires on: {ExpiresOn}, Roles: {Roles}",
                    validationResult.ExpiresOn, string.Join(", ", validationResult.Roles ?? Array.Empty<string>()));

                using var request = new HttpRequestMessage(HttpMethod.Get, apiEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                using var response = await http.SendAsync(request, stoppingToken);
                response.EnsureSuccessStatusCode();

                logger.LogInformation("API response status: {Status}", response.StatusCode);
                var body = await response.Content.ReadAsStringAsync(stoppingToken);
                logger.LogInformation("API response body: {Body}", body);
                logger.LogInformation("Entra ID token expires on: {ExpiresOn}", token.ExpiresOn);
                readiness.MarkReady();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API request failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

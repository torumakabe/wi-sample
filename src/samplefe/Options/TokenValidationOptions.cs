namespace SampleFe.Options;

public class TokenValidationOptions
{
    /// <summary>
    /// Azure AD テナントID
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// API アプリケーションのクライアントID（Audience）
    /// </summary>
    public string? ApiClientId { get; set; }

    /// <summary>
    /// 必要なロール
    /// </summary>
    public string[]? RequiredRoles { get; set; }

    /// <summary>
    /// Azure AD インスタンス
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// トークン検証を有効にするかどうか
    /// </summary>
    public bool EnableValidation { get; set; } = true;
}

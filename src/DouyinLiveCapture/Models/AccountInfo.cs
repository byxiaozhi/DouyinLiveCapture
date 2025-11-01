using System.Text.Json.Serialization;

namespace DouyinLiveCapture.Models;

/// <summary>
/// 账号信息
/// </summary>
public class AccountInfo
{
    /// <summary>
    /// 平台名称
    /// </summary>
    [JsonPropertyName("platform")]
    public required string Platform { get; set; }

    /// <summary>
    /// 用户名/账号
    /// </summary>
    [JsonPropertyName("username")]
    public required string Username { get; set; }

    /// <summary>
    /// 密码（加密存储）
    /// </summary>
    [JsonPropertyName("password")]
    public required string Password { get; set; }

    /// <summary>
    /// 额外参数（如 partner_code 等）
    /// </summary>
    [JsonPropertyName("extra_params")]
    public Dictionary<string, string>? ExtraParams { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

/// <summary>
/// 认证令牌信息
/// </summary>
public class AuthToken
{
    /// <summary>
    /// 平台名称
    /// </summary>
    [JsonPropertyName("platform")]
    public required string Platform { get; set; }

    /// <summary>
    /// 令牌值
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; set; }

    /// <summary>
    /// 令牌类型（access_token, refresh_token 等）
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "access_token";

    /// <summary>
    /// 过期时间
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已过期
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow >= ExpiresAt.Value;
}
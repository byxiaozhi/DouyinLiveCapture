using System.Text.RegularExpressions;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Utilities;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 平台适配器基类
/// </summary>
public abstract class BasePlatformAdapter : IPlatformAdapter
{
    protected readonly HttpClient _httpClient;

    /// <inheritdoc />
    public abstract PlatformType PlatformType { get; }

    /// <inheritdoc />
    public abstract string PlatformName { get; }

    protected BasePlatformAdapter()
    {
        _httpClient = HttpClientHelper.CreateDefaultHttpClient();
    }

    /// <inheritdoc />
    public abstract Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract string? ParseRoomId(string roomUrl);

    /// <inheritdoc />
    public abstract string GenerateRoomUrl(string roomId);

    /// <inheritdoc />
    public abstract bool IsSupportedUrl(string url);

    /// <summary>
    /// 检查URL是否匹配平台的域名模式
    /// </summary>
    /// <param name="url">要检查的URL</param>
    /// <param name="domainPatterns">域名模式列表</param>
    /// <returns>是否匹配</returns>
    protected bool IsUrlMatchDomain(string url, params string[] domainPatterns)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url.ToLowerInvariant());
            var host = uri.Host;

            return domainPatterns.Any(pattern =>
                host.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith($".{pattern}", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从URL中提取房间ID
    /// </summary>
    /// <param name="url">直播间URL</param>
    /// <param name="patterns">正则表达式模式列表</param>
    /// <returns>房间ID</returns>
    protected string? ExtractRoomIdFromUrl(string url, params Regex[] patterns)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        foreach (var pattern in patterns)
        {
            var match = pattern.Match(url);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <summary>
    /// 清理和标准化主播名称
    /// </summary>
    /// <param name="name">原始名称</param>
    /// <returns>清理后的名称</returns>
    protected string CleanAnchorName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // 移除emoji和特殊字符
        return Regex.Replace(name.Trim(), @"[\p{Cs}\p{Co}\p{Sk}]", "").Trim();
    }

    /// <summary>
    /// 清理和标准化直播标题
    /// </summary>
    /// <param name="title">原始标题</param>
    /// <returns>清理后的标题</returns>
    protected string CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        return title.Trim();
    }

    /// <summary>
    /// 创建基础流信息对象
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="roomId">房间ID</param>
    /// <returns>基础流信息</returns>
    protected StreamInfo CreateBaseStreamInfo(string roomUrl, string roomId)
    {
        return new StreamInfo
        {
            Url = roomUrl,
            PlatformType = PlatformType,
            Platform = PlatformName,
            RoomId = roomId,
            LiveStatus = LiveStatus.Unknown,
            FetchTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public virtual void Dispose()
    {
        _httpClient?.Dispose();
    }
}
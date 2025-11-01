using System.Text.Json.Serialization;
using DouyinLiveCapture.Services.Platform;

namespace DouyinLiveCapture.Models;

/// <summary>
/// 直播流信息
/// </summary>
public class StreamInfo
{
    /// <summary>
    /// 直播间URL
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    /// <summary>
    /// 平台类型
    /// </summary>
    [JsonPropertyName("platform_type")]
    public PlatformType PlatformType { get; set; }

    /// <summary>
    /// 平台名称
    /// </summary>
    [JsonPropertyName("platform")]
    public required string Platform { get; set; }

    /// <summary>
    /// 直播状态
    /// </summary>
    [JsonPropertyName("live_status")]
    public LiveStatus LiveStatus { get; set; }

    /// <summary>
    /// 主播名称
    /// </summary>
    [JsonPropertyName("anchor_name")]
    public string? AnchorName { get; set; }

    /// <summary>
    /// 直播标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 是否正在直播
    /// </summary>
    [JsonPropertyName("is_live")]
    public bool IsLive { get; set; }

    /// <summary>
    /// 直播间ID
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// 直播流URL列表
    /// </summary>
    [JsonPropertyName("stream_urls")]
    public List<StreamUrlInfo> StreamUrls { get; set; } = new();

    /// <summary>
    /// M3U8流地址（向后兼容）
    /// </summary>
    [JsonPropertyName("m3u8_url")]
    public string? M3u8Url { get; set; }

    /// <summary>
    /// FLV流地址（向后兼容）
    /// </summary>
    [JsonPropertyName("flv_url")]
    public string? FlvUrl { get; set; }

    /// <summary>
    /// 录制用的流地址 (优先M3U8)
    /// </summary>
    [JsonPropertyName("record_url")]
    public string? RecordUrl { get; set; }

    /// <summary>
    /// 视频质量
    /// </summary>
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    /// <summary>
    /// 获取时间
    /// </summary>
    [JsonPropertyName("fetch_time")]
    public DateTime FetchTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 备注
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// 获取最佳录制URL
    /// </summary>
    /// <returns>最佳录制URL</returns>
    public string? GetBestRecordUrl()
    {
        // 优先使用现有的录制URL
        if (!string.IsNullOrEmpty(RecordUrl))
            return RecordUrl;

        // 使用新的流URL列表
        var bestStream = StreamUrls
            .Where(s => !s.IsBackup && (s.Format.Equals("m3u8", StringComparison.OrdinalIgnoreCase) ||
                                       s.Format.Equals("flv", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(s => s.Quality)
            .FirstOrDefault();

        if (bestStream != null)
            return bestStream.Url;

        // 向后兼容：使用M3U8或FLV
        return !string.IsNullOrEmpty(M3u8Url) ? M3u8Url : FlvUrl;
    }
}

/// <summary>
/// 视频质量枚举
/// </summary>
public enum VideoQuality
{
    /// <summary>
    /// 原画
    /// </summary>
    [JsonPropertyName("OD")]
    Original = 0,

    /// <summary>
    /// 超清
    /// </summary>
    [JsonPropertyName("BD")]
    BluRay = 0,

    /// <summary>
    /// 4K超清
    /// </summary>
    [JsonPropertyName("UHD")]
    UltraHD = 1,

    /// <summary>
    /// 高清
    /// </summary>
    [JsonPropertyName("HD")]
    High = 2,

    /// <summary>
    /// 标清
    /// </summary>
    [JsonPropertyName("SD")]
    Standard = 3,

    /// <summary>
    /// 流畅
    /// </summary>
    [JsonPropertyName("LD")]
    Low = 4
}
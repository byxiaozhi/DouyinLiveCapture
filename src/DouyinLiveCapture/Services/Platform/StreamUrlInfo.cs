namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 直播流URL信息
/// </summary>
public class StreamUrlInfo
{
    /// <summary>
    /// 流URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 流质量
    /// </summary>
    public StreamQuality Quality { get; set; }

    /// <summary>
    /// 流格式
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// 编码格式
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// 分辨率
    /// </summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>
    /// 帧率
    /// </summary>
    public int? Framerate { get; set; }

    /// <summary>
    /// 比特率
    /// </summary>
    public long? Bitrate { get; set; }

    /// <summary>
    /// 是否为备用流
    /// </summary>
    public bool IsBackup { get; set; }
}
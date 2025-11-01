namespace DouyinLiveCapture.Services.Stream;

/// <summary>
/// TikTok流数据处理服务接口
/// </summary>
public interface ITiktokStreamService
{
    /// <summary>
    /// 解析TikTok直播间数据
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <param name="videoQuality">视频质量</param>
    /// <returns>解析后的流信息</returns>
    Task<TiktokStreamInfo?> ParseStreamDataAsync(string jsonString, string videoQuality = "origin");

    /// <summary>
    /// 获取视频质量URL列表
    /// </summary>
    /// <param name="streamData">流数据</param>
    /// <param name="qualityKey">质量键</param>
    /// <returns>按质量排序的URL列表</returns>
    List<VideoQualityUrl> GetQualityUrlList(Dictionary<string, object> streamData, string qualityKey);
}

/// <summary>
/// TikTok流信息模型
/// </summary>
public class TiktokStreamInfo
{
    public string AnchorName { get; set; } = string.Empty;
    public bool IsLive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string M3u8Url { get; set; } = string.Empty;
    public string FlvUrl { get; set; } = string.Empty;
    public string RecordUrl { get; set; } = string.Empty;
    public List<VideoQualityUrl> M3u8Urls { get; set; } = new();
    public List<VideoQualityUrl> FlvUrls { get; set; } = new();
}

/// <summary>
/// 视频质量URL信息
/// </summary>
public class VideoQualityUrl
{
    public string Url { get; set; } = string.Empty;
    public int VideoBitrate { get; set; }
    public (int Width, int Height) Resolution { get; set; }
    public string Codec { get; set; } = string.Empty;
}
namespace DouyinLiveCapture.Services.Stream;

/// <summary>
/// 流处理服务接口
/// </summary>
public interface IStreamProcessingService
{
    /// <summary>
    /// 获取M3U8播放列表
    /// </summary>
    /// <param name="m3u8Url">M3U8文件URL</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按带宽排序的播放URL列表</returns>
    Task<List<string>> GetPlayUrlListAsync(string m3u8Url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析M3U8内容
    /// </summary>
    /// <param name="m3u8Content">M3U8文件内容</param>
    /// <returns>解析后的流信息</returns>
    List<M3U8StreamInfo> ParseM3U8Content(string m3u8Content);

    /// <summary>
    /// 按带宽排序流URL
    /// </summary>
    /// <param name="m3u8Content">M3U8内容</param>
    /// <returns>按带宽排序的URL列表</returns>
    List<string> SortByBandwidth(string m3u8Content);
}

/// <summary>
/// M3U8流信息
/// </summary>
public class M3U8StreamInfo
{
    /// <summary>
    /// 流URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 带宽
    /// </summary>
    public long Bandwidth { get; set; }

    /// <summary>
    /// 分辨率
    /// </summary>
    public string? Resolution { get; set; }

    /// <summary>
    /// 编解码器
    /// </summary>
    public string? Codec { get; set; }

    /// <summary>
    /// 视频质量
    /// </summary>
    public string Quality { get; set; } = string.Empty;
}
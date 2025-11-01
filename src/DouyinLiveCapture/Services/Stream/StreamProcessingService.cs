using System.Text.RegularExpressions;
using DouyinLiveCapture.Services.Utilities;

namespace DouyinLiveCapture.Services.Stream;

/// <summary>
/// 流处理服务实现
/// </summary>
public class StreamProcessingService : IStreamProcessingService
{
    private readonly IHttpClientService _httpClientService;
    private readonly Regex _bandwidthRegex;
    private readonly Regex _resolutionRegex;
    private readonly Regex _codecRegex;

    /// <summary>
    /// 初始化StreamProcessingService实例
    /// </summary>
    public StreamProcessingService() : this(new HttpClientService(30))
    {
    }

    /// <summary>
    /// 初始化StreamProcessingService实例
    /// </summary>
    /// <param name="httpClientService">HTTP客户端服务</param>
    public StreamProcessingService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;

        // 正则表达式模式
        _bandwidthRegex = new Regex(@"BANDWIDTH=(\d+)", RegexOptions.IgnoreCase);
        _resolutionRegex = new Regex(@"RESOLUTION=(\d+x\d+)", RegexOptions.IgnoreCase);
        _codecRegex = new Regex(@"CODECS=""([^""]+)""", RegexOptions.IgnoreCase);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetPlayUrlListAsync(string m3u8Url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取M3U8内容
            var m3u8Content = await _httpClientService.GetStringAsync(m3u8Url, headers, cancellationToken);

            // 解析并排序URL
            return SortByBandwidth(m3u8Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting M3U8 play list: {ex.Message}");
            return new List<string>();
        }
    }

    /// <inheritdoc />
    public List<M3U8StreamInfo> ParseM3U8Content(string m3u8Content)
    {
        var streams = new List<M3U8StreamInfo>();
        var lines = m3u8Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        M3U8StreamInfo? currentStream = null;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 检查是否是EXT-X-STREAM-INF行
            if (trimmedLine.StartsWith("#EXT-X-STREAM-INF:"))
            {
                currentStream = new M3U8StreamInfo();

                // 解析带宽
                var bandwidthMatch = _bandwidthRegex.Match(trimmedLine);
                if (bandwidthMatch.Success && long.TryParse(bandwidthMatch.Groups[1].Value, out var bandwidth))
                {
                    currentStream.Bandwidth = bandwidth;
                }

                // 解析分辨率
                var resolutionMatch = _resolutionRegex.Match(trimmedLine);
                if (resolutionMatch.Success)
                {
                    currentStream.Resolution = resolutionMatch.Groups[1].Value;
                }

                // 解析编解码器
                var codecMatch = _codecRegex.Match(trimmedLine);
                if (codecMatch.Success)
                {
                    currentStream.Codec = codecMatch.Groups[1].Value;
                }

                // 根据带宽确定质量
                currentStream.Quality = DetermineQuality(currentStream.Bandwidth);
            }
            // 检查是否是URL行
            else if (!trimmedLine.StartsWith("#") && currentStream != null)
            {
                // 处理相对URL
                if (trimmedLine.StartsWith("http"))
                {
                    currentStream.Url = trimmedLine;
                }
                else
                {
                    // 这里需要基础URL来处理相对路径，暂时保持原样
                    currentStream.Url = trimmedLine;
                }

                streams.Add(currentStream);
                currentStream = null;
            }
        }

        return streams;
    }

    /// <inheritdoc />
    public List<string> SortByBandwidth(string m3u8Content)
    {
        var streams = ParseM3U8Content(m3u8Content);

        // 按带宽降序排序
        var sortedStreams = streams.OrderByDescending(s => s.Bandwidth).ToList();

        return sortedStreams.Select(s => s.Url).ToList();
    }

    /// <summary>
    /// 根据带宽确定视频质量
    /// </summary>
    /// <param name="bandwidth">带宽</param>
    /// <returns>质量标识</returns>
    private static string DetermineQuality(long bandwidth)
    {
        return bandwidth switch
        {
            >= 8000000 => "ORIGIN", // 8Mbps+
            >= 6000000 => "UHD",    // 6-8Mbps
            >= 4000000 => "HD",     // 4-6Mbps
            >= 2000000 => "SD",     // 2-4Mbps
            >= 1000000 => "LD",     // 1-2Mbps
            _ => "AUTO"
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClientService?.Dispose();
    }
}
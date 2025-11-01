using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Utilities;
using DouyinLiveCapture.Services.Signature;
using DouyinLiveCapture.Services.Stream;
using DouyinLiveCapture.Services.Exceptions;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// TikTok平台适配器
/// 支持TikTok直播间数据获取和流URL解析
/// </summary>
public class TiktokPlatformAdapter : BasePlatformAdapter
{
    private readonly ITiktokSignatureService _signatureService;
    private readonly ITiktokStreamService _streamService;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly ILogger<TiktokPlatformAdapter> _logger;
  /// <summary>
    /// 初始化TikTok平台适配器（无参构造函数，用于工厂）
    /// </summary>
    public TiktokPlatformAdapter()
    {
        // TODO: 这里应该通过依赖注入获取服务，暂时使用空实现
        _signatureService = null!;
        _streamService = null!;
        _errorHandlingService = null!;
        _logger = null!;
    }

    /// <summary>
    /// 初始化TikTok平台适配器
    /// </summary>
    /// <param name="signatureService">签名服务</param>
    /// <param name="streamService">流处理服务</param>
    /// <param name="errorHandlingService">错误处理服务</param>
    /// <param name="logger">日志记录器</param>
    public TiktokPlatformAdapter(
        ITiktokSignatureService signatureService,
        ITiktokStreamService streamService,
        IErrorHandlingService errorHandlingService,
        ILogger<TiktokPlatformAdapter> logger)
    {
        _signatureService = signatureService;
        _streamService = streamService;
        _errorHandlingService = errorHandlingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.TikTok;

    /// <inheritdoc />
    public override string PlatformName => "TikTok";

    private static readonly Regex[] RoomIdPatterns =
    {
        new Regex(@"tiktok\.com/@([^/]+)/live", RegexOptions.IgnoreCase),
        new Regex(@"tiktok\.com/@([^/]+)$", RegexOptions.IgnoreCase),
        new Regex(@"@([^/]+)/live", RegexOptions.IgnoreCase)
    };

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await _errorHandlingService.SafeExecuteAsync(
            async () =>
            {
                var roomId = ParseRoomId(roomUrl);
                if (string.IsNullOrEmpty(roomId))
                {
                    _logger.LogWarning("Unable to parse room ID from URL: {Url}", roomUrl);
                    return null;
                }

                _logger.LogInformation("Getting TikTok stream info for room: {RoomId}", roomId);

                // 设置请求头
                var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36";
                var headers = new Dictionary<string, string>
                {
                    {"User-Agent", userAgent},
                    {"Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"},
                    {"Accept-Language", "en-US,en;q=0.5"},
                    {"Accept-Encoding", "gzip, deflate"},
                    {"Referer", "https://www.tiktok.com/"}
                };

                // 使用默认Cookie（如果未提供）
                if (string.IsNullOrEmpty(cookie))
                {
                    cookie = "1%7Cz7FKki38aKyy7i-BC9rEDwcrVvjcLcFEL6QIeqldoy4%7C1761302831%7C6c1461e9f1f980cbe0404c51905177d5d53bbd822e1bf66128887d942c9c3e2f";
                }
                headers.Add("Cookie", cookie);

                // 生成X-Bogus签名
                var xbogus = await _signatureService.GenerateXBogusAsync(roomUrl, userAgent, cookie, cancellationToken);

                // 获取直播间页面（带重试机制）
                var html = await _errorHandlingService.RetryAsync(
                    async () => await _httpClient.GetStringAsync(roomUrl, headers, cancellationToken),
                    maxAttempts: 3,
                    baseDelay: TimeSpan.FromSeconds(1),
                    shouldRetry: ex => ex is HttpRequestException or TimeoutException,
                    cancellationToken: cancellationToken);

                // 检查区域限制
                if (html.Contains("We regret to inform you that we have discontinued operating TikTok"))
                {
                    var errorMsg = ExtractRegionErrorMessage(html);
                    throw new StreamDataException($"TikTok regional access blocked: {errorMsg}");
                }

                // 解析SIGI_STATE JSON数据
                var jsonData = await ExtractSigIStateAsync(html, cancellationToken);
                if (jsonData == null)
                {
                    throw new StreamDataException("Failed to extract SIGI_STATE from TikTok page");
                }

                // 解析流数据
                var tiktokStreamInfo = await _streamService.ParseStreamDataAsync(jsonData);
                if (tiktokStreamInfo == null)
                {
                    _logger.LogWarning("Failed to parse TikTok stream data for room: {RoomId}", roomId);
                    return null;
                }

                // 转换为标准StreamInfo
                var streamInfo = CreateBaseStreamInfo(roomUrl, roomId);
                streamInfo.AnchorName = CleanAnchorName(tiktokStreamInfo.AnchorName);
                streamInfo.Title = tiktokStreamInfo.Title;
                streamInfo.IsLive = tiktokStreamInfo.IsLive;
                streamInfo.LiveStatus = tiktokStreamInfo.IsLive ? LiveStatus.Live : LiveStatus.Offline;

                if (tiktokStreamInfo.IsLive)
                {
                    streamInfo.RecordUrl = tiktokStreamInfo.RecordUrl;

                    // 添加FLV流URL
                    if (!string.IsNullOrEmpty(tiktokStreamInfo.FlvUrl))
                    {
                        streamInfo.StreamUrls.Add(new StreamUrlInfo
                        {
                            Url = tiktokStreamInfo.FlvUrl,
                            Quality = ParseStreamQuality(tiktokStreamInfo.Quality),
                            Format = "flv",
                            Codec = "h264",
                            IsBackup = false
                        });
                    }

                    // 添加M3U8流URL
                    if (!string.IsNullOrEmpty(tiktokStreamInfo.M3u8Url))
                    {
                        streamInfo.StreamUrls.Add(new StreamUrlInfo
                        {
                            Url = tiktokStreamInfo.M3u8Url,
                            Quality = ParseStreamQuality(tiktokStreamInfo.Quality),
                            Format = "m3u8",
                            Codec = "h264",
                            IsBackup = !string.IsNullOrEmpty(tiktokStreamInfo.FlvUrl)
                        });
                    }

                    // 添加其他质量选项
                    foreach (var flvUrl in tiktokStreamInfo.FlvUrls.Skip(1).Take(2))
                    {
                        if (!string.IsNullOrEmpty(flvUrl.Url))
                        {
                            streamInfo.StreamUrls.Add(new StreamUrlInfo
                            {
                                Url = flvUrl.Url,
                                Quality = MapBitrateToQuality(flvUrl.VideoBitrate),
                                Format = "flv",
                                Codec = flvUrl.Codec,
                                IsBackup = true
                            });
                        }
                    }
                }

                _logger.LogInformation("Successfully retrieved TikTok stream info for {RoomId}, Live: {IsLive}",
                    roomId, streamInfo.IsLive);

                return streamInfo;
            },
            null,
            $"GetTikTokStreamInfo({roomUrl})",
            cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await _errorHandlingService.SafeExecuteAsync(
            async () =>
            {
                var streamInfo = await GetStreamInfoAsync(roomUrl, cookie, cancellationToken);
                return streamInfo?.IsLive ?? false;
            },
            false,
            $"IsTikTokLive({roomUrl})",
            cancellationToken);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return ExtractRoomIdFromUrl(roomUrl, RoomIdPatterns);
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return $"https://www.tiktok.com/@{roomId}/live";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "tiktok.com");
    }

    /// <summary>
    /// 从HTML中提取SIGI_STATE JSON数据
    /// </summary>
    private static async Task<string?> ExtractSigIStateAsync(string html, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var match = Regex.Match(html, @"<script id=""SIGI_STATE"" type=""application/json"">(.*?)</script>", RegexOptions.Singleline);
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// 提取区域限制错误消息
    /// </summary>
    private static string ExtractRegionErrorMessage(string html)
    {
        try
        {
            var match = Regex.Match(html, @"<p>\n\\s+(We regret to inform you that we have discontinu.*?)\\.\n\\s+</p>", RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : "Unknown region restriction";
        }
        catch
        {
            return "Unknown region restriction";
        }
    }

    /// <summary>
    /// 解析流质量字符串
    /// </summary>
    private static StreamQuality ParseStreamQuality(string quality)
    {
        return quality.ToLowerInvariant() switch
        {
            "origin" => StreamQuality.Original,
            "hd" or "high" => StreamQuality.High,
            "sd" or "standard" => StreamQuality.Standard,
            "ld" or "smooth" => StreamQuality.Smooth,
            _ => StreamQuality.Auto
        };
    }

    /// <summary>
    /// 根据码率映射到流质量
    /// </summary>
    private static StreamQuality MapBitrateToQuality(int bitrate)
    {
        return bitrate switch
        {
            > 5000 => StreamQuality.Original,
            > 3000 => StreamQuality.High,
            > 1000 => StreamQuality.Standard,
            _ => StreamQuality.Smooth
        };
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using DouyinLiveCapture.Services.Utilities;

namespace DouyinLiveCapture.Services.Stream;

/// <summary>
/// TikTok流数据处理服务实现
/// 基于Python版本的get_tiktok_stream_url函数实现
/// </summary>
public class TiktokStreamService : ITiktokStreamService
{
    private readonly ILogger<TiktokStreamService> _logger;
    private readonly IJsonParsingService _jsonParsingService;

    // 质量映射
    private static readonly Dictionary<string, int> QualityMap = new()
    {
        { "origin", 0 },
        { "hd", 1 },
        { "sd", 2 },
        { "ld", 3 },
        { "auto", 4 }
    };

    /// <summary>
    /// 初始化TikTok流服务
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="jsonParsingService">JSON解析服务</param>
    public TiktokStreamService(ILogger<TiktokStreamService> logger, IJsonParsingService jsonParsingService)
    {
        _logger = logger;
        _jsonParsingService = jsonParsingService;
    }

    /// <inheritdoc />
    public async Task<TiktokStreamInfo?> ParseStreamDataAsync(string jsonString, string videoQuality = "origin")
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(jsonString))
                {
                    return new TiktokStreamInfo { IsLive = false };
                }

                var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                // 检查LiveRoom数据
                if (!root.TryGetProperty("LiveRoom", out var liveRoomProp) ||
                    !liveRoomProp.TryGetProperty("liveRoomUserInfo", out var liveRoomUserProp))
                {
                    return new TiktokStreamInfo { IsLive = false };
                }

                var liveRoomUser = liveRoomUserProp;
                if (!liveRoomUser.TryGetProperty("user", out var userProp))
                {
                    return new TiktokStreamInfo { IsLive = false };
                }

                var user = userProp;
                var nickname = user.GetProperty("nickname").GetString() ?? "";
                var uniqueId = user.GetProperty("uniqueId").GetString() ?? "";
                var anchorName = $"{nickname}-{uniqueId}";
                var status = user.GetProperty("status").GetInt32();

                var result = new TiktokStreamInfo
                {
                    AnchorName = anchorName,
                    IsLive = false
                };

                // 如果状态为2，表示正在直播
                if (status == 2)
                {
                    try
                    {
                        // 获取流数据
                        var liveRoom = liveRoomUser.GetProperty("liveRoom");
                        var streamData = liveRoom.GetProperty("streamData")
                            .GetProperty("pull_data")
                            .GetProperty("stream_data")
                            .GetString();

                        if (!string.IsNullOrEmpty(streamData))
                        {
                            var streamJson = JsonDocument.Parse(streamData);
                            if (streamJson.RootElement.TryGetProperty("data", out var dataProp))
                            {
                                var dataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(dataProp.GetRawText());
                                if (dataDict != null)
                                {
                                    // 获取FLV和M3U8 URL列表
                                    var flvUrls = GetQualityUrlList(dataDict, "flv");
                                    var m3u8Urls = GetQualityUrlList(dataDict, "hls");

                                    // 确保至少有5个选项
                                    while (flvUrls.Count < 5 && flvUrls.Count > 0)
                                    {
                                        flvUrls.Add(flvUrls.Last());
                                    }
                                    while (m3u8Urls.Count < 5 && m3u8Urls.Count > 0)
                                    {
                                        m3u8Urls.Add(m3u8Urls.Last());
                                    }

                                    // 获取质量索引
                                    var (quality, qualityIndex) = GetQualityIndex(videoQuality);
                                    qualityIndex = Math.Min(qualityIndex, Math.Min(flvUrls.Count - 1, m3u8Urls.Count - 1));

                                    if (flvUrls.Count > qualityIndex && m3u8Urls.Count > qualityIndex)
                                    {
                                        var flvDict = flvUrls[qualityIndex];
                                        var m3u8Dict = m3u8Urls[qualityIndex];

                                        // 检查URL可用性（简化版本）
                                        var checkUrl = !string.IsNullOrEmpty(m3u8Dict.Url) ? m3u8Dict.Url : flvDict.Url;

                                        // 如果首选质量不可用，尝试下一个质量
                                        if (string.IsNullOrEmpty(checkUrl))
                                        {
                                            var nextIndex = Math.Min(qualityIndex + 1, Math.Min(flvUrls.Count - 1, m3u8Urls.Count - 1));
                                            if (nextIndex < flvUrls.Count && nextIndex < m3u8Urls.Count)
                                            {
                                                flvDict = flvUrls[nextIndex];
                                                m3u8Dict = m3u8Urls[nextIndex];
                                            }
                                        }

                                        result.IsLive = true;
                                        result.Title = liveRoom.GetProperty("title").GetString() ?? "";
                                        result.Quality = quality;
                                        result.M3u8Url = m3u8Dict.Url;
                                        result.FlvUrl = flvDict.Url;
                                        result.RecordUrl = !string.IsNullOrEmpty(m3u8Dict.Url) ? m3u8Dict.Url : flvDict.Url;
                                        result.M3u8Urls = m3u8Urls;
                                        result.FlvUrls = flvUrls;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to parse TikTok stream data");
                        result.IsLive = false;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse TikTok JSON data");
                return new TiktokStreamInfo { IsLive = false };
            }
        });
    }

    /// <inheritdoc />
    public List<VideoQualityUrl> GetQualityUrlList(Dictionary<string, object> streamData, string qualityKey)
    {
        var playList = new List<VideoQualityUrl>();

        try
        {
            foreach (var kvp in streamData)
            {
                if (kvp.Value is JsonElement element && element.TryGetProperty("main", out var mainProp))
                {
                    var main = mainProp;
                    var sdkParamsStr = main.GetProperty("sdk_params").GetString();

                    if (!string.IsNullOrEmpty(sdkParamsStr))
                    {
                        var sdkParams = JsonSerializer.Deserialize<Dictionary<string, object>>(sdkParamsStr);
                        if (sdkParams != null)
                        {
                            var vbitrate = 0;
                            var resolution = (0, 0);
                            var vCodec = "";

                            if (sdkParams.TryGetValue("vbitrate", out var vbr) && int.TryParse(vbr.ToString(), out var parsedVbr))
                            {
                                vbitrate = parsedVbr;
                            }

                            if (sdkParams.TryGetValue("resolution", out var res) && res.ToString() is { } resStr)
                            {
                                var parts = resStr.Split('x');
                                if (parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height))
                                {
                                    resolution = (width, height);
                                }
                            }

                            if (sdkParams.TryGetValue("VCodec", out var codec))
                            {
                                vCodec = codec.ToString() ?? "";
                            }

                            var playUrl = "";
                            if (main.TryGetProperty(qualityKey, out var urlProp))
                            {
                                var url = urlProp.GetString() ?? "";
                                if (!string.IsNullOrEmpty(url))
                                {
                                    playUrl = url + (url.EndsWith(".flv") || url.EndsWith(".m3u8") ? "?codec=" : "&codec=") + vCodec;
                                }
                            }

                            if (vbitrate > 0 && resolution.Item1 > 0)
                            {
                                playList.Add(new VideoQualityUrl
                                {
                                    Url = playUrl,
                                    VideoBitrate = vbitrate,
                                    Resolution = resolution,
                                    Codec = vCodec
                                });
                            }
                        }
                    }
                }
            }

            // 按码率排序
            playList = playList.OrderByDescending(x => x.VideoBitrate)
                              .ThenByDescending(x => x.Resolution.Width)
                              .ThenByDescending(x => x.Resolution.Height)
                              .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract quality URLs from stream data");
        }

        return playList;
    }

    /// <summary>
    /// 获取质量索引
    /// </summary>
    private static (string quality, int index) GetQualityIndex(string videoQuality)
    {
        var normalizedQuality = videoQuality.ToLowerInvariant();
        return QualityMap.TryGetValue(normalizedQuality, out var index)
            ? (normalizedQuality, index)
            : ("origin", 0);
    }
}
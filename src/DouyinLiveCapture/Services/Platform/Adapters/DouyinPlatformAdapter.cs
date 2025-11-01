using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Signature;
using DouyinLiveCapture.Services.Utilities;
using DouyinLiveCapture.Services.Room;
using DouyinLiveCapture.Services.Stream;
using DouyinLiveCapture.Services.Exceptions;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Platform.Adapters;

/// <summary>
/// 抖音平台适配器
/// </summary>
public class DouyinPlatformAdapter : BasePlatformAdapter
{
    private readonly DouyinAbSignatureService _signatureService;
    private readonly IRoomParsingService _roomParsingService;
    private readonly IStreamProcessingService _streamProcessingService;
    private readonly IJsonParsingService _jsonParsingService;
    private readonly IErrorHandlingService _errorHandlingService;
    private readonly Regex[] _roomIdPatterns;
    private readonly string[] _supportedDomains;

    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Douyin;

    /// <inheritdoc />
    public override string PlatformName => "抖音";

    public DouyinPlatformAdapter() : this(
        new DouyinAbSignatureService(),
        new RoomParsingService(),
        new StreamProcessingService(),
        new JsonParsingService(),
        new ErrorHandlingService(NullLogger<ErrorHandlingService>.Instance))
    {
    }

    /// <summary>
    /// 初始化DouyinPlatformAdapter实例
    /// </summary>
    /// <param name="signatureService">AB签名服务</param>
    /// <param name="roomParsingService">房间解析服务</param>
    /// <param name="streamProcessingService">流处理服务</param>
    /// <param name="jsonParsingService">JSON解析服务</param>
    /// <param name="errorHandlingService">错误处理服务</param>
    public DouyinPlatformAdapter(
        DouyinAbSignatureService signatureService,
        IRoomParsingService roomParsingService,
        IStreamProcessingService streamProcessingService,
        IJsonParsingService jsonParsingService,
        IErrorHandlingService errorHandlingService)
    {
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _roomParsingService = roomParsingService ?? throw new ArgumentNullException(nameof(roomParsingService));
        _streamProcessingService = streamProcessingService ?? throw new ArgumentNullException(nameof(streamProcessingService));
        _jsonParsingService = jsonParsingService ?? throw new ArgumentNullException(nameof(jsonParsingService));
        _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

        // 房间ID解析正则表达式
        _roomIdPatterns = new[]
        {
            new Regex(@"live\.douyin\.com/(\d+)", RegexOptions.IgnoreCase),
            new Regex(@"v\.douyin\.com/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase),
            new Regex(@"iesdouyin\.com/share/user/([^/]+)", RegexOptions.IgnoreCase),
            new Regex(@"live\.douyin\.com/([^/?]+)", RegexOptions.IgnoreCase)
        };

        // 支持的域名
        _supportedDomains = new[]
        {
            "live.douyin.com",
            "v.douyin.com",
            "www.douyin.com",
            "iesdouyin.com"
        };
    }

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await _errorHandlingService.SafeExecuteAsync(
            async () =>
            {
                if (!IsSupportedUrl(roomUrl))
                    return null;

                var roomId = ParseRoomId(roomUrl);

                // 如果无法直接解析房间ID，尝试使用房间解析服务
                if (string.IsNullOrEmpty(roomId))
                {
                    // 尝试获取sec_user_id
                    var secUserResult = await _roomParsingService.GetSecUserIdAsync(roomUrl, cookie, cancellationToken);
                    if (secUserResult != null)
                    {
                        roomId = secUserResult.Value.roomId;
                    }
                    else
                    {
                        // 尝试获取unique_id
                        var uniqueId = await _roomParsingService.GetUniqueIdAsync(roomUrl, cookie, cancellationToken);
                        if (!string.IsNullOrEmpty(uniqueId))
                        {
                            // 重新生成房间URL
                            roomUrl = GenerateRoomUrl(uniqueId);
                            roomId = ParseRoomId(roomUrl);
                        }
                    }
                }

                if (string.IsNullOrEmpty(roomId))
                    return null;

                // 获取web端流数据
                var webStreamData = await _errorHandlingService.RetryAsync(
                    () => GetWebStreamDataAsync(roomId, cookie, cancellationToken),
                    maxAttempts: 3,
                    baseDelay: TimeSpan.FromSeconds(1));

                if (webStreamData != null)
                    return webStreamData;

                // 如果web端失败，尝试app端
                var appStreamData = await _errorHandlingService.RetryAsync(
                    () => GetAppStreamDataAsync(roomId, cookie, cancellationToken),
                    maxAttempts: 2,
                    baseDelay: TimeSpan.FromSeconds(2));

                return appStreamData;
            },
            defaultValue: null,
            operationName: "Douyin.GetStreamInfoAsync",
            cancellationToken: cancellationToken
        );
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var streamInfo = await GetStreamInfoAsync(roomUrl, cookie, cancellationToken);
            return streamInfo?.IsLive == true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            return null;

        // 直接从URL中提取房间ID
        foreach (var pattern in _roomIdPatterns)
        {
            var match = pattern.Match(roomUrl);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return $"https://live.douyin.com/{roomId}";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url.ToLowerInvariant());
            return _supportedDomains.Any(domain =>
                uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取Web端流数据
    /// </summary>
    /// <param name="roomId">房间ID</param>
    /// <param name="cookie">Cookie</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流信息</returns>
    /// <exception cref="LiveRoomNotFoundException">直播间不存在</exception>
    /// <exception cref="StreamDataException">流数据获取失败</exception>
    /// <exception cref="SignatureGenerationException">签名生成失败</exception>
    private async Task<StreamInfo?> GetWebStreamDataAsync(string roomId, string? cookie, CancellationToken cancellationToken)
    {
        try
        {
            var headers = new Dictionary<string, string>
            {
                ["cookie"] = "ttwid=1%7C2iDIYVmjzMcpZ20fcaFde0VghXAA3NaNXE_SLR68IyE%7C1761045455%7Cab35197d5cfb21df6cbb2fa7ef1c9262206b062c315b9d04da746d0b37dfbc7d",
                ["referer"] = $"https://live.douyin.com/{roomId}",
                ["user-agent"] = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.5845.97 Safari/537.36 Core/1.116.567.400 QQBrowser/19.7.6764.400"
            };

            if (!string.IsNullOrEmpty(cookie))
            {
                headers["cookie"] = cookie;
            }

            var parameters = new Dictionary<string, string>
            {
                ["aid"] = "6383",
                ["app_name"] = "douyin_web",
                ["live_id"] = "1",
                ["device_platform"] = "web",
                ["language"] = "zh-CN",
                ["browser_language"] = "zh-CN",
                ["browser_platform"] = "Win32",
                ["browser_name"] = "Chrome",
                ["browser_version"] = "116.0.0.0",
                ["web_rid"] = roomId,
                ["msToken"] = ""
            };

            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var apiUrl = $"https://live.douyin.com/webcast/room/web/enter/?{queryString}";

            // 生成AB签名
            string signature;
            try
            {
                signature = _signatureService.GenerateSignature(queryString, headers["user-agent"]);
            }
            catch (Exception ex)
            {
                throw new SignatureGenerationException("web_api", ex);
            }

            apiUrl += $"&a_bogus={signature}";

            var jsonResponse = await _httpClient.GetStringAsync(apiUrl, headers, cancellationToken);

            // 使用JSON解析服务解析响应
            var hasData = await _jsonParsingService.ExtractPropertyAsync(jsonResponse, "data") != null;
            if (!hasData)
                throw new LiveRoomNotFoundException(roomId);

            return await ParseStreamDataFromWebAsync(jsonResponse, roomId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new LiveRoomNotFoundException(roomId, ex);
        }
        catch (HttpRequestException ex)
        {
            throw new StreamDataException($"web_api_{roomId}", ex);
        }
        catch (OperationCanceledException ex)
        {
            throw new NetworkTimeoutException(ex.Message, ex);
        }
    }

    /// <summary>
    /// 获取App端流数据
    /// </summary>
    /// <param name="roomId">房间ID</param>
    /// <param name="cookie">Cookie</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>流信息</returns>
    private async Task<StreamInfo?> GetAppStreamDataAsync(string roomId, string? cookie, CancellationToken cancellationToken)
    {
        try
        {
            var headers = new Dictionary<string, string>
            {
                ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36 Edg/141.0.0.0",
                ["Accept-Language"] = "zh-CN,zh;q=0.8,zh-TW;q=0.7,zh-HK;q=0.5,en-US;q=0.3,en;q=0.2",
                ["Referer"] = "https://live.douyin.com/"
            };

            if (!string.IsNullOrEmpty(cookie))
            {
                headers["Cookie"] = cookie;
            }

            // 如果没有房间ID或需要更多信息，尝试从房间解析服务获取
            string secUserId = "";
            if (string.IsNullOrEmpty(secUserId))
            {
                // 尝试从现有信息推断sec_user_id
                // 这里简化处理，实际可能需要更复杂的逻辑
                secUserId = ""; // 暂时留空，需要更详细的实现
            }

            var parameters = new Dictionary<string, string>
            {
                ["verifyFp"] = "verify_hwj52020_7szNlAB7_pxNY_48Vh_ALKF_GA1Uf3yteoOY",
                ["type_id"] = "0",
                ["live_id"] = "1",
                ["room_id"] = roomId,
                ["sec_user_id"] = secUserId,
                ["version_code"] = "99.99.99",
                ["app_id"] = "1128"
            };

            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var apiUrl = $"https://webcast.amemv.com/webcast/room/reflow/info/?{queryString}";

            // TODO: 需要实现AB签名，暂时跳过
            // var signature = _signatureService.GenerateSignature(queryString, headers["User-Agent"]);
            // apiUrl += $"&a_bogus={signature}";

            var jsonResponse = await _httpClient.GetStringAsync(apiUrl, headers, cancellationToken);
            var responseData = JsonSerializer.Deserialize(jsonResponse, AppJsonSerializerContext.Default.DictionaryStringObject);

            if (responseData == null || !responseData.ContainsKey("data"))
                return null;

            var data = responseData["data"] as JsonElement?;
            if (data == null || !data.Value.TryGetProperty("room", out var roomElement))
                return null;

            return ParseStreamDataFromApp(roomElement, roomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting app stream data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从Web响应解析流数据
    /// </summary>
    /// <param name="jsonResponse">JSON响应</param>
    /// <param name="roomId">房间ID</param>
    /// <returns>流信息</returns>
    /// <exception cref="LiveStreamEndedException">直播已结束</exception>
    /// <exception cref="StreamDataException">流数据解析失败</exception>
    private async Task<StreamInfo?> ParseStreamDataFromWebAsync(string jsonResponse, string roomId)
    {
        try
        {
            var streamInfo = CreateBaseStreamInfo(GenerateRoomUrl(roomId), roomId);

            // 解析主播信息
            streamInfo.AnchorName = await _jsonParsingService.ExtractStringPropertyAsync(jsonResponse, "data.room.owner.nickname");

            // 解析直播间状态
            var status = await _jsonParsingService.ExtractIntPropertyAsync(jsonResponse, "data.room.status", 4);
            streamInfo.IsLive = status == 2;

            // 检查直播状态
            if (status == 4)
            {
                throw new LiveStreamEndedException(roomId);
            }

            // 解析标题
            streamInfo.Title = await _jsonParsingService.ExtractStringPropertyAsync(jsonResponse, "data.room.title");

            // 解析流URL
            if (streamInfo.IsLive)
            {
                await ParseStreamUrlsFromJsonAsync(streamInfo, jsonResponse, "data.room.stream_url");
            }

            streamInfo.FetchTime = DateTime.UtcNow;
            return streamInfo;
        }
        catch (LiveStreamEndedException)
        {
            // 重新抛出特定的直播结束异常
            throw;
        }
        catch (Exception ex)
        {
            throw new StreamDataException($"parse_web_{roomId}", ex);
        }
    }

    /// <summary>
    /// 从Web响应解析流数据（向后兼容方法）
    /// </summary>
    /// <param name="roomElement">房间数据元素</param>
    /// <param name="roomId">房间ID</param>
    /// <returns>流信息</returns>
    private StreamInfo? ParseStreamDataFromWeb(JsonElement roomElement, string roomId)
    {
        try
        {
            var streamInfo = CreateBaseStreamInfo(GenerateRoomUrl(roomId), roomId);

            // 解析主播信息
            if (roomElement.TryGetProperty("owner", out var ownerElement))
            {
                streamInfo.AnchorName = GetStringProperty(ownerElement, "nickname");
            }

            // 解析直播间状态
            var status = GetIntProperty(roomElement, "status", 4);
            streamInfo.IsLive = status == 2;

            // 解析标题
            streamInfo.Title = GetStringProperty(roomElement, "title");

            // 解析流URL
            if (streamInfo.IsLive && roomElement.TryGetProperty("stream_url", out var streamUrlElement))
            {
                ParseStreamUrls(streamInfo, streamUrlElement);
            }

            streamInfo.FetchTime = DateTime.UtcNow;
            return streamInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing web stream data: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 从App响应解析流数据
    /// </summary>
    /// <param name="roomElement">房间数据元素</param>
    /// <param name="roomId">房间ID</param>
    /// <returns>流信息</returns>
    private StreamInfo? ParseStreamDataFromApp(JsonElement roomElement, string roomId)
    {
        try
        {
            var streamInfo = CreateBaseStreamInfo(GenerateRoomUrl(roomId), roomId);

            // 解析主播信息 (App端结构可能略有不同)
            if (roomElement.TryGetProperty("owner", out var ownerElement))
            {
                streamInfo.AnchorName = GetStringProperty(ownerElement, "nickname");
            }
            else if (roomElement.TryGetProperty("nickname", out var nicknameElement))
            {
                streamInfo.AnchorName = nicknameElement.GetString();
            }

            // 解析直播间状态
            var status = GetIntProperty(roomElement, "status", 4);
            streamInfo.IsLive = status == 2;

            // 解析标题
            streamInfo.Title = GetStringProperty(roomElement, "title");

            // 解析流URL
            if (streamInfo.IsLive && roomElement.TryGetProperty("stream_url", out var streamUrlElement))
            {
                ParseStreamUrls(streamInfo, streamUrlElement);
            }

            streamInfo.FetchTime = DateTime.UtcNow;
            return streamInfo;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing app stream data: {ex.Message}");
            return null;
        }
      }

    /// <summary>
    /// 从JSON解析流URL
    /// </summary>
    /// <param name="streamInfo">流信息对象</param>
    /// <param name="jsonResponse">JSON响应</param>
    /// <param name="streamUrlPath">流URL路径</param>
    private async Task ParseStreamUrlsFromJsonAsync(StreamInfo streamInfo, string jsonResponse, string streamUrlPath)
    {
        try
        {
            // 解析FLV流URL
            var flvData = await _jsonParsingService.ExtractPropertyAsync(jsonResponse, $"{streamUrlPath}.flv_pull_url");
            if (flvData != null)
            {
                // 将JSON对象转换为字典进行处理
                var flvDict = await _jsonParsingService.ParseToDictionaryAsync(flvData.ToString()!);
                if (flvDict != null)
                {
                    foreach (var kvp in flvDict)
                    {
                        var quality = kvp.Key;
                        var url = kvp.Value?.ToString();
                        if (!string.IsNullOrEmpty(url) && Enum.TryParse<StreamQuality>(quality, true, out var parsedQuality))
                        {
                            streamInfo.StreamUrls.Add(new StreamUrlInfo
                            {
                                Url = url,
                                Quality = parsedQuality,
                                Format = "flv"
                            });
                        }
                    }
                }
            }

            // 解析HLS流URL
            var hlsData = await _jsonParsingService.ExtractPropertyAsync(jsonResponse, $"{streamUrlPath}.hls_pull_url_map");
            if (hlsData != null)
            {
                var hlsDict = await _jsonParsingService.ParseToDictionaryAsync(hlsData.ToString()!);
                if (hlsDict != null)
                {
                    foreach (var kvp in hlsDict)
                    {
                        var quality = kvp.Key;
                        var url = kvp.Value?.ToString();
                        if (!string.IsNullOrEmpty(url) && Enum.TryParse<StreamQuality>(quality, true, out var parsedQuality))
                        {
                            streamInfo.StreamUrls.Add(new StreamUrlInfo
                            {
                                Url = url,
                                Quality = parsedQuality,
                                Format = "hls"
                            });
                        }
                    }
                }
            }

            // 设置最佳录制URL
            SetBestRecordUrl(streamInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing stream URLs from JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// 解析流URL（向后兼容方法）
    /// </summary>
    /// <param name="streamInfo">流信息对象</param>
    /// <param name="streamUrlElement">流URL数据元素</param>
    private void ParseStreamUrls(StreamInfo streamInfo, JsonElement streamUrlElement)
    {
        try
        {
            // 解析FLV流URL
            if (streamUrlElement.TryGetProperty("flv_pull_url", out var flvElement) && flvElement.ValueKind == JsonValueKind.Object)
            {
                var flvUrls = new List<StreamUrlInfo>();
                foreach (var property in flvElement.EnumerateObject())
                {
                    var quality = property.Name;
                    var url = property.Value.GetString();
                    if (!string.IsNullOrEmpty(url))
                    {
                            // 尝试解析画质枚举值，如果失败则使用默认值
                        if (Enum.TryParse<StreamQuality>(quality, true, out var parsedQuality))
                        {
                            flvUrls.Add(new StreamUrlInfo
                            {
                                Url = url,
                                Quality = parsedQuality,
                                Format = "flv"
                            });
                        }
                    }
                }
                streamInfo.StreamUrls.AddRange(flvUrls);
            }

            // 解析HLS流URL
            if (streamUrlElement.TryGetProperty("hls_pull_url_map", out var hlsElement) && hlsElement.ValueKind == JsonValueKind.Object)
            {
                var hlsUrls = new List<StreamUrlInfo>();
                foreach (var property in hlsElement.EnumerateObject())
                {
                    var quality = property.Name;
                    var url = property.Value.GetString();
                    if (!string.IsNullOrEmpty(url))
                    {
                            // 尝试解析画质枚举值，如果失败则使用默认值
                        if (Enum.TryParse<StreamQuality>(quality, true, out var parsedQuality))
                        {
                            hlsUrls.Add(new StreamUrlInfo
                            {
                                Url = url,
                                Quality = parsedQuality,
                                Format = "hls"
                            });
                        }
                    }
                }
                streamInfo.StreamUrls.AddRange(hlsUrls);
            }

            // 设置最佳录制URL
            SetBestRecordUrl(streamInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing stream URLs: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取字符串属性
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>属性值</returns>
    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString();
        }
        return null;
    }

    /// <summary>
    /// 获取整数属性
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>属性值</returns>
    private static int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
        {
            return property.GetInt32();
        }
        return defaultValue;
    }

    /// <summary>
    /// 设置最佳录制URL
    /// </summary>
    /// <param name="streamInfo">流信息对象</param>
    private static void SetBestRecordUrl(StreamInfo streamInfo)
    {
        if (streamInfo.StreamUrls.Count > 0)
        {
            // 优先选择FLV，其次选择HLS
            var bestUrl = streamInfo.StreamUrls
                .OrderByDescending(x => x.Format == "flv")
                .ThenBy(x => x.Quality)
                .FirstOrDefault();

            if (bestUrl != null)
            {
                streamInfo.RecordUrl = bestUrl.Url;
            }
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        _signatureService?.Dispose();
    }
}
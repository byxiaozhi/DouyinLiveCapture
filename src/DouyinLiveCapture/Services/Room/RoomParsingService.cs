using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using DouyinLiveCapture.Services.Utilities;

namespace DouyinLiveCapture.Services.Room;

/// <summary>
/// 房间解析服务实现
/// </summary>
public class RoomParsingService : IRoomParsingService
{
    private readonly IHttpClientService _httpClientService;
    private readonly Regex[] _roomIdPatterns;
    private readonly Dictionary<string, string> _defaultHeaders;

    /// <summary>
    /// 初始化RoomParsingService实例
    /// </summary>
    public RoomParsingService() : this(new HttpClientService(15))
    {
    }

    /// <summary>
    /// 初始化RoomParsingService实例
    /// </summary>
    /// <param name="httpClientService">HTTP客户端服务</param>
    public RoomParsingService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;

        // 房间ID解析正则表达式
        _roomIdPatterns = new[]
        {
            new Regex(@"live\.douyin\.com/(\d+)", RegexOptions.IgnoreCase),
            new Regex(@"v\.douyin\.com/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase),
            new Regex(@"iesdouyin\.com/share/user/([^/]+)", RegexOptions.IgnoreCase),
            new Regex(@"live\.douyin\.com/([^/?]+)", RegexOptions.IgnoreCase)
        };

        // 默认请求头（移动端）
        _defaultHeaders = new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Linux; Android 11; SAMSUNG SM-G973U) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/14.2 Chrome/87.0.4280.141 Mobile Safari/537.36",
            ["Accept-Language"] = "zh-CN,zh;q=0.8,zh-TW;q=0.7,zh-HK;q=0.5,en-US;q=0.3,en;q=0.2",
            ["Cookie"] = "s_v_web_id=verify_lk07kv74_QZYCUApD_xhiB_405x_Ax51_GYO9bUIyZQVf"
        };
    }

    /// <inheritdoc />
    public async Task<(string roomId, string secUserId)?> GetSecUserIdAsync(string url, string? cookie = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var headers = PrepareHeaders(cookie);

            // 发送请求并跟随重定向
            using var response = await _httpClientService.GetWithRedirectAsync(url, headers, cancellationToken);
            var redirectUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

            // 检查重定向URL是否包含reflow
            if (!redirectUrl.Contains("reflow/"))
            {
                throw new UnsupportedUrlException("The redirect URL does not contain 'reflow/'.");
            }

            // 解析房间ID和sec_user_id
            var match = Regex.Match(redirectUrl, @"sec_user_id=([\w_\-]+)&", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new InvalidOperationException("Could not find sec_user_id in the URL.");
            }

            var secUserId = match.Groups[1].Value;
            var roomId = redirectUrl.Split('?')[0].Split('/').LastOrDefault();

            if (string.IsNullOrEmpty(roomId))
            {
                throw new InvalidOperationException("Could not extract room ID from URL.");
            }

            return (roomId, secUserId);
        }
        catch (UnsupportedUrlException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting sec_user_id: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetUniqueIdAsync(string url, string? cookie = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var headers = PrepareHeaders(cookie);

            // 发送请求并跟随重定向
            using var response = await _httpClientService.GetWithRedirectAsync(url, headers, cancellationToken);
            var redirectUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

            // 如果重定向URL包含reflow，则不支持
            if (redirectUrl.Contains("reflow/"))
            {
                throw new UnsupportedUrlException("Unsupported URL");
            }

            // 提取sec_user_id
            var secUserId = redirectUrl.Split('?')[0].Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(secUserId))
            {
                throw new InvalidOperationException("Could not extract sec_user_id from URL.");
            }

            // 准备用户页面请求头
            var userPageHeaders = new Dictionary<string, string>(headers)
            {
                ["Cookie"] = "ttwid=1%7C4ejCkU2bKY76IySQENJwvGhg1IQZrgGEupSyTKKfuyk%7C1740470403%7Cbc9d2ee341f1a162f9e27f4641778030d1ae91e31f9df6553a8f2efa3bdb7b4; __ac_nonce=0683e59f3009cc48fbab0; __ac_signature=_02B4Z6wo00f01mG6waQAAIDB9JUCzFb6.TZhmsUAAPBf34; __ac_referer=__ac_blank"
            };

            // 获取用户页面
            var userPageUrl = $"https://www.iesdouyin.com/share/user/{secUserId}";
            var userPageResponse = await _httpClientService.GetStringAsync(userPageUrl, headers, cancellationToken);

            // 解析unique_id
            var matches = Regex.Matches(userPageResponse, @"unique_id"":""(.*?)""", RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var uniqueId = matches[^1].Groups[1].Value;
                return uniqueId;
            }

            throw new InvalidOperationException("Could not find unique_id in the response.");
        }
        catch (UnsupportedUrlException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting unique_id: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetLiveRoomIdAsync(string roomId, string secUserId, string? cookie = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var headers = PrepareHeaders(cookie);

            // 准备API参数
            var parameters = new Dictionary<string, string>
            {
                ["verifyFp"] = "verify_lk07kv74_QZYCUApD_xhiB_405x_Ax51_GYO9bUIyZQVf",
                ["type_id"] = "0",
                ["live_id"] = "1",
                ["room_id"] = roomId,
                ["sec_user_id"] = secUserId,
                ["app_id"] = "1128",
                ["msToken"] = "wrqzbEaTlsxt52-vxyZo_mIoL0RjNi1ZdDe7gzEGMUTVh_HvmbLLkQrA_1HKVOa2C6gkxb6IiY6TY2z8enAkPEwGq--gM-me3Yudck2ailla5Q4osnYIHxd9dI4WtQ=="
            };

            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var apiUrl = $"https://webcast.amemv.com/webcast/room/reflow/info/?{queryString}";

            // TODO: 这里需要实现X-Bogus签名，暂时跳过
            // var xbogus = await GetXBogusAsync(apiUrl, headers);
            // apiUrl += $"&X-Bogus={xbogus}";

            // 发送请求
            var response = await _httpClientService.GetStringAsync(apiUrl, headers, cancellationToken);

            // 解析JSON响应（简单的字符串解析，实际项目中应使用System.Text.Json）
            var webRidMatch = Regex.Match(response, @"""web_rid"":""([^""]+)""", RegexOptions.IgnoreCase);
            if (webRidMatch.Success)
            {
                return webRidMatch.Groups[1].Value;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting live room ID: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 准备请求头
    /// </summary>
    /// <param name="cookie">Cookie</param>
    /// <returns>请求头字典</returns>
    private Dictionary<string, string> PrepareHeaders(string? cookie)
    {
        var headers = new Dictionary<string, string>(_defaultHeaders);

        if (!string.IsNullOrEmpty(cookie))
        {
            headers["Cookie"] = cookie;
        }

        return headers;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClientService?.Dispose();
    }
}
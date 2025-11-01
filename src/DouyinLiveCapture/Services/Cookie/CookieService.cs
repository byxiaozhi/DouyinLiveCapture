using System.Text.Json;
using System.Text.Json.Serialization;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Cookie;

/// <summary>
/// Cookie 服务实现
/// </summary>
public class CookieService : ICookieService
{
    private readonly string _cookieStoragePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<string, string> _supportedPlatforms;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CookieService() : this(GetCookieStoragePath())
    {
    }

    /// <summary>
    /// 使用自定义存储路径初始化 Cookie 服务（主要用于测试）
    /// </summary>
    /// <param name="storagePath">Cookie 存储路径</param>
    public CookieService(string storagePath)
    {
        _cookieStoragePath = storagePath;
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _supportedPlatforms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "抖音", "douyin" },
            { "douyin", "douyin" },
            { "快手", "kuaishou" },
            { "kuaishou", "kuaishou" },
            { "tiktok", "tiktok" },
            { "虎牙", "huya" },
            { "huya", "huya" },
            { "斗鱼", "douyu" },
            { "douyu", "douyu" },
            { "yy", "yy" },
            { "b站", "bilibili" },
            { "bilibili", "bilibili" },
            { "小红书", "xiaohongshu" },
            { "xiaohongshu", "xiaohongshu" },
            { "bigo", "bigo" },
            { "blued", "blued" },
            { "sooplive", "sooplive" },
            { "网易cc", "netease" },
            { "netease", "netease" },
            { "千度热播", "qiandurebo" },
            { "qiandurebo", "qiandurebo" },
            { "pandatv", "pandatv" },
            { "猫耳fm", "maoerfm" },
            { "maoerfm", "maoerfm" },
            { "winktv", "winktv" },
            { "flextv", "flextv" },
            { "look", "look" },
            { "twitcasting", "twitcasting" },
            { "百度", "baidu" },
            { "baidu", "baidu" },
            { "微博", "weibo" },
            { "weibo", "weibo" },
            { "酷狗", "kugou" },
            { "kugou", "kugou" },
            { "twitch", "twitch" },
            { "liveme", "liveme" },
            { "花椒", "huajiao" },
            { "huajiao", "huajiao" },
            { "流星", "liuxing" },
            { "liuxing", "liuxing" },
            { "showroom", "showroom" },
            { "acfun", "acfun" },
            { "畅聊", "changliao" },
            { "changliao", "changliao" },
            { "映客", "yingke" },
            { "yingke", "yingke" },
            { "音播", "yinbo" },
            { "yinbo", "yinbo" },
            { "知乎", "zhihu" },
            { "zhihu", "zhihu" },
            { "chzzk", "chzzk" },
            { "嗨秀", "haixiu" },
            { "haixiu", "haixiu" },
            { "vvxqiu", "vvxqiu" },
            { "17live", "17live" },
            { "langlive", "langlive" },
            { "pplive", "pplive" },
            { "6room", "6room" },
            { "lehaitv", "lehaitv" },
            { "huamao", "huamao" },
            { "shopee", "shopee" },
            { "youtube", "youtube" },
            { "淘宝", "taobao" },
            { "taobao", "taobao" },
            { "京东", "jd" },
            { "jd", "jd" },
            { "faceit", "faceit" },
            { "咪咕", "migu" },
            { "migu", "migu" },
            { "连接", "lianjie" },
            { "lianjie", "lianjie" },
            { "来秀", "laixiu" },
            { "laixiu", "laixiu" },
            { "picarto", "picarto" }
        };

        // 确保存储目录存在
        var directory = Path.GetDirectoryName(_cookieStoragePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetCookieAsync(string platform, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("Platform cannot be null or empty", nameof(platform));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cookies = await LoadCookiesAsync(cancellationToken);
            var normalizedPlatform = NormalizePlatformName(platform);
            cookies.TryGetValue(normalizedPlatform, out var cookie);
            return cookie;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetCookieAsync(string platform, string cookie, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("Platform cannot be null or empty", nameof(platform));
        }

        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentException("Cookie cannot be null or empty", nameof(cookie));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cookies = await LoadCookiesAsync(cancellationToken);
            var normalizedPlatform = NormalizePlatformName(platform);
            cookies[normalizedPlatform] = cookie.Trim();
            await SaveCookiesAsync(cookies, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCookieAsync(string platform, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("Platform cannot be null or empty", nameof(platform));
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var cookies = await LoadCookiesAsync(cancellationToken);
            var normalizedPlatform = NormalizePlatformName(platform);
            var removed = cookies.Remove(normalizedPlatform);

            if (removed)
            {
                await SaveCookiesAsync(cookies, cancellationToken);
            }

            return removed;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetAllCookiesAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await LoadCookiesAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasCookieAsync(string platform, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("Platform cannot be null or empty", nameof(platform));
        }

        var cookie = await GetCookieAsync(platform, cancellationToken);
        return !string.IsNullOrEmpty(cookie);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedPlatforms()
    {
        return _supportedPlatforms.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 规范化平台名称
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <returns>规范化的平台名称</returns>
    private string NormalizePlatformName(string platform)
    {
        if (_supportedPlatforms.TryGetValue(platform, out var normalized))
        {
            return normalized;
        }

        // 如果不在支持列表中，返回小写形式
        return platform.ToLowerInvariant();
    }

    /// <summary>
    /// 加载 Cookie 数据
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Cookie 字典</returns>
    private async Task<Dictionary<string, string>> LoadCookiesAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_cookieStoragePath))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var json = await File.ReadAllTextAsync(_cookieStoragePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var cookies = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.DictionaryStringString);
            return cookies ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load cookies from {_cookieStoragePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 保存 Cookie 数据
    /// </summary>
    /// <param name="cookies">Cookie 字典</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    private async Task SaveCookiesAsync(Dictionary<string, string> cookies, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(cookies, AppJsonSerializerContext.Default.DictionaryStringString);
            await File.WriteAllTextAsync(_cookieStoragePath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save cookies to {_cookieStoragePath}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取 Cookie 存储路径
    /// </summary>
    /// <returns>存储路径</returns>
    private static string GetCookieStoragePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "DouyinLiveCapture");
        return Path.Combine(appFolder, "cookies.json");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
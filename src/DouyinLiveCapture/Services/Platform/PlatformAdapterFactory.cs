using DouyinLiveCapture.Services.Platform.Adapters;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 平台适配器工厂实现
/// </summary>
public class PlatformAdapterFactory : IPlatformAdapterFactory
{
    private readonly Dictionary<PlatformType, Func<IPlatformAdapter>> _adapterFactories;
    private readonly Dictionary<string, PlatformType> _urlPatterns;

    public PlatformAdapterFactory()
    {
        _adapterFactories = new Dictionary<PlatformType, Func<IPlatformAdapter>>
        {
            { PlatformType.Douyin, () => new DouyinPlatformAdapter() },
            { PlatformType.Kuaishou, () => new KuaishouPlatformAdapter() },
            { PlatformType.Bilibili, () => new BilibiliPlatformAdapter() },
            { PlatformType.Huya, () => new HuyaPlatformAdapter() },
            { PlatformType.Douyu, () => new DouyuPlatformAdapter() },
            { PlatformType.TikTok, () => new TiktokPlatformAdapter() },
            { PlatformType.SoopLive, () => new SooplivePlatformAdapter() },
            { PlatformType.FlexTV, () => new FlextvPlatformAdapter() },
            { PlatformType.PopkonTV, () => new PopkontvPlatformAdapter() },
            { PlatformType.TwitchTV, () => new TwitchPlatformAdapter() }
        };

        _urlPatterns = new Dictionary<string, PlatformType>(StringComparer.OrdinalIgnoreCase)
        {
            // 抖音
            { "live.douyin.com", PlatformType.Douyin },
            { "v.douyin.com", PlatformType.Douyin },
            { "iesdouyin.com", PlatformType.Douyin },

            // 快手
            { "live.kuaishou.com", PlatformType.Kuaishou },
            { "kuaishou.com", PlatformType.Kuaishou },

            // B站
            { "live.bilibili.com", PlatformType.Bilibili },
            { "bilibili.com", PlatformType.Bilibili },

            // 虎牙
            { "www.huya.com", PlatformType.Huya },
            { "huya.com", PlatformType.Huya },

            // 斗鱼
            { "www.douyu.com", PlatformType.Douyu },
            { "douyu.com", PlatformType.Douyu },

            // TikTok
            { "www.tiktok.com", PlatformType.TikTok },
            { "tiktok.com", PlatformType.TikTok },

            // SOOP
            { "www.sooplive.com", PlatformType.SoopLive },
            { "sooplive.com", PlatformType.SoopLive },

            // FlexTV
            { "www.flextv.co.kr", PlatformType.FlexTV },
            { "flextv.co.kr", PlatformType.FlexTV },

            // PopkonTV
            { "www.popkontv.com", PlatformType.PopkonTV },
            { "popkontv.com", PlatformType.PopkonTV },

            // Twitch
            { "www.twitch.tv", PlatformType.TwitchTV },
            { "twitch.tv", PlatformType.TwitchTV }
        };
    }

    /// <inheritdoc />
    public IPlatformAdapter? CreateAdapter(PlatformType platformType)
    {
        if (_adapterFactories.TryGetValue(platformType, out var factory))
        {
            return factory();
        }

        return null;
    }

    /// <inheritdoc />
    public IPlatformAdapter? CreateAdapterByUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var platformType = DetectPlatformByUrl(url);
        return platformType != PlatformType.Unknown ? CreateAdapter(platformType) : null;
    }

    /// <inheritdoc />
    public IReadOnlyList<PlatformType> GetSupportedPlatforms()
    {
        return _adapterFactories.Keys.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public bool IsSupported(PlatformType platformType)
    {
        return _adapterFactories.ContainsKey(platformType);
    }

    /// <summary>
    /// 通过URL检测平台类型
    /// </summary>
    /// <param name="url">URL</param>
    /// <returns>平台类型</returns>
    private PlatformType DetectPlatformByUrl(string url)
    {
        try
        {
            var uri = new Uri(url.ToLowerInvariant());
            var host = uri.Host;

            // 检查精确匹配
            if (_urlPatterns.TryGetValue(host, out var platformType))
            {
                return platformType;
            }

            // 检查域名包含匹配
            foreach (var pattern in _urlPatterns)
            {
                if (host.Contains(pattern.Key))
                {
                    return pattern.Value;
                }
            }
        }
        catch
        {
            // URL解析失败，返回未知
        }

        return PlatformType.Unknown;
    }
}
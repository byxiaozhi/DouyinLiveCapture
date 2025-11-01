using System.Text.RegularExpressions;
using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 抖音平台适配器
/// </summary>
public class DouyinPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Douyin;

    /// <inheritdoc />
    public override string PlatformName => "抖音";

    private static readonly Regex[] RoomIdPatterns =
    {
        new Regex(@"live\.douyin\.com/(\d+)", RegexOptions.IgnoreCase)
    };

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var roomId = ParseRoomId(roomUrl);
        if (string.IsNullOrEmpty(roomId))
            return null;

        var streamInfo = CreateBaseStreamInfo(roomUrl, roomId);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return ExtractRoomIdFromUrl(roomUrl, RoomIdPatterns);
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return $"https://live.douyin.com/{roomId}";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "douyin.com", "live.douyin.com");
    }
}

/// <summary>
/// B站平台适配器
/// </summary>
public class BilibiliPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Bilibili;

    /// <inheritdoc />
    public override string PlatformName => "B站";

    private static readonly Regex[] RoomIdPatterns =
    {
        new Regex(@"live\.bilibili\.com/(\d+)", RegexOptions.IgnoreCase),
        new Regex(@"bilibili\.com/(\d+)", RegexOptions.IgnoreCase)
    };

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var roomId = ParseRoomId(roomUrl);
        if (string.IsNullOrEmpty(roomId))
            return null;

        var streamInfo = CreateBaseStreamInfo(roomUrl, roomId);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return ExtractRoomIdFromUrl(roomUrl, RoomIdPatterns);
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return $"https://live.bilibili.com/{roomId}";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "bilibili.com", "live.bilibili.com");
    }
}

/// <summary>
/// 虎牙平台适配器
/// </summary>
public class HuyaPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Huya;

    /// <inheritdoc />
    public override string PlatformName => "虎牙";

    private static readonly Regex[] RoomIdPatterns =
    {
        new Regex(@"huya\.com/(\d+)", RegexOptions.IgnoreCase)
    };

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var roomId = ParseRoomId(roomUrl);
        if (string.IsNullOrEmpty(roomId))
            return null;

        var streamInfo = CreateBaseStreamInfo(roomUrl, roomId);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return ExtractRoomIdFromUrl(roomUrl, RoomIdPatterns);
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return $"https://www.huya.com/{roomId}";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "huya.com", "www.huya.com");
    }
}

/// <summary>
/// 斗鱼平台适配器
/// </summary>
public class DouyuPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Douyu;

    /// <inheritdoc />
    public override string PlatformName => "斗鱼";

    private static readonly Regex[] RoomIdPatterns =
    {
        new Regex(@"douyu\.com/(\d+)", RegexOptions.IgnoreCase)
    };

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var roomId = ParseRoomId(roomUrl);
        if (string.IsNullOrEmpty(roomId))
            return null;

        var streamInfo = CreateBaseStreamInfo(roomUrl, roomId);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return ExtractRoomIdFromUrl(roomUrl, RoomIdPatterns);
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return $"https://www.douyu.com/{roomId}";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "douyu.com", "www.douyu.com");
    }
}

/// <summary>
/// SOOP平台适配器
/// </summary>
public class SooplivePlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.SoopLive;

    /// <inheritdoc />
    public override string PlatformName => "SOOP";

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var streamInfo = CreateBaseStreamInfo(roomUrl, string.Empty);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return null;
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "sooplive.com", "www.sooplive.com");
    }
}

/// <summary>
/// FlexTV平台适配器
/// </summary>
public class FlextvPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.FlexTV;

    /// <inheritdoc />
    public override string PlatformName => "FlexTV";

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var streamInfo = CreateBaseStreamInfo(roomUrl, string.Empty);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return null;
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "flextv.co.kr", "www.flextv.co.kr");
    }
}

/// <summary>
/// PopkonTV平台适配器
/// </summary>
public class PopkontvPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.PopkonTV;

    /// <inheritdoc />
    public override string PlatformName => "PopkonTV";

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var streamInfo = CreateBaseStreamInfo(roomUrl, string.Empty);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return null;
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "popkontv.com", "www.popkontv.com");
    }
}

/// <summary>
/// Twitch平台适配器
/// </summary>
public class TwitchPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.TwitchTV;

    /// <inheritdoc />
    public override string PlatformName => "Twitch";

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var streamInfo = CreateBaseStreamInfo(roomUrl, string.Empty);
        streamInfo.IsLive = false;
        streamInfo.LiveStatus = LiveStatus.Offline;

        return await Task.FromResult(streamInfo);
    }

    /// <inheritdoc />
    public override async Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    /// <inheritdoc />
    public override string? ParseRoomId(string roomUrl)
    {
        return null;
    }

    /// <inheritdoc />
    public override string GenerateRoomUrl(string roomId)
    {
        return string.Empty;
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "twitch.tv", "www.twitch.tv");
    }
}
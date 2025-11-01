using System.Text.RegularExpressions;
using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 快手平台适配器
/// </summary>
public class KuaishouPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Kuaishou;

    /// <inheritdoc />
    public override string PlatformName => "快手";

    private static readonly Regex[] RoomIdPatterns =
    {
        new Regex(@"live\.kuaishou\.com/profile/([^/]+)", RegexOptions.IgnoreCase),
        new Regex(@"kuaishou\.com/profile/([^/]+)", RegexOptions.IgnoreCase)
    };

    /// <inheritdoc />
    public override async Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        var roomId = ParseRoomId(roomUrl);
        if (string.IsNullOrEmpty(roomId))
            return null;

        var streamInfo = CreateBaseStreamInfo(roomUrl, roomId);
        streamInfo.IsLive = false; // 简化实现，默认离线
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
        return $"https://live.kuaishou.com/profile/{roomId}";
    }

    /// <inheritdoc />
    public override bool IsSupportedUrl(string url)
    {
        return IsUrlMatchDomain(url, "kuaishou.com", "live.kuaishou.com");
    }
}
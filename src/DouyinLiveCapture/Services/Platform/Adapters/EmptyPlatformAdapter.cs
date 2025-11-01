using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 空平台适配器（用于暂时不支持的平台）
/// </summary>
public class EmptyPlatformAdapter : BasePlatformAdapter
{
    /// <inheritdoc />
    public override PlatformType PlatformType => PlatformType.Unknown;

    /// <inheritdoc />
    public override string PlatformName => "Unknown";

    /// <inheritdoc />
    public override Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<StreamInfo?>(null);
    }

    /// <inheritdoc />
    public override Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
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
        return false;
    }
}
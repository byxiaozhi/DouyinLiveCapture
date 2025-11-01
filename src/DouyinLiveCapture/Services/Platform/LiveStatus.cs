namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 直播状态枚举
/// </summary>
public enum LiveStatus
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown,

    /// <summary>
    /// 离线
    /// </summary>
    Offline,

    /// <summary>
    /// 直播中
    /// </summary>
    Live,

    /// <summary>
    /// 轮播中
    /// </summary>
    Playback,

    /// <summary>
    /// 暂停直播
    /// </summary>
    Paused
}
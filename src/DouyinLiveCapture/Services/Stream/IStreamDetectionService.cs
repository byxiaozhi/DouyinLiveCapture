using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Stream;

/// <summary>
/// 直播流检测服务接口
/// </summary>
public interface IStreamDetectionService : IDisposable
{
    /// <summary>
    /// 检测单个直播流状态
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cookie">Cookie信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>直播流信息</returns>
    Task<StreamInfo?> DetectStreamAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量检测多个直播流状态
    /// </summary>
    /// <param name="roomUrls">直播间URL列表</param>
    /// <param name="cookieProvider">Cookie提供器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>直播流信息列表</returns>
    Task<IReadOnlyList<StreamInfo>> DetectStreamsAsync(
        IEnumerable<string> roomUrls,
        Func<string, Task<string?>>? cookieProvider = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查直播流是否在线
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cookie">Cookie信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在线</returns>
    Task<bool> IsStreamLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启动流状态监控
    /// </summary>
    /// <param name="roomUrls">要监控的直播间URL列表</param>
    /// <param name="checkInterval">检查间隔</param>
    /// <param name="onStreamStatusChanged">状态变化回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>监控任务</returns>
    Task StartMonitoringAsync(
        IEnumerable<string> roomUrls,
        TimeSpan checkInterval,
        Action<StreamInfo>? onStreamStatusChanged = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止流状态监控
    /// </summary>
    void StopMonitoring();
}
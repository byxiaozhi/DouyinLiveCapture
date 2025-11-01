using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Configuration;
using DouyinLiveCapture.Services.Recording;

namespace DouyinLiveCapture.Services.LiveRecording;

/// <summary>
/// 直播录制服务接口
/// </summary>
public interface ILiveRecordingService
{
    /// <summary>
    /// 启动直播录制服务
    /// </summary>
    /// <param name="roomUrls">要监控的直播间URL列表</param>
    /// <param name="settings">录制设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>启动任务</returns>
    Task StartAsync(IEnumerable<string> roomUrls, RecordingSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止直播录制服务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取服务状态
    /// </summary>
    /// <returns>服务状态</returns>
    LiveRecordingServiceStatus GetStatus();

    /// <summary>
    /// 获取监控的直播间状态
    /// </summary>
    /// <returns>直播间状态列表</returns>
    Task<IReadOnlyList<MonitoredRoomStatus>> GetMonitoredRoomsAsync();

    /// <summary>
    /// 添加直播间到监控列表
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>添加任务</returns>
    Task AddRoomAsync(string roomUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从监控列表移除直播间
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>移除任务</returns>
    Task RemoveRoomAsync(string roomUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 手动触发录制
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>录制任务</returns>
    Task<RecordingTask?> StartManualRecordingAsync(string roomUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止指定直播间的录制
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>停止任务</returns>
    Task StopRecordingAsync(string roomUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// 服务状态变化事件
    /// </summary>
    event EventHandler<LiveRecordingServiceStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// 直播间状态变化事件
    /// </summary>
    event EventHandler<MonitoredRoomStatusChangedEventArgs>? RoomStatusChanged;

    /// <summary>
    /// 录制任务状态变化事件
    /// </summary>
    event EventHandler<RecordingTaskStatusChangedEventArgs>? RecordingTaskStatusChanged;
}

/// <summary>
/// 服务状态
/// </summary>
public enum LiveRecordingServiceStatus
{
    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// 启动中
    /// </summary>
    Starting,

    /// <summary>
    /// 运行中
    /// </summary>
    Running,

    /// <summary>
    /// 停止中
    /// </summary>
    Stopping,

    /// <summary>
    /// 错误
    /// </summary>
    Error
}

/// <summary>
/// 监控的直播间状态
/// </summary>
public class MonitoredRoomStatus
{
    /// <summary>
    /// 直播间URL
    /// </summary>
    public string RoomUrl { get; set; } = string.Empty;

    /// <summary>
    /// 直播流信息
    /// </summary>
    public StreamInfo? StreamInfo { get; set; }

    /// <summary>
    /// 是否正在监控
    /// </summary>
    public bool IsMonitoring { get; set; }

    /// <summary>
    /// 是否正在录制
    /// </summary>
    public bool IsRecording { get; set; }

    /// <summary>
    /// 当前录制任务ID
    /// </summary>
    public string? RecordingTaskId { get; set; }

    /// <summary>
    /// 最后检查时间
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 录制次数
    /// </summary>
    public int RecordingCount { get; set; }

    /// <summary>
    /// 成功录制次数
    /// </summary>
    public int SuccessfulRecordings { get; set; }
}

/// <summary>
/// 服务状态变化事件参数
/// </summary>
public class LiveRecordingServiceStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 之前的状态
    /// </summary>
    public LiveRecordingServiceStatus PreviousStatus { get; set; }

    /// <summary>
    /// 当前的状态
    /// </summary>
    public LiveRecordingServiceStatus CurrentStatus { get; set; }

    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 监控直播间状态变化事件参数
/// </summary>
public class MonitoredRoomStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 直播间URL
    /// </summary>
    public string RoomUrl { get; set; } = string.Empty;

    /// <summary>
    /// 之前的状态
    /// </summary>
    public MonitoredRoomStatus? PreviousStatus { get; set; }

    /// <summary>
    /// 当前的状态
    /// </summary>
    public MonitoredRoomStatus CurrentStatus { get; set; } = null!;

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 录制任务状态变化事件参数
/// </summary>
public class RecordingTaskStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// 录制任务
    /// </summary>
    public RecordingTask RecordingTask { get; set; } = null!;

    /// <summary>
    /// 之前的状态
    /// </summary>
    public RecordingTaskStatus PreviousStatus { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
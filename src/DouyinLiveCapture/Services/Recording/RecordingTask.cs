using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Recording;

/// <summary>
/// 录制任务状态
/// </summary>
public enum RecordingTaskStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Pending,

    /// <summary>
    /// 录制中
    /// </summary>
    Recording,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed,

    /// <summary>
    /// 已停止
    /// </summary>
    Stopped,

    /// <summary>
    /// 错误
    /// </summary>
    Error
}

/// <summary>
/// 录制任务信息
/// </summary>
public class RecordingTask
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 直播流信息
    /// </summary>
    public StreamInfo StreamInfo { get; set; } = null!;

    /// <summary>
    /// 任务状态
    /// </summary>
    public RecordingTaskStatus Status { get; set; }

    /// <summary>
    /// 输出文件路径
    /// </summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 录制时长
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 录制进程ID（如果使用外部进程）
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Configuration;

namespace DouyinLiveCapture.Services.Recording;

/// <summary>
/// 录制服务接口
/// </summary>
public interface IRecordingService : IDisposable
{
    /// <summary>
    /// 开始录制直播流
    /// </summary>
    /// <param name="streamInfo">直播流信息</param>
    /// <param name="settings">录制设置</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>录制任务</returns>
    Task<RecordingTask> StartRecordingAsync(StreamInfo streamInfo, RecordingSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止录制
    /// </summary>
    /// <param name="taskId">录制任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>录制任务</returns>
    Task<RecordingTask?> StopRecordingAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取录制任务状态
    /// </summary>
    /// <param name="taskId">录制任务ID</param>
    /// <returns>录制任务信息</returns>
    Task<RecordingTask?> GetRecordingTaskAsync(string taskId);

    /// <summary>
    /// 获取所有录制任务
    /// </summary>
    /// <returns>录制任务列表</returns>
    Task<IReadOnlyList<RecordingTask>> GetAllRecordingTasksAsync();

    /// <summary>
    /// 检查磁盘空间
    /// </summary>
    /// <param name="path">检查路径</param>
    /// <param name="requiredSpaceGB">所需空间（GB）</param>
    /// <returns>是否有足够空间</returns>
    Task<bool> CheckDiskSpaceAsync(string path, double requiredSpaceGB = 1.0);

    /// <summary>
    /// 生成录制文件名
    /// </summary>
    /// <param name="streamInfo">直播流信息</param>
    /// <param name="settings">录制设置</param>
    /// <returns>文件名</returns>
    string GenerateRecordingFileName(StreamInfo streamInfo, RecordingSettings settings);
}
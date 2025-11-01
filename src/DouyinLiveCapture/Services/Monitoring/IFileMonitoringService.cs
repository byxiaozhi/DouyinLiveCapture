namespace DouyinLiveCapture.Services.Monitoring;

/// <summary>
/// 文件监控服务接口
/// </summary>
public interface IFileMonitoringService
{
    /// <summary>
    /// 开始监控文件变化
    /// </summary>
    /// <param name="path">监控路径</param>
    /// <param name="filter">文件过滤器</param>
    /// <param name="onFileChanged">文件变化回调</param>
    /// <param name="includeSubdirectories">是否包含子目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>监控任务</returns>
    Task StartMonitoringAsync(
        string path,
        string filter = "*.*",
        Action<FileChangeEventArgs>? onFileChanged = null,
        bool includeSubdirectories = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止监控
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否存在</returns>
    bool FileExists(string filePath);

    /// <summary>
    /// 获取文件大小
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件大小（字节）</returns>
    long GetFileSize(string filePath);

    /// <summary>
    /// 监控文件大小变化
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="checkInterval">检查间隔</param>
    /// <param name="onSizeChanged">大小变化回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>监控任务</returns>
    Task MonitorFileSizeAsync(
        string filePath,
        TimeSpan checkInterval,
        Action<FileSizeChangeEventArgs>? onSizeChanged = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 文件变化事件参数
/// </summary>
public class FileChangeEventArgs
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 变化类型
    /// </summary>
    public FileChangeType ChangeType { get; set; }

    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 文件大小变化事件参数
/// </summary>
public class FileSizeChangeEventArgs
{
    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 之前的大小
    /// </summary>
    public long PreviousSize { get; set; }

    /// <summary>
    /// 当前的大小
    /// </summary>
    public long CurrentSize { get; set; }

    /// <summary>
    /// 大小变化量
    /// </summary>
    public long SizeChange => CurrentSize - PreviousSize;

    /// <summary>
    /// 发生时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 文件变化类型
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// 创建
    /// </summary>
    Created,

    /// <summary>
    /// 修改
    /// </summary>
    Modified,

    /// <summary>
    /// 删除
    /// </summary>
    Deleted,

    /// <summary>
    /// 重命名
    /// </summary>
    Renamed
}
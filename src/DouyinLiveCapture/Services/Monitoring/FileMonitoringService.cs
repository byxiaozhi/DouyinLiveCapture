namespace DouyinLiveCapture.Services.Monitoring;

/// <summary>
/// 文件监控服务实现
/// </summary>
public class FileMonitoringService : IFileMonitoringService, IDisposable
{
    private FileSystemWatcher? _fileSystemWatcher;
    private Timer? _fileSizeMonitorTimer;
    private readonly Dictionary<string, long> _lastKnownFileSizes;
    private readonly object _lockObject = new object();
    private CancellationTokenSource? _monitoringCts;

    public FileMonitoringService()
    {
        _lastKnownFileSizes = new Dictionary<string, long>();
    }

    /// <inheritdoc />
    public Task StartMonitoringAsync(
        string path,
        string filter = "*.*",
        Action<FileChangeEventArgs>? onFileChanged = null,
        bool includeSubdirectories = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        return Task.Run(() =>
        {
            lock (_lockObject)
            {
                StopMonitoring(); // 停止现有监控

                _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _fileSystemWatcher = new FileSystemWatcher(path, filter)
                {
                    IncludeSubdirectories = includeSubdirectories,
                    NotifyFilter = NotifyFilters.FileName |
                                   NotifyFilters.LastWrite |
                                   NotifyFilters.Size |
                                   NotifyFilters.Attributes
                };

                // 设置事件处理器
                _fileSystemWatcher.Created += (sender, e) =>
                {
                    onFileChanged?.Invoke(new FileChangeEventArgs
                    {
                        FilePath = e.FullPath,
                        ChangeType = FileChangeType.Created
                    });
                };

                _fileSystemWatcher.Changed += (sender, e) =>
                {
                    onFileChanged?.Invoke(new FileChangeEventArgs
                    {
                        FilePath = e.FullPath,
                        ChangeType = FileChangeType.Modified
                    });
                };

                _fileSystemWatcher.Deleted += (sender, e) =>
                {
                    onFileChanged?.Invoke(new FileChangeEventArgs
                    {
                        FilePath = e.FullPath,
                        ChangeType = FileChangeType.Deleted
                    });

                    lock (_lastKnownFileSizes)
                    {
                        _lastKnownFileSizes.Remove(e.FullPath);
                    }
                };

                _fileSystemWatcher.Renamed += (sender, e) =>
                {
                    onFileChanged?.Invoke(new FileChangeEventArgs
                    {
                        FilePath = e.FullPath,
                        ChangeType = FileChangeType.Renamed
                    });

                    lock (_lastKnownFileSizes)
                    {
                        if (_lastKnownFileSizes.TryGetValue(e.OldFullPath, out var oldSize))
                        {
                            _lastKnownFileSizes.Remove(e.OldFullPath);
                            _lastKnownFileSizes[e.FullPath] = oldSize;
                        }
                    }
                };

                _fileSystemWatcher.EnableRaisingEvents = true;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (_lockObject)
        {
            _fileSystemWatcher?.EnableRaisingEvents = false;
            _fileSystemWatcher?.Dispose();
            _fileSystemWatcher = null;

            _fileSizeMonitorTimer?.Dispose();
            _fileSizeMonitorTimer = null;

            _monitoringCts?.Cancel();
            _monitoringCts?.Dispose();
            _monitoringCts = null;
        }
    }

    /// <inheritdoc />
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <inheritdoc />
    public long GetFileSize(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return 0;

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <inheritdoc />
    public Task MonitorFileSizeAsync(
        string filePath,
        TimeSpan checkInterval,
        Action<FileSizeChangeEventArgs>? onSizeChanged = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        return Task.Run(() =>
        {
            // 记录初始文件大小
            long initialSize;
            lock (_lastKnownFileSizes)
            {
                initialSize = GetFileSize(filePath);
                _lastKnownFileSizes[filePath] = initialSize;
            }

            // 启动定时器
            _fileSizeMonitorTimer = new Timer(async _ =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                try
                {
                    var currentSize = GetFileSize(filePath);
                    long previousSize;

                    lock (_lastKnownFileSizes)
                    {
                        previousSize = _lastKnownFileSizes.GetValueOrDefault(filePath, 0);
                        _lastKnownFileSizes[filePath] = currentSize;
                    }

                    // 如果大小发生变化，触发回调
                    if (currentSize != previousSize && onSizeChanged != null)
                    {
                        onSizeChanged(new FileSizeChangeEventArgs
                        {
                            FilePath = filePath,
                            PreviousSize = previousSize,
                            CurrentSize = currentSize
                        });
                    }
                }
                catch
                {
                    // 忽略监控错误
                }
            }, null, checkInterval, checkInterval);
        }, cancellationToken);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
    }
}
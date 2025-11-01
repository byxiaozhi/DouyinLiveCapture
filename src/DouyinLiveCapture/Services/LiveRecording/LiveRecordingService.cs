using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Configuration;
using DouyinLiveCapture.Services.Cookie;
using DouyinLiveCapture.Services.Stream;
using DouyinLiveCapture.Services.Recording;
using DouyinLiveCapture.Services.Monitoring;

namespace DouyinLiveCapture.Services.LiveRecording;

/// <summary>
/// 直播录制服务实现
/// </summary>
public class LiveRecordingService : ILiveRecordingService, IDisposable
{
    private readonly IStreamDetectionService _streamDetectionService;
    private readonly IRecordingService _recordingService;
    private readonly ICookieService _cookieService;
    private readonly IFileMonitoringService _fileMonitoringService;

    private readonly Dictionary<string, MonitoredRoomStatus> _monitoredRooms;
    private readonly Dictionary<string, string> _roomToTaskMap;
    private readonly SemaphoreSlim _semaphore;
    private readonly object _lockObject = new object();

    private LiveRecordingServiceStatus _status;
    private CancellationTokenSource? _serviceCts;
    private Timer? _statusCheckTimer;
    private RecordingSettings? _currentSettings;

    public event EventHandler<LiveRecordingServiceStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<MonitoredRoomStatusChangedEventArgs>? RoomStatusChanged;
    public event EventHandler<RecordingTaskStatusChangedEventArgs>? RecordingTaskStatusChanged;

    public LiveRecordingService(
        IStreamDetectionService streamDetectionService,
        IRecordingService recordingService,
        ICookieService cookieService,
        IFileMonitoringService fileMonitoringService)
    {
        _streamDetectionService = streamDetectionService ?? throw new ArgumentNullException(nameof(streamDetectionService));
        _recordingService = recordingService ?? throw new ArgumentNullException(nameof(recordingService));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _fileMonitoringService = fileMonitoringService ?? throw new ArgumentNullException(nameof(fileMonitoringService));

        _monitoredRooms = new Dictionary<string, MonitoredRoomStatus>();
        _roomToTaskMap = new Dictionary<string, string>();
        _semaphore = new SemaphoreSlim(1, 1);
        _status = LiveRecordingServiceStatus.Stopped;
    }

    /// <inheritdoc />
    public async Task StartAsync(IEnumerable<string> roomUrls, RecordingSettings settings, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_status == LiveRecordingServiceStatus.Running)
                return;

            SetStatus(LiveRecordingServiceStatus.Starting);

            _currentSettings = settings ?? throw new ArgumentNullException(nameof(settings));
            _serviceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var urlList = roomUrls?.ToList() ?? new List<string>();
            if (urlList.Count == 0)
            {
                SetStatus(LiveRecordingServiceStatus.Running);
                return;
            }

            // 初始化监控房间
            foreach (var url in urlList)
            {
                await AddRoomInternalAsync(url);
            }

            // 启动流状态监控
            await _streamDetectionService.StartMonitoringAsync(
                urlList,
                TimeSpan.FromSeconds(settings.LoopInterval),
                OnStreamStatusChanged,
                _serviceCts.Token);

            // 启动状态检查定时器
            _statusCheckTimer = new Timer(async _ =>
            {
                await CheckRecordingStatusesAsync();
            }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            SetStatus(LiveRecordingServiceStatus.Running);
        }
        catch (Exception ex)
        {
            SetStatus(LiveRecordingServiceStatus.Error, ex.Message);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_status != LiveRecordingServiceStatus.Running)
                return;

            SetStatus(LiveRecordingServiceStatus.Stopping);

            // 停止监控
            _streamDetectionService.StopMonitoring();

            // 停止所有录制
            var recordingTasks = new List<Task>();
            foreach (var kvp in _roomToTaskMap.ToList())
            {
                recordingTasks.Add(StopRecordingInternalAsync(kvp.Key, kvp.Value));
            }

            await Task.WhenAll(recordingTasks);

            // 清理资源
            _statusCheckTimer?.Dispose();
            _statusCheckTimer = null;
            _serviceCts?.Cancel();
            _serviceCts?.Dispose();
            _serviceCts = null;

            SetStatus(LiveRecordingServiceStatus.Stopped);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public LiveRecordingServiceStatus GetStatus()
    {
        return _status;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MonitoredRoomStatus>> GetMonitoredRoomsAsync()
    {
        await Task.CompletedTask; // 保持异步签名一致性

        lock (_lockObject)
        {
            return _monitoredRooms.Values.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public async Task AddRoomAsync(string roomUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            throw new ArgumentException("Room URL cannot be null or empty", nameof(roomUrl));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await AddRoomInternalAsync(roomUrl);

            if (_status == LiveRecordingServiceStatus.Running)
            {
                // 如果服务正在运行，立即开始监控新添加的房间
                await _streamDetectionService.StartMonitoringAsync(
                    new[] { roomUrl },
                    TimeSpan.FromSeconds(_currentSettings?.LoopInterval ?? 60),
                    OnStreamStatusChanged,
                    cancellationToken);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveRoomAsync(string roomUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            throw new ArgumentException("Room URL cannot be null or empty", nameof(roomUrl));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await RemoveRoomInternalAsync(roomUrl);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RecordingTask?> StartManualRecordingAsync(string roomUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            throw new ArgumentException("Room URL cannot be null or empty", nameof(roomUrl));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await StartRecordingInternalAsync(roomUrl, isManual: true);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopRecordingAsync(string roomUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            throw new ArgumentException("Room URL cannot be null or empty", nameof(roomUrl));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_roomToTaskMap.TryGetValue(roomUrl, out var taskId))
            {
                await StopRecordingInternalAsync(roomUrl, taskId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 流状态变化回调
    /// </summary>
    private async void OnStreamStatusChanged(StreamInfo streamInfo)
    {
        if (streamInfo == null)
            return;

        await _semaphore.WaitAsync();
        try
        {
            var roomUrl = streamInfo.Url;
            MonitoredRoomStatus previousStatus = null;

            lock (_lockObject)
            {
                if (_monitoredRooms.TryGetValue(roomUrl, out var currentStatus))
                {
                    previousStatus = CloneMonitoredRoomStatus(currentStatus);
                    currentStatus.StreamInfo = streamInfo;
                    currentStatus.LastCheckTime = DateTime.UtcNow;
                    currentStatus.ErrorMessage = null;
                }
                else
                {
                    currentStatus = new MonitoredRoomStatus
                    {
                        RoomUrl = roomUrl,
                        StreamInfo = streamInfo,
                        IsMonitoring = true,
                        LastCheckTime = DateTime.UtcNow
                    };
                    _monitoredRooms[roomUrl] = currentStatus;
                }
            }

            // 处理录制逻辑
            await HandleRecordingLogicAsync(streamInfo, previousStatus);

            // 触发事件
            RoomStatusChanged?.Invoke(this, new MonitoredRoomStatusChangedEventArgs
            {
                RoomUrl = roomUrl,
                PreviousStatus = previousStatus,
                CurrentStatus = (await GetMonitoredRoomsAsync()).First(r => r.RoomUrl == roomUrl)
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling stream status change: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 处理录制逻辑
    /// </summary>
    private async Task HandleRecordingLogicAsync(StreamInfo streamInfo, MonitoredRoomStatus? previousStatus)
    {
        var roomUrl = streamInfo.Url;
        var isCurrentlyLive = streamInfo.IsLive;
        var wasPreviouslyLive = previousStatus?.StreamInfo?.IsLive ?? false;
        var isCurrentlyRecording = false;

        lock (_lockObject)
        {
            isCurrentlyRecording = _monitoredRooms.TryGetValue(roomUrl, out var status) && status.IsRecording;
        }

        // 如果开始直播且没有在录制，开始录制
        if (isCurrentlyLive && !wasPreviouslyLive && !isCurrentlyRecording)
        {
            await StartRecordingInternalAsync(roomUrl, isManual: false);
        }
        // 如果停止直播且正在录制，停止录制
        else if (!isCurrentlyLive && wasPreviouslyLive && isCurrentlyRecording)
        {
            if (_roomToTaskMap.TryGetValue(roomUrl, out var taskId))
            {
                await StopRecordingInternalAsync(roomUrl, taskId);
            }
        }
    }

    /// <summary>
    /// 开始录制
    /// </summary>
    private async Task<RecordingTask?> StartRecordingInternalAsync(string roomUrl, bool isManual)
    {
        if (_currentSettings == null)
            return null;

        try
        {
            // 获取流信息
            var streamInfo = await _streamDetectionService.DetectStreamAsync(roomUrl);
            if (streamInfo == null || !streamInfo.IsLive)
            {
                if (!isManual)
                    return null; // 自动录制时，如果不在直播就不录制
                throw new InvalidOperationException("Stream is not live");
            }

            // 开始录制
            var recordingTask = await _recordingService.StartRecordingAsync(streamInfo, _currentSettings);

            // 更新状态
            lock (_lockObject)
            {
                if (_monitoredRooms.TryGetValue(roomUrl, out var status))
                {
                    status.IsRecording = true;
                    status.RecordingTaskId = recordingTask.Id;
                    status.RecordingCount++;
                }
                _roomToTaskMap[roomUrl] = recordingTask.Id;
            }

            // 监控录制文件
            if (!string.IsNullOrEmpty(recordingTask.OutputFilePath))
            {
                await _fileMonitoringService.MonitorFileSizeAsync(
                    recordingTask.OutputFilePath,
                    TimeSpan.FromSeconds(30));
            }

            RecordingTaskStatusChanged?.Invoke(this, new RecordingTaskStatusChangedEventArgs
            {
                RecordingTask = recordingTask,
                PreviousStatus = RecordingTaskStatus.Pending
            });

            return recordingTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting recording for {roomUrl}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 停止录制
    /// </summary>
    private async Task StopRecordingInternalAsync(string roomUrl, string taskId)
    {
        try
        {
            var recordingTask = await _recordingService.StopRecordingAsync(taskId);
            if (recordingTask != null)
            {
                RecordingTaskStatusChanged?.Invoke(this, new RecordingTaskStatusChangedEventArgs
                {
                    RecordingTask = recordingTask,
                    PreviousStatus = RecordingTaskStatus.Recording
                });
            }

            // 更新状态
            lock (_lockObject)
            {
                if (_monitoredRooms.TryGetValue(roomUrl, out var status))
                {
                    status.IsRecording = false;
                    status.RecordingTaskId = null;
                    if (recordingTask?.Status == RecordingTaskStatus.Completed)
                    {
                        status.SuccessfulRecordings++;
                    }
                }
                _roomToTaskMap.Remove(roomUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping recording for {roomUrl}: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查录制状态
    /// </summary>
    private async Task CheckRecordingStatusesAsync()
    {
        try
        {
            var recordingTasks = await _recordingService.GetAllRecordingTasksAsync();
            var completedTasks = recordingTasks.Where(t => t.Status == RecordingTaskStatus.Completed).ToList();

            foreach (var task in completedTasks)
            {
                var roomUrl = _roomToTaskMap.FirstOrDefault(kvp => kvp.Value == task.Id).Key;
                if (!string.IsNullOrEmpty(roomUrl))
                {
                    await StopRecordingInternalAsync(roomUrl, task.Id);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking recording statuses: {ex.Message}");
        }
    }

    /// <summary>
    /// 添加房间到监控列表
    /// </summary>
    private async Task AddRoomInternalAsync(string roomUrl)
    {
        var roomStatus = new MonitoredRoomStatus
        {
            RoomUrl = roomUrl,
            IsMonitoring = true,
            LastCheckTime = DateTime.UtcNow
        };

        lock (_lockObject)
        {
            _monitoredRooms[roomUrl] = roomStatus;
        }

        // 立即检测一次状态
        var streamInfo = await _streamDetectionService.DetectStreamAsync(roomUrl);
        if (streamInfo != null)
        {
            OnStreamStatusChanged(streamInfo);
        }
    }

    /// <summary>
    /// 从监控列表移除房间
    /// </summary>
    private async Task RemoveRoomInternalAsync(string roomUrl)
    {
        // 停止录制
        if (_roomToTaskMap.TryGetValue(roomUrl, out var taskId))
        {
            await StopRecordingInternalAsync(roomUrl, taskId);
        }

        lock (_lockObject)
        {
            _monitoredRooms.Remove(roomUrl);
        }
    }

    /// <summary>
    /// 设置服务状态
    /// </summary>
    private void SetStatus(LiveRecordingServiceStatus newStatus, string? errorMessage = null)
    {
        var previousStatus = _status;
        _status = newStatus;

        StatusChanged?.Invoke(this, new LiveRecordingServiceStatusChangedEventArgs
        {
            PreviousStatus = previousStatus,
            CurrentStatus = newStatus,
            ErrorMessage = errorMessage
        });
    }

    /// <summary>
    /// 克隆监控房间状态
    /// </summary>
    private static MonitoredRoomStatus CloneMonitoredRoomStatus(MonitoredRoomStatus original)
    {
        return new MonitoredRoomStatus
        {
            RoomUrl = original.RoomUrl,
            StreamInfo = original.StreamInfo,
            IsMonitoring = original.IsMonitoring,
            IsRecording = original.IsRecording,
            RecordingTaskId = original.RecordingTaskId,
            LastCheckTime = original.LastCheckTime,
            ErrorMessage = original.ErrorMessage,
            RecordingCount = original.RecordingCount,
            SuccessfulRecordings = original.SuccessfulRecordings
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        try
        {
            StopAsync().Wait();
        }
        catch
        {
            // 忽略停止错误
        }

        _semaphore?.Dispose();
        _statusCheckTimer?.Dispose();
        _serviceCts?.Dispose();
    }
}
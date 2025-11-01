using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Cookie;
using DouyinLiveCapture.Services.Platform;

namespace DouyinLiveCapture.Services.Stream;

/// <summary>
/// 直播流检测服务实现
/// </summary>
public class StreamDetectionService : IStreamDetectionService, IDisposable
{
    private readonly IPlatformAdapterFactory _platformAdapterFactory;
    private readonly ICookieService _cookieService;
    private readonly SemaphoreSlim _semaphore;
    private readonly Dictionary<string, StreamInfo> _lastKnownStates;
    private Timer? _monitoringTimer;
    private CancellationTokenSource? _monitoringCts;
    private readonly object _monitoringLock = new object();

    public StreamDetectionService(
        IPlatformAdapterFactory platformAdapterFactory,
        ICookieService cookieService)
    {
        _platformAdapterFactory = platformAdapterFactory ?? throw new ArgumentNullException(nameof(platformAdapterFactory));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _semaphore = new SemaphoreSlim(1, 1);
        _lastKnownStates = new Dictionary<string, StreamInfo>();
    }

    /// <inheritdoc />
    public async Task<StreamInfo?> DetectStreamAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            return null;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var adapter = _platformAdapterFactory.CreateAdapterByUrl(roomUrl);
            if (adapter == null)
                return null;

            // 如果没有提供cookie，尝试从cookie服务获取
            cookie ??= await _cookieService.GetCookieAsync(adapter.PlatformName.ToLowerInvariant(), cancellationToken);

            return await adapter.GetStreamInfoAsync(roomUrl, cookie, cancellationToken);
        }
        catch (Exception ex)
        {
            // 记录错误但不要抛出异常，允许其他检测继续
            Console.WriteLine($"Failed to detect stream for {roomUrl}: {ex.Message}");
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StreamInfo>> DetectStreamsAsync(
        IEnumerable<string> roomUrls,
        Func<string, Task<string?>>? cookieProvider = null,
        CancellationToken cancellationToken = default)
    {
        var urlList = roomUrls?.ToList() ?? new List<string>();
        if (urlList.Count == 0)
            return new List<StreamInfo>().AsReadOnly();

        var tasks = urlList.Select(async url =>
        {
            try
            {
                var cookie = cookieProvider != null ? await cookieProvider(url) : null;
                return await DetectStreamAsync(url, cookie, cancellationToken);
            }
            catch
            {
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(info => info != null).ToList()!.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<bool> IsStreamLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roomUrl))
            return false;

        try
        {
            var streamInfo = await DetectStreamAsync(roomUrl, cookie, cancellationToken);
            return streamInfo?.IsLive ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task StartMonitoringAsync(
        IEnumerable<string> roomUrls,
        TimeSpan checkInterval,
        Action<StreamInfo>? onStreamStatusChanged = null,
        CancellationToken cancellationToken = default)
    {
        lock (_monitoringLock)
        {
            StopMonitoring(); // 停止现有监控

            _monitoringCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var urls = roomUrls?.ToList() ?? new List<string>();

            if (urls.Count == 0)
                return;

            // 立即执行一次检测
            _ = Task.Run(async () =>
            {
                await PerformMonitoringCheck(urls, onStreamStatusChanged, _monitoringCts.Token);
            }, _monitoringCts.Token);

            // 设置定时器进行周期性检测
                _monitoringTimer = new Timer(async _ =>
            {
                await PerformMonitoringCheck(urls, onStreamStatusChanged, _monitoringCts.Token);
            }, null, checkInterval, checkInterval);
        }
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (_monitoringLock)
        {
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
            _monitoringCts?.Cancel();
            _monitoringCts?.Dispose();
            _monitoringCts = null;
        }
    }

    /// <summary>
    /// 执行监控检测
    /// </summary>
    private async Task PerformMonitoringCheck(
        List<string> urls,
        Action<StreamInfo>? onStreamStatusChanged,
        CancellationToken cancellationToken)
    {
        try
        {
            var streamInfos = await DetectStreamsAsync(urls, async url =>
            {
                var adapter = _platformAdapterFactory.CreateAdapterByUrl(url);
                return adapter != null ? await _cookieService.GetCookieAsync(adapter.PlatformName.ToLowerInvariant(), cancellationToken) : null;
            }, cancellationToken);

            foreach (var streamInfo in streamInfos)
            {
                var url = streamInfo.Url;
                var hasChanged = false;

                lock (_lastKnownStates)
                {
                    if (_lastKnownStates.TryGetValue(url, out var lastInfo))
                    {
                        // 检查状态是否发生变化
                        if (lastInfo.IsLive != streamInfo.IsLive ||
                            lastInfo.LiveStatus != streamInfo.LiveStatus ||
                            lastInfo.AnchorName != streamInfo.AnchorName ||
                            lastInfo.Title != streamInfo.Title)
                        {
                            hasChanged = true;
                        }
                    }
                    else
                    {
                        // 首次检测
                        hasChanged = true;
                    }

                    // 更新最后已知状态
                    _lastKnownStates[url] = streamInfo;
                }

                // 如果状态发生变化，触发回调
                if (hasChanged && onStreamStatusChanged != null)
                {
                    onStreamStatusChanged(streamInfo);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 监控被取消，正常退出
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during monitoring check: {ex.Message}");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        StopMonitoring();
        _semaphore?.Dispose();
    }
}
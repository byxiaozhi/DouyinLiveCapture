using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;

namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// 代理服务实现，管理代理池的健康状态
/// </summary>
public class ProxyService : IProxyService
{
    private readonly ConcurrentDictionary<string, ProxyInfo> _proxies;
    private readonly Random _random;
    private readonly Timer _healthCheckTimer;
    private readonly object _lockObject = new object();

    /// <summary>
    /// 初始化ProxyService实例
    /// </summary>
    public ProxyService()
    {
        _proxies = new ConcurrentDictionary<string, ProxyInfo>();
        _random = new Random();

        // 每5分钟进行一次健康检查
        _healthCheckTimer = new Timer(
            async _ => await PerformHealthCheckAsync(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc />
    public async Task<string?> GetAvailableProxyAsync()
    {
        var availableProxies = _proxies.Values
            .Where(p => p.IsHealthy)
            .ToList();

        if (!availableProxies.Any())
        {
            return null;
        }

        // 按成功率权重选择代理
        var selectedProxy = SelectWeightedProxy(availableProxies);
        return selectedProxy?.Address;
    }

    /// <inheritdoc />
    public Task MarkProxyAsFailedAsync(string proxyAddress, string failureReason)
    {
        if (_proxies.TryGetValue(proxyAddress, out var proxyInfo))
        {
            proxyInfo.RecordFailure(failureReason);

            // 如果失败次数过多，标记为不健康
            if (proxyInfo.FailureCount >= 3)
            {
                proxyInfo.IsHealthy = false;
                proxyInfo.LastFailureTime = DateTime.UtcNow;
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkProxyAsSuccessAsync(string proxyAddress)
    {
        if (_proxies.TryGetValue(proxyAddress, out var proxyInfo))
        {
            proxyInfo.RecordSuccess();
            proxyInfo.IsHealthy = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task AddProxiesAsync(IEnumerable<string> proxyAddresses)
    {
        foreach (var address in proxyAddresses)
        {
            if (!string.IsNullOrWhiteSpace(address) && IsValidProxyAddress(address))
            {
                _proxies.TryAdd(address, new ProxyInfo(address));
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ProxyStatistics> GetProxyStatisticsAsync()
    {
        var statistics = new ProxyStatistics
        {
            TotalProxies = _proxies.Count,
            AvailableProxies = _proxies.Values.Count(p => p.IsHealthy),
            FailedProxies = _proxies.Values.Count(p => !p.IsHealthy),
            TotalSuccesses = _proxies.Values.Sum(p => p.SuccessCount),
            TotalFailures = _proxies.Values.Sum(p => p.FailureCount)
        };

        return Task.FromResult(statistics);
    }

    /// <inheritdoc />
    public async Task<bool> CheckProxyHealthAsync(string proxyAddress)
    {
        try
        {
            using var httpClient = HttpClientHelper.CreateHttpClientWithProxy(proxyAddress, 10);

            // 使用一个简单的测试URL
            var response = await httpClient.GetAsync("https://httpbin.org/ip");
            var isSuccess = response.IsSuccessStatusCode;

            if (isSuccess)
            {
                await MarkProxyAsSuccessAsync(proxyAddress);
            }
            else
            {
                await MarkProxyAsFailedAsync(proxyAddress, $"HTTP {response.StatusCode}");
            }

            return isSuccess;
        }
        catch (Exception ex)
        {
            await MarkProxyAsFailedAsync(proxyAddress, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 执行健康检查
    /// </summary>
    private async Task PerformHealthCheckAsync()
    {
        var unhealthyProxies = _proxies.Values
            .Where(p => !p.IsHealthy)
            .Where(p => DateTime.UtcNow - p.LastFailureTime > TimeSpan.FromMinutes(30)) // 30分钟后重试
            .ToList();

        var healthCheckTasks = unhealthyProxies.Select(async proxy =>
        {
            try
            {
                await CheckProxyHealthAsync(proxy.Address);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check failed for proxy {proxy.Address}: {ex.Message}");
            }
        });

        await Task.WhenAll(healthCheckTasks);
    }

    /// <summary>
    /// 根据成功率权重选择代理
    /// </summary>
    /// <param name="availableProxies">可用代理列表</param>
    /// <returns>选中的代理</returns>
    private ProxyInfo? SelectWeightedProxy(List<ProxyInfo> availableProxies)
    {
        if (!availableProxies.Any())
            return null;

        // 计算总权重
        var totalWeight = availableProxies.Sum(p => p.GetWeight());
        if (totalWeight <= 0)
            return availableProxies[_random.Next(availableProxies.Count)];

        // 随机选择
        var randomValue = _random.NextDouble() * totalWeight;
        var currentWeight = 0.0;

        foreach (var proxy in availableProxies)
        {
            currentWeight += proxy.GetWeight();
            if (randomValue <= currentWeight)
            {
                return proxy;
            }
        }

        return availableProxies.LastOrDefault();
    }

    /// <summary>
    /// 验证代理地址格式
    /// </summary>
    /// <param name="address">代理地址</param>
    /// <returns>是否有效</returns>
    private static bool IsValidProxyAddress(string address)
    {
        try
        {
            // 简单的格式验证：http://host:port 或 https://host:port
            var uri = new Uri(address);
            return uri.Scheme is "http" or "https" && uri.Port > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
}

/// <summary>
/// 代理信息
/// </summary>
internal class ProxyInfo
{
    public ProxyInfo(string address)
    {
        Address = address;
        IsHealthy = true;
        CreatedAt = DateTime.UtcNow;
    }

    public string Address { get; }
    public bool IsHealthy { get; set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public DateTime? LastFailureTime { get; internal set; }
    public DateTime CreatedAt { get; }
    public string? LastFailureReason { get; private set; }

    public void RecordSuccess()
    {
        SuccessCount++;
        FailureCount = Math.Max(0, FailureCount - 1); // 减少失败计数
    }

    public void RecordFailure(string reason)
    {
        FailureCount++;
        LastFailureTime = DateTime.UtcNow;
        LastFailureReason = reason;
    }

    /// <summary>
    /// 获取代理权重（基于成功率）
    /// </summary>
    /// <returns>权重值</returns>
    public double GetWeight()
    {
        var totalRequests = SuccessCount + FailureCount;
        if (totalRequests == 0)
            return 1.0; // 新代理给予默认权重

        var successRate = (double)SuccessCount / totalRequests;

        // 成功率越高，权重越大，但至少给予最小权重
        return Math.Max(0.1, successRate);
    }
}
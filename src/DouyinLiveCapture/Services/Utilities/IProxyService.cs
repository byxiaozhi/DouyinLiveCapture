namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// 代理服务接口
/// </summary>
public interface IProxyService : IDisposable
{
    /// <summary>
    /// 获取可用的代理地址
    /// </summary>
    /// <returns>代理地址，如果没有可用代理则返回null</returns>
    Task<string?> GetAvailableProxyAsync();

    /// <summary>
    /// 标记代理为失败
    /// </summary>
    /// <param name="proxyAddress">代理地址</param>
    /// <param name="failureReason">失败原因</param>
    /// <returns></returns>
    Task MarkProxyAsFailedAsync(string proxyAddress, string failureReason);

    /// <summary>
    /// 标记代理为成功
    /// </summary>
    /// <param name="proxyAddress">代理地址</param>
    /// <returns></returns>
    Task MarkProxyAsSuccessAsync(string proxyAddress);

    /// <summary>
    /// 添加代理地址
    /// </summary>
    /// <param name="proxyAddresses">代理地址列表</param>
    /// <returns></returns>
    Task AddProxiesAsync(IEnumerable<string> proxyAddresses);

    /// <summary>
    /// 获取代理统计信息
    /// </summary>
    /// <returns>代理统计信息</returns>
    Task<ProxyStatistics> GetProxyStatisticsAsync();

    /// <summary>
    /// 检查代理健康状态
    /// </summary>
    /// <param name="proxyAddress">代理地址</param>
    /// <returns>是否健康</returns>
    Task<bool> CheckProxyHealthAsync(string proxyAddress);
}

/// <summary>
/// 代理统计信息
/// </summary>
public class ProxyStatistics
{
    /// <summary>
    /// 总代理数量
    /// </summary>
    public int TotalProxies { get; set; }

    /// <summary>
    /// 可用代理数量
    /// </summary>
    public int AvailableProxies { get; set; }

    /// <summary>
    /// 失败代理数量
    /// </summary>
    public int FailedProxies { get; set; }

    /// <summary>
    /// 总成功次数
    /// </summary>
    public long TotalSuccesses { get; set; }

    /// <summary>
    /// 总失败次数
    /// </summary>
    public long TotalFailures { get; set; }

    /// <summary>
    /// 成功率
    /// </summary>
    public double SuccessRate => TotalSuccesses + TotalFailures > 0
        ? (double)TotalSuccesses / (TotalSuccesses + TotalFailures)
        : 0;
}
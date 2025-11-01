namespace DouyinLiveCapture.Services.Cookie;

/// <summary>
/// Cookie 服务接口
/// </summary>
public interface ICookieService : IDisposable
{
    /// <summary>
    /// 获取指定平台的 Cookie
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Cookie 字符串，如果不存在则返回 null</returns>
    Task<string?> GetCookieAsync(string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置指定平台的 Cookie
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="cookie">Cookie 字符串</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task SetCookieAsync(string platform, string cookie, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定平台的 Cookie
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteCookieAsync(string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有平台的 Cookie
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>平台到 Cookie 的字典</returns>
    Task<Dictionary<string, string>> GetAllCookiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定平台是否有 Cookie
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有 Cookie</returns>
    Task<bool> HasCookieAsync(string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取支持的平台列表
    /// </summary>
    /// <returns>平台名称列表</returns>
    IReadOnlyList<string> GetSupportedPlatforms();
}
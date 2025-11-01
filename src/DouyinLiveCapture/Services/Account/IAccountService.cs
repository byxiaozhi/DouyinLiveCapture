using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Account;

/// <summary>
/// 账号服务接口
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// 获取指定平台的账号信息
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="username">用户名（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>账号信息列表</returns>
    Task<IReadOnlyList<AccountInfo>> GetAccountsAsync(string platform, string? username = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加账号信息
    /// </summary>
    /// <param name="account">账号信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task AddAccountAsync(AccountInfo account, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新账号信息
    /// </summary>
    /// <param name="account">账号信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task UpdateAccountAsync(AccountInfo account, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除账号信息
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="username">用户名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAccountAsync(string platform, string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有账号信息
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>所有账号信息</returns>
    Task<IReadOnlyList<AccountInfo>> GetAllAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存认证令牌
    /// </summary>
    /// <param name="token">认证令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task SaveAuthTokenAsync(AuthToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取认证令牌
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>认证令牌，如果不存在则返回 null</returns>
    Task<AuthToken?> GetAuthTokenAsync(string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除认证令牌
    /// </summary>
    /// <param name="platform">平台名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteAuthTokenAsync(string platform, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取支持账号管理的平台列表
    /// </summary>
    /// <returns>平台名称列表</returns>
    IReadOnlyList<string> GetSupportedAccountPlatforms();
}
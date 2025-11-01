namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// HTTP客户端服务接口
/// </summary>
public interface IHttpClientService : IDisposable
{
    /// <summary>
    /// 获取JSON内容，带重试机制
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="url">URL</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化的对象</returns>
    Task<T?> GetJsonAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取字符串内容，带重试机制
    /// </summary>
    /// <param name="url">URL</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字符串内容</returns>
    Task<string> GetStringAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送GET请求并跟随重定向
    /// </summary>
    /// <param name="url">URL</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP响应消息</returns>
    Task<HttpResponseMessage> GetWithRedirectAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
}
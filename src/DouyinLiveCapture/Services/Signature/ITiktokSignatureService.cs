namespace DouyinLiveCapture.Services.Signature;

/// <summary>
/// TikTok X-Bogus签名服务接口
/// </summary>
public interface ITiktokSignatureService
{
    /// <summary>
    /// 生成X-Bogus签名
    /// </summary>
    /// <param name="url">请求URL</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="cookie">Cookie</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>X-Bogus签名值</returns>
    Task<string> GenerateXBogusAsync(string url, string userAgent, string? cookie = null, CancellationToken cancellationToken = default);
}
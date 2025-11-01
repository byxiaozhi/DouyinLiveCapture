namespace DouyinLiveCapture.Services.Signature;

/// <summary>
/// 抖音AB签名服务接口
/// </summary>
public interface IDouyinAbSignatureService
{
    /// <summary>
    /// 生成AB签名
    /// </summary>
    /// <param name="urlSearchParams">URL查询参数</param>
    /// <param name="userAgent">用户代理字符串</param>
    /// <returns>AB签名</returns>
    string GenerateSignature(string urlSearchParams, string userAgent);
}
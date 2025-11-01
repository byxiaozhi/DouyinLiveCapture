namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 平台适配器工厂接口
/// </summary>
public interface IPlatformAdapterFactory
{
    /// <summary>
    /// 创建平台适配器
    /// </summary>
    /// <param name="platformType">平台类型</param>
    /// <returns>平台适配器实例</returns>
    IPlatformAdapter? CreateAdapter(PlatformType platformType);

    /// <summary>
    /// 根据URL创建平台适配器
    /// </summary>
    /// <param name="url">直播URL</param>
    /// <returns>平台适配器实例</returns>
    IPlatformAdapter? CreateAdapterByUrl(string url);

    /// <summary>
    /// 获取所有支持的平台类型
    /// </summary>
    /// <returns>支持的平台类型列表</returns>
    IReadOnlyList<PlatformType> GetSupportedPlatforms();

    /// <summary>
    /// 检查是否支持指定平台
    /// </summary>
    /// <param name="platformType">平台类型</param>
    /// <returns>是否支持</returns>
    bool IsSupported(PlatformType platformType);
}
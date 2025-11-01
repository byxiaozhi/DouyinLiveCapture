using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Configuration;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 加载录制设置
    /// </summary>
    /// <param name="configPath">配置文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>录制设置</returns>
    Task<RecordingSettings> LoadRecordingSettingsAsync(string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存录制设置
    /// </summary>
    /// <param name="settings">录制设置</param>
    /// <param name="configPath">配置文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task SaveRecordingSettingsAsync(RecordingSettings settings, string configPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载监控URL列表
    /// </summary>
    /// <param name="urlConfigPath">URL配置文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>URL列表</returns>
    Task<List<string>> LoadMonitorUrlsAsync(string urlConfigPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存监控URL列表
    /// </summary>
    /// <param name="urls">URL列表</param>
    /// <param name="urlConfigPath">URL配置文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task SaveMonitorUrlsAsync(List<string> urls, string urlConfigPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认配置路径
    /// </summary>
    /// <returns>默认配置文件路径</returns>
    string GetDefaultConfigPath();

    /// <summary>
    /// 获取默认URL配置路径
    /// </summary>
    /// <returns>默认URL配置文件路径</returns>
    string GetDefaultUrlConfigPath();
}
using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Services.Platform;

/// <summary>
/// 平台适配器接口
/// </summary>
public interface IPlatformAdapter
{
    /// <summary>
    /// 平台类型
    /// </summary>
    PlatformType PlatformType { get; }

    /// <summary>
    /// 平台名称
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// 获取直播流信息
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cookie">Cookie信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>直播流信息</returns>
    Task<StreamInfo?> GetStreamInfoAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查直播间是否在线
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <param name="cookie">Cookie信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否在线</returns>
    Task<bool> IsLiveAsync(string roomUrl, string? cookie = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析房间ID
    /// </summary>
    /// <param name="roomUrl">直播间URL</param>
    /// <returns>房间ID</returns>
    string? ParseRoomId(string roomUrl);

    /// <summary>
    /// 生成直播间URL
    /// </summary>
    /// <param name="roomId">房间ID</param>
    /// <returns>直播间URL</returns>
    string GenerateRoomUrl(string roomId);

    /// <summary>
    /// 检查URL是否属于该平台
    /// </summary>
    /// <param name="url">URL</param>
    /// <returns>是否属于该平台</returns>
    bool IsSupportedUrl(string url);
}
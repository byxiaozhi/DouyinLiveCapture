namespace DouyinLiveCapture.Services.Room;

/// <summary>
/// 房间解析服务接口
/// </summary>
public interface IRoomParsingService
{
    /// <summary>
    /// 获取房间ID和sec_user_id
    /// </summary>
    /// <param name="url">输入URL</param>
    /// <param name="cookie">Cookie（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>房间ID和sec_user_id元组</returns>
    Task<(string roomId, string secUserId)?> GetSecUserIdAsync(string url, string? cookie = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取抖音号（unique_id）
    /// </summary>
    /// <param name="url">输入URL</param>
    /// <param name="cookie">Cookie（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>抖音号</returns>
    Task<string?> GetUniqueIdAsync(string url, string? cookie = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取直播间webID
    /// </summary>
    /// <param name="roomId">房间ID</param>
    /// <param name="secUserId">sec_user_id</param>
    /// <param name="cookie">Cookie（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>直播间webID</returns>
    Task<string?> GetLiveRoomIdAsync(string roomId, string secUserId, string? cookie = null, CancellationToken cancellationToken = default);
}
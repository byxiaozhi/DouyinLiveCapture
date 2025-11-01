namespace DouyinLiveCapture.Services.Exceptions;

/// <summary>
/// 抖音平台相关异常的基类
/// </summary>
public class DouyinException : Exception
{
    public DouyinException() : base() { }

    public DouyinException(string message) : base(message) { }

    public DouyinException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 直播间不存在异常
/// </summary>
public class LiveRoomNotFoundException : DouyinException
{
    public string? RoomId { get; }

    public LiveRoomNotFoundException() : base("Live room not found") { }

    public LiveRoomNotFoundException(string roomId) : base($"Live room '{roomId}' not found")
    {
        RoomId = roomId;
    }

    public LiveRoomNotFoundException(string roomId, Exception innerException)
        : base($"Live room '{roomId}' not found", innerException)
    {
        RoomId = roomId;
    }
}

/// <summary>
/// 直播间已结束异常
/// </summary>
public class LiveStreamEndedException : DouyinException
{
    public string? RoomId { get; }

    public LiveStreamEndedException() : base("Live stream has ended") { }

    public LiveStreamEndedException(string roomId) : base($"Live stream in room '{roomId}' has ended")
    {
        RoomId = roomId;
    }

    public LiveStreamEndedException(string roomId, Exception innerException)
        : base($"Live stream in room '{roomId}' has ended", innerException)
    {
        RoomId = roomId;
    }
}

/// <summary>
/// 签名生成失败异常
/// </summary>
public class SignatureGenerationException : DouyinException
{
    public string? DataType { get; }

    public SignatureGenerationException() : base("Failed to generate signature") { }

    public SignatureGenerationException(string dataType) : base($"Failed to generate signature for {dataType}")
    {
        DataType = dataType;
    }

    public SignatureGenerationException(string dataType, Exception innerException)
        : base($"Failed to generate signature for {dataType}", innerException)
    {
        DataType = dataType;
    }
}

/// <summary>
/// 流数据获取失败异常
/// </summary>
public class StreamDataException : DouyinException
{
    public string? StreamUrl { get; }

    public StreamDataException() : base("Failed to get stream data") { }

    public StreamDataException(string streamUrl) : base($"Failed to get stream data from {streamUrl}")
    {
        StreamUrl = streamUrl;
    }

    public StreamDataException(string streamUrl, Exception innerException)
        : base($"Failed to get stream data from {streamUrl}", innerException)
    {
        StreamUrl = streamUrl;
    }
}

/// <summary>
/// API请求被限制异常
/// </summary>
public class ApiRateLimitException : DouyinException
{
    public int? RetryAfterSeconds { get; }

    public ApiRateLimitException() : base("API request rate limit exceeded") { }

    public ApiRateLimitException(int retryAfterSeconds)
        : base($"API rate limit exceeded, retry after {retryAfterSeconds} seconds")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public ApiRateLimitException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 网络连接超时异常
/// </summary>
public class NetworkTimeoutException : DouyinException
{
    public TimeSpan? Timeout { get; }

    public NetworkTimeoutException() : base("Network connection timeout") { }

    public NetworkTimeoutException(TimeSpan timeout) : base($"Network connection timeout after {timeout.TotalSeconds} seconds")
    {
        Timeout = timeout;
    }

    public NetworkTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}
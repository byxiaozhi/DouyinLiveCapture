namespace DouyinLiveCapture.Services.Room;

/// <summary>
/// 不支持的URL异常
/// </summary>
public class UnsupportedUrlException : Exception
{
    /// <summary>
    /// 初始化UnsupportedUrlException实例
    /// </summary>
    public UnsupportedUrlException() : base()
    {
    }

    /// <summary>
    /// 使用错误消息初始化UnsupportedUrlException实例
    /// </summary>
    /// <param name="message">错误消息</param>
    public UnsupportedUrlException(string message) : base(message)
    {
    }

    /// <summary>
    /// 使用错误消息和内部异常初始化UnsupportedUrlException实例
    /// </summary>
    /// <param name="message">错误消息</param>
    /// <param name="innerException">内部异常</param>
    public UnsupportedUrlException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
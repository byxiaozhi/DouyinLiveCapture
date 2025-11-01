namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// 错误处理服务接口
/// </summary>
public interface IErrorHandlingService
{
    /// <summary>
    /// 安全执行异步操作，处理异常并返回结果
    /// </summary>
    Task<T?> SafeExecuteAsync<T>(
        Func<Task<T>> operation,
        T? defaultValue = default,
        string? operationName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 安全执行异步操作，处理异常但不返回结果
    /// </summary>
    Task SafeExecuteAsync(
        Func<Task> operation,
        string? operationName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重试执行异步操作
    /// </summary>
    Task<T> RetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重试执行异步操作（无返回值）
    /// </summary>
    Task RetryAsync(
        Func<Task> operation,
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录异常信息
    /// </summary>
    void LogException(Exception exception, string? context = null, params object[] parameters);

    /// <summary>
    /// 判断异常是否可重试
    /// </summary>
    bool IsRetryableException(Exception exception);

    /// <summary>
    /// 计算重试延迟时间（指数退避）
    /// </summary>
    TimeSpan CalculateRetryDelay(int attempt, TimeSpan baseDelay, double jitterFactor = 0.1);
}
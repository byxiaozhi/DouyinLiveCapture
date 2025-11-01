using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using DouyinLiveCapture.Services.Exceptions;

namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// 错误处理服务实现
/// </summary>
public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ILogger<ErrorHandlingService> _logger;
    private readonly Random _random = new();

    public ErrorHandlingService(ILogger<ErrorHandlingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T?> SafeExecuteAsync<T>(
        Func<Task<T>> operation,
        T? defaultValue = default,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(ex, operationName);
            return defaultValue;
        }
    }

    public async Task SafeExecuteAsync(
        Func<Task> operation,
        string? operationName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogException(ex, operationName);
        }
    }

    public async Task<T> RetryAsync<T>(
        Func<Task<T>> operation,
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        baseDelay ??= TimeSpan.FromSeconds(1);
        shouldRetry ??= IsRetryableException;

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
                LogException(ex, $"Retry attempt {attempt}/{maxAttempts}");

                if (attempt == maxAttempts || !shouldRetry(ex))
                {
                    break;
                }

                var delay = CalculateRetryDelay(attempt, baseDelay.Value);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after all retry attempts");
    }

    public async Task RetryAsync(
        Func<Task> operation,
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        baseDelay ??= TimeSpan.FromSeconds(1);
        shouldRetry ??= IsRetryableException;

        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await operation().ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                LogException(ex, $"Retry attempt {attempt}/{maxAttempts}");

                if (attempt == maxAttempts || !shouldRetry(ex))
                {
                    break;
                }

                var delay = CalculateRetryDelay(attempt, baseDelay.Value);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after all retry attempts");
    }

    public void LogException(Exception exception, string? context = null, params object[] parameters)
    {
        var message = string.IsNullOrEmpty(context)
            ? $"Exception occurred: {exception.Message}"
            : $"Exception in {context}: {exception.Message}";

        if (parameters.Length > 0)
        {
            message += $" | Parameters: {string.Join(", ", parameters.Select(p => p?.ToString() ?? "null"))}";
        }

        _logger.LogError(exception, message);
    }

    public bool IsRetryableException(Exception exception)
    {
        return exception switch
        {
            HttpRequestException httpEx => IsRetryableHttpRequestException(httpEx),
            TaskCanceledException => true, // 超时异常可重试
            TimeoutException => true, // 超时异常可重试
            IOException => true, // IO异常可重试
            WebException webEx => IsRetryableWebException(webEx),
            NetworkTimeoutException => true, // 自定义网络超时可重试
            ApiRateLimitException => true, // API限流可重试
            DouyinException douyinEx => IsRetryableDouyinException(douyinEx),
            _ => false
        };
    }

    public TimeSpan CalculateRetryDelay(int attempt, TimeSpan baseDelay, double jitterFactor = 0.1)
    {
        // 指数退避算法
        var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

        // 添加随机抖动以避免雷群效应
        if (jitterFactor > 0)
        {
            var jitter = delay.TotalMilliseconds * jitterFactor * (_random.NextDouble() - 0.5);
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitter);
        }

        // 确保延迟时间不为负
        return delay < TimeSpan.Zero ? TimeSpan.FromMilliseconds(100) : delay;
    }

    private static bool IsRetryableHttpRequestException(HttpRequestException exception)
    {
        // 检查HTTP状态码
        if (exception.StatusCode.HasValue)
        {
            return exception.StatusCode.Value switch
            {
                HttpStatusCode.InternalServerError => true, // 500
                HttpStatusCode.BadGateway => true, // 502
                HttpStatusCode.ServiceUnavailable => true, // 503
                HttpStatusCode.GatewayTimeout => true, // 504
                HttpStatusCode.RequestTimeout => true, // 408
                HttpStatusCode.TooManyRequests => true, // 429
                _ => false
            };
        }

        // 对于没有状态码的HTTP异常，默认可重试
        return true;
    }

    private static bool IsRetryableWebException(WebException exception)
    {
        return exception.Status switch
        {
            WebExceptionStatus.Timeout => true,
            WebExceptionStatus.ConnectFailure => true,
            WebExceptionStatus.NameResolutionFailure => true,
            WebExceptionStatus.ProxyNameResolutionFailure => true,
            WebExceptionStatus.ServerProtocolViolation => true,
            WebExceptionStatus.ConnectionClosed => true,
            WebExceptionStatus.ReceiveFailure => true,
            WebExceptionStatus.SendFailure => true,
            WebExceptionStatus.PipelineFailure => true,
            WebExceptionStatus.RequestCanceled => true,
            _ => false
        };
    }

    private static bool IsRetryableDouyinException(DouyinException exception)
    {
        return exception switch
        {
            ApiRateLimitException => true,
            NetworkTimeoutException => true,
            StreamDataException => true, // 流数据获取失败可能重试
            SignatureGenerationException => false, // 签名生成失败不应重试
            LiveRoomNotFoundException => false, // 房间不存在不应重试
            LiveStreamEndedException => false, // 直播结束不应重试
            _ => false
        };
    }
}
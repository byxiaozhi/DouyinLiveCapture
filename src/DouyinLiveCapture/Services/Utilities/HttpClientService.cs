using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// HTTP客户端服务实现，带重试机制和指数退避
/// </summary>
public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientRetryOptions _retryOptions;

    /// <summary>
    /// 初始化HttpClientService实例
    /// </summary>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <param name="retryOptions">重试选项</param>
    /// <param name="proxyAddress">代理地址（可选）</param>
    public HttpClientService(int timeoutSeconds = 30, HttpClientRetryOptions? retryOptions = null, string? proxyAddress = null)
    {
        _retryOptions = retryOptions ?? new HttpClientRetryOptions();

        if (!string.IsNullOrEmpty(proxyAddress))
        {
            _httpClient = HttpClientHelper.CreateHttpClientWithProxy(proxyAddress, timeoutSeconds);
        }
        else
        {
            _httpClient = HttpClientHelper.CreateDefaultHttpClient(timeoutSeconds);
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetJsonAsync<T>(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            async () => await _httpClient.GetJsonAsync<T>(url, headers, cancellationToken),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<string> GetStringAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            async () => await _httpClient.GetStringAsync(url, headers, cancellationToken),
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetWithRedirectAsync(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            async () =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);

                // 添加自定义请求头
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // 设置允许自动重定向
                var response = await _httpClient.SendAsync(request, cancellationToken);
                return response;
            },
            cancellationToken
        );
    }

    /// <summary>
    /// 执行带重试机制的异步操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= _retryOptions.MaxRetryAttempts; attempt++)
        {
            try
            {
                var result = await operation();

                // 如果成功，返回结果
                return result;
            }
            catch (Exception ex) when (ShouldRetry(ex) && attempt < _retryOptions.MaxRetryAttempts)
            {
                lastException = ex;

                // 计算延迟时间（指数退避）
                var delay = CalculateDelay(attempt);

                Console.WriteLine($"Request failed (attempt {attempt}/{_retryOptions.MaxRetryAttempts}): {ex.Message}. Retrying in {delay.TotalMilliseconds}ms...");

                await Task.Delay(delay, cancellationToken);
            }
        }

        // 所有重试都失败了，抛出最后一个异常
        throw lastException ?? new InvalidOperationException("Operation failed after all retry attempts.");
    }

    /// <summary>
    /// 判断异常是否应该重试
    /// </summary>
    /// <param name="exception">异常</param>
    /// <returns>是否应该重试</returns>
    private static bool ShouldRetry(Exception exception)
    {
        // 网络相关异常
        if (exception is HttpRequestException ||
            exception is TaskCanceledException ||
            exception is TimeoutException ||
            exception is IOException)
        {
            return true;
        }

        // HTTP状态码异常
        if (exception is HttpRequestException httpEx)
        {
            // 某些状态码不应该重试（4xx客户端错误）
            if (httpEx.StatusCode.HasValue)
            {
                var statusCode = httpEx.StatusCode.Value;
                return statusCode switch
                {
                    HttpStatusCode.InternalServerError => true,      // 500
                    HttpStatusCode.BadGateway => true,              // 502
                    HttpStatusCode.ServiceUnavailable => true,     // 503
                    HttpStatusCode.GatewayTimeout => true,          // 504
                    HttpStatusCode.RequestTimeout => true,          // 408
                    HttpStatusCode.TooManyRequests => true,         // 429
                    _ => false
                };
            }
        }

        return false;
    }

    /// <summary>
    /// 计算重试延迟时间（指数退避）
    /// </summary>
    /// <param name="attempt">当前尝试次数</param>
    /// <returns>延迟时间</returns>
    private TimeSpan CalculateDelay(int attempt)
    {
        // 指数退避：delay = baseDelay * (2 ^ (attempt - 1)) + jitter
        var exponentialDelay = _retryOptions.BaseDelayMilliseconds * Math.Pow(2, attempt - 1);

        // 添加随机抖动以避免雷群效应
        var jitter = _retryOptions.JitterFactor * _retryOptions.BaseDelayMilliseconds * new Random().NextDouble();

        var totalDelay = exponentialDelay + jitter;

        // 限制最大延迟时间
        var maxDelay = TimeSpan.FromMilliseconds(_retryOptions.MaxDelayMilliseconds);

        return TimeSpan.FromMilliseconds(Math.Min(totalDelay, maxDelay.TotalMilliseconds));
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// HTTP客户端重试选项
/// </summary>
public class HttpClientRetryOptions
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 基础延迟时间（毫秒）
    /// </summary>
    public int BaseDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// 最大延迟时间（毫秒）
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 30000;

    /// <summary>
    /// 抖动因子（0-1之间）
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;
}
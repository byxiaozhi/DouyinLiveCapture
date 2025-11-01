using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// HTTP 客户端辅助类
/// </summary>
public static class HttpClientHelper
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        TypeInfoResolver = AppJsonSerializerContext.Default,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 创建默认的 HttpClient
    /// </summary>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>HttpClient 实例</returns>
    public static HttpClient CreateDefaultHttpClient(int timeoutSeconds = 30)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };

        // 设置默认请求头
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        return client;
    }

    /// <summary>
    /// 创建带代理的 HttpClient
    /// </summary>
    /// <param name="proxyAddress">代理地址</param>
    /// <param name="timeoutSeconds">超时时间（秒）</param>
    /// <returns>HttpClient 实例</returns>
    public static HttpClient CreateHttpClientWithProxy(string? proxyAddress, int timeoutSeconds = 30)
    {
        var client = CreateDefaultHttpClient(timeoutSeconds);

        if (!string.IsNullOrEmpty(proxyAddress))
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyAddress)
                };
                client = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                };

                // 设置默认请求头
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid proxy address: {proxyAddress}", ex);
            }
        }

        return client;
    }

    /// <summary>
    /// 获取 JSON 内容（使用源生成器）
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="httpClient">HttpClient 实例</param>
    /// <param name="url">URL</param>
    /// <param name="typeInfo">JSON 类型信息</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化的对象</returns>
    public static async Task<T?> GetJsonAsync<T>(this HttpClient httpClient, string url, JsonTypeInfo<T> typeInfo,
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 添加自定义请求头
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize(content, typeInfo);
    }

    /// <summary>
    /// 获取 JSON 内容（兼容旧版本）
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="httpClient">HttpClient 实例</param>
    /// <param name="url">URL</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>反序列化的对象</returns>
    public static async Task<T?> GetJsonAsync<T>(this HttpClient httpClient, string url,
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 添加自定义请求头
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, _jsonOptions);
    }

    /// <summary>
    /// 获取字符串内容
    /// </summary>
    /// <param name="httpClient">HttpClient 实例</param>
    /// <param name="url">URL</param>
    /// <param name="headers">请求头</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字符串内容</returns>
    public static async Task<string> GetStringAsync(this HttpClient httpClient, string url,
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 添加自定义请求头
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    /// <summary>
    /// 检查 URL 是否可访问
    /// </summary>
    /// <param name="httpClient">HttpClient 实例</param>
    /// <param name="url">URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可访问</returns>
    public static async Task<bool> IsUrlAccessibleAsync(this HttpClient httpClient, string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
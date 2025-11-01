using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// JSON解析服务实现
/// </summary>
public class JsonParsingService : IJsonParsingService
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 初始化JsonParsingService实例
    /// </summary>
    public JsonParsingService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>?> ParseToDictionaryAsync(string jsonString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return null;

            return await Task.Run(() =>
            {
                using var document = JsonDocument.Parse(jsonString);
                return ConvertJsonElementToDictionary(document.RootElement);
            });
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing JSON to dictionary: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<object?> ExtractPropertyAsync(string jsonString, string propertyPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString) || string.IsNullOrWhiteSpace(propertyPath))
                return null;

            return await Task.Run(() =>
            {
                var node = JsonNode.Parse(jsonString);
                if (node == null)
                    return null;

                var pathSegments = propertyPath.Split('.');
                var current = node;

                foreach (var segment in pathSegments)
                {
                    if (current is JsonObject obj && obj.TryGetPropertyValue(segment, out var value))
                    {
                        current = value;
                    }
                    else
                    {
                        return null;
                    }
                }

                return current?.AsValue();
            });
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error extracting property '{propertyPath}' from JSON: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string?> ExtractStringPropertyAsync(string jsonString, string propertyPath)
    {
        var property = await ExtractPropertyAsync(jsonString, propertyPath);
        return property?.ToString();
    }

    /// <inheritdoc />
    public async Task<int> ExtractIntPropertyAsync(string jsonString, string propertyPath, int defaultValue = 0)
    {
        var property = await ExtractPropertyAsync(jsonString, propertyPath);
        if (property == null)
            return defaultValue;

        if (int.TryParse(property.ToString(), out var result))
            return result;

        return defaultValue;
    }

    /// <inheritdoc />
    public async Task<bool> ExtractBoolPropertyAsync(string jsonString, string propertyPath, bool defaultValue = false)
    {
        var property = await ExtractPropertyAsync(jsonString, propertyPath);
        if (property == null)
            return defaultValue;

        if (bool.TryParse(property.ToString(), out var result))
            return result;

        return defaultValue;
    }

    /// <inheritdoc />
    public bool IsValidJson(string jsonString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return false;

            JsonDocument.Parse(jsonString);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string?> PrettifyJsonAsync(string jsonString)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return null;

            return await Task.Run(() =>
            {
                var jsonNode = JsonNode.Parse(jsonString);
                return jsonNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            });
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error prettifying JSON: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 将JsonElement转换为字典
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <returns>字典</returns>
    private static Dictionary<string, object> ConvertJsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object>();

        if (element.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElementToObject(property.Value);
        }

        return result;
    }

    /// <summary>
    /// 将JsonElement转换为对象
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <returns>对象</returns>
    private static object ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True or JsonValueKind.False => element.GetBoolean(),
            JsonValueKind.Null => null!,
            JsonValueKind.Array => ConvertJsonElementToArray(element),
            JsonValueKind.Object => ConvertJsonElementToDictionary(element),
            _ => element.ToString()
        };
    }

    /// <summary>
    /// 将JsonElement转换为数组
    /// </summary>
    /// <param name="element">JSON元素</param>
    /// <returns>数组</returns>
    private static object[] ConvertJsonElementToArray(JsonElement element)
    {
        var result = new List<object>();

        foreach (var item in element.EnumerateArray())
        {
            result.Add(ConvertJsonElementToObject(item));
        }

        return result.ToArray();
    }
}

/// <summary>
/// JSON解析异常
/// </summary>
public class JsonParsingException : Exception
{
    public JsonParsingException() : base() { }
    public JsonParsingException(string message) : base(message) { }
    public JsonParsingException(string message, Exception innerException) : base(message, innerException) { }
}
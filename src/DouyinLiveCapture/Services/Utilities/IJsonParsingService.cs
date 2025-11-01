namespace DouyinLiveCapture.Services.Utilities;

/// <summary>
/// JSON解析服务接口
/// </summary>
public interface IJsonParsingService
{
    /// <summary>
    /// 解析JSON字符串为字典
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <returns>解析后的字典</returns>
    Task<Dictionary<string, object>?> ParseToDictionaryAsync(string jsonString);

    /// <summary>
    /// 从JSON中提取指定的属性值
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <param name="propertyPath">属性路径（支持嵌套，如 "data.room.owner.nickname"）</param>
    /// <returns>属性值</returns>
    Task<object?> ExtractPropertyAsync(string jsonString, string propertyPath);

    /// <summary>
    /// 从JSON中提取字符串属性
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <param name="propertyPath">属性路径</param>
    /// <returns>字符串值</returns>
    Task<string?> ExtractStringPropertyAsync(string jsonString, string propertyPath);

    /// <summary>
    /// 从JSON中提取数值属性
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <param name="propertyPath">属性路径</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>数值</returns>
    Task<int> ExtractIntPropertyAsync(string jsonString, string propertyPath, int defaultValue = 0);

    /// <summary>
    /// 从JSON中提取布尔属性
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <param name="propertyPath">属性路径</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>布尔值</returns>
    Task<bool> ExtractBoolPropertyAsync(string jsonString, string propertyPath, bool defaultValue = false);

    /// <summary>
    /// 验证JSON格式是否有效
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <returns>是否有效</returns>
    bool IsValidJson(string jsonString);

    /// <summary>
    /// 美化JSON字符串
    /// </summary>
    /// <param name="jsonString">JSON字符串</param>
    /// <returns>美化后的JSON字符串</returns>
    Task<string?> PrettifyJsonAsync(string jsonString);
}
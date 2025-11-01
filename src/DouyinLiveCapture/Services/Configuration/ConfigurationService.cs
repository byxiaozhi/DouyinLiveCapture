using System.Text.Json;
using System.Text.Json.Serialization;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Utilities;
using DouyinLiveCapture.Serialization;

namespace DouyinLiveCapture.Services.Configuration;

/// <summary>
/// 配置服务实现
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigurationService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = AppJsonSerializerContext.Default,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }

    /// <inheritdoc />
    public async Task<RecordingSettings> LoadRecordingSettingsAsync(string configPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
        {
            // 如果配置文件不存在，返回默认设置
            var defaultSettings = new RecordingSettings();
            await SaveRecordingSettingsAsync(defaultSettings, configPath, cancellationToken);
            return defaultSettings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath, cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new RecordingSettings();
            }

            // 尝试解析为 JSON 格式
            if (json.Trim().StartsWith("{"))
            {
                var settings = JsonSerializer.Deserialize(json, AppJsonSerializerContext.Default.RecordingSettings);
                return settings ?? new RecordingSettings();
            }

            // 尝试解析为 INI 格式
            return ParseIniFormat(json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration from {configPath}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveRecordingSettingsAsync(RecordingSettings settings, string configPath, CancellationToken cancellationToken = default)
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, AppJsonSerializerContext.Default.RecordingSettings);
            await File.WriteAllTextAsync(configPath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save configuration to {configPath}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> LoadMonitorUrlsAsync(string urlConfigPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(urlConfigPath))
        {
            return new List<string>();
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(urlConfigPath, cancellationToken);
            var urls = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // 跳过空行和注释行（以 # 或 ; 开头）
                if (string.IsNullOrEmpty(trimmedLine) ||
                    trimmedLine.StartsWith("#") ||
                    trimmedLine.StartsWith(";"))
                {
                    continue;
                }

                // 检查是否被注释（以 ,# 开头）
                if (trimmedLine.Contains(",#"))
                {
                    continue;
                }

                urls.Add(trimmedLine);
            }

            return urls;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load monitor URLs from {urlConfigPath}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public async Task SaveMonitorUrlsAsync(List<string> urls, string urlConfigPath, CancellationToken cancellationToken = default)
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(urlConfigPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllLinesAsync(urlConfigPath, urls, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save monitor URLs to {urlConfigPath}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public string GetDefaultConfigPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "DouyinLiveCapture");
        return Path.Combine(appFolder, "config.json");
    }

    /// <inheritdoc />
    public string GetDefaultUrlConfigPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "DouyinLiveCapture");
        return Path.Combine(appFolder, "urls.txt");
    }

    /// <summary>
    /// 解析 INI 格式的配置文件（兼容性支持）
    /// </summary>
    /// <param name="iniContent">INI 内容</param>
    /// <returns>录制设置</returns>
    private RecordingSettings ParseIniFormat(string iniContent)
    {
        var settings = new RecordingSettings();
        var lines = iniContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // 跳过注释和空行
            if (string.IsNullOrEmpty(trimmedLine) ||
                trimmedLine.StartsWith("#") ||
                trimmedLine.StartsWith(";") ||
                trimmedLine.StartsWith("["))
            {
                continue;
            }

            // 解析键值对
            var equalIndex = trimmedLine.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = trimmedLine.Substring(0, equalIndex).Trim();
                var value = trimmedLine.Substring(equalIndex + 1).Trim();

                // 移除括号中的说明
                var parenthesisIndex = key.IndexOf('(');
                if (parenthesisIndex > 0)
                {
                    key = key.Substring(0, parenthesisIndex).Trim();
                }

                MapIniValueToProperty(settings, key, value);
            }
        }

        return settings;
    }

    /// <summary>
    /// 将 INI 键值对映射到属性
    /// </summary>
    /// <param name="settings">设置对象</param>
    /// <param name="key">键</param>
    /// <param name="value">值</param>
    private static void MapIniValueToProperty(RecordingSettings settings, string key, string value)
    {
        key = key switch
        {
            "language" => "Language",
            "是否跳过代理检测" => "SkipProxyDetection",
            "直播保存路径" => "SavePath",
            "保存文件夹是否以作者区分" => "FolderByAuthor",
            "保存文件夹是否以时间区分" => "FolderByTime",
            "保存文件夹是否以标题区分" => "FolderByTitle",
            "保存文件名是否包含标题" => "FilenameIncludeTitle",
            "是否去除名称中的表情符号" => "RemoveEmoji",
            "视频保存格式" => "VideoFormat",
            "原画" => "VideoQuality",
            "是否使用代理ip" => "UseProxy",
            "代理地址" => "ProxyAddress",
            "同一时间访问网络的线程数" => "MaxThreads",
            "循环时间" => "LoopInterval",
            "排队读取网址时间" => "QueueInterval",
            "是否显示循环秒数" => "ShowLoopSeconds",
            "是否显示直播源地址" => "ShowStreamUrl",
            "分段录制是否开启" => "EnableSegmentRecording",
            "是否强制启用https录制" => "ForceHttps",
            "录制空间剩余阈值" => "DiskSpaceThreshold",
            "视频分段时间" => "SegmentDuration",
            "录制完成后自动转为mp4格式" => "AutoConvertMp4",
            "mp4格式重新编码为h264" => "Mp4ReencodeH264",
            "追加格式后删除原文件" => "DeleteOriginalAfterConvert",
            "生成时间字幕文件" => "GenerateSubtitle",
            "是否录制完成后执行自定义脚本" => "ExecuteCustomScript",
            "自定义脚本执行命令" => "CustomScriptCommand",
            "使用代理录制的平台" => "ProxyPlatforms",
            "额外使用代理录制的平台" => "ExtraProxyPlatforms",
            _ => key
        };

        // 设置属性值
        var property = typeof(RecordingSettings).GetProperty(key);
        if (property != null && property.CanWrite)
        {
            try
            {
                if (property.PropertyType == typeof(bool))
                {
                    var boolValue = value.Equals("是", StringComparison.OrdinalIgnoreCase) ||
                                  value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                  value == "1";
                    property.SetValue(settings, boolValue);
                }
                else if (property.PropertyType == typeof(int))
                {
                    if (int.TryParse(value, out var intValue))
                    {
                        property.SetValue(settings, intValue);
                    }
                }
                else if (property.PropertyType == typeof(double))
                {
                    if (double.TryParse(value, out var doubleValue))
                    {
                        property.SetValue(settings, doubleValue);
                    }
                }
                else if (property.PropertyType == typeof(string))
                {
                    property.SetValue(settings, string.IsNullOrEmpty(value) ? null : value);
                }
            }
            catch
            {
                // 忽略属性设置错误
            }
        }
    }
}
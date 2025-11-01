using System.Text.Json;
using System.Text.Json.Serialization;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Platform;
using DouyinLiveCapture.Services.Recording;

namespace DouyinLiveCapture.Serialization;

/// <summary>
/// JSON 序列化上下文，用于源生成器
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(RecordingSettings))]
[JsonSerializable(typeof(AccountInfo))]
[JsonSerializable(typeof(AuthToken))]
[JsonSerializable(typeof(StreamInfo))]
[JsonSerializable(typeof(StreamUrlInfo))]
[JsonSerializable(typeof(RecordingTask))]
[JsonSerializable(typeof(RecordingTaskStatus))]
[JsonSerializable(typeof(PlatformType))]
[JsonSerializable(typeof(LiveStatus))]
[JsonSerializable(typeof(VideoQuality))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<AccountInfo>))]
[JsonSerializable(typeof(List<AuthToken>))]
[JsonSerializable(typeof(List<StreamInfo>))]
[JsonSerializable(typeof(List<StreamUrlInfo>))]
[JsonSerializable(typeof(List<RecordingTask>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
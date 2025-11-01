using System.Text.Json.Serialization;

namespace DouyinLiveCapture.Models;

/// <summary>
/// 录制设置
/// </summary>
public class RecordingSettings
{
    /// <summary>
    /// 语言 (zh_cn/en)
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "zh_cn";

    /// <summary>
    /// 是否跳过代理检测
    /// </summary>
    [JsonPropertyName("skip_proxy_detection")]
    public bool SkipProxyDetection { get; set; } = false;

    /// <summary>
    /// 直播保存路径 (不填则默认)
    /// </summary>
    [JsonPropertyName("save_path")]
    public string? SavePath { get; set; }

    /// <summary>
    /// 保存文件夹是否以作者区分
    /// </summary>
    [JsonPropertyName("folder_by_author")]
    public bool FolderByAuthor { get; set; } = true;

    /// <summary>
    /// 保存文件夹是否以时间区分
    /// </summary>
    [JsonPropertyName("folder_by_time")]
    public bool FolderByTime { get; set; } = false;

    /// <summary>
    /// 保存文件夹是否以标题区分
    /// </summary>
    [JsonPropertyName("folder_by_title")]
    public bool FolderByTitle { get; set; } = false;

    /// <summary>
    /// 保存文件名是否包含标题
    /// </summary>
    [JsonPropertyName("filename_include_title")]
    public bool FilenameIncludeTitle { get; set; } = false;

    /// <summary>
    /// 是否去除名称中的表情符号
    /// </summary>
    [JsonPropertyName("remove_emoji")]
    public bool RemoveEmoji { get; set; } = true;

    /// <summary>
    /// 视频保存格式 (ts|mkv|flv|mp4|mp3|m4a)
    /// </summary>
    [JsonPropertyName("video_format")]
    public string VideoFormat { get; set; } = "ts";

    /// <summary>
    /// 视频质量 (原画|超清|高清|标清|流畅)
    /// </summary>
    [JsonPropertyName("video_quality")]
    public string VideoQuality { get; set; } = "原画";

    /// <summary>
    /// 是否使用代理IP
    /// </summary>
    [JsonPropertyName("use_proxy")]
    public bool UseProxy { get; set; } = true;

    /// <summary>
    /// 代理地址
    /// </summary>
    [JsonPropertyName("proxy_address")]
    public string? ProxyAddress { get; set; }

    /// <summary>
    /// 同一时间访问网络的线程数
    /// </summary>
    [JsonPropertyName("max_threads")]
    public int MaxThreads { get; set; } = 3;

    /// <summary>
    /// 循环时间 (秒)
    /// </summary>
    [JsonPropertyName("loop_interval")]
    public int LoopInterval { get; set; } = 300;

    /// <summary>
    /// 排队读取网址时间 (秒)
    /// </summary>
    [JsonPropertyName("queue_interval")]
    public int QueueInterval { get; set; } = 0;

    /// <summary>
    /// 是否显示循环秒数
    /// </summary>
    [JsonPropertyName("show_loop_seconds")]
    public bool ShowLoopSeconds { get; set; } = false;

    /// <summary>
    /// 是否显示直播源地址
    /// </summary>
    [JsonPropertyName("show_stream_url")]
    public bool ShowStreamUrl { get; set; } = false;

    /// <summary>
    /// 分段录制是否开启
    /// </summary>
    [JsonPropertyName("enable_segment_recording")]
    public bool EnableSegmentRecording { get; set; } = true;

    /// <summary>
    /// 是否强制启用https录制
    /// </summary>
    [JsonPropertyName("force_https")]
    public bool ForceHttps { get; set; } = false;

    /// <summary>
    /// 录制空间剩余阈值 (GB)
    /// </summary>
    [JsonPropertyName("disk_space_threshold")]
    public double DiskSpaceThreshold { get; set; } = 1.0;

    /// <summary>
    /// 视频分段时间 (秒)
    /// </summary>
    [JsonPropertyName("segment_duration")]
    public int SegmentDuration { get; set; } = 1800;

    /// <summary>
    /// 录制完成后自动转为mp4格式
    /// </summary>
    [JsonPropertyName("auto_convert_mp4")]
    public bool AutoConvertMp4 { get; set; } = true;

    /// <summary>
    /// mp4格式重新编码为h264
    /// </summary>
    [JsonPropertyName("mp4_reencode_h264")]
    public bool Mp4ReencodeH264 { get; set; } = false;

    /// <summary>
    /// 追加格式后删除原文件
    /// </summary>
    [JsonPropertyName("delete_original_after_convert")]
    public bool DeleteOriginalAfterConvert { get; set; } = true;

    /// <summary>
    /// 生成时间字幕文件
    /// </summary>
    [JsonPropertyName("generate_subtitle")]
    public bool GenerateSubtitle { get; set; } = false;

    /// <summary>
    /// 是否录制完成后执行自定义脚本
    /// </summary>
    [JsonPropertyName("execute_custom_script")]
    public bool ExecuteCustomScript { get; set; } = false;

    /// <summary>
    /// 自定义脚本执行命令
    /// </summary>
    [JsonPropertyName("custom_script_command")]
    public string? CustomScriptCommand { get; set; }

    /// <summary>
    /// 使用代理录制的平台 (逗号分隔)
    /// </summary>
    [JsonPropertyName("proxy_platforms")]
    public string ProxyPlatforms { get; set; } = "tiktok,sooplive,pandalive,winktv,flextv,popkontv,twitch,liveme,showroom,chzzk,shopee,shp,youtu";

    /// <summary>
    /// 额外使用代理录制的平台 (逗号分隔)
    /// </summary>
    [JsonPropertyName("extra_proxy_platforms")]
    public string? ExtraProxyPlatforms { get; set; }
}
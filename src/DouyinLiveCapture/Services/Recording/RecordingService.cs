using System.Diagnostics;
using System.Text.RegularExpressions;
using DouyinLiveCapture.Models;
using DouyinLiveCapture.Services.Configuration;

namespace DouyinLiveCapture.Services.Recording;

/// <summary>
/// 录制服务实现
/// </summary>
public class RecordingService : IRecordingService
{
    private readonly Dictionary<string, RecordingTask> _recordingTasks;
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer? _fileSizeMonitorTimer;
    private readonly object _lockObject = new object();

    public RecordingService()
    {
        _recordingTasks = new Dictionary<string, RecordingTask>();
        _semaphore = new SemaphoreSlim(1, 1);

        // 启动文件大小监控定时器（每30秒检查一次）
        _fileSizeMonitorTimer = new Timer(MonitorFileSizes, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <inheritdoc />
    public async Task<RecordingTask> StartRecordingAsync(StreamInfo streamInfo, RecordingSettings settings, CancellationToken cancellationToken = default)
    {
        if (streamInfo == null)
            throw new ArgumentNullException(nameof(streamInfo));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // 检查磁盘空间
            var savePath = string.IsNullOrEmpty(settings.SavePath) ?
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) : settings.SavePath;

            if (!await CheckDiskSpaceAsync(savePath, settings.DiskSpaceThreshold))
            {
                throw new InvalidOperationException($"Insufficient disk space. Required: {settings.DiskSpaceThreshold}GB");
            }

            // 检查是否已有录制任务
            var existingTask = _recordingTasks.Values
                .FirstOrDefault(t => t.StreamInfo.Url == streamInfo.Url &&
                                 (t.Status == RecordingTaskStatus.Recording || t.Status == RecordingTaskStatus.Pending));

            if (existingTask != null)
            {
                return existingTask;
            }

            // 创建录制任务
            var recordingTask = new RecordingTask
            {
                StreamInfo = streamInfo,
                Status = RecordingTaskStatus.Pending,
                StartTime = DateTime.UtcNow,
                OutputFilePath = GenerateRecordingFilePath(streamInfo, settings)
            };

            // 确保输出目录存在
            var outputDirectory = Path.GetDirectoryName(recordingTask.OutputFilePath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            _recordingTasks[recordingTask.Id] = recordingTask;

            // 启动录制
            _ = Task.Run(async () => await ExecuteRecordingAsync(recordingTask, settings), cancellationToken);

            return recordingTask;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RecordingTask?> StopRecordingAsync(string taskId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_recordingTasks.TryGetValue(taskId, out var task))
                return null;

            if (task.Status == RecordingTaskStatus.Recording || task.Status == RecordingTaskStatus.Pending)
            {
                task.Status = RecordingTaskStatus.Stopped;
                task.EndTime = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;

                // 如果有进程，尝试停止
                if (task.ProcessId.HasValue)
                {
                    try
                    {
                        var process = Process.GetProcessById(task.ProcessId.Value);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            await process.WaitForExitAsync(cancellationToken);
                        }
                    }
                    catch
                    {
                        // 进程可能已经结束，忽略错误
                    }
                }
            }

            return task;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<RecordingTask?> GetRecordingTaskAsync(string taskId)
    {
        await Task.CompletedTask; // 保持异步签名一致性

        lock (_lockObject)
        {
            return _recordingTasks.TryGetValue(taskId, out var task) ? task : null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecordingTask>> GetAllRecordingTasksAsync()
    {
        await Task.CompletedTask; // 保持异步签名一致性

        lock (_lockObject)
        {
            return _recordingTasks.Values.ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckDiskSpaceAsync(string path, double requiredSpaceGB = 1.0)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(path) ?? path);
            var availableSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            return availableSpaceGB >= requiredSpaceGB;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public string GenerateRecordingFileName(StreamInfo streamInfo, RecordingSettings settings)
    {
        if (streamInfo == null)
            throw new ArgumentNullException(nameof(streamInfo));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        var fileName = new List<string>();

        // 按作者分类
        if (settings.FolderByAuthor && !string.IsNullOrEmpty(streamInfo.AnchorName))
        {
            var cleanAnchorName = CleanFileName(streamInfo.AnchorName);
            fileName.Add(cleanAnchorName);
        }

        // 按时间分类
        if (settings.FolderByTime)
        {
            fileName.Add(DateTime.Now.ToString("yyyy-MM-dd"));
        }

        // 按标题分类
        if (settings.FolderByTitle && !string.IsNullOrEmpty(streamInfo.Title))
        {
            var cleanTitle = CleanFileName(streamInfo.Title);
            fileName.Add(cleanTitle);
        }

        // 生成基础文件名
        var baseFileName = $"{DateTime.Now:HHmmss}";

        if (settings.FilenameIncludeTitle && !string.IsNullOrEmpty(streamInfo.Title))
        {
            var cleanTitle = CleanFileName(streamInfo.Title);
            baseFileName += $"_{cleanTitle}";
        }

        // 添加平台和房间ID
        baseFileName += $"_{streamInfo.Platform}_{streamInfo.RoomId ?? "unknown"}";

        // 添加扩展名
        var extension = settings.VideoFormat.ToLowerInvariant();
        if (!extension.StartsWith('.'))
            extension = $".{extension}";

        baseFileName += extension;

        // 组合完整路径
        var fullPath = fileName.Count > 0
            ? Path.Combine(fileName.ToArray())
            : string.Empty;

        return Path.Combine(fullPath, baseFileName);
    }

    /// <summary>
    /// 生成录制文件完整路径
    /// </summary>
    private string GenerateRecordingFilePath(StreamInfo streamInfo, RecordingSettings settings)
    {
        var savePath = string.IsNullOrEmpty(settings.SavePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            : settings.SavePath;

        var fileName = GenerateRecordingFileName(streamInfo, settings);
        return Path.Combine(savePath, fileName);
    }

    /// <summary>
    /// 执行录制
    /// </summary>
    private async Task ExecuteRecordingAsync(RecordingTask task, RecordingSettings settings)
    {
        try
        {
            task.Status = RecordingTaskStatus.Recording;
            task.UpdatedAt = DateTime.UtcNow;

            var streamUrl = task.StreamInfo.GetBestRecordUrl();
            if (string.IsNullOrEmpty(streamUrl))
            {
                task.Status = RecordingTaskStatus.Error;
                task.ErrorMessage = "No valid stream URL found";
                return;
            }

            // 使用 FFmpeg 进行录制（简化版本）
            var ffmpegPath = "ffmpeg"; // 假设 FFmpeg 在 PATH 中
            var arguments = $"-i \"{streamUrl}\" -c copy \"{task.OutputFilePath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            process.Start();
            task.ProcessId = process.Id;

            // 等待进程完成
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                task.Status = RecordingTaskStatus.Completed;
            }
            else
            {
                task.Status = RecordingTaskStatus.Error;
                task.ErrorMessage = $"FFmpeg exited with code {process.ExitCode}";
            }
        }
        catch (Exception ex)
        {
            task.Status = RecordingTaskStatus.Error;
            task.ErrorMessage = ex.Message;
        }
        finally
        {
            task.EndTime = DateTime.UtcNow;
            task.UpdatedAt = DateTime.UtcNow;

            // 更新文件大小
            if (File.Exists(task.OutputFilePath))
            {
                var fileInfo = new FileInfo(task.OutputFilePath);
                task.FileSize = fileInfo.Length;
            }
        }
    }

    /// <summary>
    /// 监控文件大小
    /// </summary>
    private async void MonitorFileSizes(object? state)
    {
        try
        {
            var recordingTasks = _recordingTasks.Values
                .Where(t => t.Status == RecordingTaskStatus.Recording)
                .ToList();

            foreach (var task in recordingTasks)
            {
                if (File.Exists(task.OutputFilePath))
                {
                    var fileInfo = new FileInfo(task.OutputFilePath);
                    task.FileSize = fileInfo.Length;
                    task.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        catch
        {
            // 忽略监控错误
        }
    }

    /// <summary>
    /// 清理文件名
    /// </summary>
    private string CleanFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        // 移除不允许的字符
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanName = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // 替换空格为下划线
        cleanName = cleanName.Replace(" ", "_");

        // 移除emoji和特殊字符
        cleanName = Regex.Replace(cleanName, @"[\p{Cs}\p{Co}\p{Sk}]", "");

        // 移除连续的下划线
        cleanName = Regex.Replace(cleanName, @"_+", "_");

        // 限制长度
        if (cleanName.Length > 50)
        {
            cleanName = cleanName.Substring(0, 50);
        }

        return cleanName.Trim('_');
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _fileSizeMonitorTimer?.Dispose();
        _semaphore?.Dispose();

        // 停止所有录制任务
        var tasks = _recordingTasks.Values
            .Where(t => t.Status == RecordingTaskStatus.Recording)
            .ToList();

        foreach (var task in tasks)
        {
            try
            {
                StopRecordingAsync(task.Id).Wait();
            }
            catch
            {
                // 忽略停止错误
            }
        }
    }
}
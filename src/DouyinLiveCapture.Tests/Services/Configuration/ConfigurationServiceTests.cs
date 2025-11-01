using Microsoft.VisualStudio.TestTools.UnitTesting;
using DouyinLiveCapture.Services.Configuration;
using DouyinLiveCapture.Models;

namespace DouyinLiveCapture.Tests.Services.Configuration;

/// <summary>
/// ConfigurationService 单元测试
/// </summary>
[TestClass]
public class ConfigurationServiceTests
{
    private ConfigurationService _configurationService = null!;
    private string _tempDirectory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _configurationService = new ConfigurationService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ConfigTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    #region LoadRecordingSettingsAsync Tests

    [TestMethod]
    public async Task LoadRecordingSettingsAsync_FileNotExists_CreatesDefaultSettings()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "config.json");

        // Act
        var result = await _configurationService.LoadRecordingSettingsAsync(configPath);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("zh_cn", result.Language);
        Assert.IsFalse(result.SkipProxyDetection);
        Assert.IsTrue(result.FolderByAuthor);
        Assert.AreEqual("ts", result.VideoFormat);
        Assert.AreEqual("原画", result.VideoQuality);
        Assert.IsTrue(result.UseProxy);
        Assert.AreEqual(3, result.MaxThreads);
        Assert.AreEqual(300, result.LoopInterval);

        // Verify file was created
        Assert.IsTrue(File.Exists(configPath));
    }

    [TestMethod]
    public async Task LoadRecordingSettingsAsync_ValidJsonFile_ReturnsSettings()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "config.json");
        var jsonContent = @"{
            ""language"": ""en"",
            ""skip_proxy_detection"": true,
            ""save_path"": ""/tmp/recordings"",
            ""folder_by_author"": false,
            ""folder_by_time"": true,
            ""video_format"": ""mp4"",
            ""video_quality"": ""高清"",
            ""use_proxy"": false,
            ""max_threads"": 5,
            ""loop_interval"": 600
        }";
        await File.WriteAllTextAsync(configPath, jsonContent);

        // Act
        var result = await _configurationService.LoadRecordingSettingsAsync(configPath);

        // Assert
        Assert.AreEqual("en", result.Language);
        Assert.IsTrue(result.SkipProxyDetection);
        Assert.AreEqual("/tmp/recordings", result.SavePath);
        Assert.IsFalse(result.FolderByAuthor);
        Assert.IsTrue(result.FolderByTime);
        Assert.AreEqual("mp4", result.VideoFormat);
        Assert.AreEqual("高清", result.VideoQuality);
        Assert.IsFalse(result.UseProxy);
        Assert.AreEqual(5, result.MaxThreads);
        Assert.AreEqual(600, result.LoopInterval);
    }

    [TestMethod]
    public async Task LoadRecordingSettingsAsync_EmptyFile_ReturnsDefaultSettings()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, "");

        // Act
        var result = await _configurationService.LoadRecordingSettingsAsync(configPath);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("zh_cn", result.Language);
        Assert.IsFalse(result.SkipProxyDetection);
    }

    [TestMethod]
    public async Task LoadRecordingSettingsAsync_InvalidJsonFile_ReturnsDefaultSettings()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, "{ invalid json }");

        // Act
        var result = await _configurationService.LoadRecordingSettingsAsync(configPath);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("zh_cn", result.Language);
        Assert.IsFalse(result.SkipProxyDetection);
    }

    [TestMethod]
    public async Task LoadRecordingSettingsAsync_IniFormat_ParsesCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "config.ini");
        var iniContent = @"language=en
是否跳过代理检测=true
直播保存路径=/tmp/recordings
保存文件夹是否以作者区分=false
保存文件夹是否以时间区分=true
视频保存格式=mp4
原画=高清
是否使用代理ip=false
同一时间访问网络的线程数=5
循环时间=600";
        await File.WriteAllTextAsync(configPath, iniContent);

        // Act
        var result = await _configurationService.LoadRecordingSettingsAsync(configPath);

        // Assert
        Assert.AreEqual("en", result.Language);
        Assert.IsTrue(result.SkipProxyDetection);
        Assert.AreEqual("/tmp/recordings", result.SavePath);
        Assert.IsFalse(result.FolderByAuthor);
        Assert.IsTrue(result.FolderByTime);
        Assert.AreEqual("mp4", result.VideoFormat);
        Assert.AreEqual("高清", result.VideoQuality);
        Assert.IsFalse(result.UseProxy);
        Assert.AreEqual(5, result.MaxThreads);
        Assert.AreEqual(600, result.LoopInterval);
    }

    [TestMethod]
    public async Task LoadRecordingSettingsAsync_LoadFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidPath = @"X:\invalid\path\config.json";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _configurationService.LoadRecordingSettingsAsync(invalidPath));
    }

    #endregion

    #region SaveRecordingSettingsAsync Tests

    [TestMethod]
    public async Task SaveRecordingSettingsAsync_ValidSettings_CreatesJsonFile()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "config.json");
        var settings = new RecordingSettings
        {
            Language = "en",
            SkipProxyDetection = true,
            SavePath = "/tmp/recordings",
            FolderByAuthor = false,
            VideoFormat = "mp4",
            VideoQuality = "高清",
            UseProxy = false,
            MaxThreads = 5,
            LoopInterval = 600
        };

        // Act
        await _configurationService.SaveRecordingSettingsAsync(settings, configPath);

        // Assert
        Assert.IsTrue(File.Exists(configPath));
        var content = await File.ReadAllTextAsync(configPath);
        Assert.IsTrue(content.Contains("\"language\": \"en\""));
        Assert.IsTrue(content.Contains("\"skip_proxy_detection\": true"));
        Assert.IsTrue(content.Contains("\"save_path\": \"/tmp/recordings\""));
    }

    [TestMethod]
    public async Task SaveRecordingSettingsAsync_DirectoryNotExists_CreatesDirectory()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "subdir", "config.json");
        var settings = new RecordingSettings();

        // Act
        await _configurationService.SaveRecordingSettingsAsync(settings, configPath);

        // Assert
        Assert.IsTrue(File.Exists(configPath));
        Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(configPath)));
    }

    [TestMethod]
    public async Task SaveRecordingSettingsAsync_SaveFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidPath = @"X:\invalid\path\config.json";
        var settings = new RecordingSettings();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _configurationService.SaveRecordingSettingsAsync(settings, invalidPath));
    }

    #endregion

    #region LoadMonitorUrlsAsync Tests

    [TestMethod]
    public async Task LoadMonitorUrlsAsync_FileNotExists_ReturnsEmptyList()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");

        // Act
        var result = await _configurationService.LoadMonitorUrlsAsync(urlConfigPath);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task LoadMonitorUrlsAsync_ValidFile_ReturnsUrls()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");
        var urls = new[]
        {
            "https://example.com/stream1",
            "https://example.com/stream2",
            "https://example.com/stream3"
        };
        await File.WriteAllLinesAsync(urlConfigPath, urls);

        // Act
        var result = await _configurationService.LoadMonitorUrlsAsync(urlConfigPath);

        // Assert
        Assert.AreEqual(3, result.Count);
        CollectionAssert.AreEqual(urls, result);
    }

    [TestMethod]
    public async Task LoadMonitorUrlsAsync_WithComments_IgnoresComments()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");
        var content = @"# This is a comment
https://example.com/stream1
; This is also a comment
https://example.com/stream2

# Another comment
https://example.com/stream3";
        await File.WriteAllTextAsync(urlConfigPath, content);

        // Act
        var result = await _configurationService.LoadMonitorUrlsAsync(urlConfigPath);

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.All(url => url.StartsWith("https://example.com/stream")));
    }

    [TestMethod]
    public async Task LoadMonitorUrlsAsync_WithInlineComments_IgnoresCommentedLines()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");
        var content = @"https://example.com/stream1
https://example.com/stream2,#commented out
https://example.com/stream3";
        await File.WriteAllTextAsync(urlConfigPath, content);

        // Act
        var result = await _configurationService.LoadMonitorUrlsAsync(urlConfigPath);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains("https://example.com/stream1"));
        Assert.IsTrue(result.Contains("https://example.com/stream3"));
        Assert.IsFalse(result.Contains("https://example.com/stream2"));
    }

    [TestMethod]
    public async Task LoadMonitorUrlsAsync_EmptyLines_IgnoresEmptyLines()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");
        var content = @"https://example.com/stream1

https://example.com/stream2

";
        await File.WriteAllTextAsync(urlConfigPath, content);

        // Act
        var result = await _configurationService.LoadMonitorUrlsAsync(urlConfigPath);

        // Assert
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task LoadMonitorUrlsAsync_LoadFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidPath = @"X:\invalid\path\urls.txt";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _configurationService.LoadMonitorUrlsAsync(invalidPath));
    }

    #endregion

    #region SaveMonitorUrlsAsync Tests

    [TestMethod]
    public async Task SaveMonitorUrlsAsync_ValidUrls_CreatesFile()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");
        var urls = new List<string>
        {
            "https://example.com/stream1",
            "https://example.com/stream2",
            "https://example.com/stream3"
        };

        // Act
        await _configurationService.SaveMonitorUrlsAsync(urls, urlConfigPath);

        // Assert
        Assert.IsTrue(File.Exists(urlConfigPath));
        var lines = await File.ReadAllLinesAsync(urlConfigPath);
        CollectionAssert.AreEqual(urls, lines);
    }

    [TestMethod]
    public async Task SaveMonitorUrlsAsync_EmptyList_CreatesEmptyFile()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "urls.txt");
        var urls = new List<string>();

        // Act
        await _configurationService.SaveMonitorUrlsAsync(urls, urlConfigPath);

        // Assert
        Assert.IsTrue(File.Exists(urlConfigPath));
        var content = await File.ReadAllTextAsync(urlConfigPath);
        Assert.AreEqual("", content);
    }

    [TestMethod]
    public async Task SaveMonitorUrlsAsync_DirectoryNotExists_CreatesDirectory()
    {
        // Arrange
        var urlConfigPath = Path.Combine(_tempDirectory, "subdir", "urls.txt");
        var urls = new List<string> { "https://example.com/stream1" };

        // Act
        await _configurationService.SaveMonitorUrlsAsync(urls, urlConfigPath);

        // Assert
        Assert.IsTrue(File.Exists(urlConfigPath));
        Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(urlConfigPath)));
    }

    [TestMethod]
    public async Task SaveMonitorUrlsAsync_SaveFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidPath = @"X:\invalid\path\urls.txt";
        var urls = new List<string> { "https://example.com/stream1" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _configurationService.SaveMonitorUrlsAsync(urls, invalidPath));
    }

    #endregion

    #region GetDefaultConfigPath Tests

    [TestMethod]
    public void GetDefaultConfigPath_ReturnsValidPath()
    {
        // Act
        var result = _configurationService.GetDefaultConfigPath();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.EndsWith("config.json"));
        Assert.IsTrue(result.Contains("DouyinLiveCapture"));
        Assert.IsTrue(Path.IsPathRooted(result));
    }

    [TestMethod]
    public void GetDefaultConfigPath_ConsistentResults_ReturnsSamePath()
    {
        // Act
        var result1 = _configurationService.GetDefaultConfigPath();
        var result2 = _configurationService.GetDefaultConfigPath();

        // Assert
        Assert.AreEqual(result1, result2);
    }

    #endregion

    #region GetDefaultUrlConfigPath Tests

    [TestMethod]
    public void GetDefaultUrlConfigPath_ReturnsValidPath()
    {
        // Act
        var result = _configurationService.GetDefaultUrlConfigPath();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.EndsWith("urls.txt"));
        Assert.IsTrue(result.Contains("DouyinLiveCapture"));
        Assert.IsTrue(Path.IsPathRooted(result));
    }

    [TestMethod]
    public void GetDefaultUrlConfigPath_ConsistentResults_ReturnsSamePath()
    {
        // Act
        var result1 = _configurationService.GetDefaultUrlConfigPath();
        var result2 = _configurationService.GetDefaultUrlConfigPath();

        // Assert
        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void GetDefaultUrlConfigPath_DifferentFromConfigPath_ReturnsDifferentPaths()
    {
        // Act
        var configPath = _configurationService.GetDefaultConfigPath();
        var urlConfigPath = _configurationService.GetDefaultUrlConfigPath();

        // Assert
        Assert.AreNotEqual(configPath, urlConfigPath);
        Assert.IsTrue(configPath.EndsWith("config.json"));
        Assert.IsTrue(urlConfigPath.EndsWith("urls.txt"));
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_CreatesInstance()
    {
        // Act
        var service = new ConfigurationService();

        // Assert
        Assert.IsNotNull(service);
    }

    #endregion
}
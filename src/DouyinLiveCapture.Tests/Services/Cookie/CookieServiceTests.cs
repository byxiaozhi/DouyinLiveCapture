using Microsoft.VisualStudio.TestTools.UnitTesting;
using DouyinLiveCapture.Services.Cookie;

namespace DouyinLiveCapture.Tests.Services.Cookie;

/// <summary>
/// CookieService 单元测试
/// </summary>
[TestClass]
public class CookieServiceTests
{
    private CookieService _cookieService = null!;
    private string _tempDirectory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "CookieTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        var storagePath = Path.Combine(_tempDirectory, "cookies.json");
        _cookieService = new CookieService(storagePath);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _cookieService?.Dispose();

        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithCustomPath_CreatesInstance()
    {
        // Arrange
        var storagePath = Path.Combine(_tempDirectory, "custom_cookies.json");

        // Act
        var service = new CookieService(storagePath);

        // Assert
        Assert.IsNotNull(service);
        Assert.IsTrue(Directory.Exists(_tempDirectory));
    }

    #endregion

    #region GetCookieAsync Tests

    [TestMethod]
    public async Task GetCookieAsync_ExistingPlatform_ReturnsCookie()
    {
        // Arrange
        const string platform = "douyin";
        const string cookie = "test_cookie_value";
        await _cookieService.SetCookieAsync(platform, cookie);

        // Act
        var result = await _cookieService.GetCookieAsync(platform);

        // Assert
        Assert.AreEqual(cookie, result);
    }

    [TestMethod]
    public async Task GetCookieAsync_NonExistingPlatform_ReturnsNull()
    {
        // Arrange
        const string platform = "nonexistent";

        // Act
        var result = await _cookieService.GetCookieAsync(platform);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetCookieAsync_NullPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.GetCookieAsync(null!));
    }

    [TestMethod]
    public async Task GetCookieAsync_EmptyPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.GetCookieAsync(""));
    }

    [TestMethod]
    public async Task GetCookieAsync_CaseInsensitivePlatform_ReturnsCookie()
    {
        // Arrange
        const string cookie = "test_cookie_value";
        await _cookieService.SetCookieAsync("Douyin", cookie);

        // Act
        var result = await _cookieService.GetCookieAsync("DOUYIN");

        // Assert
        Assert.AreEqual(cookie, result);
    }

    #endregion

    #region SetCookieAsync Tests

    [TestMethod]
    public async Task SetCookieAsync_ValidPlatformAndCookie_SavesCookie()
    {
        // Arrange
        const string platform = "douyin";
        const string cookie = "test_cookie_value";

        // Act
        await _cookieService.SetCookieAsync(platform, cookie);

        // Assert
        var result = await _cookieService.GetCookieAsync(platform);
        Assert.AreEqual(cookie, result);
    }

    [TestMethod]
    public async Task SetCookieAsync_OverwriteExistingCookie_UpdatesCookie()
    {
        // Arrange
        const string platform = "douyin";
        const string originalCookie = "original_cookie";
        const string newCookie = "new_cookie";
        await _cookieService.SetCookieAsync(platform, originalCookie);

        // Act
        await _cookieService.SetCookieAsync(platform, newCookie);

        // Assert
        var result = await _cookieService.GetCookieAsync(platform);
        Assert.AreEqual(newCookie, result);
        Assert.AreNotEqual(originalCookie, result);
    }

    [TestMethod]
    public async Task SetCookieAsync_NullPlatform_ThrowsArgumentException()
    {
        // Arrange
        const string cookie = "test_cookie";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.SetCookieAsync(null!, cookie));
    }

    [TestMethod]
    public async Task SetCookieAsync_EmptyPlatform_ThrowsArgumentException()
    {
        // Arrange
        const string cookie = "test_cookie";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.SetCookieAsync("", cookie));
    }

    [TestMethod]
    public async Task SetCookieAsync_NullCookie_ThrowsArgumentException()
    {
        // Arrange
        const string platform = "douyin";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.SetCookieAsync(platform, null!));
    }

    [TestMethod]
    public async Task SetCookieAsync_EmptyCookie_ThrowsArgumentException()
    {
        // Arrange
        const string platform = "douyin";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.SetCookieAsync(platform, ""));
    }

    [TestMethod]
    public async Task SetCookieAsync_WithWhitespace_TrimWhitespace()
    {
        // Arrange
        const string platform = "douyin";
        const string cookie = "  test_cookie_value  ";

        // Act
        await _cookieService.SetCookieAsync(platform, cookie);

        // Assert
        var result = await _cookieService.GetCookieAsync(platform);
        Assert.AreEqual(cookie.Trim(), result);
    }

    #endregion

    #region DeleteCookieAsync Tests

    [TestMethod]
    public async Task DeleteCookieAsync_ExistingPlatform_ReturnsTrue()
    {
        // Arrange
        const string platform = "douyin";
        const string cookie = "test_cookie";
        await _cookieService.SetCookieAsync(platform, cookie);

        // Act
        var result = await _cookieService.DeleteCookieAsync(platform);

        // Assert
        Assert.IsTrue(result);
        var retrievedCookie = await _cookieService.GetCookieAsync(platform);
        Assert.IsNull(retrievedCookie);
    }

    [TestMethod]
    public async Task DeleteCookieAsync_NonExistingPlatform_ReturnsFalse()
    {
        // Arrange
        const string platform = "nonexistent";

        // Act
        var result = await _cookieService.DeleteCookieAsync(platform);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task DeleteCookieAsync_NullPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.DeleteCookieAsync(null!));
    }

    [TestMethod]
    public async Task DeleteCookieAsync_EmptyPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.DeleteCookieAsync(""));
    }

    #endregion

    #region GetAllCookiesAsync Tests

    [TestMethod]
    public async Task GetAllCookiesAsync_EmptyStorage_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _cookieService.GetAllCookiesAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetAllCookiesAsync_WithMultipleCookies_ReturnsAllCookies()
    {
        // Arrange
        await _cookieService.SetCookieAsync("douyin", "cookie1");
        await _cookieService.SetCookieAsync("tiktok", "cookie2");
        await _cookieService.SetCookieAsync("bilibili", "cookie3");

        // Act
        var result = await _cookieService.GetAllCookiesAsync();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("cookie1", result["douyin"]);
        Assert.AreEqual("cookie2", result["tiktok"]);
        Assert.AreEqual("cookie3", result["bilibili"]);
    }

    [TestMethod]
    public async Task GetAllCookiesAsync_CaseInsensitiveKeys_ReturnsNormalizedKeys()
    {
        // Arrange
        await _cookieService.SetCookieAsync("Douyin", "cookie1");
        await _cookieService.SetCookieAsync("TIKTOK", "cookie2");

        // Act
        var result = await _cookieService.GetAllCookiesAsync();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.ContainsKey("douyin"));
        Assert.IsTrue(result.ContainsKey("tiktok"));
    }

    #endregion

    #region HasCookieAsync Tests

    [TestMethod]
    public async Task HasCookieAsync_ExistingPlatform_ReturnsTrue()
    {
        // Arrange
        const string platform = "douyin";
        await _cookieService.SetCookieAsync(platform, "test_cookie");

        // Act
        var result = await _cookieService.HasCookieAsync(platform);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasCookieAsync_NonExistingPlatform_ReturnsFalse()
    {
        // Arrange
        const string platform = "nonexistent";

        // Act
        var result = await _cookieService.HasCookieAsync(platform);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasCookieAsync_NullPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.HasCookieAsync(null!));
    }

    [TestMethod]
    public async Task HasCookieAsync_EmptyPlatform_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _cookieService.HasCookieAsync(""));
    }

    #endregion

    #region GetSupportedPlatforms Tests

    [TestMethod]
    public void GetSupportedPlatforms_ReturnsListOfPlatforms()
    {
        // Act
        var result = _cookieService.GetSupportedPlatforms();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0);
        Assert.IsTrue(result.Contains("douyin"));
        Assert.IsTrue(result.Contains("tiktok"));
        Assert.IsTrue(result.Contains("bilibili"));
        Assert.IsTrue(result.Contains("抖音"));
    }

    [TestMethod]
    public void GetSupportedPlatforms_ReturnsReadOnlyList()
    {
        // Act
        var result = _cookieService.GetSupportedPlatforms();

        // Assert
        Assert.IsNotNull(result);
        // IReadOnlyList<T> is inherently read-only by contract
    }

    #endregion

    #region Platform Normalization Tests

    [TestMethod]
    public async Task PlatformNormalization_ChineseNames_NormalizesCorrectly()
    {
        // Arrange & Act
        await _cookieService.SetCookieAsync("抖音", "douyin_cookie");
        await _cookieService.SetCookieAsync("快手", "kuaishou_cookie");
        await _cookieService.SetCookieAsync("虎牙", "huya_cookie");

        // Assert
        Assert.AreEqual("douyin_cookie", await _cookieService.GetCookieAsync("douyin"));
        Assert.AreEqual("douyin_cookie", await _cookieService.GetCookieAsync("抖音"));
        Assert.AreEqual("kuaishou_cookie", await _cookieService.GetCookieAsync("kuaishou"));
        Assert.AreEqual("huya_cookie", await _cookieService.GetCookieAsync("huya"));
    }

    [TestMethod]
    public async Task PlatformNormalization_UnsupportedPlatform_UsesLowerCase()
    {
        // Arrange & Act
        await _cookieService.SetCookieAsync("UnknownPlatform", "test_cookie");

        // Assert
        Assert.AreEqual("test_cookie", await _cookieService.GetCookieAsync("unknownplatform"));
        Assert.IsNull(await _cookieService.GetCookieAsync("different_platform"));
    }

    #endregion

    #region Persistence Tests

    [TestMethod]
    public async Task Persistence_SaveAndReload_RetainsData()
    {
        // Arrange
        await _cookieService.SetCookieAsync("douyin", "persistent_cookie");
        await _cookieService.SetCookieAsync("tiktok", "another_cookie");

        // Act - Create new instance to simulate restart
        var storagePath = Path.Combine(_tempDirectory, "cookies.json");
        using var newService = new CookieService(storagePath);

        // Assert
        Assert.AreEqual("persistent_cookie", await newService.GetCookieAsync("douyin"));
        Assert.AreEqual("another_cookie", await newService.GetCookieAsync("tiktok"));
        Assert.AreEqual(2, (await newService.GetAllCookiesAsync()).Count);
    }

    [TestMethod]
    public async Task Persistence_ModifyExistingFile_UpdatesCorrectly()
    {
        // Arrange - Initial data
        await _cookieService.SetCookieAsync("douyin", "original_cookie");

        // Act - Modify
        await _cookieService.SetCookieAsync("douyin", "modified_cookie");
        await _cookieService.SetCookieAsync("tiktok", "new_cookie");

        // Assert
        var allCookies = await _cookieService.GetAllCookiesAsync();
        Assert.AreEqual(2, allCookies.Count);
        Assert.AreEqual("modified_cookie", allCookies["douyin"]);
        Assert.AreEqual("new_cookie", allCookies["tiktok"]);
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public async Task ConcurrentAccess_MultipleSetAndGet_HandlesCorrectly()
    {
        // Arrange
        const int taskCount = 10;
        var tasks = new List<Task>();

        // Act - Concurrent set operations
        for (int i = 0; i < taskCount; i++)
        {
            var index = i;
            tasks.Add(_cookieService.SetCookieAsync($"platform_{index}", $"cookie_{index}"));
        }

        await Task.WhenAll(tasks);

        // Assert
        var allCookies = await _cookieService.GetAllCookiesAsync();
        Assert.AreEqual(taskCount, allCookies.Count);

        for (int i = 0; i < taskCount; i++)
        {
            Assert.AreEqual($"cookie_{i}", await _cookieService.GetCookieAsync($"platform_{i}"));
        }
    }

    #endregion
}
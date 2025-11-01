using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http;
using DouyinLiveCapture.Services.Utilities;
using DouyinLiveCapture.Services.Exceptions;

namespace DouyinLiveCapture.Tests.Services.Utilities;

/// <summary>
/// ErrorHandlingService 单元测试
/// </summary>
[TestClass]
public class ErrorHandlingServiceTests
{
    private Mock<ILogger<ErrorHandlingService>> _loggerMock = null!;
    private ErrorHandlingService _errorHandlingService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<ErrorHandlingService>>();
        _errorHandlingService = new ErrorHandlingService(_loggerMock.Object);
    }

    #region SafeExecuteAsync Tests

    [TestMethod]
    public async Task SafeExecuteAsync_Generic_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        const string expectedResult = "Test Result";
        var operation = new Func<Task<string>>(() => Task.FromResult(expectedResult));

        // Act
        var result = await _errorHandlingService.SafeExecuteAsync(operation, "default", "TestOperation");

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_Generic_FailedOperation_ReturnsDefaultValue()
    {
        // Arrange
        const string defaultValue = "Default Result";
        var operation = new Func<Task<string>>(() => throw new InvalidOperationException("Test exception"));

        // Act
        var result = await _errorHandlingService.SafeExecuteAsync(operation, defaultValue, "TestOperation");

        // Assert
        Assert.AreEqual(defaultValue, result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestOperation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_Void_SuccessfulOperation_DoesNotThrow()
    {
        // Arrange
        var isExecuted = false;
        var operation = new Func<Task>(() =>
        {
            isExecuted = true;
            return Task.CompletedTask;
        });

        // Act & Assert
        await _errorHandlingService.SafeExecuteAsync(operation, "TestOperation");
        Assert.IsTrue(isExecuted);
    }

    [TestMethod]
    public async Task SafeExecuteAsync_Void_FailedOperation_LogsException()
    {
        // Arrange
        var operation = new Func<Task>(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        await _errorHandlingService.SafeExecuteAsync(operation, "TestOperation");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestOperation")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RetryAsync Tests

    [TestMethod]
    public async Task RetryAsync_Generic_SuccessOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        const string expectedResult = "Success";
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            return Task.FromResult(expectedResult);
        });

        // Act
        var result = await _errorHandlingService.RetryAsync(operation, 3);

        // Assert
        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task RetryAsync_Generic_SuccessAfterRetries_ReturnsResult()
    {
        // Arrange
        const string expectedResult = "Success";
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            if (callCount < 3)
            {
                throw new HttpRequestException("Temporary failure");
            }
            return Task.FromResult(expectedResult);
        });

        // Act
        var result = await _errorHandlingService.RetryAsync(operation, 3);

        // Assert
        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(3, callCount);
    }

    [TestMethod]
    public async Task RetryAsync_Generic_ExceedsMaxAttempts_ThrowsLastException()
    {
        // Arrange
        var operation = new Func<Task<string>>(() => throw new HttpRequestException("Persistent failure"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _errorHandlingService.RetryAsync(operation, 2));

        Assert.AreEqual("Persistent failure", exception.Message);
    }

    [TestMethod]
    public async Task RetryAsync_Generic_CustomShouldRetry_RespectsCustomLogic()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task<string>>(() =>
        {
            callCount++;
            throw new InvalidOperationException("Non-retryable exception");
        });
        var shouldRetry = new Func<Exception, bool>(ex => ex is HttpRequestException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _errorHandlingService.RetryAsync(operation, 3, TimeSpan.FromSeconds(1), shouldRetry));

        Assert.AreEqual(1, callCount); // Should only be called once
    }

    [TestMethod]
    public async Task RetryAsync_Void_SuccessOnFirstAttempt_DoesNotThrow()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task>(() =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        // Act & Assert
        await _errorHandlingService.RetryAsync(operation, 3);
        Assert.AreEqual(1, callCount);
    }

    [TestMethod]
    public async Task RetryAsync_Void_SuccessAfterRetries_DoesNotThrow()
    {
        // Arrange
        var callCount = 0;
        var operation = new Func<Task>(() =>
        {
            callCount++;
            if (callCount < 2)
            {
                throw new HttpRequestException("Temporary failure");
            }
            return Task.CompletedTask;
        });

        // Act & Assert
        await _errorHandlingService.RetryAsync(operation, 3);
        Assert.AreEqual(2, callCount);
    }

    #endregion

    #region LogException Tests

    [TestMethod]
    public void LogException_WithContext_LogsWithContext()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        const string context = "TestContext";

        // Act
        _errorHandlingService.LogException(exception, context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(context)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public void LogException_WithParameters_LogsWithParameters()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        const string context = "TestContext";
        var parameters = new object[] { "param1", 123, true };

        // Act
        _errorHandlingService.LogException(exception, context, parameters);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region IsRetryableException Tests

    [TestMethod]
    public void IsRetryableException_HttpRequestExceptionWithRetryableStatus_ReturnsTrue()
    {
        // Arrange
        var exception = new HttpRequestException("Server error", null, HttpStatusCode.InternalServerError);

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRetryableException_HttpRequestExceptionWithNonRetryableStatus_ReturnsFalse()
    {
        // Arrange
        var exception = new HttpRequestException("Not found", null, HttpStatusCode.NotFound);

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRetryableException_TaskCanceledException_ReturnsTrue()
    {
        // Arrange
        var exception = new TaskCanceledException("Task cancelled");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRetryableException_TimeoutException_ReturnsTrue()
    {
        // Arrange
        var exception = new TimeoutException("Operation timed out");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRetryableException_IOException_ReturnsTrue()
    {
        // Arrange
        var exception = new IOException("IO error");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRetryableException_ApiRateLimitException_ReturnsTrue()
    {
        // Arrange
        var exception = new ApiRateLimitException(60);

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRetryableException_NetworkTimeoutException_ReturnsTrue()
    {
        // Arrange
        var exception = new NetworkTimeoutException(TimeSpan.FromSeconds(30));

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsRetryableException_SignatureGenerationException_ReturnsFalse()
    {
        // Arrange
        var exception = new SignatureGenerationException("ABSignature");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRetryableException_LiveRoomNotFoundException_ReturnsFalse()
    {
        // Arrange
        var exception = new LiveRoomNotFoundException("12345");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRetryableException_LiveStreamEndedException_ReturnsFalse()
    {
        // Arrange
        var exception = new LiveStreamEndedException("12345");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsRetryableException_GeneralException_ReturnsFalse()
    {
        // Arrange
        var exception = new InvalidOperationException("General exception");

        // Act
        var result = _errorHandlingService.IsRetryableException(exception);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region CalculateRetryDelay Tests

    [TestMethod]
    public void CalculateRetryDelay_FirstAttempt_ReturnsBaseDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromSeconds(1);
        const double tolerance = 0.2;

        // Act
        var result = _errorHandlingService.CalculateRetryDelay(1, baseDelay);

        // Assert
        Assert.IsLessThan(Math.Abs(result.TotalSeconds - 1.0), tolerance); // Allow for jitter
    }

    [TestMethod]
    public void CalculateRetryDelay_SecondAttempt_ReturnsDoubleBaseDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromSeconds(1);
        const double tolerance = 0.4;

        // Act
        var result = _errorHandlingService.CalculateRetryDelay(2, baseDelay);

        // Assert
        Assert.IsLessThan(Math.Abs(result.TotalSeconds - 2.0), tolerance); // Allow for jitter
    }

    [TestMethod]
    public void CalculateRetryDelay_ThirdAttempt_ReturnsQuadrupleBaseDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromSeconds(1);
        const double tolerance = 0.8;

        // Act
        var result = _errorHandlingService.CalculateRetryDelay(3, baseDelay);

        // Assert
        Assert.IsLessThan(Math.Abs(result.TotalSeconds - 4.0), tolerance); // Allow for jitter
    }

    [TestMethod]
    public void CalculateRetryDelay_WithJitter_AddsRandomVariation()
    {
        // Arrange
        var baseDelay = TimeSpan.FromSeconds(1);
        var delays = new List<TimeSpan>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var delay = _errorHandlingService.CalculateRetryDelay(2, baseDelay, 0.1);
            delays.Add(delay);
        }

        // Assert
        var distinctDelays = delays.Select(d => d.TotalMilliseconds).Distinct().Count();
        Assert.IsGreaterThan(distinctDelays, 1, "Jitter should create variation in delays");
    }

    [TestMethod]
    public void CalculateRetryDelay_ZeroJitter_ReturnsExactDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromSeconds(2);

        // Act
        var result = _errorHandlingService.CalculateRetryDelay(2, baseDelay, 0);

        // Assert
        Assert.AreEqual(TimeSpan.FromSeconds(4), result);
    }

    [TestMethod]
    public void CalculateRetryDelay_NegativeDelay_ReturnsMinimumDelay()
    {
        // Arrange
        var baseDelay = TimeSpan.FromTicks(1); // Very small base delay

        // Act
        var result = _errorHandlingService.CalculateRetryDelay(10, baseDelay, 1.0);

        // Assert
        Assert.IsTrue(result >= TimeSpan.FromMilliseconds(100));
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ErrorHandlingService(null!));
    }

    [TestMethod]
    public void Constructor_ValidLogger_CreatesInstance()
    {
        // Act
        var service = new ErrorHandlingService(_loggerMock.Object);

        // Assert
        Assert.IsNotNull(service);
    }

    #endregion
}
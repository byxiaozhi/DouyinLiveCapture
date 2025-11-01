using Microsoft.VisualStudio.TestTools.UnitTesting;
using DouyinLiveCapture.Services.Cryptography;
using System.Text;

namespace DouyinLiveCapture.Tests.Services.Cryptography;

/// <summary>
/// 加密算法单元测试
/// </summary>
[TestClass]
public class CryptographyTests
{
    #region SM3 Tests

    [TestMethod]
    public void SM3_ComputeHash_SimpleInput_ReturnsCorrectHash()
    {
        // Arrange
        const string input = "hello world";

        // Act
        var result = SM3.ComputeHash(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length); // 32 bytes = 64 hex chars
        Assert.IsTrue(result.All(c => "0123456789abcdef".Contains(c)));

        // 验证哈希值的一致性
        var result2 = SM3.ComputeHash(input);
        Assert.AreEqual(result, result2);
    }

    [TestMethod]
    public void SM3_ComputeHash_DifferentInputs_ReturnsDifferentHashes()
    {
        // Arrange
        const string input1 = "input1";
        const string input2 = "input2";

        // Act
        var result1 = SM3.ComputeHash(input1);
        var result2 = SM3.ComputeHash(input2);

        // Assert
        Assert.AreNotEqual(result1, result2);
    }

    [TestMethod]
    public void SM3_Instance_UpdateAndCompute_ReturnsCorrectHash()
    {
        // Arrange
        const string input = "hello";
        using var sm3 = new SM3();

        // Act
        sm3.Update(input);
        var result = sm3.ComputeHashString();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);

        // 验证与静态方法结果一致
        var staticResult = SM3.ComputeHash(input);
        Assert.AreEqual(staticResult, result);
    }

    [TestMethod]
    public void SM3_Instance_MultipleUpdates_ReturnsCorrectHash()
    {
        // Arrange
        using var sm3 = new SM3();
        const string part1 = "hello ";
        const string part2 = "world";

        // Act
        sm3.Update(part1);
        sm3.Update(part2);
        var result = sm3.ComputeHashString();

        // Assert
        var staticResult = SM3.ComputeHash("hello world");
        Assert.AreEqual(staticResult, result);
    }

    [TestMethod]
    public void SM3_Instance_ResetAndReuse_WorksCorrectly()
    {
        // Arrange
        using var sm3 = new SM3();
        const string input1 = "first";
        const string input2 = "second";

        // Act
        sm3.Update(input1);
        var result1 = sm3.ComputeHashString();

        sm3.Reset();
        sm3.Update(input2);
        var result2 = sm3.ComputeHashString();

        // Assert
        Assert.AreNotEqual(result1, result2);
        Assert.AreEqual(SM3.ComputeHash(input2), result2);
    }

    [TestMethod]
    public void SM3_EmptyInput_ReturnsValidHash()
    {
        // Arrange
        const string input = "";

        // Act
        var result = SM3.ComputeHash(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(64, result.Length);

        // 验证一致性
        var result2 = SM3.ComputeHash(input);
        Assert.AreEqual(result, result2);
    }

    #endregion

    #region RC4 Tests

    [TestMethod]
    public void RC4_EncryptDecrypt_WithByteArray_RoundTripSucceeds()
    {
        // Arrange
        var key = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
        var plaintext = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };

        // Act
        using var rc4 = new RC4(key);
        var encrypted = rc4.Transform(plaintext);
        var decrypted = rc4.Transform(encrypted);

        // Assert
        CollectionAssert.AreEqual(plaintext, decrypted);
        CollectionAssert.AreNotEqual(plaintext, encrypted);
    }

    [TestMethod]
    public void RC4_EncryptDecrypt_WithString_RoundTripSucceeds()
    {
        // Arrange
        const string key = "secretkey";
        const string plaintext = "Hello World!";

        // Act
        using var rc4 = new RC4(key);
        var encrypted = rc4.TransformString(plaintext);
        var decrypted = rc4.TransformString(encrypted);

        // Assert
        Assert.AreEqual(plaintext, decrypted);
        Assert.AreNotEqual(plaintext, encrypted);
    }

    [TestMethod]
    public void RC4_DifferentKeys_DifferentOutputs()
    {
        // Arrange
        const string plaintext = "test data";
        const string key1 = "key1";
        const string key2 = "key2";

        // Act
        using var rc4_1 = new RC4(key1);
        using var rc4_2 = new RC4(key2);
        var encrypted1 = rc4_1.TransformString(plaintext);
        var encrypted2 = rc4_2.TransformString(plaintext);

        // Assert
        Assert.AreNotEqual(encrypted1, encrypted2);
    }

    [TestMethod]
    public void RC4_EmptyKey_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new RC4(Array.Empty<byte>()));
        Assert.Throws<ArgumentException>(() => new RC4(""));
    }

    [TestMethod]
    public void RC4_NullKey_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RC4((byte[])null!));
        Assert.Throws<ArgumentNullException>(() => new RC4((string)null!));
    }

    [TestMethod]
    public void RC4_MultipleProcessCalls_ContinuesCorrectly()
    {
        // Arrange
        const string key = "testkey";
        const string message1 = "First message";
        const string message2 = "Second message";

        // Act
        using var rc4 = new RC4(key);
        var encrypted1 = rc4.TransformString(message1);
        var encrypted2 = rc4.TransformString(message2);
        var decrypted1 = rc4.TransformString(encrypted1);
        var decrypted2 = rc4.TransformString(encrypted2);

        // Assert
        Assert.AreEqual(message1, decrypted1);
        Assert.AreEqual(message2, decrypted2);
    }

    [TestMethod]
    public void RC4_LargeData_HandlesCorrectly()
    {
        // Arrange
        const string key = "largekey";
        var plaintext = new string('A', 1000);

        // Act
        using var rc4 = new RC4(key);
        var encrypted = rc4.TransformString(plaintext);
        var decrypted = rc4.TransformString(encrypted);

        // Assert
        Assert.AreEqual(plaintext.Length, decrypted.Length);
        Assert.AreEqual(plaintext, decrypted);
        Assert.AreNotEqual(plaintext, encrypted);
    }

    #endregion

    #region CustomBase64 Tests

    [TestMethod]
    public void CustomBase64_Encode_ValidInput_ReturnsEncodedString()
    {
        // Arrange
        const string input = "Hello World";

        // Act
        var result = CustomBase64.Encode(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > input.Length);
        Assert.AreNotEqual(input, result);
    }

    [TestMethod]
    public void CustomBase64_Encode_DifferentTables_ReturnsDifferentResults()
    {
        // Arrange
        const string input = "test";

        // Act
        var result_s1 = CustomBase64.Encode(input, "s1");
        var result_s4 = CustomBase64.Encode(input, "s4");

        // Assert
        Assert.AreNotEqual(result_s1, result_s4);
    }

    [TestMethod]
    public void CustomBase64_Encode_EmptyInput_ReturnsEmptyString()
    {
        // Arrange
        const string input = "";

        // Act
        var result = CustomBase64.Encode(input);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void CustomBase64_Encode_NullInput_ReturnsEmptyString()
    {
        // Act
        var result = CustomBase64.Encode((string)null!);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void CustomBase64_ResultEncrypt_SameInput_ReturnsEncodedString()
    {
        // Arrange
        const string input = "Test message for encoding";

        // Act
        var encoded = CustomBase64.ResultEncrypt(input);

        // Assert
        Assert.IsNotNull(encoded);
        Assert.AreNotEqual(input, encoded);
    }

    [TestMethod]
    public void CustomBase64_ResultEncrypt_WithSpecificTable_WorksCorrectly()
    {
        // Arrange
        const string input = "Test data";
        const string tableKey = "s1";

        // Act
        var encoded = CustomBase64.ResultEncrypt(input, tableKey);

        // Assert
        Assert.IsNotNull(encoded);
        Assert.AreNotEqual(input, encoded);
    }

    [TestMethod]
    public void CustomBase64_Encode_LongInput_HandlesCorrectly()
    {
        // Arrange
        var longInput = new string('A', 1000);

        // Act
        var result = CustomBase64.Encode(longInput);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length > 0);
    }

    [TestMethod]
    public void CustomBase64_Encode_WithUnicode_HandlesCorrectly()
    {
        // Arrange
        const string input = "测试数据 🎉";

        // Act
        var result = CustomBase64.Encode(input);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreNotEqual(input, result);
    }

    [TestMethod]
    public void CustomBase64_Encode_InvalidTable_ThrowsException()
    {
        // Arrange
        const string input = "test";
        const string invalidTable = "invalid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CustomBase64.Encode((string)input, invalidTable));
    }

    [TestMethod]
    public void CustomBase64_ResultEncrypt_InvalidTable_ThrowsException()
    {
        // Arrange
        const string input = "test";
        const string invalidTable = "invalid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CustomBase64.ResultEncrypt(input, invalidTable));
    }

    [TestMethod]
    public void CustomBase64_GetAvailableTables_ReturnsTableKeys()
    {
        // Act
        var tables = CustomBase64.GetAvailableTables().ToList();

        // Assert
        Assert.IsTrue(tables.Count > 0);
        Assert.IsTrue(tables.Contains("s4"));
        Assert.IsTrue(tables.Contains("s1"));
    }

    [TestMethod]
    public void CustomBase64_GetEncodingTable_ValidKey_ReturnsTable()
    {
        // Act
        var table = CustomBase64.GetEncodingTable("s4");

        // Assert
        Assert.IsNotNull(table);
        Assert.IsTrue(table.Length > 0);
    }

    [TestMethod]
    public void CustomBase64_GetEncodingTable_InvalidKey_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => CustomBase64.GetEncodingTable("invalid"));
    }

    #endregion

    #region Cross-Algorithm Tests

    [TestMethod]
    public void CrossAlgorithm_SM3ThenBase64_CompositeOperation()
    {
        // Arrange
        const string input = "test message";

        // Act
        var hash = SM3.ComputeHash(input);
        var encoded = CustomBase64.Encode(hash);

        // Assert
        Assert.IsNotNull(hash);
        Assert.IsNotNull(encoded);
        Assert.AreNotEqual(hash, encoded);
    }

    [TestMethod]
    public void CrossAlgorithm_RC4ThenBase64_CompositeOperation()
    {
        // Arrange
        const string key = "encryptionkey";
        const string input = "secret message";

        // Act
        using var rc4 = new RC4(key);
        var encrypted = rc4.TransformString(input);
        var encoded = CustomBase64.Encode(encrypted);

        // Assert
        Assert.IsNotNull(encoded);
        Assert.AreNotEqual(input, encoded);
    }

    [TestMethod]
    public void CrossAlgorithm_Deterministic_SameInputProducesSameOutput()
    {
        // Arrange
        const string input = "deterministic test";

        // Act
        var hash1 = SM3.ComputeHash(input);
        var hash2 = SM3.ComputeHash(input);
        var encoded1 = CustomBase64.Encode(hash1);
        var encoded2 = CustomBase64.Encode(hash2);

        // Assert
        Assert.AreEqual(hash1, hash2);
        Assert.AreEqual(encoded1, encoded2);
    }

    #endregion

    #region Performance and Edge Cases

    [TestMethod]
    public void Performance_MultipleSM3Computations_CompletesQuickly()
    {
        // Arrange
        const string input = "performance test";
        const int iterations = 100;

        // Act
        var results = new List<string>();
        for (int i = 0; i < iterations; i++)
        {
            results.Add(SM3.ComputeHash(input + i));
        }

        // Assert
        Assert.AreEqual(iterations, results.Count);

        // Verify all hashes are unique
        var uniqueHashes = results.Distinct().Count();
        Assert.AreEqual(iterations, uniqueHashes);
    }

    [TestMethod]
    public void EdgeCase_VeryLongInput_HandlesCorrectly()
    {
        // Arrange
        var longInput = new string('x', 10000);

        // Act
        var hash = SM3.ComputeHash(longInput);

        // Assert
        Assert.IsNotNull(hash);
        Assert.AreEqual(64, hash.Length);
    }

    [TestMethod]
    public void EdgeCase_WhitespaceInput_HandlesCorrectly()
    {
        // Arrange
        const string input = "   \t\n\r   ";

        // Act
        var hash = SM3.ComputeHash(input);

        // Assert
        Assert.IsNotNull(hash);
        Assert.AreEqual(64, hash.Length);
        Assert.AreNotEqual(hash, SM3.ComputeHash("different"));
    }

    #endregion
}
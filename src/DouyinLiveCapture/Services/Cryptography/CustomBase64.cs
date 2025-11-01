using System.Text;

namespace DouyinLiveCapture.Services.Cryptography;

/// <summary>
/// 自定义Base64编码实现
/// 支持多种编码表，用于抖音AB签名算法
/// </summary>
public static class CustomBase64
{
    // 标准Base64编码表
    private const string StandardTable = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

    // 抖音AB签名使用的编码表
    private static readonly Dictionary<string, string> EncodingTables = new()
    {
        { "s0", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=" },
        { "s1", "Dkdpgh4ZKsQB80/Mfvw36XI1R25+WUAlEi7NLboqYTOPuzmFjJnryx9HVGcaStCe=" },
        { "s2", "Dkdpgh4ZKsQB80/Mfvw36XI1R25-WUAlEi7NLboqYTOPuzmFjJnryx9HVGcaStCe=" },
        { "s3", "ckdp1h4ZKsUB80/Mfvw36XIgR25+WQAlEi7NLboqYTOPuzmFjJnryx9HVGDaStCe" },
        { "s4", "Dkdpgh2ZmsQB80/MfvV36XI1R45-WUAlEixNLwoqYTOPuzKFjJnry79HbGcaStCe" }
    };

    // 位移常量掩码
    private static readonly int[] Masks = { 0x00FC0000, 0x0003F000, 0x00000FC0, 0x0000003F };
    // 位移量
    private static readonly int[] Shifts = { 18, 12, 6, 0 };

    /// <summary>
    /// 使用指定的编码表进行编码
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="tableKey">编码表键名</param>
    /// <returns>编码后的字符串</returns>
    public static string Encode(string input, string tableKey = "s4")
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        if (!EncodingTables.TryGetValue(tableKey, out var encodingTable))
            throw new ArgumentException($"Unknown encoding table: {tableKey}");

        var inputBytes = Encoding.UTF8.GetBytes(input);
        return EncodeInternal(inputBytes, encodingTable);
    }

    /// <summary>
    /// 使用指定的编码表进行编码
    /// </summary>
    /// <param name="input">输入字节数组</param>
    /// <param name="tableKey">编码表键名</param>
    /// <returns>编码后的字符串</returns>
    public static string Encode(byte[] input, string tableKey = "s4")
    {
        if (input == null || input.Length == 0)
            return string.Empty;

        if (!EncodingTables.TryGetValue(tableKey, out var encodingTable))
            throw new ArgumentException($"Unknown encoding table: {tableKey}");

        return EncodeInternal(input, encodingTable);
    }

    /// <summary>
    /// 使用标准Base64编码表进行编码
    /// </summary>
    /// <param name="input">输入字节数组</param>
    /// <returns>编码后的字符串</returns>
    public static string EncodeStandard(byte[] input)
    {
        return Convert.ToBase64String(input);
    }

    /// <summary>
    /// 使用标准Base64编码表进行编码
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>编码后的字符串</returns>
    public static string EncodeStandard(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var inputBytes = Encoding.UTF8.GetBytes(input);
        return EncodeStandard(inputBytes);
    }

    /// <summary>
    /// 抖音AB签名特定的编码方法
    /// 模拟Python版本的行为
    /// </summary>
    /// <param name="longStr">输入字符串</param>
    /// <param name="tableKey">编码表键名</param>
    /// <returns>编码后的字符串</returns>
    public static string ResultEncrypt(string longStr, string tableKey = "s4")
    {
        if (string.IsNullOrEmpty(longStr))
            return string.Empty;

        if (!EncodingTables.TryGetValue(tableKey, out var encodingTable))
            throw new ArgumentException($"Unknown encoding table: {tableKey}");

        var result = new StringBuilder();
        int roundNum = 0;
        long longInt = GetLongInt(roundNum, longStr);

        int totalChars = (int)Math.Ceiling(longStr.Length / 3.0 * 4);

        for (int i = 0; i < totalChars; i++)
        {
            // 每4个字符处理一组3字节
            if (i / 4 != roundNum)
            {
                roundNum++;
                longInt = GetLongInt(roundNum, longStr);
            }

            // 计算当前位置的索引
            int index = i % 4;

            // 使用掩码和位移提取6位值
            int charIndex = (int)((longInt & Masks[index]) >> Shifts[index]);

            result.Append(encodingTable[charIndex]);
        }

        return result.ToString();
    }

    /// <summary>
    /// 获取3字节对应的整数值
    /// </summary>
    /// <param name="roundNum">轮次</param>
    /// <param name="longStr">输入字符串</param>
    /// <returns>整数值</returns>
    private static long GetLongInt(int roundNum, string longStr)
    {
        int startIndex = roundNum * 3;

        // 获取字符串中的字符，如果超出范围则使用0
        byte char1 = startIndex < longStr.Length ? (byte)longStr[startIndex] : (byte)0;
        byte char2 = startIndex + 1 < longStr.Length ? (byte)longStr[startIndex + 1] : (byte)0;
        byte char3 = startIndex + 2 < longStr.Length ? (byte)longStr[startIndex + 2] : (byte)0;

        return (char1 << 16) | (char2 << 8) | char3;
    }

    /// <summary>
    /// 使用指定的编码表进行编码（内部方法）
    /// </summary>
    /// <param name="input">输入字节数组</param>
    /// <param name="encodingTable">编码表</param>
    /// <returns>编码后的字符串</returns>
    private static string EncodeInternal(byte[] input, string encodingTable)
    {
        var result = new StringBuilder();

        // 处理完整的3字节块
        for (int i = 0; i <= input.Length - 3; i += 3)
        {
            uint block = (uint)((input[i] << 16) | (input[i + 1] << 8) | input[i + 2]);

            result.Append(encodingTable[(int)((block >> 18) & 0x3F)]);
            result.Append(encodingTable[(int)((block >> 12) & 0x3F)]);
            result.Append(encodingTable[(int)((block >> 6) & 0x3F)]);
            result.Append(encodingTable[(int)(block & 0x3F)]);
        }

        // 处理剩余的字节
        int remaining = input.Length % 3;
        if (remaining > 0)
        {
            int offset = input.Length - remaining;
            uint block = 0;

            if (remaining == 1)
            {
                block = (uint)(input[offset] << 16);
                result.Append(encodingTable[(int)((block >> 18) & 0x3F)]);
                result.Append(encodingTable[(int)((block >> 12) & 0x3F)]);
                result.Append('=');
                result.Append('=');
            }
            else if (remaining == 2)
            {
                block = (uint)((input[offset] << 16) | (input[offset + 1] << 8));
                result.Append(encodingTable[(int)((block >> 18) & 0x3F)]);
                result.Append(encodingTable[(int)((block >> 12) & 0x3F)]);
                result.Append(encodingTable[(int)((block >> 6) & 0x3F)]);
                result.Append('=');
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 获取可用的编码表列表
    /// </summary>
    /// <returns>编码表键名列表</returns>
    public static IEnumerable<string> GetAvailableTables()
    {
        return EncodingTables.Keys;
    }

    /// <summary>
    /// 获取指定编码表的内容
    /// </summary>
    /// <param name="tableKey">编码表键名</param>
    /// <returns>编码表内容</returns>
    public static string GetEncodingTable(string tableKey)
    {
        if (EncodingTables.TryGetValue(tableKey, out var encodingTable))
            return encodingTable;

        throw new ArgumentException($"Unknown encoding table: {tableKey}");
    }
}
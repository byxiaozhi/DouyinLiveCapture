using System.Text;
using DouyinLiveCapture.Services.Cryptography;

namespace DouyinLiveCapture.Services.Signature;

/// <summary>
/// 抖音AB签名服务
/// 实现抖音平台所需的AB签名算法
/// </summary>
public class DouyinAbSignatureService : IDouyinAbSignatureService, IDisposable
{
    private const string DefaultWindowEnv = "1920|1080|1920|1040|0|30|0|0|1872|92|1920|1040|1857|92|1|24|Win32";

    /// <summary>
    /// 生成AB签名
    /// </summary>
    /// <param name="urlSearchParams">URL查询参数</param>
    /// <param name="userAgent">用户代理字符串</param>
    /// <returns>AB签名</returns>
    public string GenerateSignature(string urlSearchParams, string userAgent)
    {
        if (string.IsNullOrEmpty(urlSearchParams))
            throw new ArgumentException("URL search parameters cannot be null or empty", nameof(urlSearchParams));

        if (string.IsNullOrEmpty(userAgent))
            throw new ArgumentException("User agent cannot be null or empty", nameof(userAgent));

        // 1. 生成随机字符串前缀
        string randomPrefix = GenerateRandomString();

        // 2. 生成RC4加密的主体部分
        string rc4Encrypted = GenerateRc4EncryptedData(urlSearchParams, userAgent, DefaultWindowEnv);

        // 3. 对结果进行最终编码并添加等号后缀
        string encoded = CustomBase64.ResultEncrypt(randomPrefix + rc4Encrypted, "s4");

        return encoded + "=";
    }

    /// <summary>
    /// 生成RC4加密的主体部分
    /// </summary>
    /// <param name="urlSearchParams">URL查询参数</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="windowEnv">窗口环境信息</param>
    /// <returns>加密后的数据</returns>
    private string GenerateRc4EncryptedData(string urlSearchParams, string userAgent, string windowEnv)
    {
          long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 三次加密处理
        // 1: url_search_params两次sm3的结果
        byte[] urlParamsHash1 = SM3.ComputeHash(Encoding.UTF8.GetBytes(urlSearchParams));
        byte[] urlParamsHash2 = SM3.ComputeHash(urlParamsHash1);

        // 2: 对后缀"cus"两次sm3的结果
        byte[] suffixHash1 = SM3.ComputeHash(Encoding.UTF8.GetBytes("cus"));
        byte[] suffixHash2 = SM3.ComputeHash(suffixHash1);

        // 3: 对ua处理之后的结果
        string uaKey = "\u0000\u0001\u000E"; // [1/256, 1, 14]
        string uaRc4 = RC4.Encrypt(userAgent, uaKey);
        string uaBase64 = CustomBase64.ResultEncrypt(uaRc4, "s3");
        byte[] uaHash = SM3.ComputeHash(Encoding.UTF8.GetBytes(uaBase64));

        long endTime = startTime + 100;

        // 构建配置数据
        var configData = new byte[73];
        PopulateConfigData(configData, startTime, endTime, urlParamsHash2, suffixHash2, uaHash, windowEnv);

        // 构建最终字节数组
        var finalData = new List<byte>();

        // 添加固定顺序的数据
        finalData.AddRange(new byte[] {
            configData[18], configData[20], configData[52], configData[26],
            configData[30], configData[34], configData[58], configData[38],
            configData[40], configData[53], configData[42], configData[21],
            configData[27], configData[54], configData[55], configData[31],
            configData[35], configData[57], configData[39], configData[41],
            configData[43], configData[22], configData[28], configData[32],
            configData[60], configData[36], configData[23], configData[29],
            configData[33], configData[37], configData[44], configData[45],
            configData[59], configData[46], configData[47], configData[48],
            configData[49], configData[50], configData[24], configData[25],
            configData[65], configData[66], configData[70], configData[71]
        });

        // 添加窗口环境信息
        finalData.AddRange(Encoding.UTF8.GetBytes(windowEnv));

        // 添加校验和
        finalData.Add(configData[72]);

        // 使用RC4加密最终数据
        byte[] finalBytes = finalData.ToArray();
        var rc4 = new RC4("y"); // 固定密钥"y"
        return Encoding.UTF8.GetString(rc4.Transform(finalBytes));
    }

    /// <summary>
    /// 填充配置数据
    /// </summary>
    /// <param name="data">数据数组</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="urlParamsHash">URL参数哈希</param>
    /// <param name="suffixHash">后缀哈希</param>
    /// <param name="uaHash">用户代理哈希</param>
    /// <param name="windowEnv">窗口环境</param>
    private static void PopulateConfigData(byte[] data, long startTime, long endTime,
        byte[] urlParamsHash, byte[] suffixHash, byte[] uaHash, string windowEnv)
    {
        // 基础配置
        data[8] = 3;
        data[10] = (byte)(endTime & 0xFF);
        data[11] = (byte)((endTime >> 8) & 0xFF);
        data[12] = (byte)((endTime >> 16) & 0xFF);
        data[13] = (byte)((endTime >> 24) & 0xFF);
        data[15] = 0; // 固定值0，对应pageId的一部分

        data[16] = (byte)(startTime & 0xFF);
        data[17] = (byte)((startTime >> 8) & 0xFF);
        data[18] = (byte)((startTime >> 16) & 0xFF);
        data[19] = (byte)((startTime >> 24) & 0xFF);
        data[24] = (byte)((startTime >> 32) & 0xFF);
        data[25] = (byte)((startTime >> 40) & 0xFF);

        data[26] = 0; // arguments[0] 高字节
        data[27] = 0; // arguments[0] 次高字节
        data[28] = 0; // arguments[0] 次低字节
        data[29] = 0; // arguments[0] 低字节

        data[30] = 0; // arguments[1] 高字节
        data[31] = 1; // arguments[1] 低字节

        data[32] = 0; // arguments[1] 高字节
        data[33] = 1; // arguments[1] 低字节

        data[34] = 0; // arguments[2] 高字节
        data[35] = 14; // arguments[2] 低字节
        data[36] = 0; // arguments[2] 次高字节
        data[37] = 0; // arguments[2] 次低字节

        // 处理加密结果
        data[38] = urlParamsHash[21];
        data[39] = urlParamsHash[22];
        data[40] = suffixHash[21];
        data[41] = suffixHash[22];
        data[42] = uaHash[23];
        data[43] = uaHash[24];

        // 处理结束时间
        data[44] = data[10];
        data[45] = data[11];
        data[46] = data[12];
        data[47] = data[13];
        data[48] = data[8];
        data[49] = (byte)((endTime >> 32) & 0xFF);
        data[50] = (byte)((endTime >> 40) & 0xFF);

        // 处理页面ID
        data[51] = data[15]; // pageId
        data[52] = data[15]; // pageId 高字节
        data[53] = (byte)((110624 >> 8) & 0xFF); // pageId 次高字节
        data[54] = (byte)((110624 >> 16) & 0xFF); // pageId 次低字节
        data[55] = (byte)((110624 >> 24) & 0xFF); // pageId 低字节

        // 处理aid
        data[56] = 239; // 6383 & 255
        data[57] = (byte)((6383 >> 8) & 0xFF);
        data[58] = (byte)((6383 >> 16) & 0xFF);
        data[59] = (byte)((6383 >> 24) & 0xFF);

        // 处理环境信息长度
        byte[] windowEnvBytes = Encoding.UTF8.GetBytes(windowEnv);
        data[64] = (byte)(windowEnvBytes.Length & 0xFF);
        data[65] = (byte)((windowEnvBytes.Length >> 8) & 0xFF);
        data[66] = data[65];

        data[69] = 0;
        data[70] = 0;
        data[71] = 0;

        // 计算校验和
        data[72] = (byte)(data[18] ^ data[20] ^ data[26] ^ data[30] ^ data[38] ^ data[40] ^ data[42] ^
                       data[21] ^ data[27] ^ data[31] ^ data[35] ^ data[39] ^ data[41] ^ data[43] ^
                       data[22] ^ data[28] ^ data[32] ^ data[36] ^ data[23] ^ data[29] ^ data[33] ^
                       data[37] ^ data[44] ^ data[45] ^ data[46] ^ data[47] ^ data[48] ^ data[49] ^
                       data[50] ^ data[24] ^ data[25] ^ data[52] ^ data[53] ^ data[54] ^ data[55] ^
                       data[57] ^ data[58] ^ data[59] ^ data[60] ^ data[65] ^ data[66] ^ data[70] ^ data[71]);
    }

    /// <summary>
    /// 生成随机字符串
    /// </summary>
    /// <returns>随机字符串</returns>
    private static string GenerateRandomString()
    {
        // 使用固定的随机值以确保一致性（与Python版本保持一致）
        var randomValues = new double[] { 0.123456789, 0.987654321, 0.555555555 };

        var randomBytes = new List<byte>();

        // 生成三组随机字节
        randomBytes.AddRange(GenerateRandomBytes((int)(randomValues[0] * 10000), new byte[] { 3, 45 }));
        randomBytes.AddRange(GenerateRandomBytes((int)(randomValues[1] * 10000), new byte[] { 1, 0 }));
        randomBytes.AddRange(GenerateRandomBytes((int)(randomValues[2] * 10000), new byte[] { 1, 5 }));

        return Encoding.UTF8.GetString(randomBytes.ToArray());
    }

    /// <summary>
    /// 生成随机字节
    /// </summary>
    /// <param name="randomNum">随机数</param>
    /// <param name="option">选项数组</param>
    /// <returns>随机字节数组</returns>
    private static byte[] GenerateRandomBytes(int randomNum, byte[] option)
    {
        var result = new byte[4];

        byte byte1 = (byte)(randomNum & 0xFF);
        byte byte2 = (byte)((randomNum >> 8) & 0xFF);

        result[0] = (byte)((byte1 & 170) | (option[0] & 85));   // 偶数位与option[0]的奇数位合并
        result[1] = (byte)((byte1 & 85) | (option[0] & 170));    // 奇数位与option[0]的偶数位合并
        result[2] = (byte)((byte2 & 170) | (option[1] & 85));   // 偶数位与option[1]的奇数位合并
        result[3] = (byte)((byte2 & 85) | (option[1] & 170));    // 奇数位与option[1]的偶数位合并

        return result;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 当前实现中没有需要释放的非托管资源
    }
}
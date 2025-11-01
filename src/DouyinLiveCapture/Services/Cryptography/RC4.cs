using System.Text;

namespace DouyinLiveCapture.Services.Cryptography;

/// <summary>
/// RC4加密算法实现
/// </summary>
public class RC4 : IDisposable
{
    private byte[] _s = new byte[256];
    private byte _i;
    private byte _j;

    /// <summary>
    /// 初始化RC4实例
    /// </summary>
    /// <param name="key">加密密钥</param>
    public RC4(byte[] key)
    {
        Initialize(key);
    }

    /// <summary>
    /// 初始化RC4实例（字符串密钥）
    /// </summary>
    /// <param name="key">加密密钥</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    public RC4(string key, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        Initialize(encoding.GetBytes(key));
    }

    /// <summary>
    /// 初始化状态数组
    /// </summary>
    /// <param name="key">加密密钥</param>
    private void Initialize(byte[] key)
    {
        if (key == null || key.Length == 0)
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        // 初始化状态数组
        for (int i = 0; i < 256; i++)
        {
            _s[i] = (byte)i;
        }

        // 使用密钥对状态数组进行置换
        byte j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (byte)((j + _s[i] + key[i % key.Length]) % 256);
            Swap(_s, i, j);
        }

        _i = 0;
        _j = 0;
    }

    /// <summary>
    /// 加密或解密数据
    /// </summary>
    /// <param name="data">要处理的数据</param>
    /// <returns>处理后的数据</returns>
    public byte[] Transform(byte[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[data.Length];
        for (int k = 0; k < data.Length; k++)
        {
            result[k] = NextByte(data[k]);
        }
        return result;
    }

    /// <summary>
    /// 加密或解密字符串
    /// </summary>
    /// <param name="text">要处理的文本</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    /// <returns>处理后的文本</returns>
    public string TransformString(string text, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        byte[] data = encoding.GetBytes(text);
        byte[] result = Transform(data);
        return encoding.GetString(result);
    }

    /// <summary>
    /// 获取下一个密钥流字节并处理输入字节
    /// </summary>
    /// <param name="inputByte">输入字节</param>
    /// <returns>输出字节</returns>
    private byte NextByte(byte inputByte)
    {
        _i = (byte)((_i + 1) % 256);
        _j = (byte)((_j + _s[_i]) % 256);
        Swap(_s, _i, _j);
        byte t = (byte)((_s[_i] + _s[_j]) % 256);
        return (byte)(_s[t] ^ inputByte);
    }

    /// <summary>
    /// 交换状态数组中的两个元素
    /// </summary>
    /// <param name="s">状态数组</param>
    /// <param name="i">索引1</param>
    /// <param name="j">索引2</param>
    private static void Swap(byte[] s, int i, int j)
    {
        (s[i], s[j]) = (s[j], s[i]);
    }

    /// <summary>
    /// 重置RC4状态
    /// </summary>
    /// <param name="key">新的密钥</param>
    public void Reset(byte[] key)
    {
        Initialize(key);
    }

    /// <summary>
    /// 重置RC4状态（字符串密钥）
    /// </summary>
    /// <param name="key">新的密钥</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    public void Reset(string key, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        Initialize(encoding.GetBytes(key));
    }

    /// <summary>
    /// 静态方法：加密数据
    /// </summary>
    /// <param name="data">要加密的数据</param>
    /// <param name="key">加密密钥</param>
    /// <returns>加密后的数据</returns>
    public static byte[] Encrypt(byte[] data, byte[] key)
    {
        using var rc4 = new RC4(key);
        return rc4.Transform(data);
    }

    /// <summary>
    /// 静态方法：加密字符串
    /// </summary>
    /// <param name="text">要加密的文本</param>
    /// <param name="key">加密密钥</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    /// <returns>加密后的文本</returns>
    public static string Encrypt(string text, string key, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        using var rc4 = new RC4(key, encoding);
        return rc4.TransformString(text, encoding);
    }

    /// <summary>
    /// 静态方法：解密数据
    /// </summary>
    /// <param name="data">要解密的数据</param>
    /// <param name="key">解密密钥</param>
    /// <returns>解密后的数据</returns>
    public static byte[] Decrypt(byte[] data, byte[] key)
    {
        // RC4加密和解密是相同的操作
        return Encrypt(data, key);
    }

    /// <summary>
    /// 静态方法：解密字符串
    /// </summary>
    /// <param name="text">要解密的文本</param>
    /// <param name="key">解密密钥</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    /// <returns>解密后的文本</returns>
    public static string Decrypt(string text, string key, Encoding? encoding = null)
    {
        // RC4加密和解密是相同的操作
        return Encrypt(text, key, encoding);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        // 清理敏感数据
        Array.Clear(_s, 0, _s.Length);
        _i = 0;
        _j = 0;
    }
}
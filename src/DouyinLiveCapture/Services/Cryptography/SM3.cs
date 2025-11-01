using System.Buffers.Binary;
using System.Text;

namespace DouyinLiveCapture.Services.Cryptography;

/// <summary>
/// SM3国密哈希算法实现
/// </summary>
public class SM3 : IDisposable
{
    private uint[] _iv = new uint[8];
    private byte[] _buffer = new byte[64];
    private int _bufferPos = 0;
    private ulong _totalLength = 0;

    /// <summary>
    /// 初始化SM3实例
    /// </summary>
    public SM3()
    {
        Reset();
    }

    /// <summary>
    /// 重置SM3状态
    /// </summary>
    public void Reset()
    {
        // SM3初始向量
        _iv[0] = 0x7380166F;
        _iv[1] = 0x4914B2B9;
        _iv[2] = 0x172442D7;
        _iv[3] = 0xDA8A0600;
        _iv[4] = 0xA96F30BC;
        _iv[5] = 0x163138AA;
        _iv[6] = 0xE38DEE4D;
        _iv[7] = 0xB0FB0E4E;

        _bufferPos = 0;
        _totalLength = 0;
    }

    /// <summary>
    /// 更新哈希值
    /// </summary>
    /// <param name="data">输入数据</param>
    public void Update(byte[] data)
    {
        if (data == null || data.Length == 0)
            return;

        _totalLength += (ulong)data.Length;

        int offset = 0;
        int remaining = data.Length;

        // 填充缓冲区中的剩余空间
        if (_bufferPos > 0)
        {
            int copyLength = Math.Min(64 - _bufferPos, remaining);
            Array.Copy(data, offset, _buffer, _bufferPos, copyLength);
            _bufferPos += copyLength;
            offset += copyLength;
            remaining -= copyLength;

            if (_bufferPos == 64)
            {
                ProcessBlock(_buffer);
                _bufferPos = 0;
            }
        }

        // 处理完整的块
        while (remaining >= 64)
        {
            Array.Copy(data, offset, _buffer, 0, 64);
            ProcessBlock(_buffer);
            offset += 64;
            remaining -= 64;
        }

        // 保存剩余数据
        if (remaining > 0)
        {
            Array.Copy(data, offset, _buffer, 0, remaining);
            _bufferPos = remaining;
        }
    }

    /// <summary>
    /// 更新哈希值（字符串版本）
    /// </summary>
    /// <param name="data">输入字符串</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    public void Update(string data, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        Update(encoding.GetBytes(data));
    }

    /// <summary>
    /// 计算最终哈希值
    /// </summary>
    /// <returns>哈希值字节数组</returns>
    public byte[] ComputeHash()
    {
        // 执行填充
        byte[] padding = CreatePadding(_totalLength);
        Update(padding);

        // 将uint数组转换为字节数组
        byte[] result = new byte[32];
        for (int i = 0; i < 8; i++)
        {
            byte[] bytes = BitConverter.GetBytes(_iv[i]);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            Array.Copy(bytes, 0, result, i * 4, 4);
        }

        return result;
    }

    /// <summary>
    /// 计算哈希值（十六进制字符串）
    /// </summary>
    /// <returns>十六进制哈希值</returns>
    public string ComputeHashString()
    {
        byte[] hash = ComputeHash();
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 静态方法：计算数据的哈希值
    /// </summary>
    /// <param name="data">输入数据</param>
    /// <returns>哈希值字节数组</returns>
    public static byte[] ComputeHash(byte[] data)
    {
        using var sm3 = new SM3();
        sm3.Update(data);
        return sm3.ComputeHash();
    }

    /// <summary>
    /// 静态方法：计算字符串的哈希值
    /// </summary>
    /// <param name="data">输入字符串</param>
    /// <param name="encoding">字符编码，默认UTF-8</param>
    /// <returns>十六进制哈希值</returns>
    public static string ComputeHash(string data, Encoding? encoding = null)
    {
        using var sm3 = new SM3();
        sm3.Update(data, encoding);
        return sm3.ComputeHashString();
    }

    /// <summary>
    /// 处理一个数据块
    /// </summary>
    /// <param name="block">64字节的数据块</param>
    private void ProcessBlock(byte[] block)
    {
        // 将字节转换为字（大端序）
        uint[] w = new uint[132];

        for (int i = 0; i < 16; i++)
        {
            w[i] = BitConverter.ToUInt32(block, i * 4);
            if (BitConverter.IsLittleEndian)
                w[i] = BinaryPrimitives.ReverseEndianness(w[i]);
        }

        // 消息扩展
        for (int j = 16; j < 68; j++)
        {
            uint aValue = w[j - 16] ^ w[j - 9] ^ RotateLeft(w[j - 3], 15);
            aValue = aValue ^ RotateLeft(aValue, 15) ^ RotateLeft(aValue, 23);
            w[j] = aValue ^ RotateLeft(w[j - 13], 7) ^ w[j - 6];
        }

        // 计算 w'
        for (int j = 0; j < 64; j++)
        {
            w[j + 68] = w[j] ^ w[j + 4];
        }

        // 压缩函数
        uint a = _iv[0], b = _iv[1], c = _iv[2], d = _iv[3];
        uint e = _iv[4], f = _iv[5], g = _iv[6], h = _iv[7];

        for (int j = 0; j < 64; j++)
        {
            uint ss1 = RotateLeft((RotateLeft(a, 12) + e + RotateLeft(GetTj(j), j)), 7);
            uint ss2 = ss1 ^ RotateLeft(a, 12);
            uint tt1 = (FF(j, a, b, c) + d + ss2 + w[j + 68]) & 0xFFFFFFFF;
            uint tt2 = (GG(j, e, f, g) + h + ss1 + w[j]) & 0xFFFFFFFF;

            d = c;
            c = RotateLeft(b, 9);
            b = a;
            a = tt1;
            h = g;
            g = RotateLeft(f, 19);
            f = e;
            e = (tt2 ^ RotateLeft(tt2, 9) ^ RotateLeft(tt2, 17)) & 0xFFFFFFFF;
        }

        // 更新哈希值
        _iv[0] ^= a;
        _iv[1] ^= b;
        _iv[2] ^= c;
        _iv[3] ^= d;
        _iv[4] ^= e;
        _iv[5] ^= f;
        _iv[6] ^= g;
        _iv[7] ^= h;
    }

    /// <summary>
    /// 创建填充数据
    /// </summary>
    /// <param name="length">原始数据长度</param>
    /// <returns>填充数据</returns>
    private byte[] CreatePadding(ulong length)
    {
        byte[] padding = new byte[128]; // 最大可能的填充长度
        int paddingLength = 0;

        // 添加 1 bit
        padding[paddingLength++] = 0x80;

        // 计算需要添加的 0 的数量
        int bufferPos = _bufferPos;
        int zeroPadding = (bufferPos <= 56) ? (56 - bufferPos - 1) : (120 - bufferPos - 1);

        // 添加 0 填充
        for (int i = 0; i < zeroPadding; i++)
        {
            padding[paddingLength++] = 0;
        }

        // 添加长度（64位大端序）
        ulong bitLength = length * 8;
        byte[] lengthBytes = BitConverter.GetBytes(bitLength);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lengthBytes);

        Array.Copy(lengthBytes, 0, padding, paddingLength, 8);
        paddingLength += 8;

        Array.Resize(ref padding, paddingLength);
        return padding;
    }

    /// <summary>
    /// 左旋转操作
    /// </summary>
    /// <param name="x">要旋转的值</param>
    /// <param name="n">旋转位数</param>
    /// <returns>旋转后的值</returns>
    private static uint RotateLeft(uint x, int n)
    {
        n %= 32;
        return (x << n) | (x >> (32 - n));
    }

    /// <summary>
    /// 获取常量 Tj
    /// </summary>
    /// <param name="j">轮数</param>
    /// <returns>常量值</returns>
    private static uint GetTj(int j)
    {
        return (j < 16) ? 0x79CC4519u : 0x7A879D8Au;
    }

    /// <summary>
    /// 布尔函数 FF
    /// </summary>
    private static uint FF(int j, uint x, uint y, uint z)
    {
        if (j < 16)
            return x ^ y ^ z;
        else
            return (x & y) | (x & z) | (y & z);
    }

    /// <summary>
    /// 布尔函数 GG
    /// </summary>
    private static uint GG(int j, uint x, uint y, uint z)
    {
        if (j < 16)
            return x ^ y ^ z;
        else
            return (x & y) | (~x & z);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Reset();
    }
}
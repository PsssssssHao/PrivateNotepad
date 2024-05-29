using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PrivateNotepad.Extensions;

/// <summary>
/// String 拓展方法
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// 计算指定字符串的MD5
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <returns>MD5</returns>
    public static string ComputeHashToHex(this string sourceString, bool toLower = false)
    {
        if (string.IsNullOrEmpty(sourceString))
        {
            return string.Empty;
        }
        using var md5 = MD5.Create();
        byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(sourceString));
        var result = Convert.ToHexString(data);
        return toLower ? result.ToLower() : result;
    }

    /// <summary>
    /// Base64编码
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <returns>编码后内容</returns>
    public static string Base64Encode(this string sourceString)
    {
        var result = string.Empty;
        if (string.IsNullOrEmpty(sourceString))
        {
            return result;
        }
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(sourceString);
            result = Convert.ToBase64String(bytes);
            result = result.Split('=')[0];      // Remove any trailing '='s
            result = result.Replace('+', '-');  // 62nd char of encoding
            result = result.Replace('/', '_');  // 63rd char of encoding
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Base64解码
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <returns>解码后内容</returns>
    public static string Base64Decode(this string sourceString)
    {
        var result = string.Empty;
        if (string.IsNullOrEmpty(sourceString))
        {
            return result;
        }
        // 還原
        sourceString = sourceString.Replace('-', '+'); // 62nd char of encoding
        sourceString = sourceString.Replace('_', '/'); // 63rd char of encoding

        switch (sourceString.Length % 4)     // Pad with trailing '='s
        {
            case 0:
                break;                  // No pad chars in this case
            case 2:
                sourceString += "==";
                break;                  // Two pad chars
            case 3:
                sourceString += "=";
                break;                  // One pad char
            default:
                return result;
        }
        try
        {
            byte[] bytes = Convert.FromBase64String(sourceString);
            result = Encoding.UTF8.GetString(bytes);
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Des加密
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <param name="sKey">密钥</param>
    /// <returns>Des加密后内容</returns>
    public static string DesEncrypt(this string sourceString, string sKey)
    {
        string result = string.Empty;
        if (string.IsNullOrEmpty(sourceString) || string.IsNullOrEmpty(sKey))
        {
            return result;
        }
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(sourceString);
            var key = Encoding.UTF8.GetBytes(sKey.ComputeHashToHex().Substring(0, 8));
            using var des = DES.Create();
            des.Key = key;
            des.IV = key;
            using MemoryStream memoryStream = new();
            using ICryptoTransform encryptor = des.CreateEncryptor();
            using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            StringBuilder stringBuilder = new();
            foreach (byte b in memoryStream.ToArray())
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }
            result = stringBuilder.ToString();
        }
        catch { }
        return result;
    }

    /// <summary>
    /// Des解密
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <param name="sKey">密钥</param>
    /// <returns>Des解密后内容</returns>
    public static string DesDecrypt(this string sourceString, string sKey)
    {
        if (string.IsNullOrEmpty(sourceString) || sourceString.Length % 2 != 0 || string.IsNullOrEmpty(sKey))
        {
            return string.Empty;
        }
        try
        {
            int byteCount = sourceString.Length / 2;
            byte[] bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++)
            {
                int value = Convert.ToInt32(sourceString.Substring(i * 2, 2), 16);
                bytes[i] = (byte)value;
            }
            var key = Encoding.UTF8.GetBytes(sKey.ComputeHashToHex().Substring(0, 8));
            using var des = DES.Create();
            des.Key = key;
            des.IV = key;
            using MemoryStream memoryStream = new();
            using ICryptoTransform decryptor = des.CreateDecryptor();
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 默认Des密钥
    /// </summary>
    private static readonly string _defaultDesKey = "..!@#HYStudio#@!..";

    /// <summary>
    /// Des加密
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <returns>Des加 密后内容</returns>
    public static string DesEncrypt(this string sourceString)
    {
        return sourceString.DesEncrypt(_defaultDesKey);
    }

    /// <summary>
    /// Des解密
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <returns>Des解密后内容</returns>
    public static string DesDecrypt(this string sourceString)
    {
        return sourceString.DesDecrypt(_defaultDesKey);
    }

    /// <summary>
    /// 取字符串指定标识中间的字符串
    /// </summary>
    /// <param name="sourceString">源字符串</param>
    /// <param name="leftFlag">左边字符串</param>
    /// <param name="rightFlag">右边字符串</param>
    /// <param name="includeFlag">是否包含标志</param>
    /// <returns>返回满足条件的第一个字符串</returns>
    public static string BetweenString(this string sourceString, string leftFlag, string rightFlag, bool includeFlag = false)
    {
        // 验证参数
        string result = string.Empty;
        if (string.IsNullOrEmpty(sourceString))
        {
            throw new ArgumentNullException(nameof(sourceString));
        }
        if (string.IsNullOrEmpty(leftFlag))
        {
            throw new ArgumentNullException(nameof(sourceString));
        }
        if (string.IsNullOrEmpty(rightFlag))
        {
            throw new ArgumentNullException(nameof(rightFlag));
        }
        if (leftFlag.Length > sourceString.Length || rightFlag.Length > sourceString.Length)
        {
            throw new ArgumentException("The length of the flag string is greater than the length of the source string");
        }

        // 左索引
        int leftIndex = sourceString.IndexOf(leftFlag);
        if (leftIndex == -1)
        {
            return result;
        }
        leftIndex += leftFlag.Length;

        // 右索引
        int rightIndex = sourceString.IndexOf(rightFlag, leftIndex);
        if (rightIndex == -1)
        {
            return result;
        }

        // 结果
        result = sourceString[leftIndex..rightIndex];
        if (includeFlag)
        {
            result = $"{leftFlag}{result}{rightFlag}";
        }
        return result;
    }
}

using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.International.Converters.PinYinConverter;
using System.Collections.Concurrent;

namespace LYBT.CommonUtils;

/// <summary>
/// 通用工具类 - 性能优化版本
/// </summary>
[Description("通用工具类")]
public static partial class CommonUtils {

    // 预编译正则表达式以提升性能
    [GeneratedRegex(@"\n|\r|\s|\D", RegexOptions.Compiled)]
    private static partial Regex PhoneDigitsRegex();

    [GeneratedRegex(@"^\\d{17}[\\dXx]$", RegexOptions.Compiled)]
    private static partial Regex IdNumberRegex();

    // 身份证校验权重和校验码（避免重复计算）
    private static readonly int[] IdWeights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
    private static readonly char[] IdCodes = "10X98765432".ToCharArray();

    // 拼音码缓存
    private static readonly ConcurrentDictionary<string, string> _pinyinCache = new();

    /// <summary>
    /// 检查网络是否可用
    /// </summary>
    public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();

    /// <summary>
    /// 格式化电话号码（性能优化版本）
    /// </summary>
    public static string FormatPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = PhoneDigitsRegex().Replace(phone, string.Empty);

        return digits.Length switch {
            11 => $"{digits[..3]}-{digits[3..7]}-{digits[7..]}",
            10 => $"{digits[..3]}-{digits[3..6]}-{digits[6..]}",
            _ => digits
        };
    }

    /// <summary>
    /// 验证身份证号码（性能优化版本）
    /// </summary>
    public static bool CheckIdNumber(string? idNumber) {
        if (string.IsNullOrWhiteSpace(idNumber))
            return false;

        idNumber = idNumber.Trim();

        if (!IdNumberRegex().IsMatch(idNumber))
            return false;

        // 计算校验码
        int sum = 0;
        for (int i = 0; i < 17; i++) {
            sum += (idNumber[i] - '0') * IdWeights[i];
        }

        char expectedCode = IdCodes[sum % 11];
        return char.ToUpperInvariant(idNumber[17]) == expectedCode;
    }

    /// <summary>
    /// 根据中文名称生成拼音码（带缓存）
    /// </summary>
    public static string GetPinyinCode(string? text) {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        string trimmedText = text.Trim();

        return _pinyinCache.GetOrAdd(trimmedText, static input => {
            var sb = new StringBuilder();

            foreach (var ch in input) {
                if (ChineseChar.IsValidChar(ch)) {
                    var cc = new ChineseChar(ch);
                    var py = cc.Pinyins.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
                    if (!string.IsNullOrEmpty(py))
                        sb.Append(char.ToUpperInvariant(py[0]));
                } else if (char.IsLetter(ch)) {
                    sb.Append(char.ToUpperInvariant(ch));
                }
            }

            return sb.ToString();
        });
    }

    /// <summary>
    /// 验证邮箱格式
    /// </summary>
    public static bool IsValidEmail(string? email) {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            return mailAddress.Address == email;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// 生成随机字符串
    /// </summary>
    public static string GenerateRandomString(int length, bool includeNumbers = true) {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";

        string chars = letters + letters.ToLower();
        if (includeNumbers)
            chars += numbers;

        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// 安全地转换为整数
    /// </summary>
    public static int SafeToInt(string? value, int defaultValue = 0) {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 安全地转换为小数
    /// </summary>
    public static decimal SafeToDecimal(string? value, decimal defaultValue = 0) {
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 安全地转换为布尔值
    /// </summary>
    public static bool SafeToBool(string? value, bool defaultValue = false) {
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// 清理拼音缓存
    /// </summary>
    public static void ClearPinyinCache() {
        _pinyinCache.Clear();
    }

    /// <summary>
    /// 获取拼音缓存统计信息
    /// </summary>
    public static (int Count, long MemoryEstimate) GetPinyinCacheStats() {
        int count = _pinyinCache.Count;
        long memoryEstimate = _pinyinCache.Sum(kvp =>
            (kvp.Key.Length + kvp.Value.Length) * sizeof(char));

        return (count, memoryEstimate);
    }

    /// <summary>
    /// 脱敏手机号
    /// </summary>
    public static string MaskPhoneNumber(string? phoneNumber) {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 7)
            return phoneNumber ?? string.Empty;

        return phoneNumber.Length == 11
            ? $"{phoneNumber[..3]}****{phoneNumber[7..]}"
            : $"{phoneNumber[..3]}****{phoneNumber[^3..]}";
    }

    /// <summary>
    /// 脱敏身份证号
    /// </summary>
    public static string MaskIdNumber(string? idNumber) {
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 8)
            return idNumber ?? string.Empty;

        return idNumber.Length == 18
            ? $"{idNumber[..6]}********{idNumber[14..]}"
            : $"{idNumber[..3]}****{idNumber[^2..]}";
    }
}
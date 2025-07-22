using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Reflection;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.FormulaTemplates.Dtos;
using System.Text;
using Microsoft.International.Converters.PinYinConverter;

namespace LYBT.CommonUtils;

/// <summary>
/// 通用工具类，提供 Excel 读写及常用辅助方法
/// </summary>
[Description("通用工具类")]
public static class CommonUtils {
/// <summary>
/// 执行GetDisplayName操作。
/// </summary>
/// <param name="type">参数type</param>
/// <param name="property">参数property</param>
/// <returns>返回值</returns>
    private static string GetDisplayName(Type type, string property) {
        var prop = type.GetProperty(property);
        var attr = prop?.GetCustomAttribute<DisplayNameAttribute>();
        return attr?.DisplayName ?? property;
    }

    public static bool IsNetworkAvailable() => NetworkInterface.GetIsNetworkAvailable();

/// <summary>
/// 执行FormatPhone操作。
/// </summary>
/// <param name="phone">参数phone</param>
/// <returns>返回值</returns>
    public static string FormatPhone(string? phone) {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;
        var digits = Regex.Replace(phone, @"\n|\r|\s|\D", string.Empty);
        if (digits.Length == 11)
            return $"{digits[..3]}-{digits[3..7]}-{digits[7..]}";
        if (digits.Length == 10)
            return $"{digits[..3]}-{digits[3..6]}-{digits[6..]}";
        return digits;
    }

/// <summary>
/// 执行CheckIdNumber操作。
/// </summary>
/// <param name="idNumber">参数idNumber</param>
/// <returns>返回值</returns>
    public static bool CheckIdNumber(string? idNumber) {
        if (string.IsNullOrWhiteSpace(idNumber))
            return false;
        idNumber = idNumber.Trim();
        if (!Regex.IsMatch(idNumber, "^\\d{17}[\\dXx]$"))
            return false;
        int[] weight = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
        char[] codes = "10X98765432".ToCharArray();
        int sum = 0;
        for (int i = 0; i < 17; i++) {
            sum += (idNumber[i] - '0') * weight[i];
        }
        char code = codes[sum % 11];
        return char.ToUpperInvariant(idNumber[17]) == code;
    }

    /// <summary>
    /// 根据中文名称生成拼音码（首字母缩写，全部大写）
    /// </summary>
    public static string GetPinyinCode(string? text) {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var ch in text.Trim()) {
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
    }
}

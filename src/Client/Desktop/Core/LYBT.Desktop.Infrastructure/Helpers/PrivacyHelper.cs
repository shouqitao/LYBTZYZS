namespace LYBT.Desktop.Infrastructure.Helpers;

/// <summary>
/// 隐私数据脱敏工具方法
/// </summary>
public static class PrivacyHelper
{
    /// <summary>
    /// 脱敏身份证号：保留前6位和后4位，中间用****替换。
    /// 示例: 330106199001011234 → 330106****1234；10 位短号 1234567890 → 123456****7890。
    /// null/空白/不足 10 位原样返回。
    /// </summary>
    public static string? MaskIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length < 10)
            return idNumber;

        return idNumber[..6] + "****" + idNumber[^4..];
    }
}

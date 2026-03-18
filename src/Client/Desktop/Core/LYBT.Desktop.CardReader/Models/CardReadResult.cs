using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.CardReader.Models;

/// <summary>
/// 身份证读取结果
/// 包含从身份证中读取的所有信息，与PatientInputDto字段对齐
/// </summary>
public class CardReadResult
{
    /// <summary>姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>身份证号码</summary>
    public string IdNumber { get; set; } = string.Empty;

    /// <summary>性别（从身份证"男"/"女"转换为枚举）</summary>
    public Gender Gender { get; set; } = Gender.Unknown;

    /// <summary>民族</summary>
    public string Nation { get; set; } = string.Empty;

    /// <summary>出生日期（从身份证YYYYMMDD格式解析）</summary>
    public DateTime? BirthDate { get; set; }

    /// <summary>住址</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>签发机关</summary>
    public string IssuingAuthority { get; set; } = string.Empty;

    /// <summary>有效期开始日期</summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>有效期截止日期（长期为null）</summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>证件类型（0=居民身份证，1=外国人永久居留证，2=港澳台居民居住证）</summary>
    public CardType CardType { get; set; } = CardType.IdCard;

    /// <summary>照片数据（BMP格式字节数组）</summary>
    public byte[]? PhotoData { get; set; }

    /// <summary>照片文件路径（如果保存到文件）</summary>
    public string? PhotoFilePath { get; set; }

    /// <summary>读取时间</summary>
    public DateTime ReadTime { get; set; } = DateTime.UtcNow;

    /// <summary>是否读取成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>错误信息（读取失败时）</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>原始错误代码（读取失败时）</summary>
    public int ErrorCode { get; set; }

    /// <summary>
    /// 计算年龄
    /// </summary>
    public int? Age
    {
        get
        {
            if (!BirthDate.HasValue) return null;
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static CardReadResult Success(
        string name,
        string idNumber,
        string sex,
        string nation,
        string birth,
        string address,
        string department,
        string effectDate,
        string expireDate)
    {
        return new CardReadResult
        {
            IsSuccess = true,
            Name = name.Trim(),
            IdNumber = idNumber.Trim(),
            Gender = ParseGender(sex),
            Nation = nation.Trim(),
            BirthDate = ParseDate(birth),
            Address = address.Trim(),
            IssuingAuthority = department.Trim(),
            ValidFrom = ParseDate(effectDate),
            ValidTo = ParseExpireDate(expireDate),
            ReadTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static CardReadResult Failure(int errorCode, string? errorMessage = null)
    {
        return new CardReadResult
        {
            IsSuccess = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage ?? GetErrorMessage(errorCode),
            ReadTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 解析性别
    /// </summary>
    private static Gender ParseGender(string sex)
    {
        return sex.Trim() switch
        {
            "男" => Gender.Male,
            "女" => Gender.Female,
            _ => Gender.Unknown
        };
    }

    /// <summary>
    /// 解析日期（YYYYMMDD格式）
    /// </summary>
    private static DateTime? ParseDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr) || dateStr.Length < 8)
            return null;

        if (DateTime.TryParseExact(dateStr.Trim(), "yyyyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var date))
        {
            return date;
        }

        return null;
    }

    /// <summary>
    /// 解析有效期截止日期（处理"长期"情况）
    /// </summary>
    private static DateTime? ParseExpireDate(string expireStr)
    {
        var trimmed = expireStr.Trim();
        if (trimmed == "长期" || string.IsNullOrEmpty(trimmed))
            return null;

        return ParseDate(trimmed);
    }

    /// <summary>
    /// 获取错误信息
    /// </summary>
    private static string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            0 => "成功",
            -1 => "打开设备失败",
            -2 => "设备未初始化",
            -3 => "卡认证失败",
            -4 => "读卡失败",
            -5 => "无卡或卡未放好",
            -6 => "通讯超时",
            -7 => "设备被占用",
            -8 => "内存分配失败",
            -9 => "参数错误",
            -10 => "设备不支持",
            _ => $"未知错误 ({errorCode})"
        };
    }
}

/// <summary>
/// 证件类型枚举
/// </summary>
public enum CardType
{
    /// <summary>居民身份证</summary>
    IdCard = 0,

    /// <summary>外国人永久居留证</summary>
    ForeignerResidencePermit = 1,

    /// <summary>港澳台居民居住证</summary>
    HongKongMacaoTaiwanResidencePermit = 2
}

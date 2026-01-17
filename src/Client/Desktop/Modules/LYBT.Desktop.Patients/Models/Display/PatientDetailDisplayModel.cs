using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Patients.Models.Display;

/// <summary>
/// 患者详情展示模型 - 用于只读数据展示
/// OpenSpec: unify-control-data-binding
/// </summary>
public class PatientDetailDisplayModel
{
    /// <summary>患者ID</summary>
    public Guid Id { get; set; }

    /// <summary>患者姓名</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>拼音码</summary>
    public string PinYinCode { get; set; } = string.Empty;

    /// <summary>性别</summary>
    public Gender Gender { get; set; } = Gender.Unknown;

    /// <summary>年龄</summary>
    public int? Age { get; set; }

    /// <summary>手机号</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>地址</summary>
    public string? Address { get; set; }

    /// <summary>就诊次数</summary>
    public int VisitCount { get; set; }

    /// <summary>最后就诊时间</summary>
    public DateTime? LastVisitTime { get; set; }

    /// <summary>过敏史</summary>
    public string? AllergyHistory { get; set; }

    /// <summary>病史</summary>
    public string? MedicalHistory { get; set; }

    #region 格式化属性（用于UI展示）

    /// <summary>年龄展示文本</summary>
    public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";

    /// <summary>性别展示文本</summary>
    public string GenderDisplay => Gender switch
    {
        Gender.Male => "男",
        Gender.Female => "女",
        _ => "未知"
    };

    /// <summary>基本信息摘要</summary>
    public string Summary => $"{Name} | {GenderDisplay} | {AgeDisplay}";

    /// <summary>就诊信息</summary>
    public string VisitInfo => $"{VisitCount}次 | 最近: {(LastVisitTime.HasValue ? LastVisitTime.Value.ToString("yyyy-MM-dd") : "无")}";

    #endregion
}

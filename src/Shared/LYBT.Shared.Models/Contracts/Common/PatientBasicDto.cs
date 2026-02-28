using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Common;

/// <summary>
/// 患者基本信息DTO - 用于跨模块查询
/// 仅包含最少必要字段，避免过度暴露
/// </summary>
public class PatientBasicDto
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 性别
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// 手机号码
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 患者状态
    /// T5-P2-09: 创建医案时需检查患者状态
    /// </summary>
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

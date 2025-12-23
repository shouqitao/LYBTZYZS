using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Configuration.Constants;

namespace LYBT.Shared.Configuration.Options.Client;

/// <summary>
/// 诊所设置配置
/// </summary>
public sealed class ClinicSettingsOptions
{
    public const string SectionName = ConfigurationSections.ClinicSettings;

    /// <summary>
    /// 诊所名称
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 地址
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 电话
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// 科室
    /// </summary>
    public string Department { get; set; } = "中医科";
}

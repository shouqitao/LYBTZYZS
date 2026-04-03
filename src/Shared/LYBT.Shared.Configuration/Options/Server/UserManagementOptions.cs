using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Configuration.Options.Server;

/// <summary>
/// 用户管理配置
/// </summary>
public sealed class UserManagementOptions
{
    public const string SectionName = "UserManagement";

    /// <summary>
    /// 默认角色
    /// </summary>
    [Required]
    public string DefaultRole { get; set; } = "Doctor";

    /// <summary>
    /// 是否允许自注册
    /// </summary>
    public bool AllowSelfRegistration { get; set; } = false;

    /// <summary>
    /// 是否需要邮箱确认
    /// </summary>
    public bool RequireEmailConfirmation { get; set; } = true;

    /// <summary>
    /// 是否启用用户缓存
    /// </summary>
    public bool EnableUserCache { get; set; } = true;

    /// <summary>
    /// 最大批量操作数量
    /// </summary>
    [Range(1, 1000)]
    public int MaxBatchOperationSize { get; set; } = 100;

}

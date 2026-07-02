using LYBT.Shared.Models.Enums;

namespace LYBT.SharedKernel.DTOs;

/// <summary>
/// 跨模块用户基本信息DTO。供其他模块通过IUserCrossModuleService获取用户信息。
/// </summary>
public record UserBasicDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; init; } = string.Empty;

    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRole Role { get; init; }

    /// <summary>
    /// 是否系统管理员
    /// </summary>
    public bool IsSysAdmin { get; init; }
}



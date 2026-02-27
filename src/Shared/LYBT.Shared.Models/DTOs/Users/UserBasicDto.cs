using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.DTOs.Users;

/// <summary>
/// 跨模块用户基本信息 DTO (供 Auth 模块通过 ICrossModuleService 使用)
/// 不含敏感信息 (PasswordHash)，适用于用户信息展示场景
/// </summary>
public record UserBasicDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string RealName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public CommonStatus Status { get; init; }

    // 显示用附加字段 (供 MapToUserDetailDto 映射)
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? PinYinCode { get; init; }
    public DateTime? LastLoginTime { get; init; }
    public int FailedLoginCount { get; init; }
    public DateTime? LockoutEnd { get; init; }
    /// <summary>T5-P2-31: 下次登录须改密标记</summary>
    public bool MustChangeOnNextLogin { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? Remark { get; init; }
}

/// <summary>
/// 用户凭据 DTO - 含 PasswordHash，仅供密码验证场景使用
/// 继承 UserBasicDto 以复用用户基本字段
/// </summary>
public record UserCredentialDto : UserBasicDto
{
    public string PasswordHash { get; init; } = string.Empty;
}

using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Identity.Application.Mappers;

/// <summary>
/// 认证用户模块统一映射器（A-31-C3a 合并 AuthUserMapper + UserMapper + UserCrossModuleMapper）。
/// Mapperly 编译时生成；UserName/RealName 等 Identity 基类可空字段保留手写 null 合并防御。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class IdentityMapper
{
    // ── 原 AuthUserMapper：UserCredentialDto → UserDetailDto（登录响应）──

    /// <summary>
    /// UserCredentialDto转换为UserDetailDto（登录响应）
    /// </summary>
    public static partial UserDetailDto ToUserDetailDto(UserCredentialDto user);

    // ── 原 UserMapper：ApplicationUser → 列表/详情 DTO ──

    /// <summary>
    /// ApplicationUser 转换为 UserListDto（列表查询）。
    /// </summary>
    [UserMapping(Default = false)]
    public static UserListDto ToListDto(ApplicationUser entity) => new()
    {
        Id = entity.Id,
        UserName = entity.UserName ?? string.Empty,
        RealName = entity.RealName ?? string.Empty,
        PhoneNumber = entity.PhoneNumber,
        Role = entity.Role,
        Status = entity.Status,
        LastLoginTime = entity.LastLoginTime,
        RegistrationFee = entity.RegistrationFee,
        CreatedAt = entity.CreatedAt
    };

    /// <summary>
    /// ApplicationUser 转换为 UserDetailDto（详情查询）。
    /// </summary>
    [UserMapping(Default = false)]
    public static UserDetailDto ToDetailDto(ApplicationUser entity) => new()
    {
        Id = entity.Id,
        UserName = entity.UserName ?? string.Empty,
        RealName = entity.RealName ?? string.Empty,
        Role = entity.Role,
        Status = entity.Status,
        PhoneNumber = entity.PhoneNumber,
        Email = entity.Email,
        PinYinCode = entity.PinYinCode,
        LastLoginTime = entity.LastLoginTime,
        RegistrationFee = entity.RegistrationFee,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Remark = entity.Remark
    };

    // ── 原 UserCrossModuleMapper：ApplicationUser → 跨模块 DTO ──

    /// <summary>
    /// ApplicationUser 转换为 UserBasicDto（跨模块用户基本信息）。
    /// </summary>
    [MapProperty(nameof(ApplicationUser.LastLoginTime), nameof(UserBasicDto.LastLoginTime))]
    [MapProperty(nameof(ApplicationUser.AccessFailedCount), nameof(UserBasicDto.FailedLoginCount))]
    public static partial UserBasicDto ToBasicDto(ApplicationUser user);

    /// <summary>
    /// ApplicationUser 转换为 UserCredentialDto（含 PasswordHash，仅供密码验证场景）。
    /// </summary>
    [MapProperty(nameof(ApplicationUser.LastLoginTime), nameof(UserCredentialDto.LastLoginTime))]
    [MapProperty(nameof(ApplicationUser.AccessFailedCount), nameof(UserCredentialDto.FailedLoginCount))]
    public static partial UserCredentialDto ToCredentialDto(ApplicationUser user);

    /// <summary>
    /// 自定义映射：string? → string 的 null 合并（Identity 基类可空注解）。
    /// </summary>
    private static string ToNonNullString(string? value) => value ?? string.Empty;

    /// <summary>
    /// 自定义映射：LockoutEnd 保留 UtcDateTime 语义（DateTimeOffset? → DateTime?）。
    /// </summary>
    private static DateTime? ToLockoutEnd(DateTimeOffset? value) => value?.UtcDateTime;
}

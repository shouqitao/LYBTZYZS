using LYBT.Entities.Users;
using LYBT.Shared.Models.DTOs.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Users.Application.Mappers;

/// <summary>
/// 用户跨模块映射器（A-28 P1-4 由 UserCrossModuleService 手写映射改造）。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserCrossModuleMapper
{
    /// <summary>
    /// ApplicationUser 转换为 UserBasicDto（跨模块用户基本信息）。
    /// </summary>
    [MapProperty(nameof(ApplicationUser.LastLoginAt), nameof(UserBasicDto.LastLoginTime))]
    [MapProperty(nameof(ApplicationUser.AccessFailedCount), nameof(UserBasicDto.FailedLoginCount))]
    public static partial UserBasicDto ToBasicDto(ApplicationUser user);

    /// <summary>
    /// ApplicationUser 转换为 UserCredentialDto（含 PasswordHash，仅供密码验证场景）。
    /// </summary>
    [MapProperty(nameof(ApplicationUser.LastLoginAt), nameof(UserCredentialDto.LastLoginTime))]
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

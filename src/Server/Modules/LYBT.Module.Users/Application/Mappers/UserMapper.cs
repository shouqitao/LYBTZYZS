using LYBT.Shared.Models.Contracts.Users;
using LYBT.Entities.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Users.Application.Mappers;

/// <summary>
/// 用户数据映射器。Mapperly 风格声明（A-18 P1-4 由手写静态类改造）。
/// 映射方法保留手写：UserName/RealName 含 null 合并防御（Identity 基类可空注解），Mapperly 生成无法等价表达。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class UserMapper
{
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
        LastLoginTime = entity.LastLoginAt,
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
        LastLoginTime = entity.LastLoginAt,
        RegistrationFee = entity.RegistrationFee,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        Remark = entity.Remark
    };
}

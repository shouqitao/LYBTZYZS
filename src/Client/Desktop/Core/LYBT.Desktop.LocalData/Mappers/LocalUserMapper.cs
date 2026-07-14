using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 用户映射器 - ApplicationUser Entity <-> DTO 转换
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
internal partial class LocalUserMapper
{
    /// <summary>
    /// ApplicationUser Entity -> UserDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(ApplicationUser.PasswordHash))]
    [MapperIgnoreSource(nameof(ApplicationUser.LockoutEnd))]
    [MapperIgnoreSource(nameof(ApplicationUser.AccessFailedCount))]
    [MapperIgnoreSource(nameof(ApplicationUser.CreatedBy))]
    [MapperIgnoreSource(nameof(ApplicationUser.UpdatedBy))]
    [MapperIgnoreSource(nameof(ApplicationUser.RowVersion))]
    [MapperIgnoreSource(nameof(ApplicationUser.IsDeleted))]
    [MapperIgnoreSource(nameof(ApplicationUser.MustChangeOnNextLogin))]
    [MapperIgnoreSource(nameof(ApplicationUser.IsSysAdmin))]
    [MapperIgnoreSource(nameof(ApplicationUser.NormalizedUserName))]
    [MapperIgnoreSource(nameof(ApplicationUser.NormalizedEmail))]
    [MapperIgnoreSource(nameof(ApplicationUser.EmailConfirmed))]
    [MapperIgnoreSource(nameof(ApplicationUser.SecurityStamp))]
    [MapperIgnoreSource(nameof(ApplicationUser.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(ApplicationUser.PhoneNumberConfirmed))]
    [MapperIgnoreSource(nameof(ApplicationUser.TwoFactorEnabled))]
    [MapperIgnoreSource(nameof(ApplicationUser.LockoutEnabled))]
    [MapProperty(nameof(ApplicationUser.LastLoginAt), nameof(UserDetailDto.LastLoginTime))]
    [MapperIgnoreTarget(nameof(UserDetailDto.IsEnabled))]
    [MapperIgnoreTarget(nameof(UserDetailDto.FailedLoginCount))]
    public partial UserDetailDto ToDetailDto(ApplicationUser entity);

    /// <summary>
    /// UserInputDto -> ApplicationUser Entity
    /// </summary>
    [MapperIgnoreSource(nameof(UserInputDto.Password))]
    [MapperIgnoreSource(nameof(UserInputDto.ConfirmPassword))]
    [MapperIgnoreSource(nameof(UserInputDto.Id))]
    [MapperIgnoreTarget(nameof(ApplicationUser.MustChangeOnNextLogin))]
    [MapperIgnoreTarget(nameof(ApplicationUser.PasswordHash))]
    [MapperIgnoreTarget(nameof(ApplicationUser.AccessFailedCount))]
    [MapperIgnoreTarget(nameof(ApplicationUser.LockoutEnd))]
    [MapperIgnoreTarget(nameof(ApplicationUser.LastLoginAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.CreatedAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.UpdatedAt))]
    [MapperIgnoreTarget(nameof(ApplicationUser.CreatedBy))]
    [MapperIgnoreTarget(nameof(ApplicationUser.UpdatedBy))]
    [MapperIgnoreTarget(nameof(ApplicationUser.RowVersion))]
    [MapperIgnoreTarget(nameof(ApplicationUser.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationUser.Status))]
    [MapperIgnoreTarget(nameof(ApplicationUser.Id))]
    [MapperIgnoreTarget(nameof(ApplicationUser.IsSysAdmin))]
    [MapperIgnoreTarget(nameof(ApplicationUser.NormalizedUserName))]
    [MapperIgnoreTarget(nameof(ApplicationUser.NormalizedEmail))]
    [MapperIgnoreTarget(nameof(ApplicationUser.EmailConfirmed))]
    [MapperIgnoreTarget(nameof(ApplicationUser.SecurityStamp))]
    [MapperIgnoreTarget(nameof(ApplicationUser.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ApplicationUser.PhoneNumberConfirmed))]
    [MapperIgnoreTarget(nameof(ApplicationUser.TwoFactorEnabled))]
    [MapperIgnoreTarget(nameof(ApplicationUser.LockoutEnabled))]
    public partial ApplicationUser ToEntity(UserInputDto dto);
}

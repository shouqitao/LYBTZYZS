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
    [MapProperty(nameof(ApplicationUser.LastLoginAt), nameof(UserDetailDto.LastLoginTime))]
    [MapperIgnoreTarget(nameof(UserDetailDto.IsEnabled))]
    public partial UserDetailDto ToDetailDto(ApplicationUser entity);

    /// <summary>
    /// UserInputDto -> ApplicationUser Entity
    /// </summary>
    [MapperIgnoreSource(nameof(UserInputDto.Password))]
    [MapperIgnoreSource(nameof(UserInputDto.ConfirmPassword))]
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
    public partial ApplicationUser ToEntity(UserInputDto dto);
}

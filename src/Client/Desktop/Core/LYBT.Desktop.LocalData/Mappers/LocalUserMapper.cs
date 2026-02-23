using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 用户映射器 - Entity <-> DTO 转换
/// </summary>
[Mapper]
internal partial class LocalUserMapper
{
    /// <summary>
    /// User Entity -> UserDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.LockoutEnd))]
    [MapperIgnoreSource(nameof(User.CreatedBy))]
    [MapperIgnoreSource(nameof(User.UpdatedBy))]
    [MapperIgnoreSource(nameof(User.RowVersion))]
    [MapperIgnoreSource(nameof(User.IsDeleted))]
    [MapperIgnoreTarget(nameof(UserDetailDto.IsEnabled))]
    public partial UserDetailDto ToDetailDto(User entity);

    /// <summary>
    /// UserInputDto -> User Entity
    /// </summary>
    [MapperIgnoreSource(nameof(UserInputDto.Password))]
    [MapperIgnoreSource(nameof(UserInputDto.ConfirmPassword))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.Status))]
    [MapperIgnoreTarget(nameof(User.FailedLoginCount))]
    [MapperIgnoreTarget(nameof(User.LockoutEnd))]
    [MapperIgnoreTarget(nameof(User.LastLoginTime))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.UpdatedAt))]
    [MapperIgnoreTarget(nameof(User.CreatedBy))]
    [MapperIgnoreTarget(nameof(User.UpdatedBy))]
    [MapperIgnoreTarget(nameof(User.RowVersion))]
    [MapperIgnoreTarget(nameof(User.IsDeleted))]
    public partial User ToEntity(UserInputDto dto);
}

using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Mappers;

/// <summary>
/// User DTO/Entity 双向映射器
/// OpenSpec: implement-local-mode
/// </summary>
[Mapper]
public partial class UserDataSourceMapper
{
    #region UserDetailDto ↔ User

    /// <summary>
    /// UserDetailDto → User Entity
    /// </summary>
    [MapperIgnoreSource(nameof(UserDetailDto.IsEnabled))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.LockoutEnd))]
    [MapperIgnoreTarget(nameof(User.CreatedBy))]
    [MapperIgnoreTarget(nameof(User.UpdatedBy))]
    [MapperIgnoreTarget(nameof(User.RowVersion))]
    [MapperIgnoreTarget(nameof(User.IsDeleted))]
    public partial User ToEntity(UserDetailDto dto);

    /// <summary>
    /// User Entity → UserDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.LockoutEnd))]
    [MapperIgnoreSource(nameof(User.CreatedBy))]
    [MapperIgnoreSource(nameof(User.UpdatedBy))]
    [MapperIgnoreSource(nameof(User.RowVersion))]
    [MapperIgnoreSource(nameof(User.IsDeleted))]
    [MapperIgnoreTarget(nameof(UserDetailDto.IsEnabled))]
    public partial UserDetailDto ToDetailDto(User entity);

    #endregion

    #region UserListDto → User

    /// <summary>
    /// UserListDto → User Entity（部分属性）
    /// </summary>
    [MapperIgnoreSource(nameof(UserListDto.IsEnabled))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.LockoutEnd))]
    [MapperIgnoreTarget(nameof(User.PinYinCode))]
    [MapperIgnoreTarget(nameof(User.Email))]
    [MapperIgnoreTarget(nameof(User.FailedLoginCount))]
    [MapperIgnoreTarget(nameof(User.Remark))]
    [MapperIgnoreTarget(nameof(User.UpdatedAt))]
    [MapperIgnoreTarget(nameof(User.CreatedBy))]
    [MapperIgnoreTarget(nameof(User.UpdatedBy))]
    [MapperIgnoreTarget(nameof(User.RowVersion))]
    [MapperIgnoreTarget(nameof(User.IsDeleted))]
    public partial User ToEntity(UserListDto dto);

    #endregion

    #region UserInputDto ↔ User

    /// <summary>
    /// UserInputDto → User Entity
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

    /// <summary>
    /// User Entity → UserInputDto
    /// </summary>
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Status))]
    [MapperIgnoreSource(nameof(User.FailedLoginCount))]
    [MapperIgnoreSource(nameof(User.LockoutEnd))]
    [MapperIgnoreSource(nameof(User.LastLoginTime))]
    [MapperIgnoreSource(nameof(User.CreatedAt))]
    [MapperIgnoreSource(nameof(User.UpdatedAt))]
    [MapperIgnoreSource(nameof(User.CreatedBy))]
    [MapperIgnoreSource(nameof(User.UpdatedBy))]
    [MapperIgnoreSource(nameof(User.RowVersion))]
    [MapperIgnoreSource(nameof(User.IsDeleted))]
    [MapperIgnoreTarget(nameof(UserInputDto.Password))]
    [MapperIgnoreTarget(nameof(UserInputDto.ConfirmPassword))]
    public partial UserInputDto ToInputDto(User entity);

    #endregion
}

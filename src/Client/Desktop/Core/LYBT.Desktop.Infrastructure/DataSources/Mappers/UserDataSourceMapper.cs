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
    /// <summary>
    /// UserDetailDto → User Entity
    /// </summary>
    public partial User ToEntity(UserDetailDto dto);

    /// <summary>
    /// UserListDto → User Entity（部分属性）
    /// </summary>
    public partial User ToEntity(UserListDto dto);

    /// <summary>
    /// UserInputDto → User Entity
    /// </summary>
    public partial User ToEntity(UserInputDto dto);

    /// <summary>
    /// User Entity → UserDetailDto
    /// </summary>
    public partial UserDetailDto ToDetailDto(User entity);

    /// <summary>
    /// User Entity → UserInputDto
    /// </summary>
    public partial UserInputDto ToInputDto(User entity);
}

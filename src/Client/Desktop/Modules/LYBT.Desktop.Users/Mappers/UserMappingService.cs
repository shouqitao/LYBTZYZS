// -----------------------------------------------------------------------
// <copyright file="UserMappingService.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Infrastructure.Mapping;
using LYBT.Desktop.Users.Models.Items;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Users.Mappers;

/// <summary>
/// 用户数据映射服务实现。
/// </summary>
/// <remarks>
/// 封装UserMapper，提供DI友好的映射服务接口。
/// </remarks>
public class UserMappingService
    : MappingServiceBase<UserDetailDto, UserInputDto, UserItem>
{
    private readonly UserMapper _mapper = new();

    /// <inheritdoc />
    public override UserItem ToItem(UserDetailDto dto)
        => _mapper.ToItem(dto);

    /// <inheritdoc />
    public override UserDetailDto ToDto(UserItem item)
        => _mapper.ToDto(item);

    /// <inheritdoc />
    public override UserInputDto ToInputDto(UserItem item)
        => _mapper.ToInputDto(item);
}

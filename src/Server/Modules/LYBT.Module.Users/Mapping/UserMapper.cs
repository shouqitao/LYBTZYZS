// -----------------------------------------------------------------------
// <copyright file="UserMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: adopt-mapperly-unified-mapping - Server端Mapperly映射器
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Users.Mapping;

/// <summary>
/// 用户数据映射器 - Mapperly编译时生成
/// 替代原AutoMapper的UserMappingProfile
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class UserMapper
{
    /// <summary>
    /// User实体转换为UserListDto（列表查询）
    /// </summary>
    public partial UserListDto ToListDto(User entity);

    /// <summary>
    /// User实体列表转换为UserListDto列表
    /// </summary>
    public partial List<UserListDto> ToListDtos(List<User> entities);

    /// <summary>
    /// User实体转换为UserDetailDto（详情查询）
    /// </summary>
    public partial UserDetailDto ToDetailDto(User entity);

    /// <summary>
    /// User实体列表转换为UserDetailDto列表
    /// </summary>
    public partial List<UserDetailDto> ToDetailDtos(List<User> entities);

    /// <summary>
    /// UserInputDto转换为User实体（创建）
    /// </summary>
    /// <remarks>
    /// 密码由业务逻辑处理，PinYinCode由系统生成
    /// 忽略审计字段（由Service层自动设置）
    /// </remarks>
    [MapperIgnoreSource(nameof(UserInputDto.Id))]
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.Status))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.FailedLoginCount))]
    [MapperIgnoreTarget(nameof(User.LockoutEnd))]
    [MapperIgnoreTarget(nameof(User.PinYinCode))]
    [MapperIgnoreTarget(nameof(User.LastLoginTime))]
    [MapperIgnoreTarget(nameof(User.Remark))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.CreatedBy))]
    [MapperIgnoreTarget(nameof(User.UpdatedAt))]
    [MapperIgnoreTarget(nameof(User.UpdatedBy))]
    [MapperIgnoreTarget(nameof(User.RowVersion))]
    [MapperIgnoreTarget(nameof(User.IsDeleted))]
    public partial User ToEntity(UserInputDto dto);

    /// <summary>
    /// UserInputDto更新到现有User实体
    /// </summary>
    [MapperIgnoreSource(nameof(UserInputDto.Id))]
    [MapperIgnoreSource(nameof(UserInputDto.UserName))]
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.UserName))]
    [MapperIgnoreTarget(nameof(User.Status))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.FailedLoginCount))]
    [MapperIgnoreTarget(nameof(User.LockoutEnd))]
    [MapperIgnoreTarget(nameof(User.PinYinCode))]
    [MapperIgnoreTarget(nameof(User.LastLoginTime))]
    [MapperIgnoreTarget(nameof(User.Remark))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.CreatedBy))]
    [MapperIgnoreTarget(nameof(User.UpdatedAt))]
    [MapperIgnoreTarget(nameof(User.UpdatedBy))]
    [MapperIgnoreTarget(nameof(User.RowVersion))]
    [MapperIgnoreTarget(nameof(User.IsDeleted))]
    public partial void UpdateEntity(UserInputDto dto, User entity);
}

// -----------------------------------------------------------------------
// <copyright file="UserMapper.cs" company="凌隐宝堂中医诊所">
//     Copyright (c) 凌隐宝堂中医诊所. All rights reserved.
//     OpenSpec: resolve-mapperly-source-generator-conflict
// </copyright>
// -----------------------------------------------------------------------

using LYBT.Desktop.Users.Models.Items;
using LYBT.Shared.Models.Contracts.Users;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Users.Mappers;

/// <summary>
/// 用户数据映射器 - Mapperly编译时生成
/// </summary>
/// <remarks>
/// 映射关系：
/// - UserDetailDto → UserItem (从API加载)
/// - UserItem → UserDetailDto (保存到API)
/// - UserItem → UserInputDto (创建/更新API调用)
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class UserMapper
{
    /// <summary>
    /// 将UserDetailDto转换为UserItem
    /// </summary>
    /// <param name="dto">API返回的详情DTO</param>
    /// <returns>用于XAML绑定的Item对象</returns>
    [MapperIgnoreTarget(nameof(UserItem.IsSelected))]
    [MapperIgnoreTarget(nameof(UserItem.IsHighlighted))]
    [MapperIgnoreTarget(nameof(UserItem.IsEditing))]
    [MapperIgnoreTarget(nameof(UserItem.Department))]
    [MapperIgnoreTarget(nameof(UserItem.Title))]
    [MapperIgnoreTarget(nameof(UserItem.RoleDisplayText))]
    [MapperIgnoreTarget(nameof(UserItem.RoleColor))]
    [MapperIgnoreTarget(nameof(UserItem.StatusText))]
    [MapperIgnoreTarget(nameof(UserItem.StatusColor))]
    [MapperIgnoreTarget(nameof(UserItem.IsActive))]
    [MapperIgnoreTarget(nameof(UserItem.IsAdmin))]
    [MapperIgnoreTarget(nameof(UserItem.IsDoctor))]
    [MapperIgnoreTarget(nameof(UserItem.DisplayText))]
    [MapperIgnoreTarget(nameof(UserItem.CanEdit))]
    [MapperIgnoreTarget(nameof(UserItem.CanDelete))]
    [MapperIgnoreTarget(nameof(UserItem.CanResetPassword))]
    public partial UserItem ToItem(UserDetailDto dto);

    /// <summary>
    /// 将UserItem转换为UserDetailDto
    /// </summary>
    /// <param name="item">Item对象</param>
    /// <returns>DetailDTO对象</returns>
    [MapperIgnoreSource(nameof(UserItem.IsSelected))]
    [MapperIgnoreSource(nameof(UserItem.IsHighlighted))]
    [MapperIgnoreSource(nameof(UserItem.IsEditing))]
    [MapperIgnoreSource(nameof(UserItem.Department))]
    [MapperIgnoreSource(nameof(UserItem.Title))]
    [MapperIgnoreSource(nameof(UserItem.RoleDisplayText))]
    [MapperIgnoreSource(nameof(UserItem.RoleColor))]
    [MapperIgnoreSource(nameof(UserItem.StatusText))]
    [MapperIgnoreSource(nameof(UserItem.StatusColor))]
    [MapperIgnoreSource(nameof(UserItem.IsActive))]
    [MapperIgnoreSource(nameof(UserItem.IsAdmin))]
    [MapperIgnoreSource(nameof(UserItem.IsDoctor))]
    [MapperIgnoreSource(nameof(UserItem.DisplayText))]
    [MapperIgnoreSource(nameof(UserItem.CanEdit))]
    [MapperIgnoreSource(nameof(UserItem.CanDelete))]
    [MapperIgnoreSource(nameof(UserItem.CanResetPassword))]
    [MapperIgnoreTarget(nameof(UserDetailDto.LastLoginTime))]
    [MapperIgnoreTarget(nameof(UserDetailDto.FailedLoginCount))]
    [MapperIgnoreTarget(nameof(UserDetailDto.Remark))]
    public partial UserDetailDto ToDto(UserItem item);

    /// <summary>
    /// 将UserItem转换为UserInputDto（核心映射）
    /// </summary>
    /// <param name="item">Item对象</param>
    /// <returns>InputDTO对象</returns>
    [MapperIgnoreSource(nameof(UserItem.Id))]
    [MapperIgnoreSource(nameof(UserItem.IsSelected))]
    [MapperIgnoreSource(nameof(UserItem.IsHighlighted))]
    [MapperIgnoreSource(nameof(UserItem.IsEditing))]
    [MapperIgnoreSource(nameof(UserItem.Department))]
    [MapperIgnoreSource(nameof(UserItem.Title))]
    [MapperIgnoreSource(nameof(UserItem.Status))]
    [MapperIgnoreSource(nameof(UserItem.CreatedAt))]
    [MapperIgnoreSource(nameof(UserItem.UpdatedAt))]
    [MapperIgnoreSource(nameof(UserItem.RoleDisplayText))]
    [MapperIgnoreSource(nameof(UserItem.RoleColor))]
    [MapperIgnoreSource(nameof(UserItem.StatusText))]
    [MapperIgnoreSource(nameof(UserItem.StatusColor))]
    [MapperIgnoreSource(nameof(UserItem.IsActive))]
    [MapperIgnoreSource(nameof(UserItem.IsAdmin))]
    [MapperIgnoreSource(nameof(UserItem.IsDoctor))]
    [MapperIgnoreSource(nameof(UserItem.DisplayText))]
    [MapperIgnoreSource(nameof(UserItem.CanEdit))]
    [MapperIgnoreSource(nameof(UserItem.CanDelete))]
    [MapperIgnoreSource(nameof(UserItem.CanResetPassword))]
    [MapperIgnoreTarget(nameof(UserInputDto.Id))]
    [MapperIgnoreTarget(nameof(UserInputDto.Password))]
    [MapperIgnoreTarget(nameof(UserInputDto.ConfirmPassword))]
    [MapperIgnoreTarget(nameof(UserInputDto.Remark))]
    private partial UserInputDto ToInputDtoCore(UserItem item);

    /// <summary>
    /// 将UserItem转换为UserInputDto（完整映射）
    /// </summary>
    /// <param name="item">Item对象</param>
    /// <returns>InputDTO对象</returns>
    public UserInputDto ToInputDto(UserItem item)
    {
        var dto = ToInputDtoCore(item);
        dto.Id = item.Id == Guid.Empty ? null : item.Id;
        return dto;
    }
}

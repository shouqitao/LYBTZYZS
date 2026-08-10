using LYBT.Desktop.Users.Models;
using LYBT.Desktop.Users.Models.Items;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Utilities.Text;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Users.Mappers;

/// <summary>
/// 用户详情模型映射器 - 编译时生成。
/// D2: 启用 Mapperly——统一 UserMasterDetailViewModel.LoadDetailAsync 双份手写
/// （DetailModel 一份 + EditContext 一份）+ SaveDetailAsync 第三次手写回填，消除 3 处同字段映射。
/// </summary>
/// <remarks>
/// 映射关系：
/// - UserDetailDto → UserDetailModel (从API加载，VM 详情展示)
/// - UserDetailDto → UserEditContext (编辑真源初始化)
/// - UserEditContext → UserInputDto (保存到API)
/// - UserDetailDto → UserDetailModel (保存后回填)
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class UserMapper
{
    /// <summary>
    /// 将UserDetailDto转换为UserDetailModel（核心映射，PinYinCode 手动处理）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    [MapperIgnoreSource(nameof(UserDetailDto.PinYinCode))]
    [MapperIgnoreTarget(nameof(UserDetailModel.PinYinCode))]
    [MapperIgnoreTarget(nameof(UserDetailModel.IsNew))]
    [MapperIgnoreTarget(nameof(UserDetailModel.HasErrors))]
    [MapperIgnoreTarget(nameof(UserDetailModel.Errors))]
    [MapperIgnoreTarget(nameof(UserDetailModel.HasErrorsDictionary))]
    private partial UserDetailModel ToDetailModelCore(UserDetailDto dto);

    /// <summary>
    /// 将UserDetailDto转换为UserDetailModel（完整映射，保留 PinYinCode 回退逻辑）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    public UserDetailModel ToDetailModel(UserDetailDto dto)
    {
        var model = ToDetailModelCore(dto);
        // 原 LoadDetailAsync 行为：拼音码缺失时按姓名生成
        model.PinYinCode = dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.RealName);
        return model;
    }

    /// <summary>
    /// 将UserDetailDto转换为UserEditContext（编辑真源）。
    /// D2: 替代 UserEditorViewModel.InitializeFromDto 手写字段映射（保留 PinYinCode 回退行为）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>编辑上下文。</returns>
    public UserEditContext ToEditContext(UserDetailDto dto)
    {
        return new UserEditContext
        {
            Id = dto.Id,
            UserName = dto.UserName,
            RealName = dto.RealName,
            PinYinCode = dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.RealName),
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Role = dto.Role,
            Status = dto.Status,
            LastLoginTime = dto.LastLoginTime,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Remark = dto.Remark,
            RegistrationFee = dto.RegistrationFee
        };
    }

    /// <summary>
    /// 将UserEditContext转换为UserInputDto（保存到API）。
    /// D2: 替代 UserEditorViewModel.GetUserInput 手写映射（保留 Trim 行为）。
    /// </summary>
    /// <param name="context">编辑上下文。</param>
    /// <returns>InputDTO对象。</returns>
    public UserInputDto ToInputDto(UserEditContext context)
    {
        return new UserInputDto
        {
            Id = context.Id == Guid.Empty ? null : context.Id,
            UserName = context.UserName.Trim(),
            RealName = context.RealName?.Trim() ?? string.Empty,
            PinYinCode = context.PinYinCode?.Trim(),
            PhoneNumber = context.PhoneNumber?.Trim(),
            Email = context.Email?.Trim(),
            Role = context.Role,
            Remark = context.Remark?.Trim(),
            RegistrationFee = context.RegistrationFee
        };
    }

    /// <summary>
    /// 将保存后返回的UserDetailDto应用到已有UserDetailModel（回填）。
    /// D2: 替代 SaveDetailAsync 手写回填；PinYinCode 保留原 Model 值当 DTO 为空（原行为）。
    /// </summary>
    /// <param name="target">目标 Model（回填目标）。</param>
    /// <param name="source">保存后返回的 DTO。</param>
    public void ApplyToDetailModel(UserDetailModel target, UserDetailDto source)
    {
        target.Id = source.Id;
        target.UserName = source.UserName;
        target.RealName = source.RealName;
        target.PinYinCode = source.PinYinCode ?? target.PinYinCode ?? string.Empty;
        target.PhoneNumber = source.PhoneNumber;
        target.Email = source.Email;
        target.Role = source.Role;
        target.Status = source.Status;
        target.LastLoginTime = source.LastLoginTime;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
        target.Remark = source.Remark;
        target.RegistrationFee = source.RegistrationFee;
    }
}

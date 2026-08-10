using LYBT.Desktop.Catalog.Models;
using LYBT.Desktop.Catalog.Models.Items;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Utilities.Text;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Catalog.Mappers;

/// <summary>
/// 药材详情模型映射器 - 编译时生成。
/// D1: 新建——对齐 FormulaDetailModelMapper/MedicalCase 在用 Mapperly 模式，
/// 替代 HerbMasterDetailViewModel 手写 new HerbDetailModel + HerbEditorViewModel 手写 DTO→EditContext 的双重手工映射。
/// </summary>
/// <remarks>
/// 映射关系：
/// - HerbDetailDto → HerbDetailModel (从API加载，VM 详情展示)
/// - HerbDetailDto → HerbEditContext (编辑真源初始化)
/// - HerbEditContext → HerbInputDto (保存到API)
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class HerbDetailModelMapper
{
    /// <summary>
    /// 将HerbDetailDto转换为HerbDetailModel（核心映射）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    [MapperIgnoreSource(nameof(HerbDetailDto.CreatedAt))]
    [MapperIgnoreSource(nameof(HerbDetailDto.UpdatedAt))]
    [MapperIgnoreSource(nameof(HerbDetailDto.CreatedBy))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.IsNew))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.CreatedAt))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.UpdatedAt))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.HasErrors))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.Errors))]
    [MapperIgnoreTarget(nameof(HerbDetailModel.HasErrorsDictionary))]
    private partial HerbDetailModel ToItemCore(HerbDetailDto dto);

    /// <summary>
    /// 将HerbDetailDto转换为HerbDetailModel（完整映射，保留 PinYinCode 回退逻辑）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>用于XAML绑定的Model对象。</returns>
    public HerbDetailModel ToItem(HerbDetailDto dto)
    {
        var model = ToItemCore(dto);
        // 原 HerbMasterDetailViewModel.LoadDetailAsync 行为：拼音码缺失时按名称生成
        model.PinYinCode = dto.PinYinCode ?? PinYinHelper.GetPinYinCode(dto.Name);
        return model;
    }

    /// <summary>
    /// 将HerbDetailDto转换为HerbEditContext（编辑真源）。
    /// D1: 替代 HerbEditorViewModel.InitializeFromDto 手写字段映射（保留 PinYinCode 回退行为）。
    /// </summary>
    /// <param name="dto">API返回的详情DTO。</param>
    /// <returns>编辑上下文。</returns>
    public HerbEditContext ToEditContext(HerbDetailDto dto)
    {
        return new HerbEditContext
        {
            Id = dto.Id,
            Name = dto.Name,
            PinYinCode = dto.PinYinCode ?? dto.Name,
            Category = dto.Category,
            Properties = dto.Properties,
            Origin = dto.Origin,
            Spec = dto.Spec,
            Unit = dto.Unit,
            Price = dto.Price,
            CostPrice = dto.CostPrice,
            Effect = dto.Effect,
            Usage = dto.Usage,
            Remark = dto.Remark,
            Status = dto.Status
        };
    }

    /// <summary>
    /// 将HerbEditContext转换为HerbInputDto（保存到API）。
    /// D1: 替代 HerbEditorViewModel.GetHerbData 手写映射（保留 Trim 行为）。
    /// </summary>
    /// <param name="context">编辑上下文。</param>
    /// <returns>InputDTO对象。</returns>
    public HerbInputDto ToInputDto(HerbEditContext context)
    {
        return new HerbInputDto
        {
            Id = context.Id,
            Name = context.Name.Trim(),
            PinYinCode = context.PinYinCode?.Trim(),
            Category = context.Category?.Trim(),
            Properties = context.Properties?.Trim(),
            Origin = context.Origin?.Trim(),
            Spec = context.Spec?.Trim(),
            Unit = context.Unit.Trim(),
            Price = context.Price,
            CostPrice = context.CostPrice,
            Effect = context.Effect?.Trim(),
            Usage = context.Usage?.Trim(),
            Remark = context.Remark?.Trim()
        };
    }
}

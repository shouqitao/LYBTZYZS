using LYBT.Desktop.Catalog.Models.Items;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Catalog.ViewModels;

/// <summary>
/// 子 VM - 药材编辑 (编辑真源)
///
/// 封装 HerbEditContext，提供 DTO 初始化和数据提取
/// 替代手动字段映射和 CopyToXxx 模式
/// </summary>
public partial class HerbEditorViewModel : EditorViewModelBase<HerbEditContext>
{
    private HerbEditContext _herb = HerbEditContext.CreateNew();

    /// <summary>药材编辑上下文 (XAML 绑定目标)</summary>
    public HerbEditContext Herb
    {
        get => _herb;
        set => SetProperty(ref _herb, value);
    }

    protected override HerbEditContext Context
    {
        get => Herb;
        set => Herb = value;
    }

    protected override HerbEditContext CreateNewContext() => HerbEditContext.CreateNew();

    /// <summary>
    /// 从 DTO 初始化 (查看/编辑已有药材)
    /// </summary>
    public void InitializeFromDto(HerbDetailDto dto)
    {
        var context = new HerbEditContext
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

        Herb = context;
        IsDirty = false;
        SubscribeContext();
    }

    /// <summary>
    /// 提取编辑数据为 HerbInputDto (用于保存)
    /// </summary>
    public HerbInputDto GetHerbData()
    {
        return new HerbInputDto
        {
            Id = Herb.Id,
            Name = Herb.Name.Trim(),
            PinYinCode = Herb.PinYinCode?.Trim(),
            Category = Herb.Category?.Trim(),
            Properties = Herb.Properties?.Trim(),
            Origin = Herb.Origin?.Trim(),
            Spec = Herb.Spec?.Trim(),
            Unit = Herb.Unit.Trim(),
            Price = Herb.Price,
            CostPrice = Herb.CostPrice,
            Effect = Herb.Effect?.Trim(),
            Usage = Herb.Usage?.Trim(),
            Remark = Herb.Remark?.Trim()
        };
    }
}

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.ViewModels;

/// <summary>
/// 子 VM - 药材编辑 (编辑真源)
/// OpenSpec: frontend-architecture-unification
///
/// 封装 HerbEditContext，提供 DTO 初始化和数据提取
/// 替代手动字段映射和 CopyToXxx 模式
/// </summary>
public partial class HerbEditorViewModel : ObservableObject
{
    private HerbEditContext _herb = HerbEditContext.CreateNew();

    /// <summary>药材编辑上下文 (XAML 绑定目标)</summary>
    public HerbEditContext Herb
    {
        get => _herb;
        set => SetProperty(ref _herb, value);
    }

    /// <summary>是否已修改 (脏数据标记)</summary>
    public bool IsDirty { get; private set; }

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
        Herb.PropertyChanged += OnHerbPropertyChanged;
    }

    /// <summary>
    /// 初始化为新药材 (新建场景)
    /// </summary>
    public void InitializeForNewCase()
    {
        Herb = HerbEditContext.CreateNew();
        IsDirty = false;
        Herb.PropertyChanged += OnHerbPropertyChanged;
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

    /// <summary>验证编辑内容</summary>
    public bool Validate()
    {
        return Herb.ValidateAll();
    }

    /// <summary>重置编辑状态</summary>
    public void Reset()
    {
        Herb.PropertyChanged -= OnHerbPropertyChanged;
        Herb = HerbEditContext.CreateNew();
        IsDirty = false;
    }

    private void OnHerbPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsDirty = true;
    }
}

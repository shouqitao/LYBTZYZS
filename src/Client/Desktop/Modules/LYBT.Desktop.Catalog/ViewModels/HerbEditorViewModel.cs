using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.Catalog.Mappers;
using LYBT.Desktop.Catalog.Models.Items;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Catalog.ViewModels;

/// <summary>
/// 子 VM - 药材编辑 (编辑真源)
///
/// 封装 HerbEditContext，提供 DTO 初始化和数据提取
/// D1: 改用 Mapperly HerbDetailModelMapper，消除手写字段映射
/// </summary>
public partial class HerbEditorViewModel : EditorViewModelBase<HerbEditContext>
{
    private readonly HerbDetailModelMapper _mapper;

    /// <summary>药材编辑上下文 (XAML 绑定目标)——[ObservableProperty] 源生成属性 Herb</summary>
    [ObservableProperty]
    private HerbEditContext _herb = HerbEditContext.CreateNew();

    /// <summary>
    /// 构造函数
    /// </summary>
    public HerbEditorViewModel(HerbDetailModelMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    protected override HerbEditContext Context
    {
        get => Herb;
        set => Herb = value;
    }

    protected override HerbEditContext CreateNewContext() => HerbEditContext.CreateNew();

    /// <summary>
    /// 从 DTO 初始化 (查看/编辑已有药材)
    /// D1: 改用 Mapperly ToEditContext（保留 PinYinCode 回退行为）
    /// </summary>
    public void InitializeFromDto(HerbDetailDto dto)
    {
        Herb = _mapper.ToEditContext(dto);
        IsDirty = false;
        SubscribeContext();
    }

    /// <summary>
    /// 提取编辑数据为 HerbInputDto (用于保存)
    /// D1: 改用 Mapperly ToInputDto（保留 Trim 行为）
    /// </summary>
    public HerbInputDto GetHerbData()
    {
        return _mapper.ToInputDto(Herb);
    }
}

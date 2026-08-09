using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Catalog.Models.Items;

/// <summary>
/// 验方药材项 Model - Desktop UI 层数据模型
/// 替代直接使用 FormulaHerbItemDto，遵循 DTO 不暴露到 UI 层原则
/// </summary>
public class FormulaHerbItemModel : ObservableObject
{
    private Guid? _herbId;
    private string _herbName = string.Empty;
    private int _dosage;
    private string _unit = string.Empty;
    private string? _processingMethod;
    private DecocteMethod _decoceteMethod;
    private string? _remark;

    /// <summary>药材ID（可空，支持延迟绑定）</summary>
    public Guid? HerbId
    {
        get => _herbId;
        set => SetProperty(ref _herbId, value);
    }

    /// <summary>药材名称</summary>
    public string HerbName
    {
        get => _herbName;
        set => SetProperty(ref _herbName, value);
    }

    /// <summary>用量</summary>
    public int Dosage
    {
        get => _dosage;
        set => SetProperty(ref _dosage, value);
    }

    /// <summary>单位</summary>
    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    /// <summary>加工方法</summary>
    public string? ProcessingMethod
    {
        get => _processingMethod;
        set => SetProperty(ref _processingMethod, value);
    }

    /// <summary>煎法</summary>
    public DecocteMethod DecocteMethod
    {
        get => _decoceteMethod;
        set => SetProperty(ref _decoceteMethod, value);
    }

    /// <summary>备注</summary>
    public string? Remark
    {
        get => _remark;
        set => SetProperty(ref _remark, value);
    }
}

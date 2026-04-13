using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Primitives.Validation;

namespace LYBT.Desktop.Formula.Models.Items;

/// <summary>
/// 验方编辑上下文 - 统一编辑真源
/// OpenSpec: frontend-architecture-unification
///
/// 替代 FormulaDetailModel 的编辑角色，作为 EditControl 对象 DP 的绑定目标
/// 所有编辑字段集中于此，支持验证 (ValidatableModelBase)
/// </summary>
public class FormulaEditContext : ValidatableModelBase
{
    private Guid _id;
    private string _name = string.Empty;
    private string? _category;
    private string? _property;
    private string? _effect;
    private string? _usage;
    private string? _remark;
    private bool _isShared;
    private ObservableCollection<FormulaHerbItemDto> _herbs = new();

    /// <summary>验方ID (Guid.Empty 表示新建)</summary>
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    /// <summary>是否为新建</summary>
    public bool IsNew => Id == Guid.Empty;

    /// <summary>验方名称</summary>
    [Required(ErrorMessage = "验方名称不能为空")]
    [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "验方名称长度不能超过100个字符")]
    public string Name
    {
        get => _name;
        set => SetPropertyAndValidate(ref _name, value);
    }

    /// <summary>分类</summary>
    [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "分类长度不能超过100个字符")]
    public string? Category
    {
        get => _category;
        set => SetPropertyAndValidate(ref _category, value);
    }

    /// <summary>性味归经</summary>
    [StringLength(ValidationConstants.NameMaxLength, ErrorMessage = "性味长度不能超过100个字符")]
    public string? Property
    {
        get => _property;
        set => SetPropertyAndValidate(ref _property, value);
    }

    /// <summary>功效</summary>
    [StringLength(500, ErrorMessage = "功效长度不能超过500个字符")]
    public string? Effect
    {
        get => _effect;
        set => SetPropertyAndValidate(ref _effect, value);
    }

    /// <summary>用法</summary>
    [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
    public string? Usage
    {
        get => _usage;
        set => SetPropertyAndValidate(ref _usage, value);
    }

    /// <summary>备注</summary>
    [StringLength(ValidationConstants.RemarkMaxLength, ErrorMessage = "备注长度不能超过1000个字符")]
    public string? Remark
    {
        get => _remark;
        set => SetPropertyAndValidate(ref _remark, value);
    }

    /// <summary>是否共享</summary>
    public bool IsShared
    {
        get => _isShared;
        set => SetProperty(ref _isShared, value);
    }

    /// <summary>药材列表</summary>
    public ObservableCollection<FormulaHerbItemDto> Herbs
    {
        get => _herbs;
        set => SetProperty(ref _herbs, value);
    }

    #region Factory

    /// <summary>创建空模型</summary>
    public static FormulaEditContext CreateNew()
    {
        return new FormulaEditContext
        {
            Id = Guid.Empty,
            Name = string.Empty,
            IsShared = false,
            Herbs = new ObservableCollection<FormulaHerbItemDto>()
        };
    }

    #endregion
}

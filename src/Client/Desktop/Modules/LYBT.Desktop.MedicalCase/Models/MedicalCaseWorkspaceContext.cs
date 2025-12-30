using Prism.Mvvm;

namespace LYBT.Desktop.MedicalCase.Models;

/// <summary>
/// 医案工作区共享上下文
/// 通过RegionContext传递给所有子控件
/// OpenSpec: controlify-workspace - Phase 1.2
/// </summary>
public class MedicalCaseWorkspaceContext : BindableBase
{
    #region 核心标识

    /// <summary>
    /// 医案ID
    /// </summary>
    public Guid MedicalCaseId { get; init; }

    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string PatientName { get; init; } = string.Empty;

    /// <summary>
    /// 患者信息（性别/年龄等）
    /// </summary>
    public string PatientInfo { get; init; } = string.Empty;

    #endregion

    #region 编辑状态

    private bool _isEditing;
    /// <summary>
    /// 是否处于编辑状态
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    /// <summary>
    /// 是否只读模式
    /// </summary>
    public bool IsReadOnly => !IsEditing;

    private bool _hasUnsavedChanges;
    /// <summary>
    /// 是否有未保存的修改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    #endregion

    #region 审计相关

    /// <summary>
    /// 是否为历史编辑模式（需要审计理由）
    /// </summary>
    public bool IsHistoricalEditMode { get; init; }

    private string? _editReason;
    /// <summary>
    /// 编辑理由（历史编辑模式下必填）
    /// </summary>
    public string? EditReason
    {
        get => _editReason;
        set => SetProperty(ref _editReason, value);
    }

    #endregion

    #region 工作区模式

    /// <summary>
    /// 工作区模式（来源：临床看诊/管理编辑）
    /// </summary>
    public WorkspaceMode Mode { get; init; }

    /// <summary>
    /// 编辑类型（新建/编辑草稿/编辑已完成/只读）
    /// </summary>
    public EditType EditType { get; init; }

    #endregion

    #region RowVersion（乐观锁）

    /// <summary>
    /// 医案的RowVersion（用于乐观锁）
    /// </summary>
    public byte[]? MedicalCaseRowVersion { get; set; }

    /// <summary>
    /// 诊断的RowVersion
    /// </summary>
    public byte[]? ConsultationRowVersion { get; set; }

    /// <summary>
    /// 处方的RowVersion
    /// </summary>
    public byte[]? PrescriptionRowVersion { get; set; }

    #endregion
}

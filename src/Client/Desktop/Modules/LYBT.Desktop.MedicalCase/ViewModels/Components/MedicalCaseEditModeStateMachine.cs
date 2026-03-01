using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LYBT.Desktop.MedicalCase.Models;

namespace LYBT.Desktop.MedicalCase.ViewModels.Components;

/// <summary>
/// 医案编辑模式状态机
/// OpenSpec: refactor-viewmodel-layer Phase 1 - 解决编辑模式状态交织问题
/// OpenSpec: standardize-viewmodel-framework - 迁移到CommunityToolkit.Mvvm
///
/// 职责:
/// - 管理编辑状态（Editing/ReadOnly）
/// - 计算按钮可见性
/// - 计算标题和状态显示
/// - 处理状态转换
/// </summary>
public partial class MedicalCaseEditModeStateMachine : ObservableObject
{
    #region 核心状态字段

    private WorkspaceMode _workspaceMode = WorkspaceMode.Clinical;
    private EditState _editState = EditState.Editing;
    private EditType _editType = EditType.Create;
    private bool _canEdit;

    /// <summary>
    /// 是否有未保存的修改
    /// </summary>
    [ObservableProperty]
    private bool _hasUnsavedChanges;

    /// <summary>
    /// 编辑原因（历史编辑模式下）
    /// </summary>
    [ObservableProperty]
    private string _editReason = string.Empty;

    #endregion

    #region 状态转换事件

    /// <summary>
    /// 编辑状态变化事件
    /// </summary>
    public event EventHandler<EditStateChangedEventArgs>? EditStateChanged;

    #endregion

    #region 核心状态属性

    /// <summary>
    /// 工作区模式（Clinical/Management）
    /// </summary>
    public WorkspaceMode WorkspaceMode
    {
        get => _workspaceMode;
        set
        {
            if (SetProperty(ref _workspaceMode, value))
            {
                RaiseAllComputedPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// 编辑状态（Editing/ReadOnly）
    /// </summary>
    public EditState EditState
    {
        get => _editState;
        private set
        {
            var oldState = _editState;
            if (SetProperty(ref _editState, value))
            {
                RaiseAllComputedPropertiesChanged();
                EditStateChanged?.Invoke(this, new EditStateChangedEventArgs(oldState, value));
            }
        }
    }

    /// <summary>
    /// 编辑类型（Create/EditSuspended/EditCompleted/ViewOnly）
    /// </summary>
    public EditType EditType
    {
        get => _editType;
        set
        {
            if (SetProperty(ref _editType, value))
            {
                RaiseAllComputedPropertiesChanged();
            }
        }
    }

    /// <summary>
    /// 是否有编辑权限
    /// </summary>
    public bool CanEdit
    {
        get => _canEdit;
        set
        {
            if (SetProperty(ref _canEdit, value))
            {
                OnPropertyChanged(nameof(ShowEditButton));
                OnPropertyChanged(nameof(ShowEditButtonTopRight));
                OnPropertyChanged(nameof(CanEnterEditMode));
            }
        }
    }

    #endregion

    #region 计算属性 - 编辑状态

    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    public bool IsEditing => EditState == EditState.Editing;

    /// <summary>
    /// 是否处于只读模式
    /// </summary>
    public bool IsReadOnly => EditState == EditState.ReadOnly;

    /// <summary>
    /// 是否为历史编辑模式（已完成医案的编辑）
    /// </summary>
    public bool IsHistoricalEditMode => EditType == EditType.EditCompleted;

    /// <summary>
    /// 是否可以进入编辑模式
    /// </summary>
    public bool CanEnterEditMode => IsReadOnly && CanEdit;

    #endregion

    #region 计算属性 - 按钮可见性

    /// <summary>
    /// 是否显示底部编辑按钮（Clinical只读模式）
    /// </summary>
    public bool ShowEditButton => IsReadOnly && CanEdit && WorkspaceMode == WorkspaceMode.Clinical;

    /// <summary>
    /// 是否显示右上角编辑按钮（Management只读模式）
    /// </summary>
    public bool ShowEditButtonTopRight => IsReadOnly && CanEdit && WorkspaceMode == WorkspaceMode.Management;

    /// <summary>
    /// 是否显示保存按钮（Management编辑模式）
    /// </summary>
    public bool ShowSaveButton => IsEditing && WorkspaceMode == WorkspaceMode.Management;

    /// <summary>
    /// 是否显示暂存按钮（Clinical编辑模式）
    /// </summary>
    public bool ShowSuspendButton => IsEditing && WorkspaceMode == WorkspaceMode.Clinical;

    /// <summary>
    /// 是否显示完成看诊按钮（Clinical编辑模式）
    /// </summary>
    public bool ShowCompleteButton => IsEditing && WorkspaceMode == WorkspaceMode.Clinical;

    #endregion

    #region 计算属性 - 标题和显示文本

    /// <summary>
    /// 标题文本
    /// </summary>
    public string HeaderTitle => WorkspaceMode switch
    {
        WorkspaceMode.Clinical => IsEditing ? "看诊中" : "查看医案",
        WorkspaceMode.Management => IsEditing ? "编辑医案" : "查看医案",
        _ => "看诊中"
    };

    /// <summary>
    /// 返回按钮文本
    /// </summary>
    public string BackButtonText => WorkspaceMode switch
    {
        WorkspaceMode.Clinical => "返回患者选择",
        WorkspaceMode.Management => "返回医案列表",
        _ => "返回"
    };

    /// <summary>
    /// 编辑状态文本
    /// </summary>
    public string EditStateText => EditState switch
    {
        EditState.Editing => IsHistoricalEditMode ? "历史编辑中" : "编辑中",
        EditState.ReadOnly => "只读",
        _ => ""
    };

    /// <summary>
    /// 编辑状态颜色
    /// </summary>
    public Brush EditStateColor => EditState switch
    {
        EditState.Editing => IsHistoricalEditMode
            ? new SolidColorBrush(Color.FromRgb(255, 152, 0))  // 橙色 - 历史编辑
            : new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色 - 正常编辑
        EditState.ReadOnly => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // 灰色
        _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
    };

    #endregion

    #region 状态转换方法

    /// <summary>
    /// 进入编辑模式
    /// </summary>
    /// <returns>是否成功进入编辑模式</returns>
    public bool EnterEditMode()
    {
        if (!CanEdit)
        {
            return false;
        }

        EditState = EditState.Editing;
        return true;
    }

    /// <summary>
    /// 进入只读模式
    /// </summary>
    public void EnterReadOnlyMode()
    {
        EditState = EditState.ReadOnly;
    }

    /// <summary>
    /// 初始化状态机
    /// </summary>
    /// <param name="workspaceMode">工作区模式</param>
    /// <param name="editType">编辑类型</param>
    /// <param name="canEdit">是否有编辑权限</param>
    /// <param name="initialEditState">初始编辑状态</param>
    public void Initialize(
        WorkspaceMode workspaceMode,
        EditType editType,
        bool canEdit,
        EditState initialEditState = EditState.Editing)
    {
        _workspaceMode = workspaceMode;
        _editType = editType;
        _canEdit = canEdit;

        // 根据编辑类型和权限决定初始状态
        _editState = editType switch
        {
            EditType.Create => EditState.Editing,
            EditType.EditSuspended when canEdit => initialEditState,
            EditType.EditCompleted when canEdit => initialEditState,
            EditType.ViewOnly => EditState.ReadOnly,
            _ => canEdit ? initialEditState : EditState.ReadOnly
        };

        RaiseAllPropertiesChanged();
    }

    /// <summary>
    /// 基于医案状态和用户权限自动决定编辑模式
    /// </summary>
    /// <param name="workspaceMode">工作区模式</param>
    /// <param name="isCompleted">医案是否已完成</param>
    /// <param name="isOwner">当前用户是否是医案所有者</param>
    /// <param name="isAdmin">当前用户是否是管理员</param>
    /// <param name="preferEditing">偏好编辑模式</param>
    public void DetermineFromContext(
        WorkspaceMode workspaceMode,
        bool isCompleted,
        bool isOwner,
        bool isAdmin,
        bool preferEditing = true)
    {
        WorkspaceMode = workspaceMode;

        // 确定编辑权限
        if (isAdmin)
        {
            CanEdit = true;
        }
        else
        {
            CanEdit = isOwner && !isCompleted;
        }

        // 确定编辑类型
        EditType = isCompleted ? EditType.EditCompleted : EditType.EditSuspended;

        // 确定初始编辑状态
        if (preferEditing && CanEdit)
        {
            EditState = EditState.Editing;
        }
        else
        {
            EditState = EditState.ReadOnly;
        }
    }

    #endregion

    #region 辅助方法

    private void RaiseAllComputedPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsReadOnly));
        OnPropertyChanged(nameof(IsHistoricalEditMode));
        OnPropertyChanged(nameof(CanEnterEditMode));
        OnPropertyChanged(nameof(ShowEditButton));
        OnPropertyChanged(nameof(ShowEditButtonTopRight));
        OnPropertyChanged(nameof(ShowSaveButton));
        OnPropertyChanged(nameof(ShowSuspendButton));
        OnPropertyChanged(nameof(ShowCompleteButton));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(BackButtonText));
        OnPropertyChanged(nameof(EditStateText));
        OnPropertyChanged(nameof(EditStateColor));
    }

    private void RaiseAllPropertiesChanged()
    {
        OnPropertyChanged(nameof(WorkspaceMode));
        OnPropertyChanged(nameof(EditState));
        OnPropertyChanged(nameof(EditType));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(HasUnsavedChanges));
        RaiseAllComputedPropertiesChanged();
    }

    #endregion
}

/// <summary>
/// 编辑状态变化事件参数
/// </summary>
public class EditStateChangedEventArgs : EventArgs
{
    public EditState OldState { get; }
    public EditState NewState { get; }

    public EditStateChangedEventArgs(EditState oldState, EditState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Models;
using Microsoft.Extensions.Logging;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 医案导航处理器 - 负责工作区返回导航和离开确认逻辑
    /// OpenSpec: refactor-viewmodel-layer - Phase 5.2
    ///
    /// 职责:
    /// - 处理BackCommand返回导航
    /// - 管理Clinical模式三选项对话框 (暂存/取消/继续)
    /// - 管理Management模式离开确认对话框
    /// - 协调导航前的数据保存操作
    /// </summary>
    public class MedicalCaseNavigationHandler
    {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService? _dialogService;
        private readonly ICommonDialogService? _commonDialogService;
        private readonly ILogger<MedicalCaseNavigationHandler> _logger;

        /// <summary>
        /// 保存草稿操作委托
        /// </summary>
        public Func<Task>? SaveDraftCallback { get; set; }

        /// <summary>
        /// 取消医案操作委托
        /// </summary>
        public Func<Task>? CancelCaseCallback { get; set; }

        /// <summary>
        /// 检查并获取审计原因委托
        /// 返回null表示用户取消，空字符串表示无需审计，非空字符串为审计原因
        /// </summary>
        public Func<Task<string?>>? CheckAndGetAuditReasonCallback { get; set; }

        /// <summary>
        /// 编辑原因设置委托
        /// </summary>
        public Action<string>? SetEditReasonCallback { get; set; }

        /// <summary>
        /// 编辑状态设置委托
        /// </summary>
        public Action<bool>? SetIsEditingCallback { get; set; }

        public MedicalCaseNavigationHandler(
            IRegionManager regionManager,
            IDialogService? dialogService,
            ICommonDialogService? commonDialogService,
            ILogger<MedicalCaseNavigationHandler> logger)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _dialogService = dialogService;
            _commonDialogService = commonDialogService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行返回导航
        /// Clinical: 显示三选项对话框后返回PatientSelectionView
        /// Management只读: 直接返回MedicalCaseMasterDetailView
        /// Management编辑: 显示UnsavedChangesDialog后返回
        /// OpenSpec: refactor-medicalcase-management - 使用新的Master-Detail视图
        /// </summary>
        /// <param name="workspaceMode">工作区模式</param>
        /// <param name="isReadOnly">是否只读</param>
        public async Task ExecuteBackAsync(WorkspaceMode workspaceMode, bool isReadOnly)
        {
            try
            {
                // Management模式处理
                if (workspaceMode == WorkspaceMode.Management)
                {
                    // Management只读模式: 直接返回
                    if (isReadOnly)
                    {
                        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseMasterDetailView");
                        return;
                    }

                    // Management编辑模式: 显示UnsavedChangesDialog
                    var shouldNavigate = await HandleManagementLeaveRequestAsync();
                    if (shouldNavigate)
                    {
                        _regionManager.RequestNavigate("ContentRegion", "MedicalCaseMasterDetailView");
                    }
                    return;
                }

                // Clinical模式: 使用现有的三选项对话框
                var result = await HandleLeaveRequestAsync();
                if (result.CanLeave)
                {
                    _regionManager.RequestNavigate("ContentRegion", "PatientSelectionView");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "返回时发生异常");
            }
        }

        /// <summary>
        /// Management编辑模式返回确认
        /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-008)
        /// 三选项: 保存修改(Yes) / 放弃修改(No) / 取消(Cancel)
        /// </summary>
        /// <returns>true: 允许导航; false: 留在当前界面</returns>
        public async Task<bool> HandleManagementLeaveRequestAsync()
        {
            if (_dialogService == null)
            {
                _logger.LogWarning("IDialogService不可用，无法显示未保存修改对话框，默认不允许离开");
                return false;
            }

            var tcs = new TaskCompletionSource<bool>();
            _dialogService.ShowDialog("UnsavedChangesDialog", new DialogParameters(), async dialogResult =>
            {
                try
                {
                    switch (dialogResult.Result)
                    {
                        case ButtonResult.Yes: // 保存修改
                            // 检查审计需求
                            if (CheckAndGetAuditReasonCallback != null)
                            {
                                var auditReason = await CheckAndGetAuditReasonCallback();
                                if (auditReason == null)
                                {
                                    tcs.SetResult(false); // 用户取消审计
                                    return;
                                }

                                if (!string.IsNullOrEmpty(auditReason))
                                {
                                    SetEditReasonCallback?.Invoke(auditReason);
                                }
                            }

                            if (SaveDraftCallback != null)
                            {
                                await SaveDraftCallback();
                            }
                            SetIsEditingCallback?.Invoke(false);
                            tcs.SetResult(true);
                            break;
                        case ButtonResult.No: // 放弃修改
                            tcs.SetResult(true);
                            break;
                        default: // 取消
                            tcs.SetResult(false);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理返回确认对话框时发生异常");
                    tcs.SetResult(false);
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// 显示离开确认对话框（三选项）并处理用户选择
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 此方法可由IActiveConsultationService调用（退出登录时）
        /// </summary>
        public async Task<LeaveConsultationResult> HandleLeaveRequestAsync()
        {
            var message = "您将离开看诊界面，是否暂存当前医案？\n\n" +
                "【是】暂存医案 - 保存当前进度，下次可继续\n" +
                "【否】取消医案 - 作废本次就诊\n" +
                "【取消】继续看诊 - 返回当前界面";

            LeaveConsultationChoice choice;

            if (_commonDialogService != null)
            {
                var dialogResult = await _commonDialogService.ShowTripleChoiceAsync(message, "离开确认");
                choice = dialogResult switch
                {
                    TripleChoiceResult.Yes => LeaveConsultationChoice.SaveDraft,
                    TripleChoiceResult.No => LeaveConsultationChoice.CancelCase,
                    _ => LeaveConsultationChoice.Stay
                };
            }
            else
            {
                _logger.LogWarning("CommonDialogService不可用，无法显示离开确认对话框，默认停留");
                choice = LeaveConsultationChoice.Stay;
            }

            // 根据用户选择执行对应操作
            switch (choice)
            {
                case LeaveConsultationChoice.SaveDraft:
                    if (SaveDraftCallback != null)
                    {
                        await SaveDraftCallback();
                    }
                    return LeaveConsultationResult.AllowLeave(choice);

                case LeaveConsultationChoice.CancelCase:
                    if (CancelCaseCallback != null)
                    {
                        await CancelCaseCallback();
                    }
                    return LeaveConsultationResult.AllowLeave(choice);

                case LeaveConsultationChoice.Stay:
                default:
                    _logger.LogDebug("用户选择继续停留");
                    return LeaveConsultationResult.CancelLeave();
            }
        }
    }
}

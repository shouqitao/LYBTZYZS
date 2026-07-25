using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.Models;
using LYBT.Desktop.Foundation.ExceptionHandling;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Clinical.ViewModels.Workspace;

/// <summary>
/// 医案工作台导航处理器
/// 职责：返回/离开对话框、暂存/取消医案、Management模式保存。
/// 从 MedicalCaseWorkspaceViewModel 提取，减少父 VM 体积。
/// </summary>
internal sealed class WorkspaceNavigationHandler
{
    private readonly IMedicalCaseService _medicalCaseService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IEditModeStateMachine _editStateMachine;
    private readonly IDialogService? _dialogService;
    private readonly IToastService _toastService;
    private readonly Func<ICommonDialogService?> _getCommonDialogService;
    private readonly Func<bool> _isBusy;
    private readonly Action<bool, string?> _setBusy;
    private readonly Func<Task> _showSuccessMessage;
    private readonly Func<string, Task> _showErrorMessage;

    public WorkspaceNavigationHandler(
        IMedicalCaseService medicalCaseService,
        INavigationCoordinator navigationCoordinator,
        IActiveConsultationService activeConsultationService,
        IEditModeStateMachine editStateMachine,
        IDialogService? dialogService,
        IToastService toastService,
        Func<ICommonDialogService?> getCommonDialogService,
        Func<bool> isBusy,
        Action<bool, string?> setBusy,
        Func<Task> showSuccessMessage,
        Func<string, Task> showErrorMessage)
    {
        _medicalCaseService = medicalCaseService;
        _navigationCoordinator = navigationCoordinator;
        _activeConsultationService = activeConsultationService;
        _editStateMachine = editStateMachine;
        _dialogService = dialogService;
        _toastService = toastService;
        _getCommonDialogService = getCommonDialogService;
        _isBusy = isBusy;
        _setBusy = setBusy;
        _showSuccessMessage = showSuccessMessage;
        _showErrorMessage = showErrorMessage;
    }

    /// <summary>
    /// 执行返回导航
    /// </summary>
    public async Task ExecuteBackAsync(WorkspaceState state, Guid medicalCaseId,
        Func<ConsultationInputDto?> getConsultationData,
        Func<PrescriptionInputDto?> getPrescriptionData,
        Func<Task<LeaveConsultationResult>> handleLeaveRequest)
    {
        try
        {
            if (state.Mode == WorkspaceMode.Management)
            {
                if (state.IsReadOnly)
                {
                    _ = _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                    return;
                }
                var shouldNavigate = await HandleManagementLeaveRequestAsync(medicalCaseId, getConsultationData, getPrescriptionData);
                if (shouldNavigate) _ = _navigationCoordinator.NavigateTo(ViewNames.MedicalCaseMasterDetail);
                return;
            }

            var result = await handleLeaveRequest();
            if (result.CanLeave) _ = _navigationCoordinator.NavigateTo(ViewNames.PatientSelection);
        }
        catch (Exception ex)
        {
            // Log handled by caller
        }
    }

    /// <summary>
    /// Clinical模式三选项离开确认 (供IActiveConsultationService调用)
    /// </summary>
    public async Task<LeaveConsultationResult> HandleLeaveRequestAsync(
        Guid medicalCaseId,
        Func<ConsultationInputDto?> getConsultationData,
        Func<PrescriptionInputDto?> getPrescriptionData)
    {
        var message = "您将离开看诊界面，是否暂存当前医案？\n\n" +
            "【是】暂存医案 - 保存当前进度，下次可继续\n" +
            "【否】取消医案 - 作废本次就诊\n" +
            "【取消】继续看诊 - 返回当前界面";

        LeaveConsultationChoice choice;
        var commonDialog = _getCommonDialogService();
        if (commonDialog != null)
        {
            var dialogResult = await commonDialog.ShowTripleChoiceAsync(message, "离开确认");
            choice = dialogResult switch
            {
                TripleChoiceResult.Yes => LeaveConsultationChoice.Suspend,
                TripleChoiceResult.No => LeaveConsultationChoice.CancelCase,
                _ => LeaveConsultationChoice.Stay
            };
        }
        else
        {
            choice = LeaveConsultationChoice.Stay;
        }

        switch (choice)
        {
            case LeaveConsultationChoice.Suspend:
                await SuspendOnlyAsync(medicalCaseId, getConsultationData, getPrescriptionData);
                return LeaveConsultationResult.AllowLeave(choice);
            case LeaveConsultationChoice.CancelCase:
                await CancelCaseOnlyAsync(medicalCaseId, getConsultationData, getPrescriptionData);
                return LeaveConsultationResult.AllowLeave(choice);
            default:
                return LeaveConsultationResult.CancelLeave();
        }
    }

    /// <summary>
    /// Management模式离开确认
    /// </summary>
    public async Task<bool> HandleManagementLeaveRequestAsync(
        Guid medicalCaseId,
        Func<ConsultationInputDto?> getConsultationData,
        Func<PrescriptionInputDto?> getPrescriptionData)
    {
        if (_dialogService == null) return false;

        var tcs = new TaskCompletionSource<bool>();
        _dialogService.ShowDialog("UnsavedChangesDialog", new DialogParameters(), async dialogResult =>
        {
            try
            {
                switch (dialogResult.Result)
                {
                    case ButtonResult.Yes:
                        _editStateMachine.Fire(WorkspaceEditEvent.Save, "management-leave-save");
                        await SuspendOnlyAsync(medicalCaseId, getConsultationData, getPrescriptionData);
                        _editStateMachine.Fire(WorkspaceEditEvent.SaveCompleted, "management-leave-save-completed");
                        tcs.SetResult(true);
                        break;
                    case ButtonResult.No:
                        tcs.SetResult(true);
                        break;
                    default:
                        tcs.SetResult(false);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WorkspaceNavigationHandler] Save failed during navigation: {ex.Message}");
                tcs.SetResult(false);
            }
        });

        return await tcs.Task;
    }

    /// <summary>
    /// 暂存医案
    /// </summary>
    public async Task SuspendOnlyAsync(
        Guid medicalCaseId,
        Func<ConsultationInputDto?> getConsultationData,
        Func<PrescriptionInputDto?> getPrescriptionData)
    {
        try
        {
            _setBusy(true, "正在保存...");
            await _medicalCaseService.SaveAndSuspendAsync(
                medicalCaseId, getConsultationData(), getPrescriptionData());
        }
        finally { _setBusy(false, null); }
    }

    /// <summary>
    /// 取消医案
    /// </summary>
    public async Task CancelCaseOnlyAsync(
        Guid medicalCaseId,
        Func<ConsultationInputDto?> getConsultationData,
        Func<PrescriptionInputDto?> getPrescriptionData)
    {
        try
        {
            _setBusy(true, "正在处理...");
            await _medicalCaseService.SaveAndCancelAsync(
                medicalCaseId, getConsultationData(), getPrescriptionData());
        }
        finally { _setBusy(false, null); }
    }

    /// <summary>
    /// Management模式保存
    /// </summary>
    public async Task ExecuteSaveChangesAsync(
        Guid medicalCaseId,
        Func<ConsultationInputDto?> getConsultationData,
        Func<PrescriptionInputDto?> getPrescriptionData)
    {
        try
        {
            _setBusy(true, "正在保存...");
            _editStateMachine.Fire(WorkspaceEditEvent.Save, "save-changes");
            var result = await _medicalCaseService.SaveAndSuspendAsync(
                medicalCaseId, getConsultationData(), getPrescriptionData());

            if (result.Success)
            {
                _editStateMachine.Fire(WorkspaceEditEvent.SaveCompleted, "save-changes-completed");
                await _showSuccessMessage();
            }
            else
            {
                _editStateMachine.Fire(WorkspaceEditEvent.SaveFailed, "save-changes-failed");
                await _showErrorMessage(result.Error ?? "保存失败");
            }
        }
        catch (Exception ex)
        {
            await _showErrorMessage(ClientErrorMessageMapper.GetSafeOperationFailureMessage("保存", ex));
        }
        finally { _setBusy(false, null); }
    }
}

using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Localization;
using LYBT.Desktop.Patients.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.ViewModels.Components;

/// <summary>
/// 医案启动协调器 - 处理患者开始看诊的完整流程
/// OpenSpec: cleanup-ui-layer Phase 1.2
///
/// 职责:
/// - 检查未完成医案
/// - 处理多医生场景
/// - 协调对话框交互
/// - 执行关闭/继续/新建医案操作
/// </summary>
public class MedicalCaseStartCoordinator
{
    private readonly ILogger<MedicalCaseStartCoordinator> _logger;
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler;
    private readonly ISessionManager _sessionManager;

    /// <summary>
    /// 医案启动结果
    /// </summary>
    public enum StartResult
    {
        /// <summary>继续现有医案</summary>
        ContinueExisting,
        /// <summary>创建新医案</summary>
        CreateNew,
        /// <summary>仅关闭旧医案</summary>
        CloseOnly,
        /// <summary>用户取消</summary>
        Cancelled,
        /// <summary>其他医生有挂起医案</summary>
        BlockedByOtherDoctor,
        /// <summary>发生错误</summary>
        Error
    }

    /// <summary>
    /// 医案启动结果数据
    /// </summary>
    public class StartResultData
    {
        public StartResult Result { get; init; }
        public Guid? ExistingMedicalCaseId { get; init; }
        public string? ErrorMessage { get; init; }
        public string? OtherDoctorName { get; init; }
    }

    public MedicalCaseStartCoordinator(
        ILogger<MedicalCaseStartCoordinator> logger,
        UnfinishedCaseHandler unfinishedCaseHandler,
        ISessionManager sessionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <summary>
    /// 检查患者是否有未完成医案
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>未完成医案信息，null表示可以直接开始新医案</returns>
    public async Task<MedicalCaseDetailDto?> CheckUnfinishedCaseAsync(Guid patientId)
    {
        var doctorId = _sessionManager.CurrentUser?.Id ?? Guid.Empty;

        _logger.LogInformation("检查未完成医案 - PatientId: {PatientId}, DoctorId: {DoctorId}",
            patientId, doctorId);

        var result = await _unfinishedCaseHandler.CheckUnfinishedMedicalCaseAsync(patientId, doctorId);

        _logger.LogInformation("检查结果: {HasResult}, MedicalCaseId: {CaseId}",
            result != null, result?.Id);

        return result;
    }

    /// <summary>
    /// 判断是否为其他医生的挂起医案
    /// </summary>
    public bool IsOtherDoctorCase(MedicalCaseDetailDto? unfinishedCase)
    {
        if (unfinishedCase == null) return false;

        var currentDoctorId = _sessionManager.CurrentUser?.Id ?? Guid.Empty;
        return unfinishedCase.UserId != Guid.Empty &&
               unfinishedCase.UserId != currentDoctorId;
    }

    /// <summary>
    /// 获取其他医生名称
    /// </summary>
    public string GetOtherDoctorName(MedicalCaseDetailDto unfinishedCase)
    {
        return !string.IsNullOrEmpty(unfinishedCase.DoctorName)
            ? unfinishedCase.DoctorName
            : "其他医生";
    }

    /// <summary>
    /// 继续现有医案
    /// </summary>
    public Task<StartResultData> ContinueExistingCaseAsync(PatientDetailDto patient, Guid medicalCaseId)
    {
        _logger.LogInformation("继续看诊，患者：{PatientName}，MedicalCaseId: {MedicalCaseId}",
            patient.Name, medicalCaseId);

        return Task.FromResult(new StartResultData
        {
            Result = StartResult.ContinueExisting,
            ExistingMedicalCaseId = medicalCaseId
        });
    }

    /// <summary>
    /// 关闭旧医案并创建新医案
    /// </summary>
    public async Task<StartResultData> CloseAndCreateNewAsync(PatientDetailDto patient, Guid oldMedicalCaseId)
    {
        try
        {
            _logger.LogInformation("新建医案，先关闭旧医案：OldMedicalCaseId={OldMedicalCaseId}",
                oldMedicalCaseId);

            var closed = await _unfinishedCaseHandler.CloseAndCreateNewCaseAsync(patient.Id, oldMedicalCaseId);

            if (closed)
            {
                _logger.LogInformation("旧医案已关闭");
                return new StartResultData { Result = StartResult.CreateNew };
            }

            _logger.LogWarning("关闭旧医案失败");
            return new StartResultData
            {
                Result = StartResult.Error,
                ErrorMessage = "关闭旧医案失败，请稍后重试"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "新建医案失败");
            return new StartResultData
            {
                Result = StartResult.Error,
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("新建医案", ex)
            };
        }
    }

    /// <summary>
    /// 仅关闭旧医案（不创建新医案）
    /// </summary>
    public async Task<StartResultData> CloseOnlyAsync(PatientDetailDto patient, Guid oldMedicalCaseId)
    {
        try
        {
            _logger.LogInformation("仅关闭医案：OldMedicalCaseId={OldMedicalCaseId}",
                oldMedicalCaseId);

            var closed = await _unfinishedCaseHandler.CloseOnlyAsync(patient.Id, oldMedicalCaseId);

            if (closed)
            {
                _logger.LogInformation("医案已关闭");
                return new StartResultData { Result = StartResult.CloseOnly };
            }

            _logger.LogWarning("关闭医案失败");
            return new StartResultData
            {
                Result = StartResult.Error,
                ErrorMessage = "关闭医案失败，请稍后重试"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭医案失败");
            return new StartResultData
            {
                Result = StartResult.Error,
                ErrorMessage = ClientErrorMessageMapper.GetSafeOperationFailureMessage("关闭医案", ex)
            };
        }
    }

    /// <summary>
    /// 处理用户对话框选择
    /// </summary>
    /// <param name="choice">用户选择: 1=继续, 2=新建, 3=仅关闭, 0=取消</param>
    /// <param name="patient">患者</param>
    /// <param name="unfinishedCaseId">未完成医案ID</param>
    /// <param name="refreshPendingQueueCallback">刷新待诊队列回调（仅关闭时调用）</param>
    public async Task<StartResultData> HandleUserChoiceAsync(
        int choice,
        PatientDetailDto patient,
        Guid unfinishedCaseId,
        Func<Task>? refreshPendingQueueCallback = null)
    {
        switch (choice)
        {
            case 1: // 继续看诊
                return await ContinueExistingCaseAsync(patient, unfinishedCaseId);

            case 2: // 新建医案（先关闭旧的）
                return await CloseAndCreateNewAsync(patient, unfinishedCaseId);

            case 3: // 仅关闭旧医案
                var result = await CloseOnlyAsync(patient, unfinishedCaseId);
                if (result.Result == StartResult.CloseOnly && refreshPendingQueueCallback != null)
                {
                    await refreshPendingQueueCallback();
                    _logger.LogInformation("待看诊列表已刷新");
                }
                return result;

            case 0: // 取消
            default:
                _logger.LogInformation("用户取消操作");
                return new StartResultData { Result = StartResult.Cancelled };
        }
    }
}

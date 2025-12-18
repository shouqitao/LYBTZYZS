// MedicalCaseDataManager now in same namespace (Services)
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services;

/// <summary>
/// 医案生命周期处理器 - 负责医案的创建、暂存、取消、完成处理
/// Issue #1806: 从MedicalCaseFlowViewModel提取生命周期管理逻辑(~220行)
/// </summary>
public class MedicalCaseLifecycleHandler
{
    private readonly MedicalCaseDataManager _dataManager;
    private readonly ISessionManager? _sessionManager;
    private readonly ILogger<MedicalCaseLifecycleHandler> _logger;

    /// <summary>
    /// 生命周期操作完成事件
    /// </summary>
    public event EventHandler<LifecycleActionCompletedEventArgs>? ActionCompleted;

    public MedicalCaseLifecycleHandler(
        MedicalCaseDataManager dataManager,
        ILogger<MedicalCaseLifecycleHandler> logger,
        ISessionManager? sessionManager = null)
    {
        _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// 创建新医案
    /// </summary>
    public async Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("开始创建MedicalCase，PatientId: {PatientId}", patientId);

            // 验证SessionManager和CurrentUser
            if (!ValidateSessionAndUser(out var errorMessage))
            {
                return (false, Guid.Empty, errorMessage);
            }

            _logger.LogInformation(" SessionManager验证通过，当前用户：{UserName}（ID: {UserId}）",
                _sessionManager!.CurrentUser!.UserName, _sessionManager.CurrentUser.Id);

            // 构建MedicalCaseInputDto（Epic #1961: 统一InputDto）
            var createDto = new MedicalCaseInputDto
            {
                Id = null, // 创建操作：Id为null
                PatientId = patientId,
                DoctorId = _sessionManager.CurrentUser.Id,
                VisitDate = DateTime.Now, // 就诊日期默认为当前时间
                Remark = null // 初始创建无备注
                // 注意：Status字段由Service层管理，InputDto不包含
            };

            _logger.LogInformation(" 准备调用API创建MedicalCase，PatientId: {PatientId}, DoctorId: {DoctorId}, VisitDate: {VisitDate}",
                createDto.PatientId, createDto.DoctorId, createDto.VisitDate);

            // 使用DataManager创建MedicalCase
            var createdDto = await _dataManager.CreateAsync(createDto);

            if (createdDto == null)
            {
                _logger.LogError(" DataManager返回null，创建失败");
                return (false, Guid.Empty, "创建医案失败：服务返回空结果");
            }

            _logger.LogInformation(" MedicalCase创建成功，ID: {MedicalCaseId}", createdDto.Id);

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.Create,
                Success = true,
                MedicalCaseId = createdDto.Id
            });

            return (true, createdDto.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " 创建MedicalCase失败，PatientId: {PatientId}", patientId);
            var errorMsg = $"创建医案失败：{ex.Message}";

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.Create,
                Success = false,
                ErrorMessage = errorMsg
            });

            return (false, Guid.Empty, errorMsg);
        }
    }

    /// <summary>
    /// 暂存医案
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
    /// 使用专用 /draft 端点保存当前数据，设置状态为Draft
    /// </summary>
    public async Task<(bool success, string? errorMessage)> SaveDraftAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("暂存医案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            // OpenSpec: refactor-medicalcase-api - 使用专用 /draft 端点
            var response = await _dataManager.SaveDraftViaApiAsync(medicalCaseId);

            if (!response.Success)
            {
                return (false, response.Message ?? "暂存医案失败");
            }

            _logger.LogInformation("医案暂存成功");

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.SaveDraft,
                Success = true,
                MedicalCaseId = medicalCaseId
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "暂存医案失败");
            var errorMsg = $"暂存失败：{ex.Message}";

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.SaveDraft,
                Success = false,
                MedicalCaseId = medicalCaseId,
                ErrorMessage = errorMsg
            });

            return (false, errorMsg);
        }
    }

    /// <summary>
    /// 取消医案
    /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
    /// 使用专用 /cancel 端点设置状态为Cancelled，支持审计理由
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <param name="reason">取消原因（非当天本人操作时必填）</param>
    public async Task<(bool success, string? errorMessage)> CancelAsync(Guid medicalCaseId, string? reason = null)
    {
        try
        {
            _logger.LogInformation("取消医案，MedicalCaseId: {MedicalCaseId}, HasReason: {HasReason}",
                medicalCaseId, !string.IsNullOrEmpty(reason));

            // OpenSpec: refactor-medicalcase-api - 使用专用 /cancel 端点
            var response = await _dataManager.CancelMedicalCaseViaApiAsync(medicalCaseId, reason);

            if (!response.Success)
            {
                return (false, response.Message ?? "取消医案失败");
            }

            _logger.LogInformation("医案已取消（状态变更为Cancelled）");

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.Cancel,
                Success = true,
                MedicalCaseId = medicalCaseId
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消医案失败");
            var errorMsg = $"取消失败：{ex.Message}";

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.Cancel,
                Success = false,
                MedicalCaseId = medicalCaseId,
                ErrorMessage = errorMsg
            });

            return (false, errorMsg);
        }
    }

    /// <summary>
    /// 完成医案
    /// </summary>
    public async Task<(bool success, string? errorMessage)> CompleteAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogInformation("完成病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

            // 更新MedicalCase状态为Completed
            var result = await UpdateMedicalCaseStatusAsync(medicalCaseId, MedicalCaseStatus.Completed);

            if (!result.success)
            {
                return result;
            }

            _logger.LogInformation("病案已完成");

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.Complete,
                Success = true,
                MedicalCaseId = medicalCaseId
            });

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成病案失败");
            var errorMsg = $"完成失败：{ex.Message}";

            // 触发事件
            ActionCompleted?.Invoke(this, new LifecycleActionCompletedEventArgs
            {
                Action = LifecycleAction.Complete,
                Success = false,
                MedicalCaseId = medicalCaseId,
                ErrorMessage = errorMsg
            });

            return (false, errorMsg);
        }
    }

    /// <summary>
    /// 更新MedicalCase状态
    /// Issue #2243: 使用专用的UpdateStatusAsync API
    /// </summary>
    private async Task<(bool success, string? errorMessage)> UpdateMedicalCaseStatusAsync(Guid medicalCaseId, MedicalCaseStatus newStatus)
    {
        try
        {
            _logger.LogInformation("更新MedicalCase状态，MedicalCaseId: {MedicalCaseId}, 新状态: {NewStatus}",
                medicalCaseId, newStatus);

            // Issue #2243: 使用专用的状态更新API
            var request = new MedicalCaseStatusInputDto
            {
                Status = newStatus,
                StatusChangeReason = null // 可选的状态变更原因
            };

            var response = await _dataManager.UpdateStatusAsync(medicalCaseId, request);

            if (!response.Success)
            {
                throw new InvalidOperationException(response.Message ?? "状态更新失败");
            }

            _logger.LogInformation("MedicalCase状态更新成功，新状态: {NewStatus}", newStatus);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新MedicalCase状态失败，MedicalCaseId: {MedicalCaseId}, 目标状态: {NewStatus}",
                medicalCaseId, newStatus);
            return (false, $"更新状态失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证SessionManager和CurrentUser
    /// </summary>
    private bool ValidateSessionAndUser(out string? errorMessage)
    {
        if (_sessionManager == null)
        {
            _logger.LogError(" SessionManager为null，无法创建MedicalCase");
            errorMessage = "会话管理器未初始化，无法创建医案";
            return false;
        }

        if (_sessionManager.CurrentUser == null)
        {
            _logger.LogError(" SessionManager.CurrentUser为null，无法创建MedicalCase");
            errorMessage = "用户信息丢失，无法创建医案";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

/// <summary>
/// 生命周期操作枚举
/// </summary>
public enum LifecycleAction
{
    Create,
    SaveDraft,
    Cancel,
    Complete
}

/// <summary>
/// 生命周期操作完成事件参数
/// </summary>
public class LifecycleActionCompletedEventArgs : EventArgs
{
    public LifecycleAction Action { get; set; }
    public bool Success { get; set; }
    public Guid MedicalCaseId { get; set; }
    public string? ErrorMessage { get; set; }
}

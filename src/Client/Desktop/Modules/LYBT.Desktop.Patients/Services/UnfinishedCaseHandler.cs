using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 未完成医案处理器 - 负责未完成医案检查和处理逻辑
/// Issue #1790: 从PatientSelectionViewModel提取未完成医案处理逻辑(~250行)
/// OpenSpec: enhance-dataflow-logging - LOG-018 统一[HDL]前缀
/// </summary>
public class UnfinishedCaseHandler
{
    private readonly MedicalCaseService _medicalCaseDataManager;
    private readonly ILogger<UnfinishedCaseHandler> _logger;

    private readonly Dictionary<Guid, Guid> _pendingCaseCache = new();

    /// <summary>
    /// 医案检查完成事件
    /// </summary>
    public event EventHandler<CaseCheckCompletedEventArgs>? CaseCheckCompleted;

    /// <summary>
    /// 医案关闭完成事件
    /// </summary>
    public event EventHandler<CaseClosedEventArgs>? CaseClosed;

    public UnfinishedCaseHandler(
        MedicalCaseService medicalCaseDataManager,
        ILogger<UnfinishedCaseHandler> logger)
    {
        _medicalCaseDataManager = medicalCaseDataManager ?? throw new ArgumentNullException(nameof(medicalCaseDataManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 检查患者是否有未完成的医案
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// OpenSpec: multi-doctor-unfinished-case - 支持检测其他医生的挂起医案
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="doctorId">当前医生ID</param>
    /// <param name="checkAllDoctors">是否检查所有医生的未完成医案（默认true，用于多医生场景检测）</param>
    public async Task<MedicalCaseDetailDto?> CheckUnfinishedMedicalCaseAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = true)
    {
        try
        {
            // 1. 先查本地缓存（仅当不检查所有医生时使用缓存）
            if (!checkAllDoctors && _pendingCaseCache.TryGetValue(patientId, out var cachedMedicalCaseId))
            {
                _logger.LogDebug("[HDL] UnfinishedCase.Check → CacheHit - PatientId={PatientId} MedicalCaseId={MedicalCaseId}",
                    patientId, cachedMedicalCaseId);

                // 缓存命中,返回一个包含ID的MedicalCaseDetailDto
                return new MedicalCaseDetailDto { Id = cachedMedicalCaseId };
            }

            // 2. 调用API查询
            _logger.LogDebug("[HDL] UnfinishedCase.Check started - PatientId={PatientId} DoctorId={DoctorId} CheckAllDoctors={CheckAllDoctors}",
                patientId, doctorId, checkAllDoctors);

            // OpenSpec: multi-doctor-unfinished-case - 查询所有医生的未完成医案
            var unfinishedCase = await _medicalCaseDataManager.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors);

            if (unfinishedCase != null)
            {
                // 3. 找到未完成医案,更新缓存
                _pendingCaseCache[patientId] = unfinishedCase.Id;

                // 判断是否是其他医生的医案
                var isOtherDoctorCase = unfinishedCase.UserId != doctorId;
                _logger.LogDebug("[HDL] UnfinishedCase.Check → Found - MedicalCaseId={MedicalCaseId} CaseDoctorId={CaseDoctorId} IsOtherDoctor={IsOtherDoctor}",
                    unfinishedCase.Id, unfinishedCase.UserId, isOtherDoctorCase);
            }
            else
            {
                _logger.LogDebug("[HDL] UnfinishedCase.Check → NotFound - PatientId={PatientId}", patientId);
            }

            // 触发事件
            CaseCheckCompleted?.Invoke(this, new CaseCheckCompletedEventArgs
            {
                PatientId = patientId,
                UnfinishedCase = unfinishedCase,
                CurrentDoctorId = doctorId
            });

            return unfinishedCase;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HDL] UnfinishedCase.Check failed - PatientId={PatientId} DoctorId={DoctorId}",
                patientId, doctorId);
            return null;
        }
    }

    /// <summary>
    /// 关闭医案（新建医案后关闭旧医案场景）
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    public async Task<bool> CloseAndCreateNewCaseAsync(Guid patientId, Guid oldMedicalCaseId)
    {
        try
        {
            _logger.LogInformation("[HDL] UnfinishedCase.CloseAndCreate started - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);

            // 1. 关闭旧医案
            var response = await _medicalCaseDataManager.CloseCaseAsync(oldMedicalCaseId);

            if (response.Success)
            {
                // 2. 从缓存中移除
                _pendingCaseCache.Remove(patientId);
                _logger.LogInformation("[HDL] UnfinishedCase.CloseAndCreate completed - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);

                // 触发事件
                CaseClosed?.Invoke(this, new CaseClosedEventArgs
                {
                    PatientId = patientId,
                    MedicalCaseId = oldMedicalCaseId,
                    CreateNew = true
                });

                return true;
            }
            else
            {
                _logger.LogWarning("[HDL] UnfinishedCase.CloseAndCreate → CloseFailed - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HDL] UnfinishedCase.CloseAndCreate failed - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);
            return false;
        }
    }

    /// <summary>
    /// 仅关闭医案（不创建新医案）
    /// Issue #1790: 从PatientSelectionViewModel提取
    /// </summary>
    public async Task<bool> CloseOnlyAsync(Guid patientId, Guid oldMedicalCaseId)
    {
        try
        {
            _logger.LogInformation("[HDL] UnfinishedCase.CloseOnly started - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);

            // 1. 关闭旧医案
            var response = await _medicalCaseDataManager.CloseCaseAsync(oldMedicalCaseId);

            if (response.Success)
            {
                // 2. 从缓存中移除
                _pendingCaseCache.Remove(patientId);
                _logger.LogInformation("[HDL] UnfinishedCase.CloseOnly completed - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);

                // 触发事件
                CaseClosed?.Invoke(this, new CaseClosedEventArgs
                {
                    PatientId = patientId,
                    MedicalCaseId = oldMedicalCaseId,
                    CreateNew = false
                });

                return true;
            }
            else
            {
                _logger.LogWarning("[HDL] UnfinishedCase.CloseOnly → CloseFailed - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HDL] UnfinishedCase.CloseOnly failed - OldMedicalCaseId={OldMedicalCaseId}", oldMedicalCaseId);
            return false;
        }
    }

    /// <summary>
    /// 设置缓存（预填充）
    /// 用于从待诊队列选择患者时，直接缓存医案ID，避免重复API调用
    /// </summary>
    public void SetCache(Guid patientId, Guid medicalCaseId)
    {
        _pendingCaseCache[patientId] = medicalCaseId;
        _logger.LogDebug("[HDL] UnfinishedCase.SetCache - PatientId={PatientId} MedicalCaseId={MedicalCaseId}",
            patientId, medicalCaseId);
    }

    /// <summary>
    /// 清除缓存中的医案
    /// </summary>
    public void ClearCache(Guid patientId)
    {
        _pendingCaseCache.Remove(patientId);
        _logger.LogDebug("[HDL] UnfinishedCase.ClearCache - PatientId={PatientId}", patientId);
    }

    /// <summary>
    /// 获取缓存中的医案ID
    /// </summary>
    public Guid? GetCachedMedicalCaseId(Guid patientId)
    {
        return _pendingCaseCache.TryGetValue(patientId, out var medicalCaseId) ? medicalCaseId : null;
    }
}

/// <summary>
/// 医案检查完成事件参数
/// Issue #1790: 封装事件数据
/// OpenSpec: multi-doctor-unfinished-case - 添加CurrentDoctorId和IsOtherDoctorCase
/// </summary>
public class CaseCheckCompletedEventArgs : EventArgs
{
    public Guid PatientId { get; set; }
    public MedicalCaseDetailDto? UnfinishedCase { get; set; }
    public Guid CurrentDoctorId { get; set; }

    /// <summary>
    /// 是否是其他医生的未完成医案
    /// </summary>
    public bool IsOtherDoctorCase => UnfinishedCase != null && UnfinishedCase.UserId != CurrentDoctorId;

    /// <summary>
    /// 其他医生的名称（如果是其他医生的医案）
    /// </summary>
    public string? OtherDoctorName => IsOtherDoctorCase ? UnfinishedCase?.DoctorName : null;
}

/// <summary>
/// 医案关闭完成事件参数
/// Issue #1790: 封装事件数据
/// </summary>
public class CaseClosedEventArgs : EventArgs
{
    public Guid PatientId { get; set; }
    public Guid MedicalCaseId { get; set; }
    public bool CreateNew { get; set; }
}

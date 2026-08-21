using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Guards;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.MedicalCases.Guards;

/// <summary>
/// 医案状态守卫（T1.2）
/// 统一入口：IsLocked（诊所时区）/ CaseStatus / IsPrinted / 异人编辑
/// 供 MedicalCaseCommandService 在 Update/Complete 首行调用
/// </summary>
public sealed class MedicalCaseStateGuard : IStateGuard<MedicalCase>
{
    private readonly IMedicalCaseTimeService _timeService;

    public MedicalCaseStateGuard(IMedicalCaseTimeService timeService)
    {
        _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
    }

    /// <summary>
    /// 校验是否可编辑；与原 ValidateEditReason 等价，抽为守卫单点
    /// 未提供 EditReason 时对 Completed/IsLocked/IsPrinted/异人 抛 BusinessException
    /// </summary>
    public void EnsureCanEdit(MedicalCase medicalCase, string? editReason, Guid currentUserId)
    {
        var isPrintedEdit = medicalCase.IsPrinted && medicalCase.PrintVersion > 0;
        var isCompletedEdit = medicalCase.CaseStatus == MedicalCaseStatus.Completed;
        var isLockedEdit = _timeService.IsLocked(medicalCase);
        var isForeignEdit = medicalCase.UserId != currentUserId;

        if ((isPrintedEdit || isCompletedEdit || isLockedEdit || isForeignEdit) && string.IsNullOrWhiteSpace(editReason))
        {
            if (isPrintedEdit)
                throw new BusinessException(ErrorCode.McPrintedRequiresReason, "医案已打印，修改内容需提供编辑原因");
            if (isLockedEdit)
                throw new BusinessException(ErrorCode.McCannotEditCase, "医案已锁定（隔天），编辑需提供编辑原因");
            if (isForeignEdit)
                throw new BusinessException(ErrorCode.McCannotEditCase, "非创建医生编辑需提供编辑原因");
            throw new BusinessException(ErrorCode.McCannotEditCase, "已完成医案编辑需提供编辑原因");
        }
    }

    /// <summary>
    /// 校验未锁定；用于 Complete/强锁路径，无 EditReason 豁免
    /// </summary>
    public void EnsureNotLocked(MedicalCase medicalCase)
    {
        if (_timeService.IsLocked(medicalCase))
            throw new BusinessException(ErrorCode.McCannotEditCase, "医案已锁定，无法操作");
    }

    /// <summary>
    /// 校验是否可完成（Active/Suspended → Completed）
    /// </summary>
    public void EnsureCanComplete(MedicalCase medicalCase)
    {
        EnsureNotLocked(medicalCase);
        if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            throw new BusinessException(ErrorCode.McCannotEditCase, "医案已完成，无法重复完成");
    }
}

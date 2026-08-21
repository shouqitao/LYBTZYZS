using LYBT.Entities.MedicalCases;
using LYBT.Entities.Registrations;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Guards;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Registrations.Guards;

/// <summary>
/// 挂号状态守卫（T1.2）
/// 统一入口：Cancel 前校验 Status 与关联医案 Completed 状态
/// </summary>
public sealed class RegistrationStateGuard : IStateGuard<Registration>
{
    /// <summary>
    /// 校验是否可取消
    /// </summary>
    /// <param name="registration">挂号实体</param>
    /// <param name="medicalCase">关联医案（若有，需调用方预加载）</param>
    public void EnsureCanCancel(Registration registration, MedicalCase? medicalCase = null)
    {
        if (registration.Status != RegistrationStatus.Waiting)
            throw new BusinessException(ErrorCode.RegistrationInvalidStatusTransition, "只有等待中的挂号可以取消");

        if (registration.MedicalCaseId.HasValue)
            throw new BusinessException(ErrorCode.RegistrationCancelNotAllowed, "该挂号已关联医案，请通过医案操作取消");

        // 已完成医案关联的挂号不可取消（R14-02 / R41）
        if (medicalCase != null && medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            throw new BusinessException(ErrorCode.RegistrationCancelNotAllowed, "关联医案已完成，挂号不可取消");
    }
}

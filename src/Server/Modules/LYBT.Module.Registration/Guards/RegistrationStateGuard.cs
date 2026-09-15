using LYBT.Entities.Registrations;
using LYBT.Shared.ExceptionHandling.Exceptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Guards;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Registrations.Guards;

/// <summary>
/// 挂号状态守卫（T1.2）
/// 统一入口：Cancel 前校验 Status
/// </summary>
public sealed class RegistrationStateGuard : IStateGuard<Registration>
{
    /// <summary>
    /// 校验是否可取消
    /// </summary>
    /// <param name="registration">挂号实体</param>
    /// <remarks>
    /// 「已关联医案不可取消」（R14-02 / R41）由 <see cref="Registration.Cancel"/> 实体守卫强制，
    /// 且比「仅 Completed 不可取消」更严格 —— 此处不再缓存跨模块读出的医案实体（P10）。
    /// </remarks>
    public void EnsureCanCancel(Registration registration)
    {
        if (registration.Status != RegistrationStatus.Waiting)
            throw new BusinessException(ErrorCode.RegistrationInvalidStatusTransition, "只有等待中的挂号可以取消");
    }
}

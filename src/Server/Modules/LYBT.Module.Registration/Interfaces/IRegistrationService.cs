using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Module.Registration.Interfaces;

/// <summary>
/// 挂号服务接口
/// PRD: registration.md US-REG-001~006
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// 创建挂号 (前台模式: Source=Receptionist, Status=Waiting)
    /// US-REG-001
    /// </summary>
    Task<Result<RegistrationDetailDto>> CreateAsync(RegistrationInputDto dto);

    /// <summary>
    /// 取消挂号 (仅 Receptionist 可操作，仅 Waiting 状态)
    /// US-REG-004, REG-BR-001 前置校验
    /// </summary>
    Task<Result> CancelAsync(Guid id);

    /// <summary>
    /// 根据 ID 获取挂号详情
    /// </summary>
    Task<Result<RegistrationDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 获取等待队列 (Waiting 状态，按挂号时间升序)
    /// US-REG-003
    /// </summary>
    /// <param name="doctorId">医生 ID (null=全部)</param>
    Task<Result<List<RegistrationListDto>>> GetWaitingQueueAsync(Guid? doctorId = null);

    /// <summary>
    /// 分页查询挂号记录
    /// US-REG-007: 支持按日期范围、患者、医生过滤
    /// </summary>
    Task<Result<PagedResult<RegistrationListDto>>> GetPagedAsync(
        int page = 1, int pageSize = 20, string? keyword = null,
        DateTime? startDate = null, DateTime? endDate = null,
        Guid? patientId = null, Guid? doctorId = null);

    /// <summary>
    /// 接诊: 从队列选中患者，创建医案，Registration -> InProgress
    /// US-REG-003 验收标准第4条
    /// </summary>
    /// <param name="registrationId">挂号记录 ID</param>
    /// <returns>关联的医案 ID</returns>
    Task<Result<Guid>> StartVisitAsync(Guid registrationId);

    /// <summary>
    /// 医案完成联动: Registration -> Completed
    /// US-REG-005
    /// </summary>
    Task<Result> CompleteByMedicalCaseAsync(Guid medicalCaseId);

    /// <summary>
    /// 医案取消联动: 根据 Source 分流处理
    /// US-REG-006, D4 决策
    /// </summary>
    Task<Result> HandleMedicalCaseCancelledAsync(Guid medicalCaseId);
}

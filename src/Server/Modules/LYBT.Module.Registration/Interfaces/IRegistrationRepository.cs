using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Interfaces;

/// <summary>
/// 挂号仓储接口。定义挂号记录的数据访问契约。
/// </summary>
public interface IRegistrationRepository
{
    /// <summary>
    /// 根据ID获取挂号记录
    /// </summary>
    Task<RegistrationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询挂号记录 (带高级过滤)
    /// US-REG-007: 日期范围、患者、医生过滤
    /// </summary>
    Task<PagedResult<RegistrationEntity>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询等待队列 (Status=Waiting，按挂号时间升序)
    /// US-REG-003: 医生查看当前等待接诊的患者队列
    /// </summary>
    /// <param name="doctorId">医生 ID (null 表示全部医生)</param>
    Task<List<RegistrationEntity>> GetWaitingQueueAsync(
        Guid? doctorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取今日最大排队号
    /// 用于创建挂号时自动生成 QueueNumber
    /// </summary>
    Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查患者是否有等待中的挂号记录 (REG-70007 防重复)
    /// </summary>
    /// <param name="patientId">患者 ID</param>
    Task<bool> HasWaitingRegistrationAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据医案 ID 查找关联的挂号记录
    /// US-REG-005/006: 医案状态变更联动
    /// </summary>
    /// <param name="medicalCaseId">医案 ID</param>
    Task<RegistrationEntity?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增挂号记录
    /// </summary>
    Task AddAsync(RegistrationEntity registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新挂号记录
    /// </summary>
    Task UpdateAsync(RegistrationEntity registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存更改
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

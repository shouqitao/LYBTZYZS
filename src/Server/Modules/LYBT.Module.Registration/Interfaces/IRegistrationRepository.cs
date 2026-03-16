using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using RegistrationEntity = LYBT.Entities.Registrations.Registration;

namespace LYBT.Module.Registration.Interfaces;

/// <summary>
/// 挂号仓储接口
/// 继承 IRepository 标准 CRUD，扩展挂号特定查询
/// </summary>
public interface IRegistrationRepository : IRepository<RegistrationEntity>
{
    /// <summary>
    /// 分页查询挂号记录 (带高级过滤)
    /// US-REG-007: 日期范围、患者、医生过滤
    /// </summary>
    Task<PagedResult<RegistrationEntity>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId);
    /// <summary>
    /// 查询等待队列 (Status=Waiting，按挂号时间升序)
    /// US-REG-003: 医生查看当前等待接诊的患者队列
    /// </summary>
    /// <param name="doctorId">医生 ID (null 表示全部医生)</param>
    Task<List<RegistrationEntity>> GetWaitingQueueAsync(Guid? doctorId = null);

    /// <summary>
    /// 按状态查询挂号记录
    /// </summary>
    /// <param name="status">挂号状态</param>
    /// <param name="doctorId">医生 ID (可选)</param>
    Task<List<RegistrationEntity>> GetByStatusAsync(RegistrationStatus status, Guid? doctorId = null);

    /// <summary>
    /// 检查患者是否有等待中的挂号记录 (REG-70007 防重复)
    /// </summary>
    /// <param name="patientId">患者 ID</param>
    Task<bool> HasWaitingRegistrationAsync(Guid patientId);

    /// <summary>
    /// 根据医案 ID 查找关联的挂号记录
    /// US-REG-005/006: 医案状态变更联动
    /// </summary>
    /// <param name="medicalCaseId">医案 ID</param>
    Task<RegistrationEntity?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

    /// <summary>
    /// G-11: 获取医生等待中的挂号记录数量
    /// 用于禁用医生前检查
    /// </summary>
    /// <param name="doctorId">医生 ID</param>
    Task<int> GetWaitingCountByDoctorAsync(Guid doctorId, CancellationToken ct = default);
}

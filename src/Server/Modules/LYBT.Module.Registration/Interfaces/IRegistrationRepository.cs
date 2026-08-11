using LYBT.Entities.Registrations;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace LYBT.Module.Registrations.Interfaces;

/// <summary>
/// 挂号仓储接口。定义挂号记录的数据访问契约。
/// </summary>
public interface IRegistrationRepository
{
    /// <summary>
    /// 开启数据库事务（接诊即建：Registration + MedicalCase 原子提交）
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取挂号记录
    /// </summary>
    Task<Registration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询挂号记录 (带高级过滤)
    /// US-REG-007: 日期范围、患者、医生过滤
    /// </summary>
    Task<PagedResult<Registration>> GetPagedAsync(
        int page, int pageSize, string? keyword,
        DateTime? startDate, DateTime? endDate,
        Guid? patientId, Guid? doctorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询等待队列 (Status=Waiting，按挂号时间升序)
    /// US-REG-003: 医生查看当前等待接诊的患者队列
    /// </summary>
    /// <param name="doctorId">医生 ID (null 表示全部医生)</param>
    Task<List<Registration>> GetWaitingQueueAsync(
        Guid? doctorId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取今日最大排队号
    /// 用于创建挂号时自动生成 QueueNumber
    /// </summary>
    Task<int> GetTodayMaxQueueNumberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据医案 ID 查找关联的挂号记录
    /// US-REG-005/006: 医案状态变更联动
    /// </summary>
    /// <param name="medicalCaseId">医案 ID</param>
    Task<Registration?> GetByMedicalCaseIdAsync(Guid medicalCaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 患者当日是否已有待诊挂号（T5-1 #9 US-REG-BR-007: 同日重复挂号阻止）
    /// </summary>
    Task<bool> HasSameDayWaitingAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增挂号记录
    /// </summary>
    Task AddAsync(Registration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新挂号记录
    /// </summary>
    Task UpdateAsync(Registration registration, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存更改
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

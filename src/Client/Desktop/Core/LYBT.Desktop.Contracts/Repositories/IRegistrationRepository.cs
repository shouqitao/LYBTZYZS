using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 挂号数据仓储接口 (SYNC-D02)
/// PRD: registration.md US-REG-001~006
/// 远程模式和本地模式各有独立实现，由 DI 工厂根据 IConnectionModeProvider 选择。
/// </summary>
public interface IRegistrationRepository
{
    /// <summary>
    /// 创建挂号 (前台模式: Source=Receptionist, Status=Waiting)
    /// US-REG-001
    /// </summary>
    Task<RegistrationDetailDto> CreateAsync(RegistrationInputDto input);

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    Task<RegistrationDetailDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// 分页查询挂号记录
    /// </summary>
    Task<PagedResult<RegistrationListDto>> GetPagedAsync(int page, int pageSize, string? keyword = null);

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null);

    /// <summary>
    /// 接诊: Registration -> InProgress，返回创建的医案 ID
    /// US-REG-003 验收标准第4条
    /// </summary>
    Task<Guid?> StartVisitAsync(Guid id);

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Waiting 状态可取消
    /// </summary>
    Task<bool> CancelAsync(Guid id);
}

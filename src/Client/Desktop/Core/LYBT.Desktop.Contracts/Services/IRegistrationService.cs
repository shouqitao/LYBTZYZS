using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 挂号Service接口
/// PRD: registration.md US-REG-001~006
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// 创建挂号 (前台模式)
    /// US-REG-001: Source=Receptionist, Status=Waiting
    /// </summary>
    Task<CommandResult<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 获取挂号详情
    /// </summary>
    Task<CommandResult<RegistrationDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 分页查询挂号记录
    /// </summary>
    Task<CommandResult<PagedResult<RegistrationListDto>>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        CancellationToken ct = default);

    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    Task<CommandResult<List<RegistrationListDto>>> GetQueueAsync(
        Guid? doctorId = null,
        CancellationToken ct = default);

    /// <summary>
    /// 接诊: Registration -> InProgress
    /// US-REG-003 验收标准第4条
    /// </summary>
    Task<CommandResult<Guid>> StartVisitAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Waiting 状态可取消
    /// </summary>
    Task<CommandResult> CancelAsync(Guid id, CancellationToken ct = default);
}
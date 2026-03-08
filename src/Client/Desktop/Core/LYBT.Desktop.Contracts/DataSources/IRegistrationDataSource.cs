using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 挂号数据源接口
/// PRD: registration.md US-REG-001~006
/// </summary>
public interface IRegistrationDataSource : IDataSourceBase<RegistrationDetailDto, RegistrationInputDto>
{
    /// <summary>
    /// 获取等待队列
    /// US-REG-003: Waiting 状态，按挂号时间升序
    /// </summary>
    Task<List<RegistrationListDto>> GetWaitingQueueAsync(Guid? doctorId = null, CancellationToken ct = default);

    /// <summary>
    /// 接诊: Registration -> InProgress，返回创建的医案 ID
    /// US-REG-003 验收标准第4条
    /// </summary>
    Task<Guid?> StartVisitAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 取消挂号
    /// US-REG-004: 仅 Waiting 状态可取消
    /// </summary>
    Task<bool> CancelAsync(Guid id, CancellationToken ct = default);
}

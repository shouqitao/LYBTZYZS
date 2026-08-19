using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.MedicalCase;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 医案命令服务接口 - 跨模块共享
/// 负责医案的创建、保存、删除等写操作
/// </summary>
public interface IMedicalCaseCommandService
{
    /// <summary>
    /// 当前医案数据
    /// </summary>
    MedicalCaseDetailDto? Current { get; }

    /// <summary>
    /// 是否有未保存的变更
    /// </summary>
    bool HasChanges { get; }

    /// <summary>
    /// 保存变更（写操作，ADR-0020：返回 CommandResult）
    /// </summary>
    Task<CommandResult<bool>> SaveAsync(CancellationToken ct = default);

    /// <summary>
    /// 删除当前医案（写操作，ADR-0020）
    /// </summary>
    Task<CommandResult<bool>> DeleteAsync(CancellationToken ct = default);

    /// <summary>
    /// 创建新医案（写操作，ADR-0020）
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="registrationId">关联挂号ID（可选，从前台挂号创建时传入）</param>
    Task<CommandResult<Guid>> CreateMedicalCaseAsync(Guid patientId, Guid? registrationId = null, CancellationToken ct = default);
}

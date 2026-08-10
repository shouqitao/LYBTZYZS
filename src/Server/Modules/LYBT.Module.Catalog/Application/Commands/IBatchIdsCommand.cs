using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量目录命令契约（A-31-C3b 收敛 BatchEnable/Disable/Delete 命令的 Ids 访问）。
/// 供泛型批处理 Handler 与泛型批处理验证器统一消费。
/// </summary>
public interface IBatchIdsCommand : IRequest<Result<BatchOperationResultDto>>
{
    /// <summary>批量操作的目标实体 ID 列表</summary>
    List<Guid> Ids { get; }
}

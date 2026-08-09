using LYBT.Shared.Models.Contracts.Common;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量启用药方命令。
/// </summary>
public record BatchEnableFormulasCommand(List<Guid> Ids) : IRequest<Result<BatchOperationResultDto>>;

using MediatR;
using LYBT.SharedKernel.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 删除药材命令（软删除）。
/// </summary>
public record DeleteHerbCommand(
    Guid Id,
    Guid CurrentUserId
) : IRequest<Result>;



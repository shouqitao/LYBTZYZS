using MediatR;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Herbs.Application.Commands;

/// <summary>
/// 批量导入药材命令。
/// </summary>
public record BatchImportHerbsCommand(
    List<HerbInputDto> Herbs,
    DuplicateStrategy Strategy,
    Guid CurrentUserId
) : IRequest<Result<HerbBatchImportResultDto>>;



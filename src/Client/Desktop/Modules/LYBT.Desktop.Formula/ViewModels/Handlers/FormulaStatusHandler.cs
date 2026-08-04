using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.Formula.Models;
using LYBT.Desktop.Infrastructure.Services;
using LYBT.Desktop.Infrastructure.ViewModels.Handlers;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Handlers;

/// <summary>
/// 验方状态处理实现
/// </summary>
public class FormulaStatusHandler : BaseStatusHandler<FormulaListDto>, IFormulaStatusHandler
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaStatusHandler(
        IFormulaRepository formulaRepository,
        IMasterDetailServices<FormulaListDto, FormulaDetailModel> masterDetailServices,
        ILogger<FormulaStatusHandler> logger)
        : base(masterDetailServices.Dialog, logger)
    {
        _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
    }

    protected override string EntityTypeName => "验方";
    protected override Guid GetEntityId(FormulaListDto e) => e.Id;
    protected override string GetEntityDisplayName(FormulaListDto e) => e.Name;
    protected override CommonStatus GetEntityStatus(FormulaListDto e) => e.Status;

    protected override async Task<object?> ExecuteRestoreAsync(Guid id)
        => await _formulaRepository.RestoreAsync(id);

    protected override async Task<CommonStatus?> ExecuteToggleStatusAsync(Guid id)
    {
        var result = await _formulaRepository.ToggleStatusAsync(id);
        return result?.Status;
    }
}

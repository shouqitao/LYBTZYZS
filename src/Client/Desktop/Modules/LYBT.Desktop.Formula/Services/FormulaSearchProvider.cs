using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方搜索提供者实现 (D5-3)
/// 委托给 IFormulaRepository，供跨模块使用
/// </summary>
public class FormulaSearchProvider : IFormulaSearchProvider
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaSearchProvider(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
    }

    /// <inheritdoc />
    public async Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize)
    {
        return await _formulaRepository.GetPagedAsync(page, pageSize);
    }

    /// <inheritdoc />
    public async Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id)
    {
        return await _formulaRepository.GetByIdAsync(id);
    }
}

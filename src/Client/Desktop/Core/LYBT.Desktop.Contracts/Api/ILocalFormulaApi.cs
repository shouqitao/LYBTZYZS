using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Formula endpoints.
/// </summary>
public interface ILocalFormulaApi
{
    [Refit.Get("/api/formulas")]
    Task<List<FormulaListDto>> GetFormulasAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/formulas/{id}")]
    Task<FormulaDetailDto> GetFormulaByIdAsync(Guid id);

    [Refit.Post("/api/formulas")]
    Task<FormulaDetailDto> CreateFormulaAsync([Refit.Body] FormulaInputDto request);

    [Refit.Put("/api/formulas/{id}")]
    Task<FormulaDetailDto> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaInputDto request);

    [Refit.Delete("/api/formulas/{id}")]
    Task DeleteFormulaAsync(Guid id);
}

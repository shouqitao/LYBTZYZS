using LYBT.Entities.Formulas;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Formula endpoints.
/// </summary>
public interface ILocalFormulaApi
{
    [Refit.Get("/api/formulas")]
    Task<List<Formula>> GetFormulasAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/formulas/{id}")]
    Task<Formula> GetFormulaByIdAsync(Guid id);

    [Refit.Post("/api/formulas")]
    Task<Formula> CreateFormulaAsync([Refit.Body] Formula formula);

    [Refit.Put("/api/formulas/{id}")]
    Task<Formula> UpdateFormulaAsync(Guid id, [Refit.Body] Formula formula);

    [Refit.Delete("/api/formulas/{id}")]
    Task DeleteFormulaAsync(Guid id);
}

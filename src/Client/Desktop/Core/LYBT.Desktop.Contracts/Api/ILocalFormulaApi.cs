using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Formula endpoints.
/// </summary>
public interface ILocalFormulaApi
{
    [Refit.Get("/api/v1/formulas")]
    Task<List<FormulaListDto>> GetFormulasAsync(
        [Refit.Query] string? keyword = null,
        [Refit.Query] string? category = null);

    [Refit.Get("/api/v1/formulas/{id}")]
    Task<FormulaDetailDto> GetFormulaByIdAsync(Guid id);

    [Refit.Post("/api/v1/formulas")]
    Task<FormulaDetailDto> CreateFormulaAsync([Refit.Body] FormulaInputDto request);

    [Refit.Put("/api/v1/formulas/{id}")]
    Task<FormulaDetailDto> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaInputDto request);

    [Refit.Delete("/api/v1/formulas/{id}")]
    Task DeleteFormulaAsync(Guid id);

    [Refit.Post("/api/v1/formulas/{id}/clone")]
    Task<FormulaDetailDto> CloneFormulaAsync(Guid id);

    [Refit.Post("/api/v1/formulas/{id}/toggle-status")]
    Task<FormulaDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/v1/formulas/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

    [Refit.Get("/api/v1/formulas/categories")]
    Task<List<string>> GetCategoriesAsync();

    [Refit.Get("/api/v1/formulas/export")]
    Task<List<FormulaDetailDto>> ExportFormulasAsync([Refit.Query] string? category = null);

    [Refit.Get("/api/v1/formulas/import-template")]
    Task<object> ExportTemplateAsync();

    [Refit.Post("/api/v1/formulas/batch-import")]
    Task<FormulaBatchImportResultDto> BatchImportAsync([Refit.Body] FormulaBatchImportInputDto request);
}

using LYBT.Shared.Models.Contracts.Common;
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

    [Refit.Post("/api/formulas/{id}/clone")]
    Task<FormulaDetailDto> CloneFormulaAsync(Guid id);

    [Refit.Post("/api/formulas/{id}/toggle-status")]
    Task<FormulaDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/formulas/{id}/restore")]
    Task<FormulaDetailDto> RestoreAsync(Guid id);

    [Refit.Post("/api/formulas/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/formulas/batch-enable")]
    Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/formulas/batch-disable")]
    Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] List<Guid> ids);

    [Refit.Get("/api/formulas/export")]
    Task<List<FormulaDetailDto>> ExportFormulasAsync([Refit.Query] string? category = null);

    [Refit.Get("/api/formulas/import-template")]
    Task<object> ExportTemplateAsync();
}

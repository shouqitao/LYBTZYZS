using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Herb endpoints.
/// </summary>
public interface ILocalHerbApi
{
    [Refit.Get("/api/v1/herbs")]
    Task<List<HerbListDto>> GetHerbsAsync(
        [Refit.Query] string? keyword = null,
        [Refit.Query] string? category = null);

    [Refit.Get("/api/v1/herbs/{id}")]
    Task<HerbDetailDto> GetHerbByIdAsync(Guid id);

    [Refit.Post("/api/v1/herbs")]
    Task<HerbDetailDto> CreateHerbAsync([Refit.Body] HerbInputDto request);

    [Refit.Put("/api/v1/herbs/{id}")]
    Task<HerbDetailDto> UpdateHerbAsync(Guid id, [Refit.Body] HerbInputDto request);

    [Refit.Delete("/api/v1/herbs/{id}")]
    Task DeleteHerbAsync(Guid id);

    [Refit.Post("/api/v1/herbs/{id}/toggle-status")]
    Task<HerbDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/v1/herbs/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

    [Refit.Get("/api/v1/herbs/categories")]
    Task<List<string>> GetCategoriesAsync();

    [Refit.Get("/api/v1/herbs/export")]
    Task<List<HerbDetailDto>> ExportHerbsAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/v1/herbs/import-template")]
    Task<object> ExportTemplateAsync();

    [Refit.Post("/api/v1/herbs/batch-import")]
    Task<HerbBatchImportResultDto> BatchImportAsync([Refit.Body] HerbBatchImportInputDto request);
}

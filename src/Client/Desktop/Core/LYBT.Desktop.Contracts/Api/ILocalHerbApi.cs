using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Herb endpoints.
/// </summary>
public interface ILocalHerbApi
{
    [Refit.Get("/api/herbs")]
    Task<List<HerbListDto>> GetHerbsAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/herbs/{id}")]
    Task<HerbDetailDto> GetHerbByIdAsync(Guid id);

    [Refit.Post("/api/herbs")]
    Task<HerbDetailDto> CreateHerbAsync([Refit.Body] HerbInputDto request);

    [Refit.Put("/api/herbs/{id}")]
    Task<HerbDetailDto> UpdateHerbAsync(Guid id, [Refit.Body] HerbInputDto request);

    [Refit.Delete("/api/herbs/{id}")]
    Task DeleteHerbAsync(Guid id);

    [Refit.Post("/api/herbs/{id}/toggle-status")]
    Task<HerbDetailDto> ToggleStatusAsync(Guid id);

    [Refit.Post("/api/herbs/{id}/restore")]
    Task<HerbDetailDto> RestoreAsync(Guid id);

    [Refit.Post("/api/herbs/batch-delete")]
    Task<BatchOperationResultDto> BatchDeleteAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/herbs/batch-enable")]
    Task<BatchOperationResultDto> BatchEnableAsync([Refit.Body] List<Guid> ids);

    [Refit.Post("/api/herbs/batch-disable")]
    Task<BatchOperationResultDto> BatchDisableAsync([Refit.Body] List<Guid> ids);

    [Refit.Get("/api/herbs/export")]
    Task<List<HerbDetailDto>> ExportHerbsAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/herbs/import-template")]
    Task<object> ExportTemplateAsync();
}

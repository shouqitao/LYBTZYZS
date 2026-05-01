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
}

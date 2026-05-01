using LYBT.Entities.Herbs;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Herb endpoints.
/// </summary>
public interface ILocalHerbApi
{
    [Refit.Get("/api/herbs")]
    Task<List<Herb>> GetHerbsAsync([Refit.Query] string? keyword = null);

    [Refit.Get("/api/herbs/{id}")]
    Task<Herb> GetHerbByIdAsync(Guid id);

    [Refit.Post("/api/herbs")]
    Task<Herb> CreateHerbAsync([Refit.Body] Herb herb);

    [Refit.Put("/api/herbs/{id}")]
    Task<Herb> UpdateHerbAsync(Guid id, [Refit.Body] Herb herb);

    [Refit.Delete("/api/herbs/{id}")]
    Task DeleteHerbAsync(Guid id);
}

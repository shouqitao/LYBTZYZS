using LYBT.Entities.Registrations;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Registration endpoints.
/// </summary>
public interface ILocalRegistrationApi
{
    [Refit.Get("/api/registrations")]
    Task<List<Registration>> GetRegistrationsAsync([Refit.Query] DateTime? date = null);

    [Refit.Get("/api/registrations/{id}")]
    Task<Registration> GetRegistrationByIdAsync(Guid id);

    [Refit.Post("/api/registrations")]
    Task<Registration> CreateRegistrationAsync([Refit.Body] Registration registration);

    [Refit.Delete("/api/registrations/{id}")]
    Task DeleteRegistrationAsync(Guid id);
}

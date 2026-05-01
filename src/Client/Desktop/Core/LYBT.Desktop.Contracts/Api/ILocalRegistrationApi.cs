using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Contracts.Api;

/// <summary>
/// Local WebAPI Refit interface for Registration endpoints.
/// </summary>
public interface ILocalRegistrationApi
{
    [Refit.Get("/api/registrations")]
    Task<List<RegistrationListDto>> GetRegistrationsAsync([Refit.Query] DateTime? date = null);

    [Refit.Get("/api/registrations/{id}")]
    Task<RegistrationDetailDto> GetRegistrationByIdAsync(Guid id);

    [Refit.Post("/api/registrations")]
    Task<RegistrationDetailDto> CreateRegistrationAsync([Refit.Body] RegistrationInputDto request);

    [Refit.Delete("/api/registrations/{id}")]
    Task DeleteRegistrationAsync(Guid id);
}

// ---------------------------------------------------------------------------
// IApiClient — Unified API Client Abstraction
// ---------------------------------------------------------------------------
// This interface aggregates all domain-specific API sub-interfaces.
// Two implementations exist:
//   - RefitApiClient (Remote mode): uses Refit-generated HTTP clients
//   - HttpClientApiClient (LocalWebAPI mode): uses IHttpClientFactory
//
// The active implementation is determined by ApiMode configuration at startup.
// Mode switching (Remote ↔ Local) is handled internally via IApiRouter.
// ---------------------------------------------------------------------------

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Unified API client that aggregates all domain-specific API sub-interfaces.
/// Replaces direct dependency on individual Refit interfaces (IHerbApi, IPatientApi, etc.)
/// and HttpXxxRepository raw HttpClient usage.
/// </summary>
public interface IApiClient
{
    /// <summary>Authentication endpoints (login, logout, refresh, validate).</summary>
    IApiClientAuth Auth { get; }

    /// <summary>User management endpoints (CRUD, password, profile).</summary>
    IApiClientUsers Users { get; }

    /// <summary>Patient management endpoints (CRUD, import/export, batch operations).</summary>
    IApiClientPatients Patients { get; }

    /// <summary>Herb management endpoints (CRUD, import/export, batch operations).</summary>
    IApiClientHerbs Herbs { get; }

    /// <summary>Formula management endpoints (CRUD, clone, import/export, batch operations).</summary>
    IApiClientFormulas Formulas { get; }

    /// <summary>Medical case endpoints (CRUD, status transitions, prescriptions, audit logs).</summary>
    IApiClientMedicalCases MedicalCases { get; }

    /// <summary>Registration endpoints (CRUD, queue, visit management).</summary>
    IApiClientRegistrations Registrations { get; }
}

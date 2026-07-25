// ---------------------------------------------------------------------------
// IApiClient — Unified API Client Abstraction
// ---------------------------------------------------------------------------
// This interface aggregates all domain-specific API sub-interfaces.
// Two implementations exist:
//   - RefitApiClient (Remote mode): uses Refit-generated HTTP clients
//   - HttpClientApiClient (LocalWebAPI mode): uses IHttpClientFactory
//
// The active implementation is determined by the connection URL at runtime.
// Mode switching (Remote ↔ Local) is handled internally by SwitchingApiClient.
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

    /// <summary>Medical case endpoints (CRUD, status transitions, prescriptions).</summary>
    IApiClientMedicalCases MedicalCases { get; }

    /// <summary>Registration endpoints (CRUD, queue, visit management).</summary>
    IApiClientRegistrations Registrations { get; }

    /// <summary>Report endpoints (daily income, consultations, herb usage).</summary>
    IApiClientReports Reports { get; }

    /// <summary>Server deployment endpoints (upload, restart).</summary>
    IApiClientDeploy Deploy { get; }

    /// <summary>Diagnostics/logging endpoints (status, enable/disable, level).</summary>
    IApiClientDiagnostics Diagnostics { get; }
}

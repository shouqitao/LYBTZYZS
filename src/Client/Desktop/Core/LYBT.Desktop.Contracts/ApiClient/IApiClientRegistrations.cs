// ---------------------------------------------------------------------------
// IApiClientRegistrations — Registration API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IRegistrationApi (remote) and ILocalRegistrationApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Registration;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Registration API sub-interface — CRUD, queue management, visit operations.
/// </summary>
/// <remarks>
/// <para>Combines methods from IRegistrationApi (remote) and ILocalRegistrationApi (local).</para>
/// <para>Remote methods return ApiResponse&lt;T&gt;; local-only methods return raw DTOs.</para>
/// </remarks>
public interface IApiClientRegistrations
{
    /// <summary>
    /// Create a registration (receptionist mode).
    /// US-REG-001: Source=Receptionist, Status=Waiting
    /// </summary>
    /// <param name="request">Registration input data.</param>
    Task<ApiResponse<RegistrationDetailDto>> CreateAsync(RegistrationInputDto request);

    /// <summary>
    /// Get registration detail by ID.
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task<ApiResponse<RegistrationDetailDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get registration list with pagination and filters.
    /// US-REG-007: Supports date range, patient, doctor filtering.
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <param name="startDate">Start date filter (optional).</param>
    /// <param name="endDate">End date filter (optional).</param>
    /// <param name="patientId">Patient ID filter (optional).</param>
    /// <param name="doctorId">Doctor ID filter (optional).</param>
    Task<ApiResponse<PagedResult<RegistrationListDto>>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        Guid? patientId = null,
        Guid? doctorId = null);

    /// <summary>
    /// Get waiting queue.
    /// US-REG-003: Waiting status, ordered by registration time ascending.
    /// </summary>
    /// <param name="doctorId">Doctor ID filter (optional).</param>
    Task<ApiResponse<List<RegistrationListDto>>> GetQueueAsync(Guid? doctorId = null);

    /// <summary>
    /// Start visit — transition Registration to InProgress.
    /// US-REG-003 acceptance criteria #4.
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task<ApiResponse<Guid>> StartVisitAsync(Guid id);

    /// <summary>
    /// Cancel a registration.
    /// US-REG-004: Only Waiting status can be cancelled.
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task<ApiResponse> CancelAsync(Guid id);

    // ========== Local-only methods ==========

    /// <summary>
    /// Get registration list (local mode, simple list without pagination wrapper).
    /// </summary>
    /// <param name="date">Date filter (optional).</param>
    Task<List<RegistrationListDto>> GetRegistrationsAsync(DateTime? date = null);

    /// <summary>
    /// Quick visit — create registration and start visit in one call (local mode only).
    /// </summary>
    /// <param name="request">Quick visit input data.</param>
    Task<QuickVisitResultDto> QuickVisitAsync(QuickVisitInputDto request);

    /// <summary>
    /// Delete a registration (local mode only).
    /// </summary>
    /// <param name="id">Registration ID.</param>
    Task DeleteRegistrationAsync(Guid id);
}

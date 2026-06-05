// ---------------------------------------------------------------------------
// IApiClientPatients — Patient Management API Sub-Interface
// ---------------------------------------------------------------------------
// Unified interface combining IPatientApi (remote) and ILocalPatientApi (local).
// No Refit attributes — implementations route to the correct backend.
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.ApiClient;

/// <summary>
/// Patient management API sub-interface — CRUD, import/export, batch operations.
/// </summary>
/// <remarks>
/// <para>Combines methods from IPatientApi (remote) and ILocalPatientApi (local).</para>
/// <para>Note: Patient entity has no Status field, so there is no BatchEnable/BatchDisable.</para>
/// </remarks>
public interface IApiClientPatients
{
    /// <summary>
    /// Get patient list with pagination.
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null);

    /// <summary>
    /// Get patient detail by ID.
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id);

    /// <summary>
    /// Create a new patient.
    /// </summary>
    /// <param name="request">Patient input data.</param>
    Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request);

    /// <summary>
    /// Update an existing patient.
    /// </summary>
    /// <param name="id">Patient ID.</param>
    /// <param name="request">Patient input data.</param>
    Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request);

    /// <summary>
    /// Delete a patient (soft delete).
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse> DeletePatientAsync(Guid id);

    /// <summary>
    /// Batch import patient data.
    /// Issue #2004 Task 2.11
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request);

    /// <summary>
    /// Download patient import template.
    /// Epic #1934 FR-002
    /// </summary>
    /// <returns>Excel template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync();

    /// <summary>
    /// Export patient data to Excel.
    /// Epic #1934 FR-003
    /// </summary>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <returns>Excel file stream with patient data.</returns>
    Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null);

    /// <summary>
    /// Restore a soft-deleted patient.
    /// Note: Patient entity has no Status field, so no ToggleStatus method.
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id);

    /// <summary>
    /// Batch delete patients.
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// Toggle patient status (enable/disable).
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id);
}

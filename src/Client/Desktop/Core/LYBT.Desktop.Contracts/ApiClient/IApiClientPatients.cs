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
/// 患者管理 API 子接口——CRUD、导入/导出、批量操作。
/// </summary>
/// <remarks>
/// <para>Combines methods from IPatientApi (remote) and ILocalPatientApi (local).</para>
/// <para>Note: Patient entity has no Status field, so there is no BatchEnable/BatchDisable.</para>
/// </remarks>
public interface IApiClientPatients
{
    /// <summary>
    /// 分页获取患者列表。
    /// </summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Page size (default 20).</param>
    /// <param name="keyword">Search keyword (optional).</param>
    Task<ApiResponse<PagedResult<PatientListDto>>> GetPatientsAsync(
        int page = 1,
        int pageSize = 20,
        string? keyword = null);

    /// <summary>
    /// 按 ID 获取患者详情。
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid id);

    /// <summary>
    /// 创建新患者。
    /// </summary>
    /// <param name="request">Patient input data.</param>
    Task<ApiResponse<PatientDetailDto>> CreatePatientAsync(PatientInputDto request);

    /// <summary>
    /// 更新现有患者。
    /// </summary>
    /// <param name="id">Patient ID.</param>
    /// <param name="request">Patient input data.</param>
    Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, PatientInputDto request);

    /// <summary>
    /// 删除患者（软删除）。
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse> DeletePatientAsync(Guid id);

    /// <summary>
    /// 批量导入患者数据。
    /// Issue #2004 Task 2.11
    /// </summary>
    /// <param name="request">Batch import input data.</param>
    Task<ApiResponse<PatientBatchImportResultDto>> BatchImportAsync(PatientBatchImportInputDto request);

    /// <summary>
    /// 下载患者导入模板。
    /// Epic #1934 FR-002
    /// </summary>
    /// <returns>Excel template file stream.</returns>
    Task<HttpResponseMessage> ExportTemplateAsync();

    /// <summary>
    /// 将患者数据导出到 Excel。
    /// Epic #1934 FR-003
    /// </summary>
    /// <param name="keyword">Search keyword (optional).</param>
    /// <returns>Excel file stream with patient data.</returns>
    Task<HttpResponseMessage> ExportPatientsAsync(string? keyword = null);

    /// <summary>
    /// 批量删除患者。
    /// </summary>
    /// <param name="request">Batch delete input with IDs.</param>
    Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync(BatchDeleteInputDto request);

    /// <summary>
    /// 切换患者状态（启用/禁用）。
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse<PatientDetailDto>> ToggleStatusAsync(Guid id);

    /// <summary>
    /// 恢复软删除的患者。
    /// </summary>
    /// <param name="id">Patient ID.</param>
    Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id);
}

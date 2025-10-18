using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 处方API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPrescriptionApi
    {
        /// <summary>
        /// 获取处方列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PagedResult<PrescriptionDto>>> GetPrescriptionsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDto>> GetPrescriptionByIdAsync(Guid id);

        /// <summary>
        /// 创建处方
        /// </summary>
        [Refit.Post("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PrescriptionDto>> CreatePrescriptionAsync([Refit.Body] PrescriptionCreateDto request);

        /// <summary>
        /// 更新处方
        /// </summary>
        [Refit.Put("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, [Refit.Body] PrescriptionUpdateDto request);

        /// <summary>
        /// 删除处方
        /// </summary>
        [Refit.Delete("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<ApiResponse>> DeletePrescriptionAsync(Guid id);

        /// <summary>
        /// 根据医案ID获取处方列表
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/medicalcase/{medicalCaseId}")]
        Task<Refit.ApiResponse<List<PrescriptionDto>>> GetPrescriptionsByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者最近处方列表 (Issue #1371 ENTRY-13)
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/patient/{patientId}/recent")]
        Task<Refit.ApiResponse<List<PrescriptionSearchResultDto>>> GetPatientRecentPrescriptionsAsync(
            Guid patientId,
            [Refit.Query] int count = 5);
    }
}

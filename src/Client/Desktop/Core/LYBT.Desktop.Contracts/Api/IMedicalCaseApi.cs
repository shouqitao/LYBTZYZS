using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 医疗案例API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseApi
    {
        /// <summary>
        /// 获取医疗案例列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/medicalcases")]
        Task<ApiResponse<PagedResult<MedicalCaseDto>>> GetMedicalCasesAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDto>> GetMedicalCaseByIdAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/by-patient/{patientId}")]
        Task<ApiResponse<List<MedicalCaseDto>>> GetMedicalCasesByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取完整的医疗案例（包含所有关联数据）
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}/with-details")]
        Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcases")]
        Task<ApiResponse<MedicalCaseDto>> CreateMedicalCaseAsync([Refit.Body] MedicalCaseCreateDto request);

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗和可选处方）
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/with-details")]
        Task<ApiResponse<MedicalCaseDto>> CreateMedicalCaseWithDetailsAsync([Refit.Body] MedicalCaseWithDetailsCreateDto request);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDto>> UpdateMedicalCaseAsync(Guid id, [Refit.Body] MedicalCaseUpdateDto request);

        /// <summary>
        /// 删除医疗案例
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<ApiResponse>> DeleteMedicalCaseAsync(Guid id);
    }
}

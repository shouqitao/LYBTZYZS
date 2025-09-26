using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Shared.Interfaces.Api
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
        Task<Refit.ApiResponse<PagedResult<MedicalCaseDto>>> GetMedicalCasesAsync(
            [Refit.Query] int pageIndex = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? searchTerm = null);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}")]
        Task<Refit.ApiResponse<MedicalCaseDto>> GetMedicalCaseByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcases")]
        Task<Refit.ApiResponse<MedicalCaseDto>> CreateMedicalCaseAsync([Refit.Body] MedicalCaseCreateDto request);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}")]
        Task<Refit.ApiResponse<MedicalCaseDto>> UpdateMedicalCaseAsync(Guid id, [Refit.Body] MedicalCaseUpdateDto request);

        /// <summary>
        /// 删除医疗案例
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}")]
        Task<Refit.ApiResponse<object>> DeleteMedicalCaseAsync(Guid id);
    }
}
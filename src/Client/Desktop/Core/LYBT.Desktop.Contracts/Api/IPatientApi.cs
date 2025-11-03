using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
namespace LYBT.Desktop.Contracts.Api
{

    /// <summary>
    /// 患者API客户端接口 - UltraThink统一标准
    /// 移动到shared层以确保前后端契约一致性
    /// </summary>
    public interface IPatientApi
    {
        /// <summary>
        /// 获取患者列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/patients")]
        Task<ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [Refit.Get("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);

        /// <summary>
        /// 创建患者
        /// </summary>
        [Refit.Post("/api/v1/patients")]
        Task<ApiResponse<PatientDto>> CreatePatientAsync([Refit.Body] PatientInputDto request);

        /// <summary>
        /// 更新患者
        /// </summary>
        [Refit.Put("/api/v1/patients/{id}")]
        Task<ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, [Refit.Body] PatientInputDto request);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/patients/{id}")]
        Task<ApiResponse<ApiResponse>> DeletePatientAsync(Guid id);
    }
}

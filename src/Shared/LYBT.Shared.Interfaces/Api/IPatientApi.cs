using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Refit;

namespace LYBT.Shared.Interfaces.Api
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
        [Get("/api/v1/patients")]
        Task<Refit.ApiResponse<PagedResult<PatientDto>>> GetPatientsAsync(
            [Query] int pageIndex = 1,
            [Query] int pageSize = 20,
            [Query] string? searchTerm = null);

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [Get("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<PatientDto>> GetPatientByIdAsync(Guid id);

        /// <summary>
        /// 创建患者
        /// </summary>
        [Post("/api/v1/patients")]
        Task<Refit.ApiResponse<PatientDto>> CreatePatientAsync([Body] PatientCreateDto request);

        /// <summary>
        /// 更新患者
        /// </summary>
        [Put("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<PatientDto>> UpdatePatientAsync(Guid id, [Body] PatientUpdateDto request);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [Delete("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<object>> DeletePatientAsync(Guid id);

        /// <summary>
        /// 获取患者处方列表
        /// </summary>
        [Get("/api/v1/patients/{id}/prescriptions")]
        Task<Refit.ApiResponse<List<PrescriptionDto>>> GetPrescriptionsAsync(Guid id);

        /// <summary>
        /// 切换患者状态
        /// </summary>
        [Patch("/api/v1/patients/{id}/toggle-status")]
        Task<Refit.ApiResponse<object>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        [Get("/api/v1/patients/active")]
        Task<Refit.ApiResponse<List<PatientDto>>> GetActivePatientsAsync();

        /// <summary>
        /// 批量导入患者数据
        /// </summary>
        [Post("/api/v1/patients/import")]
        Task<Refit.ApiResponse<int>> ImportPatientsAsync([Body] List<PatientImportDto> patients);

        /// <summary>
        /// 导出患者数据
        /// </summary>
        [Get("/api/v1/patients/export")]
        Task<Refit.ApiResponse<List<PatientDto>>> ExportPatientsAsync();

        /// <summary>
        /// 获取患者导入模板
        /// </summary>
        [Get("/api/v1/patients/import-template")]
        Task<Refit.ApiResponse<byte[]>> GetImportTemplateAsync();
    }
}

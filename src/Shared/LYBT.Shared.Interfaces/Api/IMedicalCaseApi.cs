using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
namespace LYBT.Shared.Interfaces.Api
{

    /// <summary>
    /// 医疗案例API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IMedicalCaseApi
    {

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        [Refit.Get("/api/v1/medicalcase")]
        Task<Refit.ApiResponse<PagedResult<MedicalCaseDto>>> GetPagedAsync(
            [Refit.Query] int pageIndex = 1,
            [Refit.Query] int pageSize = 20);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/{id}")]
        Task<Refit.ApiResponse<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcase")]
        Task<Refit.ApiResponse<MedicalCaseDto>> CreateAsync([Refit.Body] MedicalCaseCreateDto createDto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [Refit.Put("/api/v1/medicalcase/{id}")]
        Task<Refit.ApiResponse<bool>> UpdateAsync(Guid id, [Refit.Body] MedicalCaseEditDto editDto);

        /// <summary>
        /// 获取患者的医疗案例列表
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/patient/{patientId}")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/user/{userId}/today")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> GetTodayByUserIdAsync(Guid userId);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        [Refit.Put("/api/v1/medicalcase/{id}/status")]
        Task<Refit.ApiResponse<bool>> UpdateStatusAsync(Guid id, [Refit.Body] MedicalCaseStatus status);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/medicalcase/{id}")]
        Task<Refit.ApiResponse<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 获取患者活跃医疗案例
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/patient/{patientId}/active")]
        Task<Refit.ApiResponse<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcase/{id}/complete")]
        Task<Refit.ApiResponse<bool>> CompleteAsync(Guid id, [Refit.Body] CompleteMedicalCaseDto dto);

        /// <summary>
        /// 暂停医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcase/{id}/suspend")]
        Task<Refit.ApiResponse<bool>> SuspendAsync(Guid id, [Refit.Body] SuspendMedicalCaseDto dto);

        /// <summary>
        /// 恢复医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcase/{id}/resume")]
        Task<Refit.ApiResponse<bool>> ResumeAsync(Guid id);

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        [Refit.Post("/api/v1/medicalcase/{id}/archive")]
        Task<Refit.ApiResponse<bool>> ArchiveAsync(Guid id, [Refit.Body] ArchiveMedicalCaseDto dto);

        /// <summary>
        /// 搜索医疗案例
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/search")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> SearchAsync([Refit.Query] string keyword);

        /// <summary>
        /// 获取医疗案例统计信息
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/statistics")]
        Task<Refit.ApiResponse<object>> GetStatisticsAsync([Refit.Query] DateTime? startDate = null, [Refit.Query] DateTime? endDate = null);

        /// <summary>
        /// 获取医疗案例历史记录
        /// </summary>
        [Refit.Get("/api/v1/medicalcase/{id}/history")]
        Task<Refit.ApiResponse<List<object>>> GetHistoryAsync(Guid id);
    }

}

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
namespace LYBT.Shared.Interfaces.Api
{

    /// <summary>
    /// 处方API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IPrescriptionApi
    {

        /// <summary>
        /// 获取处方列表（支持分页和查询）
        /// </summary>
        [Refit.Get("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PagedResult<PrescriptionDto>>> GetListAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] string? patientName = null,
            [Refit.Query] string? doctorName = null,
            [Refit.Query] string? diagnosis = null,
            [Refit.Query] PrescriptionStatus? status = null,
            [Refit.Query] DateTime? startDate = null,
            [Refit.Query] DateTime? endDate = null,
            [Refit.Query] int? minDosageCount = null,
            [Refit.Query] int? maxDosageCount = null);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Refit.Get("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建处方
        /// </summary>
        [Refit.Post("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PrescriptionDto>> CreatePrescriptionAsync([Refit.Body] PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方
        /// </summary>
        [Refit.Put("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, [Refit.Body] PrescriptionEditDto dto);

        /// <summary>
        /// 删除处方
        /// </summary>
        [Refit.Delete("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<bool>> DeletePrescriptionAsync(Guid id);

        /// <summary>
        /// 作废处方
        /// </summary>
        [Refit.Post("/api/v1/prescriptions/void/{id}")]
        Task<Refit.ApiResponse<PrescriptionDto>> CancelPrescriptionAsync(Guid id);
    }
}

using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Refit;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 处方API服务接口
    /// </summary>
    public interface IPrescriptionApiService
    {
        /// <summary>
        /// 创建处方
        /// </summary>
        [Post("/api/v1/Prescriptions")]
        Task<ApiResponse<PrescriptionDto>> CreatePrescriptionAsync([Body] PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方
        /// </summary>
        [Put("/api/v1/Prescriptions")]
        Task<ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync([Body] PrescriptionEditDto dto);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Get("/api/v1/Prescriptions/{id}")]
        Task<ApiResponse<PrescriptionDetailDto>> GetPrescriptionAsync(Guid id);

        /// <summary>
        /// 获取处方列表
        /// </summary>
        [Get("/api/v1/Prescriptions")]
        Task<ApiResponse<PaginatedResult<PrescriptionDto>>> GetPrescriptionsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? patientName = null,
            [Query] string? doctorName = null,
            [Query] string? diagnosis = null);

        /// <summary>
        /// 删除处方
        /// </summary>
        [Delete("/api/v1/Prescriptions/{id}")]
        Task<ApiResponse<bool>> DeletePrescriptionAsync(Guid id);
    }
}
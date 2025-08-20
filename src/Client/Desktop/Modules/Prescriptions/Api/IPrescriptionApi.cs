using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Modules.Prescriptions.Api
{
    /// <summary>
    /// 处方API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IPrescriptionApi
    {
        /// <summary>
        /// 获取处方列表（支持分页和查询）
        /// </summary>
        [Get("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PagedData<PrescriptionDto>>> GetListAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? patientName = null,
            [Query] string? doctorName = null,
            [Query] string? diagnosis = null,
            [Query] PrescriptionStatus? status = null,
            [Query] DateTime? startDate = null,
            [Query] DateTime? endDate = null,
            [Query] int? minDosageCount = null,
            [Query] int? maxDosageCount = null);

        /// <summary>
        /// 获取处方详情
        /// </summary>
        [Get("/api/v1/prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建处方
        /// </summary>
        [Post("/api/v1/prescriptions")]
        Task<Refit.ApiResponse<PrescriptionDto>> CreatePrescriptionAsync([Body] PrescriptionCreateDto dto);

        /// <summary>
        /// 更新处方
        /// </summary>
        [Put("/api/v1/Prescriptions/{id}")]
        Task<Refit.ApiResponse<PrescriptionDto>> UpdatePrescriptionAsync(Guid id, [Body] PrescriptionEditDto dto);

        /// <summary>
        /// 删除处方
        /// </summary>
        [Delete("/api/v1/Prescriptions/{id}")]
        Task<Refit.ApiResponse<bool>> DeletePrescriptionAsync(Guid id);

        /// <summary>
        /// 作废处方
        /// </summary>
        [Post("/api/v1/Prescriptions/void/{id}")]
        Task<Refit.ApiResponse<PrescriptionDto>> CancelPrescriptionAsync(Guid id);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Modules.MedicalCase.Api
{
    /// <summary>
    /// 医疗案例API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IMedicalCaseApi
    {
        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        [Get("/api/v1/medicalcase")]
        Task<Refit.ApiResponse<PagedResult<MedicalCaseDto>>> GetPagedAsync(
            [Query] int pageIndex = 1,
            [Query] int pageSize = 20);

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        [Get("/api/v1/medicalcase/{id}")]
        Task<Refit.ApiResponse<MedicalCaseDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        [Post("/api/v1/medicalcase")]
        Task<Refit.ApiResponse<MedicalCaseDto>> CreateAsync([Body] MedicalCaseCreateDto createDto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        [Put("/api/v1/medicalcase/{id}")]
        Task<Refit.ApiResponse<bool>> UpdateAsync(Guid id, [Body] MedicalCaseEditDto editDto);

        /// <summary>
        /// 获取患者的医疗案例列表
        /// </summary>
        [Get("/api/v1/medicalcase/patient/{patientId}")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        [Get("/api/v1/medicalcase/user/{userId}/today")]
        Task<Refit.ApiResponse<List<MedicalCaseDto>>> GetTodayByUserIdAsync(Guid userId);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        [Put("/api/v1/medicalcase/{id}/status")]
        Task<Refit.ApiResponse<bool>> UpdateStatusAsync(Guid id, [Body] MedicalCaseStatus status);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        [Delete("/api/v1/medicalcase/{id}")]
        Task<Refit.ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
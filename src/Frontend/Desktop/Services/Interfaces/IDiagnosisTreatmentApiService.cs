using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 诊疗API服务接口
    /// </summary>
    public interface IDiagnosisTreatmentApiService
    {
        /// <summary>
        /// 获取诊疗列表
        /// </summary>
        [Get("/api/v1/diagnosistreatment")]
        Task<Refit.ApiResponse<List<DiagnosisTreatmentDto>>> GetListAsync();

        /// <summary>
        /// 分页获取诊疗列表
        /// </summary>
        [Get("/api/v1/diagnosistreatment/paged")]
        Task<Refit.ApiResponse<PaginatedResult<DiagnosisTreatmentDto>>> GetPagedListAsync([Query] PaginationRequest query);

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        [Get("/api/v1/diagnosistreatment/{id}")]
        Task<Refit.ApiResponse<DiagnosisTreatmentDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 新增诊疗
        /// </summary>
        [Post("/api/v1/diagnosistreatment")]
        Task<Refit.ApiResponse<object>> AddAsync([Body] DiagnosisTreatmentCreateDto dto);

        /// <summary>
        /// 编辑诊疗
        /// </summary>
        [Put("/api/v1/diagnosistreatment")]
        Task<Refit.ApiResponse<object>> UpdateAsync([Body] DiagnosisTreatmentEditDto dto);

        /// <summary>
        /// 删除诊疗
        /// </summary>
        [Delete("/api/v1/diagnosistreatment/{id}")]
        Task<Refit.ApiResponse<object>> DeleteAsync(Guid id);
    }
}
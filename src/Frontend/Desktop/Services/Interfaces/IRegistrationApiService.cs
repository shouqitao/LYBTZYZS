using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using Refit;

namespace LYBT.WPF.Client.Services.Interfaces {
    /// <summary>
    /// 挂号API服务接口
    /// </summary>
    public interface IRegistrationApiService {
        /// <summary>
        /// 分页查询挂号记录
        /// </summary>
        [Post("/api/v1/registration/paged")]
        Task<Refit.ApiResponse<PaginatedResult<RegistrationDto>>> GetPagedRegistrationsAsync([Body] RegistrationPagedQueryDto query);

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        [Get("/api/v1/registration")]
        Task<Refit.ApiResponse<List<RegistrationDto>>> GetRegistrationsAsync();

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        [Get("/api/v1/registration/{id}")]
        Task<Refit.ApiResponse<RegistrationDetailDto>> GetRegistrationByIdAsync(Guid id);

        /// <summary>
        /// 创建挂号
        /// </summary>
        [Post("/api/v1/registration")]
        Task<Refit.ApiResponse<object>> CreateRegistrationAsync([Body] RegistrationCreateDto registration);

        /// <summary>
        /// 更新挂号
        /// </summary>
        [Put("/api/v1/registration")]
        Task<Refit.ApiResponse<object>> UpdateRegistrationAsync([Body] RegistrationEditDto registration);

        /// <summary>
        /// 删除挂号
        /// </summary>
        [Delete("/api/v1/registration/{id}")]
        Task<Refit.ApiResponse<object>> DeleteRegistrationAsync(Guid id);

        /// <summary>
        /// 取消挂号
        /// </summary>
        [Post("/api/v1/registration/{id}/cancel")]
        Task<Refit.ApiResponse<object>> CancelRegistrationAsync(Guid id);


        /// <summary>
        /// 获取医生可预约时间段
        /// </summary>
        [Get("/api/v1/registration/doctor/{doctorId}/available-slots")]
        Task<Refit.ApiResponse<List<TimeSlotDto>>> GetAvailableSlotsAsync(Guid doctorId, [Query] DateTime date);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 医生API服务接口
    /// </summary>
    public interface IDoctorsApiService
    {
        /// <summary>
        /// 分页查询医生列表
        /// </summary>
        [Post("/api/v1/doctors/paged")]
        Task<Refit.ApiResponse<PaginatedResult<DoctorDto>>> GetPagedAsync([Body] DoctorQueryDto query);

        /// <summary>
        /// RESTful 获取医生列表
        /// </summary>
        [Get("/api/v1/doctors")]
        Task<Refit.ApiResponse<PaginatedResult<DoctorDto>>> GetDoctorsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? realName = null,
            [Query] string? specialty = null,
            [Query] string? licenseNumber = null,
            [Query] string? phoneNumber = null,
            [Query] DoctorTitle? title = null,
            [Query] DoctorStatus? status = null,
            [Query] DoctorWorkStatus? workStatus = null,
            [Query] bool? isActive = null);

        /// <summary>
        /// 搜索医生
        /// </summary>
        [Get("/api/v1/doctors/search")]
        Task<Refit.ApiResponse<List<DoctorDto>>> SearchAsync([Query] string keyword = "");

        /// <summary>
        /// 获取所有在职医生列表
        /// </summary>
        [Get("/api/v1/doctors/active")]
        Task<Refit.ApiResponse<List<DoctorDto>>> GetActiveDoctorsAsync();

        /// <summary>
        /// 根据ID获取医生详情
        /// </summary>
        [Get("/api/v1/doctors/{id}")]
        Task<Refit.ApiResponse<DoctorDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据用户ID获取医生详情
        /// </summary>
        [Get("/api/v1/doctors/by-user/{userId}")]
        Task<Refit.ApiResponse<DoctorDetailDto>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 新增医生
        /// </summary>
        [Post("/api/v1/doctors/add")]
        Task<Refit.ApiResponse<DoctorDetailDto>> AddAsync([Body] DoctorDetailDto dto);

        /// <summary>
        /// 更新医生信息
        /// </summary>
        [Put("/api/v1/doctors/{id}")]
        Task<Refit.ApiResponse<DoctorDetailDto>> UpdateAsync(Guid id, [Body] DoctorDetailDto dto);

        /// <summary>
        /// 禁用医生
        /// </summary>
        [Patch("/api/v1/doctors/{id}/disable")]
        Task<Refit.ApiResponse<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 启用医生
        /// </summary>
        [Patch("/api/v1/doctors/{id}/enable")]
        Task<Refit.ApiResponse<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 切换医生状态
        /// </summary>
        [Patch("/api/v1/doctors/{id}/toggle-status")]
        Task<Refit.ApiResponse<object>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 批量禁用医生
        /// </summary>
        [Patch("/api/v1/doctors/batch-disable")]
        Task<Refit.ApiResponse<int>> BatchDisableAsync([Body] BatchIdsDto dto);

        /// <summary>
        /// 批量启用医生
        /// </summary>
        [Patch("/api/v1/doctors/batch-enable")]
        Task<Refit.ApiResponse<int>> BatchEnableAsync([Body] BatchIdsDto dto);

        /// <summary>
        /// 检查用户是否已关联医生档案
        /// </summary>
        [Get("/api/v1/doctors/check-user-link/{userId}")]
        Task<Refit.ApiResponse<bool>> CheckUserLinkAsync(Guid userId);

        /// <summary>
        /// 获取用户角色枚举列表
        /// </summary>
        [Get("/api/v1/doctors/roles")]
        Task<Refit.ApiResponse<object>> GetRolesAsync();

        // ======================== RESTful 标准接口 ========================

        /// <summary>
        /// 创建新医生 (RESTful POST)
        /// </summary>
        [Post("/api/v1/doctors")]
        Task<Refit.ApiResponse<DoctorDto>> CreateDoctorAsync([Body] DoctorDetailDto dto);

        /// <summary>
        /// 删除医生 (RESTful DELETE) - 实际执行软删除
        /// </summary>
        [Delete("/api/v1/doctors/{id}")]
        Task<Refit.ApiResponse<object>> DeleteDoctorAsync(Guid id);
    }
}
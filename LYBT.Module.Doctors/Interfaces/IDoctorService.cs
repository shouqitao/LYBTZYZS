using LYBT.Common.Models;
using LYBT.Common.Responses;
using LYBT.Module.Doctors.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生业务服务接口
    /// </summary>
    public interface IDoctorService {

        /// <summary>
        /// 根据ID获取医生详情
        /// </summary>
        Task<ApiResponse<DoctorDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据用户ID获取医生详情
        /// </summary>
        Task<ApiResponse<DoctorDetailDto>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 搜索医生
        /// </summary>
        Task<ApiResponse<List<DoctorDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 分页获取医生列表
        /// </summary>
        Task<ApiResponse<PagedResultDto<DoctorDto>>> GetPagedAsync(DoctorQueryDto query);

        /// <summary>
        /// 新增医生
        /// </summary>
        Task<ApiResponse<bool>> AddAsync(DoctorDetailDto dto);

        /// <summary>
        /// 更新医生信息
        /// </summary>
        Task<ApiResponse<bool>> UpdateAsync(DoctorDetailDto dto);

        /// <summary>
        /// 禁用医生
        /// </summary>
        Task<ApiResponse<bool>> DisableAsync(Guid id);

        /// <summary>
        /// 启用医生
        /// </summary>
        Task<ApiResponse<bool>> EnableAsync(Guid id);

        /// <summary>
        /// 批量禁用医生
        /// </summary>
        Task<ApiResponse<int>> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用医生
        /// </summary>
        Task<ApiResponse<int>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 获取所有在职医生列表（不分页）
        /// </summary>
        Task<ApiResponse<List<DoctorDto>>> GetActiveDoctorsAsync();

        /// <summary>
        /// 检查用户是否已关联医生档案
        /// </summary>
        Task<ApiResponse<bool>> IsUserLinkedToDoctorAsync(Guid userId);
    }
}
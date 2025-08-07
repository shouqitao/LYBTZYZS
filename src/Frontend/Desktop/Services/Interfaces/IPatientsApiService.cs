using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 患者API服务接口
    /// </summary>
    public interface IPatientsApiService
    {
        // 移除重复的新增患者接口，统一使用RESTful POST接口

        /// <summary>
        /// 快速创建患者档案
        /// </summary>
        [Post("/api/v1/patients/quick")]
        Task<Refit.ApiResponse<object>> QuickCreateAsync([Body] QuickPatientCreateDto dto);

        // 移除单独的Enable/Disable接口，统一使用ToggleStatus

        /// <summary>
        /// 切换患者档案状态
        /// </summary>
        [Patch("/api/v1/patients/{id}/toggle-status")]
        Task<Refit.ApiResponse<object>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 获取全部患者
        /// </summary>
        [Get("/api/v1/patients/all")]
        Task<Refit.ApiResponse<List<PatientDetailDto>>> GetAllAsync();

        // 移除重复的分页查询接口，统一使用RESTful GET接口

        // 移除未实现的批量操作接口

        /// <summary>
        /// 搜索患者档案
        /// </summary>
        [Get("/api/v1/patients/search")]
        Task<Refit.ApiResponse<List<PatientDetailDto>>> SearchAsync([Query] string keyword = "");

        // 移除未实现的导入功能

        /// <summary>
        /// 导出患者档案数据
        /// </summary>
        [Get("/api/v1/patients/export")]
        Task<Refit.ApiResponse<List<PatientDetailDto>>> ExportAsync();

        // 移除未实现的历史病历功能

        /// <summary>
        /// 获取启用的患者档案列表
        /// </summary>
        [Get("/api/v1/patients/active")]
        Task<Refit.ApiResponse<List<PatientDetailDto>>> GetActivePatientsAsync();

        /// <summary>
        /// 查询或创建患者档案
        /// </summary>
        [Post("/api/v1/patients/find-or-create")]
        Task<Refit.ApiResponse<PatientDetailDto>> FindOrCreateAsync([Body] PatientDetailDto dto);

        // ======================== RESTful 标准接口 ========================

        /// <summary>
        /// 获取所有患者列表 (RESTful GET)
        /// </summary>
        [Get("/api/v1/patients")]
        Task<Refit.ApiResponse<PaginatedResult<PatientDetailDto>>> GetPatientsAsync(
            [Query] int page = 1,
            [Query] int pageSize = 20,
            [Query] string? keyword = null,
            [Query] string? name = null,
            [Query] string? phoneNumber = null,
            [Query] string? idNumber = null,
            [Query] string? address = null,
            [Query] Gender? gender = null,
            [Query] int? minAge = null,
            [Query] int? maxAge = null,
            [Query] PatientStatus? status = null);

        /// <summary>
        /// 创建新患者 (RESTful POST)
        /// </summary>
        [Post("/api/v1/patients")]
        Task<Refit.ApiResponse<PatientDetailDto>> CreatePatientAsync([Body] PatientDetailDto dto);

        /// <summary>
        /// 根据ID获取患者 (RESTful GET)
        /// </summary>
        [Get("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<PatientDetailDto>> GetPatientAsync(Guid id);

        /// <summary>
        /// 更新患者信息 (RESTful PUT)
        /// </summary>
        [Put("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid id, [Body] PatientDetailDto dto);

        /// <summary>
        /// 删除患者 (RESTful DELETE) - 实际执行软删除
        /// </summary>
        [Delete("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<object>> DeletePatientAsync(Guid id);
    }
}
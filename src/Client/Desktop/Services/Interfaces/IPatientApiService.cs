using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Desktop.Core.Models;

namespace LYBT.Desktop.Services.Interfaces
{
    /// <summary>
    /// 患者API服务接口 - 统一标准
    /// </summary>
    public interface IPatientApiService
    {
        /// <summary>
        /// 获取患者列表（支持分页和查询）
        /// </summary>
        [Get("/api/v1/patients")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<PagedData<PatientDto>>>> GetPatientsAsync(
            [Query] int pageIndex = 1,
            [Query] int pageSize = 20,
            [Query] string? searchTerm = null);
        
        /// <summary>
        /// 获取患者详情
        /// </summary>
        [Get("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<PatientDto>>> GetPatientByIdAsync(Guid id);
        
        /// <summary>
        /// 创建患者
        /// </summary>
        [Post("/api/v1/patients")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<PatientDto>>> CreatePatientAsync([Body] PatientCreateDto request);
        
        /// <summary>
        /// 更新患者
        /// </summary>
        [Put("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<PatientDto>>> UpdatePatientAsync(Guid id, [Body] PatientUpdateDto request);
        
        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [Delete("/api/v1/patients/{id}")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<object>>> DeletePatientAsync(Guid id);
        
        /// <summary>
        /// 获取患者处方列表
        /// </summary>
        [Get("/api/v1/patients/{id}/prescriptions")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<List<PrescriptionDto>>>> GetPrescriptionsAsync(Guid id);
        
        /// <summary>
        /// 切换患者状态
        /// </summary>
        [Patch("/api/v1/patients/{id}/toggle-status")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<object>>> ToggleStatusAsync(Guid id);
        
        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        [Get("/api/v1/patients/active")]
        Task<Refit.ApiResponse<LYBT.Desktop.Core.Models.ApiResponse<List<PatientDto>>>> GetActivePatientsAsync();
    }
}
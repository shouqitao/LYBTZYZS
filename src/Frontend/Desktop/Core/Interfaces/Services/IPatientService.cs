using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.WPF.Client.Core.Models.Patients;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 新增患者
        /// </summary>
        Task<ApiResponse<object>> AddAsync(PatientDetailDto dto);

        /// <summary>
        /// 编辑患者
        /// </summary>
        Task<ApiResponse<object>> UpdateAsync(PatientDetailDto dto);

        /// <summary>
        /// 启用患者档案
        /// </summary>
        Task<ApiResponse<object>> EnableAsync(Guid id);

        /// <summary>
        /// 禁用患者档案
        /// </summary>
        Task<ApiResponse<object>> DisableAsync(Guid id);

        /// <summary>
        /// 获取患者详情
        /// </summary>
        Task<ApiResponse<PatientDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有患者
        /// </summary>
        Task<ApiResponse<List<PatientDetailDto>>> GetAllAsync();

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ApiResponse<PaginatedResult<PatientDetailDto>>> GetPagedAsync(PatientPagedQueryDto query);

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        Task<ApiResponse<object>> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用患者
        /// </summary>
        Task<ApiResponse<object>> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ApiResponse<List<PatientDetailDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 导入患者数据
        /// </summary>
        Task<ApiResponse<object>> ImportAsync(List<PatientDetailDto> patients);

        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ApiResponse<List<PatientDetailDto>>> ExportAsync();

        /// <summary>
        /// 获取患者历史病历
        /// </summary>
        Task<ApiResponse<List<RecordDto>>> GetHistoryRecordsAsync(Guid patientId);

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        Task<ApiResponse<List<PatientDetailDto>>> GetActivePatientsAsync();

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        Task<ApiResponse<PatientDetailDto>> FindOrCreateAsync(PatientDetailDto dto);

        /// <summary>
        /// 快速搜索患者（根据关键词）
        /// </summary>
        Task<ApiResponse<List<PatientDetailDto>>> QuickSearchAsync(string keyword);
    }
}
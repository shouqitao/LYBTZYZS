using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Desktop.Core.Models.Patients;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 新增患者
        /// </summary>
        Task<ServiceResult> AddAsync(PatientDetailDto dto);

        /// <summary>
        /// 编辑患者
        /// </summary>
        Task<ServiceResult> UpdateAsync(PatientDetailDto dto);

        /// <summary>
        /// 启用患者档案
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);

        /// <summary>
        /// 禁用患者档案
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);

        /// <summary>
        /// 获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有患者
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> GetAllAsync();

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<Models.Common.PagedResult<PatientInfo>> GetPagedAsync(PatientPagedQueryDto query);

        /// <summary>
        /// 批量禁用患者
        /// </summary>
        Task<ServiceResult> BatchDisableAsync(List<Guid> ids);

        /// <summary>
        /// 批量启用患者
        /// </summary>
        Task<ServiceResult> BatchEnableAsync(List<Guid> ids);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 导入患者数据
        /// </summary>
        Task<ServiceResult> ImportAsync(List<PatientDetailDto> patients);

        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> ExportAsync();

        /// <summary>
        /// 获取患者历史病历
        /// </summary>

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> GetActivePatientsAsync();

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        Task<ServiceResult<PatientDetailDto>> FindOrCreateAsync(PatientDetailDto dto);

        /// <summary>
        /// 快速搜索患者（根据关键词）
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> QuickSearchAsync(string keyword);

        /// <summary>
        /// 获取患者列表
        /// </summary>
        Task<List<PatientInfo>> GetListAsync();

        /// <summary>
        /// 创建患者
        /// </summary>
        Task<ServiceResult<PatientDetailDto>> CreateAsync(PatientDetailDto dto);

        /// <summary>
        /// 按姓名或拼音搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> SearchByNameOrPinYinAsync(string keyword);

        /// <summary>
        /// 按电话号码搜索患者（支持后几位）
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> SearchByPhoneAsync(string phone);

        /// <summary>
        /// 按身份证号搜索患者（支持后几位）
        /// </summary>
        Task<ServiceResult<List<PatientDetailDto>>> SearchByIdCardAsync(string idCard);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Desktop.Core.Models.Patients;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口 - UltraThink四层架构（UI层）
    /// 使用PatientInfo模型，避免Dto泄漏到UI层
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 新增患者
        /// </summary>
        Task<ServiceResult> AddAsync(PatientCreateDto dto);

        /// <summary>
        /// 编辑患者
        /// </summary>
        Task<ServiceResult> UpdateAsync(PatientUpdateDto dto);

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
        Task<ServiceResult<PatientInfo>> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有患者
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> GetAllAsync();

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<LYBT.Shared.Models.Contracts.Common.PagedResult<PatientInfo>> GetPagedAsync(PatientPagedQueryDto query);

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
        Task<ServiceResult<List<PatientInfo>>> SearchAsync(string keyword);

        /// <summary>
        /// 导入患者数据
        /// </summary>
        Task<ServiceResult> ImportAsync(List<PatientImportDto> patients);

        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> ExportAsync();

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> GetActivePatientsAsync();

        /// <summary>
        /// 查询或创建患者
        /// </summary>
        Task<ServiceResult<PatientInfo>> FindOrCreateAsync(PatientCreateDto dto);

        /// <summary>
        /// 快速搜索患者（根据关键词）
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> QuickSearchAsync(string keyword);

        /// <summary>
        /// 获取患者列表
        /// </summary>
        Task<List<PatientInfo>> GetListAsync();

        /// <summary>
        /// 创建患者
        /// </summary>
        Task<ServiceResult<PatientInfo>> CreateAsync(PatientCreateDto dto);

        /// <summary>
        /// 按姓名或拼音搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> SearchByNameOrPinYinAsync(string keyword);

        /// <summary>
        /// 按电话号码搜索患者（支持后几位）
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> SearchByPhoneAsync(string phone);

        /// <summary>
        /// 按身份证号搜索患者（支持后几位）
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> SearchByIdCardAsync(string idCard);
    }
}
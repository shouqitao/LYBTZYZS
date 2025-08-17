using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口 - UltraThink统一标准
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query);
        
        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
        
        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
        
        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        /// <summary>
        /// 启用患者
        /// </summary>
        Task<ServiceResult<bool>> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<ServiceResult<bool>> DisableAsync(Guid id);
        
        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard);
        
        /// <summary>
        /// 根据电话号码查找患者
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone);
        
        /// <summary>
        /// 搜索患者（按姓名或身份证）
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
        
        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync();
        
        /// <summary>
        /// 获取患者档案概览
        /// </summary>
        Task<ServiceResult<object>> GetArchiveAsync(Guid id);
        
        /// <summary>
        /// 更新患者档案
        /// </summary>
        Task<ServiceResult<bool>> UpdateArchiveAsync(Guid id, object dto);
        
        /// <summary>
        /// 批量导入患者
        /// </summary>
        Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients);
        
        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 验证患者信息
        /// </summary>
        Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto);
        
        /// <summary>
        /// 获取患者年龄分布统计
        /// </summary>
        Task<ServiceResult<List<object>>> GetAgeStatisticsAsync();
    }
}
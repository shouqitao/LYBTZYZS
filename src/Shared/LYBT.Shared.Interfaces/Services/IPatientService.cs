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
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
        
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
        /// 删除患者（带操作者信息）
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName);
        
        /// <summary>
        /// 启用患者
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);
        
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
        /// 获取所有患者列表
        /// </summary>
        Task<List<PatientDto>> GetAllAsync();

        /// <summary>
        /// 获取可用患者列表
        /// </summary>
        Task<List<PatientDto>> GetActivePatientsAsync();

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        Task<PatientDto?> GetByPhoneNumberAsync(string phoneNumber);

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        Task<PatientDto?> GetByIDNumberAsync(string idNumber);

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        Task<PagedResult<PatientDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query);

        /// <summary>
        /// 检查重复患者
        /// </summary>
        Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber);
        
        
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
        /// 获取导入模板
        /// </summary>
        Task<ServiceResult<byte[]>> GetImportTemplateAsync();
        
    }
}
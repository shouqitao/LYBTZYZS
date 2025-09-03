using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 患者服务接口 - UltraThink双层架构精简标准（小诊所适用）
    /// </summary>
    public interface IPatientService
    {
        #region 查询操作 - QueryService专业负责

        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query);
        
        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
        
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

        #endregion

        #region 业务操作 - BusinessService专业负责

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
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);

        #endregion

        #region 批量操作 - 必需功能（用户明确需求）

        /// <summary>
        /// 批量导入患者
        /// </summary>
        Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients);
        
        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query);

        #endregion
    }
}
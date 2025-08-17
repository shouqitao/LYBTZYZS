using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Patients.Services.Interfaces
{
    /// <summary>
    /// Patient模块核心业务服务接口
    /// UltraThink模块化架构：模块内部服务，不依赖外部SharedServices
    /// </summary>
    public interface IPatientModuleService
    {
        #region 基础CRUD操作
        
        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientInfo>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 根据ID获取患者
        /// </summary>
        Task<ServiceResult<PatientInfo>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 创建患者
        /// </summary>
        Task<ServiceResult<PatientInfo>> CreateAsync(PatientCreateInfo createInfo);
        
        /// <summary>
        /// 更新患者
        /// </summary>
        Task<ServiceResult<PatientInfo>> UpdateAsync(PatientUpdateInfo updateInfo);
        
        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
        
        #endregion
        
        #region 业务特定操作
        
        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientInfo>>> SearchPatientsAsync(PagedQueryBaseDto request);
        
        /// <summary>
        /// 根据关键字搜索患者（姓名、电话）
        /// </summary>
        Task<ServiceResult<IEnumerable<PatientInfo>>> SearchByKeywordAsync(string keyword);
        
        /// <summary>
        /// 验证患者数据
        /// </summary>
        Task<ServiceResult> ValidateAsync(PatientInfo patientInfo);
        
        /// <summary>
        /// 检查电话号码是否已被使用
        /// </summary>
        Task<ServiceResult<bool>> IsPhoneExistsAsync(string phone, Guid? excludeId = null);
        
        /// <summary>
        /// 检查身份证号是否已被使用
        /// </summary>
        Task<ServiceResult<bool>> IsIdCardExistsAsync(string idCard, Guid? excludeId = null);
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 启用患者
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);
        
        /// <summary>
        /// 禁用患者
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);
        
        #endregion
        
        #region 统计查询
        
        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        Task<ServiceResult<PatientStatisticsInfo>> GetStatisticsAsync();
        
        /// <summary>
        /// 获取最近活跃患者
        /// </summary>
        Task<ServiceResult<IEnumerable<PatientInfo>>> GetRecentActiveAsync(int count = 10);
        
        #endregion
        
        #region 导入导出功能
        
        /// <summary>
        /// 导入患者数据
        /// </summary>
        Task<ServiceResult<IEnumerable<PatientInfo>>> ImportAsync(string filePath);
        
        /// <summary>
        /// 导出患者数据
        /// </summary>
        Task<ServiceResult> ExportAsync(IEnumerable<Guid> patientIds, string filePath);
        
        /// <summary>
        /// 生成导入模板
        /// </summary>
        Task<ServiceResult> GenerateImportTemplateAsync(string filePath);
        
        #endregion
    }
    
    /// <summary>
    /// 患者统计信息
    /// </summary>
    public class PatientStatisticsInfo
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int NewThisMonthCount { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public Dictionary<string, int> AgeGroupCounts { get; set; } = new();
    }
}
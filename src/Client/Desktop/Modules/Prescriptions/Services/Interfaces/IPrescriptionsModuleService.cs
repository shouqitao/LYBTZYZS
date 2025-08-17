using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Services.Interfaces
{
    /// <summary>
    /// Prescriptions模块核心业务服务接口
    /// UltraThink模块化架构：封装处方管理模块所有业务逻辑
    /// </summary>
    public interface IPrescriptionsModuleService
    {
        #region 基础CRUD操作
        
        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        Task<ServiceResult<PagedResult<PrescriptionInfo>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<ServiceResult<PrescriptionInfo>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 创建处方
        /// </summary>
        Task<ServiceResult<PrescriptionInfo>> CreateAsync(PrescriptionCreateInfo createInfo);
        
        /// <summary>
        /// 更新处方
        /// </summary>
        Task<ServiceResult<PrescriptionInfo>> UpdateAsync(PrescriptionUpdateInfo updateInfo);
        
        /// <summary>
        /// 删除处方
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 更新处方状态
        /// </summary>
        Task<ServiceResult> UpdateStatusAsync(Guid id, PrescriptionStatus status, string? reason = null);
        
        /// <summary>
        /// 完成处方
        /// </summary>
        Task<ServiceResult> CompletePrescriptionAsync(Guid id);
        
        /// <summary>
        /// 作废处方
        /// </summary>
        Task<ServiceResult> VoidPrescriptionAsync(Guid id, string reason);
        
        /// <summary>
        /// 批量更新状态
        /// </summary>
        Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, PrescriptionStatus status, string? reason = null);
        
        #endregion
        
        #region 查询操作
        
        /// <summary>
        /// 搜索处方
        /// </summary>
        Task<ServiceResult<PagedResult<PrescriptionInfo>>> SearchAsync(PagedQueryBaseDto request);
        
        /// <summary>
        /// 根据患者ID获取处方
        /// </summary>
        Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByPatientIdAsync(Guid patientId);
        
        /// <summary>
        /// 根据医生ID获取处方
        /// </summary>
        Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByDoctorIdAsync(Guid doctorId);
        
        /// <summary>
        /// 根据医疗案例ID获取处方
        /// </summary>
        Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
        
        /// <summary>
        /// 根据状态获取处方
        /// </summary>
        Task<ServiceResult<PagedResult<PrescriptionInfo>>> GetByStatusAsync(PrescriptionStatus status, PagedQueryBaseDto query);
        
        /// <summary>
        /// 获取日期范围内的处方
        /// </summary>
        Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        #endregion
        
        #region 处方项目管理
        
        /// <summary>
        /// 添加处方项目
        /// </summary>
        Task<ServiceResult<PrescriptionItemInfo>> AddPrescriptionItemAsync(Guid prescriptionId, PrescriptionItemCreateInfo itemInfo);
        
        /// <summary>
        /// 更新处方项目
        /// </summary>
        Task<ServiceResult<PrescriptionItemInfo>> UpdatePrescriptionItemAsync(PrescriptionItemUpdateInfo itemInfo);
        
        /// <summary>
        /// 删除处方项目
        /// </summary>
        Task<ServiceResult> DeletePrescriptionItemAsync(Guid itemId);
        
        /// <summary>
        /// 批量添加处方项目
        /// </summary>
        Task<ServiceResult<List<PrescriptionItemInfo>>> BatchAddPrescriptionItemsAsync(Guid prescriptionId, IEnumerable<PrescriptionItemCreateInfo> items);
        
        #endregion
        
        #region 验证操作
        
        /// <summary>
        /// 验证处方信息
        /// </summary>
        Task<ServiceResult> ValidateAsync(PrescriptionInfo prescriptionInfo);
        
        /// <summary>
        /// 验证处方项目
        /// </summary>
        Task<ServiceResult> ValidatePrescriptionItemAsync(PrescriptionItemInfo itemInfo);
        
        /// <summary>
        /// 检查是否可以修改处方
        /// </summary>
        Task<ServiceResult<bool>> CanModifyAsync(Guid id);
        
        /// <summary>
        /// 检查是否可以删除处方
        /// </summary>
        Task<ServiceResult<bool>> CanDeleteAsync(Guid id);
        
        #endregion
        
        #region 统计功能
        
        /// <summary>
        /// 获取处方统计信息
        /// </summary>
        Task<ServiceResult<PrescriptionStatisticsInfo>> GetStatisticsAsync();
        
        /// <summary>
        /// 获取今日处方统计
        /// </summary>
        Task<ServiceResult<PrescriptionStatisticsInfo>> GetTodayStatisticsAsync();
        
        /// <summary>
        /// 获取医生处方统计
        /// </summary>
        Task<ServiceResult<DoctorPrescriptionStatisticsInfo>> GetDoctorStatisticsAsync(Guid doctorId);
        
        /// <summary>
        /// 获取热门药材统计
        /// </summary>
        Task<ServiceResult<IEnumerable<HerbUsageStatisticsInfo>>> GetPopularHerbsAsync(int count = 10);
        
        /// <summary>
        /// 获取费用统计
        /// </summary>
        Task<ServiceResult<PrescriptionCostStatisticsInfo>> GetCostStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        #endregion
        
        #region 模板和复制功能
        
        /// <summary>
        /// 复制处方
        /// </summary>
        Task<ServiceResult<PrescriptionInfo>> CopyPrescriptionAsync(Guid prescriptionId, Guid? newPatientId = null);
        
        /// <summary>
        /// 从验方模板创建处方
        /// </summary>
        Task<ServiceResult<PrescriptionInfo>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId);
        
        /// <summary>
        /// 保存为验方模板
        /// </summary>
        Task<ServiceResult> SaveAsTemplateAsync(Guid prescriptionId, string templateName, string? description = null);
        
        #endregion
        
        #region 业务规则验证
        
        /// <summary>
        /// 检查药材库存是否充足
        /// </summary>
        Task<ServiceResult<List<HerbStockWarningInfo>>> CheckHerbStockAsync(Guid prescriptionId);
        
        /// <summary>
        /// 验证药材配伍禁忌
        /// </summary>
        Task<ServiceResult<List<HerbCompatibilityWarningInfo>>> CheckHerbCompatibilityAsync(Guid prescriptionId);
        
        /// <summary>
        /// 计算处方总价
        /// </summary>
        Task<ServiceResult<decimal>> CalculateTotalPriceAsync(Guid prescriptionId);
        
        /// <summary>
        /// 获取处方打印信息
        /// </summary>
        Task<ServiceResult<PrescriptionPrintInfo>> GetPrintInfoAsync(Guid id);
        
        #endregion
        
        #region 关联数据
        
        /// <summary>
        /// 获取可用的中药材列表
        /// </summary>
        Task<ServiceResult<IEnumerable<AvailableHerbInfo>>> GetAvailableHerbsAsync(string? keyword = null);
        
        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        Task<ServiceResult<IEnumerable<FormulaTemplateInfo>>> GetFormulaTemplatesAsync(string? keyword = null);
        
        /// <summary>
        /// 获取历史处方记录（用于参考）
        /// </summary>
        Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetHistoryPrescriptionsAsync(Guid patientId, int count = 10);
        
        #endregion
    }
    
    #region 辅助信息类
    
    /// <summary>
    /// 处方统计信息
    /// </summary>
    public class PrescriptionStatisticsInfo
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime StatisticsDate { get; set; }
        public Dictionary<string, int> HerbUsageCounts { get; set; } = new();
        public Dictionary<string, int> DoctorPrescriptionCounts { get; set; } = new();
    }
    
    /// <summary>
    /// 医生处方统计信息
    /// </summary>
    public class DoctorPrescriptionStatisticsInfo
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int TotalPrescriptions { get; set; }
        public int CompletedPrescriptions { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime LastPrescriptionTime { get; set; }
        public List<string> FrequentHerbs { get; set; } = new();
    }
    
    /// <summary>
    /// 药材使用统计信息
    /// </summary>
    public class HerbUsageStatisticsInfo
    {
        public string HerbName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal Percentage { get; set; }
        public DateTime LastUsed { get; set; }
        public decimal AverageQuantity { get; set; }
    }
    
    /// <summary>
    /// 处方费用统计信息
    /// </summary>
    public class PrescriptionCostStatisticsInfo
    {
        public decimal TotalCost { get; set; }
        public decimal AverageCost { get; set; }
        public decimal MinCost { get; set; }
        public decimal MaxCost { get; set; }
        public int PrescriptionCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    
    /// <summary>
    /// 药材库存警告信息
    /// </summary>
    public class HerbStockWarningInfo
    {
        public string HerbName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableStock { get; set; }
        public decimal ShortageQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 药材配伍禁忌警告信息
    /// </summary>
    public class HerbCompatibilityWarningInfo
    {
        public string HerbName1 { get; set; } = string.Empty;
        public string HerbName2 { get; set; } = string.Empty;
        public string WarningType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 处方打印信息
    /// </summary>
    public class PrescriptionPrintInfo
    {
        public PrescriptionInfo Prescription { get; set; } = new();
        public string PatientInfo { get; set; } = string.Empty;
        public string DoctorInfo { get; set; } = string.Empty;
        public string ClinicInfo { get; set; } = string.Empty;
        public string PrintTime { get; set; } = string.Empty;
        public string QrCodeData { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 可用中药材信息
    /// </summary>
    public class AvailableHerbInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Stock { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
    
    /// <summary>
    /// 验方模板信息
    /// </summary>
    public class FormulaTemplateInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<FormulaHerbInfo> Herbs { get; set; } = new();
        public DateTime CreateTime { get; set; }
    }
    
    /// <summary>
    /// 验方药材信息
    /// </summary>
    public class FormulaHerbInfo
    {
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? Usage { get; set; }
    }
    
    /// <summary>
    /// 处方项目创建信息
    /// </summary>
    public class PrescriptionItemCreateInfo
    {
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string? Usage { get; set; }
        public string? Remark { get; set; }
    }
    
    /// <summary>
    /// 处方项目更新信息
    /// </summary>
    public class PrescriptionItemUpdateInfo
    {
        public Guid Id { get; set; }
        public Guid HerbId { get; set; }
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string? Usage { get; set; }
        public string? Remark { get; set; }
    }
    
    #endregion
}
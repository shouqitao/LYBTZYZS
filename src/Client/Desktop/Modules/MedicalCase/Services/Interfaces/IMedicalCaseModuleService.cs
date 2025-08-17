using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Services.Interfaces
{
    /// <summary>
    /// MedicalCase模块核心业务服务接口
    /// UltraThink模块化架构：封装医疗案例模块所有业务逻辑
    /// </summary>
    public interface IMedicalCaseModuleService
    {
        #region 基础CRUD操作
        
        /// <summary>
        /// 分页获取医疗案例列表
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseInfo>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        Task<ServiceResult<MedicalCaseInfo>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 创建医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseInfo>> CreateAsync(MedicalCaseCreateInfo createInfo);
        
        /// <summary>
        /// 更新医疗案例
        /// </summary>
        Task<ServiceResult<MedicalCaseInfo>> UpdateAsync(MedicalCaseUpdateInfo updateInfo);
        
        /// <summary>
        /// 删除医疗案例
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);
        
        #endregion
        
        #region 状态管理
        
        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        Task<ServiceResult> UpdateStatusAsync(Guid id, MedicalCaseStatus status, string? reason = null);
        
        /// <summary>
        /// 开始看诊
        /// </summary>
        Task<ServiceResult> StartConsultationAsync(Guid id);
        
        /// <summary>
        /// 完成看诊
        /// </summary>
        Task<ServiceResult> CompleteConsultationAsync(Guid id, string? diagnosis = null);
        
        /// <summary>
        /// 取消医疗案例
        /// </summary>
        Task<ServiceResult> CancelAsync(Guid id, string reason);
        
        /// <summary>
        /// 批量更新状态
        /// </summary>
        Task<ServiceResult<int>> BatchUpdateStatusAsync(IEnumerable<Guid> ids, MedicalCaseStatus status, string? reason = null);
        
        #endregion
        
        #region 查询操作
        
        /// <summary>
        /// 搜索医疗案例
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseInfo>>> SearchAsync(PagedQueryBaseDto request);
        
        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        Task<ServiceResult<IEnumerable<MedicalCaseInfo>>> GetByPatientIdAsync(Guid patientId);
        
        /// <summary>
        /// 根据医生ID获取医疗案例
        /// </summary>
        Task<ServiceResult<IEnumerable<MedicalCaseInfo>>> GetByDoctorIdAsync(Guid doctorId);
        
        /// <summary>
        /// 根据状态获取医疗案例
        /// </summary>
        Task<ServiceResult<PagedResult<MedicalCaseInfo>>> GetByStatusAsync(MedicalCaseStatus status, PagedQueryBaseDto query);
        
        /// <summary>
        /// 获取日期范围内的医疗案例
        /// </summary>
        Task<ServiceResult<IEnumerable<MedicalCaseInfo>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        
        #endregion
        
        #region 验证操作
        
        /// <summary>
        /// 验证医疗案例信息
        /// </summary>
        Task<ServiceResult> ValidateAsync(MedicalCaseInfo medicalCaseInfo);
        
        /// <summary>
        /// 验证患者是否可以创建新案例
        /// </summary>
        Task<ServiceResult<bool>> CanCreateCaseForPatientAsync(Guid patientId);
        
        /// <summary>
        /// 验证医生是否可以处理案例
        /// </summary>
        Task<ServiceResult<bool>> CanDoctorHandleCaseAsync(Guid doctorId, Guid caseId);
        
        #endregion
        
        #region 统计功能
        
        /// <summary>
        /// 获取医疗案例统计信息
        /// </summary>
        Task<ServiceResult<MedicalCaseStatisticsInfo>> GetStatisticsAsync();
        
        /// <summary>
        /// 获取今日案例统计
        /// </summary>
        Task<ServiceResult<MedicalCaseStatisticsInfo>> GetTodayStatisticsAsync();
        
        /// <summary>
        /// 获取医生案例统计
        /// </summary>
        Task<ServiceResult<DoctorCaseStatisticsInfo>> GetDoctorStatisticsAsync(Guid doctorId);
        
        /// <summary>
        /// 获取热门诊断
        /// </summary>
        Task<ServiceResult<IEnumerable<DiagnosisStatisticsInfo>>> GetPopularDiagnosisAsync(int count = 10);
        
        #endregion
        
        #region 业务规则验证
        
        /// <summary>
        /// 检查是否可以修改医疗案例
        /// </summary>
        Task<ServiceResult<bool>> CanModifyAsync(Guid id);
        
        /// <summary>
        /// 检查是否可以删除医疗案例
        /// </summary>
        Task<ServiceResult<bool>> CanDeleteAsync(Guid id);
        
        /// <summary>
        /// 获取案例操作历史
        /// </summary>
        Task<ServiceResult<IEnumerable<CaseOperationHistoryInfo>>> GetOperationHistoryAsync(Guid id);
        
        #endregion
        
        #region 关联数据
        
        /// <summary>
        /// 获取案例的看诊记录
        /// </summary>
        Task<ServiceResult<IEnumerable<ConsultationInfo>>> GetConsultationsAsync(Guid caseId);
        
        /// <summary>
        /// 获取案例的处方记录
        /// </summary>
        Task<ServiceResult<IEnumerable<PrescriptionInfo>>> GetPrescriptionsAsync(Guid caseId);
        
        /// <summary>
        /// 检查患者是否有未完成的案例
        /// </summary>
        Task<ServiceResult<bool>> HasIncompleteCasesAsync(Guid patientId);
        
        #endregion
    }
    
    #region 辅助信息类
    
    /// <summary>
    /// 医疗案例统计信息
    /// </summary>
    public class MedicalCaseStatisticsInfo
    {
        public int TotalCount { get; set; }
        public int RegisteredCount { get; set; }
        public int InConsultationCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal AverageConsultationTime { get; set; }
        public DateTime StatisticsDate { get; set; }
        public Dictionary<string, int> DiagnosisCounts { get; set; } = new();
        public Dictionary<string, int> DoctorCaseCounts { get; set; } = new();
    }
    
    /// <summary>
    /// 医生案例统计信息
    /// </summary>
    public class DoctorCaseStatisticsInfo
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int TotalCases { get; set; }
        public int CompletedCases { get; set; }
        public int InProgressCases { get; set; }
        public decimal AverageConsultationTime { get; set; }
        public decimal CompletionRate { get; set; }
        public DateTime LastCaseTime { get; set; }
        public List<string> CommonDiagnoses { get; set; } = new();
    }
    
    /// <summary>
    /// 诊断统计信息
    /// </summary>
    public class DiagnosisStatisticsInfo
    {
        public string Diagnosis { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
        public DateTime LastUsed { get; set; }
    }
    
    /// <summary>
    /// 案例操作历史信息
    /// </summary>
    public class CaseOperationHistoryInfo
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string? Details { get; set; }
        public Guid OperatorId { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public DateTime OperationTime { get; set; }
        public string? Reason { get; set; }
    }
    
    /// <summary>
    /// 看诊信息（简化版）
    /// </summary>
    public class ConsultationInfo
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Symptoms { get; set; }
        public string? Diagnosis { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? CompleteTime { get; set; }
    }
    
    /// <summary>
    /// 处方信息（简化版）
    /// </summary>
    public class PrescriptionInfo
    {
        public Guid Id { get; set; }
        public Guid CaseId { get; set; }
        public string? PrescriptionName { get; set; }
        public string? Instructions { get; set; }
        public DateTime CreateTime { get; set; }
        public int HerbCount { get; set; }
    }
    
    #endregion
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Services.Interfaces
{
    /// <summary>
    /// 看诊模块业务服务接口 - UltraThink架构重构版
    /// 整合看诊数据管理、处方管理、验方管理等所有业务逻辑
    /// 遵循UltraThink模块化原则：高内聚、低耦合、自包含
    /// </summary>
    public interface IConsultationModuleService
    {
        #region 看诊基础操作

        /// <summary>
        /// 获取分页看诊记录
        /// </summary>
        Task<ServiceResult<PagedResult<ConsultationInfo>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据ID获取看诊详情
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新的看诊记录
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> CreateAsync(ConsultationCreateInfo createInfo);

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> UpdateAsync(ConsultationUpdateInfo updateInfo);

        /// <summary>
        /// 删除看诊记录
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除看诊记录
        /// </summary>
        Task<ServiceResult> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 检查是否可以删除
        /// </summary>
        Task<ServiceResult<bool>> CanDeleteAsync(Guid id);

        /// <summary>
        /// 检查是否可以修改
        /// </summary>
        Task<ServiceResult<bool>> CanModifyAsync(Guid id);

        #endregion

        #region 看诊工作流管理

        /// <summary>
        /// 开始新的看诊流程
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> StartConsultationAsync(Guid patientId, Guid doctorId);

        /// <summary>
        /// 完成看诊
        /// </summary>
        Task<ServiceResult> CompleteConsultationAsync(Guid consultationId);

        /// <summary>
        /// 暂停看诊（保存草稿）
        /// </summary>
        Task<ServiceResult> PauseConsultationAsync(Guid consultationId);

        /// <summary>
        /// 恢复看诊
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> ResumeConsultationAsync(Guid consultationId);

        /// <summary>
        /// 获取当前正在进行的看诊
        /// </summary>
        Task<ServiceResult<List<ConsultationInfo>>> GetActiveConsultationsAsync(Guid doctorId);

        #endregion

        #region 中医四诊管理

        /// <summary>
        /// 更新望诊信息
        /// </summary>
        Task<ServiceResult> UpdateInspectionAsync(Guid consultationId, TCMInspectionInfo inspectionInfo);

        /// <summary>
        /// 更新闻诊信息
        /// </summary>
        Task<ServiceResult> UpdateAuscultationOlfactionAsync(Guid consultationId, TCMAuscultationOlfactionInfo auscultationInfo);

        /// <summary>
        /// 更新问诊信息
        /// </summary>
        Task<ServiceResult> UpdateInquiryAsync(Guid consultationId, TCMInquiryInfo inquiryInfo);

        /// <summary>
        /// 更新切诊信息
        /// </summary>
        Task<ServiceResult> UpdatePalpationAsync(Guid consultationId, TCMPalpationInfo palpationInfo);

        /// <summary>
        /// 生成四诊综合描述
        /// </summary>
        Task<ServiceResult<string>> GenerateTCMSummaryAsync(Guid consultationId);

        /// <summary>
        /// 验证四诊完整性
        /// </summary>
        Task<ServiceResult<TCMCompletenessInfo>> ValidateTCMCompletenessAsync(Guid consultationId);

        #endregion

        #region 体征管理

        /// <summary>
        /// 更新生命体征
        /// </summary>
        Task<ServiceResult> UpdateVitalSignsAsync(Guid consultationId, VitalSignsInfo vitalSigns);

        /// <summary>
        /// 获取体征历史
        /// </summary>
        Task<ServiceResult<List<VitalSignsHistoryInfo>>> GetVitalSignsHistoryAsync(Guid patientId, int days = 30);

        /// <summary>
        /// 分析体征趋势
        /// </summary>
        Task<ServiceResult<VitalSignsTrendInfo>> AnalyzeVitalSignsTrendsAsync(Guid patientId);

        #endregion

        #region 诊断管理

        /// <summary>
        /// 更新诊断信息
        /// </summary>
        Task<ServiceResult> UpdateDiagnosisAsync(Guid consultationId, DiagnosisInfo diagnosisInfo);

        /// <summary>
        /// 获取诊断建议
        /// </summary>
        Task<ServiceResult<List<DiagnosisSuggestionInfo>>> GetDiagnosisSuggestionsAsync(string symptoms);

        /// <summary>
        /// 验证诊断完整性
        /// </summary>
        Task<ServiceResult<DiagnosisValidationInfo>> ValidateDiagnosisAsync(Guid consultationId);

        /// <summary>
        /// 获取常用诊断
        /// </summary>
        Task<ServiceResult<List<string>>> GetFrequentDiagnosesAsync(Guid doctorId, int count = 20);

        #endregion

        #region 处方管理（集成）

        /// <summary>
        /// 获取当前处方项目
        /// </summary>
        ObservableCollection<PrescriptionItemInfo> GetCurrentPrescriptionItems();

        /// <summary>
        /// 添加药材到处方
        /// </summary>
        Task<ServiceResult> AddHerbToPrescriptionAsync(Guid consultationId, HerbDto herb, decimal quantity = 10m);

        /// <summary>
        /// 从处方中移除药材
        /// </summary>
        Task<ServiceResult> RemoveHerbFromPrescriptionAsync(Guid consultationId, Guid herbId);

        /// <summary>
        /// 更新处方项目数量
        /// </summary>
        Task<ServiceResult> UpdateHerbQuantityAsync(Guid consultationId, Guid herbId, decimal newQuantity);

        /// <summary>
        /// 清空当前处方
        /// </summary>
        Task<ServiceResult> ClearPrescriptionAsync(Guid consultationId);

        /// <summary>
        /// 保存处方到数据库
        /// </summary>
        Task<ServiceResult<PrescriptionInfo>> SavePrescriptionAsync(ConsultationPrescriptionCreateInfo prescriptionInfo);

        /// <summary>
        /// 验证处方完整性
        /// </summary>
        Task<ServiceResult<PrescriptionValidationInfo>> ValidatePrescriptionAsync(Guid consultationId);

        /// <summary>
        /// 计算处方总价
        /// </summary>
        Task<ServiceResult<decimal>> CalculatePrescriptionTotalAsync(Guid consultationId);

        #endregion

        #region 验方管理（集成）

        /// <summary>
        /// 应用验方模板
        /// </summary>
        Task<ServiceResult<List<PrescriptionItemInfo>>> ApplyFormulaTemplateAsync(Guid consultationId, FormulaInfo formula);

        /// <summary>
        /// 合并验方到当前处方
        /// </summary>
        Task<ServiceResult<List<PrescriptionItemInfo>>> MergeFormulaToPrescriptionAsync(
            Guid consultationId, FormulaInfo formula, FormulaMergeMode mergeMode = FormulaMergeMode.Merge);

        /// <summary>
        /// 创建自定义验方
        /// </summary>
        Task<ServiceResult<FormulaInfo>> CreateCustomFormulaAsync(CustomFormulaCreateInfo formulaInfo);

        /// <summary>
        /// 获取推荐验方
        /// </summary>
        Task<ServiceResult<List<FormulaInfo>>> GetRecommendedFormulasAsync(string symptoms);

        /// <summary>
        /// 获取常用验方
        /// </summary>
        Task<ServiceResult<List<FormulaInfo>>> GetFrequentlyUsedFormulasAsync(Guid doctorId, int count = 10);

        #endregion

        #region 数据载入与缓存

        /// <summary>
        /// 加载患者列表
        /// </summary>
        Task<ServiceResult<List<PatientInfo>>> LoadPatientsAsync(bool forceRefresh = false);

        /// <summary>
        /// 加载中药材列表
        /// </summary>
        Task<ServiceResult<List<HerbDto>>> LoadHerbsAsync(bool forceRefresh = false);

        /// <summary>
        /// 加载验方模板列表
        /// </summary>
        Task<ServiceResult<List<FormulaInfo>>> LoadFormulasAsync(bool forceRefresh = false);

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        void ClearAllCache();

        /// <summary>
        /// 清除特定类型缓存
        /// </summary>
        void ClearSpecificCache(string cacheType);

        /// <summary>
        /// 获取缓存统计
        /// </summary>
        CacheStatisticsInfo GetCacheStatistics();

        #endregion

        #region 看诊统计与报告

        /// <summary>
        /// 获取医生看诊统计
        /// </summary>
        Task<ServiceResult<DoctorConsultationStatsInfo>> GetDoctorStatsAsync(Guid doctorId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// 获取患者看诊历史
        /// </summary>
        Task<ServiceResult<List<ConsultationInfo>>> GetPatientConsultationHistoryAsync(Guid patientId, int count = 10);

        /// <summary>
        /// 生成看诊报告
        /// </summary>
        Task<ServiceResult<ConsultationReportInfo>> GenerateConsultationReportAsync(Guid consultationId);

        /// <summary>
        /// 导出看诊数据
        /// </summary>
        Task<ServiceResult<byte[]>> ExportConsultationDataAsync(List<Guid> consultationIds, string format = "Excel");

        #endregion

        #region 辅助与验证

        /// <summary>
        /// 验证看诊数据完整性
        /// </summary>
        Task<ServiceResult<ConsultationValidationInfo>> ValidateConsultationAsync(Guid consultationId);

        /// <summary>
        /// 检查药材库存
        /// </summary>
        Task<ServiceResult<List<HerbStockWarningInfo>>> CheckHerbStockAsync(Guid consultationId);

        /// <summary>
        /// 获取看诊模板
        /// </summary>
        Task<ServiceResult<List<ConsultationTemplateInfo>>> GetConsultationTemplatesAsync(string category);

        /// <summary>
        /// 应用看诊模板
        /// </summary>
        Task<ServiceResult> ApplyConsultationTemplateAsync(Guid consultationId, Guid templateId);

        #endregion

        #region 事件与通知

        /// <summary>
        /// 处方项目变更事件
        /// </summary>
        event EventHandler<PrescriptionItemsChangedEventArgs>? PrescriptionItemsChanged;

        /// <summary>
        /// 看诊状态变更事件
        /// </summary>
        event EventHandler<ConsultationStatusChangedEventArgs>? ConsultationStatusChanged;

        /// <summary>
        /// 四诊数据更新事件
        /// </summary>
        event EventHandler<TCMDataUpdatedEventArgs>? TCMDataUpdated;

        #endregion
    }

    #region 辅助信息类

    /// <summary>TCM四诊完整性信息</summary>
    public class TCMCompletenessInfo
    {
        public bool IsInspectionComplete { get; set; }
        public bool IsAuscultationComplete { get; set; }
        public bool IsInquiryComplete { get; set; }
        public bool IsPalpationComplete { get; set; }
        public bool IsComplete => IsInspectionComplete && IsAuscultationComplete && IsInquiryComplete && IsPalpationComplete;
        public List<string> MissingItems { get; set; } = new();
    }

    /// <summary>体征信息</summary>
    public class VitalSignsInfo
    {
        public decimal? Temperature { get; set; }
        public int? SystolicPressure { get; set; }
        public int? DiastolicPressure { get; set; }
        public int? HeartRate { get; set; }
        public int? RespiratoryRate { get; set; }
        public DateTime MeasureTime { get; set; } = DateTime.Now;
    }

    /// <summary>体征历史信息</summary>
    public class VitalSignsHistoryInfo : VitalSignsInfo
    {
        public Guid Id { get; set; }
        public Guid ConsultationId { get; set; }
    }

    /// <summary>体征趋势信息</summary>
    public class VitalSignsTrendInfo
    {
        public bool HasAbnormalTrend { get; set; }
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> TrendData { get; set; } = new();
    }

    /// <summary>诊断信息</summary>
    public class DiagnosisInfo
    {
        public string? TCMDiagnosis { get; set; }
        public string? WesternDiagnosis { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public Guid? DiagnosisCatalogId { get; set; }
        public string? TreatmentPrinciple { get; set; }
        public string? MedicalAdvice { get; set; }
    }

    /// <summary>诊断建议信息</summary>
    public class DiagnosisSuggestionInfo
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public List<string> ReasoningSteps { get; set; } = new();
    }

    /// <summary>诊断验证信息</summary>
    public class DiagnosisValidationInfo
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    /// <summary>处方验证信息</summary>
    public class PrescriptionValidationInfo
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<HerbStockWarningInfo> StockWarnings { get; set; } = new();
    }

    /// <summary>看诊处方创建信息</summary>
    public class ConsultationPrescriptionCreateInfo
    {
        public Guid ConsultationId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? DosageForm { get; set; }
        public int DosageCount { get; set; } = 7;
        public string? Usage { get; set; }
        public string? Advice { get; set; }
        public List<PrescriptionItemInfo> Items { get; set; } = new();
    }

    /// <summary>自定义验方创建信息</summary>
    public class CustomFormulaCreateInfo
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<PrescriptionItemInfo> Items { get; set; } = new();
        public Guid CreatedBy { get; set; }
        public bool IsPersonal { get; set; } = true;
    }

    /// <summary>缓存统计信息</summary>
    public class CacheStatisticsInfo
    {
        public int PatientsCount { get; set; }
        public int HerbsCount { get; set; }
        public int FormulasCount { get; set; }
        public DateTime LastRefreshTime { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>医生看诊统计信息</summary>
    public class DoctorConsultationStatsInfo
    {
        public int TotalConsultations { get; set; }
        public int CompletedConsultations { get; set; }
        public decimal AverageConsultationTime { get; set; }
        public List<string> TopDiagnoses { get; set; } = new();
        public Dictionary<string, int> MonthlyStats { get; set; } = new();
    }

    /// <summary>看诊报告信息</summary>
    public class ConsultationReportInfo
    {
        public Guid ConsultationId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime ConsultationTime { get; set; }
        public string ReportContent { get; set; } = string.Empty;
        public Dictionary<string, object> ReportData { get; set; } = new();
    }

    /// <summary>看诊验证信息</summary>
    public class ConsultationValidationInfo
    {
        public bool IsValid { get; set; }
        public bool IsTCMComplete { get; set; }
        public bool IsVitalSignsComplete { get; set; }
        public bool IsDiagnosisComplete { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
    }

    /// <summary>看诊模板信息</summary>
    public class ConsultationTemplateInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> TemplateData { get; set; } = new();
    }

    /// <summary>TCM望诊信息</summary>
    public class TCMInspectionInfo
    {
        public string? GeneralAppearance { get; set; }
        public string? FacialComplexion { get; set; }
        public string? EyesInspection { get; set; }
        public string? TongueInspection { get; set; }
        public string? SkinInspection { get; set; }
        public string? OverallInspection { get; set; }
    }

    /// <summary>TCM闻诊信息</summary>
    public class TCMAuscultationOlfactionInfo
    {
        public string? VoiceAndSpeech { get; set; }
        public string? BreathingSound { get; set; }
        public string? CoughingSound { get; set; }
        public string? BodyOdor { get; set; }
        public string? BreathOdor { get; set; }
        public string? OverallAuscultation { get; set; }
    }

    /// <summary>TCM问诊信息</summary>
    public class TCMInquiryInfo
    {
        public string? ChiefComplaint { get; set; }
        public string? PresentIllness { get; set; }
        public string? PastHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public string? LifestyleInquiry { get; set; }
        public string? SymptomInquiry { get; set; }
        public string? OverallInquiry { get; set; }
    }

    /// <summary>TCM切诊信息</summary>
    public class TCMPalpationInfo
    {
        public string? PulseCondition { get; set; }
        public string? AbdomenPalpation { get; set; }
        public string? ExtremitiesPalpation { get; set; }
        public string? AcupointsPalpation { get; set; }
        public string? SkinTemperature { get; set; }
        public string? OverallPalpation { get; set; }
    }

    #endregion

    #region 事件参数类

    /// <summary>处方项目变更事件参数</summary>
    public class PrescriptionItemsChangedEventArgs : EventArgs
    {
        public Guid ConsultationId { get; set; }
        public List<PrescriptionItemInfo> Items { get; set; } = new();
        public string ChangeType { get; set; } = string.Empty;
    }

    /// <summary>看诊状态变更事件参数</summary>
    public class ConsultationStatusChangedEventArgs : EventArgs
    {
        public Guid ConsultationId { get; set; }
        public CommonStatus OldStatus { get; set; }
        public CommonStatus NewStatus { get; set; }
    }

    /// <summary>TCM数据更新事件参数</summary>
    public class TCMDataUpdatedEventArgs : EventArgs
    {
        public Guid ConsultationId { get; set; }
        public string UpdatedSection { get; set; } = string.Empty;
        public Dictionary<string, object> UpdatedData { get; set; } = new();
    }

    #endregion
}
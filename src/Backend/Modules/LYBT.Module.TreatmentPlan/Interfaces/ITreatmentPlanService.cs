using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentPlan;

namespace LYBT.Module.TreatmentPlan.Interfaces
{
    /// <summary>
    /// 治疗方案服务接口
    /// </summary>
    public interface ITreatmentPlanService
    {
        /// <summary>
        /// 根据ID获取治疗方案详情
        /// </summary>
        Task<TreatmentPlanDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanDto>> GetListAsync();

        /// <summary>
        /// 分页查询治疗方案
        /// </summary>
        Task<PaginatedResult<TreatmentPlanDto>> GetPagedAsync(TreatmentPlanQueryDto query);

        /// <summary>
        /// 创建治疗方案
        /// </summary>
        Task<TreatmentPlanDetailDto?> CreateAsync(TreatmentPlanCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新治疗方案
        /// </summary>
        Task<TreatmentPlanDetailDto?> UpdateAsync(Guid id, TreatmentPlanUpdateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除治疗方案
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 根据医疗案例ID获取治疗方案
        /// </summary>
        Task<TreatmentPlanDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanDto>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 获取治疗方案统计
        /// </summary>
        Task<TreatmentPlanStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate, Guid? doctorId = null);

        // ==================== 治疗方案状态管理 ====================

        /// <summary>
        /// 开始治疗方案
        /// </summary>
        Task<bool> StartPlanAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 完成治疗方案
        /// </summary>
        Task<bool> CompletePlanAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 暂停治疗方案
        /// </summary>
        Task<bool> PausePlanAsync(Guid id, string reason, Guid operatorId, string operatorName);

        /// <summary>
        /// 恢复治疗方案
        /// </summary>
        Task<bool> ResumePlanAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 取消治疗方案
        /// </summary>
        Task<bool> CancelPlanAsync(Guid id, string reason, Guid operatorId, string operatorName);

        // ==================== 治疗方案组合管理 ====================

        /// <summary>
        /// 添加处方到治疗方案
        /// </summary>
        Task<bool> AddPrescriptionAsync(Guid planId, Guid prescriptionId, bool isPrimary, Guid operatorId, string operatorName);

        /// <summary>
        /// 移除处方
        /// </summary>
        Task<bool> RemovePrescriptionAsync(Guid planId, Guid prescriptionId, Guid operatorId, string operatorName);

        /// <summary>
        /// 添加治疗项目
        /// </summary>
        Task<bool> AddTreatmentItemAsync(Guid planId, TreatmentPlanItemCreateDto item, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新治疗项目
        /// </summary>
        Task<bool> UpdateTreatmentItemAsync(Guid planId, Guid itemId, TreatmentPlanItemUpdateDto item, Guid operatorId, string operatorName);

        /// <summary>
        /// 移除治疗项目
        /// </summary>
        Task<bool> RemoveTreatmentItemAsync(Guid planId, Guid itemId, Guid operatorId, string operatorName);

        // ==================== 治疗执行追踪 ====================

        /// <summary>
        /// 记录治疗执行
        /// </summary>
        Task<bool> RecordExecutionAsync(Guid planId, TreatmentExecutionRecordDto record, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取治疗执行记录
        /// </summary>
        Task<List<TreatmentExecutionRecordDto>> GetExecutionRecordsAsync(Guid planId);

        /// <summary>
        /// 获取治疗进度
        /// </summary>
        Task<TreatmentProgressDto> GetTreatmentProgressAsync(Guid planId);

        // ==================== 治疗方案模板 ====================

        /// <summary>
        /// 从模板创建治疗方案
        /// </summary>
        Task<TreatmentPlanDetailDto?> CreateFromTemplateAsync(Guid templateId, Guid medicalCaseId, Guid operatorId, string operatorName);

        /// <summary>
        /// 保存为模板
        /// </summary>
        Task<TreatmentPlanTemplateDto?> SaveAsTemplateAsync(Guid planId, string templateName, string diseaseCategory, Guid operatorId, string operatorName);

        /// <summary>
        /// 获取治疗方案模板列表
        /// </summary>
        Task<List<TreatmentPlanTemplateDto>> GetTemplatesAsync(string? diseaseCategory = null);

        /// <summary>
        /// 搜索治疗方案
        /// </summary>
        Task<List<TreatmentPlanDto>> SearchPlansAsync(string keyword, int maxResults = 50);
    }

    /// <summary>
    /// 治疗进度DTO
    /// </summary>
    public class TreatmentProgressDto
    {
        public Guid TreatmentPlanId { get; set; }
        public int TotalPrescriptions { get; set; }
        public int CompletedPrescriptions { get; set; }
        public int TotalTreatmentItems { get; set; }
        public int CompletedTreatmentItems { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string CurrentPhase { get; set; } = string.Empty;
        public DateTime? NextAppointment { get; set; }
        public List<string> PendingTasks { get; set; } = new();
    }
}
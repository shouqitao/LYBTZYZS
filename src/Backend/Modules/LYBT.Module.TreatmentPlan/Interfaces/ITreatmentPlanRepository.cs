using LYBT.Models.TreatmentPlan;

namespace LYBT.Module.TreatmentPlan.Interfaces
{
    /// <summary>
    /// 治疗方案仓储接口
    /// </summary>
    public interface ITreatmentPlanRepository
    {
        /// <summary>
        /// 获取所有治疗方案
        /// </summary>
        Task<List<TreatmentPlanModel>> GetListAsync();

        /// <summary>
        /// 根据ID获取治疗方案
        /// </summary>
        Task<TreatmentPlanModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建治疗方案
        /// </summary>
        Task<TreatmentPlanModel> CreateAsync(TreatmentPlanModel model);

        /// <summary>
        /// 更新治疗方案
        /// </summary>
        Task<bool> UpdateAsync(TreatmentPlanModel model);

        /// <summary>
        /// 删除治疗方案
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取治疗方案
        /// </summary>
        Task<TreatmentPlanModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据患者ID获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanModel>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 根据日期范围获取治疗方案列表
        /// </summary>
        Task<List<TreatmentPlanModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}
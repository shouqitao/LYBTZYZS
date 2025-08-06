using LYBT.Models.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例仓储接口
    /// </summary>
    public interface IMedicalCaseRepository
    {
        /// <summary>
        /// 获取所有医疗案例
        /// </summary>
        Task<List<MedicalCaseModel>> GetListAsync();

        /// <summary>
        /// 根据ID获取医疗案例
        /// </summary>
        Task<MedicalCaseModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        Task<MedicalCaseModel> CreateAsync(MedicalCaseModel model);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        Task<bool> UpdateAsync(MedicalCaseModel model);

        /// <summary>
        /// 删除医疗案例
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 根据日期范围获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 根据状态获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByStatusAsync(MedicalCaseStatus status);
    }
}
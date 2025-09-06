using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.Prescriptions.Interfaces {

    /// <summary>
    /// 处方查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// </summary>
    public interface IPrescriptionQueryService {

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页查询处方
        /// </summary>
        Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query);

        /// <summary>
        /// 根据患者ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 搜索处方
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取所有处方
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetAllAsync();

        /// <summary>
        /// 获取医生今日处方列表
        /// </summary>
        Task<ServiceResult<List<PrescriptionDto>>> GetDoctorTodayPrescriptionsAsync(Guid doctorId);
    }
}

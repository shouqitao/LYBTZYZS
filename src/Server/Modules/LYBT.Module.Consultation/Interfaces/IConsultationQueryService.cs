using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{

    /// <summary>
    /// 诊疗查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// </summary>
    public interface IConsultationQueryService
    {

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据患者ID获取诊疗记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取诊疗记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据医生ID获取诊疗记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 搜索诊疗记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取患者历史诊疗记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id);
    }
}

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Interfaces.Services
{

    /// <summary>
    /// 诊疗服务接口 - UltraThink统一标准
    /// </summary>
    public interface IConsultationService
    {

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 开始诊疗
        /// </summary>
        Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto);

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto);

        /// <summary>
        /// 删除诊疗记录
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取诊疗记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据医生ID获取诊疗记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 获取诊疗统计信息
        /// </summary>
        Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// 搜索诊疗记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取患者历史就诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

    }
}

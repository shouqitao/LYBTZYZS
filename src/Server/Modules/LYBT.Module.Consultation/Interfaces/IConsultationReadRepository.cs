using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 诊疗只读仓储接口 - 专门为QueryService提供数据访问
    /// 继承IReadOnlyRepository提供基础查询功能，扩展诊疗特定的查询方法
    /// </summary>
    public interface IConsultationReadRepository : IReadOnlyRepository<LYBT.Entities.Consultation.Consultation>
    {
        /// <summary>
        /// 分页查询诊疗记录并映射为DTO
        /// </summary>
        Task<PagedResult<ConsultationDto>> GetPagedConsultationDtosAsync(ConsultationQueryDto query);

        /// <summary>
        /// 根据患者ID获取诊疗记录DTO列表
        /// </summary>
        Task<List<ConsultationDto>> GetConsultationDtosByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取诊疗记录DTO列表
        /// </summary>
        Task<List<ConsultationDto>> GetConsultationDtosByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据医生ID获取诊疗记录DTO列表
        /// </summary>
        Task<List<ConsultationDto>> GetConsultationDtosByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 搜索诊疗记录并映射为DTO
        /// </summary>
        Task<List<ConsultationDto>> SearchConsultationDtosAsync(string keyword, int maxResults = 50);

        /// <summary>
        /// 获取患者历史诊疗记录DTO列表
        /// </summary>
        Task<List<ConsultationDto>> GetPatientHistoryDtosAsync(Guid patientId);

        /// <summary>
        /// 根据ID获取诊疗详情DTO
        /// </summary>
        Task<ConsultationDetailDto?> GetConsultationDetailDtoAsync(Guid id);

        /// <summary>
        /// 根据ID获取诊疗详情DTO (别名)
        /// </summary>
        Task<ConsultationDetailDto?> GetConsultationDetailDtoByIdAsync(Guid id);

        /// <summary>
        /// 获取患者诊疗历史记录DTO列表 (别名)
        /// </summary>
        Task<List<ConsultationDto>> GetPatientConsultationHistoryAsync(Guid patientId);
    }
}
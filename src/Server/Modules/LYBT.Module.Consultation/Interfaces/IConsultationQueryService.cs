using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Module.Consultation.Interfaces
{
    /// <summary>
    /// 看诊查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// </summary>
    public interface IConsultationQueryService
    {
        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 根据患者ID获取看诊记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取看诊记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 根据医生ID获取看诊记录列表
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 搜索看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取中医四诊信息
        /// </summary>
        Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId);
    }
}
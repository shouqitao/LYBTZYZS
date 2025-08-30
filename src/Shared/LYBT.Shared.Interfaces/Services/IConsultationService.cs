using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Interfaces.Services
{
    /// <summary>
    /// 看诊服务接口 - UltraThink统一标准
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 根据ID获取看诊详情
        /// </summary>
        Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query);
        
        /// <summary>
        /// 开始看诊
        /// </summary>
        Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto);
        
        /// <summary>
        /// 更新看诊记录
        /// </summary>
        Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto);
        
        /// <summary>
        /// 删除看诊记录
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);
        
        /// <summary>
        /// 根据患者ID获取看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId);
        
        /// <summary>
        /// 根据医疗案例ID获取看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
        
        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId);
        

        
        /// <summary>
        /// 获取看诊统计信息
        /// </summary>
        Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate);
        
        /// <summary>
        /// 搜索看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取患者历史就诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 根据医疗案例ID获取四诊数据
        /// </summary>
        Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 保存四诊数据
        /// </summary>
        Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData);
    }
}
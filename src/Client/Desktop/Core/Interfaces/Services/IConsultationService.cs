using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Core.Interfaces.Services
{
    /// <summary>
    /// 看诊服务接口
    /// </summary>
    public interface IConsultationService
    {
        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        Task<LYBT.Shared.Models.Contracts.Common.PagedResult<ConsultationInfo>> SearchConsultationsAsync(PagedQueryBaseDto query);

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取看诊信息
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 开始看诊
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> StartConsultationAsync(ConsultationStartDto dto);

        /// <summary>
        /// 更新看诊信息
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> UpdateConsultationAsync(Guid id, ConsultationUpdateDto dto);

        /// <summary>
        /// 完成看诊
        /// </summary>
        Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto);

        /// <summary>
        /// 获取医生今日看诊列表
        /// </summary>
        Task<ServiceResult<List<ConsultationInfo>>> GetTodayConsultationsByDoctorAsync(Guid doctorId);

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        Task<ServiceResult<List<ConsultationInfo>>> GetPatientHistoryAsync(Guid patientId);

        /// <summary>
        /// 统计医生看诊数量
        /// </summary>
        Task<ServiceResult<int>> GetDoctorConsultationCountAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// 更新看诊状态
        /// </summary>
        Task<ServiceResult<ConsultationInfo>> UpdateStatusAsync(Guid id, int status, string? reason = null);

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 根据医疗案例ID获取四诊信息
        /// </summary>
        Task<ServiceResult<FourDiagnosisData>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId);

        /// <summary>
        /// 保存四诊信息
        /// </summary>
        Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid medicalCaseId, FourDiagnosisData data);

        /// <summary>
        /// 保存整个诊疗数据
        /// </summary>
        Task<ServiceResult<bool>> SaveAsync(ConsultationData data);
    }
}
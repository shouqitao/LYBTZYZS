using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Consultation.Interfaces
{

    /// <summary>
    /// 看诊业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// </summary>
    public interface IConsultationBusinessService
    {

        /// <summary>
        /// 保存中医四诊信息
        /// </summary>
        Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData);

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
    }
}

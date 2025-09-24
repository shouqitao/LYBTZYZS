using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Consultation.Interfaces
{

    /// <summary>
    /// 诊疗业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// </summary>
    public interface IConsultationBusinessService
    {
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
    }
}

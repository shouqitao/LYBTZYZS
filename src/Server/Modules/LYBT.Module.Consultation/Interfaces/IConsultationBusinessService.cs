using LYBT.Shared.Models.Contracts.Common;
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
        /// 验证工作流状态转换 - Record-Only模式简化版（功能移除）
        /// </summary>
        [Obsolete("Complex workflow state validation removed in Record-Only mode. Use simple status updates instead.", false)]
        Task<ServiceResult<bool>> ValidateWorkflowStateAsync(Guid consultationId, ConsultationStatus targetStatus);
    }
}

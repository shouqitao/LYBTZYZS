using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Interfaces
{
    /// <summary>
    /// Consultation API Client接口
    /// Issue #1590: REQ-001 - 三步工作流优化-Step1
    /// </summary>
    public interface IConsultationApiClient
    {
        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="request">完成Step1请求（包含PrescriptionEnabled）</param>
        /// <returns>诊疗步骤状态DTO</returns>
        Task<ConsultationStepDto> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request);
    }
}

using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Repositories
{
    /// <summary>
    /// Consultation API Client实现
    /// Issue #1590: REQ-001 - 三步工作流优化-Step1
    /// </summary>
    public class ConsultationApiClient : IConsultationApiClient
    {
        private readonly IConsultationApi _api;
        private readonly ILogger<ConsultationApiClient> _logger;

        public ConsultationApiClient(
            IConsultationApi consultationApi,
            ILogger<ConsultationApiClient> logger)
        {
            _api = consultationApi ?? throw new ArgumentNullException(nameof(consultationApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 完成辩证步骤（Step 1）
        /// </summary>
        public async Task<ConsultationStepDto> CompleteStep1Async(Guid medicalCaseId, CompleteStep1Request request)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            try
            {
                _logger.LogInformation("开始完成Step1，MedicalCaseId: {MedicalCaseId}, PrescriptionEnabled: {PrescriptionEnabled}",
                    medicalCaseId, request.PrescriptionEnabled);

                var response = await _api.CompleteStep1Async(medicalCaseId, request);

                if (response.Data == null)
                    throw new InvalidOperationException("服务器未返回Step1完成状态数据");

                _logger.LogInformation("Step1完成成功，MedicalCaseId: {MedicalCaseId}, Step1CompletedAt: {Step1CompletedAt}",
                    medicalCaseId, response.Data.Step1CompletedAt);

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成Step1失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }
    }
}

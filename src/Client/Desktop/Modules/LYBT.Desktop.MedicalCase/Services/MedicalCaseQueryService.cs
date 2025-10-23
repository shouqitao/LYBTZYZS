using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 医案查询服务实现
    /// Epic #1583 - Phase 2: 智能路由（临时实现）
    /// </summary>
    public class MedicalCaseQueryService : IMedicalCaseQueryService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly ILogger<MedicalCaseQueryService> _logger;

        public MedicalCaseQueryService(
            IMedicalCaseRepository repository,
            ILoggerFactory loggerFactory)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = loggerFactory?.CreateLogger<MedicalCaseQueryService>()
                ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Phase 2临时实现：使用GetByPatientIdAsync过滤Status=Active的医案
        /// Phase 5优化：实现专用API /api/medicalcases/patient/{patientId}/unfinished
        /// </summary>
        public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("查询患者未完成医案：PatientId={PatientId}", patientId);

                // Phase 2临时方案：调用现有API，过滤Status=Enabled的医案
                // 注：CommonStatus.Enabled表示活跃状态，对应业务上的“未完成医案”
                var cases = await _repository.GetByPatientIdAsync(patientId);
                var unfinishedCase = cases.FirstOrDefault(c => c.Status == CommonStatus.Enabled);

                if (unfinishedCase != null)
                {
                    _logger.LogInformation("找到未完成医案：MedicalCaseId={MedicalCaseId}，Status={Status}",
                        unfinishedCase.Id, unfinishedCase.Status);
                }
                else
                {
                    _logger.LogInformation("患者无未完成医案");
                }

                return unfinishedCase;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询未完成医案失败：PatientId={PatientId}", patientId);
                return null;
            }
        }

        /// <summary>
        /// Phase 2临时实现：使用DeleteAsync（不符合业务语义，但功能等效）
        /// Phase 5优化：实现专用API /api/medicalcases/{id}/close（业务语义清晰）
        /// </summary>
        public async Task<bool> CloseAsync(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("关闭医案（级联删除）：MedicalCaseId={MedicalCaseId}", medicalCaseId);

                // Phase 2临时方案：使用DeleteAsync（会级联删除Consultation和Prescription）
                // TODO Phase 5: 实现专用的CloseAsync API，业务语义更清晰
                var result = await _repository.DeleteAsync(medicalCaseId);

                _logger.LogInformation("医案关闭{Result}：MedicalCaseId={MedicalCaseId}",
                    result ? "成功" : "失败", medicalCaseId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭医案失败：MedicalCaseId={MedicalCaseId}", medicalCaseId);
                return false;
            }
        }
    }
}

using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// OpenSpec: medicalcase-management-ui-refactor (EDITMODE-010)
    /// 审计需求检查器实现 - 判断修改医案时是否需要填写修改原因
    /// </summary>
    public class AuditRequirementChecker : IAuditRequirementChecker
    {
        private readonly ILogger<AuditRequirementChecker> _logger;

        public AuditRequirementChecker(ILogger<AuditRequirementChecker> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public bool IsAuditRequired(MedicalCaseDetailDto medicalCase, Guid currentUserId)
        {
            if (medicalCase == null)
            {
                _logger.LogWarning("IsAuditRequired: medicalCase is null, returning true for safety");
                return true;
            }

            // 规则1：已完成的医案必须审计
            if (medicalCase.CaseStatus == MedicalCaseStatus.Completed)
            {
                _logger.LogDebug("需要审计: 医案 {CaseNumber} 状态为Completed", medicalCase.CaseNumber);
                return true;
            }

            // 规则2：非本人修改必须审计
            if (medicalCase.UserId != currentUserId)
            {
                _logger.LogDebug("需要审计: 医案 {CaseNumber} 的医生 {UserId} 与当前用户 {CurrentUserId} 不同",
                    medicalCase.CaseNumber, medicalCase.UserId, currentUserId);
                return true;
            }

            // 规则3：隔天修改必须审计
            var caseDate = medicalCase.CreatedAt.Date;
            var today = DateTime.Today;
            if (caseDate < today)
            {
                _logger.LogDebug("需要审计: 医案 {CaseNumber} 创建日期 {CaseDate} 早于今天 {Today}",
                    medicalCase.CaseNumber, caseDate, today);
                return true;
            }

            // 其他情况：当天本人修改进行中的医案，无需审计
            _logger.LogDebug("无需审计: 医案 {CaseNumber} 为当天本人的进行中医案", medicalCase.CaseNumber);
            return false;
        }
    }
}

using System.Text.Json;
using System.Threading;
using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案审计服务实现
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// </summary>
    public class MedicalCaseAuditService : IMedicalCaseAuditService
    {
        private readonly IMedicalCaseAuditLogRepository _auditLogRepository;
        private readonly ILogger<MedicalCaseAuditService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public MedicalCaseAuditService(
            IMedicalCaseAuditLogRepository auditLogRepository,
            ILogger<MedicalCaseAuditService> logger)
        {
            _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task LogAsync(
            MedicalCase? before,
            MedicalCase after,
            Guid operatorId,
            string operatorName,
            UserRole role,
            AuditOperationType operationType,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            if (after == null)
                throw new ArgumentNullException(nameof(after));

            try
            {
                var (changedFields, oldValues, newValues) = DetectChanges(before, after);

                var auditLog = new MedicalCaseAuditLog
                {
                    MedicalCaseId = after.Id,
                    OperatorId = operatorId,
                    OperatorName = operatorName,
                    OperatorRole = role,
                    OperationType = operationType,
                    ChangedFields = changedFields,
                    OldValues = oldValues,
                    NewValues = newValues,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                };

                await _auditLogRepository.AddAsync(auditLog, cancellationToken);
                await _auditLogRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[SVC] MedicalCase.Audit completed - MedicalCaseId={MedicalCaseId} OperationType={OperationType} OperatorName={OperatorName} OperatorRole={OperatorRole} ChangedFields={ChangedFields}",
                    after.Id, operationType, operatorName, role, changedFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SVC] MedicalCase.Audit failed - MedicalCaseId={MedicalCaseId} OperatorId={OperatorId}",
                    after.Id, operatorId);
                // 审计日志失败不应影响主业务流程
            }
        }

        /// <inheritdoc/>
        public async Task<List<MedicalCaseAuditLog>> GetLogsAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            return await _auditLogRepository.GetByMedicalCaseIdAsync(medicalCaseId, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetLogsPagedAsync(
            Guid medicalCaseId,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            return await _auditLogRepository.GetPagedByMedicalCaseIdAsync(medicalCaseId, page, pageSize, cancellationToken);
        }

        #region Private Methods

        /// <summary>
        /// 检测两个医案实体之间的变更
        /// </summary>
        private (string? ChangedFields, string? OldValues, string? NewValues) DetectChanges(
            MedicalCase? before,
            MedicalCase after)
        {
            if (before == null)
            {
                // 创建操作 - 只记录新值
                // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
                var newValues = new Dictionary<string, object?>
                {
                    ["PatientId"] = after.PatientId,
                    ["PatientName"] = after.PatientName,
                    ["UserId"] = after.UserId,
                    ["DoctorName"] = after.DoctorName,
                    ["CaseStatus"] = after.CaseStatus.ToString(),
                    ["Remark"] = after.Remark
                };

                return (
                    JsonSerializer.Serialize(newValues.Keys, _jsonOptions),
                    null,
                    JsonSerializer.Serialize(newValues, _jsonOptions)
                );
            }

            // 更新操作 - 比较变更
            var changedFields = new List<string>();
            var oldValues = new Dictionary<string, object?>();
            var newValuesDict = new Dictionary<string, object?>();

            // 检查各字段变更
            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId, ConsultationDate移除
            CompareField("PatientId", before.PatientId, after.PatientId, changedFields, oldValues, newValuesDict);
            CompareField("PatientName", before.PatientName, after.PatientName, changedFields, oldValues, newValuesDict);
            CompareField("UserId", before.UserId, after.UserId, changedFields, oldValues, newValuesDict);
            CompareField("DoctorName", before.DoctorName, after.DoctorName, changedFields, oldValues, newValuesDict);
            CompareField("CaseStatus", before.CaseStatus.ToString(), after.CaseStatus.ToString(), changedFields, oldValues, newValuesDict);
            CompareField("Remark", before.Remark, after.Remark, changedFields, oldValues, newValuesDict);
            CompareField("NeedsPrescription", before.NeedsPrescription, after.NeedsPrescription, changedFields, oldValues, newValuesDict);
            CompareField("IsDeleted", before.IsDeleted, after.IsDeleted, changedFields, oldValues, newValuesDict);
            CompareField("CompletedAt", before.CompletedAt, after.CompletedAt, changedFields, oldValues, newValuesDict);

            // 嵌套实体: Consultation 变更检测
            if (before.Consultation != null && after.Consultation != null)
            {
                CompareField("Consultation.PresentIllness", before.Consultation.PresentIllness, after.Consultation.PresentIllness, changedFields, oldValues, newValuesDict);
                CompareField("Consultation.TongueDiagnosis", before.Consultation.TongueDiagnosis, after.Consultation.TongueDiagnosis, changedFields, oldValues, newValuesDict);
                CompareField("Consultation.PulseDiagnosis", before.Consultation.PulseDiagnosis, after.Consultation.PulseDiagnosis, changedFields, oldValues, newValuesDict);
                CompareField("Consultation.TcmDiagnosis", before.Consultation.TcmDiagnosis, after.Consultation.TcmDiagnosis, changedFields, oldValues, newValuesDict);
            }
            else if (before.Consultation == null && after.Consultation != null)
            {
                changedFields.Add("Consultation");
                newValuesDict["Consultation"] = "Created";
            }

            // 嵌套实体: Prescription 变更检测
            if (before.Prescription != null && after.Prescription != null)
            {
                CompareField("Prescription.Usage", before.Prescription.Usage, after.Prescription.Usage, changedFields, oldValues, newValuesDict);
                CompareField("Prescription.DosageCount", before.Prescription.DosageCount, after.Prescription.DosageCount, changedFields, oldValues, newValuesDict);
                CompareField("Prescription.Discount", before.Prescription.Discount, after.Prescription.Discount, changedFields, oldValues, newValuesDict);
                CompareField("Prescription.Advice", before.Prescription.Advice, after.Prescription.Advice, changedFields, oldValues, newValuesDict);
                CompareField("Prescription.ReferencedFormulas", before.Prescription.ReferencedFormulas, after.Prescription.ReferencedFormulas, changedFields, oldValues, newValuesDict);
                CompareField("Prescription.IsDeleted", before.Prescription.IsDeleted, after.Prescription.IsDeleted, changedFields, oldValues, newValuesDict);

                // Items 数量变化
                var beforeItemCount = before.Prescription.Items?.Count ?? 0;
                var afterItemCount = after.Prescription.Items?.Count ?? 0;
                CompareField("Prescription.ItemCount", beforeItemCount, afterItemCount, changedFields, oldValues, newValuesDict);
            }
            else if (before.Prescription == null && after.Prescription != null)
            {
                changedFields.Add("Prescription");
                newValuesDict["Prescription"] = "Created";
            }
            else if (before.Prescription != null && after.Prescription == null)
            {
                changedFields.Add("Prescription");
                oldValues["Prescription"] = "Existed";
                newValuesDict["Prescription"] = "Removed";
            }

            if (changedFields.Count == 0)
                return (null, null, null);

            return (
                JsonSerializer.Serialize(changedFields, _jsonOptions),
                JsonSerializer.Serialize(oldValues, _jsonOptions),
                JsonSerializer.Serialize(newValuesDict, _jsonOptions)
            );
        }

        /// <summary>
        /// 比较单个字段的变更
        /// </summary>
        private static void CompareField<T>(
            string fieldName,
            T? oldValue,
            T? newValue,
            List<string> changedFields,
            Dictionary<string, object?> oldValues,
            Dictionary<string, object?> newValues)
        {
            if (!Equals(oldValue, newValue))
            {
                changedFields.Add(fieldName);
                oldValues[fieldName] = oldValue;
                newValues[fieldName] = newValue;
            }
        }

        #endregion
    }
}

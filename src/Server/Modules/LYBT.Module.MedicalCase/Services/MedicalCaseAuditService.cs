using System.Text.Json;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案审计服务实现
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// </summary>
    public class MedicalCaseAuditService : IMedicalCaseAuditService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MedicalCaseAuditService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public MedicalCaseAuditService(
            AppDbContext dbContext,
            ILogger<MedicalCaseAuditService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
            string? reason = null)
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
                    CreatedAt = DateTime.Now
                };

                _dbContext.MedicalCaseAuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "审计日志: {OperationType} 医案 {MedicalCaseId}, 操作者: {OperatorName}({OperatorRole}), 变更字段: {ChangedFields}",
                    operationType, after.Id, operatorName, role, changedFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "记录审计日志失败: 医案 {MedicalCaseId}, 操作者: {OperatorId}",
                    after.Id, operatorId);
                // 审计日志失败不应影响主业务流程
            }
        }

        /// <inheritdoc/>
        public async Task<List<MedicalCaseAuditLog>> GetLogsAsync(Guid medicalCaseId)
        {
            return await _dbContext.MedicalCaseAuditLogs
                .Where(l => l.MedicalCaseId == medicalCaseId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetLogsPagedAsync(
            Guid medicalCaseId,
            int page = 1,
            int pageSize = 20)
        {
            var query = _dbContext.MedicalCaseAuditLogs
                .Where(l => l.MedicalCaseId == medicalCaseId)
                .OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (logs, totalCount);
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
                var newValues = new Dictionary<string, object?>
                {
                    ["PatientId"] = after.PatientId,
                    ["PatientName"] = after.PatientName,
                    ["DoctorId"] = after.DoctorId,
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
            CompareField("PatientId", before.PatientId, after.PatientId, changedFields, oldValues, newValuesDict);
            CompareField("PatientName", before.PatientName, after.PatientName, changedFields, oldValues, newValuesDict);
            CompareField("DoctorId", before.DoctorId, after.DoctorId, changedFields, oldValues, newValuesDict);
            CompareField("DoctorName", before.DoctorName, after.DoctorName, changedFields, oldValues, newValuesDict);
            CompareField("CaseStatus", before.CaseStatus.ToString(), after.CaseStatus.ToString(), changedFields, oldValues, newValuesDict);
            CompareField("Remark", before.Remark, after.Remark, changedFields, oldValues, newValuesDict);
            CompareField("NeedsPrescription", before.NeedsPrescription, after.NeedsPrescription, changedFields, oldValues, newValuesDict);
            CompareField("ConsultationDate", before.ConsultationDate, after.ConsultationDate, changedFields, oldValues, newValuesDict);
            CompareField("IsDeleted", before.IsDeleted, after.IsDeleted, changedFields, oldValues, newValuesDict);

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

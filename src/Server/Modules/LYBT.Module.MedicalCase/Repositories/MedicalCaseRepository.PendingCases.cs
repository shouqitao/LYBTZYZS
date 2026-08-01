using System.Threading;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Repositories
{
    /// <summary>
    /// 医案仓储 - 待看诊（Pending）相关查询
    /// ctor 与 _context/_dbSet/_logger 由主文件提供
    /// </summary>
    internal partial class MedicalCaseRepository
    {
        /// <summary>
        /// 获取待看诊医案列表（Status=Suspended或Active）
        /// Epic #1583 - Phase 5
        /// Bug Fix: 应包含Suspended和Active两种未完成状态
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null, CancellationToken cancellationToken = default)
        {
            // Epic #2210 Phase 3: 按医生ID过滤，实现多医生数据隔离
            // Bug Fix: 包含Suspended和Active两种未完成状态，挂起后的医案应显示在待诊队列
            var query = _dbSet
                .Where(m => !m.IsDeleted
                    && (m.CaseStatus == MedicalCaseStatus.Suspended || m.CaseStatus == MedicalCaseStatus.Active)
                    && m.UserId == doctorId);
            if (patientId.HasValue)
            {
                query = query.Where(m => m.PatientId == patientId.Value);
            }

            // Bug Fix: MaskPhoneNumber无法在EF Core查询中翻译，先查询原始数据再在内存中处理
            var rawData = await query
                .Join(
                    _context.Set<Patient>(),
                    m => m.PatientId,
                    p => p.Id,
                    (m, p) => new { MedicalCase = m, Patient = p })
                .OrderBy(r => r.MedicalCase.CreatedAt) // 按创建时间升序
                .Select(r => new
                {
                    PatientId = r.Patient.Id,
                    PatientName = r.Patient.Name,
                    PhoneNumber = r.Patient.PhoneNumber ?? string.Empty,
                    CaseStatus = r.MedicalCase.CaseStatus,
                    MedicalCaseId = r.MedicalCase.Id,
                    CreatedAt = r.MedicalCase.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 在内存中应用电话脱敏并转换为DTO
            var result = rawData.Select(r => new PendingMedicalCaseDto
            {
                PatientId = r.PatientId,
                PatientName = r.PatientName,
                PhoneNumber = r.PhoneNumber,
                PhoneMasked = MaskPhoneNumber(r.PhoneNumber),
                CaseStatus = r.CaseStatus,
                MedicalCaseId = r.MedicalCaseId,
                CreatedAt = r.CreatedAt
            }).ToList();
            for (int i = 0; i < result.Count; i++)
            {
                result[i].QueueNumber = i + 1;
            }

            _logger?.LogInformation("获取待看诊列表（DoctorId: {DoctorId}），共 {Count} 条记录",
                doctorId, result.Count);
            return result;
        }

        /// <summary>
        /// 获取所有待看诊医案列表（管理员专用）
        /// Bug Fix: 应包含Suspended和Active两种未完成状态
        /// </summary>
        public async Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync(CancellationToken cancellationToken = default)
        {
            // Bug Fix: 包含Suspended和Active两种未完成状态
            // Bug Fix: MaskPhoneNumber无法在EF Core查询中翻译，先查询原始数据再在内存中处理
            var rawData = await _dbSet
                .Where(m => !m.IsDeleted && (m.CaseStatus == MedicalCaseStatus.Suspended || m.CaseStatus == MedicalCaseStatus.Active))
                .Join(
                    _context.Set<Patient>(),
                    m => m.PatientId,
                    p => p.Id,
                    (m, p) => new { MedicalCase = m, Patient = p })
                .OrderBy(r => r.MedicalCase.CreatedAt) // 按创建时间升序
                .Select(r => new
                {
                    PatientId = r.Patient.Id,
                    PatientName = r.Patient.Name,
                    PhoneNumber = r.Patient.PhoneNumber ?? string.Empty,
                    CaseStatus = r.MedicalCase.CaseStatus,
                    MedicalCaseId = r.MedicalCase.Id,
                    CreatedAt = r.MedicalCase.CreatedAt
                })
                .ToListAsync(cancellationToken);

            // 在内存中应用电话脱敏并转换为DTO
            var result = rawData.Select(r => new PendingMedicalCaseDto
            {
                PatientId = r.PatientId,
                PatientName = r.PatientName,
                PhoneNumber = r.PhoneNumber,
                PhoneMasked = MaskPhoneNumber(r.PhoneNumber),
                CaseStatus = r.CaseStatus,
                MedicalCaseId = r.MedicalCaseId,
                CreatedAt = r.CreatedAt
            }).ToList();
            for (int i = 0; i < result.Count; i++)
            {
                result[i].QueueNumber = i + 1;
            }

            _logger?.LogInformation("获取所有待看诊列表（管理员），共 {Count} 条记录", result.Count);
            return result;
        }

        /// <summary>
        /// 手机号脱敏处理（138****1234格式）
        /// Epic #1583 - Phase 5
        /// </summary>
        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 11)
                return phoneNumber;

            return $"{phoneNumber.Substring(0, 3)}****{phoneNumber.Substring(7)}";
        }
    }
}

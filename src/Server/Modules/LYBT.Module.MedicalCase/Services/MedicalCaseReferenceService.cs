using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.MedicalCases.Services
{
    /// <summary>
    /// 医案引用查询服务实现 - 用于跨模块查询
    /// Architecture Fix: 集中处理医案查询逻辑，供其他模块（如Patient）使用
    /// 同时实现IMedicalCaseReferenceService和IMedicalCaseCrossModuleService
    /// </summary>
    public class MedicalCaseReferenceService : IMedicalCaseReferenceService, IMedicalCaseCrossModuleService
    {
        private readonly AppDbContext _dbContext;

        public MedicalCaseReferenceService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <inheritdoc/>
        public async Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted
                    && (mc.CaseStatus == MedicalCaseStatus.Active || mc.CaseStatus == MedicalCaseStatus.Suspended))
                .CountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> CountMedicalCasesAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
                .CountAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count, CancellationToken cancellationToken = default)
        {
            return await _dbContext.MedicalCases
                .Where(mc => mc.PatientId == patientId && !mc.IsDeleted)
                .OrderByDescending(mc => mc.CreatedAt)
                .Take(count)
                .Select(mc => new MedicalCaseReferenceDto
                {
                    MedicalCaseId = mc.Id,
                    CaseNumber = mc.CaseNumber ?? string.Empty,
                    CreatedAt = mc.CreatedAt,
                    Status = mc.CaseStatus.ToString()
                })
                .ToListAsync(cancellationToken);
        }
    }
}

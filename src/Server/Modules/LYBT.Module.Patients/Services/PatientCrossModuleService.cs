using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services;

/// <summary>
/// 患者跨模块服务实现
/// 替代 CrossModuleService 中的患者查询逻辑
/// </summary>
public class PatientCrossModuleService : IPatientCrossModuleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PatientCrossModuleService> _logger;

    public PatientCrossModuleService(IDbContextAccessor dbAccessor, ILogger<PatientCrossModuleService> logger)
    {
        _context = dbAccessor.Context;
        _logger = logger;
    }

    public async Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId && !p.IsDeleted)
            .Select(p => new PatientBasicDto
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.PhoneNumber,
                Status = p.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(
        IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default)
    {
        var ids = patientIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, PatientBasicDto>();

        var result = new Dictionary<Guid, PatientBasicDto>();
        foreach (var patientId in ids)
        {
            var patient = await _context.Patients
                .AsNoTracking()
                .Where(p => p.Id == patientId && !p.IsDeleted)
                .Select(p => new PatientBasicDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Gender = p.Gender,
                    Phone = p.PhoneNumber,
                    Status = p.Status
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (patient != null)
            {
                result[patient.Id] = patient;
            }
        }

        return result;
    }

    public async Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && !p.IsDeleted, cancellationToken);
    }

    public async Task<ReferenceCheckResult> CheckPatientReferenceAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var count = await _context.MedicalCases
            .AsNoTracking()
            .CountAsync(mc => mc.PatientId == patientId && !mc.IsDeleted, cancellationToken);

        return new ReferenceCheckResult(
            HasReferences: count > 0,
            ReferenceCount: count,
            Message: count > 0 ? $"患者有 {count} 条医案记录" : null);
    }
}



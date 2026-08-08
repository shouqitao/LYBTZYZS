using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Patients.Infrastructure;
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
    private readonly PatientsDbContext _context;
    private readonly ILogger<PatientCrossModuleService> _logger;

    public PatientCrossModuleService(PatientsDbContext context, ILogger<PatientCrossModuleService> logger)
    {
        _context = context;
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
}



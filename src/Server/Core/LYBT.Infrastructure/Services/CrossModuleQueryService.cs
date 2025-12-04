using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块查询服务实现
/// 直接使用DbContext进行只读查询，不经过模块Service
/// </summary>
public class CrossModuleQueryService : ICrossModuleQueryService
{
    private readonly AppDbContext _context;

    public CrossModuleQueryService(AppDbContext context)
    {
        _context = context;
    }

    #region 患者查询

    /// <inheritdoc />
    public async Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId)
    {
        return await _context.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId && !p.IsDeleted)
            .Select(p => new PatientBasicDto
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.PhoneNumber
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(
        IEnumerable<Guid> patientIds)
    {
        var ids = patientIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, PatientBasicDto>();

        var patients = await _context.Patients
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new PatientBasicDto
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Phone = p.PhoneNumber
            })
            .ToListAsync();

        return patients.ToDictionary(p => p.Id);
    }

    #endregion

    #region 医案查询

    /// <inheritdoc />
    public async Task<MedicalCaseBasicDto?> GetMedicalCaseBasicInfoAsync(Guid medicalCaseId)
    {
        return await _context.MedicalCases
            .AsNoTracking()
            .Where(mc => mc.Id == medicalCaseId && !mc.IsDeleted)
            .Select(mc => new MedicalCaseBasicDto
            {
                Id = mc.Id,
                PatientId = mc.PatientId,
                Status = mc.CaseStatus,
                CreatedAt = mc.CreatedAt,
                // 关联诊断信息 - 使用子查询
                TCMDiagnosis = _context.Consultations
                    .Where(c => c.Id == mc.Id)
                    .Select(c => c.TCMDiagnosis)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, MedicalCaseBasicDto>> GetMedicalCasesBasicInfoAsync(
        IEnumerable<Guid> medicalCaseIds)
    {
        var ids = medicalCaseIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, MedicalCaseBasicDto>();

        // 批量查询医案 - 分两步避免复杂Join
        var medicalCases = await _context.MedicalCases
            .AsNoTracking()
            .Where(mc => ids.Contains(mc.Id) && !mc.IsDeleted)
            .Select(mc => new MedicalCaseBasicDto
            {
                Id = mc.Id,
                PatientId = mc.PatientId,
                Status = mc.CaseStatus,
                CreatedAt = mc.CreatedAt
            })
            .ToListAsync();

        // 批量查询关联诊断 - 第二次数据库查询
        var consultations = await _context.Consultations
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.TCMDiagnosis })
            .ToDictionaryAsync(c => c.Id, c => c.TCMDiagnosis);

        // 合并诊断信息 - 内存操作
        foreach (var mc in medicalCases)
        {
            if (consultations.TryGetValue(mc.Id, out var diagnosis))
            {
                mc.TCMDiagnosis = diagnosis;
            }
        }

        return medicalCases.ToDictionary(mc => mc.Id);
    }

    #endregion

    #region 药材查询

    /// <inheritdoc />
    public async Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId)
    {
        return await _context.Herbs
            .AsNoTracking()
            .Where(h => h.Id == herbId && !h.IsDeleted)
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.PinYinCode,
                Category = h.Category
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin)
    {
        if (string.IsNullOrWhiteSpace(nameOrPinyin))
            return null;

        return await _context.Herbs
            .AsNoTracking()
            .Where(h => !h.IsDeleted &&
                (h.Name == nameOrPinyin || h.PinYinCode == nameOrPinyin))
            .Select(h => new HerbBasicDto
            {
                Id = h.Id,
                Name = h.Name,
                Pinyin = h.PinYinCode,
                Category = h.Category
            })
            .FirstOrDefaultAsync();
    }

    #endregion
}

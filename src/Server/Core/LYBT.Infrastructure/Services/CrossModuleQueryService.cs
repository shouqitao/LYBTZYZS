using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.DTOs.Users;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块服务实现
/// 直接使用DbContext进行跨模块数据访问，不经过模块Service
/// </summary>
public class CrossModuleService : ICrossModuleService
{
    private readonly AppDbContext _context;

    public CrossModuleService(AppDbContext context)
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

    // ========== 医案查询方法已删除（OpenSpec: consolidate-medicalcase-queries）==========
    // GetMedicalCaseBasicInfoAsync 已删除 - 请使用 MedicalCaseQueryService
    // GetMedicalCasesBasicInfoAsync 已删除 - 请使用 MedicalCaseQueryService

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

    #region 用户查询

    /// <inheritdoc />
    public async Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new UserBasicDto
            {
                Id = u.Id,
                UserName = u.UserName,
                RealName = u.RealName,
                Role = u.Role,
                Status = u.Status,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                PinYinCode = u.PinYinCode,
                LastLoginTime = u.LastLoginTime,
                FailedLoginCount = u.FailedLoginCount,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Remark = u.Remark
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<UserCredentialDto?> GetUserByUsernameAsync(string username)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.UserName == username && !u.IsDeleted)
            .Select(u => new UserCredentialDto
            {
                Id = u.Id,
                UserName = u.UserName,
                RealName = u.RealName,
                Role = u.Role,
                Status = u.Status,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                PinYinCode = u.PinYinCode,
                LastLoginTime = u.LastLoginTime,
                FailedLoginCount = u.FailedLoginCount,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Remark = u.Remark,
                PasswordHash = u.PasswordHash
            })
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash)
    {
        // 不使用 FindAsync: 当实体不在 ChangeTracker 中时，FindAsync 会应用全局查询过滤器 (IsDeleted)
        // 仅更新未删除用户的密码哈希
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync();
        }
    }

    #endregion
}

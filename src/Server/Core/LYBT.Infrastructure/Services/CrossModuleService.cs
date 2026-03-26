using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.DTOs.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块服务实现
/// 直接使用DbContext进行跨模块数据访问，不经过模块Service
/// 实现 4 个 ISP 接口 (D5-1)
/// 实现 ISP 接口取代原 ICrossModuleService 单一大接口
/// </summary>
public class CrossModuleService :
    IPatientCrossModuleService,
    IHerbCrossModuleService,
    IUserCrossModuleService,
    ICrossModuleAuthService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CrossModuleService> _logger;

    public CrossModuleService(AppDbContext context, ILogger<CrossModuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region 患者查询 (IPatientCrossModuleService)

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
                Phone = p.PhoneNumber,
                Status = p.Status  // T5-P2-09
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

        // OPENJSON-COMPAT: 逐个查询避免 EF Core 8 List<Guid>.Contains() 生成 OPENJSON WITH 语法
        // SQL Server 兼容级别 < 130 不支持此语法
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
                    Status = p.Status  // T5-P2-09
                })
                .FirstOrDefaultAsync();

            if (patient != null)
            {
                result[patient.Id] = patient;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> PatientExistsAsync(Guid patientId)
    {
        return await _context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && !p.IsDeleted);
    }

    /// <inheritdoc />
    public async Task<ReferenceCheckResult> CheckPatientReferenceAsync(Guid patientId)
    {
        var count = await _context.MedicalCases
            .AsNoTracking()
            .CountAsync(mc => mc.PatientId == patientId && !mc.IsDeleted);

        return new ReferenceCheckResult(
            HasReferences: count > 0,
            ReferenceCount: count,
            Message: count > 0 ? $"患者有 {count} 条医案记录" : null);
    }

    #endregion

    // ========== 医案查询方法已删除（OpenSpec: consolidate-medicalcase-queries）==========
    // GetMedicalCaseBasicInfoAsync 已删除 - 请使用 MedicalCaseQueryService
    // GetMedicalCasesBasicInfoAsync 已删除 - 请使用 MedicalCaseQueryService

    #region 药材查询 (IHerbCrossModuleService)

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

    /// <inheritdoc />
    public async Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId)
    {
        var count = await _context.PrescriptionItems
            .AsNoTracking()
            .CountAsync(pi => pi.HerbId == herbId);

        return new ReferenceCheckResult(
            HasReferences: count > 0,
            ReferenceCount: count,
            Message: count > 0 ? $"药材被 {count} 个处方项引用" : null);
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds)
    {
        var idList = herbIds.ToList();
        if (idList.Count == 0) return new Dictionary<Guid, decimal>();

        // OPENJSON-COMPAT: 逐个查询避免 EF Core 8 List<Guid>.Contains() 生成 OPENJSON WITH 语法
        // SQL Server 兼容级别 < 130 不支持此语法
        var result = new Dictionary<Guid, decimal>();
        foreach (var herbId in idList)
        {
            var herb = await _context.Herbs
                .AsNoTracking()
                .Where(h => h.Id == herbId && !h.IsDeleted)
                .Select(h => new { h.Id, h.Price })
                .FirstOrDefaultAsync();

            if (herb != null)
            {
                result[herb.Id] = herb.Price;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds)
    {
        var idList = herbIds.ToList();
        if (idList.Count == 0) return new HashSet<Guid>();

        // AD-02: 逐个查询避免 EF Core 8 OPENJSON WITH 语法在低版本 SQL Server 上的兼容性问题
        var disabledIds = new HashSet<Guid>();
        foreach (var herbId in idList)
        {
            var herb = await _context.Herbs
                .AsNoTracking()
                .Where(h => h.Id == herbId && !h.IsDeleted)
                .Select(h => new { h.Id, h.Status })
                .FirstOrDefaultAsync();

            if (herb != null && herb.Status == Shared.Models.Enums.CommonStatus.Disabled)
            {
                disabledIds.Add(herb.Id);
            }
        }

        return disabledIds;
    }

    #endregion

    #region 用户查询 (IUserCrossModuleService)

    /// <inheritdoc />
    public async Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)
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
                LockoutEnd = u.LockoutEnd,
                MustChangeOnNextLogin = u.MustChangeOnNextLogin,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Remark = u.Remark
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
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
                LockoutEnd = u.LockoutEnd,
                MustChangeOnNextLogin = u.MustChangeOnNextLogin,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Remark = u.Remark,
                PasswordHash = u.PasswordHash
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default)
    {
        // 不使用 FindAsync: 当实体不在 ChangeTracker 中时，FindAsync 会应用全局查询过滤器 (IsDeleted)
        // 仅更新未删除用户的密码哈希
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
    }

    /// <inheritdoc />
    /// <summary>
    /// T5-P2-01: 更新登录失败状态
    /// </summary>
    public async Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user != null)
        {
            user.FailedLoginCount = failedLoginCount;
            user.LockoutEnd = lockoutEnd;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// T5-P2-01: 重置登录状态 (成功登录后)
    /// </summary>
    public async Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);
        if (user != null)
        {
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            user.LastLoginTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region 认证服务 (ICrossModuleAuthService)

    /// <inheritdoc />
    /// <remarks>X3: 按 UserId 批量撤销所有未撤销的 RefreshToken</remarks>
    public async Task RevokeUserTokensAsync(Guid userId, string reason)
    {
        try
        {
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            if (activeTokens.Count == 0)
            {
                _logger.LogDebug("[CMQS] RevokeUserTokens → NoActiveTokens - UserId={UserId}", userId);
                return;
            }

            foreach (var token in activeTokens)
            {
                token.Revoke(reason, "System:CrossModuleRevocation");
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "[CMQS] RevokeUserTokens completed - UserId={UserId} RevokedCount={Count} Reason={Reason}",
                userId, activeTokens.Count, reason);
        }
        catch (Exception ex)
        {
            // X3 约束: 失败记 Warning 不阻塞主操作
            _logger.LogWarning(ex,
                "[CMQS] RevokeUserTokens failed - UserId={UserId} Reason={Reason}", userId, reason);
        }
    }

    #endregion
}

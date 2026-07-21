using System.Threading;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.DTOs.Users;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 跨模块通信服务 — 统一接口
/// 替代旧的 IPatientCrossModuleService/IHerbCrossModuleService/IUserCrossModuleService
/// </summary>
public interface ICrossModuleService
{
    // ===== Patient =====

    /// <summary>获取患者基本信息</summary>
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>批量获取患者基本信息</summary>
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default);

    /// <summary>检查患者是否存在 (未删除)</summary>
    Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>检查患者引用关系 (医案引用数)</summary>
    Task<ReferenceCheckResult> CheckPatientReferenceAsync(Guid patientId, CancellationToken cancellationToken = default);

    // ===== Herb =====

    /// <summary>获取药材基本信息</summary>
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default);

    /// <summary>按名称或拼音查找药材</summary>
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin, CancellationToken cancellationToken = default);

    /// <summary>检查药材引用关系 (处方引用数)</summary>
    Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId, CancellationToken cancellationToken = default);

    /// <summary>批量获取药材单价（用于处方项UnitPrice自动填充）</summary>
    Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);

    /// <summary>从给定的药材ID中筛选出已禁用的药材ID</summary>
    Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default);

    /// <summary>获取所有有效药材（用于批量导入时的名称/拼音匹配）</summary>
    Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default);

    // ===== User =====

    /// <summary>获取用户基本信息</summary>
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>按用户名获取用户凭证信息 (含密码哈希)</summary>
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>更新用户密码哈希</summary>
    Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default);

    /// <summary>检查用户是否存在 (未删除)</summary>
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>更新登录失败状态 (FailedLoginCount + LockoutEnd)</summary>
    Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default);

    /// <summary>重置登录状态 (成功登录后清除锁定，更新 LastLoginTime)</summary>
    Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>验证用户密码 (使用 Identity PBKDF2)</summary>
    Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default);
}

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.DTOs.Users;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 跨模块通信服务 — 统一实现
/// 委托给现有的各域 CrossModuleService
/// </summary>
public class CrossModuleService : ICrossModuleService
{
    private readonly IPatientCrossModuleService _patient;
    private readonly IHerbCrossModuleService _herb;
    private readonly IUserCrossModuleService _user;

    public CrossModuleService(
        IPatientCrossModuleService patient,
        IHerbCrossModuleService herb,
        IUserCrossModuleService user)
    {
        _patient = patient;
        _herb = herb;
        _user = user;
    }

    // ===== Patient =====

    public Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId, CancellationToken cancellationToken = default)
        => _patient.GetPatientBasicInfoAsync(patientId, cancellationToken);

    public Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default)
        => _patient.GetPatientsBasicInfoAsync(patientIds, cancellationToken);

    public Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default)
        => _patient.PatientExistsAsync(patientId, cancellationToken);

    public Task<ReferenceCheckResult> CheckPatientReferenceAsync(Guid patientId, CancellationToken cancellationToken = default)
        => _patient.CheckPatientReferenceAsync(patientId, cancellationToken);

    // ===== Herb =====

    public Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId, CancellationToken cancellationToken = default)
        => _herb.GetHerbBasicInfoAsync(herbId, cancellationToken);

    public Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin, CancellationToken cancellationToken = default)
        => _herb.GetHerbByNameOrPinyinAsync(nameOrPinyin, cancellationToken);

    public Task<ReferenceCheckResult> CheckHerbReferenceAsync(Guid herbId, CancellationToken cancellationToken = default)
        => _herb.CheckHerbReferenceAsync(herbId, cancellationToken);

    public Task<Dictionary<Guid, decimal>> GetHerbPricesAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
        => _herb.GetHerbPricesAsync(herbIds, cancellationToken);

    public Task<HashSet<Guid>> GetDisabledHerbIdsAsync(IEnumerable<Guid> herbIds, CancellationToken cancellationToken = default)
        => _herb.GetDisabledHerbIdsAsync(herbIds, cancellationToken);

    public Task<List<HerbBasicDto>> GetAllActiveHerbsAsync(CancellationToken cancellationToken = default)
        => _herb.GetAllActiveHerbsAsync(cancellationToken);

    // ===== User =====

    public Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId, CancellationToken cancellationToken = default)
        => _user.GetUserBasicInfoAsync(userId, cancellationToken);

    public Task<UserCredentialDto?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _user.GetUserByUsernameAsync(username, cancellationToken);

    public Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash, CancellationToken cancellationToken = default)
        => _user.UpdateUserPasswordHashAsync(userId, newPasswordHash, cancellationToken);

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        => _user.UserExistsAsync(userId, cancellationToken);

    public Task UpdateLoginFailureAsync(Guid userId, int failedLoginCount, DateTime? lockoutEnd, CancellationToken cancellationToken = default)
        => _user.UpdateLoginFailureAsync(userId, failedLoginCount, lockoutEnd, cancellationToken);

    public Task ResetLoginStateAsync(Guid userId, CancellationToken cancellationToken = default)
        => _user.ResetLoginStateAsync(userId, cancellationToken);

    public Task<bool> VerifyPasswordAsync(string username, string password, CancellationToken cancellationToken = default)
        => _user.VerifyPasswordAsync(username, password, cancellationToken);
}

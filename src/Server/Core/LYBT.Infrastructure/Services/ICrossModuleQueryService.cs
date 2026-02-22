using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.DTOs.Users;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块服务接口
/// 提供模块间数据访问，避免直接跨模块注入Repository
/// </summary>
/// <remarks>
/// 设计原则：
/// - 轻量封装：不引入框架级复杂性，仅封装跨模块数据访问
/// - 返回DTO：防止Entity泄露，符合Bounded Context
/// - 批量优先：提供批量查询方法，避免N+1问题
/// - 查询优先：查询方法使用AsNoTracking()优化性能
/// - 写操作最小化：仅包含跨模块必要的写操作（如密码哈希升级）
/// </remarks>
public interface ICrossModuleService
{
    #region 患者查询

    /// <summary>
    /// 获取患者基本信息
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <returns>患者基本信息DTO，不存在或已删除返回null</returns>
    Task<PatientBasicDto?> GetPatientBasicInfoAsync(Guid patientId);

    /// <summary>
    /// 批量获取患者基本信息
    /// </summary>
    /// <param name="patientIds">患者ID集合</param>
    /// <returns>患者ID到基本信息的字典，不包含已删除的患者</returns>
    Task<Dictionary<Guid, PatientBasicDto>> GetPatientsBasicInfoAsync(IEnumerable<Guid> patientIds);

    #endregion

    // ========== 医案查询方法已删除（OpenSpec: consolidate-medicalcase-queries）==========
    // GetMedicalCaseBasicInfoAsync 已删除 - 请使用 MedicalCaseQueryService
    // GetMedicalCasesBasicInfoAsync 已删除 - 请使用 MedicalCaseQueryService

    #region 药材查询

    /// <summary>
    /// 获取药材基本信息 (供Formula模块使用)
    /// </summary>
    /// <param name="herbId">药材ID</param>
    /// <returns>药材基本信息DTO，不存在或已删除返回null</returns>
    Task<HerbBasicDto?> GetHerbBasicInfoAsync(Guid herbId);

    /// <summary>
    /// 按名称或拼音查询药材 (供Formula模块使用)
    /// </summary>
    /// <param name="nameOrPinyin">药材名称或拼音</param>
    /// <returns>匹配的药材基本信息DTO，无匹配返回null</returns>
    Task<HerbBasicDto?> GetHerbByNameOrPinyinAsync(string nameOrPinyin);

    #endregion

    #region 用户查询 (供 Auth 模块使用)

    /// <summary>根据用户ID获取基本信息 (不含 PasswordHash)</summary>
    Task<UserBasicDto?> GetUserBasicInfoAsync(Guid userId);

    /// <summary>根据用户名获取凭据信息 (含 PasswordHash, 仅供密码验证)</summary>
    Task<UserCredentialDto?> GetUserByUsernameAsync(string username);

    /// <summary>更新用户密码哈希 (BCrypt hash 升级场景)</summary>
    Task UpdateUserPasswordHashAsync(Guid userId, string newPasswordHash);

    #endregion
}

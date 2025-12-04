using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Infrastructure.Services;

/// <summary>
/// 跨模块查询服务接口
/// 提供模块间只读数据访问，避免直接跨模块注入Repository
/// </summary>
/// <remarks>
/// 设计原则：
/// - 轻量封装：不引入框架级复杂性，仅封装跨模块查询
/// - 返回DTO：防止Entity泄露，符合Bounded Context
/// - 批量优先：提供批量查询方法，避免N+1问题
/// - 只读查询：使用AsNoTracking()优化性能
/// </remarks>
public interface ICrossModuleQueryService
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

    #region 医案查询

    /// <summary>
    /// 获取医案基本信息（包含患者ID、诊断摘要）
    /// </summary>
    /// <param name="medicalCaseId">医案ID</param>
    /// <returns>医案基本信息DTO，包含关联的TCMDiagnosis</returns>
    Task<MedicalCaseBasicDto?> GetMedicalCaseBasicInfoAsync(Guid medicalCaseId);

    /// <summary>
    /// 批量获取医案基本信息
    /// </summary>
    /// <param name="medicalCaseIds">医案ID集合</param>
    /// <returns>医案ID到基本信息的字典，包含关联的TCMDiagnosis</returns>
    Task<Dictionary<Guid, MedicalCaseBasicDto>> GetMedicalCasesBasicInfoAsync(IEnumerable<Guid> medicalCaseIds);

    #endregion

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
}

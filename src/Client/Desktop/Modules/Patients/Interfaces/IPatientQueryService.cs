using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Patients.Interfaces;

/// <summary>
/// 患者查询服务接口 - UltraThink双层架构精简版（仅核心查询功能）
/// 职责：查询和搜索操作
/// </summary>
public interface IPatientQueryService
{

    #region 核心查询操作

    /// <summary>
    /// 分页查询患者
    /// </summary>
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query);

    /// <summary>
    /// 根据ID获取患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据身份证号查找患者
    /// </summary>
    Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard);

    /// <summary>
    /// 根据电话号码查找患者
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone);

    /// <summary>
    /// 搜索患者（按姓名或身份证）
    /// </summary>
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

    /// <summary>
    /// 获取患者统计数据
    /// </summary>
    Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync();

    #endregion 核心查询操作
}

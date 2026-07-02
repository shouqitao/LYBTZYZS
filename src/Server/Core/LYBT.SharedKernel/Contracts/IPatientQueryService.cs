namespace LYBT.SharedKernel.Contracts;

/// <summary>
/// 跨模块患者查询服务接口。供MedicalCase、Registration等模块查询患者信息。
/// 实现位于LYBT.Module.Patients。
/// </summary>
public interface IPatientQueryService
{
    /// <summary>
    /// 根据ID获取患者基本信息。
    /// </summary>
    /// <param name="patientId">患者ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>患者基本信息，不存在返回null</returns>
    Task<PatientBasicDto?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取患者基本信息。
    /// </summary>
    /// <param name="patientIds">患者ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>患者基本信息列表</returns>
    Task<IReadOnlyList<PatientBasicDto>> GetByIdsAsync(IEnumerable<Guid> patientIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// 跨模块患者基本信息DTO。
/// </summary>
public record PatientBasicDto
{
    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 手机号码
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// 身份证号
    /// </summary>
    public string? IdNumber { get; init; }
}



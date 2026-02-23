using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 患者数据源接口
/// </summary>
public interface IPatientDataSource : IDataSourceBase<PatientDetailDto, PatientInputDto>
{
    /// <summary>
    /// 搜索患者（姓名、电话、证件号）
    /// </summary>
    Task<List<PatientDetailDto>> SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>
    /// 根据证件号获取患者
    /// </summary>
    Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default);

    /// <summary>
    /// 恢复已删除的患者
    /// </summary>
    Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量删除患者
    /// </summary>
    Task<BatchOperationResultDto> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);
}

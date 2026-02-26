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

    // Sprint 4 X2 扩展方法
    // OpenSpec: SYNC-D02 - 过渡态方法

    /// <summary>
    /// T4-X2-09: 批量导入患者 (Excel解析后的数据)
    /// </summary>
    Task<BatchOperationResultDto> BatchImportAsync(List<PatientInputDto> items, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-10: 获取导出数据
    /// </summary>
    Task<List<PatientDetailDto>> GetAllForExportAsync(string? keyword = null, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-11: 检查患者是否有关联医案
    /// </summary>
    Task<bool> HasMedicalCasesAsync(Guid patientId, CancellationToken ct = default);

    /// <summary>
    /// T4-X2-12: 批量检查患者关联医案
    /// </summary>
    Task<Dictionary<Guid, bool>> BatchCheckReferencesAsync(List<Guid> patientIds, CancellationToken ct = default);
}

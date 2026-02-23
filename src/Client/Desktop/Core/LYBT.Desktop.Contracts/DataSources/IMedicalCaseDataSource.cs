using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 医案数据源接口 - 聚合根操作
/// </summary>
public interface IMedicalCaseDataSource : IDataSourceBase<MedicalCaseDetailDto, MedicalCaseInputDto>
{
    /// <summary>
    /// 聚合保存医案（MedicalCase + Consultation + Prescription）
    /// </summary>
    Task<MedicalCaseDetailDto> SaveAsync(MedicalCaseInputDto input, CancellationToken ct = default);

    /// <summary>
    /// 完成医案（设置状态为 Completed）
    /// </summary>
    Task<bool> CompleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 取消医案（软删除，设置 IsDeleted=true）
    /// </summary>
    Task<bool> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// 获取医案详情（包含 Consultation + Prescription + Items）
    /// </summary>
    Task<MedicalCaseDetailDto?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 查询医案列表
    /// </summary>
    Task<(List<MedicalCaseDetailDto> Items, int Total)> QueryAsync(
        Guid? patientId = null,
        Guid? userId = null,
        MedicalCaseStatus? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>
    /// 获取患者的医案列表
    /// </summary>
    Task<List<MedicalCaseDetailDto>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
}

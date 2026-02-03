using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.DataSources;

/// <summary>
/// 医案数据源接口 - 聚合根操作
/// OpenSpec: implement-local-mode
/// </summary>
public interface IMedicalCaseDataSource : IDataSourceBase<MedicalCase>
{
    /// <summary>
    /// 保存医案（聚合保存：MedicalCase + Consultation + Prescription）
    /// </summary>
    Task<MedicalCase> SaveAsync(MedicalCase entity, CancellationToken ct = default);

    /// <summary>
    /// 完成医案（设置状态为 Completed）
    /// </summary>
    Task<bool> CompleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 取消医案（设置状态为 Cancelled）
    /// </summary>
    Task<bool> CancelAsync(Guid id, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// 获取医案详情（包含 Consultation + Prescription + Items）
    /// </summary>
    Task<MedicalCase?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 查询医案列表
    /// </summary>
    /// <param name="patientId">患者ID（可选）</param>
    /// <param name="userId">医生ID（可选）</param>
    /// <param name="status">状态过滤（可选）</param>
    /// <param name="startDate">开始日期（可选）</param>
    /// <param name="endDate">结束日期（可选）</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <param name="ct">取消令牌</param>
    Task<(List<MedicalCase> Items, int Total)> QueryAsync(
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
    Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
}

using System.Threading;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 患者数据仓储接口
/// List 返回轻量 ListDto，Detail 返回完整 DetailDto。
/// </summary>
public interface IPatientRepository
{
    /// <summary>
    /// 分页查询患者列表 (返回轻量级 ListDto)
    /// </summary>
    Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken ct = default);

    /// <summary>
    /// 根据 ID 获取患者详情 (返回完整 DetailDto)
    /// </summary>
    Task<PatientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 创建新患者
    /// </summary>
    Task<PatientDetailDto> CreateAsync(PatientInputDto patient, CancellationToken ct = default);

    /// <summary>
    /// 更新患者信息
    /// </summary>
    Task<PatientDetailDto> UpdateAsync(PatientInputDto patient, CancellationToken ct = default);

    /// <summary>
    /// 删除患者 (软删除)
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 搜索患者 (基于关键词，返回 ListDto)
    /// </summary>
    Task<List<PatientListDto>> SearchAsync(string keyword, CancellationToken ct = default);

    /// <summary>
    /// 根据身份证号获取患者详情
    /// </summary>
    Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber, CancellationToken ct = default);

    #region 批量导入/导出功能

    /// <summary>
    /// 批量导入患者数据 (仅远程模式支持)
    /// </summary>
    Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request, CancellationToken ct = default);

    /// <summary>
    /// 下载患者导入模板 (仅远程模式支持)
    /// </summary>
    Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default);

    /// <summary>
    /// 导出患者数据到 Excel (仅远程模式支持)
    /// </summary>
    Task<byte[]?> ExportPatientsAsync(string? keyword = null, CancellationToken ct = default);

    #endregion

    #region 恢复和批量操作

    /// <summary>
    /// 恢复已删除的患者
    /// </summary>
    Task<PatientDetailDto?> RestoreAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 批量删除患者
    /// </summary>
    Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids, CancellationToken ct = default);

    #endregion
}

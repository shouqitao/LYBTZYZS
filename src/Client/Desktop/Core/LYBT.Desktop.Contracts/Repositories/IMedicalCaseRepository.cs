using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Contracts.Repositories;

/// <summary>
/// 医案数据仓储接口
/// List 返回轻量 MedicalCaseListDto，Detail 返回完整 MedicalCaseDetailDto。
/// </summary>
public interface IMedicalCaseRepository
{
    /// <summary>
    /// 分页查询医案列表 (返回轻量级 ListDto)
    /// </summary>
    Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

    /// <summary>
    /// 搜索医案 (返回 DetailDto，支持跨医生查询)
    /// </summary>
    Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
        string? patientName = null,
        string? diagnosisKeyword = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// 根据 ID 获取医案详情 (返回完整 DetailDto)
    /// </summary>
    Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// 统一查询医案
    /// </summary>
    Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query);

    /// <summary>
    /// 创建医案 (Epic #1961: 统一 MedicalCaseInputDto)
    /// </summary>
    Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto);

    /// <summary>
    /// 更新医案 (Epic #1961: 统一 MedicalCaseInputDto)
    /// </summary>
    Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto);

    /// <summary>
    /// 删除医案 (软删除)
    /// </summary>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// 关闭医案 (直接标记为 Completed)
    /// Epic #1676 Phase 4 Task 4.4
    /// </summary>
    Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId);

    /// <summary>
    /// 聚合保存医案 (诊断+处方一次性保存)
    /// </summary>
    Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto);

    /// <summary>
    /// 设置处方标志
    /// </summary>
    Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request);

    /// <summary>
    /// 更新医案状态
    /// </summary>
    Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request);

    /// <summary>
    /// 取消医案
    /// </summary>
    Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request);

    /// <summary>
    /// 挂起医案
    /// </summary>
    Task<MedicalCaseDetailDto?> SuspendAsync(Guid id, ConsultationInputDto? request);

    #region 批量操作

    /// <summary>
    /// 批量删除医案
    /// </summary>
    Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids);

    #endregion
}

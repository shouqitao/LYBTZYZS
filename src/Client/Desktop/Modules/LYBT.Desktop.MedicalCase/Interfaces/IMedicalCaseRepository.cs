using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 医案数据仓储接口 - RESTful设计
    /// List返回轻量MedicalCaseListDto，Detail返回完整MedicalCaseDetailDto
    /// </summary>
    public interface IMedicalCaseRepository
    {
        /// <summary>
        /// 分页查询医案列表（返回轻量级ListDto）
        /// </summary>
        Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 搜索医案（返回DetailDto，支持跨医生查询）
        /// OpenSpec: fix-history-copy-all-patients - 用于历史医案复制查看全部患者功能
        /// </summary>
        Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// 根据ID获取医案详情（返回完整DetailDto）
        /// </summary>
        Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 统一查询医案
        /// OpenSpec: optimize-medicalcase-api - 整合多种查询方式
        /// </summary>
        Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query);
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto);
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto);
        Task<bool> DeleteAsync(Guid id);

        // OpenSpec: consolidate-medicalcase-detail-queries - 废弃方法已删除
        // - GetByPatientIdAsync: 使用QueryAsync(QueryType=ByPatient)
        // - GetByIdWithDetailsAsync: 使用GetByIdAsync
        // - GetUnfinishedCaseByPatientIdAsync: 使用QueryAsync(QueryType=Unfinished)

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>权限详情</returns>
        Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId);

        /// <summary>
        /// 聚合保存医案（诊断+处方一次性保存）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.5)
        /// 简化前端保存逻辑，减少API调用次数
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="dto">聚合输入DTO（包含诊断和处方数据）</param>
        /// <returns>更新后的医案详情</returns>
        Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto);

        /// <summary>
        /// 批量获取医案详情（解决N+1查询问题）
        /// OpenSpec: consolidate-medicalcase-detail-queries
        /// 用于历史处方选择等需要批量获取详情的场景
        /// </summary>
        /// <param name="ids">医案ID列表（最多50个）</param>
        /// <returns>医案详情列表</returns>
        Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids);

        // ========================================
        // OpenSpec: simplify-desktop-data-layer - Phase 1
        // 以下方法从Service层迁移，统一数据访问入口
        // ========================================

        /// <summary>
        /// 设置处方标志
        /// OpenSpec: simplify-desktop-data-layer (Phase 1)
        /// </summary>
        Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request);

        /// <summary>
        /// 更新医案状态
        /// OpenSpec: simplify-desktop-data-layer (Phase 1)
        /// </summary>
        Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request);

        /// <summary>
        /// 取消医案
        /// OpenSpec: simplify-desktop-data-layer (Phase 1)
        /// </summary>
        Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request);

        /// <summary>
        /// 暂存医案草稿
        /// OpenSpec: simplify-desktop-data-layer (Phase 1)
        /// </summary>
        Task<MedicalCaseDetailDto?> SaveDraftAsync(Guid id, ConsultationInputDto? request);
    }
}

using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Contracts.Api
{
    /// <summary>
    /// 医疗案例API客户端接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IMedicalCaseApi
    {
        /// <summary>
        /// 获取医疗案例列表（支持分页和查询）
        /// OpenSpec: fix-history-copy-all-patients - 添加includeAllDoctors参数
        /// OpenSpec: post-release-cleanup - 统一返回MedicalCaseListDto
        /// </summary>
        [Refit.Get("/api/v1/medicalcases")]
        Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] bool includeAllDoctors = false);

        // OpenSpec: post-release-cleanup - GetMedicalCasesListAsync已合并到GetMedicalCasesAsync
        // 原GET /list端点已删除

        /// <summary>
        /// 统一查询医案端点
        /// OpenSpec: optimize-medicalcase-api - 整合多种查询方式
        /// </summary>
        /// <param name="queryType">查询类型</param>
        /// <param name="patientId">患者ID（ByPatient/Unfinished/Recent时必填）</param>
        /// <param name="doctorId">医生ID（可选）</param>
        /// <param name="keyword">关键词（可选）</param>
        /// <param name="pageIndex">页码（默认1）</param>
        /// <param name="pageSize">每页数量（默认20）</param>
        /// <param name="includeAllDoctors">是否包含所有医生（管理员用）</param>
        /// <param name="limit">限制数量（Recent查询时使用）</param>
        [Refit.Get("/api/v1/medicalcases/query")]
        Task<ApiResponse<PagedResult<MedicalCaseListDto>>> QueryMedicalCasesAsync(
            [Refit.Query] MedicalCaseQueryType queryType = MedicalCaseQueryType.All,
            [Refit.Query] Guid? patientId = null,
            [Refit.Query] Guid? doctorId = null,
            [Refit.Query] string? keyword = null,
            [Refit.Query] int pageIndex = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] bool includeAllDoctors = false,
            [Refit.Query] int? limit = null);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDetailDto>> GetMedicalCaseByIdAsync(Guid id);

        // OpenSpec: consolidate-medicalcase-detail-queries - GetMedicalCasesByPatientIdAsync已删除
        // 使用QueryMedicalCasesAsync(QueryType=ByPatient)

        /// <summary>
        /// 获取待看诊医案列表（Status=Draft/Active）
        /// Epic #1583 - Phase 5
        /// Epic #2210 Phase 3: 多医生数据隔离（从JWT获取当前用户）
        /// OpenSpec: standardize-api-naming - 保留此API，DTO结构与QueryMedicalCasesAsync不兼容
        /// 返回PendingMedicalCaseDto（含Type字段），QueryMedicalCasesAsync返回MedicalCaseListDto（含CaseStatus字段）
        /// OpenSpec: unify-pending-query-api - 添加patientId参数支持按患者筛选
        /// </summary>
        /// <param name="patientId">患者ID（可选）- 传入时仅返回该患者的待看诊医案</param>
        [Refit.Get("/api/v1/medicalcases/pending")]
        Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync([Refit.Query] Guid? patientId = null);

        // QueryMedicalCasesAsync 已删除 - 与 SearchMedicalCasesAsync 功能重复
        // OpenSpec: standardize-desktop-api-layer

        /// <summary>
        /// 跨医案搜索（分页版）
        /// OpenSpec: consolidate-medicalcase-queries (LIFECYCLE-015)
        /// 支持按患者名称、诊断关键词等条件查询，返回分页结果
        /// </summary>
        [Refit.Get("/api/v1/medicalcases/search")]
        Task<ApiResponse<PagedResult<MedicalCaseDetailDto>>> SearchMedicalCasesAsync(
            [Refit.Query] string? patientName = null,
            [Refit.Query] string? diagnosisKeyword = null,
            [Refit.Query] DateTime? startDate = null,
            [Refit.Query] DateTime? endDate = null,
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20);


        // OpenSpec: consolidate-medicalcase-detail-queries - GetMedicalCaseByIdWithDetailsAsync已删除，使用GetMedicalCaseByIdAsync
        /// <summary>
        /// 创建医疗案例
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        [Refit.Post("/api/v1/medicalcases")]
        Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync([Refit.Body] MedicalCaseInputDto request);

        // ========== CreateMedicalCaseWithDetailsAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
        // Server端点POST /api/v1/medicalcases/with-details 不存在，且无调用者

        // OpenSpec: simplify-medicalcase-api - UpdateConsultationAsync已删除
        // 诊断更新通过聚合保存 SaveAsync 处理

        /// <summary>
        /// 删除医疗案例（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// OpenSpec: standardize-api-naming - 统一使用ApiResponse返回类型
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}")]
        Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);

        // ========== SoftDeleteMedicalCaseAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
        // Server端点DELETE /api/v1/medicalcases/{id}/soft 不存在，且无调用者

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        // CompleteStep1Async和ResetConsultationStepsAsync已移除 - 简化业务流程，移除Step概念

        // OpenSpec: simplify-medicalcase-api - Ghost APIs已删除
        // - ClearPrescriptionAsync: Server端从未实现
        // - ImportFormulaIntoPrescriptionAsync: Server端从未实现

        // OpenSpec: simplify-medicalcase-api - 独立Prescription CRUD接口已删除
        // - CreatePrescriptionAsync: 通过SaveAsync创建
        // - UpdatePrescriptionAsync: 通过SaveAsync更新
        // - DeletePrescriptionAsync: 通过SaveAsync设置NeedsPrescription=false触发

        /// <summary>
        /// 标记是否开处方
        /// Task 3.4 (#1661): RadioBox变化时自动保存
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{medicalCaseId}/prescription-flag")]
        Task<ApiResponse<MedicalCaseDetailDto>> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            [Refit.Body] SetPrescriptionFlagRequest request);

        // ========== Epic #1676 Phase 4 Task 4.1 - 新增专用API ==========

        // OpenSpec: consolidate-medicalcase-detail-queries - GetUnfinishedCaseByPatientIdAsync已删除
        // 使用QueryMedicalCasesAsync(QueryType=Unfinished)

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/close")]
        Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);

        /// <summary>
        /// 暂存医案（保存草稿）
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
        /// 保存当前数据，设置状态为Draft，不触发完成验证
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/draft")]
        Task<ApiResponse<MedicalCaseDetailDto>> SaveDraftAsync(
            Guid id,
            [Refit.Body] ConsultationInputDto? request = null);

        /// <summary>
        /// 取消医案（统一为软删除 + 审计日志）
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/cancel")]
        Task<ApiResponse<MedicalCaseDetailDto>> CancelMedicalCaseAsync(
            Guid id,
            [Refit.Body] CancelMedicalCaseRequestDto? request = null);

        /// <summary>
        /// 更新医案状态
        /// Issue #2243: 修复SaveDraft和Complete功能
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/status")]
        Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(
            Guid id,
            [Refit.Body] MedicalCaseStatusInputDto request);

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <returns>权限详情</returns>
        [Refit.Get("/api/v1/medicalcases/{id}/permissions")]
        Task<ApiResponse<MedicalCasePermissionDto>> GetPermissionsAsync(Guid id);

        /// <summary>
        /// 获取医案审计日志
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="page">页码（默认1）</param>
        /// <param name="pageSize">每页数量（默认20）</param>
        /// <returns>分页的审计日志列表</returns>
        [Refit.Get("/api/v1/medicalcases/{id}/audit-logs")]
        Task<ApiResponse<MedicalCaseAuditLogPagedResultDto>> GetAuditLogsAsync(
            Guid id,
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20);

        /// <summary>
        /// 聚合保存医案（诊断+处方一次性保存）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.5)
        /// 简化前端保存逻辑，减少API调用次数
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">统一输入DTO（包含诊断和处方数据）</param>
        /// <returns>更新后的医案详情</returns>
        [Refit.Put("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(
            Guid id,
            [Refit.Body] MedicalCaseInputDto request);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除医案
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量获取医案详情（解决N+1查询问题）
        /// OpenSpec: consolidate-medicalcase-detail-queries
        /// 用于历史处方选择等需要批量获取详情的场景
        /// </summary>
        /// <param name="request">批量查询参数（最多50个ID）</param>
        /// <returns>医案详情列表</returns>
        [Refit.Post("/api/v1/medicalcases/batch-details")]
        Task<ApiResponse<List<MedicalCaseDetailDto>>> GetBatchDetailsAsync([Refit.Body] BatchDetailQueryDto request);
    }
}

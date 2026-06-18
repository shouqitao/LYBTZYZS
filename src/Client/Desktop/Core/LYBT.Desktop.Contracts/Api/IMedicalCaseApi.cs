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
        /// </summary>
        [Refit.Get("/api/v1/medicalcases")]
        Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCasesAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null,
            [Refit.Query] bool includeAllDoctors = false);
        // 原GET /list端点已删除

        /// <summary>
        /// 统一查询医案端点
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
        // 使用QueryMedicalCasesAsync(QueryType=ByPatient)

        /// <summary>
        /// 获取待看诊医案列表（Status=Draft/Active）
        /// Epic #1583 - Phase 5
        /// Epic #2210 Phase 3: 多医生数据隔离（从JWT获取当前用户）
        /// 返回PendingMedicalCaseDto（含Type字段），QueryMedicalCasesAsync返回MedicalCaseListDto（含CaseStatus字段）
        /// </summary>
        /// <param name="patientId">患者ID（可选）- 传入时仅返回该患者的待看诊医案</param>
        [Refit.Get("/api/v1/medicalcases/pending")]
        Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync([Refit.Query] Guid? patientId = null);

        // QueryMedicalCasesAsync 已删除 - 与 SearchMedicalCasesAsync 功能重复

        /// <summary>
        /// 跨医案搜索（分页版）
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
        /// <summary>
        /// 创建医疗案例
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        [Refit.Post("/api/v1/medicalcases")]
        Task<ApiResponse<MedicalCaseDetailDto>> CreateMedicalCaseAsync([Refit.Body] MedicalCaseInputDto request);

        // ========== CreateMedicalCaseWithDetailsAsync 已删除==========
        // Server端点POST /api/v1/medicalcases/with-details 不存在，且无调用者
        // 诊断更新通过聚合保存 SaveAsync 处理

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        [Refit.Delete("/api/v1/medicalcases/{id}")]
        Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);

        // ========== SoftDeleteMedicalCaseAsync 已删除==========
        // Server端点DELETE /api/v1/medicalcases/{id}/soft 不存在，且无调用者

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        // CompleteStep1Async和ResetConsultationStepsAsync已移除 - 简化业务流程，移除Step概念
        // - ClearPrescriptionAsync: Server端从未实现
        // - ImportFormulaIntoPrescriptionAsync: Server端从未实现
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
        // 使用QueryMedicalCasesAsync(QueryType=Unfinished)

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/close")]
        Task<ApiResponse<MedicalCaseDetailDto>> CloseCaseAsync(Guid id);

        /// <summary>
        /// 挂起医案
        /// 挂起医案，设置状态为Suspended，不触发完成验证
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/suspend")]
        Task<ApiResponse<MedicalCaseDetailDto>> SuspendAsync(
            Guid id,
            [Refit.Body] ConsultationInputDto? request = null);

        /// <summary>
        /// 取消医案（统一为软删除 + 审计日志）
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/cancel")]
        Task<Refit.IApiResponse> CancelMedicalCaseAsync(
            Guid id,
            [Refit.Body] CancelMedicalCaseRequestDto? request = null);

        /// <summary>
        /// 更新医案状态
        /// Issue #2243: 修复Suspend和Complete功能
        /// </summary>
        [Refit.Put("/api/v1/medicalcases/{id}/status")]
        Task<ApiResponse<MedicalCaseDetailDto>> UpdateStatusAsync(
            Guid id,
            [Refit.Body] MedicalCaseStatusInputDto request);

        /// <summary>
        /// 聚合保存医案（诊断+处方一次性保存）
        /// 简化前端保存逻辑，减少API调用次数
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">统一输入DTO（包含诊断和处方数据）</param>
        /// <returns>更新后的医案详情</returns>
        [Refit.Put("/api/v1/medicalcases/{id}")]
        Task<ApiResponse<MedicalCaseDetailDto>> SaveAsync(
            Guid id,
            [Refit.Body] MedicalCaseInputDto request);
        /// <summary>
        /// 批量删除医案
        /// </summary>
        [Refit.Post("/api/v1/medicalcases/batch-delete")]
        Task<ApiResponse<BatchOperationResultDto>> BatchDeleteAsync([Refit.Body] BatchDeleteInputDto request);

        /// <summary>
        /// 批量获取医案详情（解决N+1查询问题）
    }
}

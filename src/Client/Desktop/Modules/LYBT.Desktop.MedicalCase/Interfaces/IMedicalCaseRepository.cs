using LYBT.Shared.Models.Contracts.Common;
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

        /// <summary>
        /// 根据患者ID获取医案列表
        /// </summary>
        [Obsolete("Use QueryAsync with QueryType=ByPatient instead. Will be removed in v2.0")]
        Task<List<MedicalCaseDetailDto>> GetByPatientIdAsync(Guid patientId);

        // ========== CreateWithDetailsAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
        // Server端点POST /api/v1/medicalcases/with-details 不存在，且无调用者

        /// <summary>
        /// OpenSpec: optimize-medicalcase-api - 此方法已废弃，内部已改用统一端点
        /// </summary>
        [Obsolete("Internal implementation now uses unified endpoint. Will be removed in v2.0")]
        Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id);

        // OpenSpec: simplify-medicalcase-api - UpdateConsultationAsync已删除
        // 诊断更新通过聚合保存 SaveAsync 处理


        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        // CompleteStep1Async和ResetConsultationStepsAsync已移除 - 简化业务流程，移除Step概念

        // OpenSpec: simplify-medicalcase-api - Ghost APIs已删除
        // - ClearPrescriptionAsync: Server端从未实现
        // - ImportFormulaIntoPrescriptionAsync: Server端从未实现

        // OpenSpec: simplify-medicalcase-api - 独立Prescription CRUD接口已删除
        // - CreatePrescriptionAsync: 通过SaveAsync创建
        // - UpdatePrescriptionAsync: 通过SaveAsync更新
        // - DeletePrescriptionAsync: 通过SaveAsync设置NeedsPrescription=false触发

        // ========== Epic #1676 Phase 4 Task 4.4 - Desktop端新增方法 ==========

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// Epic #2210 Task 3.1.4: 添加doctorId参数
        /// OpenSpec: multi-doctor-unfinished-case - 添加checkAllDoctors参数
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（当checkAllDoctors=false时使用）</param>
        /// <param name="checkAllDoctors">是否查询所有医生的未完成医案（用于多医生场景检测）</param>
        [Obsolete("Use QueryAsync with QueryType=Unfinished instead. Will be removed in v2.0")]
        Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
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
    }
}

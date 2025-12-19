using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IMedicalCaseRepository
    {
        Task<PagedResult<MedicalCaseDetailDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 获取医案列表（返回MedicalCaseListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        Task<PagedResult<MedicalCaseListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 获取医案分页列表（包含所有医生的数据）
        /// OpenSpec: fix-history-copy-all-patients - 用于历史医案复制查看全部患者功能
        /// </summary>
        Task<PagedResult<MedicalCaseDetailDto>> GetPagedIncludeAllDoctorsAsync(int page = 1, int pageSize = 20, string? keyword = null);

        Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id);
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto);
        /// <summary>Epic #1961: 使用统一的 MedicalCaseInputDto</summary>
        Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<MedicalCaseDetailDto>> GetByPatientIdAsync(Guid patientId);

        // ========== CreateWithDetailsAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
        // Server端点POST /api/v1/medicalcases/with-details 不存在，且无调用者

        Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="dto">诊断更新信息</param>
        /// <returns>更新后的诊断信息</returns>
        Task<ConsultationDetailDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationInputDto dto);

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        Task<List<MedicalCaseDetailDto>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null);

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        // CompleteStep1Async和ResetConsultationStepsAsync已移除 - 简化业务流程，移除Step概念

        /// <summary>
        /// 清空处方内容（保留处方框架）
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        Task ClearPrescriptionAsync(Guid medicalCaseId);

        /// <summary>
        /// 从配方导入处方
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        Task<PrescriptionDetailDto> ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId);

        /// <summary>
        /// 为已存在的医案创建处方（Issue #1608补充）
        /// </summary>
        Task<PrescriptionDetailDto> CreatePrescriptionAsync(Guid medicalCaseId, PrescriptionInputDto dto);

        /// <summary>
        /// 更新医案的处方（Issue #1608补充）
        /// </summary>
        Task<PrescriptionDetailDto> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionInputDto dto);

        /// <summary>
        /// 删除医案的处方（Issue #1608补充）
        /// </summary>
        Task DeletePrescriptionAsync(Guid medicalCaseId);

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
        Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        Task<bool> CloseCaseAsync(Guid medicalCaseId);

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

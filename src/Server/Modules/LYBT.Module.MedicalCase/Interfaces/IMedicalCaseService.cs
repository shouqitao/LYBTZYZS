using LYBT.Module.MedicalCase.Dtos;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 病案Service接口 - Epic #1612 重构版
    /// 遵循Write/Read/Helper Layer分离原则
    ///
    /// 业务规则：
    /// - AR-001: 所有Write操作必须通过MedicalCase聚合根
    /// - BF-002: 三步流程验证（辨证→开方标记→处方）
    /// - AR-003: 一诊一方约束
    /// </summary>
    public interface IMedicalCaseService
    {
        // ========== Write Layer（写操作，通过聚合根）==========

        /// <summary>
        /// 创建新病案
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="visitDate">就诊日期</param>
        /// <param name="doctorId">医生ID</param>
        /// <returns>创建的病案实体</returns>
        Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate, Guid doctorId);

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// Epic #1612: 通过聚合根协调Consultation更新
        /// Epic #1731: 添加权限检查（currentUserId）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="request">辨证信息请求</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员（默认false）</param>
        /// <returns>更新后的病案实体（包含Consultation）</returns>
        Task<MedicalCaseEntity?> UpdateConsultationAsync(
            Guid medicalCaseId,
            ConsultationInputDto request,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// Epic #1612: 动态流程控制，用户可选择跳过处方
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="needsPrescription">是否需要开处方</param>
        /// <returns>更新后的病案实体</returns>
        Task<MedicalCaseEntity?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// Epic #1612: 通过聚合根创建Prescription
        /// 业务规则：AR-003（一诊一方约束验证）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="request">处方创建请求</param>
        /// <returns>创建的处方实体</returns>
        Task<PrescriptionEntity?> CreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionCreateDto request);

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// Epic #1612: 通过聚合根更新Prescription
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="request">处方更新请求</param>
        /// <returns>更新后的处方实体</returns>
        Task<PrescriptionEntity?> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            PrescriptionEditDto request,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 删除处方（软删除）
        /// Epic #1612: 通过聚合根删除Prescription
        /// 业务规则：仅允许删除未打印处方
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <returns>删除是否成功</returns>
        Task<bool> DeletePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 更新病案状态
        /// Epic #1612: 支持Active/Completed/Cancelled状态流转
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="status">目标状态</param>
        /// <returns>更新后的病案实体</returns>
        Task<MedicalCaseEntity?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status);

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612: 验证三步流程完整性后标记为Completed
        /// 业务规则：BF-002（三步流程验证）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>完成后的病案实体</returns>
        Task<MedicalCaseEntity?> CompleteAsync(Guid medicalCaseId);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>关闭是否成功</returns>
        Task<bool> CloseCaseAsync(Guid id);

        /// <summary>
        /// 暂存医案（保存草稿）
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-010)
        /// 业务规则：保存当前数据，设置状态为Draft，不触发完成验证
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <param name="request">可选的诊断信息更新</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>更新后的病案实体</returns>
        Task<MedicalCaseEntity?> SaveDraftAsync(
            Guid id,
            ConsultationInputDto? request,
            Guid operatorId,
            bool isAdmin = false);

        /// <summary>
        /// 取消医案
        /// OpenSpec: refactor-medicalcase-api (LIFECYCLE-011)
        /// 业务规则：设置状态为Cancelled，需要审计理由（非当天本人操作时）
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="reason">取消原因（审计时必填）</param>
        /// <returns>更新后的病案实体</returns>
        Task<MedicalCaseEntity?> CancelAsync(
            Guid id,
            Guid operatorId,
            bool isAdmin = false,
            string? reason = null);

        /// <summary>
        /// 删除病案（软删除）
        /// OpenSpec: clarify-cancel-consultation-logic
        /// 业务规则：使用BaseRepository默认软删除机制（IsDeleted=true）
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>删除是否成功</returns>
        Task<bool> DeleteAsync(Guid id);

        // ========== Read Layer（读操作，独立查询）==========

        /// <summary>
        /// 根据ID获取病案详情
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription（含Items）
        /// 优化：避免N+1查询
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>病案实体（包含完整关联数据）</returns>
        Task<MedicalCaseEntity?> GetByIdAsync(Guid id);

        /// <summary>
        /// 查询病案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// </summary>
        /// <param name="status">病案状态（可选）</param>
        /// <param name="patientId">患者ID（可选）</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<MedicalCaseEntity>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize);

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回病案的所有历史辨证记录
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>辨证记录DTO列表</returns>
        Task<List<ConsultationDto>> GetConsultationListAsync(Guid medicalCaseId);

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回病案的所有历史处方记录
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>处方DTO列表</returns>
        Task<List<MedicalCasePrescriptionDto>> GetPrescriptionListAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// Epic #2210 Task 3.1.2: 添加doctorId参数
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="doctorId">医生ID（为Guid.Empty时不筛选医生）</param>
        /// <returns>未完成的病案实体（包含关联数据），若无则返回null</returns>
        Task<MedicalCaseEntity?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId);

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// Epic #2210 Phase 3: P0 Bug修复 - 添加缺失的API端点
        /// 业务规则：返回当前医生的所有Active状态医案的患者信息
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>待诊队列列表</returns>
        Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId);

        /// <summary>
        /// 获取所有待看诊队列（管理员专用）
        /// 业务规则：返回所有Active状态医案的患者信息，不限定医生
        /// </summary>
        /// <returns>待诊队列列表</returns>
        Task<List<PendingMedicalCaseDto>> GetAllPendingCasesAsync();

    }
}

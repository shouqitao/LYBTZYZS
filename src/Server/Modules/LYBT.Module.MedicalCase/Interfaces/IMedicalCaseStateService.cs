using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 病案状态服务接口 - 状态管理操作
    /// Phase 3: 从IMedicalCaseService拆分，遵循CQRS原则
    /// 职责：UpdateStatus, Complete, CloseCase, SaveDraft, Cancel等状态流转操作
    /// </summary>
    public interface IMedicalCaseStateService
    {
        /// <summary>
        /// 更新病案状态
        /// Epic #1612: 支持Active/Completed/Cancelled状态流转
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="status">目标状态</param>
        /// <returns>更新后的病案实体</returns>
        Task<MedicalCase?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status);

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612: 验证三步流程完整性后标记为Completed
        /// 业务规则：BF-002（三步流程验证）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <returns>完成后的病案实体</returns>
        Task<MedicalCase?> CompleteAsync(Guid medicalCaseId);

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>关闭是否成功</returns>
        /// <summary>
    /// 关闭医案（直接标记为Completed）
    /// OpenSpec: optimize-medicalcase-api - 返回完整医案实体用于DTO映射
    /// </summary>
    /// <param name="id">医案ID</param>
    /// <returns>更新后的医案实体，不存在返回null</returns>
    Task<MedicalCase?> CloseCaseAsync(Guid id);

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
        Task<MedicalCase?> SaveDraftAsync(
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
        Task<MedicalCase?> CancelAsync(
            Guid id,
            Guid operatorId,
            bool isAdmin = false,
            string? reason = null);
    }
}

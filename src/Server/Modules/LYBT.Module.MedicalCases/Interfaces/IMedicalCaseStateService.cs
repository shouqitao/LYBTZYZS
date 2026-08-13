using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 医案状态服务接口 - 状态管理操作
    /// Phase 3: 从IMedicalCaseService拆分，遵循CQRS原则
    /// 职责：UpdateStatus, Complete, CloseCase, Suspend, Cancel等状态流转操作
    /// </summary>
    public interface IMedicalCaseStateService
    {
        /// <summary>
        /// 更新医案状态
        /// 支持 Suspended/Active/Completed 状态流转
        /// </summary>
        /// <summary>
        /// 更新医案状态（P1-10 2026-08-14: 统一处理 Completed——Completed 委托 CompleteAsync）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="status">目标状态</param>
        /// <param name="operatorId">操作者ID（Completed 分支使用）</param>
        /// <param name="isAdmin">是否管理员（Completed 分支使用）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的医案实体</returns>
        Task<MedicalCase?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status,
            Guid operatorId = default,
            bool isAdmin = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 统一完成医案入口
        /// skipWorkflowValidation=false: 验证 NeedsPrescription + 处方存在性 (BR-003)
        /// skipWorkflowValidation=true: 直接完成 (原 CloseCaseAsync 行为)
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="skipWorkflowValidation">是否跳过工作流验证</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<MedicalCase?> CompleteAsync(
            Guid medicalCaseId,
            Guid operatorId,
            bool isAdmin = false,
            bool skipWorkflowValidation = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 挂起医案（暂停处理）
        /// 业务规则：保存当前数据，设置状态为Suspended，不触发完成验证
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">可选的诊断信息更新</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的医案实体</returns>
        Task<MedicalCase?> SuspendAsync(
            Guid id,
            ConsultationInputDto? request,
            Guid operatorId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消医案（US-MC-014：物理删除）
        /// 级联清除聚合 + 审计记录 OperationType=Cancel；已完成医案不可取消（只可软删）
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="reason">取消原因（非当天本人取消时必填）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>被物理删除的医案实体（null=未找到）</returns>
        Task<MedicalCase?> CancelAsync(
            Guid id,
            Guid operatorId,
            bool isAdmin = false,
            string? reason = null,
            CancellationToken cancellationToken = default);
    }
}



using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Enums;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医案审计服务接口
    /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
    /// 负责记录医案的所有变更历史
    /// </summary>
    public interface IMedicalCaseAuditService
    {
        /// <summary>
        /// 记录医案变更日志
        /// </summary>
        /// <param name="before">变更前的医案状态（创建时为null）</param>
        /// <param name="after">变更后的医案状态</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="operatorName">操作者姓名</param>
        /// <param name="role">操作者角色</param>
        /// <param name="operationType">操作类型</param>
        /// <param name="reason">修改原因（历史医案修改时必填）</param>
        Task LogAsync(
            MedicalCaseEntity? before,
            MedicalCaseEntity after,
            Guid operatorId,
            string operatorName,
            UserRole role,
            AuditOperationType operationType,
            string? reason = null);

        /// <summary>
        /// 获取医案的审计日志列表
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>审计日志列表，按创建时间倒序</returns>
        Task<List<MedicalCaseAuditLog>> GetLogsAsync(Guid medicalCaseId);

        /// <summary>
        /// 获取医案的审计日志列表（分页）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>审计日志列表，按创建时间倒序</returns>
        Task<(List<MedicalCaseAuditLog> Logs, int TotalCount)> GetLogsPagedAsync(
            Guid medicalCaseId,
            int page = 1,
            int pageSize = 20);
    }
}

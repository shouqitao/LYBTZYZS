using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 医案命令服务接口 - 写操作
    /// Phase 3: 从IMedicalCaseService拆分，遵循CQRS原则
    /// 职责：Create, Update, Delete操作
    /// </summary>
    public interface IMedicalCaseCommandService
    {
        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="needsPrescription">是否需要开处方</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的医案实体</returns>
        Task<MedicalCase?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除医案（软删除）
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除是否成功</returns>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统一保存医案（支持创建和更新）
        /// - Id为null时：创建新MedicalCase（含Consultation，可选Prescription）
        /// - Id有值时：更新现有MedicalCase（含Consultation和Prescription）
        /// </summary>
        /// <param name="request">统一输入DTO（Id=null创建，Id有值更新）</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>保存后的医案实体（包含Consultation和Prescription）</returns>
        Task<MedicalCase?> SaveAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default);
        /// <summary>
        /// 批量删除医案
        /// </summary>
        /// <param name="ids">医案ID列表</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<LYBT.Shared.Models.Contracts.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default);

        /// <summary>
        /// 统一保存医案并返回详情DTO（含NotFound语义）
        /// </summary>
        Task<Result<MedicalCaseDetailDto>> SaveWithDetailAsync(
            MedicalCaseInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 标记是否需要开处方并返回详情DTO（含NotFound语义）
        /// </summary>
        Task<Result<MedicalCaseDetailDto>> SetPrescriptionFlagWithDetailAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 添加打印日志（成功回写打印状态，失败仅记录日志）
        /// </summary>
        Task<Result<bool>> AddPrintLogAsync(
            Guid medicalCaseId,
            int printType,
            bool isSuccess,
            string? printerName,
            Guid operatorId,
            string operatorName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 记录打印完成（回写打印状态 + 记录日志）
        /// </summary>
        Task<Result<bool>> RecordPrintAsync(
            Guid medicalCaseId,
            int printType,
            string? printerName,
            Guid operatorId,
            string operatorName,
            CancellationToken cancellationToken = default);
    }
}



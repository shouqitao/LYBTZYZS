using System.Threading;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

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
        /// 创建新医案
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="visitDate">就诊日期</param>
        /// <param name="doctorId">医生ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的医案实体</returns>
        Task<MedicalCase?> CreateAsync(Guid patientId, DateTime visitDate, Guid doctorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="request">辨证信息请求</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员（默认false）</param>
        /// <param name="editReason">修改原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的医案实体（包含Consultation）</returns>
        Task<MedicalCase?> UpdateConsultationAsync(
            Guid medicalCaseId,
            ConsultationInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            string? editReason = null,
            CancellationToken cancellationToken = default);

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
        /// 创建处方（三步流程Step 3a）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="request">处方创建请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建的处方实体</returns>
        Task<Prescription?> CreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionInputDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="request">处方更新请求</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="editReason">修改原因</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的处方实体</returns>
        Task<Prescription?> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            PrescriptionInputDto request,
            Guid currentUserId,
            bool isAdmin = false,
            string? editReason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除处方（软删除）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除是否成功</returns>
        Task<bool> DeletePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
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
        /// OpenSpec: simplify-medicalcase-dataflow Phase 2 - 统一SaveAsync
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

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除医案
        /// </summary>
        /// <param name="ids">医案ID列表</param>
        /// <param name="operatorId">操作者ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task<LYBT.Shared.Models.Common.Result<LYBT.Shared.Models.Contracts.Common.BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, Guid operatorId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}

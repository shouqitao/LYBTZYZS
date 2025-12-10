using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Module.MedicalCases.Interfaces
{
    /// <summary>
    /// 病案命令服务接口 - 写操作
    /// Phase 3: 从IMedicalCaseService拆分，遵循CQRS原则
    /// 职责：Create, Update, Delete操作
    /// </summary>
    public interface IMedicalCaseCommandService
    {
        /// <summary>
        /// 创建新病案
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="visitDate">就诊日期</param>
        /// <param name="doctorId">医生ID</param>
        /// <returns>创建的病案实体</returns>
        Task<MedicalCase?> CreateAsync(Guid patientId, DateTime visitDate, Guid doctorId);

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="request">辨证信息请求</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员（默认false）</param>
        /// <returns>更新后的病案实体（包含Consultation）</returns>
        Task<MedicalCase?> UpdateConsultationAsync(
            Guid medicalCaseId,
            ConsultationInputDto request,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="needsPrescription">是否需要开处方</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>更新后的病案实体</returns>
        Task<MedicalCase?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="request">处方创建请求</param>
        /// <returns>创建的处方实体</returns>
        Task<Prescription?> CreatePrescriptionAsync(
            Guid medicalCaseId,
            PrescriptionCreateDto request);

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="request">处方更新请求</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>更新后的处方实体</returns>
        Task<Prescription?> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            PrescriptionEditDto request,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 删除处方（软删除）
        /// </summary>
        /// <param name="medicalCaseId">病案ID</param>
        /// <param name="prescriptionId">处方ID</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>删除是否成功</returns>
        Task<bool> DeletePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            Guid currentUserId,
            bool isAdmin = false);

        /// <summary>
        /// 删除病案（软删除）
        /// </summary>
        /// <param name="id">病案ID</param>
        /// <returns>删除是否成功</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 保存医案聚合根（统一保存Consultation和Prescription）
        /// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
        /// 在单个事务中同时保存诊断和处方数据
        /// </summary>
        /// <param name="request">医案聚合输入DTO</param>
        /// <param name="currentUserId">当前操作用户ID</param>
        /// <param name="isAdmin">是否管理员</param>
        /// <returns>更新后的病案实体（包含Consultation和Prescription）</returns>
        Task<MedicalCase?> SaveAggregateAsync(
            MedicalCaseAggregateInputDto request,
            Guid currentUserId,
            bool isAdmin = false);
    }
}

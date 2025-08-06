using LYBT.Shared.Models.Common;
using LYBT.Models.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例服务接口 - 统筹整个诊疗流程
    /// </summary>
    public interface IMedicalCaseService
    {
        /// <summary>
        /// 创建医疗案例（从挂号开始）
        /// </summary>
        Task<MedicalCaseModel> CreateFromRegistrationAsync(Guid registrationId);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        Task<MedicalCaseModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取患者的所有医疗案例
        /// </summary>
        Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 获取医生今日的医疗案例
        /// </summary>
        Task<List<MedicalCaseModel>> GetTodayByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        Task<bool> UpdateStatusAsync(Guid id, MedicalCaseStatus newStatus);

        /// <summary>
        /// 开始看诊
        /// </summary>
        Task<bool> StartConsultationAsync(Guid medicalCaseId, Guid consultationId);

        /// <summary>
        /// 完成看诊，进入待缴费
        /// </summary>
        Task<bool> CompleteConsultationAsync(Guid medicalCaseId, Guid? treatmentPlanId);

        /// <summary>
        /// 完成缴费
        /// </summary>
        Task<bool> CompletePaymentAsync(Guid medicalCaseId, Guid cashierId);

        /// <summary>
        /// 开始药房服务
        /// </summary>
        Task<bool> StartPharmacyServiceAsync(Guid medicalCaseId, Guid pharmacyId);

        /// <summary>
        /// 完成药房服务
        /// </summary>
        Task<bool> CompletePharmacyServiceAsync(Guid medicalCaseId);

        /// <summary>
        /// 开始理疗服务
        /// </summary>
        Task<bool> StartTreatmentRoomServiceAsync(Guid medicalCaseId, Guid treatmentRoomServiceId);

        /// <summary>
        /// 完成理疗服务
        /// </summary>
        Task<bool> CompleteTreatmentRoomServiceAsync(Guid medicalCaseId);

        /// <summary>
        /// 完成整个医疗案例
        /// </summary>
        Task<bool> CompleteMedicalCaseAsync(Guid medicalCaseId);

        /// <summary>
        /// 取消医疗案例
        /// </summary>
        Task<bool> CancelMedicalCaseAsync(Guid medicalCaseId, string reason);

        /// <summary>
        /// 获取待处理的医疗案例（按状态）
        /// </summary>
        Task<List<MedicalCaseModel>> GetPendingCasesByStatusAsync(MedicalCaseStatus status);

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        Task<PagedResultDto<MedicalCaseModel>> GetPagedAsync(
            int page,
            int pageSize,
            MedicalCaseStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
    }
}
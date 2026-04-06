using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 医案Service接口 - 聚合根门面模式
    /// 继承 Query/Command/Lifecycle 三职责接口 + Coordinator 数据加载/聚合保存职责
    /// </summary>
    public interface IMedicalCaseService :
        IMedicalCaseQueryService,
        IMedicalCaseCommandService,
        IMedicalCaseLifecycleService
    {
        #region 数据加载与缓存 (原 Coordinator 职责)

        /// <summary>
        /// 加载医案详情并缓存
        /// </summary>
        Task<(bool success, MedicalCaseDetailDto? detail, string? errorMessage)> LoadDetailsAsync(Guid medicalCaseId, CancellationToken ct = default);

        /// <summary>
        /// 缓存的医案详情
        /// </summary>
        MedicalCaseDetailDto? CachedMedicalCase { get; }

        /// <summary>
        /// 缓存的诊疗记录
        /// </summary>
        ConsultationDetailDto? CachedConsultation { get; }

        /// <summary>
        /// 缓存的处方信息
        /// </summary>
        PrescriptionDetailDto? CachedPrescription { get; }

        /// <summary>
        /// 清除缓存数据
        /// </summary>
        void ClearCache();

        #endregion

        #region 聚合保存 (原 Coordinator 职责)

        /// <summary>
        /// 聚合保存（诊断+处方一次性保存）
        /// </summary>
        Task<(bool Success, MedicalCaseDetailDto? Data, string? Error)> AggregateSaveAsync(
            Guid medicalCaseId,
            ConsultationInputDto? consultation,
            PrescriptionInputDto? prescription,
            string? remark = null,
            string? editReason = null,
            CancellationToken ct = default);

        /// <summary>
        /// 保存后完成医案
        /// </summary>
        Task<(bool Success, string? Error)> SaveAndCompleteAsync(
            Guid medicalCaseId,
            ConsultationInputDto? consultation,
            PrescriptionInputDto? prescription,
            IValidatable? consultationValidator,
            IValidatable? prescriptionValidator,
            string? remark = null,
            bool isPrescriptionEnabled = true,
            CancellationToken ct = default);

        /// <summary>
        /// 保存后挂起医案
        /// </summary>
        Task<(bool Success, string? Error)> SaveAndSuspendAsync(
            Guid medicalCaseId,
            ConsultationInputDto? consultation,
            PrescriptionInputDto? prescription,
            string? remark = null,
            CancellationToken ct = default);

        /// <summary>
        /// 保存后取消医案
        /// </summary>
        Task<(bool Success, string? Error)> SaveAndCancelAsync(
            Guid medicalCaseId,
            ConsultationInputDto? consultation,
            PrescriptionInputDto? prescription,
            string? remark = null,
            CancellationToken ct = default);

        #endregion
    }
}

using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.MedicalCase.Models;
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
        #region 数据加载 (原 Coordinator 职责)

        /// <summary>
        /// 加载医案详情（D5: 委托 LifecycleService 初始化 EditContext 会话并持有 DTO 快照；
        /// 详情/诊疗/处方门面经 Current/CurrentConsultation/CurrentPrescription 读取）
        /// </summary>
        Task<CommandResult<MedicalCaseDetailModel>> LoadDetailsAsync(Guid medicalCaseId, CancellationToken ct = default);

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
        Task<CommandResult<bool>> SaveAndCompleteAsync(
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
        Task<CommandResult<bool>> SaveAndSuspendAsync(
            Guid medicalCaseId,
            ConsultationInputDto? consultation,
            PrescriptionInputDto? prescription,
            string? remark = null,
            CancellationToken ct = default);

        /// <summary>
        /// 保存后取消医案
        /// </summary>
        Task<CommandResult<bool>> SaveAndCancelAsync(
            Guid medicalCaseId,
            ConsultationInputDto? consultation,
            PrescriptionInputDto? prescription,
            string? remark = null,
            CancellationToken ct = default);

        #endregion
    }
}

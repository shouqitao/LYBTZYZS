using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 病案Service接口 - 聚合根模式
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: simplify-medicalcase-api - 聚合根统一管理Consultation和Prescription
    /// </summary>
    public interface IMedicalCaseService
    {
        #region IDataManager成员（原继承自IDataManager<MedicalCaseDetailDto>）

        /// <summary>
        /// 当前病案数据
        /// </summary>
        MedicalCaseDetailDto? Current { get; }

        /// <summary>
        /// 是否有未保存的变更
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// 初始化并加载病案数据
        /// </summary>
        Task InitializeAsync(Guid entityId);

        /// <summary>
        /// 保存变更
        /// </summary>
        Task<bool> SaveAsync();

        /// <summary>
        /// 删除当前病案
        /// </summary>
        Task<bool> DeleteAsync();

        /// <summary>
        /// 重新加载数据
        /// </summary>
        Task ReloadAsync();

        #endregion

        /// <summary>
        /// 医案ID
        /// </summary>
        Guid MedicalCaseId { get; }

        /// <summary>
        /// 当前诊疗数据（来自聚合根导航属性）
        /// </summary>
        ConsultationDetailDto? CurrentConsultation { get; }

        /// <summary>
        /// 当前处方数据（来自聚合根导航属性）
        /// </summary>
        PrescriptionDetailDto? CurrentPrescription { get; }

        /// <summary>
        /// 更新诊断数据（内存中更新，调用SaveAsync保存）
        /// OpenSpec: simplify-medicalcase-api - 聚合根统一管理
        /// </summary>
        void UpdateConsultation(ConsultationDetailDto consultation);

        /// <summary>
        /// 创建处方
        /// </summary>
        Task<PrescriptionDetailDto?> CreatePrescriptionAsync(PrescriptionInputDto createDto);

        /// <summary>
        /// 删除处方
        /// </summary>
        Task<bool> DeletePrescriptionAsync();

        /// <summary>
        /// 分页查询病案列表（返回轻量级ListDto）
        /// </summary>
        Task<PagedResult<MedicalCaseListDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null);

        /// <summary>
        /// 统一查询医案
        /// OpenSpec: optimize-medicalcase-api
        /// </summary>
        Task<PagedResult<MedicalCaseListDto>?> QueryAsync(MedicalCaseQueryDto query);

        #region 生命周期管理（合并自MedicalCaseLifecycleHandler）

        /// <summary>
        /// 创建新医案
        /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <returns>(是否成功, 医案ID, 错误信息)</returns>
        Task<(bool success, Guid medicalCaseId, string? errorMessage)> CreateMedicalCaseAsync(Guid patientId);

        /// <summary>
        /// 暂存医案
        /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>(是否成功, 错误信息)</returns>
        Task<(bool success, string? errorMessage)> SaveDraftAsync(Guid medicalCaseId);

        /// <summary>
        /// 取消医案
        /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="reason">取消原因</param>
        /// <returns>(是否成功, 错误信息)</returns>
        Task<(bool success, string? errorMessage)> CancelMedicalCaseAsync(Guid medicalCaseId, string? reason = null);

        /// <summary>
        /// 完成医案
        /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>(是否成功, 错误信息)</returns>
        Task<(bool success, string? errorMessage)> CompleteMedicalCaseAsync(Guid medicalCaseId);

        /// <summary>
        /// 恢复暂存医案为Active状态
        /// OpenSpec: simplify-medicalcase-module - 合并Handler到Service
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>(是否成功, 错误信息)</returns>
        Task<(bool success, string? errorMessage)> ResumeDraftAsync(Guid medicalCaseId);

        #endregion
    }
}

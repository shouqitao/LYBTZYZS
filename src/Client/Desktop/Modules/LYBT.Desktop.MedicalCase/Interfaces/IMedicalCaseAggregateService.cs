using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 病案数据管理器接口 - 聚合根模式
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// OpenSpec: simplify-medicalcase-api - 聚合根统一管理Consultation和Prescription
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IMedicalCaseAggregateService
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
    }
}

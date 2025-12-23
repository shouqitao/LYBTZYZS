using LYBT.Desktop.Contracts.Components;
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
    public interface IMedicalCaseDataManager : IDataManager<MedicalCaseDetailDto>
    {
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

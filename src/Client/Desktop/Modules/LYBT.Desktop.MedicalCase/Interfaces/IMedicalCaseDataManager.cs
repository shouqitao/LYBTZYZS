using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 病案数据管理器接口 - 聚合根模式
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IMedicalCaseDataManager : IDataManager<MedicalCaseDto>
    {
        /// <summary>
        /// 当前诊疗数据（来自聚合根导航属性）
        /// </summary>
        ConsultationDto? CurrentConsultation { get; }

        /// <summary>
        /// 当前处方数据（来自聚合根导航属性）
        /// </summary>
        PrescriptionDto? CurrentPrescription { get; }

        /// <summary>
        /// 创建处方
        /// </summary>
        Task<PrescriptionDto?> CreatePrescriptionAsync(PrescriptionCreateDto createDto);

        /// <summary>
        /// 更新诊疗信息
        /// </summary>
        Task<ApiResponse<ConsultationDto>> UpdateConsultationAsync(Guid medicalCaseId, ConsultationInputDto request);

        /// <summary>
        /// 删除处方
        /// </summary>
        Task<bool> DeletePrescriptionAsync();

        /// <summary>
        /// 分页查询病案列表
        /// </summary>
        Task<PagedResult<MedicalCaseDto>?> GetPagedAsync(int page, int pageSize, string? searchText = null);
    }
}

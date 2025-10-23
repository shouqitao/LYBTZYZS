using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例数据仓储接口 - Phase 2模块化架构
    /// Issue #1114 - Repository下沉到模块
    /// </summary>
    public interface IMedicalCaseRepository
    {
        Task<PagedResult<MedicalCaseDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
        Task<MedicalCaseDto?> GetByIdAsync(Guid id);
        Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto dto);
        Task<MedicalCaseDto> UpdateAsync(MedicalCaseUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据患者ID获取未完成的医案列表
        /// Issue #1568: 支持患者选择时自动检测并恢复未完成医案
        /// </summary>
        Task<List<MedicalCaseDto>> GetIncompleteCasesByPatientIdAsync(Guid patientId);

        Task<MedicalCaseDto> CreateWithDetailsAsync(MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null);
        Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="dto">诊断更新信息</param>
        /// <returns>更新后的诊断信息</returns>
        Task<ConsultationDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto);
    }
}

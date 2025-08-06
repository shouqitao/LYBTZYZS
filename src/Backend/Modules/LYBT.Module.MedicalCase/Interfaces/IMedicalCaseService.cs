using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例服务接口
    /// </summary>
    public interface IMedicalCaseService
    {
        /// <summary>
        /// 获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetListAsync();

        /// <summary>
        /// 分页获取医疗案例列表
        /// </summary>
        Task<PaginatedResult<MedicalCaseDto>> GetPagedAsync(PaginationRequest request);

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseCreateDto dto);

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        Task<bool> UpdateAsync(Guid id, MedicalCaseUpdateDto dto);

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        Task<bool> UpdateStatusAsync(Guid id, MedicalCaseStatus status);

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据医生ID获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetByDoctorIdAsync(Guid doctorId);

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        Task<List<MedicalCaseDto>> GetTodayCasesAsync();

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        Task<bool> CompleteCaseAsync(Guid id);
    }
}
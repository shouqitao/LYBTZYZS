using LYBT.Entities.Patients;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者服务优化接口 - 消除双重映射，直接返回Entity
    /// Phase 3 Task 3.1: Service层优化 - Entity直接返回策略
    /// </summary>
    public interface IPatientServiceOptimized
    {
        /// <summary>
        /// 获取分页患者数据（直接返回Patient Entity）
        /// </summary>
        Task<Result<PagedResult<Patient>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> GetByIdEntityAsync(Guid id);

        /// <summary>
        /// 创建患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> CreateEntityAsync(PatientInputDto dto);

        /// <summary>
        /// 更新患者（直接返回Patient Entity）
        /// </summary>
        Task<Result<Patient>> UpdateEntityAsync(Guid id, PatientInputDto dto);

        /// <summary>
        /// 删除患者
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// 批量导入患者数据（保持原有DTO结果，因为需要详细的导入报告）
        /// </summary>
        Task<Result<BatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出患者导入模板
        /// </summary>
        Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config);

        /// <summary>
        /// 导出患者数据到Excel
        /// </summary>
        Task<MemoryStream> ExportPatientsAsync(string? keyword = null);
    }
}
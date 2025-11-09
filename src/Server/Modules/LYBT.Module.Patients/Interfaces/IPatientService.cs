using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者服务接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// 分页查询患者
        /// </summary>
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新患者
        /// </summary>
        Task<ServiceResult<PatientDto>> CreateAsync(PatientInputDto dto);

        /// <summary>
        /// 更新患者信息
        /// </summary>
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientInputDto dto);

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索患者
        /// </summary>
        Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// 支持部分成功模式、失败恢复机制（BR-002）
        /// </summary>
        /// <param name="stream">Excel文件流</param>
        /// <param name="fileName">文件名（可选，用于日志记录）</param>
        /// <returns>批量导入结果，包含成功/失败/跳过数量和详细失败信息</returns>
        Task<ServiceResult<BatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        /// <param name="config">模板配置（示例数据行数等）</param>
        /// <returns>Excel模板文件流</returns>
        Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config);

        /// <summary>
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        /// <param name="keyword">搜索关键词（可选）</param>
        /// <returns>Excel文件流</returns>
        Task<MemoryStream> ExportPatientsAsync(string? keyword = null);
    }
}

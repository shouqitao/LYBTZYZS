using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 患者导入导出服务接口 (Epic #1934)
    /// 负责Excel批量导入、模板导出、数据导出
    /// </summary>
    public interface IPatientImportExportService
    {
        /// <summary>
        /// 批量导入患者数据 (Epic #1934 FR-001)
        /// 支持部分成功模式、失败恢复机制（BR-002）
        /// </summary>
        /// <param name="stream">Excel文件流</param>
        /// <param name="fileName">文件名（可选，用于日志记录）</param>
        /// <returns>批量导入结果，包含成功/失败/跳过数量和详细失败信息</returns>
        Task<Result<PatientBatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null);

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

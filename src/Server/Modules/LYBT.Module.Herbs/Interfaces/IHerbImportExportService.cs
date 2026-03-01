using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Herbs.Interfaces
{
    /// <summary>
    /// 药材导入导出服务接口
    /// 从 IHerbService 拆分出的导入/导出职责
    /// </summary>
    public interface IHerbImportExportService
    {
        /// <summary>
        /// 从Excel文件导入药材数据 (Issue #1166)
        /// </summary>
        Task<Result<ImportResultDto<HerbDetailDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        Task<MemoryStream> ExportAsync(string? category = null);

        /// <summary>
        /// 生成药材导入模板 (Issue #1166)
        /// </summary>
        MemoryStream GenerateImportTemplate();

        /// <summary>
        /// 批量导入药材（Epic #1962 Task 2.2）
        /// Desktop层负责Excel解析，Server层接收DTO列表
        /// </summary>
        /// <param name="herbs">药材DTO列表（≤10000条，BR-006）</param>
        /// <param name="strategy">重复处理策略（Skip/Update/Error）</param>
        Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy);

        /// <summary>
        /// 获取所有药材数据用于导出（Epic #1962 Task 3.1）
        /// Desktop层负责Excel生成，Server层返回JSON数据
        /// </summary>
        /// <param name="category">分类筛选（可选）</param>
        Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null);
    }
}

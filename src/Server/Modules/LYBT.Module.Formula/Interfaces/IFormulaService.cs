using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces
{
    /// <summary>
    /// 验方服务接口 - 简化版，包含基础CRUD和分类筛选
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 分页查询验方（Issue #1164: 扩展支持分类筛选）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选（可选）</param>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null);

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateAsync(FormulaInputDto dto);

        /// <summary>
        /// 更新验方信息
        /// </summary>
        Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaInputDto dto);

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        Task<ServiceResult> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除验方（软删除）(Issue #1169)
        /// </summary>
        /// <param name="ids">验方ID列表</param>
        Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 搜索验方 - 支持多条件搜索
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 从Excel文件导入验方数据 (Issue #1166, #1347)
        /// 返回FormulaImportResultDto包含药材匹配统计
        /// </summary>
        Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(Stream stream, string? fileName = null);

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        Task<MemoryStream> ExportAsync(string? category = null);

        /// <summary>
        /// 生成验方导入模板 (Issue #1166)
        /// </summary>
        MemoryStream GenerateImportTemplate();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <param name="herbItemId">待验证的药材项ID</param>
        /// <param name="selectedHerbId">选中的系统药材ID</param>
        Task<ServiceResult> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId);

        /// <summary>
        /// 获取待验证的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync();
    }
}

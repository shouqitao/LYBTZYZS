using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
namespace LYBT.Shared.Interfaces.Api
{

    /// <summary>
    /// 验方API客户端接口 - UltraThink统一标准
    /// </summary>
    public interface IFormulaApi
    {

        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        [Refit.Post("/api/v1/formulas/paged")]
        Task<Refit.ApiResponse<PagedResult<FormulaDto>>> GetPagedFormulasAsync([Refit.Body] PagedQueryBaseDto query);

        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        [Refit.Get("/api/v1/formulas")]
        Task<Refit.ApiResponse<PagedResult<FormulaDto>>> GetFormulasAsync(
            [Refit.Query] string? keyword = null,
            [Refit.Query] string? category = null);

        /// <summary>
        /// 获取分页验方列表（兼容性别名）
        /// </summary>
        [Refit.Get("/api/v1/formulas")]
        Task<Refit.ApiResponse<PagedResult<FormulaDto>>> GetPagedAsync(
            [Refit.Query] int page = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? keyword = null);

        /// <summary>
        /// 根据ID获取验方模板详情
        /// </summary>
        [Refit.Get("/api/v1/formulas/{id}")]
        Task<Refit.ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        [Refit.Post("/api/v1/formulas")]
        Task<Refit.ApiResponse<FormulaDto>> CreateFormulaAsync([Refit.Body] FormulaCreateDto createDto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        [Refit.Put("/api/v1/formulas/{id}")]
        Task<Refit.ApiResponse<FormulaDto>> UpdateFormulaAsync(Guid id, [Refit.Body] FormulaUpdateDto updateDto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        [Refit.Delete("/api/v1/formulas/{id}")]
        Task<Refit.ApiResponse<bool>> DeleteFormulaAsync(Guid id);

        /// <summary>
        /// 批量删除验方模板
        /// </summary>
        [Refit.Post("/api/v1/formulas/batch-delete")]
        Task<Refit.ApiResponse<int>> BatchDeleteFormulasAsync([Refit.Body] List<Guid> ids);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        [Refit.Post("/api/v1/formulas/{id}/copy")]
        Task<Refit.ApiResponse<FormulaDto>> CopyFormulaAsync(Guid id, [Refit.Query] string newName);

        /// <summary>
        /// 启用/禁用验方模板
        /// </summary>
        [Refit.Patch("/api/v1/formulas/{id}/toggle-status")]
        Task<Refit.ApiResponse<bool>> ToggleFormulaStatusAsync(Guid id);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        [Refit.Get("/api/v1/formulas/categories")]
        Task<Refit.ApiResponse<List<string>>> GetCategoriesAsync();

        // UltraThink v2.0: 导入导出功能（应用户业务需求恢复）

        /// <summary>
        /// 批量导入验方数据
        /// </summary>
        [Refit.Post("/api/v1/formulas/import")]
        Task<Refit.ApiResponse<FormulaImportResultDto>> ImportFormulasAsync(
            [Refit.Body] List<FormulaImportDto> formulas,
            [Refit.Query] FormulaImportOptionsDto options);

        /// <summary>
        /// 验证导入数据
        /// </summary>
        [Refit.Post("/api/v1/formulas/import/validate")]
        Task<Refit.ApiResponse<FormulaImportResultDto>> ValidateImportDataAsync(
            [Refit.Body] List<FormulaImportDto> formulas,
            [Refit.Query] FormulaImportOptionsDto options);

        /// <summary>
        /// 导出验方数据
        /// </summary>
        [Refit.Post("/api/v1/formulas/export")]
        Task<Refit.ApiResponse<List<FormulaExportDto>>> ExportFormulasAsync(
            [Refit.Body] List<Guid> formulaIds);

        /// <summary>
        /// 导出所有验方数据
        /// </summary>
        [Refit.Get("/api/v1/formulas/export/all")]
        Task<Refit.ApiResponse<List<FormulaExportDto>>> ExportAllFormulasAsync(
            [Refit.Query] bool includePrivate = false,
            [Refit.Query] string? category = null);

        /// <summary>
        /// 从Excel文件导入验方
        /// </summary>
        [Refit.Multipart]
        [Refit.Post("/api/v1/formulas/import/excel")]
        Task<Refit.ApiResponse<FormulaImportResultDto>> ImportFromExcelAsync(
            [Refit.AliasAs("file")] Refit.StreamPart file,
            [Refit.AliasAs("options")] FormulaImportOptionsDto options);

        /// <summary>
        /// 导出为Excel文件
        /// </summary>
        [Refit.Post("/api/v1/formulas/export/excel")]
        Task<Refit.ApiResponse<byte[]>> ExportToExcelAsync(
            [Refit.Body] List<Guid> formulaIds);

        /// <summary>
        /// 获取导入历史记录
        /// </summary>
        [Refit.Get("/api/v1/formulas/import/history")]
        Task<Refit.ApiResponse<PagedResult<FormulaImportResultDto>>> GetImportHistoryAsync(
            [Refit.Query] int pageIndex = 1,
            [Refit.Query] int pageSize = 20,
            [Refit.Query] string? importBatch = null);

        /// <summary>
        /// 获取导入模板
        /// </summary>
        [Refit.Get("/api/v1/formulas/import/template")]
        Task<Refit.ApiResponse<byte[]>> GetImportTemplateAsync();
    }
}

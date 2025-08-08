using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 验方API服务接口 - Refit定义
    /// </summary>
    public interface IFormulaApiService
    {
        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        [Post("/api/v1/Formulas/paged")]
        Task<Refit.ApiResponse<PaginatedResult<FormulaDto>>> GetPagedFormulasAsync([Body] PaginationRequest query);

        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        [Get("/api/v1/Formulas")]
        Task<Refit.ApiResponse<PaginatedResult<FormulaDto>>> GetFormulasAsync(
            [Query] string? keyword = null,
            [Query] string? category = null);

        /// <summary>
        /// 根据ID获取验方模板详情
        /// </summary>
        [Get("/api/v1/Formulas/{id}")]
        Task<Refit.ApiResponse<FormulaDetailDto>> GetFormulaByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        [Post("/api/v1/Formulas")]
        Task<Refit.ApiResponse<FormulaDto>> CreateFormulaAsync([Body] FormulaCreateDto createDto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        [Put("/api/v1/Formulas/{id}")]
        Task<Refit.ApiResponse<FormulaDto>> UpdateFormulaAsync(Guid id, [Body] FormulaUpdateDto updateDto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        [Delete("/api/v1/Formulas/{id}")]
        Task<Refit.ApiResponse<bool>> DeleteFormulaAsync(Guid id);

        /// <summary>
        /// 批量删除验方模板
        /// </summary>
        [Post("/api/v1/Formulas/batch-delete")]
        Task<Refit.ApiResponse<int>> BatchDeleteFormulasAsync([Body] List<Guid> ids);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        [Post("/api/v1/Formulas/{id}/copy")]
        Task<Refit.ApiResponse<FormulaDto>> CopyFormulaAsync(Guid id, [Query] string newName);

        /// <summary>
        /// 启用/禁用验方模板
        /// </summary>
        [Patch("/api/v1/Formulas/{id}/toggle-status")]
        Task<Refit.ApiResponse<bool>> ToggleFormulaStatusAsync(Guid id);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        [Get("/api/v1/Formulas/categories")]
        Task<Refit.ApiResponse<List<string>>> GetCategoriesAsync();
    }
}
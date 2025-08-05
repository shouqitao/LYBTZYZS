using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 验方模板API服务接口 - Refit定义
    /// </summary>
    public interface IFormulaTemplateApiService
    {
        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplates/paged")]
        Task<Refit.ApiResponse<PaginatedResult<FormulaTemplateDto>>> GetPagedFormulaTemplatesAsync([Body] PaginationRequest query);

        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        [Get("/api/v1/FormulaTemplates")]
        Task<Refit.ApiResponse<PaginatedResult<FormulaTemplateDto>>> GetFormulaTemplatesAsync(
            [Query] string? keyword = null, 
            [Query] string? category = null);

        /// <summary>
        /// 根据ID获取验方模板详情
        /// </summary>
        [Get("/api/v1/FormulaTemplates/{id}")]
        Task<Refit.ApiResponse<FormulaTemplateDetailDto>> GetFormulaTemplateByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplates")]
        Task<Refit.ApiResponse<FormulaTemplateDto>> CreateFormulaTemplateAsync([Body] FormulaTemplateCreateDto createDto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        [Put("/api/v1/FormulaTemplates/{id}")]
        Task<Refit.ApiResponse<FormulaTemplateDto>> UpdateFormulaTemplateAsync(Guid id, [Body] FormulaTemplateUpdateDto updateDto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        [Delete("/api/v1/FormulaTemplates/{id}")]
        Task<Refit.ApiResponse<bool>> DeleteFormulaTemplateAsync(Guid id);

        /// <summary>
        /// 批量删除验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplates/batch-delete")]
        Task<Refit.ApiResponse<int>> BatchDeleteFormulaTemplatesAsync([Body] List<Guid> ids);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplates/{id}/copy")]
        Task<Refit.ApiResponse<FormulaTemplateDto>> CopyFormulaTemplateAsync(Guid id, [Query] string newName);

        /// <summary>
        /// 启用/禁用验方模板
        /// </summary>
        [Patch("/api/v1/FormulaTemplates/{id}/toggle-status")]
        Task<Refit.ApiResponse<bool>> ToggleFormulaTemplateStatusAsync(Guid id);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        [Get("/api/v1/FormulaTemplates/categories")]
        Task<Refit.ApiResponse<List<string>>> GetCategoriesAsync();
    }
}
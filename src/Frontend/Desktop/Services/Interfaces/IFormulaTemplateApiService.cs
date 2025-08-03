using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using ApiResponse = LYBT.Shared.Models.Common.ApiResponse;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 验方模板API服务接口 - Refit定义
    /// </summary>
    public interface IFormulaTemplateApiService
    {
        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        [Get("/api/v1/FormulaTemplate")]
        Task<LYBT.Shared.Models.Common.ApiResponse<List<FormulaTemplateDto>>> GetFormulaTemplatesAsync(
            [Query] string? keyword = null, 
            [Query] string? category = null);

        /// <summary>
        /// 根据ID获取验方模板详情
        /// </summary>
        [Get("/api/v1/FormulaTemplate/{id}")]
        Task<LYBT.Shared.Models.Common.ApiResponse<FormulaTemplateDto>> GetFormulaTemplateByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplate")]
        Task<LYBT.Shared.Models.Common.ApiResponse<FormulaTemplateDto>> CreateFormulaTemplateAsync([Body] FormulaTemplateCreateDto createDto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        [Put("/api/v1/FormulaTemplate/{id}")]
        Task<LYBT.Shared.Models.Common.ApiResponse<FormulaTemplateDto>> UpdateFormulaTemplateAsync(Guid id, [Body] FormulaTemplateEditDto updateDto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        [Delete("/api/v1/FormulaTemplate/{id}")]
        Task<LYBT.Shared.Models.Common.ApiResponse<bool>> DeleteFormulaTemplateAsync(Guid id);

        /// <summary>
        /// 批量删除验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplate/batch-delete")]
        Task<LYBT.Shared.Models.Common.ApiResponse<int>> BatchDeleteFormulaTemplatesAsync([Body] List<Guid> ids);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        [Post("/api/v1/FormulaTemplate/{id}/copy")]
        Task<LYBT.Shared.Models.Common.ApiResponse<FormulaTemplateDto>> CopyFormulaTemplateAsync(Guid id, [Query] string newName);

        /// <summary>
        /// 启用/禁用验方模板
        /// </summary>
        [Patch("/api/v1/FormulaTemplate/{id}/toggle-status")]
        Task<LYBT.Shared.Models.Common.ApiResponse<bool>> ToggleFormulaTemplateStatusAsync(Guid id);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        [Get("/api/v1/FormulaTemplate/categories")]
        Task<LYBT.Shared.Models.Common.ApiResponse<List<string>>> GetCategoriesAsync();
    }
}
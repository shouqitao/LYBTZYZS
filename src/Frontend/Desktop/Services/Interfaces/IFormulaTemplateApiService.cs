using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.FormulaTemplates;
using ApiResponse = LYBT.Shared.Models.Common.ApiResponse;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 验方模板API服务接口
    /// </summary>
    public interface IFormulaTemplateApiService
    {
        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        [Get("/api/v1/formulatemplate")]
        Task<ApiResponse<List<FormulaTemplateDto>>> GetTemplatesAsync([Query] string? search = null);

        /// <summary>
        /// 获取验方模板详情
        /// </summary>
        [Get("/api/v1/formulatemplate/{id}")]
        Task<ApiResponse<FormulaTemplateDto>> GetTemplateByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        [Post("/api/v1/formulatemplate")]
        Task<ApiResponse<FormulaTemplateDto>> CreateTemplateAsync([Body] CreateFormulaTemplateDto dto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        [Put("/api/v1/formulatemplate/{id}")]
        Task<ApiResponse<FormulaTemplateDto>> UpdateTemplateAsync(Guid id, [Body] UpdateFormulaTemplateDto dto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        [Delete("/api/v1/formulatemplate/{id}")]
        Task<ApiResponse<bool>> DeleteTemplateAsync(Guid id);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        [Post("/api/v1/formulatemplate/{id}/copy")]
        Task<ApiResponse<FormulaTemplateDto>> CopyTemplateAsync(Guid id, [Query] string newName);

        /// <summary>
        /// 按分类获取模板
        /// </summary>
        [Get("/api/v1/formulatemplate/by-category/{category}")]
        Task<ApiResponse<List<FormulaTemplateDto>>> GetTemplatesByCategoryAsync(string category);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        [Get("/api/v1/formulatemplate/categories")]
        Task<ApiResponse<List<string>>> GetCategoriesAsync();
    }
}
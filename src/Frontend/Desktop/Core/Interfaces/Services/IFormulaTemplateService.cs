using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Contracts.FormulaTemplates;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 验方模板服务接口
    /// </summary>
    public interface IFormulaTemplateService
    {
        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        Task<PagedResult<FormulaTemplateInfo>> SearchFormulasAsync(PaginationRequest query);

        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        Task<ApiResponse<List<FormulaTemplateInfo>>> GetListAsync(string? keyword = null, string? category = null);

        /// <summary>
        /// 根据ID获取验方模板详情
        /// </summary>
        Task<ApiResponse<FormulaTemplateDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        Task<ApiResponse<FormulaTemplateInfo>> CreateAsync(FormulaTemplateCreateDto createDto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        Task<ApiResponse<FormulaTemplateInfo>> UpdateAsync(FormulaTemplateUpdateDto updateDto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        Task<ApiResponse<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除验方模板
        /// </summary>
        Task<ApiResponse<int>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        Task<ApiResponse<FormulaTemplateInfo>> CopyAsync(Guid id, string newName);

        /// <summary>
        /// 启用/禁用验方模板
        /// </summary>
        Task<ApiResponse<bool>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        Task<ApiResponse<List<string>>> GetCategoriesAsync();
    }
}
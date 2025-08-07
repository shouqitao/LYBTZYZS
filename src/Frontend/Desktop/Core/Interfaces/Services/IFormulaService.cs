using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Formula;
using FormulaPagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.Formulas.FormulaInfo>;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 验方模板服务接口
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        Task<FormulaPagedResult> SearchFormulasAsync(PaginationRequest query);

        /// <summary>
        /// 获取验方模板列表
        /// </summary>
        Task<ServiceResult<List<FormulaInfo>>> GetListAsync(string? keyword = null, string? category = null);

        /// <summary>
        /// 根据ID获取验方模板详情
        /// </summary>
        Task<ServiceResult<FormulaDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> CreateAsync(FormulaCreateDto createDto);

        /// <summary>
        /// 更新验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> UpdateAsync(FormulaUpdateDto updateDto);

        /// <summary>
        /// 删除验方模板
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 批量删除验方模板
        /// </summary>
        Task<ServiceResult<int>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 复制验方模板
        /// </summary>
        Task<ServiceResult<FormulaInfo>> CopyAsync(Guid id, string newName);

        /// <summary>
        /// 启用/禁用验方模板
        /// </summary>
        Task<ServiceResult<bool>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 获取所有分类
        /// </summary>
        Task<ServiceResult<List<string>>> GetCategoriesAsync();
    }
}
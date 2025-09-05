using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces
{
    /// <summary>
    /// 验方查询服务接口
    /// UltraThink架构 - Query层接口抽象
    /// 职责：验方查询、搜索、筛选等只读操作
    /// </summary>
    public interface IFormulaQueryService
    {
        /// <summary>
        /// 分页查询验方
        /// </summary>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaPagedQueryDto query);

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 搜索验方（根据名称、症状、功效）
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 获取所有可用验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetAllAvailableAsync();

        /// <summary>
        /// 根据症状查找相关验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetBySymptomAsync(string symptom);

        /// <summary>
        /// 根据功效查找验方
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetByEffectAsync(string effect);

        /// <summary>
        /// 获取经典验方（标记为经典的验方）
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetClassicFormulasAsync();

        /// <summary>
        /// 获取常用验方（按使用频率排序）
        /// </summary>
        Task<ServiceResult<List<FormulaDto>>> GetPopularFormulasAsync(int count = 20);
    }
}

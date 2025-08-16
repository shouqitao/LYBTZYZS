using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Shared
{
    /// <summary>
    /// 共享验方服务接口
    /// 提供跨工作台的验方模板管理功能
    /// </summary>
    public interface ISharedFormulaService
    {
        /// <summary>
        /// 获取所有验方模板
        /// </summary>
        /// <returns>验方模板列表</returns>
        Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync();

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>验方详细信息</returns>
        Task<ServiceResult<FormulaDto>> GetFormulaByIdAsync(Guid formulaId);

        /// <summary>
        /// 搜索验方
        /// 支持名称、功效、适应症搜索
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的验方列表</returns>
        Task<ServiceResult<List<FormulaDto>>> SearchFormulasAsync(string keyword);

        /// <summary>
        /// 根据症候获取推荐验方
        /// </summary>
        /// <param name="symptoms">症候描述</param>
        /// <returns>推荐的验方列表</returns>
        Task<ServiceResult<List<FormulaDto>>> GetRecommendedFormulasBySymptomAsync(string symptoms);

        /// <summary>
        /// 获取经典验方列表
        /// </summary>
        /// <returns>经典验方列表</returns>
        Task<ServiceResult<List<FormulaDto>>> GetClassicFormulasAsync();

        /// <summary>
        /// 获取个人验方列表
        /// 医生个人收藏的验方
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>个人验方列表</returns>
        Task<ServiceResult<List<FormulaDto>>> GetPersonalFormulasAsync(Guid doctorId);

        /// <summary>
        /// 创建新验方
        /// </summary>
        /// <param name="dto">验方信息</param>
        /// <returns>创建的验方信息</returns>
        Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto dto);

        /// <summary>
        /// 更新验方
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <param name="dto">更新的验方信息</param>
        /// <returns>更新结果</returns>
        Task<ServiceResult> UpdateFormulaAsync(Guid id, FormulaUpdateDto dto);

        /// <summary>
        /// 删除验方
        /// </summary>
        /// <param name="id">验方ID</param>
        /// <returns>删除结果</returns>
        Task<ServiceResult> DeleteFormulaAsync(Guid id);

        /// <summary>
        /// 收藏验方
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <param name="doctorId">医生ID</param>
        /// <returns>收藏结果</returns>
        Task<ServiceResult> FavoriteFormulaAsync(Guid formulaId, Guid doctorId);

        /// <summary>
        /// 获取验方使用统计
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>使用统计信息</returns>
        Task<ServiceResult<object>> GetFormulaUsageStatisticsAsync(Guid formulaId);

        /// <summary>
        /// 验证验方组成合理性
        /// </summary>
        /// <param name="formulaDto">验方信息</param>
        /// <returns>验证结果</returns>
        Task<ServiceResult<bool>> ValidateFormulaCompositionAsync(FormulaDto formulaDto);

        /// <summary>
        /// 获取常用验方列表
        /// 基于使用频率
        /// </summary>
        /// <param name="limit">返回数量，默认20</param>
        /// <returns>常用验方列表</returns>
        Task<ServiceResult<List<FormulaDto>>> GetFrequentlyUsedFormulasAsync(int limit = 20);

        /// <summary>
        /// 分页查询验方列表
        /// </summary>
        /// <param name="queryDto">查询参数</param>
        /// <returns>分页验方列表</returns>
        Task<ServiceResult<PagedResult<FormulaDto>>> GetFormulasAsync(FormulaQueryDto queryDto);

        /// <summary>
        /// 获取验方统计信息
        /// </summary>
        /// <returns>验方统计信息</returns>
        Task<ServiceResult<FormulaStatisticsDto>> GetFormulaStatisticsAsync();

        /// <summary>
        /// 获取验方创建者列表
        /// </summary>
        /// <returns>创建者列表</returns>
        Task<ServiceResult<List<string>>> GetFormulaCreatorsAsync();

        /// <summary>
        /// 获取验方功效列表
        /// </summary>
        /// <returns>功效列表</returns>
        Task<ServiceResult<List<string>>> GetFormulaEffectsAsync();

        /// <summary>
        /// 复制验方
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>复制的验方</returns>
        Task<ServiceResult<FormulaDto>> CopyFormulaAsync(Guid formulaId);

        /// <summary>
        /// 切换验方共享状态
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>操作结果</returns>
        Task<ServiceResult<bool>> ToggleFormulaShareStatusAsync(Guid formulaId);

        /// <summary>
        /// 获取验方详细信息（与GetFormulaByIdAsync别名）
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>验方详细信息</returns>
        Task<ServiceResult<FormulaDto>> GetFormulaAsync(Guid formulaId);
    }
}
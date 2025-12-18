using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 配方数据管理器接口
    /// Desktop层架构重构 Phase 2: DataManager接口化重构
    /// 目的：消除具体类依赖，提升可测试性
    /// </summary>
    public interface IFormulaDataManager
    {
        /// &lt;summary&gt;
        /// 当前配方数据
        /// Desktop层架构重构 Phase 3: 为Validator接口化提供数据支持
        /// &lt;/summary&gt;
        FormulaDetailDto? CurrentFormula { get; }
        /// <summary>
        /// 加载配方详情
        /// </summary>
        Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> LoadFormulaAsync(Guid formulaId);

        /// <summary>
        /// 刷新配方数据
        /// </summary>
        Task<(bool success, FormulaDetailDto? formula, string? errorMessage)> RefreshFormulaAsync(Guid formulaId);
    }
}

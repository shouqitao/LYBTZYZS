using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.ViewModels.Handlers;

/// <summary>
/// 验方状态处理接口
/// </summary>
public interface IFormulaStatusHandler
{
    /// <summary>
    /// 切换验方启用/禁用状态
    /// </summary>
    /// <param name="formula">验方信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> ToggleStatusAsync(FormulaListDto formula);

    /// <summary>
    /// 恢复已删除的验方
    /// </summary>
    /// <param name="formula">验方信息</param>
    /// <returns>操作是否成功</returns>
    Task<bool> RestoreAsync(FormulaListDto formula);
}

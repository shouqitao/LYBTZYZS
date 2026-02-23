using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Contracts.Services.CrossModule;

/// <summary>
/// 验方搜索提供者 (D5-3)
/// 供 MedicalCase 模块导入验方，解耦对 LYBT.Desktop.Formula 的编译期依赖
/// </summary>
public interface IFormulaSearchProvider
{
    /// <summary>分页获取验方列表</summary>
    Task<PagedResult<FormulaListDto>> GetFormulasPagedAsync(int page, int pageSize);

    /// <summary>获取验方详情 (含药材列表)</summary>
    Task<FormulaDetailDto?> GetFormulaByIdAsync(Guid id);
}

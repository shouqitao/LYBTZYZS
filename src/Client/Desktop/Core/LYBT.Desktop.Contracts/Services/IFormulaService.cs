using LYBT.Desktop.Contracts.Results;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using System.Threading;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 配方Service接口
    /// 提供配方CRUD和业务操作的统一处理
    /// 使用 CommandResult&lt;T&gt; 统一返回类型，遵循 ICrudService 金标准模式
    /// D1: 对齐 IHerbService/HerbService 模式——继承 ICrudService 泛型契约，
    /// 特有业务操作（复制/批量/导入导出）保留在本接口。
    /// </summary>
    public interface IFormulaService : ICrudService<FormulaListDto, FormulaDetailDto, FormulaInputDto>
    {
        #region 复制操作

        /// <summary>
        /// 复制配方
        /// </summary>
        Task<CommandResult<FormulaDetailDto>> CopyFormulaAsync(FormulaDetailDto sourceFormula, CancellationToken ct = default);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除验方
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> formulaIds, CancellationToken ct = default);

        #endregion

        #region 批量导入/导出

        /// <summary>
        /// 获取待校验验方分页列表（US-FORM-007——Desktop 验方校验待办）
        /// </summary>
        Task<CommandResult<PagedResult<FormulaDetailDto>>> GetPendingValidationAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);

        /// <summary>
        /// 校验验方药材（绑定到系统药材库，US-FORM-008）
        /// </summary>
        Task<CommandResult<bool>> ValidateHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId, CancellationToken ct = default);

        /// <summary>
        /// 批量导入验方数据
        /// </summary>
        Task<CommandResult<FormulaBatchImportResultDto>> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default);

        /// <summary>
        /// 导出验方数据到Excel
        /// </summary>
        Task<CommandResult<byte[]>> ExportFormulasAsync(string? category = null, CancellationToken ct = default);

        /// <summary>
        /// 下载验方导入模板
        /// </summary>
        Task<CommandResult<byte[]>> ExportTemplateAsync(CancellationToken ct = default);

        #endregion
    }
}

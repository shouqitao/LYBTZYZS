using System.Threading;
using LYBT.Desktop.Contracts.CommandHandlers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces
{
    /// <summary>
    /// 配方Service接口
    /// OpenSpec: standardize-service-layer - 统一使用Service命名
    /// OpenSpec: cleanup-formula-dead-code - 清理未使用的占位方法和FormulaValidation方法
    /// 提供配方CRUD和业务操作的统一处理
    /// 使用 CommandResult&lt;T&gt; 统一返回类型，遵循 IUserService/RemoteUserService 金标准模式
    /// </summary>
    public interface IFormulaService
    {
        #region 查询操作

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<CommandResult<FormulaDetailDto>> GetByIdAsync(Guid formulaId, CancellationToken ct = default);

        /// <summary>
        /// 分页查询验方列表
        /// </summary>
        Task<CommandResult<PagedResult<FormulaListDto>>> GetPagedAsync(
            int page, int pageSize, string? keyword = null, CancellationToken ct = default);

        #endregion

        #region 保存操作

        /// <summary>
        /// 保存配方（创建或更新）
        /// Issue #2149: 优化双重映射，直接接收InputDto以提升性能
        /// </summary>
        Task<CommandResult<FormulaDetailDto>> SaveFormulaAsync(
            FormulaDetailDto currentFormula,
            string formulaName,
            string effect,
            string usage,
            string property,
            string category,
            string remark,
            bool isShared,
            List<FormulaHerbItemInputDto> herbInputDtos,
            CancellationToken ct = default);

        /// <summary>
        /// 复制配方
        /// </summary>
        Task<CommandResult<FormulaDetailDto>> CopyFormulaAsync(FormulaDetailDto sourceFormula, CancellationToken ct = default);

        #endregion

        #region 删除操作

        /// <summary>
        /// 删除配方（软删除）
        /// </summary>
        Task<CommandResult<bool>> DeleteFormulaAsync(Guid formulaId, CancellationToken ct = default);

        #endregion

        #region 状态管理

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        Task<CommandResult<FormulaDetailDto>> ToggleStatusAsync(Guid formulaId, CancellationToken ct = default);

        /// <summary>
        /// 恢复已删除的验方
        /// </summary>
        Task<CommandResult<FormulaDetailDto>> RestoreAsync(Guid formulaId, CancellationToken ct = default);

        #endregion

        #region 批量操作

        /// <summary>
        /// 批量删除验方
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> formulaIds, CancellationToken ct = default);

        /// <summary>
        /// 批量启用验方
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchEnableAsync(List<Guid> formulaIds, CancellationToken ct = default);

        /// <summary>
        /// 批量禁用验方
        /// </summary>
        Task<CommandResult<BatchOperationResultDto>> BatchDisableAsync(List<Guid> formulaIds, CancellationToken ct = default);

        #endregion

        #region 批量导入/导出

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

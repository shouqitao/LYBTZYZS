using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces {

    /// <summary>
    /// 验方业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// 职责：验方业务逻辑、CRUD操作、状态管理
    /// </summary>
    public interface IFormulaBusinessService {

        /// <summary>
        /// 创建验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto createDto);

        /// <summary>
        /// 更新验方信息
        /// </summary>
        Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto updateDto);

        /// <summary>
        /// 删除验方
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 批量导入验方
        /// </summary>
        Task<ServiceResult<int>> ImportFormulasAsync(List<FormulaImportDto> formulas);

        /// <summary>
        /// 设置验方状态（启用/禁用）
        /// </summary>
        Task<ServiceResult<bool>> SetStatusAsync(Guid id, bool isActive);

        /// <summary>
        /// 标记为经典验方
        /// </summary>
        Task<ServiceResult<bool>> MarkAsClassicAsync(Guid id);

        /// <summary>
        /// 复制验方创建新的验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CopyFormulaAsync(Guid id, string newName);

        /// <summary>
        /// 验证验方数据完整性
        /// </summary>
        Task<ServiceResult<List<string>>> ValidateFormulaAsync(FormulaCreateDto createDto);
    }
}

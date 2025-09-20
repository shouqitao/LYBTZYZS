using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Module.Formula.Interfaces
{

    /// <summary>
    /// 验方业务服务接口 - UltraThink双层架构Business层抽象
    /// 职责：验方业务逻辑处理、复制、分享、分析等业务功能
    /// </summary>
    public interface IFormulaBusinessService
    {

        /// <summary>
        /// 复制验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName);

        /// <summary>
        /// 从处方创建验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name);

        /// <summary>
        /// 分享验方
        /// </summary>
        Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 取消分享验方
        /// </summary>
        Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 分析验方
        /// </summary>
        Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId);

        /// <summary>
        /// 创建验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);

        /// <summary>
        /// 更新验方
        /// </summary>
        Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);

        /// <summary>
        /// 删除验方
        /// </summary>
        Task<ServiceResult<bool>> DeleteAsync(Guid id);

        /// <summary>
        /// 启用验方
        /// </summary>
        Task<ServiceResult> EnableAsync(Guid id);

        /// <summary>
        /// 禁用验方
        /// </summary>
        Task<ServiceResult> DisableAsync(Guid id);

        /// <summary>
        /// 切换验方状态
        /// </summary>
        Task<ServiceResult<bool>> ToggleStatusAsync(Guid id);
    }
}
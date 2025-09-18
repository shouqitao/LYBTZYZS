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
    }
}
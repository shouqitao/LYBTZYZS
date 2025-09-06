using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Interfaces;

/// <summary>
/// 验方业务服务接口 - UltraThink三层架构业务逻辑层
/// 职责：业务流程编排、完整事务管理、基础CRUD操作
/// 简化版本：对应后端FormulasController实际API
/// </summary>
public interface IFormulaBusinessService {

    #region 核心业务操作 - 对应后端FormulasController

    /// <summary>
    /// 创建验方 (对应 POST /formulas)
    /// </summary>
    Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto createDto);

    /// <summary>
    /// 更新验方 (对应 PUT /formulas/{id})
    /// </summary>
    Task<ServiceResult<FormulaDto>> UpdateFormulaAsync(Guid id, FormulaUpdateDto updateDto);

    /// <summary>
    /// 删除验方 (对应 DELETE /formulas/{id})
    /// </summary>
    Task<ServiceResult<bool>> DeleteFormulaAsync(Guid id);

    /// <summary>
    /// 更新验方状态 (对应 PATCH /formulas/{id}/status)
    /// </summary>
    Task<ServiceResult<bool>> UpdateFormulaStatusAsync(Guid id, bool isEnabled);

    /// <summary>
    /// 启用验方
    /// </summary>
    Task<ServiceResult> EnableAsync(Guid id);

    /// <summary>
    /// 禁用验方
    /// </summary>
    Task<ServiceResult> DisableAsync(Guid id);

    /// <summary>
    /// 克隆验方 (基于现有验方创建新验方)
    /// </summary>
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId);

    #endregion 核心业务操作 - 对应后端FormulasController

    #region 业务验证 - 基础验证逻辑

    /// <summary>
    /// 验证验方名称可用性
    /// </summary>
    Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null);

    /// <summary>
    /// 验证验方业务规则
    /// </summary>
    ServiceResult ValidateFormulaBusinessRules(FormulaDto formula);

    /// <summary>
    /// 检查验方操作权限
    /// </summary>
    Task<ServiceResult<bool>> CheckFormulaOperationPermissionAsync(Guid formulaId, Guid userId, string operation);

    #endregion 业务验证 - 基础验证逻辑

    #region 业务流程处理

    /// <summary>
    /// 处理验方创建完整业务流程
    /// </summary>
    Task<ServiceResult<FormulaDto>> ProcessFormulaCreationAsync(FormulaCreateDto createDto, Guid userId);

    /// <summary>
    /// 处理验方更新完整业务流程
    /// </summary>
    Task<ServiceResult<FormulaDto>> ProcessFormulaUpdateAsync(Guid id, FormulaUpdateDto updateDto, Guid userId);

    #endregion 业务流程处理
}

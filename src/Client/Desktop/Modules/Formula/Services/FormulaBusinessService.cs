using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方业务服务 - UltraThink双层架构业务逻辑层
/// 简化版本：仅支持基础业务操作
/// </summary>
public class FormulaBusinessService(ILogger<FormulaBusinessService> logger) : IFormulaBusinessService
{
    private readonly ILogger<FormulaBusinessService> _logger = logger;

    #region 核心业务操作

    public Task<ServiceResult<FormulaDto>> CreateFormulaAsync(FormulaCreateDto createDto)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本暂不支持创建验方"));
    }

    public Task<ServiceResult<FormulaDto>> UpdateFormulaAsync(Guid id, FormulaUpdateDto updateDto)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本暂不支持更新验方"));
    }

    public Task<ServiceResult<bool>> DeleteFormulaAsync(Guid id)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持删除验方"));
    }

    #endregion

    #region 状态管理

    public Task<ServiceResult> EnableAsync(Guid id)
    {
        return Task.FromResult(ServiceResult.Failure("简单诊所版本暂不支持启用验方"));
    }

    public Task<ServiceResult> DisableAsync(Guid id)
    {
        return Task.FromResult(ServiceResult.Failure("简单诊所版本暂不支持禁用验方"));
    }

    #endregion

    #region 简化的不支持方法

    public Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null)
    {
        return Task.FromResult(ServiceResult<bool>.Success(true));
    }

    // IFormulaBusinessService缺失的方法
    public Task<ServiceResult<bool>> UpdateFormulaStatusAsync(Guid id, bool isEnabled)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本暂不支持更新验方状态"));
    }

    public Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本暂不支持克隆验方"));
    }

    public ServiceResult ValidateFormulaBusinessRules(FormulaDto formula)
    {
        return ServiceResult.Success("验证通过");
    }

    public Task<ServiceResult<bool>> CheckFormulaOperationPermissionAsync(Guid formulaId, Guid userId, string operation)
    {
        return Task.FromResult(ServiceResult<bool>.Success(true));
    }

    public Task<ServiceResult<FormulaDto>> ProcessFormulaCreationAsync(FormulaCreateDto createDto, Guid operatorId)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本暂不支持创建验方"));
    }

    public Task<ServiceResult<FormulaDto>> ProcessFormulaUpdateAsync(Guid id, FormulaUpdateDto updateDto, Guid operatorId)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本暂不支持更新验方"));
    }

    #endregion
}
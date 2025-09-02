using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Formula.Interfaces;

/// <summary>
/// 验方模块接口 - UltraThink三层架构纯委托层
/// 职责：统一服务入口，请求路由分发，事件转发
/// </summary>
public interface IFormulaModule : IFormulaService, IDisposable
{
    #region 事件定义

    /// <summary>
    /// 验方状态变更事件
    /// </summary>
    event EventHandler<FormulaStatusChangedEventArgs>? FormulaStatusChanged;

    /// <summary>
    /// 验方操作事件
    /// </summary>
    event EventHandler<FormulaOperationEventArgs>? FormulaOperation;

    /// <summary>
    /// 验方验证事件
    /// </summary>
    event EventHandler<FormulaValidationEventArgs>? FormulaValidation;

    #endregion

    #region 模块特定方法

    /// <summary>
    /// 根据名称获取验方
    /// </summary>
    Task<ServiceResult<FormulaDto>> GetByNameAsync(string name);

    /// <summary>
    /// 获取个人验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetPersonalFormulasAsync(Guid userId);

    /// <summary>
    /// 获取经典验方
    /// </summary>
    Task<ServiceResult<List<FormulaDto>>> GetClassicFormulasAsync();

    /// <summary>
    /// 克隆验方
    /// </summary>
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId);


    /// <summary>
    /// 批量启用验方
    /// </summary>
    Task<ServiceResult<int>> BatchEnableAsync(List<Guid> formulaIds);

    /// <summary>
    /// 批量禁用验方
    /// </summary>
    Task<ServiceResult<int>> BatchDisableAsync(List<Guid> formulaIds);

    /// <summary>
    /// 获取验方统计信息
    /// </summary>
    Task<ServiceResult<FormulaStatisticsDto>> GetFormulaStatisticsAsync();

    /// <summary>
    /// 检查验方名称可用性
    /// </summary>
    Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null);

    /// <summary>
    /// 导入验方
    /// </summary>
    Task<ServiceResult<FormulaImportResultDto>> ImportFormulasAsync(FormulaImportDto importDto);


    #endregion
}

/// <summary>
/// 验方状态变更事件参数
/// </summary>
public class FormulaStatusChangedEventArgs : EventArgs
{
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
    public bool OldStatus { get; set; }
    public bool NewStatus { get; set; }
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime ChangeTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 验方操作事件参数
/// </summary>
public class FormulaOperationEventArgs : EventArgs
{
    public string Operation { get; set; } = string.Empty;
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
    public Guid OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime OperationTime { get; set; } = DateTime.Now;
    public string? AdditionalInfo { get; set; }
}

/// <summary>
/// 验方验证事件参数
/// </summary>
public class FormulaValidationEventArgs : EventArgs
{
    public Guid FormulaId { get; set; }
    public string FormulaName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
    public DateTime ValidationTime { get; set; } = DateTime.Now;
}
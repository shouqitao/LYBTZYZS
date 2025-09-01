using System;
using System.Threading.Tasks;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Interfaces;

/// <summary>
/// 处方模块接口 - UltraThink三层架构模块层
/// 职责：统一模块入口，事件管理，模块间协调
/// </summary>
public interface IPrescriptionsModule : IPrescriptionService, IDisposable
{
    #region 事件定义

    /// <summary>
    /// 处方状态变更事件
    /// </summary>
    event EventHandler<PrescriptionStatusChangedEventArgs>? PrescriptionStatusChanged;

    /// <summary>
    /// 处方操作事件
    /// </summary>
    event EventHandler<PrescriptionOperationEventArgs>? PrescriptionOperation;

    /// <summary>
    /// 处方验证事件
    /// </summary>
    event EventHandler<PrescriptionValidationEventArgs>? PrescriptionValidation;

    #endregion

    #region 模块特定方法

    /// <summary>
    /// 获取处方打印信息
    /// </summary>
    Task<ServiceResult<PrescriptionPrintInfoDto>> GetPrintInfoAsync(Guid prescriptionId);

    /// <summary>
    /// 批量获取处方价格
    /// </summary>
    Task<ServiceResult<PrescriptionBatchPriceDto>> GetBatchPrescriptionPricesAsync(List<Guid> prescriptionIds);

    /// <summary>
    /// 应用折扣
    /// </summary>
    Task<ServiceResult<PrescriptionDto>> ApplyDiscountAsync(Guid prescriptionId, decimal discountRate, string reason);

    /// <summary>
    /// 计算单剂价格
    /// </summary>
    Task<ServiceResult<decimal>> CalculateSingleDosePriceAsync(Guid prescriptionId);

    /// <summary>
    /// 计算总价格
    /// </summary>
    Task<ServiceResult<decimal>> CalculateTotalPriceAsync(Guid prescriptionId);

    /// <summary>
    /// 获取处方使用历史
    /// </summary>
    Task<ServiceResult<List<PrescriptionUsageHistoryDto>>> GetUsageHistoryAsync(Guid prescriptionId);

    /// <summary>
    /// 批量更新处方状态
    /// </summary>
    Task<ServiceResult<int>> BatchUpdateStatusAsync(List<Guid> prescriptionIds, PrescriptionStatus status);

    /// <summary>
    /// 获取处方统计信息
    /// </summary>
    Task<ServiceResult<PrescriptionStatisticsDto>> GetPrescriptionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    #endregion
}

/// <summary>
/// 处方状态变更事件参数
/// </summary>
public class PrescriptionStatusChangedEventArgs : EventArgs
{
    public Guid PrescriptionId { get; set; }
    public PrescriptionStatus OldStatus { get; set; }
    public PrescriptionStatus NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// 处方操作事件参数
/// </summary>
public class PrescriptionOperationEventArgs : EventArgs
{
    public Guid PrescriptionId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string OperationDetails { get; set; } = string.Empty;
    public DateTime OperatedAt { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 处方验证事件参数
/// </summary>
public class PrescriptionValidationEventArgs : EventArgs
{
    public Guid PrescriptionId { get; set; }
    public string ValidationType { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// 处方打印信息DTO
/// </summary>
public class PrescriptionPrintInfoDto
{
    public PrescriptionDto Prescription { get; set; } = new();
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public DateTime PrintDate { get; set; }
    public string PrintNumber { get; set; } = string.Empty;
}

/// <summary>
/// 处方批量价格DTO
/// </summary>
public class PrescriptionBatchPriceDto
{
    public List<PrescriptionPriceItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// 处方价格项目DTO
/// </summary>
public class PrescriptionPriceItemDto
{
    public Guid PrescriptionId { get; set; }
    public string PrescriptionNo { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal SingleDosePrice { get; set; }
    public int DosageCount { get; set; }
}

/// <summary>
/// 处方使用历史DTO
/// </summary>
public class PrescriptionUsageHistoryDto
{
    public Guid Id { get; set; }
    public Guid PrescriptionId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActionDetails { get; set; } = string.Empty;
    public DateTime ActionTime { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

/// <summary>
/// 处方统计信息DTO
/// </summary>
public class PrescriptionStatisticsDto
{
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public List<PrescriptionDailyStatDto> DailyStats { get; set; } = new();
    public List<PrescriptionHerbStatDto> TopUsedHerbs { get; set; } = new();
}

/// <summary>
/// 处方日统计DTO
/// </summary>
public class PrescriptionDailyStatDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// 处方药材使用统计DTO
/// </summary>
public class PrescriptionHerbStatDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
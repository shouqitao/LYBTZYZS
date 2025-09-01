using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方模块 - UltraThink三层架构纯委托层
/// 职责：统一服务入口，请求路由分发，事件转发
/// </summary>
public class PrescriptionsModule(
    IPrescriptionsCoreService coreService,
    IPrescriptionsQueryService queryService,
    IPrescriptionsBusinessService businessService,
    IMapper mapper) : IPrescriptionService, IPrescriptionsModule, IDisposable
{
    private readonly IPrescriptionsCoreService _coreService = coreService;
    private readonly IPrescriptionsQueryService _queryService = queryService;
    private readonly IPrescriptionsBusinessService _businessService = businessService;
    private readonly IMapper _mapper = mapper;

    #region 事件转发

    /// <summary>
    /// 处方状态变更事件
    /// </summary>
    public event EventHandler<PrescriptionStatusChangedEventArgs>? PrescriptionStatusChanged
    {
        add => _businessService.PrescriptionStatusChanged += value;
        remove => _businessService.PrescriptionStatusChanged -= value;
    }

    /// <summary>
    /// 处方操作事件
    /// </summary>
    public event EventHandler<PrescriptionOperationEventArgs>? PrescriptionOperation
    {
        add => _businessService.PrescriptionOperation += value;
        remove => _businessService.PrescriptionOperation -= value;
    }

    /// <summary>
    /// 处方验证事件
    /// </summary>
    public event EventHandler<PrescriptionValidationEventArgs>? PrescriptionValidation
    {
        add => _businessService.PrescriptionValidation += value;
        remove => _businessService.PrescriptionValidation -= value;
    }

    #endregion

    #region IPrescriptionService基础CRUD接口实现

    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        => await _queryService.GetPagedAsync(query);

    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto)
        => await _businessService.CreatePrescriptionAsync(createDto);

    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto)
        => await _businessService.UpdatePrescriptionAsync(id, updateDto);

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeletePrescriptionAsync(id);

    #endregion

    #region IPrescriptionService搜索接口实现

    public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.GetByPatientIdAsync(patientId);

    public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        => await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);

    #endregion

    #region IPrescriptionService状态管理接口实现

    public async Task<ServiceResult> CompletePrescriptionAsync(Guid id)
        => await _businessService.CompletePrescriptionAsync(id);

    public async Task<ServiceResult> VoidPrescriptionAsync(Guid id, string reason)
        => await _businessService.VoidPrescriptionAsync(id, reason);

    #endregion

    #region IPrescriptionService验证接口实现

    public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        => await _coreService.ValidateCreateDtoAsync(dto);

    #endregion

    #region IPrescriptionService复制接口实现

    public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        => await _businessService.CopyPrescriptionAsync(id, newName);

    #endregion

    #region IPrescriptionsModule模块特定方法

    public async Task<ServiceResult<PrescriptionPrintInfoDto>> GetPrintInfoAsync(Guid prescriptionId)
        => await _businessService.GetPrintInfoAsync(prescriptionId);

    public async Task<ServiceResult<PrescriptionBatchPriceDto>> GetBatchPrescriptionPricesAsync(List<Guid> prescriptionIds)
        => await _businessService.CalculateBatchPricesAsync(prescriptionIds);

    public async Task<ServiceResult<PrescriptionDto>> ApplyDiscountAsync(Guid prescriptionId, decimal discountRate, string reason)
        => await _businessService.ApplyDiscountAsync(prescriptionId, discountRate, reason);

    public async Task<ServiceResult<decimal>> CalculateSingleDosePriceAsync(Guid prescriptionId)
        => await _businessService.CalculateSingleDosePriceAsync(prescriptionId);

    public async Task<ServiceResult<decimal>> CalculateTotalPriceAsync(Guid prescriptionId)
        => await _businessService.CalculateTotalPriceAsync(prescriptionId);

    public async Task<ServiceResult<List<PrescriptionUsageHistoryDto>>> GetUsageHistoryAsync(Guid prescriptionId)
        => await _businessService.GetUsageHistoryAsync(prescriptionId);

    public async Task<ServiceResult<int>> BatchUpdateStatusAsync(List<Guid> prescriptionIds, PrescriptionStatus status)
    {
        var result = await _businessService.BatchUpdateStatusAsync(prescriptionIds, status);
        return result.IsSuccess 
            ? ServiceResult<int>.Success(result.Data.SuccessCount)
            : ServiceResult<int>.Failure(result.ErrorMessage);
    }

    public async Task<ServiceResult<PrescriptionStatisticsDto>> GetPrescriptionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        => await _queryService.GetPrescriptionStatisticsAsync(startDate, endDate);

    #endregion

    #region IPrescriptionService兼容性接口实现

    /// <summary>
    /// 获取处方模板（兼容性接口）
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetTemplatesAsync()
    {
        var query = new PrescriptionQueryDto
        {
            PageIndex = 1,
            PageSize = 100,
            Keyword = "模板" // 简化的模板查询
        };
        var result = await _queryService.GetPagedAsync(query);
        return result.IsSuccess
            ? ServiceResult<List<PrescriptionDto>>.Success(result.Data?.Items?.ToList() ?? new List<PrescriptionDto>())
            : ServiceResult<List<PrescriptionDto>>.Failure(result.ErrorMessage ?? "获取处方模板失败");
    }

    /// <summary>
    /// 根据类型获取处方（兼容性接口）
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByTypeAsync(string prescriptionType)
    {
        // 简化实现：根据类型过滤处方
        var searchResult = await _queryService.SearchAsync(prescriptionType);
        return searchResult;
    }

    /// <summary>
    /// 获取处方列表（兼容性接口）
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
    {
        if (!string.IsNullOrEmpty(keyword))
        {
            return await _queryService.SearchAsync(keyword);
        }
        
        var query = new PrescriptionQueryDto
        {
            PageIndex = 1,
            PageSize = 1000
        };
        var result = await _queryService.GetPagedAsync(query);
        return result.IsSuccess
            ? ServiceResult<List<PrescriptionDto>>.Success(result.Data?.Items?.ToList() ?? new List<PrescriptionDto>())
            : ServiceResult<List<PrescriptionDto>>.Failure(result.ErrorMessage ?? "获取处方列表失败");
    }

    /// <summary>
    /// 获取所有处方（兼容性接口）
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetAllFormulasAsync()
        => await GetFormulasAsync();

    /// <summary>
    /// 切换处方状态（兼容性接口）
    /// </summary>
    public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
    {
        var prescriptionResult = await _queryService.GetByIdAsync(id);
        if (!prescriptionResult.IsSuccess || prescriptionResult.Data == null)
        {
            return ServiceResult<bool>.Failure("获取处方信息失败");
        }

        var isCompleted = prescriptionResult.Data.PrescriptionStatus == PrescriptionStatus.Completed;
        ServiceResult result;
        
        if (isCompleted)
        {
            // 如果已完成，则作废
            result = await _businessService.VoidPrescriptionAsync(id, "状态切换");
        }
        else
        {
            // 如果未完成，则完成
            result = await _businessService.CompletePrescriptionAsync(id);
        }
        
        return ServiceResult<bool>.Success(result.IsSuccess);
    }

    #endregion

    #region 简化的不支持方法（UltraThink简化版）

    public Task<ServiceResult<PrescriptionDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
    {
        return Task.FromResult(ServiceResult<PrescriptionDto>.Failure("简单诊所版本不支持从处方创建处方功能"));
    }

    public Task<ServiceResult<PrescriptionAnalysisResult>> AnalyzePrescriptionAsync(Guid prescriptionId)
    {
        return Task.FromResult(ServiceResult<PrescriptionAnalysisResult>.Failure("简单诊所版本不支持处方分析功能"));
    }

    public Task<ServiceResult<List<PrescriptionRecommendationDto>>> GetRecommendationsAsync(string syndrome)
    {
        return Task.FromResult(ServiceResult<List<PrescriptionRecommendationDto>>.Success(new List<PrescriptionRecommendationDto>()));
    }

    public Task<ServiceResult<List<PrescriptionRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId)
    {
        return Task.FromResult(ServiceResult<List<PrescriptionRecommendationDto>>.Success(new List<PrescriptionRecommendationDto>()));
    }

    public Task<ServiceResult<bool>> SharePrescriptionAsync(Guid id, Guid operatorId, string operatorName)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本不支持处方分享功能"));
    }

    public Task<ServiceResult<bool>> UnsharePrescriptionAsync(Guid id, Guid operatorId, string operatorName)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本不支持处方分享功能"));
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        // 清理事件订阅
        // 注意：在实际实现中，这里的事件清理是自动的，因为我们使用的是委托转发
        GC.SuppressFinalize(this);
    }

    #endregion
}
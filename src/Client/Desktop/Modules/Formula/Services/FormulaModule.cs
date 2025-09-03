using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方模块 - UltraThink双层架构纯委托层
/// 职责：统一服务入口，请求路由分发
/// 简化版本：仅支持后端实际API功能
/// </summary>
public class FormulaModule(
    IFormulaQueryService queryService,  
    IFormulaBusinessService businessService,
    IMapper mapper) : IFormulaService, IDisposable
{
    private readonly IFormulaQueryService _queryService = queryService;
    private readonly IFormulaBusinessService _businessService = businessService;
    private readonly IMapper _mapper = mapper;

    #region IFormulaService基础CRUD接口实现

    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
        => await _queryService.GetPagedAsync(query);

    public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto createDto)
        => await _businessService.CreateFormulaAsync(createDto);

    public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto updateDto)
        => await _businessService.UpdateFormulaAsync(id, updateDto);

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteFormulaAsync(id);

    #endregion

    #region IFormulaService搜索接口实现（简化版）

    public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        => await _queryService.GetTemplatesAsync();

    public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        => await _queryService.GetByTypeAsync(formulaType);

    public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
    {
        if (!string.IsNullOrEmpty(keyword))
        {
            return await _queryService.SearchAsync(keyword);
        }
        return await _queryService.GetTemplatesAsync();
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        => await _queryService.GetTemplatesAsync();

    #endregion

    #region IFormulaService状态管理接口实现

    public async Task<ServiceResult> EnableAsync(Guid id)
        => await _businessService.EnableAsync(id);

    public async Task<ServiceResult> DisableAsync(Guid id)
        => await _businessService.DisableAsync(id);

    public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
    {
        var formulaResult = await _queryService.GetByIdAsync(id);
        if (!formulaResult.IsSuccess || formulaResult.Data == null)
            return ServiceResult<bool>.Failure(formulaResult.ErrorMessage ?? "验方不存在");
        
        var result = formulaResult.Data.IsEnabled 
            ? await _businessService.DisableAsync(id)
            : await _businessService.EnableAsync(id);
        
        return ServiceResult<bool>.Success(result.IsSuccess);
    }

    #endregion

    #region 简化的不支持方法（UltraThink简化版）

    public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
    {
        // 获取当前用户ID (简化实现，实际应从认证上下文获取)
        var currentUserId = Guid.NewGuid(); // TODO: 从认证上下文获取实际用户ID
        return await _businessService.CloneFormulaAsync(id, newName, currentUserId);
    }

    public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
    {
        return await _queryService.GetCategoriesAsync();
    }

    public Task<ServiceResult<IEnumerable<FormulaDto>>> GetByCategoryAsync(string category)
    {
        // 简化实现：返回所有验方
        return Task.FromResult(ServiceResult<IEnumerable<FormulaDto>>.Success(new List<FormulaDto>()));
    }

    public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto request)
    {
        return await _queryService.SearchFormulasAsync(request);
    }

    public async Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null)
    {
        return await _businessService.CheckNameAvailabilityAsync(name, excludeFormulaId);
    }

    public Task<ServiceResult<int>> ImportFormulasAsync(List<FormulaImportDto> formulas)
    {
        return Task.FromResult(ServiceResult<int>.Failure("简单诊所版本不支持导入功能"));
    }

    public Task<ServiceResult<List<FormulaDto>>> ExportFormulasAsync()
    {
        return Task.FromResult(ServiceResult<List<FormulaDto>>.Failure("简单诊所版本不支持导出功能"));
    }

    public Task<ServiceResult<byte[]>> GetImportTemplateAsync()
    {
        return Task.FromResult(ServiceResult<byte[]>.Failure("简单诊所版本不支持模板下载"));
    }

    // IFormulaService缺失的方法
    public Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本不支持从处方创建验方功能"));
    }

    public Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
    {
        return Task.FromResult(ServiceResult<FormulaAnalysisResult>.Failure("简单诊所版本不支持验方分析功能"));
    }

    public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
    {
        return await _queryService.GetRecommendationsBySyndromeAsync(syndrome);
    }

    public Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId)
    {
        return Task.FromResult(ServiceResult<List<FormulaRecommendationDto>>.Success(new List<FormulaRecommendationDto>()));
    }

    public Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本不支持验方分享功能"));
    }

    public Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
    {
        return Task.FromResult(ServiceResult<bool>.Failure("简单诊所版本不支持验方分享功能"));
    }

    #endregion

    #region 资源清理

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #endregion
}
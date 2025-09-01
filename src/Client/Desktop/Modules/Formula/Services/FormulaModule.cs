using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Formula.Services;

/// <summary>
/// 验方模块 - UltraThink三层架构纯委托层
/// 职责：统一服务入口，请求路由分发，事件转发
/// </summary>
public class FormulaModule(
    IFormulaCoreService coreService,
    IFormulaQueryService queryService,  
    IFormulaBusinessService businessService,
    IMapper mapper) : IFormulaService, IFormulaModule, IDisposable
{
    private readonly IFormulaCoreService _coreService = coreService;
    private readonly IFormulaQueryService _queryService = queryService;
    private readonly IFormulaBusinessService _businessService = businessService;
    private readonly IMapper _mapper = mapper;
        
    #region 事件转发

    /// <summary>
    /// 验方状态变更事件
    /// </summary>
    public event EventHandler<FormulaStatusChangedEventArgs>? FormulaStatusChanged
    {
        add => _businessService.FormulaStatusChanged += value;
        remove => _businessService.FormulaStatusChanged -= value;
    }

    /// <summary>
    /// 验方操作事件
    /// </summary>
    public event EventHandler<FormulaOperationEventArgs>? FormulaOperation
    {
        add => _businessService.FormulaOperation += value;
        remove => _businessService.FormulaOperation -= value;
    }

    /// <summary>
    /// 验方验证事件
    /// </summary>
    public event EventHandler<FormulaValidationEventArgs>? FormulaValidation
    {
        add => _businessService.FormulaValidation += value;
        remove => _businessService.FormulaValidation -= value;
    }

    #endregion

    #region IFormulaService基础CRUD接口实现

    public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaPagedQueryDto query)
        => await _queryService.GetPagedAsync(query);

    public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        => await _coreService.CallGetFormulaByIdApiAsync(id);

    public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto createDto)
        => await _businessService.CreateFormulaAsync(createDto);

    public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto updateDto)
        => await _businessService.UpdateFormulaAsync(id, updateDto);

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteFormulaAsync(id);
        
    #endregion

    #region IFormulaService搜索接口实现

    public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto request)
    {
        var searchDto = new FormulaSearchDto
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            Name = request.Keyword
        };
        return await _queryService.SearchFormulasAsync(searchDto);
    }

    public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
    {
        // TODO: 实现类型分布查询转换为分类列表
        var typeDistributionResult = await _queryService.GetFormulaTypeDistributionAsync();
        if (!typeDistributionResult.IsSuccess)
        {
            // 返回默认分类
            var defaultCategories = new List<string>
            {
                "全部", "内科方", "外科方", "妇科方", "儿科方",
                "皮肤科方", "五官科方", "骨伤科方", "经典方", "时方", "验方", "其他"
            };
            return ServiceResult<List<string>>.Success(defaultCategories);
        }
        
        var categories = new List<string> { "全部" };
        categories.AddRange(typeDistributionResult.Data.Keys);
        return ServiceResult<List<string>>.Success(categories);
    }

    public async Task<ServiceResult<IEnumerable<FormulaDto>>> GetByCategoryAsync(string category)
    {
        List<FormulaDto> formulas;
        if (string.IsNullOrWhiteSpace(category) || category == "全部")
        {
            var allResult = await _queryService.GetActiveFormulasAsync();
            if (!allResult.IsSuccess)
                return ServiceResult<IEnumerable<FormulaDto>>.Failure(allResult.ErrorMessage);
            formulas = allResult.Data;
        }
        else
        {
            var typeResult = await _queryService.SearchByTypeAsync(category);
            if (!typeResult.IsSuccess)
                return ServiceResult<IEnumerable<FormulaDto>>.Failure(typeResult.ErrorMessage);
            formulas = typeResult.Data;
        }
        
        return ServiceResult<IEnumerable<FormulaDto>>.Success(formulas);
    }
        
    #endregion

    #region IFormulaService状态管理接口实现

    public async Task<ServiceResult> EnableAsync(Guid id)
        => await _businessService.EnableFormulaAsync(id);

    public async Task<ServiceResult> DisableAsync(Guid id)
        => await _businessService.DisableFormulaAsync(id);
        
    #endregion

    #region IFormulaService验证接口实现

    public async Task<ServiceResult<bool>> CheckNameAvailabilityAsync(string name, Guid? excludeFormulaId = null)
        => await _businessService.CheckNameAvailabilityAsync(name, excludeFormulaId);

    #endregion

    #region IFormulaService导入导出接口实现

    public async Task<ServiceResult<int>> ImportFormulasAsync(List<FormulaImportDto> formulas)
    {
        var importDto = new FormulaImportDto
        {
            Records = formulas.Select(f => new FormulaImportRecordDto
            {
                Name = f.Name,
                Type = f.Type,
                Source = f.Source,
                Effect = f.Effect,
                Ingredients = f.Ingredients
            }).ToList(),
            SkipDuplicates = true,
            ValidateData = true
        };

        var result = await _businessService.ImportFormulasAsync(importDto);
        return result.IsSuccess 
            ? ServiceResult<int>.Success(result.Data.SuccessCount)
            : ServiceResult<int>.Failure(result.ErrorMessage);
    }

    public async Task<ServiceResult<List<FormulaDto>>> ExportFormulasAsync()
    {
        var exportQuery = new FormulaExportQueryDto
        {
            IncludePersonalInfo = true,
            IncludeMedicalInfo = false
        };

        var result = await _businessService.ExportFormulasAsync(exportQuery);
        if (!result.IsSuccess)
        {
            return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage);
        }

        // 简化：返回基础信息，实际应该转换为FormulaDto
        var basicInfoResult = await _queryService.GetFormulaBasicInfoAsync();
        var formulaDtos = basicInfoResult.Data?.Select(info => new FormulaDto
        {
            Id = info.Id,
            Name = info.Name,
            Type = info.Type,
            Source = info.Source,
            IsEnabled = info.IsEnabled
        }).ToList() ?? new List<FormulaDto>();

        return ServiceResult<List<FormulaDto>>.Success(formulaDtos);
    }

    public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
    {
        return await _businessService.GenerateImportTemplateAsync();
    }
        
    #endregion

    #region IFormulaModule模块特定方法

    public async Task<ServiceResult<FormulaDto>> GetByNameAsync(string name)
        => await _queryService.GetFormulaByNameAsync(name);

    public async Task<ServiceResult<List<FormulaDto>>> GetPersonalFormulasAsync(Guid userId)
        => await _queryService.GetPersonalFormulasAsync(userId);

    public async Task<ServiceResult<List<FormulaDto>>> GetClassicFormulasAsync()
        => await _queryService.GetClassicFormulasAsync();

    public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId, string newName, Guid userId)
        => await _businessService.CloneFormulaAsync(formulaId, newName, userId);

    public async Task<ServiceResult<FormulaValidationResultDto>> ValidateFormulaCompletenessAsync(Guid formulaId)
        => await _businessService.ValidateFormulaCompletenessAsync(formulaId);

    public async Task<ServiceResult<List<FormulaUsageHistoryDto>>> GetFormulaUsageHistoryAsync(Guid formulaId)
        => await _businessService.GetFormulaUsageHistoryAsync(formulaId);

    public async Task<ServiceResult<int>> BatchEnableAsync(List<Guid> formulaIds)
    {
        var result = await _businessService.BatchUpdateFormulaStatusAsync(formulaIds, true);
        return result.IsSuccess 
            ? ServiceResult<int>.Success(result.Data.SuccessCount)
            : ServiceResult<int>.Failure(result.ErrorMessage);
    }

    public async Task<ServiceResult<int>> BatchDisableAsync(List<Guid> formulaIds)
    {
        var result = await _businessService.BatchUpdateFormulaStatusAsync(formulaIds, false);
        return result.IsSuccess 
            ? ServiceResult<int>.Success(result.Data.SuccessCount)
            : ServiceResult<int>.Failure(result.ErrorMessage);
    }

    public async Task<ServiceResult<FormulaStatisticsDto>> GetFormulaStatisticsAsync()
        => await _queryService.GetFormulaStatisticsAsync();

    public async Task<ServiceResult<FormulaImportResultDto>> ImportFormulasAsync(FormulaImportDto importDto)
        => await _businessService.ImportFormulasAsync(importDto);

    public async Task<ServiceResult<FormulaExportResultDto>> ExportFormulasAsync(FormulaExportQueryDto exportQuery)
        => await _businessService.ExportFormulasAsync(exportQuery);

    #endregion

    #region IFormulaService兼容性接口实现

    public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        => await _queryService.GetActiveFormulasAsync();

    public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        => await _queryService.SearchByTypeAsync(formulaType);

    public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
    {
        if (!string.IsNullOrEmpty(keyword))
        {
            var searchResult = await _queryService.SearchByNameAsync(keyword);
            if (searchResult.IsSuccess && !string.IsNullOrEmpty(category) && category != "全部")
            {
                var filtered = searchResult.Data.Where(f => f.Type == category).ToList();
                return ServiceResult<List<FormulaDto>>.Success(filtered);
            }
            return searchResult;
        }
        
        if (!string.IsNullOrEmpty(category) && category != "全部")
        {
            return await _queryService.SearchByTypeAsync(category);
        }
        
        return await _queryService.GetActiveFormulasAsync();
    }

    public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        => await _queryService.GetActiveFormulasAsync();

    public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
        => await _businessService.CloneFormulaAsync(id, newName, Guid.Empty); // TODO: 获取当前用户ID

    public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
    {
        var formulaResult = await _coreService.CallGetFormulaByIdApiAsync(id);
        if (!formulaResult.IsSuccess)
            return ServiceResult<bool>.Failure(formulaResult.ErrorMessage);
        
        var result = formulaResult.Data.IsEnabled 
            ? await _businessService.DisableFormulaAsync(id)
            : await _businessService.EnableFormulaAsync(id);
        
        return ServiceResult<bool>.Success(result.IsSuccess);
    }

    #endregion

    #region 简化的不支持方法（UltraThink简化版）

    public Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
    {
        return Task.FromResult(ServiceResult<FormulaDto>.Failure("简单诊所版本不支持从处方创建验方功能"));
    }

    public Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
    {
        return Task.FromResult(ServiceResult<FormulaAnalysisResult>.Failure("简单诊所版本不支持验方分析功能"));
    }

    public Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
    {
        return Task.FromResult(ServiceResult<List<FormulaRecommendationDto>>.Success(new List<FormulaRecommendationDto>()));
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
        // 清理事件订阅
        // 注意：在实际实现中，这里的事件清理是自动的，因为我们使用的是委托转发
        GC.SuppressFinalize(this);
    }

    #endregion
}
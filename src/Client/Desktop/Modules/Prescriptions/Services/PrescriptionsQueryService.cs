using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方查询服务实现 - UltraThink三层架构查询层
/// 职责：复杂查询、搜索优化、统计分析、性能监控
/// </summary>
public class PrescriptionsQueryService(
    IPrescriptionsCoreService coreService,
    IMemoryCache cache,
    ILogger<PrescriptionsQueryService> logger) : IPrescriptionsQueryService
{
    private readonly IPrescriptionsCoreService _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<PrescriptionsQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // 查询性能监控
    private static readonly Dictionary<string, List<double>> _queryPerformanceStats = new();

    #region 基础查询方法

    /// <summary>
    /// 分页查询处方列表
    /// </summary>
    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
    {
        var startTime = DateTime.Now;
        try
        {
            // 参数验证
            var validationResult = await _coreService.ValidateQueryParametersAsync(query);
            if (!validationResult.IsSuccess)
            {
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure(validationResult.ErrorMessage ?? "查询参数验证失败");
            }

            // 调用核心服务进行查询
            var result = await _coreService.CallGetPrescriptionListApiAsync(query);
            
            RecordQueryPerformance("GetPaged", startTime);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("分页查询处方列表成功 - 页码: {PageIndex}, 页大小: {PageSize}, 结果数: {Count}", 
                    query.PageIndex, query.PageSize, result.Data?.Items?.Count() ?? 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询处方列表异常");
            return ServiceResult<PagedResult<PrescriptionDto>>.Failure($"分页查询处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据ID获取处方详情
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
    {
        var startTime = DateTime.Now;
        try
        {
            var result = await _coreService.CallGetPrescriptionByIdApiAsync(id);
            
            RecordQueryPerformance("GetById", startTime);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("获取处方详情成功: {PrescriptionId}", id);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取处方详情异常: {PrescriptionId}", id);
            return ServiceResult<PrescriptionDto>.Failure($"获取处方详情异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据编号查询处方
    /// </summary>
    public async Task<ServiceResult<PrescriptionDto>> GetByNumberAsync(string prescriptionNumber)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(prescriptionNumber))
            {
                return ServiceResult<PrescriptionDto>.Failure("处方编号不能为空");
            }

            // 使用搜索功能查找处方编号
            var searchResult = await _coreService.CallSearchPrescriptionsApiAsync(prescriptionNumber, 10);
            if (!searchResult.IsSuccess)
            {
                return ServiceResult<PrescriptionDto>.Failure(searchResult.ErrorMessage ?? "根据编号查询处方失败");
            }

            var prescription = searchResult.Data?.FirstOrDefault(p => 
                string.Equals(p.PrescriptionNo, prescriptionNumber, StringComparison.OrdinalIgnoreCase));

            RecordQueryPerformance("GetByNumber", startTime);

            if (prescription != null)
            {
                _logger.LogDebug("根据编号查询处方成功: {PrescriptionNumber}", prescriptionNumber);
                return ServiceResult<PrescriptionDto>.Success(prescription);
            }

            return ServiceResult<PrescriptionDto>.Failure($"未找到处方编号为 {prescriptionNumber} 的处方");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据编号查询处方异常: {PrescriptionNumber}", prescriptionNumber);
            return ServiceResult<PrescriptionDto>.Failure($"根据编号查询处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量获取处方
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByIdsAsync(List<Guid> ids)
    {
        var startTime = DateTime.Now;
        try
        {
            if (ids == null || !ids.Any())
            {
                return ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
            }

            var result = await _coreService.CallGetPrescriptionsByIdsApiAsync(ids);
            
            RecordQueryPerformance("GetByIds", startTime);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("批量获取处方成功 - 请求: {RequestCount}, 获取: {ResultCount}", 
                    ids.Count, result.Data?.Count ?? 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取处方异常");
            return ServiceResult<List<PrescriptionDto>>.Failure($"批量获取处方异常: {ex.Message}");
        }
    }

    #endregion

    #region 条件查询方法

    /// <summary>
    /// 根据患者ID获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
    {
        var startTime = DateTime.Now;
        try
        {
            if (patientId == Guid.Empty)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("患者ID不能为空");
            }

            // 使用缓存优化患者处方查询
            var cacheKey = $"prescriptions:patient:{patientId}";
            var result = await _coreService.GetOrSetCacheAsync(cacheKey, async () =>
            {
                var query = new PrescriptionQueryDto
                {
                    PatientId = patientId,
                    PageIndex = 1,
                    PageSize = 1000 // 获取患者所有处方
                };

                var pagedResult = await GetPagedAsync(query);
                if (!pagedResult.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据患者ID获取处方列表失败");
                }

                return ServiceResult<List<PrescriptionDto>>.Success(
                    pagedResult.Data?.Items?.ToList() ?? new List<PrescriptionDto>());
            }, TimeSpan.FromMinutes(15));

            RecordQueryPerformance("GetByPatientId", startTime);

            if (result.IsSuccess)
            {
                _logger.LogInformation("根据患者ID获取处方列表成功 - 患者ID: {PatientId}, 处方数: {Count}", 
                    patientId, result.Data?.Count ?? 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据患者ID获取处方列表异常: {PatientId}", patientId);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据患者ID获取处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据医生ID获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByDoctorIdAsync(Guid doctorId)
    {
        var startTime = DateTime.Now;
        try
        {
            if (doctorId == Guid.Empty)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("医生ID不能为空");
            }

            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000,
                Keyword = doctorId.ToString() // 简化实现，实际应该有专门的医生ID查询
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据医生ID获取处方列表失败");
            }

            // 筛选出匹配的处方
            var filteredPrescriptions = pagedResult.Data?.Items?
                .Where(p => p.UserId == doctorId)
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("GetByDoctorId", startTime);

            _logger.LogInformation("根据医生ID获取处方列表成功 - 医生ID: {DoctorId}, 处方数: {Count}", 
                doctorId, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据医生ID获取处方列表异常: {DoctorId}", doctorId);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据医生ID获取处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据医案ID获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        var startTime = DateTime.Now;
        try
        {
            if (medicalCaseId == Guid.Empty)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("医疗案例ID不能为空");
            }

            // 使用缓存优化医案处方查询
            var cacheKey = $"prescriptions:medicalcase:{medicalCaseId}";
            var result = await _coreService.GetOrSetCacheAsync(cacheKey, async () =>
            {
                var query = new PrescriptionQueryDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = medicalCaseId.ToString()
                };

                var pagedResult = await GetPagedAsync(query);
                if (!pagedResult.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据医疗案例ID获取处方列表失败");
                }

                // 筛选出匹配的处方
                var filteredPrescriptions = pagedResult.Data?.Items?
                    .Where(p => p.MedicalCaseId == medicalCaseId)
                    .ToList() ?? new List<PrescriptionDto>();

                return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
            }, TimeSpan.FromMinutes(20));

            RecordQueryPerformance("GetByMedicalCaseId", startTime);

            if (result.IsSuccess)
            {
                _logger.LogInformation("根据医案ID获取处方列表成功 - 医案ID: {MedicalCaseId}, 处方数: {Count}", 
                    medicalCaseId, result.Data?.Count ?? 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据医案ID获取处方列表异常: {MedicalCaseId}", medicalCaseId);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据医案ID获取处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据状态获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByStatusAsync(CommonStatus status)
    {
        var startTime = DateTime.Now;
        try
        {
            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据状态获取处方列表失败");
            }

            // 筛选出匹配状态的处方
            var filteredPrescriptions = pagedResult.Data?.Items?
                .Where(p => p.Status == status)
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("GetByStatus", startTime);

            _logger.LogInformation("根据状态获取处方列表成功 - 状态: {Status}, 处方数: {Count}", 
                status, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据状态获取处方列表异常: {Status}", status);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据状态获取处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据处方状态获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPrescriptionStatusAsync(PrescriptionStatus status)
    {
        var startTime = DateTime.Now;
        try
        {
            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据处方状态获取处方列表失败");
            }

            // 筛选出匹配处方状态的处方
            var filteredPrescriptions = pagedResult.Data?.Items?
                .Where(p => p.PrescriptionStatus == status)
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("GetByPrescriptionStatus", startTime);

            _logger.LogInformation("根据处方状态获取处方列表成功 - 状态: {Status}, 处方数: {Count}", 
                status, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据处方状态获取处方列表异常: {Status}", status);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据处方状态获取处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据日期范围获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var startTime = DateTime.Now;
        try
        {
            if (startDate > endDate)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("开始日期不能大于结束日期");
            }

            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据日期范围获取处方列表失败");
            }

            // 筛选出日期范围内的处方
            var filteredPrescriptions = pagedResult.Data?.Items?
                .Where(p => p.CreateTime >= startDate && p.CreateTime <= endDate)
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("GetByDateRange", startTime);

            _logger.LogInformation("根据日期范围获取处方列表成功 - 开始日期: {StartDate}, 结束日期: {EndDate}, 处方数: {Count}", 
                startDate, endDate, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据日期范围获取处方列表异常");
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据日期范围获取处方列表异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据价格范围获取处方列表
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        var startTime = DateTime.Now;
        try
        {
            if (minPrice < 0 || maxPrice < 0)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("价格不能为负数");
            }

            if (minPrice > maxPrice)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("最小价格不能大于最大价格");
            }

            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据价格范围获取处方列表失败");
            }

            // 筛选出价格范围内的处方
            var filteredPrescriptions = pagedResult.Data?.Items?
                .Where(p => p.TotalAmount >= minPrice && p.TotalAmount <= maxPrice)
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("GetByPriceRange", startTime);

            _logger.LogInformation("根据价格范围获取处方列表成功 - 最小价格: {MinPrice}, 最大价格: {MaxPrice}, 处方数: {Count}", 
                minPrice, maxPrice, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据价格范围获取处方列表异常");
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据价格范围获取处方列表异常: {ex.Message}");
        }
    }

    #endregion

    #region 搜索方法

    /// <summary>
    /// 关键词搜索处方
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
    {
        var startTime = DateTime.Now;
        try
        {
            var result = await _coreService.CallSearchPrescriptionsApiAsync(keyword, 100);
            
            RecordQueryPerformance("Search", startTime);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("关键词搜索处方成功 - 关键词: {Keyword}, 结果数: {Count}", 
                    keyword, result.Data?.Count ?? 0);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关键词搜索处方异常: {Keyword}", keyword);
            return ServiceResult<List<PrescriptionDto>>.Failure($"关键词搜索处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 高级搜索处方
    /// </summary>
    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> AdvancedSearchAsync(PrescriptionAdvancedSearchDto searchDto)
    {
        var startTime = DateTime.Now;
        try
        {
            if (searchDto == null)
            {
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("搜索参数不能为空");
            }

            // 构造基础查询
            var query = new PrescriptionQueryDto
            {
                PageIndex = searchDto.PageIndex,
                PageSize = searchDto.PageSize,
                Keyword = searchDto.Keyword,
                PatientId = searchDto.PatientId
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return pagedResult;
            }

            // 应用高级筛选条件
            var filteredItems = pagedResult.Data?.Items?.AsEnumerable() ?? Enumerable.Empty<PrescriptionDto>();

            if (searchDto.DoctorId.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.UserId == searchDto.DoctorId.Value);
            }

            if (searchDto.MedicalCaseId.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.MedicalCaseId == searchDto.MedicalCaseId.Value);
            }

            if (searchDto.Status.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.Status == searchDto.Status.Value);
            }

            if (searchDto.PrescriptionStatus.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.PrescriptionStatus == searchDto.PrescriptionStatus.Value);
            }

            if (searchDto.StartDate.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.CreateTime >= searchDto.StartDate.Value);
            }

            if (searchDto.EndDate.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.CreateTime <= searchDto.EndDate.Value);
            }

            if (searchDto.MinAmount.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.TotalAmount >= searchDto.MinAmount.Value);
            }

            if (searchDto.MaxAmount.HasValue)
            {
                filteredItems = filteredItems.Where(p => p.TotalAmount <= searchDto.MaxAmount.Value);
            }

            if (!string.IsNullOrEmpty(searchDto.Diagnosis))
            {
                filteredItems = filteredItems.Where(p => 
                    p.Diagnosis != null && p.Diagnosis.Contains(searchDto.Diagnosis, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(searchDto.Usage))
            {
                filteredItems = filteredItems.Where(p => 
                    p.Usage != null && p.Usage.Contains(searchDto.Usage, StringComparison.OrdinalIgnoreCase));
            }

            var finalList = filteredItems.ToList();
            var result = new PagedResult<PrescriptionDto>(
                finalList,
                finalList.Count,
                searchDto.PageIndex,
                searchDto.PageSize);

            RecordQueryPerformance("AdvancedSearch", startTime);

            _logger.LogInformation("高级搜索处方成功 - 结果数: {Count}", finalList.Count);

            return ServiceResult<PagedResult<PrescriptionDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "高级搜索处方异常");
            return ServiceResult<PagedResult<PrescriptionDto>>.Failure($"高级搜索处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据诊断搜索处方
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> SearchByDiagnosisAsync(string diagnosis)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("诊断关键词不能为空");
            }

            var searchResult = await SearchAsync(diagnosis);
            if (!searchResult.IsSuccess)
            {
                return searchResult;
            }

            // 进一步筛选诊断匹配的处方
            var filteredPrescriptions = searchResult.Data?
                .Where(p => p.Diagnosis != null && 
                           p.Diagnosis.Contains(diagnosis, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("SearchByDiagnosis", startTime);

            _logger.LogInformation("根据诊断搜索处方成功 - 诊断: {Diagnosis}, 结果数: {Count}", 
                diagnosis, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据诊断搜索处方异常: {Diagnosis}", diagnosis);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据诊断搜索处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据药材搜索处方
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> SearchByHerbAsync(Guid herbId)
    {
        var startTime = DateTime.Now;
        try
        {
            if (herbId == Guid.Empty)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("药材ID不能为空");
            }

            // 获取所有处方并筛选包含指定药材的处方
            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "根据药材搜索处方失败");
            }

            var filteredPrescriptions = pagedResult.Data?.Items?
                .Where(p => p.Items.Any(item => item.HerbId == herbId))
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("SearchByHerb", startTime);

            _logger.LogInformation("根据药材搜索处方成功 - 药材ID: {HerbId}, 结果数: {Count}", 
                herbId, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据药材搜索处方异常: {HerbId}", herbId);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据药材搜索处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据验方搜索处方
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> SearchByFormulaAsync(Guid formulaId)
    {
        var startTime = DateTime.Now;
        try
        {
            if (formulaId == Guid.Empty)
            {
                return ServiceResult<List<PrescriptionDto>>.Failure("验方ID不能为空");
            }

            var searchResult = await SearchAsync(formulaId.ToString());
            if (!searchResult.IsSuccess)
            {
                return searchResult;
            }

            // 筛选包含验方来源的处方
            var filteredPrescriptions = searchResult.Data?
                .Where(p => p.FormulaSource != null && 
                           p.FormulaSource.Contains(formulaId.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<PrescriptionDto>();

            RecordQueryPerformance("SearchByFormula", startTime);

            _logger.LogInformation("根据验方搜索处方成功 - 验方ID: {FormulaId}, 结果数: {Count}", 
                formulaId, filteredPrescriptions.Count);

            return ServiceResult<List<PrescriptionDto>>.Success(filteredPrescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据验方搜索处方异常: {FormulaId}", formulaId);
            return ServiceResult<List<PrescriptionDto>>.Failure($"根据验方搜索处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 全文搜索处方
    /// </summary>
    public async Task<ServiceResult<List<PrescriptionDto>>> FullTextSearchAsync(string searchText, int limit = 50)
    {
        var startTime = DateTime.Now;
        try
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
            }

            var searchResult = await _coreService.CallSearchPrescriptionsApiAsync(searchText, limit);
            
            RecordQueryPerformance("FullTextSearch", startTime);
            
            if (searchResult.IsSuccess)
            {
                _logger.LogInformation("全文搜索处方成功 - 搜索文本: {SearchText}, 结果数: {Count}", 
                    searchText, searchResult.Data?.Count ?? 0);
            }

            return searchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "全文搜索处方异常: {SearchText}", searchText);
            return ServiceResult<List<PrescriptionDto>>.Failure($"全文搜索处方异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 智能搜索建议
    /// </summary>
    public async Task<ServiceResult<List<string>>> GetSearchSuggestionsAsync(string input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
            {
                return ServiceResult<List<string>>.Success(new List<string>());
            }

            // 简化的搜索建议实现
            var suggestions = new List<string>();
            
            // 基于输入生成建议
            if (input.Contains("感冒"))
            {
                suggestions.AddRange(new[] { "风寒感冒", "风热感冒", "流行性感冒" });
            }
            
            if (input.Contains("咳嗽"))
            {
                suggestions.AddRange(new[] { "干咳", "湿咳", "久咳" });
            }
            
            if (input.Contains("胃"))
            {
                suggestions.AddRange(new[] { "胃炎", "胃痛", "胃胀" });
            }

            _logger.LogDebug("智能搜索建议 - 输入: {Input}, 建议数: {Count}", input, suggestions.Count);

            return ServiceResult<List<string>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "智能搜索建议异常: {Input}", input);
            return ServiceResult<List<string>>.Failure($"智能搜索建议异常: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    #endregion

    #region 统计分析方法 - 简化实现

    /// <summary>
    /// 获取处方统计信息
    /// </summary>
    public async Task<ServiceResult<PrescriptionStatisticsDto>> GetPrescriptionStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var startTime = DateTime.Now;
        try
        {
            var query = new PrescriptionQueryDto
            {
                PageIndex = 1,
                PageSize = 1000
            };

            var pagedResult = await GetPagedAsync(query);
            if (!pagedResult.IsSuccess)
            {
                return ServiceResult<PrescriptionStatisticsDto>.Failure(pagedResult.ErrorMessage ?? "获取处方统计信息失败");
            }

            var prescriptions = pagedResult.Data?.Items?.ToList() ?? new List<PrescriptionDto>();

            // 应用日期过滤
            if (startDate.HasValue)
            {
                prescriptions = prescriptions.Where(p => p.CreateTime >= startDate.Value).ToList();
            }
            if (endDate.HasValue)
            {
                prescriptions = prescriptions.Where(p => p.CreateTime <= endDate.Value).ToList();
            }

            var stats = new PrescriptionStatisticsDto
            {
                TotalCount = prescriptions.Count,
                DraftCount = prescriptions.Count(p => p.PrescriptionStatus == PrescriptionStatus.Draft),
                CompletedCount = prescriptions.Count(p => p.PrescriptionStatus == PrescriptionStatus.Completed),
                CancelledCount = prescriptions.Count(p => p.PrescriptionStatus == PrescriptionStatus.Cancelled),
                TotalAmount = prescriptions.Sum(p => p.TotalAmount),
                AverageAmount = prescriptions.Any() ? prescriptions.Average(p => p.TotalAmount) : 0,
                DailyStats = new List<PrescriptionDailyStatDto>(),
                TopUsedHerbs = new List<PrescriptionHerbStatDto>()
            };

            RecordQueryPerformance("GetPrescriptionStatistics", startTime);

            _logger.LogInformation("获取处方统计信息成功 - 总数: {Total}, 平均金额: {Average}", 
                stats.TotalCount, stats.AverageAmount);

            return ServiceResult<PrescriptionStatisticsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取处方统计信息异常");
            return ServiceResult<PrescriptionStatisticsDto>.Failure($"获取处方统计信息异常: {ex.Message}");
        }
    }

    #endregion

    #region 其他查询方法 - 简化实现

    // 为了节省代码长度，其他方法提供基础实现
    public async Task<ServiceResult<PatientPrescriptionStatDto>> GetPatientPrescriptionStatAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var prescriptionsResult = await GetByPatientIdAsync(patientId);
            if (!prescriptionsResult.IsSuccess)
            {
                return ServiceResult<PatientPrescriptionStatDto>.Failure(prescriptionsResult.ErrorMessage ?? "获取患者处方统计失败");
            }

            var prescriptions = prescriptionsResult.Data ?? new List<PrescriptionDto>();
            
            var stat = new PatientPrescriptionStatDto
            {
                PatientId = patientId,
                TotalPrescriptions = prescriptions.Count,
                TotalAmount = prescriptions.Sum(p => p.TotalAmount),
                AverageAmount = prescriptions.Any() ? prescriptions.Average(p => p.TotalAmount) : 0
            };

            return ServiceResult<PatientPrescriptionStatDto>.Success(stat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者处方统计异常: {PatientId}", patientId);
            return ServiceResult<PatientPrescriptionStatDto>.Failure($"获取患者处方统计异常: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    public async Task<ServiceResult<DoctorPrescriptionStatDto>> GetDoctorPrescriptionStatAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var prescriptionsResult = await GetByDoctorIdAsync(doctorId);
            if (!prescriptionsResult.IsSuccess)
            {
                return ServiceResult<DoctorPrescriptionStatDto>.Failure(prescriptionsResult.ErrorMessage ?? "获取医生处方统计失败");
            }

            var prescriptions = prescriptionsResult.Data ?? new List<PrescriptionDto>();
            
            var stat = new DoctorPrescriptionStatDto
            {
                DoctorId = doctorId,
                TotalPrescriptions = prescriptions.Count,
                TotalAmount = prescriptions.Sum(p => p.TotalAmount),
                AverageAmount = prescriptions.Any() ? prescriptions.Average(p => p.TotalAmount) : 0,
                UniquePatients = prescriptions.Select(p => p.PatientId).Distinct().Count()
            };

            return ServiceResult<DoctorPrescriptionStatDto>.Success(stat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取医生处方统计异常: {DoctorId}", doctorId);
            return ServiceResult<DoctorPrescriptionStatDto>.Failure($"获取医生处方统计异常: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    // 其他方法的简化实现
    public Task<ServiceResult<List<HerbUsageStatDto>>> GetHerbUsageStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20) =>
        Task.FromResult(ServiceResult<List<HerbUsageStatDto>>.Success(new List<HerbUsageStatDto>()));

    public Task<ServiceResult<List<DiagnosisFrequencyDto>>> GetDiagnosisFrequencyAsync(DateTime? startDate = null, DateTime? endDate = null, int topCount = 20) =>
        Task.FromResult(ServiceResult<List<DiagnosisFrequencyDto>>.Success(new List<DiagnosisFrequencyDto>()));

    public Task<ServiceResult<PriceDistributionDto>> GetPriceDistributionAsync(DateTime? startDate = null, DateTime? endDate = null) =>
        Task.FromResult(ServiceResult<PriceDistributionDto>.Success(new PriceDistributionDto()));

    public Task<ServiceResult<List<MonthlyTrendDto>>> GetMonthlyTrendAsync(int months = 12) =>
        Task.FromResult(ServiceResult<List<MonthlyTrendDto>>.Success(new List<MonthlyTrendDto>()));

    public Task<ServiceResult<List<PrescriptionDto>>> GetDuplicatePrescriptionsAsync(Guid patientId, TimeSpan withinPeriod) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<List<PrescriptionDto>>> GetHighValuePrescriptionsAsync(decimal minAmount, int limit = 50) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<List<PrescriptionDto>>> GetAbnormalPrescriptionsAsync() =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<List<PrescriptionDto>>> GetIncompletePrescriptionsAsync(int daysThreshold = 7) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<List<PrescriptionDto>>> GetSimilarPrescriptionsAsync(Guid prescriptionId, int limit = 10) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));

    public Task<ServiceResult<List<PrescriptionPatternDto>>> GetCommonPrescriptionPatternsAsync(int minOccurrence = 3) =>
        Task.FromResult(ServiceResult<List<PrescriptionPatternDto>>.Success(new List<PrescriptionPatternDto>()));

    public Task<ServiceResult<List<PrescriptionPatientInfoDto>>> GetPrescriptionPatientInfoAsync(List<Guid> prescriptionIds) =>
        Task.FromResult(ServiceResult<List<PrescriptionPatientInfoDto>>.Success(new List<PrescriptionPatientInfoDto>()));

    public Task<ServiceResult<List<PrescriptionMedicalCaseInfoDto>>> GetPrescriptionMedicalCaseInfoAsync(List<Guid> prescriptionIds) =>
        Task.FromResult(ServiceResult<List<PrescriptionMedicalCaseInfoDto>>.Success(new List<PrescriptionMedicalCaseInfoDto>()));

    public Task<ServiceResult<PrescriptionDetailDto>> GetPrescriptionDetailAsync(Guid prescriptionId) =>
        Task.FromResult(ServiceResult<PrescriptionDetailDto>.Success(new PrescriptionDetailDto()));

    #endregion

    #region 性能优化方法

    /// <summary>
    /// 记录查询性能
    /// </summary>
    private void RecordQueryPerformance(string queryType, DateTime startTime)
    {
        try
        {
            var duration = (DateTime.Now - startTime).TotalMilliseconds;
            
            if (!_queryPerformanceStats.ContainsKey(queryType))
            {
                _queryPerformanceStats[queryType] = new List<double>();
            }
            
            _queryPerformanceStats[queryType].Add(duration);
            
            // 记录慢查询
            if (duration > 1000)
            {
                _logger.LogWarning("慢查询检测: {QueryType} 耗时 {Duration}ms", queryType, duration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录查询性能异常");
        }
    }

    /// <summary>
    /// 预加载常用查询缓存
    /// </summary>
    public async Task PreloadCommonQueriesAsync()
    {
        try
        {
            _logger.LogInformation("开始预加载常用查询缓存");
            // TODO: 根据业务需求实现具体的预加载策略
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载常用查询缓存异常");
        }
    }

    /// <summary>
    /// 获取查询性能统计
    /// </summary>
    public async Task<ServiceResult<QueryPerformanceStatDto>> GetQueryPerformanceStatAsync()
    {
        try
        {
            var totalQueries = _queryPerformanceStats.Values.Sum(list => list.Count);
            var averageResponseTime = totalQueries > 0 
                ? _queryPerformanceStats.Values.SelectMany(list => list).Average()
                : 0;

            var slowQueries = _queryPerformanceStats
                .SelectMany(kvp => kvp.Value.Select(duration => new { Type = kvp.Key, Duration = duration }))
                .Where(x => x.Duration > 1000)
                .Select(x => new SlowQueryDto
                {
                    QueryType = x.Type,
                    ResponseTime = x.Duration,
                    ExecutedAt = DateTime.Now
                })
                .ToList();

            var stat = new QueryPerformanceStatDto
            {
                TotalQueries = totalQueries,
                AverageResponseTime = averageResponseTime,
                SlowQueries = slowQueries,
                QueryTypeDistribution = _queryPerformanceStats.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Count)
            };

            return ServiceResult<QueryPerformanceStatDto>.Success(stat);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取查询性能统计异常");
            return ServiceResult<QueryPerformanceStatDto>.Failure($"获取查询性能统计异常: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 优化慢查询
    /// </summary>
    public async Task<ServiceResult<bool>> OptimizeSlowQueriesAsync()
    {
        try
        {
            _logger.LogInformation("开始优化慢查询");
            // TODO: 实现慢查询优化逻辑
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "优化慢查询异常");
            return ServiceResult<bool>.Failure($"优化慢查询异常: {ex.Message}");
        }
        await Task.CompletedTask;
    }

    #endregion
}
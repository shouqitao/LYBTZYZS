using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Interfaces.Api;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者核心服务 - UltraThink三层架构核心操作层
/// 职责：API通信、基础CRUD操作、数据验证、缓存管理
/// </summary>
public partial class PatientCoreService : IPatientCoreService
{
    private readonly IPatientApi _patientApi;
    private readonly ILogger<PatientCoreService> _logger;
    private readonly IMemoryCache _cache;
    
    private const string PATIENT_CACHE_PREFIX = "patient_";
    private const string ALL_PATIENTS_CACHE_KEY = "all_patients";
    private readonly TimeSpan _defaultCacheExpiry = TimeSpan.FromMinutes(10);

    public PatientCoreService(
        IPatientApi patientApi,
        ILogger<PatientCoreService> logger,
        IMemoryCache cache)
    {
        _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    #region API通信操作

    /// <summary>
    /// 调用创建患者API
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CallCreatePatientApiAsync(PatientCreateDto createDto)
    {
        try
        {
            _logger.LogInformation("正在调用创建患者API，患者姓名：{Name}", createDto.Name);
            
            var apiResponse = await _patientApi.CreatePatientAsync(createDto);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("患者创建成功，ID：{PatientId}", apiResponse.Content.Id);
                
                // 清除相关缓存
                ClearPatientCache();
                
                return ServiceResult<PatientDto>.Success(
                    apiResponse.Content,
                    "患者创建成功"
                );
            }
            
            _logger.LogWarning("创建患者API调用失败");
            return ServiceResult<PatientDto>.Failure("创建患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用创建患者API时发生异常，患者姓名：{Name}", createDto.Name);
            return ServiceResult<PatientDto>.Failure($"创建患者时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用更新患者API
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CallUpdatePatientApiAsync(Guid id, PatientUpdateDto updateDto)
    {
        try
        {
            _logger.LogInformation("正在调用更新患者API，患者ID：{PatientId}", id);
            
            var apiResponse = await _patientApi.UpdatePatientAsync(id, updateDto);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("患者更新成功，ID：{PatientId}", id);
                
                // 更新缓存
                var cacheKey = $"{PATIENT_CACHE_PREFIX}{id}";
                _cache.Remove(cacheKey);
                _cache.Remove(ALL_PATIENTS_CACHE_KEY);
                
                return ServiceResult<PatientDto>.Success(
                    apiResponse.Content,
                    "患者更新成功"
                );
            }
            
            _logger.LogWarning("更新患者API调用失败");
            return ServiceResult<PatientDto>.Failure("更新患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用更新患者API时发生异常，患者ID：{PatientId}", id);
            return ServiceResult<PatientDto>.Failure($"更新患者时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用删除患者API
    /// </summary>
    public async Task<ServiceResult<bool>> CallDeletePatientApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("正在调用删除患者API，患者ID：{PatientId}", id);
            
            var apiResponse = await _patientApi.DeletePatientAsync(id);
            
            if (apiResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("患者删除成功，ID：{PatientId}", id);
                
                // 清除相关缓存
                var cacheKey = $"{PATIENT_CACHE_PREFIX}{id}";
                _cache.Remove(cacheKey);
                _cache.Remove(ALL_PATIENTS_CACHE_KEY);
                
                return ServiceResult<bool>.Success(true, "患者删除成功");
            }
            
            _logger.LogWarning("删除患者API调用失败");
            return ServiceResult<bool>.Failure("删除患者失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用删除患者API时发生异常，患者ID：{PatientId}", id);
            return ServiceResult<bool>.Failure($"删除患者时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取患者详情API
    /// </summary>
    public async Task<ServiceResult<PatientDto>> CallGetPatientByIdApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("正在调用获取患者详情API，患者ID：{PatientId}", id);
            
            var apiResponse = await _patientApi.GetPatientByIdAsync(id);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("获取患者详情成功，ID：{PatientId}", id);
                
                return ServiceResult<PatientDto>.Success(
                    apiResponse.Content,
                    "获取患者详情成功"
                );
            }
            
            _logger.LogWarning("获取患者详情API调用失败");
            return ServiceResult<PatientDto>.Failure("获取患者详情失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取患者详情API时发生异常，患者ID：{PatientId}", id);
            return ServiceResult<PatientDto>.Failure($"获取患者详情时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取患者列表API
    /// </summary>
    public async Task<ServiceResult<PagedResult<PatientDto>>> CallGetPatientsApiAsync(int page, int pageSize, string? keyword = null)
    {
        try
        {
            _logger.LogInformation("正在调用获取患者列表API，页码：{Page}，页大小：{PageSize}，关键词：{Keyword}", 
                page, pageSize, keyword);
            
            var apiResponse = await _patientApi.GetPatientsAsync(page, pageSize, keyword);
            
            if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
            {
                _logger.LogInformation("获取患者列表成功，返回 {Count} 条记录", apiResponse.Content.Items?.Count ?? 0);
                
                // 构造标准的PagedResult
                var result = new PagedResult<PatientDto>(
                    apiResponse.Content.Items?.ToList() ?? new List<PatientDto>(),
                    apiResponse.Content.TotalCount,
                    apiResponse.Content.CurrentPage,
                    apiResponse.Content.PageSize);
                
                return ServiceResult<PagedResult<PatientDto>>.Success(
                    result,
                    "获取患者列表成功"
                );
            }
            
            _logger.LogWarning("获取患者列表API调用失败");
            return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取患者列表API时发生异常");
            return ServiceResult<PagedResult<PatientDto>>.Failure($"获取患者列表时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用切换患者状态API
    /// </summary>
    public async Task<ServiceResult<bool>> CallTogglePatientStatusApiAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("正在调用切换患者状态API，患者ID：{PatientId}", id);
            
            // TODO: 实现患者状态切换API调用
            // 目前PatientApi可能没有这个方法，需要后端支持
            await Task.Delay(10);
            
            // 清除相关缓存
            var cacheKey = $"{PATIENT_CACHE_PREFIX}{id}";
            _cache.Remove(cacheKey);
            _cache.Remove(ALL_PATIENTS_CACHE_KEY);
            
            return ServiceResult<bool>.Success(true, "切换患者状态成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用切换患者状态API时发生异常，患者ID：{PatientId}", id);
            return ServiceResult<bool>.Failure($"切换患者状态时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 基础数据操作

    /// <summary>
    /// 获取患者信息（带缓存）
    /// </summary>
    public async Task<ServiceResult<PatientDto>> GetPatientByIdAsync(Guid id)
    {
        var cacheKey = $"{PATIENT_CACHE_PREFIX}{id}";
        
        if (_cache.TryGetValue(cacheKey, out PatientDto? cachedPatient) && cachedPatient != null)
        {
            _logger.LogDebug("从缓存获取患者信息，ID：{PatientId}", id);
            return ServiceResult<PatientDto>.Success(cachedPatient, "从缓存获取患者信息成功");
        }

        var result = await CallGetPatientByIdApiAsync(id);
        
        if (result.IsSuccess && result.Data != null)
        {
            // 缓存患者数据
            _cache.Set(cacheKey, result.Data, _defaultCacheExpiry);
            _logger.LogDebug("已缓存患者信息，ID：{PatientId}", id);
        }

        return result;
    }

    /// <summary>
    /// 获取所有患者（带缓存）
    /// </summary>
    public async Task<ServiceResult<List<PatientDto>>> GetAllPatientsAsync()
    {
        if (_cache.TryGetValue(ALL_PATIENTS_CACHE_KEY, out List<PatientDto>? cachedPatients) && cachedPatients != null)
        {
            _logger.LogDebug("从缓存获取所有患者信息");
            return ServiceResult<List<PatientDto>>.Success(cachedPatients, "从缓存获取患者列表成功");
        }

        // 获取第一页的较大数据量来模拟获取所有患者
        var result = await CallGetPatientsApiAsync(1, 1000);
        
        if (result.IsSuccess && result.Data?.Items != null)
        {
            var allPatients = result.Data.Items.ToList();
            
            // 缓存患者列表
            _cache.Set(ALL_PATIENTS_CACHE_KEY, allPatients, _defaultCacheExpiry);
            _logger.LogDebug("已缓存患者列表，总计 {Count} 个患者", allPatients.Count);
            
            return ServiceResult<List<PatientDto>>.Success(allPatients, "获取患者列表成功");
        }

        return ServiceResult<List<PatientDto>>.Failure(result.ErrorMessage ?? "获取患者列表失败");
    }

    /// <summary>
    /// 验证患者是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> ValidatePatientExistsAsync(Guid id)
    {
        try
        {
            var result = await GetPatientByIdAsync(id);
            return ServiceResult<bool>.Success(result.IsSuccess, result.IsSuccess ? "患者存在" : "患者不存在");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证患者是否存在时发生异常，患者ID：{PatientId}", id);
            return ServiceResult<bool>.Failure($"验证患者是否存在时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查手机号是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckPhoneExistsAsync(string phone, Guid? excludeId = null)
    {
        try
        {
            var allPatientsResult = await GetAllPatientsAsync();
            
            if (!allPatientsResult.IsSuccess || allPatientsResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取患者列表失败，无法检查手机号重复性");
            }

            var exists = allPatientsResult.Data.Any(p => 
                string.Equals(p.Phone, phone, StringComparison.OrdinalIgnoreCase) && 
                p.Id != excludeId);
            
            return ServiceResult<bool>.Success(exists, exists ? "手机号已存在" : "手机号可用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查手机号是否存在时发生异常，手机号：{Phone}", phone);
            return ServiceResult<bool>.Failure($"检查手机号是否存在时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查身份证号是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckIdCardExistsAsync(string idCard, Guid? excludeId = null)
    {
        try
        {
            var allPatientsResult = await GetAllPatientsAsync();
            
            if (!allPatientsResult.IsSuccess || allPatientsResult.Data == null)
            {
                return ServiceResult<bool>.Failure("获取患者列表失败，无法检查身份证号重复性");
            }

            var exists = allPatientsResult.Data.Any(p => 
                string.Equals(p.IdCard, idCard, StringComparison.OrdinalIgnoreCase) && 
                p.Id != excludeId);
            
            return ServiceResult<bool>.Success(exists, exists ? "身份证号已存在" : "身份证号可用");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查身份证号是否存在时发生异常，身份证号：{IdCard}", idCard);
            return ServiceResult<bool>.Failure($"检查身份证号是否存在时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 数据验证操作

    /// <summary>
    /// 验证患者创建数据
    /// </summary>
    public ServiceResult ValidatePatientCreateData(PatientCreateDto createDto)
    {
        if (createDto == null)
        {
            return ServiceResult.Failure("患者创建数据不能为空");
        }

        var validationResults = new List<string>();

        // 验证患者姓名
        var nameValidation = ValidatePatientName(createDto.Name);
        if (!nameValidation.IsSuccess)
        {
            validationResults.Add(nameValidation.ErrorMessage);
        }

        // 验证手机号
        var phoneValidation = ValidatePhone(createDto.Phone);
        if (!phoneValidation.IsSuccess)
        {
            validationResults.Add(phoneValidation.ErrorMessage);
        }

        // 验证身份证号（如果提供）
        if (!string.IsNullOrEmpty(createDto.IdCard))
        {
            var idCardValidation = ValidateIdCard(createDto.IdCard);
            if (!idCardValidation.IsSuccess)
            {
                validationResults.Add(idCardValidation.ErrorMessage);
            }
        }

        if (validationResults.Any())
        {
            return ServiceResult.Failure($"患者创建数据验证失败：{string.Join("; ", validationResults)}");
        }

        return ServiceResult.Success("患者创建数据验证通过");
    }

    /// <summary>
    /// 验证患者更新数据
    /// </summary>
    public ServiceResult ValidatePatientUpdateData(PatientUpdateDto updateDto)
    {
        if (updateDto == null)
        {
            return ServiceResult.Failure("患者更新数据不能为空");
        }

        var validationResults = new List<string>();

        // 验证基础信息（如果提供）
        if (!string.IsNullOrEmpty(updateDto.Name))
        {
            var nameValidation = ValidatePatientName(updateDto.Name);
            if (!nameValidation.IsSuccess)
            {
                validationResults.Add(nameValidation.ErrorMessage);
            }
        }

        if (!string.IsNullOrEmpty(updateDto.Phone))
        {
            var phoneValidation = ValidatePhone(updateDto.Phone);
            if (!phoneValidation.IsSuccess)
            {
                validationResults.Add(phoneValidation.ErrorMessage);
            }
        }

        if (!string.IsNullOrEmpty(updateDto.IdCard))
        {
            var idCardValidation = ValidateIdCard(updateDto.IdCard);
            if (!idCardValidation.IsSuccess)
            {
                validationResults.Add(idCardValidation.ErrorMessage);
            }
        }

        if (validationResults.Any())
        {
            return ServiceResult.Failure($"患者更新数据验证失败：{string.Join("; ", validationResults)}");
        }

        return ServiceResult.Success("患者更新数据验证通过");
    }

    /// <summary>
    /// 验证患者姓名格式
    /// </summary>
    public ServiceResult ValidatePatientName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult.Failure("患者姓名不能为空");
        }

        if (name.Length < 2)
        {
            return ServiceResult.Failure("患者姓名长度不能少于2个字符");
        }

        if (name.Length > 50)
        {
            return ServiceResult.Failure("患者姓名长度不能超过50个字符");
        }

        // 只允许中文、英文字母
        if (!PatientNameRegex().IsMatch(name))
        {
            return ServiceResult.Failure("患者姓名只能包含中文和英文字符");
        }

        return ServiceResult.Success("患者姓名格式验证通过");
    }

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    public ServiceResult ValidatePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return ServiceResult.Failure("手机号不能为空");
        }

        if (!PhoneRegex().IsMatch(phone))
        {
            return ServiceResult.Failure("手机号格式不正确");
        }

        return ServiceResult.Success("手机号格式验证通过");
    }

    /// <summary>
    /// 验证身份证号格式
    /// </summary>
    public ServiceResult ValidateIdCard(string? idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard))
        {
            return ServiceResult.Success("身份证号为空，跳过验证");
        }

        if (!IdCardRegex().IsMatch(idCard))
        {
            return ServiceResult.Failure("身份证号格式不正确");
        }

        return ServiceResult.Success("身份证号格式验证通过");
    }

    /// <summary>
    /// 验证患者基础信息
    /// </summary>
    public ServiceResult ValidatePatientBasicInfo(string? name, string? phone, string? idCard)
    {
        var validationResults = new List<string>();

        // 验证患者姓名
        if (!string.IsNullOrEmpty(name))
        {
            var nameValidation = ValidatePatientName(name);
            if (!nameValidation.IsSuccess)
            {
                validationResults.Add(nameValidation.ErrorMessage);
            }
        }

        // 验证手机号
        if (!string.IsNullOrEmpty(phone))
        {
            var phoneValidation = ValidatePhone(phone);
            if (!phoneValidation.IsSuccess)
            {
                validationResults.Add(phoneValidation.ErrorMessage);
            }
        }

        // 验证身份证号
        if (!string.IsNullOrEmpty(idCard))
        {
            var idCardValidation = ValidateIdCard(idCard);
            if (!idCardValidation.IsSuccess)
            {
                validationResults.Add(idCardValidation.ErrorMessage);
            }
        }

        if (validationResults.Any())
        {
            return ServiceResult.Failure($"患者基础信息验证失败：{string.Join("; ", validationResults)}");
        }

        return ServiceResult.Success("患者基础信息验证通过");
    }

    #endregion

    #region 患者状态管理

    /// <summary>
    /// 更新患者状态
    /// </summary>
    public void UpdatePatientStatus(Guid patientId, bool isEnabled)
    {
        try
        {
            _logger.LogInformation("更新患者状态，患者ID：{PatientId}，状态：{Status}", patientId, isEnabled);
            
            // 清除相关缓存
            var cacheKey = $"{PATIENT_CACHE_PREFIX}{patientId}";
            _cache.Remove(cacheKey);
            _cache.Remove(ALL_PATIENTS_CACHE_KEY);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新患者状态时发生异常，患者ID：{PatientId}", patientId);
        }
    }

    /// <summary>
    /// 批量更新患者状态
    /// </summary>
    public async Task<ServiceResult<int>> BatchUpdatePatientStatusAsync(List<Guid> patientIds, bool isEnabled)
    {
        try
        {
            _logger.LogInformation("批量更新患者状态，患者数量：{Count}，状态：{Status}", patientIds.Count, isEnabled);
            
            int successCount = 0;
            
            foreach (var patientId in patientIds)
            {
                try
                {
                    var result = await CallTogglePatientStatusApiAsync(patientId);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量更新患者状态时处理患者失败，患者ID：{PatientId}", patientId);
                }
            }

            // 清除相关缓存
            ClearPatientCache();

            return ServiceResult<int>.Success(successCount, $"成功更新 {successCount} 个患者状态");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新患者状态时发生异常");
            return ServiceResult<int>.Failure($"批量更新患者状态时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取患者状态信息
    /// </summary>
    public ServiceResult<PatientStatusInfo> GetPatientStatusInfo(Guid patientId)
    {
        try
        {
            // 这里应该从缓存或API获取患者状态信息
            // 为演示目的，返回一个默认状态
            var statusInfo = new PatientStatusInfo
            {
                IsEnabled = true,
                LastVisitTime = DateTime.Now.AddDays(-7),
                LastUpdateTime = DateTime.Now.AddHours(-2),
                VisitCount = 5,
                StatusDescription = "正常"
            };

            return ServiceResult<PatientStatusInfo>.Success(statusInfo, "获取患者状态信息成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取患者状态信息时发生异常，患者ID：{PatientId}", patientId);
            return ServiceResult<PatientStatusInfo>.Failure($"获取患者状态信息时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 缓存和性能优化

    /// <summary>
    /// 预加载常用患者数据
    /// </summary>
    public async Task<ServiceResult> PreloadCommonPatientsAsync()
    {
        try
        {
            _logger.LogInformation("预加载常用患者数据");
            
            // 预加载所有患者
            var allPatientsResult = await GetAllPatientsAsync();
            
            if (allPatientsResult.IsSuccess && allPatientsResult.Data != null)
            {
                // 预加载最近就诊的患者详细信息
                var recentPatients = allPatientsResult.Data
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.UpdateTime)
                    .Take(20);
                
                foreach (var patient in recentPatients)
                {
                    await GetPatientByIdAsync(patient.Id);
                }
                
                return ServiceResult.Success("预加载常用患者数据完成");
            }
            
            return ServiceResult.Failure("预加载患者数据失败：无法获取患者列表");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载常用患者数据时发生异常");
            return ServiceResult.Failure($"预加载患者数据时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 清除患者缓存
    /// </summary>
    public ServiceResult ClearPatientCache()
    {
        try
        {
            _logger.LogInformation("清除患者缓存");
            
            _cache.Remove(ALL_PATIENTS_CACHE_KEY);
            
            return ServiceResult.Success("患者缓存清除完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除患者缓存时发生异常");
            return ServiceResult.Failure($"清除患者缓存时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取缓存的患者数据
    /// </summary>
    public ServiceResult<List<PatientDto>> GetCachedPatients()
    {
        try
        {
            if (_cache.TryGetValue(ALL_PATIENTS_CACHE_KEY, out List<PatientDto>? cachedPatients) && cachedPatients != null)
            {
                return ServiceResult<List<PatientDto>>.Success(cachedPatients, "获取缓存患者数据成功");
            }
            
            return ServiceResult<List<PatientDto>>.Failure("缓存中没有患者数据");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存患者数据时发生异常");
            return ServiceResult<List<PatientDto>>.Failure($"获取缓存患者数据时发生异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 刷新患者缓存
    /// </summary>
    public async Task<ServiceResult> RefreshPatientCacheAsync(Guid patientId)
    {
        try
        {
            _logger.LogInformation("刷新患者缓存，患者ID：{PatientId}", patientId);
            
            var cacheKey = $"{PATIENT_CACHE_PREFIX}{patientId}";
            _cache.Remove(cacheKey);
            
            // 重新获取患者数据并缓存
            var result = await CallGetPatientByIdApiAsync(patientId);
            
            if (result.IsSuccess && result.Data != null)
            {
                _cache.Set(cacheKey, result.Data, _defaultCacheExpiry);
                return ServiceResult.Success("患者缓存刷新完成");
            }
            
            return ServiceResult.Failure("刷新患者缓存失败：无法获取患者数据");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新患者缓存时发生异常，患者ID：{PatientId}", patientId);
            return ServiceResult.Failure($"刷新患者缓存时发生异常：{ex.Message}");
        }
    }

    #endregion

    #region 正则表达式

    [GeneratedRegex(@"^[\u4e00-\u9fa5a-zA-Z\s]+$")]
    private static partial Regex PatientNameRegex();

    [GeneratedRegex(@"^1[3-9]\d{9}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[1-9]\d{5}(18|19|20)\d{2}(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{3}[\dXx]$")]
    private static partial Regex IdCardRegex();

    #endregion
}
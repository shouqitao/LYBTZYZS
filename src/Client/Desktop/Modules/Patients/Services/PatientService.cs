using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services;

/// <summary>
/// 患者服务 - 重构后的统一实现
/// 合并原QueryService和BusinessService的所有功能
/// </summary>
public class PatientService(
    ILogger<PatientService> logger,
    IPatientApi patientApi,
    IExceptionHandler exceptionHandler) : IPatientService
{
    private readonly ILogger<PatientService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPatientApi _patientApi = patientApi ?? throw new ArgumentNullException(nameof(patientApi));
    private readonly IExceptionHandler _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

    #region Query Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientSearchDto query)
    {
        return await _exceptionHandler.HandleException<PagedResult<PatientDto>>(
            async (ct) =>
            {
                _logger.LogDebug("执行患者分页查询，页码: {PageNumber}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

                var refitResponse = await _patientApi.GetPatientsAsync(query.PageIndex, query.PageSize, query.Keyword).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    return ServiceResult<PagedResult<PatientDto>>.Success(refitResponse.Content);
                }

                _logger.LogWarning("患者分页查询HTTP请求失败, 状态码: {StatusCode}", refitResponse.StatusCode);
                return ServiceResult<PagedResult<PatientDto>>.Failure("查询患者列表失败，请检查网络连接");
            },
            nameof(GetPagedAsync), "患者分页查询", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        return await _exceptionHandler.HandleException<PatientDto>(
            async (ct) =>
            {
                _logger.LogDebug("查询患者详细档案: {PatientId}", id);

                var refitResponse = await _patientApi.GetPatientByIdAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    return ServiceResult<PatientDto>.Success(refitResponse.Content);
                }

                _logger.LogWarning("查询患者详情HTTP请求失败: {PatientId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<PatientDto>.Failure("查询患者详情失败，请检查网络连接");
            },
            nameof(GetByIdAsync), $"查询患者: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
    {
        return await _exceptionHandler.HandleException<List<PatientDto>>(
            async (ct) =>
            {
                _logger.LogDebug("患者关键字搜索: {Keyword}", keyword);

                var refitResponse = await _patientApi.GetPatientsAsync(1, 100, keyword).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    return ServiceResult<List<PatientDto>>.Success(refitResponse.Content.Items);
                }

                _logger.LogWarning("患者搜索HTTP请求失败: {Keyword}, 状态码: {StatusCode}", keyword, refitResponse.StatusCode);
                return ServiceResult<List<PatientDto>>.Failure("患者搜索失败，请检查网络连接");
            },
            nameof(SearchAsync), $"搜索患者: {keyword}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
    {
        try
        {
            _logger.LogDebug("生成患者档案统计数据");
            // 简单版本：基础统计实现
            var stats = new PatientStatisticsDto();
            return ServiceResult<PatientStatisticsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者统计数据生成异常");
            return ServiceResult<PatientStatisticsDto>.Failure("生成统计数据失败");
        }
    }

    #endregion

    #region Business Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        return await _exceptionHandler.HandleException<PatientDto>(
            async (ct) =>
            {
                _logger.LogInformation("开始处理患者档案创建: 姓名: {PatientName}, 联系电话: {Phone}", createDto.Name, createDto.PhoneNumber);

                var refitResponse = await _patientApi.CreatePatientAsync(createDto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var patient = refitResponse.Content;
                    _logger.LogInformation("患者档案创建成功: {PatientName}", patient.Name);
                    return ServiceResult<PatientDto>.Success(patient, "患者档案创建成功");
                }

                _logger.LogWarning("患者档案创建HTTP请求失败: {PatientName}, 状态码: {StatusCode}", createDto.Name, refitResponse.StatusCode);
                return ServiceResult<PatientDto>.Failure("创建患者档案网络请求失败，请检查网络连接");
            },
            nameof(CreateAsync), $"创建患者档案: {createDto.Name}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        return await _exceptionHandler.HandleException<PatientDto>(
            async (ct) =>
            {
                _logger.LogInformation("开始处理患者档案更新: 患者ID: {PatientId}", id);

                var refitResponse = await _patientApi.UpdatePatientAsync(id, updateDto).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
                {
                    var patient = refitResponse.Content;
                    _logger.LogInformation("患者档案更新成功: {PatientName}", patient.Name);
                    return ServiceResult<PatientDto>.Success(patient, "患者档案更新成功");
                }

                _logger.LogWarning("患者档案更新HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult<PatientDto>.Failure("更新患者档案网络请求失败，请检查网络连接");
            },
            nameof(UpdateAsync), $"更新患者档案: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid patientId)
    {
        return await _exceptionHandler.HandleException<bool>(
            async (ct) =>
            {
                _logger.LogInformation("删除患者档案: {PatientId}", patientId);

                var refitResponse = await _patientApi.DeletePatientAsync(patientId).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("患者档案删除成功: {PatientId}", patientId);
                    return ServiceResult<bool>.Success(true, "患者档案删除成功");
                }

                _logger.LogWarning("患者档案删除HTTP请求失败: {PatientId}, 状态码: {StatusCode}", patientId, refitResponse.StatusCode);
                return ServiceResult<bool>.Failure("删除患者档案网络请求失败，请检查网络连接");
            },
            nameof(DeleteAsync), $"删除患者档案: {patientId}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> EnableAsync(Guid id)
    {
        return await _exceptionHandler.HandleException(
            async (ct) =>
            {
                _logger.LogInformation("启用患者档案: {PatientId}", id);

                var refitResponse = await _patientApi.ToggleStatusAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("患者档案启用成功: {PatientId}", id);
                    return ServiceResult.Success("患者档案启用成功");
                }

                _logger.LogWarning("患者档案启用HTTP请求失败: {PatientId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult.Failure("启用患者档案网络请求失败，请检查网络连接");
            },
            nameof(EnableAsync), $"启用患者档案: {id}", CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> DisableAsync(Guid id)
    {
        return await _exceptionHandler.HandleException(
            async (ct) =>
            {
                _logger.LogInformation("禁用患者档案: {PatientId}", id);

                var refitResponse = await _patientApi.ToggleStatusAsync(id).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                if (refitResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("患者档案禁用成功: {PatientId}", id);
                    return ServiceResult.Success("患者档案禁用成功");
                }

                _logger.LogWarning("患者档案禁用HTTP请求失败: {PatientId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
                return ServiceResult.Failure("禁用患者档案网络请求失败，请检查网络连接");
            },
            nameof(DisableAsync), $"禁用患者档案: {id}", CancellationToken.None);
    }

    #endregion

    #region Additional Interface Methods

    /// <summary>
    /// 启用患者（返回详细结果）
    /// </summary>
    public async Task<ServiceResult<bool>> EnablePatientAsync(Guid patientId)
    {
        var result = await EnableAsync(patientId);
        return result.IsSuccess 
            ? ServiceResult<bool>.Success(true, result.Message)
            : ServiceResult<bool>.Failure(result.ErrorMessage ?? "启用患者失败");
    }

    /// <summary>
    /// 禁用患者（返回详细结果）
    /// </summary>
    public async Task<ServiceResult<bool>> DisablePatientAsync(Guid patientId)
    {
        var result = await DisableAsync(patientId);
        return result.IsSuccess 
            ? ServiceResult<bool>.Success(true, result.Message)
            : ServiceResult<bool>.Failure(result.ErrorMessage ?? "禁用患者失败");
    }

    /// <summary>
    /// 删除患者（带操作者信息）
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
    {
        var result = await DeleteAsync(id);
        return result.IsSuccess && result.Data == true;
    }

    /// <summary>
    /// 设置患者状态（启用/禁用）
    /// </summary>
    public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
    {
        var result = isActive ? await EnablePatientAsync(id) : await DisablePatientAsync(id);
        return result.IsSuccess && result.Data == true;
    }

    /// <summary>
    /// 根据身份证号查找患者
    /// </summary>
    public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard))
        {
            return ServiceResult<PatientDto>.Failure("身份证号不能为空");
        }

        var searchResult = await SearchAsync(idCard);
        if (searchResult.IsSuccess && searchResult.Data?.Any() == true)
        {
            return ServiceResult<PatientDto>.Success(searchResult.Data.First(), "根据身份证号查找成功");
        }

        return ServiceResult<PatientDto>.Failure("未找到匹配的患者信息");
    }

    /// <summary>
    /// 根据电话号码查找患者
    /// </summary>
    public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return ServiceResult<List<PatientDto>>.Success([]);
        }
        return await SearchAsync(phone);
    }

    /// <summary>
    /// 获取所有患者列表
    /// </summary>
    public async Task<List<PatientDto>> GetAllAsync()
    {
        var query = new PatientSearchDto { PageIndex = 1, PageSize = 1000 };
        var result = await GetPagedAsync(query);
        return result.IsSuccess ? result.Data?.Items?.ToList() ?? [] : [];
    }

    /// <summary>
    /// 获取可用患者列表
    /// </summary>
    public async Task<List<PatientDto>> GetActivePatientsAsync()
    {
        var allPatients = await GetAllAsync();
        return allPatients.Where(p => p.Status == Shared.Models.Enums.CommonStatus.Enabled).ToList();
    }

    /// <summary>
    /// 根据手机号查找患者
    /// </summary>
    public async Task<PatientDto?> GetByPhoneNumberAsync(string phoneNumber)
    {
        var result = await GetByPhoneAsync(phoneNumber);
        return result.IsSuccess ? result.Data?.FirstOrDefault() : null;
    }

    /// <summary>
    /// 根据身份证号查找患者
    /// </summary>
    public async Task<PatientDto?> GetByIDNumberAsync(string idNumber)
    {
        var result = await GetByIdCardAsync(idNumber);
        return result.IsSuccess ? result.Data : null;
    }

    /// <summary>
    /// 高级搜索患者
    /// </summary>
    public async Task<PagedResult<PatientDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
    {
        // 转换为基础搜索
        var basicQuery = new PatientSearchDto
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Keyword = query.Keyword
        };
        var result = await GetPagedAsync(basicQuery);
        return result.IsSuccess ? result.Data ?? new PagedResult<PatientDto>() : new PagedResult<PatientDto>();
    }

    /// <summary>
    /// 检查重复患者
    /// </summary>
    public async Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
    {
        var duplicates = new List<PatientDto>();
        
        if (!string.IsNullOrWhiteSpace(idNumber))
        {
            var idResult = await GetByIdCardAsync(idNumber);
            if (idResult.IsSuccess && idResult.Data != null)
                duplicates.Add(idResult.Data);
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneResult = await GetByPhoneAsync(phoneNumber);
            if (phoneResult.IsSuccess && phoneResult.Data?.Any() == true)
                duplicates.AddRange(phoneResult.Data);
        }

        return duplicates.DistinctBy(p => p.Id).ToList();
    }

    /// <summary>
    /// 批量导入患者
    /// </summary>
    public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
    {
        if (patients == null || !patients.Any())
        {
            return ServiceResult<object>.Failure("导入的患者列表不能为空");
        }

        try
        {
            var successCount = 0;
            var failedItems = new List<string>();

            foreach (var patient in patients)
            {
                var result = await CreateAsync(patient);
                if (result.IsSuccess)
                {
                    successCount++;
                }
                else
                {
                    failedItems.Add($"{patient.Name}: {result.ErrorMessage}");
                }
            }

            var importResult = new
            {
                SuccessCount = successCount,
                FailedCount = failedItems.Count,
                FailedItems = failedItems
            };

            return successCount > 0
                ? ServiceResult<object>.Success(importResult, $"导入完成，成功: {successCount}, 失败: {failedItems.Count}")
                : ServiceResult<object>.Failure("导入失败，没有成功导入任何患者");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量导入患者异常");
            return ServiceResult<object>.Failure($"批量导入患者失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 导出患者数据
    /// </summary>
    public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
    {
        try
        {
            var allPatientsQuery = new PatientSearchDto
            {
                PageIndex = 1,
                PageSize = 10000,
                Keyword = query.Keyword
            };

            var result = await GetPagedAsync(allPatientsQuery);
            if (!result.IsSuccess || result.Data?.Items == null)
            {
                return ServiceResult<byte[]>.Failure("获取患者数据失败");
            }

            var csvContent = "患者姓名,性别,联系电话,出生日期,状态\n";
            foreach (var patient in result.Data.Items)
            {
                var name = patient.Name ?? string.Empty;
                var gender = patient.Gender == 0 ? "男" : "女";
                var phone = patient.PhoneNumber ?? string.Empty;
                var birthDate = patient.BirthDate?.ToString("yyyy-MM-dd") ?? string.Empty;
                var status = patient.Status == Shared.Models.Enums.CommonStatus.Enabled ? "正常" : "禁用";
                csvContent += $"{name},{gender},{phone},{birthDate},{status}\n";
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
            return ServiceResult<byte[]>.Success(csvBytes, $"患者数据导出完成，共 {result.Data.Items.Count} 条");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出患者数据异常");
            return ServiceResult<byte[]>.Failure($"导出患者数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证患者信息
    /// </summary>
    public Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return Task.FromResult(ServiceResult<object>.Failure("患者姓名不能为空"));
        }

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            return Task.FromResult(ServiceResult<object>.Failure("联系电话不能为空"));
        }

        return Task.FromResult(ServiceResult<object>.Success(new { IsValid = true }));
    }

    /// <summary>
    /// 获取导入模板
    /// </summary>
    public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
    {
        try
        {
            var templateContent = "患者姓名*,性别(男/女)*,联系电话*,出生日期(yyyy-MM-dd),地址,身份证号\n";
            templateContent += "示例患者,男,13800138000,1990-01-01,北京市朝阳区,110101199001011234\n";
            templateContent += "注意：带*的字段为必填项\n";

            var templateBytes = System.Text.Encoding.UTF8.GetBytes(templateContent);
            return ServiceResult<byte[]>.Success(templateBytes, "患者导入模板生成成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成导入模板异常");
            return ServiceResult<byte[]>.Failure($"生成导入模板失败: {ex.Message}");
        }
    }

    #endregion
}
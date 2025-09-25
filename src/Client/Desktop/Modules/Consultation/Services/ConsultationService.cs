using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 诊疗服务 - 重构后的统一实现
/// 合并原QueryService和BusinessService的所有功能
/// </summary>
public class ConsultationService(
    ILogger<ConsultationService> logger,
    IConsultationApi consultationApi) : IConsultationService
{
    private readonly ILogger<ConsultationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IConsultationApi _consultationApi = consultationApi ?? throw new ArgumentNullException(nameof(consultationApi));

    #region Query Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
    {
        try
        {
            _logger.LogDebug("查询诊疗诊断详细档案: {ConsultationId}", id);

            var refitResponse = await _consultationApi.GetByIdAsync(id);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var detail = consultation;

                _logger.LogDebug("诊疗诊断详细档案查询成功: {ConsultationId}", id);
                return ServiceResult<ConsultationDetailDto>.Success(detail, "查询诊断详情成功");
            }

            _logger.LogWarning("诊疗诊断详细档案HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
            return ServiceResult<ConsultationDetailDto>.Failure("查询诊断详情网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询诊断详情异常: {ConsultationId}", id);
            return ServiceResult<ConsultationDetailDto>.Failure($"查询诊断详情失败: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
    {
        try
        {
            _logger.LogDebug("执行诊疗诊断分页查询，页码: {PageNumber}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

            var refitResponse = await _consultationApi.GetConsultationsAsync(
                page: query.PageIndex,
                pageSize: query.PageSize,
                keyword: query.Keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                return ServiceResult<PagedResult<ConsultationDto>>.Success(refitResponse.Content, "查询成功");
            }

            _logger.LogError("诊疗分页查询失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<PagedResult<ConsultationDto>>.Failure($"查询失败: {refitResponse.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗诊断分页查询异常");
            return ServiceResult<PagedResult<ConsultationDto>>.Failure("查询诊疗诊断列表失败");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
    {
        try
        {
            _logger.LogDebug("诊疗诊断关键字搜索: {Keyword}", keyword);

            if (string.IsNullOrWhiteSpace(keyword))
            {
                return ServiceResult<List<ConsultationDto>>.Success([]);
            }

            var refitResponse = await _consultationApi.GetConsultationsAsync(
                page: 1,
                pageSize: 100,
                keyword: keyword);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                _logger.LogDebug("诊断搜索成功: {Keyword}, 结果数: {Count}", keyword, refitResponse.Content.Items.Count);
                return ServiceResult<List<ConsultationDto>>.Success(refitResponse.Content.Items, "搜索成功");
            }

            _logger.LogError("诊断搜索失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<List<ConsultationDto>>.Success([]); // 搜索失败时返回空列表而不是错误
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊断搜索异常");
            return ServiceResult<List<ConsultationDto>>.Failure("诊断搜索失败");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
    {
        try
        {
            _logger.LogDebug("按患者ID查询诊疗记录: {PatientId}", patientId);

            var refitResponse = await _consultationApi.GetPatientHistoryAsync(patientId);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                return ServiceResult<List<ConsultationDto>>.Success(refitResponse.Content, "查询成功");
            }

            _logger.LogError("按患者ID查询诊疗记录失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<List<ConsultationDto>>.Success([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按患者ID查询诊疗记录异常");
            return ServiceResult<List<ConsultationDto>>.Failure("查询患者诊疗记录失败");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogDebug("按医疗案例ID查询诊疗记录: {MedicalCaseId}", medicalCaseId);

            var refitResponse = await _consultationApi.GetByMedicalCaseIdAsync(medicalCaseId);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultationList = new List<ConsultationDto>
                {
                    new ConsultationDto
                    {
                        Id = refitResponse.Content.Id,
                        PatientId = refitResponse.Content.PatientId,
                        MedicalCaseId = refitResponse.Content.MedicalCaseId,
                        CreateTime = refitResponse.Content.CreateTime,
                        UpdateTime = refitResponse.Content.UpdateTime,
                        UserId = refitResponse.Content.UserId
                    }
                };
                return ServiceResult<List<ConsultationDto>>.Success(consultationList, "查询成功");
            }

            _logger.LogError("按医疗案例ID查询诊疗记录失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<List<ConsultationDto>>.Success([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按医疗案例ID查询诊疗记录异常");
            return ServiceResult<List<ConsultationDto>>.Failure("查询医疗案例诊疗记录失败");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
    {
        try
        {
            _logger.LogDebug("按医生ID查询诊疗记录: {DoctorId}", doctorId);

            var refitResponse = await _consultationApi.GetTodayConsultationsByDoctorAsync(doctorId);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                return ServiceResult<List<ConsultationDto>>.Success(refitResponse.Content, "查询成功");
            }

            _logger.LogError("按医生ID查询诊疗记录失败: {ReasonPhrase}", refitResponse.ReasonPhrase);
            return ServiceResult<List<ConsultationDto>>.Success([]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按医生ID查询诊疗记录异常");
            return ServiceResult<List<ConsultationDto>>.Failure("查询医生诊疗记录失败");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
    {
        return await GetByPatientIdAsync(patientId);
    }

    /// <inheritdoc/>
    public Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            _logger.LogDebug("生成诊疗诊断统计数据");
            
            var stats = new ConsultationStatisticsDto
            {
                TotalConsultations = 0,
                CompletedConsultations = 0,
                InProgressConsultations = 0,
                CancelledConsultations = 0
            };

            return Task.FromResult(ServiceResult<object>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊断统计数据生成异常");
            return Task.FromResult(ServiceResult<object>.Failure("生成统计数据失败"));
        }
    }

    #endregion

    #region Business Operations

    /// <inheritdoc/>
    public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        try
        {
            _logger.LogInformation("开始处理诊疗诊断创建: 患者ID: {PatientId}, 医案ID: {MedicalCaseId}", createDto.PatientId, createDto.MedicalCaseId);

            // 转换为StartConsultationDto
            var startDto = new ConsultationStartDto
            {
                PatientId = createDto.PatientId,
                MedicalCaseId = createDto.MedicalCaseId,
                DoctorId = createDto.UserId,
                InitialComplaint = "诊疗记录"
            };

            var refitResponse = await _consultationApi.StartConsultationAsync(startDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    PatientId = consultation.PatientId,
                    MedicalCaseId = consultation.MedicalCaseId,
                    Status = consultation.ConsultationStatus == ConsultationStatus.Completed ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreateTime = consultation.CreateTime,
                    UserId = consultation.UserId
                };
                _logger.LogInformation("诊疗诊断创建成功: {ConsultationId}", consultation.Id);
                return ServiceResult<ConsultationDto>.Success(consultationDto, "诊疗诊断创建成功");
            }

            _logger.LogWarning("诊疗诊断创建HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}", createDto.PatientId, refitResponse.StatusCode);
            return ServiceResult<ConsultationDto>.Failure("创建诊疗诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗诊断创建过程发生异常: 患者ID: {PatientId}", createDto.PatientId);
            return ServiceResult<ConsultationDto>.Failure($"创建诊疗诊断过程发生错误: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        try
        {
            _logger.LogInformation("开始处理诊疗诊断更新: {ConsultationId}", id);

            var refitResponse = await _consultationApi.UpdateConsultationAsync(id, updateDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    PatientId = consultation.PatientId,
                    MedicalCaseId = consultation.MedicalCaseId,
                    Status = consultation.ConsultationStatus == ConsultationStatus.Completed ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreateTime = consultation.CreateTime,
                    UserId = consultation.UserId
                };
                _logger.LogInformation("诊疗诊断更新成功: {ConsultationId}", id);
                return ServiceResult<ConsultationDto>.Success(consultationDto, "诊疗诊断更新成功");
            }

            _logger.LogWarning("诊疗诊断更新HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
            return ServiceResult<ConsultationDto>.Failure("更新诊疗诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗诊断更新过程发生异常: {ConsultationId}", id);
            return ServiceResult<ConsultationDto>.Failure($"更新诊疗诊断过程发生错误: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto updateDto)
    {
        var simpleUpdate = new ConsultationUpdateDto
        {
            Id = updateDto.Id
        };
        return await UpdateAsync(id, simpleUpdate);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        _logger.LogWarning("诊疗诊断删除请求被拒绝: {ConsultationId} - 确保诊疗历史完整性", id);
        return ServiceResult<bool>.Failure("简单诊所版本暂不支持删除诊疗诊断，确保诊疗历史数据完整性");
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto startDto)
    {
        ArgumentNullException.ThrowIfNull(startDto, nameof(startDto));

        try
        {
            _logger.LogInformation("开始处理诊疗开始: 患者ID: {PatientId}, 医案ID: {MedicalCaseId}", startDto.PatientId, startDto.MedicalCaseId);

            var refitResponse = await _consultationApi.StartConsultationAsync(startDto);

            if (refitResponse.IsSuccessStatusCode && refitResponse.Content != null)
            {
                var consultation = refitResponse.Content;
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    PatientId = consultation.PatientId,
                    MedicalCaseId = consultation.MedicalCaseId,
                    Status = consultation.ConsultationStatus == ConsultationStatus.Completed ? CommonStatus.Enabled : CommonStatus.Disabled,
                    CreateTime = consultation.CreateTime,
                    UserId = consultation.UserId
                };

                _logger.LogInformation("诊疗开始成功: {ConsultationId}", consultationDto.Id);
                return ServiceResult<ConsultationDto>.Success(consultationDto, "诊疗开始成功");
            }

            _logger.LogWarning("诊疗开始HTTP请求失败: 患者ID: {PatientId}, 状态码: {StatusCode}", startDto.PatientId, refitResponse.StatusCode);
            return ServiceResult<ConsultationDto>.Failure("开始诊疗网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗开始过程发生异常: 患者ID: {PatientId}", startDto.PatientId);
            return ServiceResult<ConsultationDto>.Failure($"开始诊疗过程发生错误: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> EnableAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("启用诊疗诊断: {ConsultationId}", id);

            var statusDto = new UpdateStatusDto { Status = ConsultationStatus.InProgress };
            var refitResponse = await _consultationApi.UpdateStatusAsync(id, statusDto);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("诊疗诊断启用成功: {ConsultationId}", id);
                return ServiceResult<bool>.Success(true, "诊疗诊断启用成功");
            }

            _logger.LogWarning("诊疗诊断启用HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("启用诊疗诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗诊断启用过程发生异常: {ConsultationId}", id);
            return ServiceResult<bool>.Failure($"启用诊疗诊断过程发生错误: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DisableAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("禁用诊疗诊断: {ConsultationId}", id);

            var refitResponse = await _consultationApi.DeleteAsync(id);

            if (refitResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("诊疗诊断禁用成功: {ConsultationId}", id);
                return ServiceResult<bool>.Success(true, "诊疗诊断禁用成功");
            }

            _logger.LogWarning("诊疗诊断禁用HTTP请求失败: {ConsultationId}, 状态码: {StatusCode}", id, refitResponse.StatusCode);
            return ServiceResult<bool>.Failure("禁用诊疗诊断网络请求失败，请检查网络连接");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊疗诊断禁用过程发生异常: {ConsultationId}", id);
            return ServiceResult<bool>.Failure($"禁用诊疗诊断过程发生错误: {ex.Message}");
        }
    }

    #endregion

    #region Traditional Chinese Medicine Features

    /// <inheritdoc/>
    public Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        try
        {
            _logger.LogDebug("获取医疗案例的中医四诊数据: {MedicalCaseId}", medicalCaseId);
            
            var fourDiagnosis = new
            {
                InspectionData = string.Empty,
                AuscultationData = string.Empty,
                InquiryData = string.Empty,
                PalpationData = string.Empty
            };

            return Task.FromResult(ServiceResult<object>.Success(fourDiagnosis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取中医四诊数据异常");
            return Task.FromResult(ServiceResult<object>.Failure("获取中医四诊数据失败"));
        }
    }

    /// <inheritdoc/>
    public Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
    {
        try
        {
            _logger.LogDebug("保存中医四诊数据: {ConsultationId}", consultationId);
            
            // 简化实现：返回成功但不执行实际保存
            return Task.FromResult(ServiceResult<bool>.Success(false, "简化版本暂不支持保存中医四诊数据"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存中医四诊数据异常");
            return Task.FromResult(ServiceResult<bool>.Failure("保存中医四诊数据失败"));
        }
    }

    #endregion
}
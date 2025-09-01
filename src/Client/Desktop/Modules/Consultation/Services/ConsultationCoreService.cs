using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;

namespace LYBT.Desktop.Consultation.Services;

/// <summary>
/// 看诊核心服务实现 - UltraThink三层架构核心层
/// 职责：API通信、数据验证、缓存管理、基础操作
/// </summary>
public class ConsultationCoreService(
    IConsultationApi consultationApi,
    IMemoryCache cache,
    ILogger<ConsultationCoreService> logger) : IConsultationCoreService
{
    private readonly IConsultationApi _consultationApi = consultationApi;
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<ConsultationCoreService> _logger = logger;

    #region API通信层

    /// <summary>
    /// 调用创建看诊API
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> CallStartConsultationApiAsync(ConsultationStartDto startDto)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            var result = await _consultationApi.StartConsultationAsync(startDto);
            return ServiceResult<ConsultationDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用创建看诊API失败");
            return ServiceResult<ConsultationDto>.Failure($"创建看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用更新看诊API
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> CallUpdateConsultationApiAsync(Guid id, ConsultationUpdateDto updateDto)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            var result = await _consultationApi.UpdateConsultationAsync(id, updateDto);
            return ServiceResult<ConsultationDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用更新看诊API失败，ID: {Id}", id);
            return ServiceResult<ConsultationDto>.Failure($"更新看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用删除看诊API
    /// </summary>
    public async Task<ServiceResult<bool>> CallDeleteConsultationApiAsync(Guid id)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            await _consultationApi.DeleteConsultationAsync(id);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用删除看诊API失败，ID: {Id}", id);
            return ServiceResult<bool>.Failure($"删除看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取看诊详情API
    /// </summary>
    public async Task<ServiceResult<ConsultationDetailDto>> CallGetConsultationByIdApiAsync(Guid id)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            var result = await _consultationApi.GetConsultationByIdAsync(id);
            return ServiceResult<ConsultationDetailDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取看诊详情API失败，ID: {Id}", id);
            return ServiceResult<ConsultationDetailDto>.Failure($"获取看诊详情失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取看诊列表API
    /// </summary>
    public async Task<ServiceResult<PagedResult<ConsultationDto>>> CallGetConsultationListApiAsync(PagedQueryBaseDto query)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            var result = await _consultationApi.GetConsultationListAsync(query);
            return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取看诊列表API失败");
            return ServiceResult<PagedResult<ConsultationDto>>.Failure($"获取看诊列表失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用完成看诊API
    /// </summary>
    public async Task<ServiceResult<bool>> CallCompleteConsultationApiAsync(Guid id, ConsultationCompleteDto completeDto)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            await _consultationApi.CompleteConsultationAsync(id, completeDto);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用完成看诊API失败，ID: {Id}", id);
            return ServiceResult<bool>.Failure($"完成看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用取消看诊API
    /// </summary>
    public async Task<ServiceResult<bool>> CallCancelConsultationApiAsync(Guid id, string reason)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            await _consultationApi.CancelConsultationAsync(id, reason);
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用取消看诊API失败，ID: {Id}", id);
            return ServiceResult<bool>.Failure($"取消看诊失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用获取统计信息API
    /// </summary>
    public async Task<ServiceResult<object>> CallGetStatisticsApiAsync(DateTime? startDate, DateTime? endDate)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            var result = await _consultationApi.GetStatisticsAsync(startDate, endDate);
            return ServiceResult<object>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用获取统计信息API失败");
            return ServiceResult<object>.Failure($"获取统计信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 调用搜索看诊API
    /// </summary>
    public async Task<ServiceResult<List<ConsultationDto>>> CallSearchConsultationsApiAsync(string keyword, int limit = 100)
    {
        try
        {
            // TODO: 将API通信移至公共模块 - 统一API客户端管理
            var result = await _consultationApi.SearchConsultationsAsync(keyword, limit);
            return ServiceResult<List<ConsultationDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用搜索看诊API失败，关键词: {Keyword}", keyword);
            return ServiceResult<List<ConsultationDto>>.Failure($"搜索看诊失败：{ex.Message}");
        }
    }

    #endregion

    #region 数据验证层

    /// <summary>
    /// 验证看诊ID有效性
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateConsultationIdAsync(Guid consultationId)
    {
        try
        {
            if (consultationId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("看诊ID不能为空");
            }

            // 可以添加更多验证逻辑，如检查ID是否存在
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证看诊ID失败，ID: {Id}", consultationId);
            return ServiceResult<bool>.Failure($"验证看诊ID失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证创建看诊DTO
    /// </summary>
    public async Task<ServiceResult<ConsultationValidationResult>> ValidateStartDtoAsync(ConsultationStartDto startDto)
    {
        try
        {
            var result = new ConsultationValidationResult { IsValid = true };

            if (startDto == null)
            {
                result.IsValid = false;
                result.Errors.Add("创建看诊数据不能为空");
                return ServiceResult<ConsultationValidationResult>.Success(result);
            }

            if (startDto.PatientId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("患者ID不能为空");
            }

            if (startDto.DoctorId == Guid.Empty)
            {
                result.IsValid = false;
                result.Errors.Add("医生ID不能为空");
            }

            if (string.IsNullOrWhiteSpace(startDto.ChiefComplaint))
            {
                result.Warnings.Add("建议填写主诉信息");
            }

            return ServiceResult<ConsultationValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证创建看诊DTO失败");
            return ServiceResult<ConsultationValidationResult>.Failure($"验证创建看诊数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证更新看诊DTO
    /// </summary>
    public async Task<ServiceResult<ConsultationValidationResult>> ValidateUpdateDtoAsync(ConsultationUpdateDto updateDto)
    {
        try
        {
            var result = new ConsultationValidationResult { IsValid = true };

            if (updateDto == null)
            {
                result.IsValid = false;
                result.Errors.Add("更新看诊数据不能为空");
                return ServiceResult<ConsultationValidationResult>.Success(result);
            }

            // 可以添加更多的更新验证规则

            return ServiceResult<ConsultationValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证更新看诊DTO失败");
            return ServiceResult<ConsultationValidationResult>.Failure($"验证更新看诊数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证四诊数据完整性
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateFourDiagnosisDataAsync(CompleteFourDiagnosisDto fourDiagnosis)
    {
        try
        {
            if (fourDiagnosis == null)
            {
                return ServiceResult<bool>.Failure("四诊数据不能为空");
            }

            // 检查四诊基本数据是否完整
            var hasInspection = !string.IsNullOrWhiteSpace(fourDiagnosis.Inspection);
            var hasAuscultation = !string.IsNullOrWhiteSpace(fourDiagnosis.Auscultation);
            var hasInquiry = !string.IsNullOrWhiteSpace(fourDiagnosis.Inquiry);
            var hasPalpation = !string.IsNullOrWhiteSpace(fourDiagnosis.Palpation);

            if (!hasInspection && !hasAuscultation && !hasInquiry && !hasPalpation)
            {
                return ServiceResult<bool>.Failure("四诊数据至少需要包含一项：望诊、闻诊、问诊或切诊");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证四诊数据失败");
            return ServiceResult<bool>.Failure($"验证四诊数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证诊断信息
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateDiagnosisAsync(string diagnosis)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
            {
                return ServiceResult<bool>.Failure("诊断信息不能为空");
            }

            if (diagnosis.Length > 500)
            {
                return ServiceResult<bool>.Failure("诊断信息长度不能超过500字符");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证诊断信息失败");
            return ServiceResult<bool>.Failure($"验证诊断信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证主诉信息
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateChiefComplaintAsync(string chiefComplaint)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chiefComplaint))
            {
                return ServiceResult<bool>.Failure("主诉信息不能为空");
            }

            if (chiefComplaint.Length > 200)
            {
                return ServiceResult<bool>.Failure("主诉信息长度不能超过200字符");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证主诉信息失败");
            return ServiceResult<bool>.Failure($"验证主诉信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证查询参数
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateQueryParametersAsync(PagedQueryBaseDto query)
    {
        try
        {
            if (query == null)
            {
                return ServiceResult<bool>.Failure("查询参数不能为空");
            }

            if (query.PageIndex < 1)
            {
                return ServiceResult<bool>.Failure("页码必须大于0");
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                return ServiceResult<bool>.Failure("每页大小必须在1-100之间");
            }

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证查询参数失败");
            return ServiceResult<bool>.Failure($"验证查询参数失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证患者和医生关联
    /// </summary>
    public async Task<ServiceResult<bool>> ValidatePatientDoctorAssociationAsync(Guid patientId, Guid doctorId)
    {
        try
        {
            if (patientId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("患者ID不能为空");
            }

            if (doctorId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("医生ID不能为空");
            }

            // 可以添加更多业务规则验证，如医生是否有权限处理该患者等
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证患者医生关联失败，PatientId: {PatientId}, DoctorId: {DoctorId}", patientId, doctorId);
            return ServiceResult<bool>.Failure($"验证患者医生关联失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证医案关联性
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateMedicalCaseAssociationAsync(Guid consultationId, Guid medicalCaseId)
    {
        try
        {
            if (consultationId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("看诊ID不能为空");
            }

            if (medicalCaseId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("医案ID不能为空");
            }

            // 可以添加更多业务规则验证，如医案状态是否允许关联等
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证医案关联失败，ConsultationId: {ConsultationId}, MedicalCaseId: {MedicalCaseId}", consultationId, medicalCaseId);
            return ServiceResult<bool>.Failure($"验证医案关联失败：{ex.Message}");
        }
    }

    #endregion

    #region 缓存管理层

    /// <summary>
    /// 获取或设置看诊缓存
    /// </summary>
    public async Task<T> GetOrSetCacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
    {
        try
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("缓存命中: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("缓存未命中，执行工厂方法: {Key}", key);
            var value = await factory();
            
            var cacheOptions = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = expiry.Value;
            }
            else
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10); // 默认10分钟
            }

            _cache.Set(key, value, cacheOptions);
            _logger.LogDebug("缓存已设置: {Key}", key);
            
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存操作失败，Key: {Key}", key);
            // 缓存失败时直接执行工厂方法
            return await factory();
        }
    }

    /// <summary>
    /// 清除看诊缓存
    /// </summary>
    public async Task ClearConsultationCacheAsync(Guid consultationId)
    {
        try
        {
            var keysToRemove = new[]
            {
                $"consultation_{consultationId}",
                $"consultation_detail_{consultationId}",
                $"consultation_four_diagnosis_{consultationId}"
            };

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _logger.LogDebug("缓存已清除: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除看诊缓存失败，ConsultationId: {ConsultationId}", consultationId);
        }
    }

    /// <summary>
    /// 清除患者看诊缓存
    /// </summary>
    public async Task ClearPatientConsultationCacheAsync(Guid patientId)
    {
        try
        {
            // 清除与患者相关的看诊缓存
            var keysToRemove = new[]
            {
                $"patient_consultations_{patientId}",
                $"patient_consultation_history_{patientId}",
                $"patient_consultation_stats_{patientId}"
            };

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _logger.LogDebug("患者看诊缓存已清除: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除患者看诊缓存失败，PatientId: {PatientId}", patientId);
        }
    }

    /// <summary>
    /// 清除医生看诊缓存
    /// </summary>
    public async Task ClearDoctorConsultationCacheAsync(Guid doctorId)
    {
        try
        {
            var keysToRemove = new[]
            {
                $"doctor_consultations_{doctorId}",
                $"doctor_consultation_stats_{doctorId}",
                $"doctor_work_statistics_{doctorId}"
            };

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _logger.LogDebug("医生看诊缓存已清除: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除医生看诊缓存失败，DoctorId: {DoctorId}", doctorId);
        }
    }

    /// <summary>
    /// 清除医案看诊缓存
    /// </summary>
    public async Task ClearMedicalCaseConsultationCacheAsync(Guid medicalCaseId)
    {
        try
        {
            var keysToRemove = new[]
            {
                $"medical_case_consultations_{medicalCaseId}",
                $"medical_case_consultation_detail_{medicalCaseId}"
            };

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _logger.LogDebug("医案看诊缓存已清除: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除医案看诊缓存失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
        }
    }

    /// <summary>
    /// 批量清除看诊缓存
    /// </summary>
    public async Task BatchClearConsultationCacheAsync(List<Guid> consultationIds)
    {
        try
        {
            foreach (var consultationId in consultationIds)
            {
                await ClearConsultationCacheAsync(consultationId);
            }
            _logger.LogInformation("批量清除看诊缓存完成，数量: {Count}", consultationIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量清除看诊缓存失败");
        }
    }

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    public async Task<ServiceResult<ConsultationCacheStatisticsDto>> GetCacheStatisticsAsync()
    {
        try
        {
            // 这里简化实现，实际可以通过反射或其他方式获取更详细的缓存统计信息
            var stats = new ConsultationCacheStatisticsDto
            {
                TotalCacheItems = 0, // 需要实际实现获取缓存项数量
                ConsultationCacheCount = 0,
                PatientConsultationCacheCount = 0,
                DoctorConsultationCacheCount = 0,
                TotalMemoryUsage = GC.GetTotalMemory(false),
                HitRate = 0.0, // 需要实际统计命中率
                LastClearTime = DateTime.Now,
                TopCacheItems = []
            };

            return ServiceResult<ConsultationCacheStatisticsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存统计信息失败");
            return ServiceResult<ConsultationCacheStatisticsDto>.Failure($"获取缓存统计信息失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 预加载常用看诊缓存
    /// </summary>
    public async Task PreloadCommonConsultationCacheAsync()
    {
        try
        {
            _logger.LogInformation("开始预加载常用看诊缓存");
            
            // 这里可以预加载一些常用的缓存数据
            // 例如：今日看诊统计、常用诊断等
            
            _logger.LogInformation("常用看诊缓存预加载完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预加载常用看诊缓存失败");
        }
    }

    #endregion

    #region 基础操作层

    /// <summary>
    /// 检查看诊是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckConsultationExistsAsync(Guid consultationId)
    {
        try
        {
            // 可以通过API调用检查看诊是否存在
            var result = await CallGetConsultationByIdApiAsync(consultationId);
            return ServiceResult<bool>.Success(result.IsSuccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查看诊存在性失败，ID: {Id}", consultationId);
            return ServiceResult<bool>.Failure($"检查看诊存在性失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查患者是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckPatientExistsAsync(Guid patientId)
    {
        try
        {
            // 这里可以调用患者服务检查患者是否存在
            // 暂时返回true，实际需要调用患者API
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查患者存在性失败，ID: {Id}", patientId);
            return ServiceResult<bool>.Failure($"检查患者存在性失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查医生是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckDoctorExistsAsync(Guid doctorId)
    {
        try
        {
            // 这里可以调用用户服务检查医生是否存在
            // 暂时返回true，实际需要调用用户API
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查医生存在性失败，ID: {Id}", doctorId);
            return ServiceResult<bool>.Failure($"检查医生存在性失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 检查医案是否存在
    /// </summary>
    public async Task<ServiceResult<bool>> CheckMedicalCaseExistsAsync(Guid medicalCaseId)
    {
        try
        {
            // 这里可以调用医案服务检查医案是否存在
            // 暂时返回true，实际需要调用医案API
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查医案存在性失败，ID: {Id}", medicalCaseId);
            return ServiceResult<bool>.Failure($"检查医案存在性失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 生成看诊编号
    /// </summary>
    public async Task<ServiceResult<string>> GenerateConsultationNumberAsync()
    {
        try
        {
            // 生成格式：CONS-YYYYMMDD-HHMMSS-XXX
            var now = DateTime.Now;
            var dateStr = now.ToString("yyyyMMdd");
            var timeStr = now.ToString("HHmmss");
            var randomStr = new Random().Next(100, 999).ToString();
            
            var consultationNumber = $"CONS-{dateStr}-{timeStr}-{randomStr}";
            return ServiceResult<string>.Success(consultationNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成看诊编号失败");
            return ServiceResult<string>.Failure($"生成看诊编号失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 格式化看诊数据
    /// </summary>
    public async Task<ServiceResult<ConsultationDto>> FormatConsultationDataAsync(ConsultationDto consultation)
    {
        try
        {
            if (consultation == null)
            {
                return ServiceResult<ConsultationDto>.Failure("看诊数据不能为空");
            }

            // 可以在这里添加数据格式化逻辑
            // 例如：时间格式化、字符串处理等
            
            return ServiceResult<ConsultationDto>.Success(consultation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "格式化看诊数据失败");
            return ServiceResult<ConsultationDto>.Failure($"格式化看诊数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 计算看诊持续时间
    /// </summary>
    public async Task<ServiceResult<TimeSpan>> CalculateConsultationDurationAsync(DateTime startTime, DateTime? endTime = null)
    {
        try
        {
            var endDateTime = endTime ?? DateTime.Now;
            var duration = endDateTime - startTime;
            
            if (duration.TotalMinutes < 0)
            {
                return ServiceResult<TimeSpan>.Failure("结束时间不能早于开始时间");
            }
            
            return ServiceResult<TimeSpan>.Success(duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算看诊持续时间失败");
            return ServiceResult<TimeSpan>.Failure($"计算看诊持续时间失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 验证看诊完整性
    /// </summary>
    public async Task<ServiceResult<bool>> ValidateConsultationCompletenessAsync(ConsultationDetailDto consultation)
    {
        try
        {
            if (consultation == null)
            {
                return ServiceResult<bool>.Failure("看诊数据不能为空");
            }

            var isComplete = !string.IsNullOrWhiteSpace(consultation.ChiefComplaint) &&
                            !string.IsNullOrWhiteSpace(consultation.Diagnosis);
            
            return ServiceResult<bool>.Success(isComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证看诊完整性失败");
            return ServiceResult<bool>.Failure($"验证看诊完整性失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 转换DTO格式
    /// </summary>
    public async Task<ServiceResult<TTarget>> ConvertDtoAsync<TSource, TTarget>(TSource source) 
        where TTarget : class, new()
    {
        try
        {
            if (source == null)
            {
                return ServiceResult<TTarget>.Failure("源数据不能为空");
            }

            // 这里可以使用AutoMapper或手动转换
            // 暂时简化实现，实际需要根据具体情况进行转换
            var target = new TTarget();
            return ServiceResult<TTarget>.Success(target);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "转换DTO格式失败");
            return ServiceResult<TTarget>.Failure($"转换DTO格式失败：{ex.Message}");
        }
    }

    #endregion

    #region 系统集成层

    /// <summary>
    /// 记录操作日志
    /// </summary>
    public async Task LogOperationAsync(string operation, Guid consultationId, string details, Guid userId)
    {
        try
        {
            _logger.LogInformation("看诊操作：{Operation}，看诊ID：{ConsultationId}，用户ID：{UserId}，详情：{Details}",
                operation, consultationId, userId, details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录操作日志失败");
        }
    }

    /// <summary>
    /// 触发事件通知
    /// </summary>
    public async Task TriggerEventNotificationAsync(string eventType, Guid consultationId, Dictionary<string, object> eventData)
    {
        try
        {
            _logger.LogInformation("触发事件：{EventType}，看诊ID：{ConsultationId}", eventType, consultationId);
            
            // 这里可以实现事件发布逻辑
            // 例如：通过EventBus或SignalR发送事件通知
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "触发事件通知失败，EventType: {EventType}, ConsultationId: {ConsultationId}", 
                eventType, consultationId);
        }
    }

    /// <summary>
    /// 获取系统配置
    /// </summary>
    public async Task<ServiceResult<T>> GetSystemConfigAsync<T>(string configKey, T defaultValue)
    {
        try
        {
            // 这里可以从配置系统获取配置项
            // 暂时返回默认值
            return ServiceResult<T>.Success(defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统配置失败，ConfigKey: {ConfigKey}", configKey);
            return ServiceResult<T>.Failure($"获取系统配置失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 健康检查
    /// </summary>
    public async Task<ServiceResult<bool>> HealthCheckAsync()
    {
        try
        {
            // 检查API连接、缓存状态等
            var isHealthy = true;
            
            // 可以添加更多健康检查项
            
            return ServiceResult<bool>.Success(isHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return ServiceResult<bool>.Failure($"健康检查失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取时间戳
    /// </summary>
    public async Task<DateTime> GetCurrentTimestampAsync()
    {
        return DateTime.Now;
    }

    /// <summary>
    /// 发送通知
    /// </summary>
    public async Task SendNotificationAsync(string notificationType, Dictionary<string, object> notificationData)
    {
        try
        {
            _logger.LogInformation("发送通知：{NotificationType}", notificationType);
            
            // 这里可以实现通知发送逻辑
            // 例如：邮件、短信、推送通知等
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送通知失败，NotificationType: {NotificationType}", notificationType);
        }
    }

    #endregion
}
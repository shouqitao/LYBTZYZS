using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.Events;
using LYBT.Desktop.Modules.Consultation.Api;
using LYBT.Desktop.Modules.Consultation.Extensions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 看诊模块业务服务实现 - UltraThink v2.0简化版
    /// 专注中医四诊功能，移除西医逻辑和过度设计
    /// </summary>
    /// <summary>
    /// UltraThink Phase 1: 合并后的统一看诊服务
    /// 整合了 ConsultationModuleService + ConsultationDataService + ConsultationDataManager 的功能
    /// 去除冗余，专注核心业务逻辑
    /// </summary>
    public class ConsultationService : LYBT.Shared.Interfaces.Services.IConsultationService
    {
        #region 依赖服务

        private readonly IConsultationApi _consultationApi;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        #endregion

        #region 事件

        // UltraThink v2.0: 保留TCM数据更新事件
        public event EventHandler<TCMDataUpdatedEventArgs>? TCMDataUpdated;

        #endregion

        #region 构造函数

        public ConsultationService(
            IConsultationApi consultationApi,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _consultationApi = consultationApi ?? throw new ArgumentNullException(nameof(consultationApi));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 看诊基础操作

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                _logger.LogInformation("获取分页看诊记录，页码: {PageIndex}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

                // UltraThink v2.0: 使用新的API接口
                var apiResult = await _consultationApi.GetConsultationsAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PagedResult<ConsultationDto>>.Failure(
                        apiResult.Error?.Message ?? "获取看诊记录失败");
                }

                // UltraThink v2.0: 直接使用DTO，无需映射
                var result = new PagedResult<ConsultationDto>(
                    apiResult.Content.Items.ToList(),
                    apiResult.Content.TotalCount,
                    apiResult.Content.CurrentPage,
                    apiResult.Content.PageSize);

                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分页看诊记录时发生异常");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure($"获取看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure("看诊ID不能为空");
                }

                _logger.LogInformation("获取看诊详情，ID: {ConsultationId}", id);

                // UltraThink v2.0: 使用新的API接口，返回的是DetailDto但可以转换
                var apiResult = await _consultationApi.GetByIdAsync(id);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure(
                        apiResult.Error?.Message ?? "获取看诊详情失败");
                }

                // UltraThink v2.0: 直接返回DetailDto
                return ServiceResult<ConsultationDetailDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<ConsultationDetailDto>.Failure($"获取看诊详情失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto createDto)
        {
            try
            {
                _logger.LogInformation("创建看诊记录，患者ID: {PatientId}, 医生ID: {DoctorId}", createDto.PatientId, createDto.DoctorId);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = ValidateStartDto(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<ConsultationDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                // UltraThink v2.0: 直接使用传入的DTO
                var apiResult = await _consultationApi.StartConsultationAsync(createDto);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDto>.Failure(
                        apiResult.Error?.Message ?? "创建看诊记录失败");
                }

                // UltraThink v2.0: 转换DetailDto为ConsultationDto
                var consultationDto = apiResult.Content.ToConsultationDto();
                _logger.LogInformation("成功创建看诊记录，ID: {ConsultationId}", consultationDto.Id);
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建看诊记录时发生异常");
                return ServiceResult<ConsultationDto>.Failure($"创建看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新看诊记录，ID: {ConsultationId}", id);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = ValidateUpdateDetailDto(id, updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<ConsultationDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var apiResult = await _consultationApi.UpdateConsultationAsync(id, updateDto);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDto>.Failure(
                        apiResult.Error?.Message ?? "更新看诊记录失败");
                }

                // UltraThink v2.0: 转换DetailDto为ConsultationDto
                var consultationDto = apiResult.Content.ToConsultationDto();
                _logger.LogInformation("成功更新看诊记录，ID: {ConsultationId}", id);
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊记录时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<ConsultationDto>.Failure($"更新看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("看诊ID不能为空");
                }

                _logger.LogInformation("删除看诊记录，ID: {ConsultationId}", id);

                var apiResult = await _consultationApi.DeleteAsync(id);

                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "删除看诊记录失败");
                }

                _logger.LogInformation("成功删除看诊记录，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"删除看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量删除看诊记录，数量: {Count}", ids.Count);

                var successCount = 0;
                var errors = new List<string>();

                foreach (var id in ids)
                {
                    var result = await DeleteAsync(id);
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        errors.Add($"删除 {id} 失败: {result.ErrorMessage}");
                    }
                }

                if (errors.Any())
                {
                    return ServiceResult.Failure($"批量删除部分失败，成功: {successCount}, 失败: {errors.Count}");
                }

                return ServiceResult.Success($"成功批量删除 {successCount} 条记录");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除看诊记录时发生异常");
                return ServiceResult.Failure($"批量删除失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CanDeleteAsync(Guid id)
        {
            try
            {
                // 检查看诊状态，如果已完成则不能删除
                var consultationResult = await GetByIdAsync(id);
                if (!consultationResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                if (consultation != null && consultation.IsCompleted)
                {
                    return ServiceResult<bool>.Success(false); // 已完成的看诊不能删除
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查是否可删除时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"检查删除权限失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CanModifyAsync(Guid id)
        {
            try
            {
                // 检查看诊状态，如果已完成则不能修改
                var consultationResult = await GetByIdAsync(id);
                if (!consultationResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                if (consultation != null && consultation.IsCompleted)
                {
                    return ServiceResult<bool>.Success(false); // 已完成的看诊不能修改
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查是否可修改时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"检查修改权限失败: {ex.Message}");
            }
        }

        #endregion

        #region 中医诊断管理

        /// <summary>
        /// UltraThink v2.0: 统一的诊断更新方法，包含四诊+诊断结果
        /// </summary>
        public async Task<ServiceResult> UpdateDiagnosisAsync(Guid consultationId, ConsultationUpdateDto diagnosisData)
        {
            try
            {
                _logger.LogInformation("更新诊断信息，ID: {ConsultationId}", consultationId);

                // 直接使用传入的DTO进行更新
                var result = await UpdateAsync(diagnosisData);
                if (result.IsSuccess)
                {
                    // 触发TCM数据更新事件
                    var updatedData = new Dictionary<string, object>();
                    
                    // 添加四诊数据
                    if (!string.IsNullOrEmpty(diagnosisData.Inspection))
                        updatedData["Inspection"] = diagnosisData.Inspection;
                    if (!string.IsNullOrEmpty(diagnosisData.AuscultationOlfaction))
                        updatedData["AuscultationOlfaction"] = diagnosisData.AuscultationOlfaction;
                    if (!string.IsNullOrEmpty(diagnosisData.Inquiry))
                        updatedData["Inquiry"] = diagnosisData.Inquiry;
                    if (!string.IsNullOrEmpty(diagnosisData.Palpation))
                        updatedData["Palpation"] = diagnosisData.Palpation;
                    
                    // 添加诊断数据
                    if (!string.IsNullOrEmpty(diagnosisData.Diagnosis))
                        updatedData["Diagnosis"] = diagnosisData.Diagnosis;
                    if (!string.IsNullOrEmpty(diagnosisData.TCMDiagnosis))
                        updatedData["TCMDiagnosis"] = diagnosisData.TCMDiagnosis;
                    if (!string.IsNullOrEmpty(diagnosisData.TreatmentPrinciple))
                        updatedData["TreatmentPrinciple"] = diagnosisData.TreatmentPrinciple;

                    TCMDataUpdated?.Invoke(this, new TCMDataUpdatedEventArgs
                    {
                        ConsultationId = consultationId,
                        UpdatedSection = "Diagnosis",
                        UpdatedData = updatedData
                    });
                }

                return result.IsSuccess ? ServiceResult.Success("诊断信息更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊断信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新诊断信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<string>> GenerateTCMSummaryAsync(Guid consultationId)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult<string>.Failure("获取看诊信息失败");
                }

                // UltraThink v2.0: 直接使用DTO属性
                var consultation = consultationResult.Data;
                var summary = $"四诊综合：\n" +
                             $"望诊：{consultation.Inspection ?? "未记录"}\n" +
                             $"闻诊：{consultation.AuscultationOlfaction ?? "未记录"}\n" +
                             $"问诊：{consultation.Inquiry ?? "未记录"}\n" +
                             $"切诊：{consultation.Palpation ?? "未记录"}\n" +
                             $"舌诊：{consultation.TongueInspection ?? "未记录"}\n" +
                             $"脉诊：{consultation.PulseCondition ?? "未记录"}";

                return ServiceResult<string>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成四诊综合时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult<string>.Failure($"生成四诊综合失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> ValidateTCMCompletenessAsync(Guid consultationId)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult<object>.Failure("获取看诊信息失败");
                }

                // UltraThink v2.0: 直接使用DTO属性，返回匿名对象
                var consultation = consultationResult.Data;
                var isInspectionComplete = !string.IsNullOrWhiteSpace(consultation.Inspection);
                var isAuscultationComplete = !string.IsNullOrWhiteSpace(consultation.AuscultationOlfaction);
                var isInquiryComplete = !string.IsNullOrWhiteSpace(consultation.Inquiry);
                var isPalpationComplete = !string.IsNullOrWhiteSpace(consultation.Palpation);
                
                var missingItems = new List<string>();
                if (!isInspectionComplete) missingItems.Add("望诊");
                if (!isAuscultationComplete) missingItems.Add("闻诊");
                if (!isInquiryComplete) missingItems.Add("问诊");
                if (!isPalpationComplete) missingItems.Add("切诊");

                var completeness = new
                {
                    IsInspectionComplete = isInspectionComplete,
                    IsAuscultationComplete = isAuscultationComplete,
                    IsInquiryComplete = isInquiryComplete,
                    IsPalpationComplete = isPalpationComplete,
                    MissingItems = missingItems
                };

                return ServiceResult<object>.Success(completeness);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证四诊完整性时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult<object>.Failure($"验证四诊完整性失败: {ex.Message}");
            }
        }

        #endregion

        #region 历史记录管理 - UltraThink合并ConsultationDataService功能

        /// <summary>
        /// 获取患者看诊历史记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("获取患者看诊历史，患者ID: {PatientId}", patientId);

                // 使用分页查询API获取患者历史
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100, // 获取最近100条记录
                    Keyword = patientId.ToString()
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    // 按创建时间倒序排列
                    var history = result.Data.Items
                        .Where(c => c.PatientId == patientId)
                        .OrderByDescending(c => c.CreateTime)
                        .ToList();

                    return ServiceResult<List<ConsultationDto>>.Success(history);
                }

                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者看诊历史时发生异常，患者ID: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取看诊历史失败: {ex.Message}");
            }
        }

        #endregion

        #region 接口实现的缺失方法

        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("根据患者ID获取看诊记录，患者ID: {PatientId}", patientId);

                // 使用现有的GetPatientHistoryAsync方法
                return await GetPatientHistoryAsync(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取看诊记录时发生异常，患者ID: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取患者看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("根据医疗案例ID获取看诊记录，医疗案例ID: {MedicalCaseId}", medicalCaseId);

                // 使用分页查询API，通过关键字查找
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = medicalCaseId.ToString()
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    var consultations = result.Data.Items
                        .Where(c => c.MedicalCaseId == medicalCaseId)
                        .OrderByDescending(c => c.CreateTime)
                        .ToList();

                    return ServiceResult<List<ConsultationDto>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊记录时发生异常，医疗案例ID: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医疗案例看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                _logger.LogInformation("根据医生ID获取看诊记录，医生ID: {DoctorId}", doctorId);

                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = doctorId.ToString()
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    var consultations = result.Data.Items
                        .Where(c => c.DoctorId == doctorId)
                        .OrderByDescending(c => c.CreateTime)
                        .ToList();

                    return ServiceResult<List<ConsultationDto>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取看诊记录时发生异常，医生ID: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医生看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            try
            {
                _logger.LogInformation("完成看诊，ID: {ConsultationId}", id);

                // 调用API的完成看诊接口
                var apiResult = await _consultationApi.CompleteConsultationAsync(id, dto);

                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "完成看诊失败");
                }

                _logger.LogInformation("成功完成看诊，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"完成看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            try
            {
                _logger.LogInformation("取消看诊，ID: {ConsultationId}, 原因: {Reason}", id, reason);

                // 调用API的取消看诊接口
                var apiResult = await _consultationApi.CancelConsultationAsync(id, reason);

                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "取消看诊失败");
                }

                _logger.LogInformation("成功取消看诊，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消看诊时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"取消看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                _logger.LogInformation("获取看诊统计信息，开始日期: {StartDate}, 结束日期: {EndDate}", startDate, endDate);

                // 调用API的统计接口
                var apiResult = await _consultationApi.GetStatisticsAsync(startDate, endDate);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<object>.Failure(apiResult.Error?.Message ?? "获取统计信息失败");
                }

                return ServiceResult<object>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊统计信息时发生异常");
                return ServiceResult<object>.Failure($"获取统计信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索看诊记录，关键字: {Keyword}", keyword);

                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = keyword
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<List<ConsultationDto>>.Success(result.Data.Items.ToList());
                }

                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索看诊记录时发生异常，关键字: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure($"搜索看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("根据医疗案例ID获取四诊数据，医疗案例ID: {MedicalCaseId}", medicalCaseId);

                // 先获取对应的看诊记录
                var consultationsResult = await GetByMedicalCaseIdAsync(medicalCaseId);
                if (!consultationsResult.IsSuccess || consultationsResult.Data == null || !consultationsResult.Data.Any())
                {
                    return ServiceResult<object>.Failure("未找到对应的看诊记录");
                }

                // 取第一个看诊记录的四诊数据
                var consultation = consultationsResult.Data.First();
                var fourDiagnosis = new
                {
                    Inspection = consultation.Inspection,
                    AuscultationOlfaction = consultation.AuscultationOlfaction,
                    Inquiry = consultation.Inquiry,
                    Palpation = consultation.Palpation,
                    TongueInspection = consultation.TongueInspection,
                    PulseCondition = consultation.PulseCondition
                };

                return ServiceResult<object>.Success(fourDiagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取四诊数据时发生异常，医疗案例ID: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<object>.Failure($"获取四诊数据失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
        {
            try
            {
                _logger.LogInformation("保存四诊数据，看诊ID: {ConsultationId}", consultationId);

                // 将object转换为字典以便处理
                var dataDict = fourDiagnosisData as Dictionary<string, object> ?? new Dictionary<string, object>();
                
                // 创建更新DTO
                var updateDto = new ConsultationDetailDto();
                
                if (dataDict.TryGetValue("Inspection", out var inspection))
                    updateDto.Inspection = inspection?.ToString();
                if (dataDict.TryGetValue("AuscultationOlfaction", out var auscultation))
                    updateDto.AuscultationOlfaction = auscultation?.ToString();
                if (dataDict.TryGetValue("Inquiry", out var inquiry))
                    updateDto.Inquiry = inquiry?.ToString();
                if (dataDict.TryGetValue("Palpation", out var palpation))
                    updateDto.Palpation = palpation?.ToString();
                if (dataDict.TryGetValue("TongueInspection", out var tongue))
                    updateDto.TongueInspection = tongue?.ToString();
                if (dataDict.TryGetValue("PulseCondition", out var pulse))
                    updateDto.PulseCondition = pulse?.ToString();

                // 使用已有的UpdateAsync方法
                var result = await UpdateAsync(consultationId, updateDto);
                return ServiceResult<bool>.Success(result.IsSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存四诊数据时发生异常，看诊ID: {ConsultationId}", consultationId);
                return ServiceResult<bool>.Failure($"保存四诊数据失败: {ex.Message}");
            }
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证开始看诊DTO
        /// </summary>
        private ServiceResult ValidateStartDto(ConsultationStartDto createDto)
        {
            try
            {
                if (createDto == null)
                    return ServiceResult.Failure("创建看诊信息不能为空");

                if (createDto.PatientId == Guid.Empty)
                    return ServiceResult.Failure("患者ID不能为空");

                if (createDto.DoctorId == Guid.Empty)
                    return ServiceResult.Failure("医生ID不能为空");

                if (createDto.MedicalCaseId == Guid.Empty)
                    return ServiceResult.Failure("医疗案例ID不能为空");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证创建看诊DTO异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证更新看诊DetailDTO
        /// </summary>
        private ServiceResult ValidateUpdateDetailDto(Guid id, ConsultationDetailDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult.Failure("更新看诊信息不能为空");

                if (id == Guid.Empty)
                    return ServiceResult.Failure("看诊ID不能为空");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证更新看诊DTO异常: {ex.Message}");
            }
        }

        #endregion
    }
}
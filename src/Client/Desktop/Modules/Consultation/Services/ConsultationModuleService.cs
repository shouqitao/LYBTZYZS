using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Desktop.Core.Models.Consultation;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Core.Enums;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 看诊模块业务服务实现 - UltraThink架构重构版
    /// 整合看诊数据管理、处方管理、验方管理等所有业务逻辑
    /// 遵循UltraThink模块化原则：高内聚、低耦合、自包含
    /// </summary>
    public class ConsultationModuleService : IConsultationModuleService
    {
        #region 缓存配置常量

        private const string HERBS_CACHE_KEY = "consultation:herbs";
        private const string FORMULAS_CACHE_KEY = "consultation:formulas";
        private const string PATIENTS_CACHE_KEY = "consultation:patients";
        
        private static readonly TimeSpan HERBS_CACHE_DURATION = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan FORMULAS_CACHE_DURATION = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan PATIENTS_CACHE_DURATION = TimeSpan.FromMinutes(10);

        #endregion

        #region 依赖服务

        private readonly IConsultationApiService _consultationApiService;
        private readonly IPrescriptionApiService _prescriptionApiService;
        private readonly IFormulaApiService _formulaApiService;
        private readonly IPatientApiService _patientApiService;
        private readonly IHerbApiService _herbApiService;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationModuleService> _logger;

        #endregion

        #region 内部状态

        private readonly ObservableCollection<PrescriptionItemInfo> _currentPrescriptionItems = new();
        private readonly Dictionary<Guid, ConsultationInfo> _activeConsultations = new();

        #endregion

        #region 事件

        public event EventHandler<PrescriptionItemsChangedEventArgs>? PrescriptionItemsChanged;
        public event EventHandler<ConsultationStatusChangedEventArgs>? ConsultationStatusChanged;
        public event EventHandler<TCMDataUpdatedEventArgs>? TCMDataUpdated;

        #endregion

        #region 构造函数

        public ConsultationModuleService(
            IConsultationApiService consultationApiService,
            IPrescriptionApiService prescriptionApiService,
            IFormulaApiService formulaApiService,
            IPatientApiService patientApiService,
            IHerbApiService herbApiService,
            ICacheService cacheService,
            IMapper mapper,
            ILogger<ConsultationModuleService> logger)
        {
            _consultationApiService = consultationApiService ?? throw new ArgumentNullException(nameof(consultationApiService));
            _prescriptionApiService = prescriptionApiService ?? throw new ArgumentNullException(nameof(prescriptionApiService));
            _formulaApiService = formulaApiService ?? throw new ArgumentNullException(nameof(formulaApiService));
            _patientApiService = patientApiService ?? throw new ArgumentNullException(nameof(patientApiService));
            _herbApiService = herbApiService ?? throw new ArgumentNullException(nameof(herbApiService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 看诊基础操作

        public async Task<ServiceResult<PagedResult<ConsultationInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                _logger.LogInformation("获取分页看诊记录，页码: {PageIndex}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

                var response = await _consultationApiService.GetConsultationsAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultationInfos = _mapper.Map<List<ConsultationInfo>>(response.Content.Items);
                    var pagedResult = new PagedResult<ConsultationInfo>
                    {
                        Items = consultationInfos,
                        TotalCount = response.Content.TotalCount,
                        PageIndex = response.Content.PageIndex,
                        PageSize = response.Content.PageSize
                    };

                    return ServiceResult<PagedResult<ConsultationInfo>>.Success(pagedResult);
                }

                return ServiceResult<PagedResult<ConsultationInfo>>.Failure("获取看诊记录失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分页看诊记录时发生异常");
                return ServiceResult<PagedResult<ConsultationInfo>>.Failure($"获取看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("获取看诊详情，ID: {ConsultationId}", id);

                var response = await _consultationApiService.GetByIdAsync(id);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultationInfo = _mapper.Map<ConsultationInfo>(response.Content);
                    return ServiceResult<ConsultationInfo>.Success(consultationInfo);
                }

                return ServiceResult<ConsultationInfo>.Failure("获取看诊详情失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<ConsultationInfo>.Failure($"获取看诊详情失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationInfo>> CreateAsync(ConsultationCreateInfo createInfo)
        {
            try
            {
                _logger.LogInformation("创建看诊记录，患者: {PatientName}, 医生: {DoctorName}", createInfo.PatientName, createInfo.DoctorName);

                // UltraThink四层架构：验证数据
                var validationResult = createInfo.Validate();
                if (!validationResult.IsValid)
                {
                    return ServiceResult<ConsultationInfo>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var startDto = _mapper.Map<ConsultationStartDto>(createInfo);

                var response = await _consultationApiService.StartConsultationAsync(startDto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultationInfo = _mapper.Map<ConsultationInfo>(response.Content);
                    
                    // 添加到活跃看诊列表
                    _activeConsultations[consultationInfo.Id] = consultationInfo;
                    
                    _logger.LogInformation("成功创建看诊记录，ID: {ConsultationId}", consultationInfo.Id);
                    return ServiceResult<ConsultationInfo>.Success(consultationInfo);
                }

                return ServiceResult<ConsultationInfo>.Failure("创建看诊记录失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建看诊记录时发生异常");
                return ServiceResult<ConsultationInfo>.Failure($"创建看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationInfo>> UpdateAsync(ConsultationUpdateInfo updateInfo)
        {
            try
            {
                _logger.LogInformation("更新看诊记录，ID: {ConsultationId}", updateInfo.Id);

                // 验证数据
                var validationResult = updateInfo.Validate();
                if (!validationResult.IsValid)
                {
                    return ServiceResult<ConsultationInfo>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                // 转换为DTO
                var updateDto = _mapper.Map<ConsultationUpdateDto>(updateInfo);

                var response = await _consultationApiService.UpdateConsultationAsync(updateInfo.Id, updateDto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultationInfo = _mapper.Map<ConsultationInfo>(response.Content);
                    
                    // 更新活跃看诊列表
                    _activeConsultations[consultationInfo.Id] = consultationInfo;
                    
                    _logger.LogInformation("成功更新看诊记录，ID: {ConsultationId}", consultationInfo.Id);
                    return ServiceResult<ConsultationInfo>.Success(consultationInfo);
                }

                return ServiceResult<ConsultationInfo>.Failure("更新看诊记录失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊记录时发生异常，ID: {ConsultationId}", updateInfo.Id);
                return ServiceResult<ConsultationInfo>.Failure($"更新看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("删除看诊记录，ID: {ConsultationId}", id);

                var response = await _consultationApiService.DeleteAsync(id);

                if (response.IsSuccessStatusCode)
                {
                    // 从活跃看诊列表中移除
                    _activeConsultations.Remove(id);
                    
                    _logger.LogInformation("成功删除看诊记录，ID: {ConsultationId}", id);
                    return ServiceResult.Success();
                }

                return ServiceResult.Failure("删除看诊记录失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录时发生异常，ID: {ConsultationId}", id);
                return ServiceResult.Failure($"删除看诊记录失败: {ex.Message}");
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
                    return ServiceResult<bool>.Success(false, "已完成的看诊不能删除");
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
                    return ServiceResult<bool>.Success(false, "已完成的看诊不能修改");
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

        #region 看诊工作流管理

        public async Task<ServiceResult<ConsultationInfo>> StartConsultationAsync(Guid patientId, Guid doctorId)
        {
            try
            {
                _logger.LogInformation("开始新的看诊流程，患者ID: {PatientId}, 医生ID: {DoctorId}", patientId, doctorId);

                var startDto = new ConsultationStartDto
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    ConsultationTime = DateTime.Now
                };

                var response = await _consultationApiService.StartConsultationAsync(startDto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultationInfo = _mapper.Map<ConsultationInfo>(response.Content);
                    
                    // 添加到活跃看诊列表
                    _activeConsultations[consultationInfo.Id] = consultationInfo;
                    
                    // 触发状态变更事件
                    ConsultationStatusChanged?.Invoke(this, new ConsultationStatusChangedEventArgs
                    {
                        ConsultationId = consultationInfo.Id,
                        NewStatus = consultationInfo.Status,
                        OldStatus = CommonStatus.Disabled
                    });

                    return ServiceResult<ConsultationInfo>.Success(consultationInfo);
                }

                return ServiceResult<ConsultationInfo>.Failure("开始看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊时发生异常");
                return ServiceResult<ConsultationInfo>.Failure($"开始看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> CompleteConsultationAsync(Guid consultationId)
        {
            try
            {
                _logger.LogInformation("完成看诊，ID: {ConsultationId}", consultationId);

                var completeDto = new ConsultationCompleteDto
                {
                    CompleteTime = DateTime.Now
                };

                var response = await _consultationApiService.CompleteConsultationAsync(consultationId, completeDto);

                if (response.IsSuccessStatusCode)
                {
                    // 从活跃看诊列表中移除
                    if (_activeConsultations.TryGetValue(consultationId, out var consultation))
                    {
                        var oldStatus = consultation.Status;
                        _activeConsultations.Remove(consultationId);

                        // 触发状态变更事件
                        ConsultationStatusChanged?.Invoke(this, new ConsultationStatusChangedEventArgs
                        {
                            ConsultationId = consultationId,
                            OldStatus = oldStatus,
                            NewStatus = CommonStatus.Disabled // 完成状态
                        });
                    }

                    return ServiceResult.Success("看诊已完成");
                }

                return ServiceResult.Failure("完成看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"完成看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> PauseConsultationAsync(Guid consultationId)
        {
            try
            {
                // 暂停看诊实际上是更新状态
                var updateDto = new UpdateStatusDto
                {
                    Status = CommonStatus.Disabled,
                    Reason = "暂停看诊"
                };

                var response = await _consultationApiService.UpdateStatusAsync(consultationId, updateDto);

                if (response.IsSuccessStatusCode)
                {
                    return ServiceResult.Success("看诊已暂停");
                }

                return ServiceResult.Failure("暂停看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停看诊时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"暂停看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationInfo>> ResumeConsultationAsync(Guid consultationId)
        {
            try
            {
                // 恢复看诊实际上是更新状态
                var updateDto = new UpdateStatusDto
                {
                    Status = CommonStatus.Enabled,
                    Reason = "恢复看诊"
                };

                var response = await _consultationApiService.UpdateStatusAsync(consultationId, updateDto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultationInfo = _mapper.Map<ConsultationInfo>(response.Content);
                    
                    // 重新添加到活跃看诊列表
                    _activeConsultations[consultationInfo.Id] = consultationInfo;
                    
                    return ServiceResult<ConsultationInfo>.Success(consultationInfo);
                }

                return ServiceResult<ConsultationInfo>.Failure("恢复看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复看诊时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult<ConsultationInfo>.Failure($"恢复看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ConsultationInfo>>> GetActiveConsultationsAsync(Guid doctorId)
        {
            try
            {
                var response = await _consultationApiService.GetTodayConsultationsByDoctorAsync(doctorId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultations = _mapper.Map<List<ConsultationInfo>>(response.Content);
                    
                    // 更新活跃看诊列表
                    foreach (var consultation in consultations)
                    {
                        _activeConsultations[consultation.Id] = consultation;
                    }

                    return ServiceResult<List<ConsultationInfo>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationInfo>>.Failure("获取活跃看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃看诊时发生异常，医生ID: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationInfo>>.Failure($"获取活跃看诊失败: {ex.Message}");
            }
        }

        #endregion

        #region 中医四诊管理

        public async Task<ServiceResult> UpdateInspectionAsync(Guid consultationId, TCMInspectionInfo inspectionInfo)
        {
            try
            {
                // 先获取当前看诊信息
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                
                // 创建更新DTO
                var updateInfo = ConsultationUpdateInfo.FromConsultationInfo(consultation);
                updateInfo.Inspection = inspectionInfo.OverallInspection;
                updateInfo.TongueInspection = inspectionInfo.TongueInspection;

                var result = await UpdateAsync(updateInfo);
                if (result.IsSuccess)
                {
                    // 触发TCM数据更新事件
                    TCMDataUpdated?.Invoke(this, new TCMDataUpdatedEventArgs
                    {
                        ConsultationId = consultationId,
                        UpdatedSection = "Inspection",
                        UpdatedData = new Dictionary<string, object>
                        {
                            ["Inspection"] = inspectionInfo.OverallInspection ?? "",
                            ["TongueInspection"] = inspectionInfo.TongueInspection ?? ""
                        }
                    });
                }

                return result.IsSuccess ? ServiceResult.Success("望诊信息更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新望诊信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新望诊信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateAuscultationOlfactionAsync(Guid consultationId, TCMAuscultationOlfactionInfo auscultationInfo)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var updateInfo = ConsultationUpdateInfo.FromConsultationInfo(consultation);
                updateInfo.AuscultationOlfaction = auscultationInfo.OverallAuscultation;

                var result = await UpdateAsync(updateInfo);
                if (result.IsSuccess)
                {
                    TCMDataUpdated?.Invoke(this, new TCMDataUpdatedEventArgs
                    {
                        ConsultationId = consultationId,
                        UpdatedSection = "AuscultationOlfaction",
                        UpdatedData = new Dictionary<string, object>
                        {
                            ["AuscultationOlfaction"] = auscultationInfo.OverallAuscultation ?? ""
                        }
                    });
                }

                return result.IsSuccess ? ServiceResult.Success("闻诊信息更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新闻诊信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新闻诊信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateInquiryAsync(Guid consultationId, TCMInquiryInfo inquiryInfo)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var updateInfo = ConsultationUpdateInfo.FromConsultationInfo(consultation);
                updateInfo.Inquiry = inquiryInfo.OverallInquiry;
                updateInfo.ChiefComplaint = inquiryInfo.ChiefComplaint;
                updateInfo.PresentIllness = inquiryInfo.PresentIllness;
                updateInfo.PastHistory = inquiryInfo.PastHistory;
                updateInfo.FamilyHistory = inquiryInfo.FamilyHistory;

                var result = await UpdateAsync(updateInfo);
                if (result.IsSuccess)
                {
                    TCMDataUpdated?.Invoke(this, new TCMDataUpdatedEventArgs
                    {
                        ConsultationId = consultationId,
                        UpdatedSection = "Inquiry",
                        UpdatedData = new Dictionary<string, object>
                        {
                            ["Inquiry"] = inquiryInfo.OverallInquiry ?? "",
                            ["ChiefComplaint"] = inquiryInfo.ChiefComplaint ?? "",
                            ["PresentIllness"] = inquiryInfo.PresentIllness ?? "",
                            ["PastHistory"] = inquiryInfo.PastHistory ?? "",
                            ["FamilyHistory"] = inquiryInfo.FamilyHistory ?? ""
                        }
                    });
                }

                return result.IsSuccess ? ServiceResult.Success("问诊信息更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新问诊信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新问诊信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdatePalpationAsync(Guid consultationId, TCMPalpationInfo palpationInfo)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var updateInfo = ConsultationUpdateInfo.FromConsultationInfo(consultation);
                updateInfo.Palpation = palpationInfo.OverallPalpation;
                updateInfo.PulseCondition = palpationInfo.PulseCondition;

                var result = await UpdateAsync(updateInfo);
                if (result.IsSuccess)
                {
                    TCMDataUpdated?.Invoke(this, new TCMDataUpdatedEventArgs
                    {
                        ConsultationId = consultationId,
                        UpdatedSection = "Palpation",
                        UpdatedData = new Dictionary<string, object>
                        {
                            ["Palpation"] = palpationInfo.OverallPalpation ?? "",
                            ["PulseCondition"] = palpationInfo.PulseCondition ?? ""
                        }
                    });
                }

                return result.IsSuccess ? ServiceResult.Success("切诊信息更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新切诊信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新切诊信息失败: {ex.Message}");
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

        public async Task<ServiceResult<TCMCompletenessInfo>> ValidateTCMCompletenessAsync(Guid consultationId)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult<TCMCompletenessInfo>.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var completeness = new TCMCompletenessInfo
                {
                    IsInspectionComplete = !string.IsNullOrWhiteSpace(consultation.Inspection),
                    IsAuscultationComplete = !string.IsNullOrWhiteSpace(consultation.AuscultationOlfaction),
                    IsInquiryComplete = !string.IsNullOrWhiteSpace(consultation.Inquiry),
                    IsPalpationComplete = !string.IsNullOrWhiteSpace(consultation.Palpation),
                    MissingItems = new List<string>()
                };

                if (!completeness.IsInspectionComplete) completeness.MissingItems.Add("望诊");
                if (!completeness.IsAuscultationComplete) completeness.MissingItems.Add("闻诊");
                if (!completeness.IsInquiryComplete) completeness.MissingItems.Add("问诊");
                if (!completeness.IsPalpationComplete) completeness.MissingItems.Add("切诊");

                return ServiceResult<TCMCompletenessInfo>.Success(completeness);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证四诊完整性时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult<TCMCompletenessInfo>.Failure($"验证四诊完整性失败: {ex.Message}");
            }
        }

        #endregion

        #region 体征管理

        public async Task<ServiceResult> UpdateVitalSignsAsync(Guid consultationId, VitalSignsInfo vitalSigns)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var updateInfo = ConsultationUpdateInfo.FromConsultationInfo(consultation);
                updateInfo.SetVitalSigns(
                    vitalSigns.Temperature,
                    vitalSigns.SystolicPressure,
                    vitalSigns.DiastolicPressure,
                    vitalSigns.HeartRate,
                    vitalSigns.RespiratoryRate);

                var result = await UpdateAsync(updateInfo);
                return result.IsSuccess ? ServiceResult.Success("生命体征更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新生命体征时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新生命体征失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<VitalSignsHistoryInfo>>> GetVitalSignsHistoryAsync(Guid patientId, int days = 30)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddDays(-days);

                var response = await _consultationApiService.GetConsultationsAsync(
                    patientId: patientId,
                    startDate: startDate,
                    endDate: endDate);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultations = _mapper.Map<List<ConsultationInfo>>(response.Content.Items);
                    var vitalSignsHistory = consultations
                        .Where(c => c.IsVitalSignsComplete)
                        .Select(c => new VitalSignsHistoryInfo
                        {
                            Id = Guid.NewGuid(),
                            ConsultationId = c.Id,
                            Temperature = c.Temperature,
                            SystolicPressure = c.SystolicPressure,
                            DiastolicPressure = c.DiastolicPressure,
                            HeartRate = c.HeartRate,
                            RespiratoryRate = c.RespiratoryRate,
                            MeasureTime = c.ConsultationTime
                        })
                        .OrderByDescending(v => v.MeasureTime)
                        .ToList();

                    return ServiceResult<List<VitalSignsHistoryInfo>>.Success(vitalSignsHistory);
                }

                return ServiceResult<List<VitalSignsHistoryInfo>>.Failure("获取体征历史失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取体征历史时发生异常，患者ID: {PatientId}", patientId);
                return ServiceResult<List<VitalSignsHistoryInfo>>.Failure($"获取体征历史失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<VitalSignsTrendInfo>> AnalyzeVitalSignsTrendsAsync(Guid patientId)
        {
            try
            {
                var historyResult = await GetVitalSignsHistoryAsync(patientId, 90); // 获取3个月数据
                if (!historyResult.IsSuccess || historyResult.Data == null || !historyResult.Data.Any())
                {
                    return ServiceResult<VitalSignsTrendInfo>.Success(new VitalSignsTrendInfo
                    {
                        HasAbnormalTrend = false,
                        Warnings = new List<string> { "无足够的历史数据进行趋势分析" }
                    });
                }

                var history = historyResult.Data;
                var trendInfo = new VitalSignsTrendInfo();
                var warnings = new List<string>();

                // 分析血压趋势
                var recentBP = history.Take(5).Where(h => h.SystolicPressure.HasValue && h.DiastolicPressure.HasValue);
                if (recentBP.Any())
                {
                    var avgSystolic = recentBP.Average(h => h.SystolicPressure!.Value);
                    var avgDiastolic = recentBP.Average(h => h.DiastolicPressure!.Value);

                    if (avgSystolic > 140 || avgDiastolic > 90)
                    {
                        warnings.Add("血压偏高趋势");
                        trendInfo.HasAbnormalTrend = true;
                    }
                    else if (avgSystolic < 90 || avgDiastolic < 60)
                    {
                        warnings.Add("血压偏低趋势");
                        trendInfo.HasAbnormalTrend = true;
                    }
                }

                // 分析体温趋势
                var recentTemp = history.Take(5).Where(h => h.Temperature.HasValue);
                if (recentTemp.Any())
                {
                    var avgTemp = recentTemp.Average(h => h.Temperature!.Value);
                    if (avgTemp > 37.5m)
                    {
                        warnings.Add("体温偏高趋势");
                        trendInfo.HasAbnormalTrend = true;
                    }
                }

                trendInfo.Warnings = warnings;
                trendInfo.TrendData = new Dictionary<string, object>
                {
                    ["RecordCount"] = history.Count,
                    ["AnalysisPeriod"] = "90天"
                };

                return ServiceResult<VitalSignsTrendInfo>.Success(trendInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析体征趋势时发生异常，患者ID: {PatientId}", patientId);
                return ServiceResult<VitalSignsTrendInfo>.Failure($"分析体征趋势失败: {ex.Message}");
            }
        }

        #endregion

        #region 诊断管理

        public async Task<ServiceResult> UpdateDiagnosisAsync(Guid consultationId, DiagnosisInfo diagnosisInfo)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var updateInfo = ConsultationUpdateInfo.FromConsultationInfo(consultation);
                updateInfo.TCMDiagnosis = diagnosisInfo.TCMDiagnosis;
                updateInfo.WesternDiagnosis = diagnosisInfo.WesternDiagnosis;
                updateInfo.Diagnosis = diagnosisInfo.Diagnosis;
                updateInfo.DiagnosisCatalogId = diagnosisInfo.DiagnosisCatalogId;
                updateInfo.TreatmentPrinciple = diagnosisInfo.TreatmentPrinciple;
                updateInfo.MedicalAdvice = diagnosisInfo.MedicalAdvice;

                var result = await UpdateAsync(updateInfo);
                return result.IsSuccess ? ServiceResult.Success("诊断信息更新成功") : ServiceResult.Failure(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊断信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新诊断信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<DiagnosisSuggestionInfo>>> GetDiagnosisSuggestionsAsync(string symptoms)
        {
            try
            {
                // 这里可以实现基于症状的诊断建议逻辑
                // 目前返回示例数据
                var suggestions = new List<DiagnosisSuggestionInfo>();

                if (symptoms.Contains("咳嗽"))
                {
                    suggestions.Add(new DiagnosisSuggestionInfo
                    {
                        Diagnosis = "风寒咳嗽",
                        Category = "中医诊断",
                        Confidence = 0.8m,
                        ReasoningSteps = new List<string> { "症状包含咳嗽", "需进一步望诊确认" }
                    });
                }

                if (symptoms.Contains("发热"))
                {
                    suggestions.Add(new DiagnosisSuggestionInfo
                    {
                        Diagnosis = "外感发热",
                        Category = "中医诊断",
                        Confidence = 0.7m,
                        ReasoningSteps = new List<string> { "症状包含发热", "需结合四诊确诊" }
                    });
                }

                return ServiceResult<List<DiagnosisSuggestionInfo>>.Success(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊断建议时发生异常，症状: {Symptoms}", symptoms);
                return ServiceResult<List<DiagnosisSuggestionInfo>>.Failure($"获取诊断建议失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<DiagnosisValidationInfo>> ValidateDiagnosisAsync(Guid consultationId)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult<DiagnosisValidationInfo>.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                var validation = new DiagnosisValidationInfo();
                var issues = new List<string>();
                var suggestions = new List<string>();

                if (string.IsNullOrWhiteSpace(consultation.Diagnosis))
                {
                    issues.Add("缺少主要诊断");
                    validation.IsValid = false;
                }

                if (string.IsNullOrWhiteSpace(consultation.TCMDiagnosis) && string.IsNullOrWhiteSpace(consultation.WesternDiagnosis))
                {
                    issues.Add("至少需要提供中医或西医诊断之一");
                    suggestions.Add("建议完善中医辨证或西医诊断");
                }

                if (!consultation.IsTCMComplete)
                {
                    suggestions.Add("建议完善中医四诊以支持诊断");
                }

                if (consultation.IsVitalSignsComplete && consultation.Temperature > 38.5m)
                {
                    suggestions.Add("患者体温偏高，建议考虑感染性疾病");
                }

                validation.IsValid = !issues.Any();
                validation.Issues = issues;
                validation.Suggestions = suggestions;

                return ServiceResult<DiagnosisValidationInfo>.Success(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证诊断时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult<DiagnosisValidationInfo>.Failure($"验证诊断失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<string>>> GetFrequentDiagnosesAsync(Guid doctorId, int count = 20)
        {
            try
            {
                var endDate = DateTime.Now;
                var startDate = endDate.AddMonths(-6); // 获取最近6个月的数据

                var response = await _consultationApiService.GetConsultationsAsync(
                    doctorId: doctorId,
                    startDate: startDate,
                    endDate: endDate,
                    pageSize: 1000);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultations = _mapper.Map<List<ConsultationInfo>>(response.Content.Items);
                    var frequentDiagnoses = consultations
                        .Where(c => !string.IsNullOrWhiteSpace(c.Diagnosis))
                        .GroupBy(c => c.Diagnosis)
                        .OrderByDescending(g => g.Count())
                        .Take(count)
                        .Select(g => g.Key)
                        .ToList();

                    return ServiceResult<List<string>>.Success(frequentDiagnoses);
                }

                return ServiceResult<List<string>>.Failure("获取常用诊断失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取常用诊断时发生异常，医生ID: {DoctorId}", doctorId);
                return ServiceResult<List<string>>.Failure($"获取常用诊断失败: {ex.Message}");
            }
        }

        #endregion

        #region 处方管理（集成）

        public ObservableCollection<PrescriptionItemInfo> GetCurrentPrescriptionItems()
        {
            return _currentPrescriptionItems;
        }

        public async Task<ServiceResult> AddHerbToPrescriptionAsync(Guid consultationId, HerbDto herb, decimal quantity = 10m)
        {
            try
            {
                var existingItem = _currentPrescriptionItems.FirstOrDefault(item => item.HerbId == herb.Id);
                if (existingItem != null)
                {
                    // 如果已存在，增加数量
                    existingItem.Quantity += quantity;
                    existingItem.CalculateSubtotal();
                }
                else
                {
                    // 添加新项目
                    var newItem = new PrescriptionItemInfo
                    {
                        Id = Guid.NewGuid(),
                        HerbId = herb.Id,
                        HerbName = herb.Name,
                        Quantity = quantity,
                        Unit = herb.Unit,
                        UnitPrice = herb.Price,
                        Usage = "常规用法",
                        Remark = ""
                    };
                    newItem.CalculateSubtotal();

                    _currentPrescriptionItems.Add(newItem);
                }

                // 触发处方项目变更事件
                PrescriptionItemsChanged?.Invoke(this, new PrescriptionItemsChangedEventArgs
                {
                    ConsultationId = consultationId,
                    Items = _currentPrescriptionItems.ToList(),
                    ChangeType = "Add"
                });

                return ServiceResult.Success($"已添加 {herb.Name} 到处方");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加药材到处方时发生异常，药材: {HerbName}", herb.Name);
                return ServiceResult.Failure($"添加药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> RemoveHerbFromPrescriptionAsync(Guid consultationId, Guid herbId)
        {
            try
            {
                var item = _currentPrescriptionItems.FirstOrDefault(i => i.HerbId == herbId);
                if (item != null)
                {
                    _currentPrescriptionItems.Remove(item);

                    // 触发处方项目变更事件
                    PrescriptionItemsChanged?.Invoke(this, new PrescriptionItemsChangedEventArgs
                    {
                        ConsultationId = consultationId,
                        Items = _currentPrescriptionItems.ToList(),
                        ChangeType = "Remove"
                    });

                    return ServiceResult.Success($"已从处方中移除 {item.HerbName}");
                }

                return ServiceResult.Failure("未找到指定的药材");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从处方中移除药材时发生异常，药材ID: {HerbId}", herbId);
                return ServiceResult.Failure($"移除药材失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> UpdateHerbQuantityAsync(Guid consultationId, Guid herbId, decimal newQuantity)
        {
            try
            {
                var item = _currentPrescriptionItems.FirstOrDefault(i => i.HerbId == herbId);
                if (item != null)
                {
                    item.Quantity = newQuantity;
                    item.CalculateSubtotal();

                    // 触发处方项目变更事件
                    PrescriptionItemsChanged?.Invoke(this, new PrescriptionItemsChangedEventArgs
                    {
                        ConsultationId = consultationId,
                        Items = _currentPrescriptionItems.ToList(),
                        ChangeType = "Update"
                    });

                    return ServiceResult.Success($"已更新 {item.HerbName} 的用量");
                }

                return ServiceResult.Failure("未找到指定的药材");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材用量时发生异常，药材ID: {HerbId}", herbId);
                return ServiceResult.Failure($"更新药材用量失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ClearPrescriptionAsync(Guid consultationId)
        {
            try
            {
                _currentPrescriptionItems.Clear();

                // 触发处方项目变更事件
                PrescriptionItemsChanged?.Invoke(this, new PrescriptionItemsChangedEventArgs
                {
                    ConsultationId = consultationId,
                    Items = new List<PrescriptionItemInfo>(),
                    ChangeType = "Clear"
                });

                return ServiceResult.Success("已清空当前处方");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空处方时发生异常");
                return ServiceResult.Failure($"清空处方失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PrescriptionInfo>> SavePrescriptionAsync(ConsultationPrescriptionCreateInfo prescriptionInfo)
        {
            try
            {
                var createDto = _mapper.Map<PrescriptionCreateDto>(prescriptionInfo);

                var response = await _prescriptionApiService.CreateAsync(createDto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var prescriptionResult = _mapper.Map<PrescriptionInfo>(response.Content);
                    
                    // 清空当前处方
                    await ClearPrescriptionAsync(prescriptionInfo.ConsultationId);

                    return ServiceResult<PrescriptionInfo>.Success(prescriptionResult);
                }

                return ServiceResult<PrescriptionInfo>.Failure("保存处方失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方时发生异常");
                return ServiceResult<PrescriptionInfo>.Failure($"保存处方失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<PrescriptionValidationInfo>> ValidatePrescriptionAsync(Guid consultationId)
        {
            try
            {
                var validation = new PrescriptionValidationInfo();
                var errors = new List<string>();
                var warnings = new List<string>();

                if (!_currentPrescriptionItems.Any())
                {
                    errors.Add("处方不能为空");
                }

                foreach (var item in _currentPrescriptionItems)
                {
                    if (item.Quantity <= 0)
                    {
                        errors.Add($"药材 {item.HerbName} 的用量必须大于0");
                    }

                    if (item.UnitPrice < 0)
                    {
                        errors.Add($"药材 {item.HerbName} 的单价不能为负数");
                    }

                    if (string.IsNullOrWhiteSpace(item.HerbName))
                    {
                        errors.Add("存在未指定名称的药材");
                    }
                }

                // 检查药材配伍（示例逻辑）
                var herbNames = _currentPrescriptionItems.Select(i => i.HerbName.ToLower()).ToList();
                if (herbNames.Contains("甘草") && herbNames.Contains("甘遂"))
                {
                    warnings.Add("甘草与甘遂存在配伍禁忌");
                }

                validation.IsValid = !errors.Any();
                validation.Errors = errors;
                validation.Warnings = warnings;

                return ServiceResult<PrescriptionValidationInfo>.Success(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方时发生异常");
                return ServiceResult<PrescriptionValidationInfo>.Failure($"验证处方失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<decimal>> CalculatePrescriptionTotalAsync(Guid consultationId)
        {
            try
            {
                var total = _currentPrescriptionItems.Sum(item => item.Subtotal);
                return ServiceResult<decimal>.Success(total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算处方总价时发生异常");
                return ServiceResult<decimal>.Failure($"计算处方总价失败: {ex.Message}");
            }
        }

        #endregion

        #region 验方管理（集成）

        public async Task<ServiceResult<List<PrescriptionItemInfo>>> ApplyFormulaTemplateAsync(Guid consultationId, FormulaInfo formula)
        {
            try
            {
                var items = formula.Herbs.Select(herb => new PrescriptionItemInfo
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = herb.Quantity,
                    Unit = herb.Unit,
                    UnitPrice = herb.Price,
                    Usage = "按验方用法",
                    Remark = $"来自验方: {formula.Name}"
                }).ToList();

                // 计算小计
                foreach (var item in items)
                {
                    item.CalculateSubtotal();
                }

                // 清空当前处方并添加验方项目
                _currentPrescriptionItems.Clear();
                foreach (var item in items)
                {
                    _currentPrescriptionItems.Add(item);
                }

                // 触发处方项目变更事件
                PrescriptionItemsChanged?.Invoke(this, new PrescriptionItemsChangedEventArgs
                {
                    ConsultationId = consultationId,
                    Items = items,
                    ChangeType = "Apply Formula"
                });

                return ServiceResult<List<PrescriptionItemInfo>>.Success(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用验方模板时发生异常，验方: {FormulaName}", formula.Name);
                return ServiceResult<List<PrescriptionItemInfo>>.Failure($"应用验方模板失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<PrescriptionItemInfo>>> MergeFormulaToPrescriptionAsync(
            Guid consultationId, FormulaInfo formula, FormulaMergeMode mergeMode = FormulaMergeMode.Merge)
        {
            try
            {
                var formulaItems = formula.Herbs.Select(herb => new PrescriptionItemInfo
                {
                    Id = Guid.NewGuid(),
                    HerbId = herb.Id,
                    HerbName = herb.Name,
                    Quantity = herb.Quantity,
                    Unit = herb.Unit,
                    UnitPrice = herb.Price,
                    Usage = "按验方用法",
                    Remark = $"来自验方: {formula.Name}"
                }).ToList();

                switch (mergeMode)
                {
                    case FormulaMergeMode.Replace:
                        _currentPrescriptionItems.Clear();
                        foreach (var item in formulaItems)
                        {
                            item.CalculateSubtotal();
                            _currentPrescriptionItems.Add(item);
                        }
                        break;

                    case FormulaMergeMode.Append:
                        foreach (var item in formulaItems)
                        {
                            item.CalculateSubtotal();
                            _currentPrescriptionItems.Add(item);
                        }
                        break;

                    case FormulaMergeMode.Merge:
                        foreach (var formulaItem in formulaItems)
                        {
                            var existingItem = _currentPrescriptionItems.FirstOrDefault(i => i.HerbId == formulaItem.HerbId);
                            if (existingItem != null)
                            {
                                existingItem.Quantity += formulaItem.Quantity;
                                existingItem.CalculateSubtotal();
                            }
                            else
                            {
                                formulaItem.CalculateSubtotal();
                                _currentPrescriptionItems.Add(formulaItem);
                            }
                        }
                        break;
                }

                // 触发处方项目变更事件
                PrescriptionItemsChanged?.Invoke(this, new PrescriptionItemsChangedEventArgs
                {
                    ConsultationId = consultationId,
                    Items = _currentPrescriptionItems.ToList(),
                    ChangeType = $"Merge Formula ({mergeMode})"
                });

                return ServiceResult<List<PrescriptionItemInfo>>.Success(_currentPrescriptionItems.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "合并验方到处方时发生异常，验方: {FormulaName}", formula.Name);
                return ServiceResult<List<PrescriptionItemInfo>>.Failure($"合并验方失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaInfo>> CreateCustomFormulaAsync(CustomFormulaCreateInfo formulaInfo)
        {
            try
            {
                var createDto = _mapper.Map<FormulaCreateDto>(formulaInfo);

                var response = await _formulaApiService.CreateAsync(createDto);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var formulaResult = _mapper.Map<FormulaInfo>(response.Content);
                    return ServiceResult<FormulaInfo>.Success(formulaResult);
                }

                return ServiceResult<FormulaInfo>.Failure("创建自定义验方失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建自定义验方时发生异常");
                return ServiceResult<FormulaInfo>.Failure($"创建自定义验方失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FormulaInfo>>> GetRecommendedFormulasAsync(string symptoms)
        {
            try
            {
                // 这里可以实现基于症状的验方推荐逻辑
                // 目前返回示例数据
                var formulas = await LoadFormulasAsync();
                if (!formulas.IsSuccess || formulas.Data == null)
                {
                    return ServiceResult<List<FormulaInfo>>.Failure("获取验方数据失败");
                }

                // 简单的关键词匹配推荐
                var recommended = formulas.Data
                    .Where(f => !string.IsNullOrEmpty(f.Description) && 
                               symptoms.Split('、', '，', ',')
                                      .Any(symptom => f.Description.Contains(symptom.Trim())))
                    .Take(10)
                    .ToList();

                return ServiceResult<List<FormulaInfo>>.Success(recommended);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐验方时发生异常，症状: {Symptoms}", symptoms);
                return ServiceResult<List<FormulaInfo>>.Failure($"获取推荐验方失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FormulaInfo>>> GetFrequentlyUsedFormulasAsync(Guid doctorId, int count = 10)
        {
            try
            {
                // 这里可以实现基于医生使用频率的验方推荐
                // 目前返回一般常用验方
                var formulas = await LoadFormulasAsync();
                if (!formulas.IsSuccess || formulas.Data == null)
                {
                    return ServiceResult<List<FormulaInfo>>.Failure("获取验方数据失败");
                }

                var frequentFormulas = formulas.Data
                    .Take(count)
                    .ToList();

                return ServiceResult<List<FormulaInfo>>.Success(frequentFormulas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取常用验方时发生异常，医生ID: {DoctorId}", doctorId);
                return ServiceResult<List<FormulaInfo>>.Failure($"获取常用验方失败: {ex.Message}");
            }
        }

        #endregion

        #region 数据载入与缓存

        public async Task<ServiceResult<List<PatientInfo>>> LoadPatientsAsync(bool forceRefresh = false)
        {
            try
            {
                if (forceRefresh)
                {
                    _cacheService.Remove(PATIENTS_CACHE_KEY);
                }

                var patients = await _cacheService.GetOrCreateAsync(PATIENTS_CACHE_KEY, async () =>
                {
                    _logger.LogInformation("从API加载患者列表");
                    var response = await _patientApiService.GetActivePatientsAsync();

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        var patientList = _mapper.Map<List<PatientInfo>>(response.Content);
                        _logger.LogInformation($"成功加载 {patientList.Count} 个患者");
                        return patientList;
                    }

                    _logger.LogWarning("加载患者列表失败，返回空列表");
                    return new List<PatientInfo>();
                }, PATIENTS_CACHE_DURATION);

                return ServiceResult<List<PatientInfo>>.Success(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载患者列表时发生异常");
                return ServiceResult<List<PatientInfo>>.Failure($"加载患者列表失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> LoadHerbsAsync(bool forceRefresh = false)
        {
            try
            {
                if (forceRefresh)
                {
                    _cacheService.Remove(HERBS_CACHE_KEY);
                }

                var herbs = await _cacheService.GetOrCreateAsync(HERBS_CACHE_KEY, async () =>
                {
                    _logger.LogInformation("从API加载中药材列表");
                    var response = await _herbApiService.GetActiveHerbsAsync();

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        var herbList = response.Content.ToList();
                        _logger.LogInformation($"成功加载 {herbList.Count} 个中药材");
                        return herbList;
                    }

                    _logger.LogWarning("加载中药材列表失败，返回空列表");
                    return new List<HerbDto>();
                }, HERBS_CACHE_DURATION);

                return ServiceResult<List<HerbDto>>.Success(herbs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载中药材列表时发生异常");
                return ServiceResult<List<HerbDto>>.Failure($"加载中药材列表失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<FormulaInfo>>> LoadFormulasAsync(bool forceRefresh = false)
        {
            try
            {
                if (forceRefresh)
                {
                    _cacheService.Remove(FORMULAS_CACHE_KEY);
                }

                var formulas = await _cacheService.GetOrCreateAsync(FORMULAS_CACHE_KEY, async () =>
                {
                    _logger.LogInformation("从API加载验方模板列表");
                    var response = await _formulaApiService.GetActiveFormulasAsync();

                    if (response.IsSuccessStatusCode && response.Content != null)
                    {
                        var formulaList = _mapper.Map<List<FormulaInfo>>(response.Content);
                        _logger.LogInformation($"成功加载 {formulaList.Count} 个验方模板");
                        return formulaList;
                    }

                    _logger.LogWarning("加载验方模板列表失败，返回空列表");
                    return new List<FormulaInfo>();
                }, FORMULAS_CACHE_DURATION);

                return ServiceResult<List<FormulaInfo>>.Success(formulas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载验方模板列表时发生异常");
                return ServiceResult<List<FormulaInfo>>.Failure($"加载验方模板列表失败: {ex.Message}");
            }
        }

        public void ClearAllCache()
        {
            _cacheService.Remove(PATIENTS_CACHE_KEY);
            _cacheService.Remove(HERBS_CACHE_KEY);
            _cacheService.Remove(FORMULAS_CACHE_KEY);
            _logger.LogInformation("已清除所有缓存");
        }

        public void ClearSpecificCache(string cacheType)
        {
            switch (cacheType.ToLower())
            {
                case "patients":
                    _cacheService.Remove(PATIENTS_CACHE_KEY);
                    break;
                case "herbs":
                    _cacheService.Remove(HERBS_CACHE_KEY);
                    break;
                case "formulas":
                    _cacheService.Remove(FORMULAS_CACHE_KEY);
                    break;
                default:
                    _logger.LogWarning("未知的缓存类型: {CacheType}", cacheType);
                    break;
            }
        }

        public CacheStatisticsInfo GetCacheStatistics()
        {
            // 这里需要根据实际的缓存服务实现来获取统计信息
            return new CacheStatisticsInfo
            {
                LastRefreshTime = DateTime.Now,
                Details = new Dictionary<string, object>
                {
                    ["CacheKeys"] = new[] { PATIENTS_CACHE_KEY, HERBS_CACHE_KEY, FORMULAS_CACHE_KEY }
                }
            };
        }

        #endregion

        #region 其他功能（示例实现）

        public async Task<ServiceResult<DoctorConsultationStatsInfo>> GetDoctorStatsAsync(Guid doctorId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var response = await _consultationApiService.GetConsultationsAsync(
                    doctorId: doctorId,
                    startDate: startDate,
                    endDate: endDate,
                    pageSize: 1000);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultations = _mapper.Map<List<ConsultationInfo>>(response.Content.Items);
                    
                    var stats = new DoctorConsultationStatsInfo
                    {
                        TotalConsultations = consultations.Count,
                        CompletedConsultations = consultations.Count(c => c.IsCompleted),
                        AverageConsultationTime = consultations.Where(c => c.Duration.HasValue).Average(c => c.Duration!.Value),
                        TopDiagnoses = consultations
                            .Where(c => !string.IsNullOrWhiteSpace(c.Diagnosis))
                            .GroupBy(c => c.Diagnosis)
                            .OrderByDescending(g => g.Count())
                            .Take(5)
                            .Select(g => g.Key)
                            .ToList()
                    };

                    return ServiceResult<DoctorConsultationStatsInfo>.Success(stats);
                }

                return ServiceResult<DoctorConsultationStatsInfo>.Failure("获取医生统计失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生统计时发生异常");
                return ServiceResult<DoctorConsultationStatsInfo>.Failure($"获取医生统计失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ConsultationInfo>>> GetPatientConsultationHistoryAsync(Guid patientId, int count = 10)
        {
            try
            {
                var response = await _consultationApiService.GetPatientHistoryAsync(patientId);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var consultations = _mapper.Map<List<ConsultationInfo>>(response.Content)
                        .Take(count)
                        .ToList();

                    return ServiceResult<List<ConsultationInfo>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationInfo>>.Failure("获取患者看诊历史失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者看诊历史时发生异常");
                return ServiceResult<List<ConsultationInfo>>.Failure($"获取患者看诊历史失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationReportInfo>> GenerateConsultationReportAsync(Guid consultationId)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult<ConsultationReportInfo>.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                
                var report = new ConsultationReportInfo
                {
                    ConsultationId = consultationId,
                    PatientName = consultation.PatientName,
                    DoctorName = consultation.DoctorName,
                    ConsultationTime = consultation.ConsultationTime,
                    ReportContent = $"看诊报告\n患者：{consultation.PatientName}\n医生：{consultation.DoctorName}\n时间：{consultation.ConsultationTimeText}\n诊断：{consultation.Diagnosis}\n",
                    ReportData = new Dictionary<string, object>
                    {
                        ["IsTCMComplete"] = consultation.IsTCMComplete,
                        ["IsVitalSignsComplete"] = consultation.IsVitalSignsComplete,
                        ["Duration"] = consultation.Duration
                    }
                };

                return ServiceResult<ConsultationReportInfo>.Success(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成看诊报告时发生异常");
                return ServiceResult<ConsultationReportInfo>.Failure($"生成看诊报告失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<byte[]>> ExportConsultationDataAsync(List<Guid> consultationIds, string format = "Excel")
        {
            try
            {
                // 这里应该实现实际的导出逻辑
                // 目前返回示例数据
                var exportData = System.Text.Encoding.UTF8.GetBytes("导出数据示例");
                return ServiceResult<byte[]>.Success(exportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出看诊数据时发生异常");
                return ServiceResult<byte[]>.Failure($"导出看诊数据失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationValidationInfo>> ValidateConsultationAsync(Guid consultationId)
        {
            try
            {
                var consultationResult = await GetByIdAsync(consultationId);
                if (!consultationResult.IsSuccess || consultationResult.Data == null)
                {
                    return ServiceResult<ConsultationValidationInfo>.Failure("获取看诊信息失败");
                }

                var consultation = consultationResult.Data;
                
                var validation = new ConsultationValidationInfo
                {
                    IsTCMComplete = consultation.IsTCMComplete,
                    IsVitalSignsComplete = consultation.IsVitalSignsComplete,
                    IsDiagnosisComplete = consultation.IsDiagnosisComplete,
                    ValidationErrors = new List<string>(),
                    ValidationWarnings = new List<string>()
                };

                if (string.IsNullOrWhiteSpace(consultation.Diagnosis))
                {
                    validation.ValidationErrors.Add("缺少诊断信息");
                }

                if (!consultation.IsTCMComplete)
                {
                    validation.ValidationWarnings.Add("中医四诊不完整");
                }

                if (!consultation.IsVitalSignsComplete)
                {
                    validation.ValidationWarnings.Add("生命体征不完整");
                }

                validation.IsValid = !validation.ValidationErrors.Any();

                return ServiceResult<ConsultationValidationInfo>.Success(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证看诊信息时发生异常");
                return ServiceResult<ConsultationValidationInfo>.Failure($"验证看诊信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<HerbStockWarningInfo>>> CheckHerbStockAsync(Guid consultationId)
        {
            try
            {
                var warnings = new List<HerbStockWarningInfo>();

                foreach (var item in _currentPrescriptionItems)
                {
                    // 这里应该检查实际的库存数据
                    // 目前返回示例警告
                    if (item.Quantity > 100) // 假设库存不足的条件
                    {
                        warnings.Add(new HerbStockWarningInfo
                        {
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            RequiredQuantity = item.Quantity,
                            AvailableStock = 50, // 示例库存
                            WarningType = "库存不足"
                        });
                    }
                }

                return ServiceResult<List<HerbStockWarningInfo>>.Success(warnings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查药材库存时发生异常");
                return ServiceResult<List<HerbStockWarningInfo>>.Failure($"检查药材库存失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ConsultationTemplateInfo>>> GetConsultationTemplatesAsync(string category)
        {
            try
            {
                // 这里应该从数据库或配置文件中获取模板
                // 目前返回示例模板
                var templates = new List<ConsultationTemplateInfo>
                {
                    new ConsultationTemplateInfo
                    {
                        Id = Guid.NewGuid(),
                        Name = "常规中医看诊模板",
                        Category = category,
                        Content = "标准中医四诊流程模板",
                        TemplateData = new Dictionary<string, object>
                        {
                            ["Steps"] = new[] { "望诊", "闻诊", "问诊", "切诊", "辨证", "立法", "处方" }
                        }
                    }
                };

                return ServiceResult<List<ConsultationTemplateInfo>>.Success(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊模板时发生异常");
                return ServiceResult<List<ConsultationTemplateInfo>>.Failure($"获取看诊模板失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> ApplyConsultationTemplateAsync(Guid consultationId, Guid templateId)
        {
            try
            {
                // 这里应该实现模板应用逻辑
                // 目前返回成功示例
                return ServiceResult.Success("成功应用看诊模板");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用看诊模板时发生异常");
                return ServiceResult.Failure($"应用看诊模板失败: {ex.Message}");
            }
        }

        #endregion
    }
}
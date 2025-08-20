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
    public class ConsultationModuleService
    {
        #region 依赖服务

        private readonly IConsultationApi _consultationApi;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationModuleService> _logger;

        #endregion

        #region 事件

        // UltraThink v2.0: 保留TCM数据更新事件
        public event EventHandler<TCMDataUpdatedEventArgs>? TCMDataUpdated;

        #endregion

        #region 构造函数

        public ConsultationModuleService(
            IConsultationApi consultationApi,
            IMapper mapper,
            ILogger<ConsultationModuleService> logger)
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

        public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("看诊ID不能为空");
                }

                _logger.LogInformation("获取看诊详情，ID: {ConsultationId}", id);

                // UltraThink v2.0: 使用新的API接口，返回的是DetailDto但可以转换
                var apiResult = await _consultationApi.GetByIdAsync(id);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDto>.Failure(
                        apiResult.Error?.Message ?? "获取看诊详情失败");
                }

                // UltraThink v2.0: 转换DetailDto为ConsultationDto
                var consultationDto = apiResult.Content.ToConsultationDto();
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<ConsultationDto>.Failure($"获取看诊详情失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("创建看诊记录，患者ID: {PatientId}, 医生ID: {DoctorId}", createDto.PatientId, createDto.DoctorId);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<ConsultationDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                // UltraThink v2.0: 转换为API专用DTO
                var startDto = _mapper.Map<ConsultationStartDto>(createDto);

                var apiResult = await _consultationApi.StartConsultationAsync(startDto);

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

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(ConsultationUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新看诊记录，ID: {ConsultationId}", updateDto.Id);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<ConsultationDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var apiResult = await _consultationApi.UpdateConsultationAsync(updateDto.Id, updateDto);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDto>.Failure(
                        apiResult.Error?.Message ?? "更新看诊记录失败");
                }

                // UltraThink v2.0: 转换DetailDto为ConsultationDto
                var consultationDto = apiResult.Content.ToConsultationDto();
                _logger.LogInformation("成功更新看诊记录，ID: {ConsultationId}", consultationDto.Id);
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊记录时发生异常，ID: {ConsultationId}", updateDto.Id);
                return ServiceResult<ConsultationDto>.Failure($"更新看诊记录失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("看诊ID不能为空");
                }

                _logger.LogInformation("删除看诊记录，ID: {ConsultationId}", id);

                var apiResult = await _consultationApi.DeleteAsync(id);

                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure(apiResult.Error?.Message ?? "删除看诊记录失败");
                }

                _logger.LogInformation("成功删除看诊记录，ID: {ConsultationId}", id);
                return ServiceResult.Success();
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

        #region 验证方法

        /// <summary>
        /// 验证创建看诊DTO
        /// </summary>
        private async Task<ServiceResult> ValidateCreateDtoAsync(ConsultationCreateDto createDto)
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
        /// 验证更新看诊DTO
        /// </summary>
        private async Task<ServiceResult> ValidateUpdateDtoAsync(ConsultationUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                    return ServiceResult.Failure("更新看诊信息不能为空");

                if (updateDto.Id == Guid.Empty)
                    return ServiceResult.Failure("看诊ID不能为空");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证更新看诊DTO异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新看诊状态
        /// </summary>
        private async Task<ServiceResult> UpdateStatusAsync(Guid id, ConsultationStatus status, string? remark = null)
        {
            try
            {
                // UltraThink v2.0: 使用UpdateStatusDto进行状态更新
                var updateStatusDto = new UpdateStatusDto
                {
                    Status = (LYBT.Shared.Models.Enums.ConsultationStatus)(int)status,
                    Reason = remark
                };

                var apiResult = await _consultationApi.UpdateStatusAsync(id, updateStatusDto);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure(apiResult.Error?.Message ?? "更新看诊状态失败");
                }

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"更新看诊状态异常: {ex.Message}");
            }
        }

        #endregion

        // UltraThink v2.0: 移除看诊工作流管理功能 - 工作流控制已移到MedicalCaseModule作为聚合根管理

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

        // UltraThink v2.0: 移除西医体征管理功能 - 专注中医诊断，删除所有西医指标

        #region 诊断管理

        // UltraThink v2.0: 移除老版诊断更新方法 - 已统一为UpdateDiagnosisAsync(ConsultationUpdateDto)

        // UltraThink v2.0: 移除AI诊断建议功能 - 删除过度设计的AI功能

        // UltraThink v2.0: 移除诊断验证功能 - 已整合到ValidateTCMCompletenessAsync中

        // UltraThink v2.0: 移除常用诊断统计功能 - 删除过度设计的统计功能

        #endregion

        // UltraThink v2.0: 移除处方管理功能 - 处方功能已独立到PrescriptionModule

        // UltraThink v2.0: 移除验方管理功能 - 验方功能已独立到FormulaModule

        // UltraThink v2.0: 移除缓存管理功能 - 删除过度设计的缓存系统

        #region 其他功能（示例实现）

        // UltraThink v2.0: 移除医生统计功能 - 删除过度设计的统计功能

        // UltraThink v2.0: 移除患者历史功能 - 历史查询已独立到MedicalCaseModule

        // UltraThink v2.0: 移除报告生成功能 - 删除过度设计的报告功能

        // UltraThink v2.0: 移除导出功能 - 删除过度设计的导出功能

        // UltraThink v2.0: 移除看诊验证功能 - 已整合到ValidateTCMCompletenessAsync中

        // UltraThink v2.0: 移除药材库存检查功能 - 库存管理已独立到HerbModule

        // UltraThink v2.0: 移除看诊模板功能 - 根据重构计划删除模板功能

        #endregion
    }
}
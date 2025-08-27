using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Modules.Consultation.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 看诊模块业务服务 - 纯数据记录版
    /// 只负责简单的数据录入、查询、更新、删除，不包含流程监管和智能处理
    /// </summary>
    public class ConsultationModule : LYBT.Shared.Interfaces.Services.IConsultationService
    {
        #region 依赖服务

        private readonly IConsultationApi _consultationApi;
        private readonly ILogger<ConsultationModule> _logger;

        #endregion

        #region 构造函数

        public ConsultationModule(
            IConsultationApi consultationApi,
            ILogger<ConsultationModule> logger)
        {
            _consultationApi = consultationApi ?? throw new ArgumentNullException(nameof(consultationApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 基本CRUD操作

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                _logger.LogInformation("获取分页看诊记录，页码: {PageIndex}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

                var apiResult = await _consultationApi.GetConsultationsAsync(
                    page: query.PageIndex,
                    pageSize: query.PageSize,
                    keyword: query.Keyword);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PagedResult<ConsultationDto>>.Failure(
                        apiResult.Error?.Message ?? "获取看诊记录失败");
                }

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

                var apiResult = await _consultationApi.GetByIdAsync(id);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure(
                        apiResult.Error?.Message ?? "获取看诊详情失败");
                }

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

                if (createDto.PatientId == Guid.Empty || createDto.DoctorId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("患者ID和医生ID不能为空");
                }

                var apiResult = await _consultationApi.StartConsultationAsync(createDto);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDto>.Failure(
                        apiResult.Error?.Message ?? "创建看诊记录失败");
                }

                // 简单转换
                var consultationDto = new ConsultationDto
                {
                    Id = apiResult.Content.Id,
                    MedicalCaseId = apiResult.Content.MedicalCaseId,
                    PatientId = apiResult.Content.PatientId,
                    UserId = apiResult.Content.DoctorId,
                    ConsultationTime = apiResult.Content.ConsultationTime,
                    ChiefComplaint = apiResult.Content.ChiefComplaint,
                    Inspection = apiResult.Content.Inspection,
                    Auscultation = apiResult.Content.AuscultationOlfaction,
                    Inquiry = apiResult.Content.Inquiry,
                    Palpation = apiResult.Content.Palpation,
                    Diagnosis = apiResult.Content.Diagnosis,
                    Remark = apiResult.Content.Remark,
                    Status = (LYBT.Shared.Models.Enums.CommonStatus)(int)apiResult.Content.Status,
                    CreateTime = apiResult.Content.CreateTime,
                    UpdateTime = apiResult.Content.UpdateTime
                };
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

                if (id == Guid.Empty || updateDto == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("更新参数不能为空");
                }

                // 简单转换
                var updateRequestDto = new ConsultationUpdateDto
                {
                    Id = id,
                    ChiefComplaint = updateDto.ChiefComplaint,
                    Inspection = updateDto.Inspection,
                    AuscultationOlfaction = updateDto.AuscultationOlfaction,
                    Inquiry = updateDto.Inquiry,
                    Palpation = updateDto.Palpation,
                    Diagnosis = updateDto.Diagnosis,
                    Remark = updateDto.Remark,
                    PatientId = updateDto.PatientId,
                    DoctorId = updateDto.DoctorId
                };

                var apiResult = await _consultationApi.UpdateConsultationAsync(id, updateRequestDto);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<ConsultationDto>.Failure(
                        apiResult.Error?.Message ?? "更新看诊记录失败");
                }

                // 简单转换返回结果
                var consultationDto = new ConsultationDto
                {
                    Id = apiResult.Content.Id,
                    MedicalCaseId = apiResult.Content.MedicalCaseId,
                    PatientId = apiResult.Content.PatientId,
                    UserId = apiResult.Content.DoctorId,
                    ConsultationTime = apiResult.Content.ConsultationTime,
                    ChiefComplaint = apiResult.Content.ChiefComplaint,
                    Inspection = apiResult.Content.Inspection,
                    Auscultation = apiResult.Content.AuscultationOlfaction,
                    Inquiry = apiResult.Content.Inquiry,
                    Palpation = apiResult.Content.Palpation,
                    Diagnosis = apiResult.Content.Diagnosis,
                    Remark = apiResult.Content.Remark,
                    Status = (LYBT.Shared.Models.Enums.CommonStatus)(int)apiResult.Content.Status,
                    CreateTime = apiResult.Content.CreateTime,
                    UpdateTime = apiResult.Content.UpdateTime
                };
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

        #endregion

        #region 简化的患者历史查询

        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            // 简化：直接调用GetByPatientIdAsync方法
            return await GetByPatientIdAsync(patientId);
        }

        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                _logger.LogInformation("根据患者ID获取看诊记录，患者ID: {PatientId}", patientId);

                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = patientId.ToString()
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    var consultations = result.Data.Items
                        .Where(c => c.PatientId == patientId)
                        .OrderByDescending(c => c.CreateTime)
                        .ToList();

                    return ServiceResult<List<ConsultationDto>>.Success(consultations);
                }

                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
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
                        .Where(c => c.UserId == doctorId)
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

        #endregion

        #region 简化接口实现

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
            // 简化：只检查ID是否有效
            return id != Guid.Empty 
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure("无效的ID");
        }

        public async Task<ServiceResult<bool>> CanModifyAsync(Guid id)
        {
            // 简化：只检查ID是否有效
            return id != Guid.Empty 
                ? ServiceResult<bool>.Success(true)
                : ServiceResult<bool>.Failure("无效的ID");
        }

        public async Task<ServiceResult> UpdateDiagnosisAsync(Guid consultationId, ConsultationUpdateDto diagnosisData)
        {
            // 简化：直接调用更新方法，不做复杂事件处理
            try
            {
                _logger.LogInformation("更新诊断信息，ID: {ConsultationId}", consultationId);

                var apiResult = await _consultationApi.UpdateConsultationAsync(consultationId, diagnosisData);
                
                return apiResult.IsSuccessStatusCode 
                    ? ServiceResult.Success("诊断信息更新成功")
                    : ServiceResult.Failure(apiResult.Error?.Message ?? "更新诊断信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊断信息时发生异常，ID: {ConsultationId}", consultationId);
                return ServiceResult.Failure($"更新诊断信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            // 简化：直接调用API，不做复杂流程管理
            try
            {
                var apiResult = await _consultationApi.CompleteConsultationAsync(id, dto);
                return apiResult.IsSuccessStatusCode 
                    ? ServiceResult<bool>.Success(true)
                    : ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "完成看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成看诊时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"完成看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            // 简化：直接调用API，不做复杂流程管理
            try
            {
                var apiResult = await _consultationApi.CancelConsultationAsync(id, reason);
                return apiResult.IsSuccessStatusCode 
                    ? ServiceResult<bool>.Success(true)
                    : ServiceResult<bool>.Failure(apiResult.Error?.Message ?? "取消看诊失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消看诊时发生异常，ID: {ConsultationId}", id);
                return ServiceResult<bool>.Failure($"取消看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            // 简化：直接调用API，不做复杂分析
            try
            {
                var apiResult = await _consultationApi.GetStatisticsAsync(startDate, endDate);
                return apiResult.IsSuccessStatusCode && apiResult.Content != null
                    ? ServiceResult<object>.Success(apiResult.Content)
                    : ServiceResult<object>.Failure(apiResult.Error?.Message ?? "获取统计信息失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊统计信息时发生异常");
                return ServiceResult<object>.Failure($"获取统计信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            // 简化：直接返回看诊记录中的四诊数据
            try
            {
                var consultationsResult = await GetByMedicalCaseIdAsync(medicalCaseId);
                if (!consultationsResult.IsSuccess || !consultationsResult.Data?.Any() == true)
                {
                    return ServiceResult<object>.Failure("未找到对应的看诊记录");
                }

                var consultation = consultationsResult.Data.First();
                var fourDiagnosis = new
                {
                    Inspection = consultation.Inspection,
                    Auscultation = consultation.Auscultation,
                    Inquiry = consultation.Inquiry,
                    Palpation = consultation.Palpation
                };

                return ServiceResult<object>.Success(fourDiagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取四诊数据失败，医疗案例ID: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<object>.Failure($"获取四诊数据失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
        {
            // 简化：直接调用更新方法
            try
            {
                var updateDto = new ConsultationUpdateDto
                {
                    Id = consultationId,
                    Inspection = fourDiagnosisData?.ToString() ?? "",
                    AuscultationOlfaction = "",
                    Inquiry = "",
                    Palpation = ""
                };

                var result = await UpdateDiagnosisAsync(consultationId, updateDto);
                return ServiceResult<bool>.Success(result.IsSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存四诊数据失败，看诊ID: {ConsultationId}", consultationId);
                return ServiceResult<bool>.Failure($"保存四诊数据失败: {ex.Message}");
            }
        }

        #endregion
    }
}
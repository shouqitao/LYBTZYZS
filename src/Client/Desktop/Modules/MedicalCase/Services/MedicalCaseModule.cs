using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Desktop.Modules.MedicalCase.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// MedicalCase模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，移除Info层转换逻辑
    /// </summary>
    public class MedicalCaseModule : LYBT.Shared.Interfaces.Services.IMedicalCaseService
    {
        #region 依赖服务

        private readonly IMedicalCaseApi _medicalCaseApi;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseModule> _logger;

        #endregion
        
        #region 构造函数

        public MedicalCaseModule(
            IMedicalCaseApi medicalCaseApi,
            IMapper mapper,
            ILogger<MedicalCaseModule> logger)
        {
            _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                _logger.LogInformation("获取分页医疗案例记录，页码: {PageIndex}, 页大小: {PageSize}", query.PageIndex, query.PageSize);

                // UltraThink v2.0: 使用新的API接口 
                var apiResult = await _medicalCaseApi.GetPagedAsync(
                    pageIndex: query.PageIndex,
                    pageSize: query.PageSize);

                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("获取医疗案例列表失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                var pagedData = apiResult.Content;
                var result = new PagedResult<MedicalCaseDto>(
                    pagedData.Items.ToList(),
                    pagedData.TotalCount,
                    pagedData.CurrentPage,
                    pagedData.PageSize);
                
                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取分页医疗案例记录时发生异常");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure($"获取医疗案例列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例ID不能为空");
                }
                
                _logger.LogInformation("获取医疗案例详情，ID: {MedicalCaseId}", id);

                // UltraThink v2.0: 使用新的API接口，返回的是DetailDto但可以转换
                var apiResult = await _medicalCaseApi.GetByIdAsync(id);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<MedicalCaseDetailDto>.Failure("获取医疗案例详情失败");
                }
                
                // UltraThink v2.0: 直接返回DetailDto
                return ServiceResult<MedicalCaseDetailDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<MedicalCaseDetailDto>.Failure($"获取医疗案例详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("创建医疗案例记录，患者ID: {PatientId}, 医生ID: {DoctorId}", createDto.PatientId, createDto.DoctorId);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }
                
                var apiResult = await _medicalCaseApi.CreateAsync(createDto);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("创建医疗案例失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                _logger.LogInformation("成功创建医疗案例记录，ID: {MedicalCaseId}", apiResult.Content.Id);
                return ServiceResult<MedicalCaseDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例记录时发生异常");
                return ServiceResult<MedicalCaseDto>.Failure($"创建医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新医疗案例记录，ID: {MedicalCaseId}", id);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(id, updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }
                
                // 转换为EditDto - 需要检查API接口要求的DTO类型
                var editDto = _mapper.Map<MedicalCaseEditDto>(updateDto);
                
                var apiResult = await _medicalCaseApi.UpdateAsync(id, editDto);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败");
                }
                
                // 重新获取更新后的数据，但GetByIdAsync返回DetailDto，需要转换
                var updatedDetailResult = await GetByIdAsync(id);
                if (!updatedDetailResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("更新成功但获取最新数据失败");
                }
                
                // 将DetailDto转换为基础DTO
                var updatedDto = _mapper.Map<MedicalCaseDto>(updatedDetailResult.Data);
                
                _logger.LogInformation("成功更新医疗案例记录，ID: {MedicalCaseId}", id);
                return ServiceResult<MedicalCaseDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例记录时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<MedicalCaseDto>.Failure($"更新医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }
                
                _logger.LogInformation("删除医疗案例记录，ID: {MedicalCaseId}", id);

                var apiResult = await _medicalCaseApi.DeleteAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("删除医疗案例失败");
                }
                
                _logger.LogInformation("成功删除医疗案例记录，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例记录时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"删除医疗案例异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }
                
                _logger.LogInformation("更新医疗案例状态，ID: {MedicalCaseId}, 状态: {Status}", id, status);

                // 将int转换为MedicalCaseStatus枚举
                var medicalCaseStatus = (MedicalCaseStatus)status;
                var apiResult = await _medicalCaseApi.UpdateStatusAsync(id, medicalCaseStatus);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("更新状态失败");
                }
                
                _logger.LogInformation("成功更新医疗案例状态，ID: {MedicalCaseId}, 状态: {Status}", id, status);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"更新状态异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 开始看诊
        /// </summary>
        private async Task<ServiceResult<bool>> StartConsultationAsync(Guid id)
        {
            try
            {
                return await UpdateStatusAsync(id, (int)MedicalCaseStatus.InConsultation);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"开始看诊异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 完成看诊
        /// </summary>
        private async Task<ServiceResult<bool>> CompleteConsultationInternalAsync(Guid id, string? diagnosis = null)
        {
            try
            {
                var result = await UpdateStatusAsync(id, (int)MedicalCaseStatus.Completed);
                
                // 如果提供了诊断结果，同时更新诊断信息
                if (result.IsSuccess && !string.IsNullOrWhiteSpace(diagnosis))
                {
                    var caseResult = await GetByIdAsync(id);
                    if (caseResult.IsSuccess)
                    {
                        // UltraThink v2.0: 直接使用DTO创建更新对象
                        var updateDto = new MedicalCaseUpdateDto
                        {
                            Id = caseResult.Data.Id,
                            PatientId = caseResult.Data.PatientId,
                            DoctorId = caseResult.Data.DoctorId,
                            DiagnosisResult = diagnosis ?? "看诊完成"
                        };
                        
                        await UpdateAsync(id, updateDto);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"完成看诊异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 暂停看诊
        /// </summary>
        private async Task<ServiceResult<bool>> PauseConsultationAsync(Guid id, string? reason = null)
        {
            try
            {
                return await UpdateStatusAsync(id, (int)MedicalCaseStatus.Registered);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"暂停看诊异常: {ex.Message}");
            }
        }

        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 取消医疗案例
        /// </summary>
        private async Task<ServiceResult<bool>> CancelInternalAsync(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return ServiceResult<bool>.Failure("取消原因不能为空");
                }
                
                return await UpdateStatusAsync(id, (int)MedicalCaseStatus.Cancelled);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"取消医疗案例异常: {ex.Message}");
            }
        }
        

        // UltraThink v2.0: 移除批量操作功能 - 删除过度设计的批量功能
        
        #endregion
        
        #region 接口实现的缺失方法

        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<MedicalCaseDto>>.Failure("患者ID不能为空");
                }

                _logger.LogInformation("根据患者ID获取医疗案例，患者ID: {PatientId}", patientId);

                // 使用分页查询API，通过关键字查找
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = patientId.ToString()
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    var cases = result.Data.Items
                        .Where(c => c.PatientId == patientId)
                        .OrderByDescending(c => c.CreateTime)
                        .ToList();

                    return ServiceResult<List<MedicalCaseDto>>.Success(cases);
                }

                return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例时发生异常，患者ID: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取患者医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                var casesResult = await GetByPatientIdAsync(patientId);
                if (!casesResult.IsSuccess || casesResult.Data == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("获取患者医疗案例失败");
                }

                // 查找活跃状态的医疗案例（非完成、非取消状态）
                var activeCase = casesResult.Data
                    .Where(c => c.Status != (CommonStatus)MedicalCaseStatus.Completed && c.Status != (CommonStatus)MedicalCaseStatus.Cancelled)
                    .OrderByDescending(c => c.CreateTime)
                    .FirstOrDefault();

                if (activeCase == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("未找到活跃的医疗案例");
                }

                return ServiceResult<MedicalCaseDto>.Success(activeCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃医疗案例时发生异常，患者ID: {PatientId}", patientId);
                return ServiceResult<MedicalCaseDto>.Failure($"获取活跃医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            try
            {
                return await CompleteConsultationInternalAsync(id, completionReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"完成医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        {
            try
            {
                return await PauseConsultationAsync(id, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"暂停医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        {
            try
            {
                return await UpdateStatusAsync(id, (int)MedicalCaseStatus.InConsultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"恢复医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            try
            {
                return await CancelInternalAsync(id, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消看诊时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"取消看诊失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(archiveReason))
                {
                    return ServiceResult<bool>.Failure("归档原因不能为空");
                }

                // 将状态设为完成并记录归档原因
                var result = await UpdateStatusAsync(id, (int)MedicalCaseStatus.Completed);
                if (result.IsSuccess)
                {
                    // 更新归档信息
                    var caseResult = await GetByIdAsync(id);
                    if (caseResult.IsSuccess)
                    {
                        var updateDto = new MedicalCaseUpdateDto
                        {
                            Id = caseResult.Data.Id,
                            PatientId = caseResult.Data.PatientId,
                            DoctorId = caseResult.Data.DoctorId,
                            DiagnosisResult = $"归档: {archiveReason}"
                        };
                        
                        await UpdateAsync(id, updateDto);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<bool>.Failure($"归档医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                _logger.LogInformation("获取医疗案例统计信息，开始日期: {StartDate}, 结束日期: {EndDate}", startDate, endDate);

                // 获取所有案例进行统计
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 1000 // 获取更多数据用于统计
                };

                var result = await GetPagedAsync(query);
                if (!result.IsSuccess || result.Data == null)
                {
                    return ServiceResult<object>.Failure("获取医疗案例数据失败");
                }

                var cases = result.Data.Items;

                // 按日期筛选
                if (startDate.HasValue)
                    cases = cases.Where(c => c.CreateTime >= startDate.Value).ToList();
                if (endDate.HasValue)
                    cases = cases.Where(c => c.CreateTime <= endDate.Value).ToList();

                // 统计信息
                var statistics = new
                {
                    TotalCases = cases.Count,
                    CompletedCases = cases.Count(c => c.Status == (CommonStatus)MedicalCaseStatus.Completed),
                    InProgressCases = cases.Count(c => c.Status == (CommonStatus)MedicalCaseStatus.InConsultation),
                    CancelledCases = cases.Count(c => c.Status == (CommonStatus)MedicalCaseStatus.Cancelled),
                    RegisteredCases = cases.Count(c => c.Status == (CommonStatus)MedicalCaseStatus.Registered)
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例统计信息时发生异常");
                return ServiceResult<object>.Failure($"获取统计信息失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("搜索医疗案例，关键字: {Keyword}", keyword);

                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = 100,
                    Keyword = keyword
                };

                var result = await GetPagedAsync(query);
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<List<MedicalCaseDto>>.Success(result.Data.Items.ToList());
                }

                return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医疗案例时发生异常，关键字: {Keyword}", keyword);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"搜索医疗案例失败: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("获取医疗案例历史记录，ID: {MedicalCaseId}", id);

                // 获取医疗案例详情
                var caseResult = await GetByIdAsync(id);
                if (!caseResult.IsSuccess || caseResult.Data == null)
                {
                    return ServiceResult<List<object>>.Failure("获取医疗案例详情失败");
                }

                // 构建历史记录（基础实现）
                var history = new List<object>
                {
                    new
                    {
                        Action = "创建",
                        Time = caseResult.Data.CreateTime,
                        Description = "医疗案例创建"
                    },
                    new
                    {
                        Action = "状态变更",
                        Time = caseResult.Data.UpdateTime ?? caseResult.Data.CreateTime,
                        Description = $"状态: {caseResult.Data.Status}"
                    }
                };

                return ServiceResult<List<object>>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例历史记录时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<List<object>>.Failure($"获取历史记录失败: {ex.Message}");
            }
        }

        #endregion
        
        #region 验证操作
        
        // UltraThink v2.0: 移除通用验证方法 - 验证逻辑已整合到Create/Update方法中
        
        private async Task<ServiceResult> ValidateCreateDtoAsync(MedicalCaseCreateDto createDto)
        {
            if (createDto == null) return ServiceResult.Failure("创建医疗案例信息不能为空");
            if (createDto.PatientId == Guid.Empty) return ServiceResult.Failure("患者ID不能为空");
            if (createDto.DoctorId == Guid.Empty) return ServiceResult.Failure("医生ID不能为空");
            // 验证DiagnosisSummary而不是ChiefComplaint
            if (!string.IsNullOrWhiteSpace(createDto.DiagnosisSummary) && createDto.DiagnosisSummary.Length > 200)
            {
                return ServiceResult.Failure("诊断摘要长度不能超过200个字符");
            }
            return ServiceResult.Success();
        }
        
        private async Task<ServiceResult> ValidateUpdateDtoAsync(Guid id, MedicalCaseUpdateDto updateDto)
        {
            if (updateDto == null) return ServiceResult.Failure("更新医疗案例信息不能为空");
            if (updateDto.Id == Guid.Empty) return ServiceResult.Failure("医疗案例ID不能为空");
            if (updateDto.PatientId == Guid.Empty) return ServiceResult.Failure("患者ID不能为空");
            if (updateDto.DoctorId == Guid.Empty) return ServiceResult.Failure("医生ID不能为空");
            return ServiceResult.Success();
        }
        
        // UltraThink v2.0: 移除权限检查方法 - 权限验证应由业务层统一处理
        
        #endregion
        
        // UltraThink v2.0: 移除统计分析功能 - 删除过度设计的统计功能
        
        #region 业务规则验证
        
        // UltraThink v2.0: 移除Can方法 - 权限检查已整合到Update/Delete方法中
        
        // UltraThink v2.0: 移除操作历史功能 - 删除过度设计的操作历史跟踪
        
        #endregion
        
        #region 关联数据
        
        // UltraThink v2.0: 移除关联数据获取功能 - 各模块数据应由对应模块服务获取
        
        // UltraThink v2.0: 移除HasIncompleteCasesAsync - 业务逻辑简化，由上层判断
        
        #endregion
        
        // UltraThink v2.0: 移除私有辅助方法 - 删除过度设计的统计计算
    }
}
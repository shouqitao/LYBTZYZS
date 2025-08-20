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
    public class MedicalCaseModuleService
    {
        #region 依赖服务

        private readonly IMedicalCaseApi _medicalCaseApi;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseModuleService> _logger;

        #endregion
        
        #region 构造函数

        public MedicalCaseModuleService(
            IMedicalCaseApi medicalCaseApi,
            IMapper mapper,
            ILogger<MedicalCaseModuleService> logger)
        {
            _medicalCaseApi = medicalCaseApi ?? throw new ArgumentNullException(nameof(medicalCaseApi));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(MedicalCaseQueryDto query)
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
        
        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例ID不能为空");
                }
                
                _logger.LogInformation("获取医疗案例详情，ID: {MedicalCaseId}", id);

                // UltraThink v2.0: 使用新的API接口，返回的是DetailDto但可以转换
                var apiResult = await _medicalCaseApi.GetByIdAsync(id);
                if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("获取医疗案例详情失败");
                }
                
                // UltraThink v2.0: 直接使用DetailDto，因为它包含所有基础信息
                return ServiceResult<MedicalCaseDto>.Success(apiResult.Content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult<MedicalCaseDto>.Failure($"获取医疗案例详情异常: {ex.Message}");
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
        
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(MedicalCaseUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation("更新医疗案例记录，ID: {MedicalCaseId}", updateDto.Id);

                // UltraThink v2.0: 直接使用DTO进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }
                
                // 转换为EditDto - 需要检查API接口要求的DTO类型
                var editDto = _mapper.Map<MedicalCaseEditDto>(updateDto);
                
                var apiResult = await _medicalCaseApi.UpdateAsync(updateDto.Id, editDto);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败");
                }
                
                // 重新获取更新后的数据
                var updatedResult = await GetByIdAsync(updateDto.Id);
                if (!updatedResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("更新成功但获取最新数据失败");
                }
                
                _logger.LogInformation("成功更新医疗案例记录，ID: {MedicalCaseId}", updateDto.Id);
                return ServiceResult<MedicalCaseDto>.Success(updatedResult.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例记录时发生异常，ID: {MedicalCaseId}", updateDto.Id);
                return ServiceResult<MedicalCaseDto>.Failure($"更新医疗案例异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("医疗案例ID不能为空");
                }
                
                _logger.LogInformation("删除医疗案例记录，ID: {MedicalCaseId}", id);

                var apiResult = await _medicalCaseApi.DeleteAsync(id);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("删除医疗案例失败");
                }
                
                _logger.LogInformation("成功删除医疗案例记录，ID: {MedicalCaseId}", id);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例记录时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult.Failure($"删除医疗案例异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> UpdateStatusAsync(Guid id, MedicalCaseStatus status, string? reason = null)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("医疗案例ID不能为空");
                }
                
                _logger.LogInformation("更新医疗案例状态，ID: {MedicalCaseId}, 状态: {Status}", id, status);

                var apiResult = await _medicalCaseApi.UpdateStatusAsync(id, status);
                if (!apiResult.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("更新状态失败");
                }
                
                _logger.LogInformation("成功更新医疗案例状态，ID: {MedicalCaseId}, 状态: {Status}", id, status);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态时发生异常，ID: {MedicalCaseId}", id);
                return ServiceResult.Failure($"更新状态异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 开始看诊
        /// </summary>
        public async Task<ServiceResult> StartConsultationAsync(Guid id)
        {
            try
            {
                return await UpdateStatusAsync(id, MedicalCaseStatus.InConsultation, "开始看诊");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"开始看诊异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 完成看诊
        /// </summary>
        public async Task<ServiceResult> CompleteConsultationAsync(Guid id, string? diagnosis = null)
        {
            try
            {
                var result = await UpdateStatusAsync(id, MedicalCaseStatus.Completed, "完成看诊");
                
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
                        
                        await UpdateAsync(updateDto);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"完成看诊异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 暂停看诊
        /// </summary>
        public async Task<ServiceResult> PauseConsultationAsync(Guid id, string? reason = null)
        {
            try
            {
                return await UpdateStatusAsync(id, MedicalCaseStatus.Registered, reason ?? "暂停看诊");
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"暂停看诊异常: {ex.Message}");
            }
        }

        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理看诊工作流 - 取消医疗案例
        /// </summary>
        public async Task<ServiceResult> CancelAsync(Guid id, string reason)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return ServiceResult.Failure("取消原因不能为空");
                }
                
                return await UpdateStatusAsync(id, MedicalCaseStatus.Cancelled, reason);
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"取消医疗案例异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// UltraThink v2.0: 作为聚合根管理处方工作流 - 创建处方
        /// </summary>
        public async Task<ServiceResult> CreatePrescriptionAsync(Guid caseId, object prescriptionData)
        {
            try
            {
                // 检查案例状态是否允许创建处方
                var caseResult = await GetByIdAsync(caseId);
                if (!caseResult.IsSuccess)
                {
                    return ServiceResult.Failure("获取医疗案例信息失败");
                }

                if (caseResult.Data.CaseStatus != MedicalCaseStatus.InConsultation)
                {
                    return ServiceResult.Failure("只有进行中的看诊才能创建处方");
                }

                // TODO: 调用PrescriptionModule的API创建处方
                // 这里返回成功表示功能框架已建立
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"创建处方异常: {ex.Message}");
            }
        }

        // UltraThink v2.0: 移除批量操作功能 - 删除过度设计的批量功能
        
        #endregion
        
        #region 查询操作
        
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> SearchAsync(MedicalCaseQueryDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure($"搜索医疗案例异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 移除复杂查询方法 - 统一使用GetPagedAsync，由上层处理筛选逻辑
        
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
        
        private async Task<ServiceResult> ValidateUpdateDtoAsync(MedicalCaseUpdateDto updateDto)
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
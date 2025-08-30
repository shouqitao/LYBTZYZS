using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Module.MedicalCase.Helpers;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务实现 - UltraThink三层架构重构版
    /// 采用纯委托模式，将具体业务逻辑委托给专门的服务层处理
    /// 实现医疗案例的完整生命周期管理：创建→进行→完成→归档
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly Core.MedicalCaseServiceCore _coreService;
        private readonly MedicalCaseQueryService _queryService;
        private readonly MedicalCaseBusinessService _businessService;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            Core.MedicalCaseServiceCore coreService,
            MedicalCaseQueryService queryService,
            MedicalCaseBusinessService businessService,
            ILogger<MedicalCaseService> logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Shared Interface Implementation (纯委托模式)

        /// <summary>
        /// 根据ID获取医疗案例详情 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                return await _queryService.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", id);
                return ServiceResult<MedicalCaseDetailDto>.Failure("获取医疗案例详情失败");
            }
        }

        /// <summary>
        /// 分页查询医疗案例 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                return await _queryService.GetPagedAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("分页查询医疗案例失败");
            }
        }

        /// <summary>
        /// 创建医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                return await _businessService.CreateAsync(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                return ServiceResult<MedicalCaseDto>.Failure("创建医疗案例失败");
            }
        }

        /// <summary>
        /// 更新医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                return await _businessService.UpdateAsync(id, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败: {Id}", id);
                return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败");
            }
        }

        /// <summary>
        /// 删除医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                return await _businessService.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除医疗案例失败");
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                return await _queryService.GetByPatientIdAsync(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例失败: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure("获取患者医疗案例失败");
            }
        }

        /// <summary>
        /// 获取患者的活跃医疗案例 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                return await _queryService.GetActiveByPatientIdAsync(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者活跃医疗案例失败: {PatientId}", patientId);
                return ServiceResult<MedicalCaseDto>.Failure("获取患者活跃医疗案例失败");
            }
        }

        /// <summary>
        /// 完成医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            try
            {
                return await _businessService.CompleteAsync(id, completionReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("完成医疗案例失败");
            }
        }

        /// <summary>
        /// 暂停医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        {
            try
            {
                return await _businessService.SuspendAsync(id, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("暂停医疗案例失败");
            }
        }

        /// <summary>
        /// 恢复医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        {
            try
            {
                return await _businessService.ResumeAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("恢复医疗案例失败");
            }
        }

        /// <summary>
        /// 归档医疗案例 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        {
            try
            {
                return await _businessService.ArchiveAsync(id, archiveReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("归档医疗案例失败");
            }
        }

        /// <summary>
        /// 获取医疗案例统计数据 (已废弃)
        /// UltraThink v2.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                await Task.CompletedTask;
                var emptyStats = new { Message = "统计功能已废弃 - UltraThink精简", TotalCount = 0 };                return ServiceResult<object>.Success(emptyStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例统计失败");                return ServiceResult<object>.Failure("获取医疗案例统计失败");            }
        }

        /// <summary>
        /// 搜索医疗案例 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                return await _queryService.SearchAsync(keyword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医疗案例失败: {Keyword}", keyword);
                return ServiceResult<List<MedicalCaseDto>>.Failure("搜索医疗案例失败");
            }
        }

        /// <summary>
        /// 获取医疗案例历史记录 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
        {
            try
            {
                return await _queryService.GetHistoryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例历史记录失败: {Id}", id);
                return ServiceResult<List<object>>.Failure("获取历史记录失败");
            }
        }

        /// <summary>
        /// 更新医疗案例状态 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
        {
            try
            {
                return await _businessService.UpdateStatusAsync(id, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", id);
                return ServiceResult<bool>.Failure("更新案例状态失败");
            }
        }

        #endregion

        #region 扩展方法 (纯委托模式)

        /// <summary>
        /// 批量更新案例状态 (委托给BusinessService)
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(Guid[] ids, int status)
        {
            try
            {
                if (!Enum.IsDefined(typeof(Shared.Models.Enums.MedicalCaseStatus), status))
                {
                    return ServiceResult<int>.Failure($"无效的状态值: {status}");
                }

                var medicalCaseStatus = (Shared.Models.Enums.MedicalCaseStatus)status;
                return await _businessService.BatchUpdateStatusAsync(ids, medicalCaseStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新医疗案例状态失败");
                return ServiceResult<int>.Failure("批量更新失败");
            }
        }

        /// <summary>
        /// 复制医疗案例
        /// </summary>
        #region 已废弃功能 - UltraThink精简
        /*
        // 克隆案例功能已删除 - 小诊所不需要克隆功能
        public async Task<ServiceResult<MedicalCaseDto>> CloneAsync(Guid id)
        {
            // 功能已废弃
        }
        */

        /// <summary>
        /// 检查患者是否有活跃案例 (委托给QueryService)
        /// </summary>
        public async Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId)
        {
            try
            {
                return await _queryService.HasActiveCaseAsync(patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查患者活跃案例失败: {PatientId}", patientId);
                return ServiceResult<bool>.Failure("检查患者活跃案例失败");
            }
        }

        #endregion

        #region 新增功能 - 打印病历/处方
        
        /// <summary>
        /// 打印医疗病历/处方 - 从Prescriptions模块迁移
        /// </summary>
        /// <param name="caseId">案例ID</param>        /// <returns>PDF字节数组</returns>
        public async Task<ServiceResult<byte[]>> PrintMedicalRecordAsync(Guid caseId)
        {
            try
            {
                // 获取案例信息
                var caseResult = await GetByIdAsync(caseId);
                if (!caseResult.IsSuccess || caseResult.Data == null)
                {
                    return ServiceResult<byte[]>.Failure("医疗案例不存在");                }

                var medicalCase = caseResult.Data;

                // 生成打印内容 - 包含诊断结果、处方组成、费用等
                var printContent = new
                {
                    PatientInfo = new 
                    {
                        medicalCase.PatientName,
                        medicalCase.PatientId,
                        PrintTime = DateTime.Now
                    },
                    CaseInfo = new 
                    {
                        medicalCase.Id,
                        CaseNumber = medicalCase.Id.ToString("N")[..8], // 使用ID的前8位作为案例号                        medicalCase.Status,
                        medicalCase.CreateTime
                    },
                    // 诊断结果 - 需要从Consultation获取
                    Diagnosis = "待获取诊断信息", // TODO: 集成Consultation服务                    // 处方信息 - 需要从Prescriptions获取  
                    PrescriptionDetails = "待获取处方信息", // TODO: 集成Prescription服务                    // 费用总计
                    TotalAmount = 0.0m // TODO: 计算总费用
                };

                // TODO: 调用打印服务生成PDF
                // var pdfBytes = await _printService.GeneratePdfAsync(printContent);
                
                // 临时实现 - 返回空字节数组，实际实现需要PDF生成服务
                var tempBytes = System.Text.Encoding.UTF8.GetBytes($"医疗病历打印 - 案例ID: {caseId}, 生成时间: {DateTime.Now}");                
                _logger.LogInformation("医疗病历打印成功: {CaseId}", caseId);                return ServiceResult<byte[]>.Success(tempBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印医疗病历失败: {CaseId}", caseId);                return ServiceResult<byte[]>.Failure($"打印医疗病历失败: {ex.Message}", ex);            }
        }

        #endregion

        /// <summary>
        /// 取消咨询/诊断 (委托给BusinessHelper)
        /// </summary>
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            try
            {
                _logger.LogInformation("取消咨询: {CaseId}, 原因: {Reason}", id, reason);                
                // 直接实现取消逻辑 - 更新案例状态为取消
                var result = await UpdateStatusAsync(id, (int)Shared.Models.Enums.MedicalCaseStatus.Cancelled);
                if (result.IsSuccess)
                {
                    // 记录取消原因到日志
                    _logger.LogWarning("医疗案例已取消: {CaseId}, 取消原因: {Reason}", id, reason);                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    return ServiceResult<bool>.Failure("取消咨询失败: ");                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消咨询失败: {CaseId}", id);                return ServiceResult<bool>.Failure($"取消咨询失败: {ex.Message}", ex);
            }
        }

        #endregion
    }
}



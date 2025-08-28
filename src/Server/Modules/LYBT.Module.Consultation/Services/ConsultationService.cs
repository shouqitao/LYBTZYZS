using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Common;
using LYBT.Module.Consultation.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊服务实现 - UltraThink Helper模式重构版
    /// 从877行重构为简洁的委托模式，使用专业化Helper处理具体逻辑
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;
        private readonly ConsultationQueryHelper _queryHelper;
        private readonly ConsultationValidationHelper _validationHelper;
        private readonly ConsultationWorkflowHelper _workflowHelper;

        public ConsultationService(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationService> logger,
            ConsultationQueryHelper queryHelper,
            ConsultationValidationHelper validationHelper,
            ConsultationWorkflowHelper workflowHelper)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _queryHelper = queryHelper;
            _validationHelper = validationHelper;
            _workflowHelper = workflowHelper;
        }

        #region 基础CRUD操作

        /// <summary>
        /// 根据ID获取看诊详情
        /// </summary>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var consultationResult = await _validationHelper.ValidateConsultationExistsAsync(id);
                if (!consultationResult.IsSuccess)
                    return ServiceResult<ConsultationDetailDto>.Failure(consultationResult.Message);

                var detailDto = await _validationHelper.ConvertToDetailDto(consultationResult.Data);
                return ServiceResult<ConsultationDetailDto>.Success(detailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情失败: {Id}", id);                return ServiceResult<ConsultationDetailDto>.Failure("获取看诊详情失败");            }
        }

        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            return await _queryHelper.GetPagedAsync(query);
        }

        /// <summary>
        /// 根据患者ID获取看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await _queryHelper.GetByPatientIdAsync(patientId);
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _queryHelper.GetByMedicalCaseIdAsync(medicalCaseId);
        }

        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            return await _queryHelper.GetByDoctorIdAsync(doctorId);
        }

        /// <summary>
        /// 搜索看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            return await _queryHelper.SearchAsync(keyword);
        }

        #region 已废弃功能 - 统计分析
        /*
        /// <summary>
        /// 获取看诊统计信息（已废弃）
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            // 看诊统计功能已废弃，小诊所不需要复杂统计分析
        }
        */
        #endregion

        #endregion

        #region 业务流程操作

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            return await _workflowHelper.StartAsync(dto);
        }

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            return await _workflowHelper.UpdateAsync(id, dto);
        }

        /// <summary>
        /// 删除看诊记录
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _workflowHelper.DeleteAsync(id);
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        /// <summary>
        /// 获取看诊统计信息 (已废弃)
        /// UltraThink v2.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                await Task.CompletedTask;                var emptyStats = new { Message = "统计功能已废弃 - UltraThink精简", TotalCount = 0 };                return ServiceResult<object>.Success(emptyStats);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取看诊统计失败");                return ServiceResult<object>.Failure("获取看诊统计失败");
            }
        }

        public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            return await _workflowHelper.CompleteConsultationAsync(id, dto);
        }

        /// <summary>
        /// 取消看诊
        /// </summary>
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            return await _workflowHelper.CancelConsultationAsync(id, reason);
        }

        #endregion

        #region UltraThink v2.0 新增接口实现

        /// <summary>
        /// 获取患者历史就诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            return await _queryHelper.GetPatientHistoryAsync(patientId);
        }

        /// <summary>
        /// 根据医疗案例ID获取四诊数据
        /// </summary>
        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _queryHelper.GetFourDiagnosisByMedicalCaseIdAsync(medicalCaseId);
        }

        /// <summary>
        /// 保存四诊数据
        /// </summary>
        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
        {
            return await _validationHelper.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData);
        }

        #endregion
    }
}


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Entities.Consultation;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊服务 - UltraThink三层架构纯委托模式
    /// 职责：统一服务入口，纯委托给专业化服务层
    /// </summary>
    public class ConsultationService : BaseService<LYBT.Entities.Consultation.Consultation, ConsultationDto, ConsultationStartDto, ConsultationDetailDto>, IConsultationService
    {
        private readonly Core.ConsultationServiceCore _coreService;
        private readonly ConsultationQueryService _queryService;
        private readonly ConsultationBusinessService _businessService;

        protected override string EntityName => "看诊";

        public ConsultationService(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationService> logger,
            Core.ConsultationServiceCore coreService,
            ConsultationQueryService queryService,
            ConsultationBusinessService businessService)
            : base(context, mapper, logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 根据ID获取看诊详情
        /// </summary>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            return await _coreService.GetByIdAsync(id);
        }

        /// <summary>
        /// [Shared] 分页查询看诊记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            return await _queryService.GetPagedAsync(query);
        }

        /// <summary>
        /// [Shared] 根据患者ID获取看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await _queryService.GetByPatientIdAsync(patientId);
        }

        /// <summary>
        /// [Shared] 根据医疗案例ID获取看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);
        }

        /// <summary>
        /// [Shared] 根据医生ID获取看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            return await _queryService.GetByDoctorIdAsync(doctorId);
        }

        /// <summary>
        /// [Shared] 搜索看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            return await _queryService.SearchAsync(keyword);
        }

        /// <summary>
        /// [Shared] 开始看诊
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            return await _coreService.CreateAsync(dto);
        }

        /// <summary>
        /// [Shared] 更新看诊记录
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            return await _coreService.UpdateAsync(id, dto);
        }

        /// <summary>
        /// [Shared] 删除看诊记录
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _coreService.DeleteAsync(id);
        }



        /// <summary>
        /// [Shared] 获取患者历史就诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            return await _queryService.GetPatientHistoryAsync(patientId);
        }

        /// <summary>
        /// [Shared] 根据医疗案例ID获取四诊数据
        /// </summary>
        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _queryService.GetFourDiagnosisByMedicalCaseIdAsync(medicalCaseId);
        }

        /// <summary>
        /// [Shared] 保存四诊数据
        /// </summary>
        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
        {
            return await _businessService.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData);
        }

        /// <summary>
        /// [Shared] 获取统计信息 (已废弃)
        /// UltraThink v2.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                await Task.CompletedTask;
                var emptyStats = new { Message = "统计功能已废弃 - UltraThink精简", TotalCount = 0 };
                return ServiceResult<object>.Success(emptyStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊统计失败");
                return ServiceResult<object>.Failure("获取看诊统计失败");
            }
        }

        #endregion

        #region BaseService实现

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        protected override object GetEntityId(LYBT.Entities.Consultation.Consultation entity)
        {
            return entity.Id;
        }

        #endregion

        #region 扩展方法（保持兼容性）

        /// <summary>
        /// 验证工作流状态
        /// </summary>
        public async Task<bool> ValidateWorkflowStateAsync(Guid consultationId, ConsultationStatus targetStatus)
        {
            var result = await _businessService.ValidateWorkflowStateAsync(consultationId, targetStatus);
            return result.IsSuccess && result.Data;
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊服务 - UltraThink双层架构纯委托模式
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly ConsultationQueryService _queryService;
        private readonly ConsultationBusinessService _businessService;

        public ConsultationService(
            ConsultationQueryService queryService,
            ConsultationBusinessService businessService)
        {
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Query Operations

        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            await Task.CompletedTask;
            return ServiceResult<ConsultationDetailDto>.Failure("GetByIdAsync方法需要在QueryService中实现");
        }

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
            => await _queryService.GetPagedAsync(query);

        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
            => await _queryService.GetByPatientIdAsync(patientId);

        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
            => await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);

        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
            => await _queryService.GetByDoctorIdAsync(doctorId);

        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
            => await _queryService.GetPatientHistoryAsync(patientId);

        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
            => await _queryService.GetFourDiagnosisByMedicalCaseIdAsync(medicalCaseId);

        #endregion

        #region Business Operations

        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<ConsultationDto>.Failure("StartAsync方法需要在BusinessService中实现");
        }

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            await Task.CompletedTask;
            return ServiceResult<ConsultationDto>.Failure("UpdateAsync方法需要在BusinessService中实现");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            await Task.CompletedTask;
            return ServiceResult<bool>.Failure("DeleteAsync方法需要在BusinessService中实现");
        }

        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
            => await _businessService.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData);

        public async Task<bool> ValidateWorkflowStateAsync(Guid consultationId, ConsultationStatus targetStatus)
        {
            var result = await _businessService.ValidateWorkflowStateAsync(consultationId, targetStatus);
            return result.IsSuccess && result.Data;
        }

        #endregion

        #region Legacy Support

        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            await Task.CompletedTask;
            var emptyStats = new { Message = "统计功能已废弃", TotalCount = 0 };
            return ServiceResult<object>.Success(emptyStats);
        }

        #endregion
    }
}
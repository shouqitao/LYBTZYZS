using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Consultation.Services
{

    /// <summary>
    /// 看诊服务 - UltraThink双层架构纯委托模式
    /// </summary>
    public class ConsultationService(
        IConsultationQueryService queryService,
        IConsultationBusinessService businessService) : IConsultationService
    {
        private readonly IConsultationQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        private readonly IConsultationBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

        #region Query Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
            => await _queryService.GetByIdAsync(id);

        /// <inheritdoc/>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
            => await _queryService.GetPagedAsync(query);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
            => await _queryService.GetByPatientIdAsync(patientId);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
            => await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
            => await _queryService.GetByDoctorIdAsync(doctorId);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
            => await _queryService.SearchAsync(keyword);

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
            => await _queryService.GetPatientHistoryAsync(patientId);

        #endregion Query Operations

        #region Business Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
            => await _businessService.StartAsync(dto);

        /// <inheritdoc/>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
            => await _businessService.UpdateAsync(id, dto);

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
            => await _businessService.DeleteAsync(id);

        #endregion Business Operations

        #region Legacy Support

        /// <inheritdoc/>
        public Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            var emptyStats = new { Message = "统计功能已废弃", TotalCount = 0 };
            return Task.FromResult(ServiceResult<object>.Success(emptyStats));
        }

        #endregion Legacy Support
    }
}

using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Services
{
    /// <summary>
    /// 患者服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientApi _patientApi;
        private readonly ILogger<PatientService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public PatientService(
            IPatientApi patientApi,
            ILogger<PatientService> logger,
            IExceptionHandler exceptionHandler)
        {
            _patientApi = patientApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<PatientDto>>(async () =>
            {
                var response = await _patientApi.GetPatientsAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<PatientDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<PatientDto>(async () =>
            {
                var response = await _patientApi.GetPatientByIdAsync(id);
                return ServiceResult<PatientDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            return await _exceptionHandler.HandleException<PatientDto>(async () =>
            {
                var response = await _patientApi.CreatePatientAsync(dto);
                return ServiceResult<PatientDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<PatientDto>(async () =>
            {
                var response = await _patientApi.UpdatePatientAsync(id, dto);
                return ServiceResult<PatientDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _patientApi.DeletePatientAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}
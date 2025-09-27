using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseApi _medicalCaseApi;
        private readonly ILogger<MedicalCaseService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public MedicalCaseService(
            IMedicalCaseApi medicalCaseApi,
            ILogger<MedicalCaseService> logger,
            IExceptionHandler exceptionHandler)
        {
            _medicalCaseApi = medicalCaseApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<MedicalCaseDto>>(async () =>
            {
                var response = await _medicalCaseApi.GetMedicalCasesAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
                var response = await _medicalCaseApi.GetMedicalCaseByIdAsync(id);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
                var response = await _medicalCaseApi.CreateMedicalCaseAsync(dto);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<MedicalCaseDto>(async () =>
            {
                var response = await _medicalCaseApi.UpdateMedicalCaseAsync(id, dto);
                return ServiceResult<MedicalCaseDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _medicalCaseApi.DeleteMedicalCaseAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await _exceptionHandler.HandleException<List<MedicalCaseDto>>(async () =>
            {
                // TODO: 当API实现后，调用 _medicalCaseApi.GetMedicalCasesByPatientIdAsync(patientId)
                // 暂时返回空列表以通过编译
                await Task.CompletedTask;
                return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
            }, nameof(GetByPatientIdAsync));
        }
    }
}
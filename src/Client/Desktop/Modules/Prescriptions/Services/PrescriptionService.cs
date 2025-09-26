using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly IPrescriptionApi _prescriptionApi;
        private readonly ILogger<PrescriptionService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public PrescriptionService(
            IPrescriptionApi prescriptionApi,
            ILogger<PrescriptionService> logger,
            IExceptionHandler exceptionHandler)
        {
            _prescriptionApi = prescriptionApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<PrescriptionDto>>(async () =>
            {
                var response = await _prescriptionApi.GetPrescriptionsAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<PrescriptionDto>(async () =>
            {
                var response = await _prescriptionApi.GetPrescriptionByIdAsync(id);
                return ServiceResult<PrescriptionDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            return await _exceptionHandler.HandleException<PrescriptionDto>(async () =>
            {
                var response = await _prescriptionApi.CreatePrescriptionAsync(dto);
                return ServiceResult<PrescriptionDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<PrescriptionDto>(async () =>
            {
                var response = await _prescriptionApi.UpdatePrescriptionAsync(id, dto);
                return ServiceResult<PrescriptionDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _prescriptionApi.DeletePrescriptionAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}
using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationApi _consultationApi;
        private readonly ILogger<ConsultationService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public ConsultationService(
            IConsultationApi consultationApi,
            ILogger<ConsultationService> logger,
            IExceptionHandler exceptionHandler)
        {
            _consultationApi = consultationApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<ConsultationDto>>(async () =>
            {
                var response = await _consultationApi.GetConsultationsAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<ConsultationDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                var response = await _consultationApi.GetConsultationByIdAsync(id);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                var response = await _consultationApi.CreateConsultationAsync(dto);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<ConsultationDto>(async () =>
            {
                var response = await _consultationApi.UpdateConsultationAsync(id, dto);
                return ServiceResult<ConsultationDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _consultationApi.DeleteConsultationAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}
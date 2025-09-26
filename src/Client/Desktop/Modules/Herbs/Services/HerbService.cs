using LYBT.Desktop.Core.Services.Exceptions;
using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Services
{
    /// <summary>
    /// 草药服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IHerbApi _herbApi;
        private readonly ILogger<HerbService> _logger;
        private readonly IExceptionHandler _exceptionHandler;

        public HerbService(
            IHerbApi herbApi,
            ILogger<HerbService> logger,
            IExceptionHandler exceptionHandler)
        {
            _herbApi = herbApi;
            _logger = logger;
            _exceptionHandler = exceptionHandler;
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.HandleException<PagedResult<HerbDto>>(async () =>
            {
                var response = await _herbApi.GetHerbsAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<HerbDto>>.Success(response.Content);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.HandleException<HerbDto>(async () =>
            {
                var response = await _herbApi.GetHerbByIdAsync(id);
                return ServiceResult<HerbDto>.Success(response.Content);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            return await _exceptionHandler.HandleException<HerbDto>(async () =>
            {
                var response = await _herbApi.CreateHerbAsync(dto);
                return ServiceResult<HerbDto>.Success(response.Content);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            return await _exceptionHandler.HandleException<HerbDto>(async () =>
            {
                var response = await _herbApi.UpdateHerbAsync(id, dto);
                return ServiceResult<HerbDto>.Success(response.Content);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.HandleException(async () =>
            {
                await _herbApi.DeleteHerbAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}
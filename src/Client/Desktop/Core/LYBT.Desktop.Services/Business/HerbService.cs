using LYBT.Shared.Models.Extensions;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 草药服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly ILogger<HerbService> _logger;
        private readonly IHerbRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        public HerbService(
            IHerbRepository repository,
            ILogger<HerbService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 直接调用Repository的服务端分页方法
                var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<HerbDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var herb = await _repository.GetByIdAsync(id);
                return ServiceResult<HerbDto>.Success(herb);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建药材: {dto.Name}");

                // 使用扩展方法转换 DTO (Issue #1152)
                var herb = dto.ToDto();
                herb.Id = Guid.NewGuid();

                var created = await _repository.CreateAsync(herb);
                return ServiceResult<HerbDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 使用扩展方法更新字段 (Issue #1152)
                existing.ApplyUpdate(dto);

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<HerbDto>.Success(updated);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"搜索药材: {keyword}");

                var allHerbs = await _repository.GetAllAsync();
                var results = allHerbs.Where(h =>
                    h.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(h.Category) && h.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(h.Properties) && h.Properties.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(h.Effect) && h.Effect.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return ServiceResult<List<HerbDto>>.Success(results);
            }, nameof(SearchAsync));
        }
    }
}

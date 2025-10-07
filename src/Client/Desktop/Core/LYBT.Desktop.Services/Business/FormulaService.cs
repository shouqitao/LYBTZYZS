using AutoMapper;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 配方服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly ILogger<FormulaService> _logger;
        private readonly IFormulaRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly IMapper _mapper;

        public FormulaService(
            IFormulaRepository repository,
            ILogger<FormulaService> logger,
            IExceptionHandler exceptionHandler,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 直接调用Repository的服务端分页方法
                var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
                return ServiceResult<PagedResult<FormulaDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var formula = await _repository.GetByIdAsync(id);
                return ServiceResult<FormulaDto>.Success(formula);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建验方: {dto.Name}");

                // 使用 AutoMapper 转换 DTO
                var formula = _mapper.Map<FormulaDto>(dto);
                formula.Id = Guid.NewGuid();
                formula.Herbs = new List<FormulaHerbItemDto>(); // Herbs 集合在 Profile 中 Ignore,需手动初始化

                var created = await _repository.CreateAsync(formula);
                return ServiceResult<FormulaDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 使用 AutoMapper 更新字段
                _mapper.Map(dto, existing);

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<FormulaDto>.Success(updated);
            }, nameof(UpdateAsync));
        }


        public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"搜索验方: {keyword}");

                var allFormulas = await _repository.GetAllAsync();
                var results = allFormulas.Where(f =>
                    f.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(f.Effect) && f.Effect.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(f.Description) && f.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return ServiceResult<List<FormulaDto>>.Success(results);
            }, nameof(SearchAsync));
        }

        public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"克隆验方: {formulaId}");

                // 获取原验方
                var original = await _repository.GetByIdAsync(formulaId);

                // 创建克隆副本
                var cloned = new FormulaDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"{original.Name} (副本)",
                    Effect = original.Effect,
                    Description = original.Description,
                    Usage = original.Usage,
                    Property = original.Property,
                    IsShared = false, // 克隆的验方默认为私有
                    Indications = original.Indications,
                    Contraindications = original.Contraindications,
                    Remark = original.Remark,
                    Status = CommonStatus.Enabled,
                    Herbs = original.Herbs?.Select(h => new FormulaHerbItemDto
                    {
                        Id = Guid.NewGuid(),
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Quantity = h.Quantity,
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod,
                        SpecialInstructions = h.SpecialInstructions
                    }).ToList() ?? new List<FormulaHerbItemDto>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(cloned);
                return ServiceResult<FormulaDto>.Success(created);
            }, nameof(CloneFormulaAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }
    }
}

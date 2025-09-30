using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public FormulaService(
            IFormulaRepository repository,
            ILogger<FormulaService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allFormulas = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    allFormulas = allFormulas.Where(f =>
                        f.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (f.Effect != null && f.Effect.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (f.Indications != null && f.Indications.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 分页
                var totalCount = allFormulas.Count;
                var items = allFormulas
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedResult<FormulaDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

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

                // 转换DTO
                var formula = new FormulaDto
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Effect = dto.Effect,
                    Description = dto.Description,
                    Usage = dto.Usage,
                    Property = dto.Property,
                    IsShared = dto.IsShared,
                    Indications = dto.Indications,
                    Contraindications = dto.Contraindications,
                    Remark = dto.Remark,
                    Status = CommonStatus.Enabled,
                    Herbs = new List<FormulaHerbItemDto>(),
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

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

                // 更新字段
                existing.Name = dto.Name;
                existing.Effect = dto.Effect;
                existing.Description = dto.Description;
                existing.Usage = dto.Usage;
                existing.Property = dto.Property;
                existing.IsShared = dto.IsShared;
                existing.Indications = dto.Indications;
                existing.Contraindications = dto.Contraindications;
                existing.Remark = dto.Remark;
                existing.Status = dto.Status;
                existing.UpdateTime = DateTime.UtcNow;

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<FormulaDto>.Success(updated);
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
    }
}
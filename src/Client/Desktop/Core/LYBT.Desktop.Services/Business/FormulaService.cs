using LYBT.Shared.Models.Extensions;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using System.IO;

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

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 直接调用Repository的服务端分页方法
                var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);

                // 应用分类过滤 (Issue #1164)
                var items = pagedResult.Items;
                if (!string.IsNullOrWhiteSpace(category))
                {
                    items = items.Where(f => f.Property != null && f.Property.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // 更新分页结果
                var filteredResult = new PagedResult<FormulaDto>
                {
                    Items = items,
                    TotalCount = items.Count,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<FormulaDto>>.Success(filteredResult);
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

                // 使用扩展方法转换 DTO (Issue #1152)
                var formula = dto.ToDto();
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

                // 使用扩展方法更新字段 (Issue #1152)
                existing.ApplyUpdate(dto);

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

        /// <summary>
        /// 批量删除验方（软删除） - Issue #1169
        /// </summary>
        public Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            throw new NotImplementedException("批量删除功能待实现 (Issue #1169)");
        }

        /// <summary>
        /// 从Excel文件导入验方数据 - Issue #1166
        /// </summary>
        public Task<ServiceResult<ImportResultDto<FormulaDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
        {
            throw new NotImplementedException("Excel导入功能待实现 (Issue #1166)");
        }

        /// <summary>
        /// 导出验方数据到Excel - Issue #1166
        /// </summary>
        public Task<MemoryStream> ExportAsync(string? category = null)
        {
            throw new NotImplementedException("Excel导出功能待实现 (Issue #1166)");
        }

        /// <summary>
        /// 生成验方导入模板 - Issue #1166
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            throw new NotImplementedException("生成导入模板功能待实现 (Issue #1166)");
        }
    }
}

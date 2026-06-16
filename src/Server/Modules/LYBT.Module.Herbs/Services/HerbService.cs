using FluentValidation;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;

using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// Phase 2: 继承BaseService<Herb>复用统一错误处理和验证逻辑
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用HerbMapper替代AutoMapper
    /// </summary>
    public class HerbService : BaseService<Herb>, IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly IValidator<HerbInputDto> _validator;
        private readonly HerbMapper _mapper = new();
        private readonly ICacheInvalidationService _cacheInvalidation;

        public HerbService(
            IHerbRepository repository,
            ILogger<HerbService> logger,
            IValidator<HerbInputDto> validator,
            ICacheInvalidationService cacheInvalidation)
            : base(logger)
        {
            _repository = repository;
            _validator = validator;
            _cacheInvalidation = cacheInvalidation;
        }

        public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null, CancellationToken cancellationToken = default)
        {
            // Sprint3-X6: keyword + category 筛选均在 DB 层执行，TotalCount 自然正确
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword, category);
            var dtos = _mapper.ToListDtos(pagedResult.Items.ToList());

            var dto = new PagedResult<HerbListDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<HerbListDto>>.Success(dto);
        }

        public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<HerbDetailDto>.Failure(GenericErrorCode.HerbNotFound);

            var dto = _mapper.ToDetailDto(entity);
            return Result<HerbDetailDto>.Success(dto);
        }

        public async Task<Result<HerbDetailDto>> CreateAsync(HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // FluentValidation 验证（Phase 1 Task 1.8）
            var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Herb.Create → ValidationFailed - Errors={Errors}", string.Join("; ", errors));
                return Result<HerbDetailDto>.Failure(errors);
            }

            // US-HERB-001: 检查药材名称是否已存在
            if (await _repository.ExistsByNameAsync(dto.Name))
            {
                _logger.LogWarning("[SVC] Herb.Create → DuplicateName - Name={Name}", dto.Name);
                return Result<HerbDetailDto>.Failure(GenericErrorCode.InvalidRequest, $"药材名称 '{dto.Name}' 已存在");
            }

            // T5-P2-33: 拼音码自动生成
            if (string.IsNullOrWhiteSpace(dto.PinYinCode))
            {
                dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
            }

            var entity = _mapper.ToEntity(dto);
            var result = await _repository.AddAsync(entity);
            await _cacheInvalidation.InvalidateAsync("herbs");
            var resultDto = _mapper.ToDetailDto(result);
            return Result<HerbDetailDto>.Success(resultDto);
        }

        public async Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<HerbDetailDto>.Failure(GenericErrorCode.HerbNotFound);

            // FluentValidation 验证（Phase 1 Task 1.8）
            var validationResult = await _validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Herb.Update → ValidationFailed - HerbId={HerbId} Errors={Errors}", id, string.Join("; ", errors));
                return Result<HerbDetailDto>.Failure(errors);
            }

            // T5-P2-34: 名称变更时重新生成拼音码，并检查重复
            if (entity.Name != dto.Name)
            {
                if (await _repository.ExistsByNameAsync(dto.Name, id))
                {
                    _logger.LogWarning("[SVC] Herb.Update → DuplicateName - HerbId={HerbId} Name={Name}", id, dto.Name);
                    return Result<HerbDetailDto>.Failure(GenericErrorCode.InvalidRequest, $"药材名称 '{dto.Name}' 已存在");
                }
                dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
            }
            else if (string.IsNullOrWhiteSpace(dto.PinYinCode))
            {
                // 名称未变但拼音码为空（历史数据补全）
                dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
            }

            _mapper.UpdateEntity(dto, entity);
            var result = await _repository.UpdateAsync(entity);
            await _cacheInvalidation.InvalidateAsync("herbs");
            var resultDto = _mapper.ToDetailDto(result);
            return Result<HerbDetailDto>.Success(resultDto);
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _repository.DeleteAsync(id);
            await _cacheInvalidation.InvalidateAsync("herbs");
            return Result.Success();
        }

        public async Task<Result<List<HerbDetailDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entities = await _repository.FindAsync(h =>
                h.Name.Contains(keyword) ||
                (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            var dtos = _mapper.ToDetailDtos(entities.ToList());
            return Result<List<HerbDetailDto>>.Success(dtos);
        }

        // ========== Batch Import / Export (JSON) ==========

        /// <inheritdoc/>
        public async Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy, CancellationToken cancellationToken = default)
        {
            const int MAX_IMPORT_SIZE = 10000;

            var result = new HerbBatchImportResultDto
            {
                ImportTime = DateTime.UtcNow
            };

            if (herbs.Count > MAX_IMPORT_SIZE)
            {
                return Result<HerbBatchImportResultDto>.Failure($"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
            }

            _logger.LogInformation("[SVC] Herb.BatchImport started - Count={Count} Strategy={Strategy}", herbs.Count, strategy);

            for (int i = 0; i < herbs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dto = herbs[i];
                var rowNumber = i + 2;

                try
                {
                    if (string.IsNullOrWhiteSpace(dto.PinYinCode))
                    {
                        dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
                    }

                    var exists = await _repository.ExistsByNameAsync(dto.Name);

                    if (exists)
                    {
                        switch (strategy)
                        {
                            case DuplicateStrategy.Skip:
                                result.SkippedCount++;
                                continue;

                            case DuplicateStrategy.Update:
                                var existingHerbs = await _repository.FindAsync(h => h.Name == dto.Name);
                                var existingHerb = existingHerbs.FirstOrDefault();
                                if (existingHerb != null)
                                {
                                    _mapper.UpdateEntity(dto, existingHerb);
                                    existingHerb.UpdatedAt = DateTime.UtcNow;
                                    await _repository.UpdateAsync(existingHerb);
                                    result.SuccessCount++;
                                }
                                continue;

                            case DuplicateStrategy.Error:
                                result.FailureCount++;
                                result.Failures.Add(new HerbImportFailureDto
                                {
                                    RowNumber = rowNumber,
                                    HerbName = dto.Name,
                                    Reason = "药材名称重复",
                                    ErrorDetails = new List<string> { "已存在同名药材，导入策略设置为报错" }
                                });
                                continue;
                        }
                    }

                    var entity = _mapper.ToEntity(dto);
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.Status = CommonStatus.Enabled;

                    await _repository.AddAsync(entity);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Failures.Add(new HerbImportFailureDto
                    {
                        RowNumber = rowNumber,
                        HerbName = dto.Name,
                        Reason = "导入失败",
                        ErrorDetails = new List<string> { "数据处理异常" }
                    });
                    _logger.LogError(ex, "[SVC] Herb.BatchImport → ItemFailed - Row={Row} HerbName={HerbName}", rowNumber, dto.Name);
                }
            }

            _logger.LogInformation("[SVC] Herb.BatchImport completed - SuccessCount={Success} FailureCount={Failed} SkippedCount={Skipped}",
                result.SuccessCount, result.FailureCount, result.SkippedCount);

            return Result<HerbBatchImportResultDto>.Success(result);
        }

        /// <inheritdoc/>
        public async Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null, CancellationToken cancellationToken = default)
        {
            var herbs = await _repository.GetAllAsync();
            var herbDtos = _mapper.ToDetailDtos(herbs.ToList());

            if (!string.IsNullOrWhiteSpace(category))
            {
                herbDtos = herbDtos.Where(h =>
                    !string.IsNullOrEmpty(h.Category) &&
                    h.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            }

            _logger.LogInformation("[SVC] Herb.Export completed - Count={Count} Category={Category}",
                herbDtos.Count, category ?? "All");

            return Result<List<HerbDetailDto>>.Success(herbDtos);
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换方法实现 ==========

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        public async Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<HerbDetailDto>.Failure(GenericErrorCode.HerbNotFound);
            }

            // 切换状态
            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] Herb.ToggleStatus completed - HerbId={HerbId} Status={Status}", id, entity.Status);

            return Result<HerbDetailDto>.Success(dto);
        }

        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "药材不存在"
                        });
                        continue;
                    }

                    // 软删除
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] Herb.BatchDelete → ItemSuccess - HerbId={HerbId} HerbName={HerbName}", id, entity.Name);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "删除操作失败"
                    });
                    _logger.LogError(ex, "[SVC] Herb.BatchDelete → ItemFailed - HerbId={HerbId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            // CODE-11: 批量删除成功后失效缓存
            if (result.SuccessCount > 0)
            {
                await _cacheInvalidation.InvalidateAsync("herbs");
            }

            return Result<BatchOperationResultDto>.Success(result);
        }

    }
}

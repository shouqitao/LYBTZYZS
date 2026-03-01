using FluentValidation;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// Phase 2: 继承BaseService<Herb>复用统一错误处理和验证逻辑
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用HerbMapper替代AutoMapper
    /// Import/Export职责委托给 IHerbImportExportService
    /// </summary>
    public class HerbService : BaseService<Herb>, IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly IValidator<HerbInputDto> _validator;
        private readonly HerbMapper _mapper = new();
        private readonly AppDbContext _dbContext;
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly IHerbImportExportService _importExport;

        public HerbService(
            IHerbRepository repository,
            ILogger<HerbService> logger,
            IValidator<HerbInputDto> validator,
            AppDbContext dbContext,
            ICacheInvalidationService cacheInvalidation,
            IHerbImportExportService importExport)
            : base(logger)
        {
            _repository = repository;
            _validator = validator;
            _dbContext = dbContext;
            _cacheInvalidation = cacheInvalidation;
            _importExport = importExport;
        }

        public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
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

        public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<HerbDetailDto>.Failure(GenericErrorCode.HerbNotFound);

            var dto = _mapper.ToDetailDto(entity);
            return Result<HerbDetailDto>.Success(dto);
        }

        public async Task<Result<HerbDetailDto>> CreateAsync(HerbInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // FluentValidation 验证（Phase 1 Task 1.8）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Herb.Create → ValidationFailed - Errors={Errors}", string.Join("; ", errors));
                return Result<HerbDetailDto>.Failure(errors);
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

        public async Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<HerbDetailDto>.Failure(GenericErrorCode.HerbNotFound);

            // FluentValidation 验证（Phase 1 Task 1.8）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Herb.Update → ValidationFailed - HerbId={HerbId} Errors={Errors}", id, string.Join("; ", errors));
                return Result<HerbDetailDto>.Failure(errors);
            }

            // T5-P2-34: 名称变更时重新生成拼音码
            if (entity.Name != dto.Name)
            {
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

        public async Task<Result> DeleteAsync(Guid id)
        {
            // X7: 删除前强制引用检查
            var refCheck = await CheckReferenceAsync(id);
            if (refCheck.IsSuccess && refCheck.Data != null && refCheck.Data.HasReferences)
            {
                _logger.LogWarning("[SVC] Herb.Delete → HasReferences - HerbId={HerbId} ReferenceCount={Count}",
                    id, refCheck.Data.ReferenceCount);
                return Result.Failure(GenericErrorCode.HerbInUse, refCheck.Data.DeleteWarning ?? $"药材被 {refCheck.Data.ReferenceCount} 个引用，无法删除");
            }

            await _repository.DeleteAsync(id);
            await _cacheInvalidation.InvalidateAsync("herbs");
            return Result.Success();
        }

        public async Task<Result<List<HerbDetailDto>>> SearchAsync(string keyword)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entities = await _repository.FindAsync(h =>
                h.Name.Contains(keyword) ||
                (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            var dtos = _mapper.ToDetailDtos(entities.ToList());
            return Result<List<HerbDetailDto>>.Success(dtos);
        }

        // ========== Import/Export 职责委托给 IHerbImportExportService ==========

        /// <inheritdoc/>
        public Task<Result<ImportResultDto<HerbDetailDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
            => _importExport.ImportFromExcelAsync(stream, fileName);

        /// <inheritdoc/>
        public Task<MemoryStream> ExportAsync(string? category = null)
            => _importExport.ExportAsync(category);

        /// <inheritdoc/>
        public MemoryStream GenerateImportTemplate()
            => _importExport.GenerateImportTemplate();

        /// <inheritdoc/>
        public Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy)
            => _importExport.BatchImportAsync(herbs, strategy);

        /// <inheritdoc/>
        public Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null)
            => _importExport.GetAllForExportAsync(category);

        /// <summary>
        /// 检查药材是否被处方引用（Epic #1962 Task 4.2）
        /// OpenSpec: implement-data-sync - 实现处方引用检查
        /// </summary>
        public async Task<Result<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var herb = await _repository.GetByIdAsync(herbId);
            if (herb == null)
            {
                return Result<HerbReferenceCheckDto>.Failure(GenericErrorCode.HerbNotFound);
            }

            // 查询处方引用计数
            var prescriptionRefCount = await _dbContext.PrescriptionItems
                .CountAsync(pi => pi.HerbId == herbId);

            // CODE-11: 查询验方引用计数 (FormulaHerbItem.HerbId 可空，仅统计已绑定的)
            var formulaRefCount = await _dbContext.Set<FormulaHerbItem>()
                .CountAsync(fhi => fhi.HerbId != null && fhi.HerbId == herbId);

            var referenceCount = prescriptionRefCount + formulaRefCount;

            // 获取最近5条处方引用记录（使用 Join 查询，因为 PrescriptionItem 没有导航属性）
            var recentReferences = await (
                from pi in _dbContext.PrescriptionItems
                join p in _dbContext.Prescriptions on pi.PrescriptionId equals p.Id
                join mc in _dbContext.MedicalCases on p.MedicalCaseId equals mc.Id
                join patient in _dbContext.Patients on mc.PatientId equals patient.Id
                where pi.HerbId == herbId
                orderby p.CreatedAt descending
                select new PrescriptionReferenceDto
                {
                    PrescriptionId = p.Id,
                    PrescriptionNumber = p.PrescriptionNumber ?? string.Empty,
                    PatientName = patient.Name,
                    CreatedAt = p.CreatedAt,
                    // T2-X8-09: IsPrinted 已迁移到 MedicalCase 层级
                    Status = mc.IsPrinted ? "已打印" : "未打印"
                })
                .Take(5)
                .ToListAsync();

            var hasReferences = referenceCount > 0;
            var deleteWarning = hasReferences
                ? BuildReferenceWarning(prescriptionRefCount, formulaRefCount)
                : null;
            var result = new HerbReferenceCheckDto
            {
                HerbId = herbId,
                HerbName = herb.Name,
                HasReferences = hasReferences,
                ReferenceCount = referenceCount,
                CanDelete = !hasReferences, // X7: 有引用不可删除
                DeleteWarning = deleteWarning,
                RecentReferences = recentReferences
            };

            _logger.LogInformation("[SVC] Herb.CheckReference completed - HerbName={HerbName} HasReferences={HasReferences} PrescriptionRefs={PrescriptionRefs} FormulaRefs={FormulaRefs}",
                herb.Name, hasReferences, prescriptionRefCount, formulaRefCount);

            return Result<HerbReferenceCheckDto>.Success(result);
        }

        /// <summary>
        /// 批量检查药材引用关系（Epic #1962 Task 4.2）
        /// </summary>
        public async Task<Result<List<HerbReferenceCheckDto>>> BatchCheckReferenceAsync(List<Guid> herbIds)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            const int MAX_CHECK_SIZE = 100; // BR-006

            // BR-006: 批量检查数量限制
            if (herbIds.Count > MAX_CHECK_SIZE)
            {
                return Result<List<HerbReferenceCheckDto>>.Failure($"批量检查最多支持{MAX_CHECK_SIZE}条记录");
            }

            var results = new List<HerbReferenceCheckDto>();

            foreach (var herbId in herbIds)
            {
                var checkResult = await CheckReferenceAsync(herbId);
                if (checkResult.IsSuccess && checkResult.Data != null)
                {
                    results.Add(checkResult.Data);
                }
            }

            _logger.LogInformation("[SVC] Herb.BatchCheckReference completed - Count={Count}", results.Count);

            return Result<List<HerbReferenceCheckDto>>.Success(results);
        }

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法实现 ==========

        /// <summary>
        /// 切换药材状态（启用/禁用）
        /// </summary>
        public async Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id)
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
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] Herb.ToggleStatus completed - HerbId={HerbId} Status={Status}", id, entity.Status);

            return Result<HerbDetailDto>.Success(dto);
        }

        /// <summary>
        /// 恢复软删除的药材
        /// </summary>
        public async Task<Result<HerbDetailDto>> RestoreAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 使用GetByIdIncludingDeletedAsync获取包括已删除的实体
            var entity = await _repository.GetByIdIncludingDeletedAsync(id);
            if (entity == null)
            {
                return Result<HerbDetailDto>.Failure(GenericErrorCode.HerbNotFound);
            }

            if (!entity.IsDeleted)
            {
                return Result<HerbDetailDto>.Failure(GenericErrorCode.InvalidRequest, "该药材未被删除，无需恢复");
            }

            // 恢复软删除
            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] Herb.Restore completed - HerbId={HerbId} HerbName={HerbName}", id, entity.Name);

            return Result<HerbDetailDto>.Success(dto);
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        public async Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status)
        {
            var statusText = status == CommonStatus.Enabled ? "启用" : "禁用";

            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                try
                {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null || entity.IsDeleted)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "药材不存在或已删除"
                        });
                        continue;
                    }

                    entity.Status = status;
                    entity.UpdatedAt = DateTime.Now;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] Herb.BatchUpdateStatus → ItemSuccess - HerbId={HerbId} HerbName={HerbName} Status={Status}", id, entity.Name, statusText);
                }
                catch (Exception ex)
                {
                    // 保留项级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "状态更新失败"
                    });
                    _logger.LogError(ex, "[SVC] Herb.BatchUpdateStatus → ItemFailed - HerbId={HerbId} Status={Status}", id, statusText);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量{statusText}完成：成功 {result.SuccessCount} 个，失败 {result.FailureCount} 个";

            return Result<BatchOperationResultDto>.Success(result);
        }

        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
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

                    // CODE-11: 批量删除前检查引用（跳过有引用的项，不中断批量操作）
                    var refCheck = await CheckReferenceAsync(id);
                    if (refCheck.IsSuccess && refCheck.Data != null && refCheck.Data.HasReferences)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = refCheck.Data.DeleteWarning ?? $"药材被 {refCheck.Data.ReferenceCount} 个引用，无法删除"
                        });
                        _logger.LogWarning("[SVC] Herb.BatchDelete → HasReferences - HerbId={HerbId} RefCount={Count}",
                            id, refCheck.Data.ReferenceCount);
                        continue;
                    }

                    // 软删除
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.Now;
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

        /// <summary>
        /// 构建引用警告消息 (处方+验方)
        /// </summary>
        private static string BuildReferenceWarning(int prescriptionRefCount, int formulaRefCount)
        {
            var parts = new List<string>(2);
            if (prescriptionRefCount > 0)
                parts.Add($"{prescriptionRefCount} 个处方");
            if (formulaRefCount > 0)
                parts.Add($"{formulaRefCount} 个验方");

            return $"该药材被 {string.Join("和 ", parts)} 引用，无法删除";
        }
    }
}

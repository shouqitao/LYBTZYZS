using LYBT.Module.Formulas.Mapping;
using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Formulas.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// Sprint3-A3-07: 继承 BaseService 统一日志和权限验证
    /// </summary>
    public class FormulaService : BaseService, IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly IHerbCrossModuleService _crossModuleQuery;
        private readonly FormulaMapper _mapper = new();

        public FormulaService(
            IFormulaRepository repository,
            IHerbCrossModuleService crossModuleQuery,
            ILogger<FormulaService> logger)
            : base(logger)
        {
            _repository = repository;
            _crossModuleQuery = crossModuleQuery;
        }

        public async Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            string? category = null,
            Guid? currentUserId = null,
            bool isAdmin = false)
        {
            // Sprint3-X6: keyword + category + role 筛选均在 DB 层执行，TotalCount 自然正确
            var pagedResult = await _repository.GetPagedWithDetailsAsync(
                page, pageSize, keyword, category, currentUserId, isAdmin);

            var items = _mapper.ToListDtos(pagedResult.Items.ToList());

            var dto = new PagedResult<FormulaListDto>
            {
                Items = items,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<FormulaListDto>>.Success(dto);
        }

        public async Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 使用优化后的查询方法，包含所有药材配伍
            var entity = await _repository.GetByIdWithHerbsAsync(id);
            if (entity == null)
                return Result<FormulaDetailDto>.Failure(GenericErrorCode.FormulaNotFound);

            var dto = _mapper.ToDetailDto(entity);
            return Result<FormulaDetailDto>.Success(dto);
        }

        public async Task<Result<FormulaDetailDto>> CreateAsync(FormulaInputDto dto, Guid? creatorId = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // Issue #2014: 手动创建entity（不依赖AutoMapper处理Herbs集合）
            // OpenSpec: implement-formula-copy-flow - 设置UserId用于所有权过滤
            var entity = new Formula
            {
                Name = dto.Name,
                Effect = dto.Effect,
                Indication = dto.Indications, // Issue #2014: DTO.Indications → Entity.Indication
                Usage = dto.Usage,
                Remark = dto.Remark,
                Property = dto.Property,
                Category = dto.Category,
                FormulaType = FormulaType.Experience, // 默认经验方（DTO暂无此字段）
                IsShared = dto.IsShared,
                Status = CommonStatus.Enabled,
                ValidationStatus = FormulaValidationStatus.Draft,
                UserId = creatorId, // OpenSpec: implement-formula-copy-flow - 设置创建者ID
                Herbs = dto.Herbs?.Select(h => new FormulaHerbItem
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage, // decimal → int
                    Unit = h.Unit,
                    ProcessingMethod = h.ProcessingMethod ?? h.Preparation, // 优先使用ProcessingMethod，回退到Preparation
                    Usage = h.Usage,
                    DecocteMethod = h.DecocteMethod,
                    OriginalHerbName = h.HerbName, // 保存原始名称用于延迟绑定
                    IsValidated = h.HerbId.HasValue // HerbId有值则标记为已验证
                }).ToList() ?? new List<FormulaHerbItem>()
            };

            var result = await _repository.AddAsync(entity);
            var resultDto = _mapper.ToDetailDto(result);
            return Result<FormulaDetailDto>.Success(resultDto);
        }

        public async Task<Result<FormulaDetailDto>> UpdateAsync(Guid id, FormulaInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // Issue #2014: 使用GetByIdWithHerbsAsync（包含Herbs集合）
            var entity = await _repository.GetByIdWithHerbsAsync(id);
            if (entity == null)
                return Result<FormulaDetailDto>.Failure(GenericErrorCode.FormulaNotFound);

            // Issue #2014: 手动更新基础字段（包括新增的Indication）
            entity.Name = dto.Name;
            entity.Effect = dto.Effect;
            entity.Indication = dto.Indications; // Issue #2014: DTO.Indications → Entity.Indication
            entity.Usage = dto.Usage;
            entity.Remark = dto.Remark;
            entity.Property = dto.Property;
            entity.Category = dto.Category;
            // FormulaType保持现有值（DTO暂无此字段）
            entity.IsShared = dto.IsShared;

            // Issue #2014: 粗粒度全量替换Herbs（Formula-Design-Decision-002）
            // 优势：匹配用户工作流（Excel批量保存）、DDD模式、性能可接受
            entity.Herbs.Clear();
            if (dto.Herbs != null && dto.Herbs.Any())
            {
                foreach (var h in dto.Herbs)
                {
                    entity.Herbs.Add(new FormulaHerbItem
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Dosage = h.Dosage, // decimal → int
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod ?? h.Preparation, // 优先使用ProcessingMethod
                        Usage = h.Usage,
                        DecocteMethod = h.DecocteMethod,
                        OriginalHerbName = h.HerbName, // 保存原始名称
                        IsValidated = h.HerbId.HasValue // HerbId有值则标记为已验证
                    });
                }
            }

            var result = await _repository.UpdateAsync(entity);
            var resultDto = _mapper.ToDetailDto(result);
            return Result<FormulaDetailDto>.Success(resultDto);
        }

        public async Task<Result<List<FormulaDetailDto>>> SearchAsync(string keyword)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 简化搜索逻辑 - 直接使用分页查询，取前100个结果
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Result<List<FormulaDetailDto>>.Success(new List<FormulaDetailDto>());
            }

            var pagedResult = await _repository.GetPagedWithDetailsAsync(1, 100, keyword);
            var formulaDtos = _mapper.ToDetailDtos(pagedResult.Items.ToList());

            return Result<List<FormulaDetailDto>>.Success(formulaDtos);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var result = await _repository.DeleteAsync(id);
            return result ? Result.Success() : Result.Failure(GenericErrorCode.InternalError, "删除失败");
        }


        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        public async Task<Result> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 1. 查询验方（包含所有药材）
            var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
            if (formula == null)
            {
                return Result.Failure(GenericErrorCode.FormulaNotFound);
            }

            // 2. 查找待验证的药材项
            var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == herbItemId);
            if (herbItem == null)
            {
                return Result.Failure(GenericErrorCode.FormulaValidationFailed, "药材项不存在");
            }

            // 3. 验证是否已校验
            if (herbItem.IsValidated)
            {
                return Result.Failure(GenericErrorCode.FormulaValidationFailed, "该药材已校验，无需重复操作");
            }

            // 4. 查询选定的药材 - OpenSpec: decouple-server-modules 使用ICrossModuleService
            var selectedHerb = await _crossModuleQuery.GetHerbBasicInfoAsync(selectedHerbId);
            if (selectedHerb == null)
            {
                return Result.Failure(GenericErrorCode.HerbNotFound, "所选药材不存在");
            }

            // 5. 更新药材项的验证信息
            herbItem.HerbId = selectedHerbId;
            herbItem.HerbName = selectedHerb.Name;
            herbItem.IsValidated = true;

            // 6. 检查该验方的所有药材是否都已验证
            bool allValidated = formula.Herbs.All(h => h.IsValidated);
            if (allValidated)
            {
                // 所有药材都已验证，更新验方状态
                formula.ValidationStatus = FormulaValidationStatus.Validated;
                _logger.LogInformation("[SVC] Formula.ValidateHerb → AllValidated - FormulaId={FormulaId}", formulaId);
            }

            // 7. 保存变更
            await _repository.UpdateAsync(formula);
            await _repository.SaveChangesAsync();

            // 8. 返回成功（详细消息通过日志记录）
            if (allValidated)
            {
                _logger.LogInformation("[SVC] Formula.ValidateHerb completed - OriginalHerbName={OriginalHerbName} MappedName={MappedName} FormulaName={FormulaName} AllValidated=true",
                    herbItem.OriginalHerbName, selectedHerb.Name, formula.Name);
            }
            else
            {
                _logger.LogInformation("[SVC] Formula.ValidateHerb completed - OriginalHerbName={OriginalHerbName} MappedName={MappedName}",
                    herbItem.OriginalHerbName, selectedHerb.Name);
            }
            return Result.Success();
        }


        /// <summary>
        /// 获取待验证的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        public async Task<Result<List<FormulaDetailDto>>> GetPendingValidationFormulasAsync()
        {
            // Sprint3-X6: 在 DB 层过滤 Draft 状态，避免 GetAllAsync 全量加载
            var pendingFormulas = await _repository.FindAsync(
                f => f.ValidationStatus == FormulaValidationStatus.Draft);

            // 映射为DTO
            var formulaDtos = _mapper.ToDetailDtos(pendingFormulas.ToList());

            _logger.LogInformation("[SVC] Formula.GetPendingValidation completed - Count={Count}", formulaDtos.Count);
            return Result<List<FormulaDetailDto>>.Success(formulaDtos);
        }

        // OpenSpec: refactor-server-srp-patterns - Import/Export方法已迁移到FormulaImportExportService
        // 包括：ImportFromDataAsync, ExportAsync, GenerateImportTemplate, TryMatchHerbAsync

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法实现 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        public async Task<Result<FormulaDetailDto>> ToggleStatusAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<FormulaDetailDto>.Failure(GenericErrorCode.FormulaNotFound);
            }

            // 切换状态
            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] Formula.ToggleStatus completed - FormulaId={FormulaId} Status={Status}", id, entity.Status);

            return Result<FormulaDetailDto>.Success(dto);
        }

        /// <summary>
        /// 恢复软删除的验方
        /// </summary>
        public async Task<Result<FormulaDetailDto>> RestoreAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 使用GetByIdIncludingDeletedAsync获取包括已删除的实体
            var entity = await _repository.GetByIdIncludingDeletedAsync(id);
            if (entity == null)
            {
                return Result<FormulaDetailDto>.Failure(GenericErrorCode.FormulaNotFound);
            }

            if (!entity.IsDeleted)
            {
                return Result<FormulaDetailDto>.Failure(GenericErrorCode.InvalidRequest, "该验方未被删除，无需恢复");
            }

            // 恢复软删除
            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            _logger.LogInformation("[SVC] Formula.Restore completed - FormulaId={FormulaId} FormulaName={FormulaName}", id, entity.Name);

            return Result<FormulaDetailDto>.Success(dto);
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除验方
        /// </summary>
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
                            Reason = "方剂不存在"
                        });
                        continue;
                    }

                    // 软删除
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.Now;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] Formula.BatchDelete → ItemSuccess - FormulaId={FormulaId} FormulaName={FormulaName}", id, entity.Name);
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
                    _logger.LogError(ex, "[SVC] Formula.BatchDelete → ItemFailed - FormulaId={FormulaId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return Result<BatchOperationResultDto>.Success(result);
        }

        /// <summary>
        /// 批量更新方剂状态
        /// </summary>
        public async Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            var statusText = status == CommonStatus.Enabled ? "启用" : "禁用";

            foreach (var id in ids)
            {
                try
                {
                    var formula = await _repository.GetByIdAsync(id);
                    if (formula == null || formula.IsDeleted)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "方剂不存在"
                        });
                        continue;
                    }

                    formula.Status = status;
                    formula.UpdatedAt = DateTime.Now;
                    await _repository.UpdateAsync(formula);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] Formula.BatchUpdateStatus → ItemSuccess - FormulaId={FormulaId} FormulaName={FormulaName} Status={Status}", id, formula.Name, statusText);
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
                    _logger.LogError(ex, "[SVC] Formula.BatchUpdateStatus → ItemFailed - FormulaId={FormulaId} Status={Status}", id, statusText);
                }
            }

            await _repository.SaveChangesAsync();

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量{statusText}完成: 成功 {result.SuccessCount} 个, 失败 {result.FailureCount} 个";

            return Result<BatchOperationResultDto>.Success(result);
        }
    }
}

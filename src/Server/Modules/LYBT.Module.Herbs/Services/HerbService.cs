using AutoMapper;
using FluentValidation;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// Phase 2: 继承BaseService<Herb>复用统一错误处理和验证逻辑
    /// </summary>
    public class HerbService : BaseService<Herb>, IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly IValidator<HerbInputDto> _validator;

        public HerbService(
            IHerbRepository repository,
            IMapper mapper,
            ILogger<HerbService> logger,
            IValidator<HerbInputDto> validator)
            : base(logger, mapper)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // 修复：传递keyword参数到Repository进行数据库级别搜索
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
            var dtos = _mapper.Map<List<HerbListDto>>(pagedResult.Items);

            // Issue #1164: 应用分类筛选（在DTO级别过滤）
            if (!string.IsNullOrWhiteSpace(category))
            {
                dtos = dtos.Where(h =>
                    !string.IsNullOrEmpty(h.Category) &&
                    h.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            }

            var dto = new PagedResult<HerbListDto>
            {
                Items = dtos,
                TotalCount = !string.IsNullOrWhiteSpace(category) ? dtos.Count : pagedResult.TotalCount,
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
                return Result<HerbDetailDto>.Failure("药材不存在");

            var dto = _mapper.Map<HerbDetailDto>(entity);
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

            var entity = _mapper.Map<Herb>(dto);
            var result = await _repository.AddAsync(entity);
            var resultDto = _mapper.Map<HerbDetailDto>(result);
            return Result<HerbDetailDto>.Success(resultDto);
        }

        public async Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<HerbDetailDto>.Failure("药材不存在");

            // FluentValidation 验证（Phase 1 Task 1.8）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("[SVC] Herb.Update → ValidationFailed - HerbId={HerbId} Errors={Errors}", id, string.Join("; ", errors));
                return Result<HerbDetailDto>.Failure(errors);
            }

            _mapper.Map(dto, entity);
            var result = await _repository.UpdateAsync(entity);
            var resultDto = _mapper.Map<HerbDetailDto>(result);
            return Result<HerbDetailDto>.Success(resultDto);
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            await _repository.DeleteAsync(id);
            return Result.Success();
        }

        public async Task<Result<List<HerbDetailDto>>> SearchAsync(string keyword)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var entities = await _repository.FindAsync(h =>
                h.Name.Contains(keyword) ||
                (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            var dtos = _mapper.Map<List<HerbDetailDto>>(entities);
            return Result<List<HerbDetailDto>>.Success(dtos);
        }

        /// <summary>
        /// 从Excel文件导入药材数据 (Issue #1166)
        /// </summary>
        public async Task<Result<ImportResultDto<HerbDetailDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
        {
            // eliminate-service-catch-return: 移除外层冗余try-catch，保留行级错误隔离
            var result = new ImportResultDto<HerbDetailDto>
            {
                FileName = fileName,
                ImportTime = DateTime.Now
            };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.IsSuccess = false;
                result.Message = "Excel文件中没有工作表";
                return Result<ImportResultDto<HerbDetailDto>>.Failure("Excel文件格式错误");
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1)
            {
                result.IsSuccess = false;
                result.Message = "Excel文件中没有数据行";
                return Result<ImportResultDto<HerbDetailDto>>.Success(result);
            }

            result.TotalCount = rowCount - 1;

            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var name = worksheet.Cells[row, 1].Text?.Trim();
                    var unit = worksheet.Cells[row, 2].Text?.Trim();
                    var priceText = worksheet.Cells[row, 3].Text?.Trim();
                    var origin = worksheet.Cells[row, 4].Text?.Trim();
                    var spec = worksheet.Cells[row, 5].Text?.Trim();
                    var effect = worksheet.Cells[row, 6].Text?.Trim();
                    var usage = worksheet.Cells[row, 7].Text?.Trim();
                    var remark = worksheet.Cells[row, 8].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = $"第{row}行",
                            ErrorMessage = "药材名称不能为空"
                        });
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(unit))
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = $"第{row}行",
                            ErrorMessage = "单位不能为空"
                        });
                        continue;
                    }

                    if (!decimal.TryParse(priceText, out var price) || price <= 0)
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = $"第{row}行",
                            ErrorMessage = "单价格式错误或必须大于0"
                        });
                        continue;
                    }

                    var herb = new Herb
                    {
                        Name = name,
                        PinYinCode = PinYinHelper.GetPinYinCode(name), // Issue #2174: 自动生成拼音码
                        Unit = unit,
                        Price = price,
                        Origin = origin,
                        Spec = spec,
                        Effect = effect,
                        Usage = usage,
                        Remark = remark,
                        Status = CommonStatus.Enabled,
                        CreatedAt = DateTime.Now
                    };

                    var savedHerb = await _repository.AddAsync(herb);
                    var herbDto = _mapper.Map<HerbDetailDto>(savedHerb);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(savedHerb.Id);
                    result.ImportedData.Add(herbDto);
                }
                catch (Exception ex)
                {
                    // 保留行级错误隔离，ERR-012: 使用安全错误消息
                    result.FailureCount++;
                    result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                    {
                        RecordIdentifier = $"第{row}行",
                        ErrorMessage = "导入失败：数据处理异常"
                    });
                    _logger.LogError(ex, "[SVC] Herb.Import → RowError - Row={Row}", row);
                }
            }

            result.IsSuccess = true;
            result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return Result<ImportResultDto<HerbDetailDto>>.Success(result);
        }

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        public async Task<MemoryStream> ExportAsync(string? category = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var herbs = await _repository.GetAllAsync();
            var herbDtos = _mapper.Map<List<HerbDetailDto>>(herbs);

            // 应用分类筛选
            if (!string.IsNullOrWhiteSpace(category))
            {
                herbDtos = herbDtos.Where(h =>
                    !string.IsNullOrEmpty(h.Category) &&
                    h.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
            }

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("药材列表");

                // 表头
                worksheet.Cells[1, 1].Value = "药材名称";
                worksheet.Cells[1, 2].Value = "单位";
                worksheet.Cells[1, 3].Value = "单价";
                worksheet.Cells[1, 4].Value = "产地";
                worksheet.Cells[1, 5].Value = "规格";
                worksheet.Cells[1, 6].Value = "功效";
                worksheet.Cells[1, 7].Value = "用法用量";
                worksheet.Cells[1, 8].Value = "备注";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 数据行
                for (int i = 0; i < herbDtos.Count; i++)
                {
                    var herb = herbDtos[i];
                    int row = i + 2;

                    worksheet.Cells[row, 1].Value = herb.Name;
                    worksheet.Cells[row, 2].Value = herb.Unit;
                    worksheet.Cells[row, 3].Value = herb.Price;
                    worksheet.Cells[row, 4].Value = herb.Origin;
                    worksheet.Cells[row, 5].Value = herb.Spec;
                    worksheet.Cells[row, 6].Value = herb.Effect;
                    worksheet.Cells[row, 7].Value = herb.Usage;
                    worksheet.Cells[row, 8].Value = herb.Remark;
                }

                worksheet.Cells.AutoFitColumns();
                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// 生成药材导入模板 (Issue #1166)
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var stream = new MemoryStream();
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets.Add("药材信息");

                // 表头
                worksheet.Cells[1, 1].Value = "药材名称*";
                worksheet.Cells[1, 2].Value = "单位*";
                worksheet.Cells[1, 3].Value = "单价*";
                worksheet.Cells[1, 4].Value = "产地";
                worksheet.Cells[1, 5].Value = "规格";
                worksheet.Cells[1, 6].Value = "功效";
                worksheet.Cells[1, 7].Value = "用法用量";
                worksheet.Cells[1, 8].Value = "备注";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // 示例数据
                worksheet.Cells[2, 1].Value = "人参";
                worksheet.Cells[2, 2].Value = "克";
                worksheet.Cells[2, 3].Value = 5.0;
                worksheet.Cells[2, 4].Value = "吉林";
                worksheet.Cells[2, 5].Value = "特级";
                worksheet.Cells[2, 6].Value = "大补元气，复脉固脱";
                worksheet.Cells[2, 7].Value = "3-9克";
                worksheet.Cells[2, 8].Value = "贵重药材";

                worksheet.Cells.AutoFitColumns();
                package.Save();
            }

            stream.Position = 0;
            return stream;
        }

        // ========== Epic #1962: 批量导入/导出和引用检查方法实现 ==========

        /// <summary>
        /// 批量导入药材（Epic #1962 Task 2.2）
        /// </summary>
        public async Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy)
        {
            // eliminate-service-catch-return: 移除外层冗余try-catch，保留行级错误隔离
            const int MAX_IMPORT_SIZE = 10000; // BR-006

            var result = new HerbBatchImportResultDto
            {
                ImportTime = DateTime.Now
            };

            // BR-006: 批量导入数量限制
            if (herbs.Count > MAX_IMPORT_SIZE)
            {
                return Result<HerbBatchImportResultDto>.Failure($"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
            }

            _logger.LogInformation("[SVC] Herb.BatchImport started - Count={Count} Strategy={Strategy}", herbs.Count, strategy);

            for (int i = 0; i < herbs.Count; i++)
            {
                var dto = herbs[i];
                var rowNumber = i + 2; // Excel行号（从第2行开始）

                try
                {
                    // BR-008: 自动生成拼音码
                    if (string.IsNullOrWhiteSpace(dto.PinYinCode))
                    {
                        dto.PinYinCode = PinYinHelper.GetPinYinCode(dto.Name);
                    }

                    // BR-002: 检查药材名称是否已存在
                    var exists = await _repository.ExistsByNameAsync(dto.Name);

                    if (exists)
                    {
                        // 处理重复项
                        switch (strategy)
                        {
                            case DuplicateStrategy.Skip:
                                result.SkippedCount++;
                                _logger.LogDebug("[SVC] Herb.BatchImport → Skipped - HerbName={HerbName}", dto.Name);
                                continue;

                            case DuplicateStrategy.Update:
                                // 查找现有药材并更新
                                var existingHerbs = await _repository.FindAsync(h => h.Name == dto.Name);
                                var existingHerb = existingHerbs.FirstOrDefault();
                                if (existingHerb != null)
                                {
                                    _mapper.Map(dto, existingHerb);
                                    existingHerb.UpdatedAt = DateTime.Now;
                                    await _repository.UpdateAsync(existingHerb);
                                    result.SuccessCount++;
                                    _logger.LogDebug("[SVC] Herb.BatchImport → Updated - HerbName={HerbName}", dto.Name);
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
                                _logger.LogWarning("[SVC] Herb.BatchImport → DuplicateError - HerbName={HerbName}", dto.Name);
                                continue;
                        }
                    }

                    // 创建新药材
                    var entity = _mapper.Map<Herb>(dto);
                    entity.CreatedAt = DateTime.Now;
                    entity.Status = CommonStatus.Enabled;

                    await _repository.AddAsync(entity);
                    result.SuccessCount++;
                    _logger.LogDebug("[SVC] Herb.BatchImport → ItemSuccess - HerbName={HerbName}", dto.Name);
                }
                catch (Exception ex)
                {
                    // 保留行级错误隔离，ERR-012: 使用安全错误消息
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

        /// <summary>
        /// 获取所有药材数据用于导出（Epic #1962 Task 3.1）
        /// </summary>
        public async Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var herbs = await _repository.GetAllAsync();
            var herbDtos = _mapper.Map<List<HerbDetailDto>>(herbs);

            // 应用分类筛选
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

        /// <summary>
        /// 检查药材是否被处方引用（Epic #1962 Task 4.2）
        /// </summary>
        public async Task<Result<HerbReferenceCheckDto>> CheckReferenceAsync(Guid herbId)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var herb = await _repository.GetByIdAsync(herbId);
            if (herb == null)
            {
                return Result<HerbReferenceCheckDto>.Failure("药材不存在");
            }

            var result = new HerbReferenceCheckDto
            {
                HerbId = herbId,
                HerbName = herb.Name,
                HasReferences = false,
                ReferenceCount = 0,
                CanDelete = true, // BR-007: 支持软删除，始终可删除
                RecentReferences = new List<PrescriptionReferenceDto>()
            };

            // TODO: 实现处方引用检查
            // 当前版本暂不检查，直接返回无引用
            // 后续迭代中需要查询 PrescriptionItems 表
            _logger.LogInformation("[SVC] Herb.CheckReference completed - HerbName={HerbName} HasReferences=false", herb.Name);

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
                return Result<HerbDetailDto>.Failure("药材不存在");
            }

            // 切换状态
            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.Map<HerbDetailDto>(result);

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
                return Result<HerbDetailDto>.Failure("药材不存在");
            }

            if (!entity.IsDeleted)
            {
                return Result<HerbDetailDto>.Failure("该药材未被删除，无需恢复");
            }

            // 恢复软删除
            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.Now;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.Map<HerbDetailDto>(result);

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

            return Result<BatchOperationResultDto>.Success(result);
        }
    }
}

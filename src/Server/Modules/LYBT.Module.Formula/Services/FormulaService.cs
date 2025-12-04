using AutoMapper;
using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Services;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Formulas.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService替代IHerbRepository
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly ICrossModuleQueryService _crossModuleQuery;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaRepository repository,
            ICrossModuleQueryService crossModuleQuery,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _repository = repository;
            _crossModuleQuery = crossModuleQuery;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Herbs集合
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);

                // Issue #1164: 应用分类筛选（MVP阶段内存过滤，Formula实体有Category字段）
                var filteredItems = pagedResult.Items.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(category))
                {
                    filteredItems = filteredItems.Where(f =>
                        !string.IsNullOrEmpty(f.Category) &&
                        f.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
                }

                var filteredList = filteredItems.ToList();

                var dto = new PagedResult<FormulaDto>
                {
                    Items = _mapper.Map<List<FormulaDto>>(filteredList),
                    TotalCount = !string.IsNullOrWhiteSpace(category) ? filteredList.Count : pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return Result<PagedResult<FormulaDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方列表失败");
                return Result<PagedResult<FormulaDto>>.Failure("获取验方列表失败");
            }
        }

        public async Task<Result<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含所有药材配伍
                var entity = await _repository.GetByIdWithHerbsAsync(id);
                if (entity == null)
                    return Result<FormulaDto>.Failure("验方不存在");

                var dto = _mapper.Map<FormulaDto>(entity);
                return Result<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败");
                return Result<FormulaDto>.Failure("获取验方详情失败");
            }
        }

        public async Task<Result<FormulaDto>> CreateAsync(FormulaInputDto dto)
        {
            try
            {
                // Issue #2014: 手动创建entity（不依赖AutoMapper处理Herbs集合）
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
                    Herbs = dto.Herbs?.Select(h => new FormulaHerbItem
                    {
                        HerbId = h.HerbId,
                        HerbName = h.HerbName,
                        Quantity = (int)h.Quantity, // decimal → int
                        Unit = h.Unit,
                        ProcessingMethod = h.ProcessingMethod ?? h.Preparation, // 优先使用ProcessingMethod，回退到Preparation
                        Usage = h.Usage,
                        OriginalHerbName = h.HerbName, // 保存原始名称用于延迟绑定
                        IsValidated = h.HerbId.HasValue // HerbId有值则标记为已验证
                    }).ToList() ?? new List<FormulaHerbItem>()
                };

                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return Result<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                return Result<FormulaDto>.Failure("创建验方失败");
            }
        }

        public async Task<Result<FormulaDto>> UpdateAsync(Guid id, FormulaInputDto dto)
        {
            try
            {
                // Issue #2014: 使用GetByIdWithHerbsAsync（包含Herbs集合）
                var entity = await _repository.GetByIdWithHerbsAsync(id);
                if (entity == null)
                    return Result<FormulaDto>.Failure("验方不存在");

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
                            Quantity = (int)h.Quantity, // decimal → int
                            Unit = h.Unit,
                            ProcessingMethod = h.ProcessingMethod ?? h.Preparation, // 优先使用ProcessingMethod
                            Usage = h.Usage,
                            OriginalHerbName = h.HerbName, // 保存原始名称
                            IsValidated = h.HerbId.HasValue // HerbId有值则标记为已验证
                        });
                    }
                }

                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return Result<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败");
                return Result<FormulaDto>.Failure("更新验方失败");
            }
        }

        public async Task<Result<List<FormulaDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 简化搜索逻辑 - 直接使用分页查询，取前100个结果
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Result<List<FormulaDto>>.Success(new List<FormulaDto>());
                }

                var pagedResult = await _repository.GetPagedWithDetailsAsync(1, 100, keyword);
                var formulaDtos = _mapper.Map<List<FormulaDto>>(pagedResult.Items);

                return Result<List<FormulaDto>>.Success(formulaDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方时发生错误，关键字：{Keyword}", keyword);
                return Result<List<FormulaDto>>.Failure($"搜索处方失败：{ex.Message}");
            }
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? Result.Success() : Result.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败");
                return Result.Failure("删除验方失败");
            }
        }


        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        public async Task<Result> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            try
            {
                // 1. 查询验方（包含所有药材）
                var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
                if (formula == null)
                {
                    return Result.Failure("验方不存在");
                }

                // 2. 查找待验证的药材项
                var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == herbItemId);
                if (herbItem == null)
                {
                    return Result.Failure("药材项不存在");
                }

                // 3. 验证是否已校验
                if (herbItem.IsValidated)
                {
                    return Result.Failure("该药材已校验，无需重复操作");
                }

                // 4. 查询选定的药材 - OpenSpec: decouple-server-modules 使用ICrossModuleQueryService
                var selectedHerb = await _crossModuleQuery.GetHerbBasicInfoAsync(selectedHerbId);
                if (selectedHerb == null)
                {
                    return Result.Failure("所选药材不存在");
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
                    _logger.LogInformation("验方 {FormulaId} 所有药材已验证，状态更新为Validated", formulaId);
                }

                // 7. 保存变更
                await _repository.UpdateAsync(formula);
                await _repository.SaveChangesAsync();

                // 8. 返回成功（详细消息通过日志记录）
                if (allValidated)
                {
                    _logger.LogInformation("药材\"{OriginalHerbName}\"已映射为\"{HerbName}\"，验方\"{FormulaName}\"所有药材已校验完成",
                        herbItem.OriginalHerbName, selectedHerb.Name, formula.Name);
                }
                else
                {
                    _logger.LogInformation("药材\"{OriginalHerbName}\"已映射为\"{HerbName}\"",
                        herbItem.OriginalHerbName, selectedHerb.Name);
                }
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证验方药材失败：formulaId={FormulaId}, herbItemId={HerbItemId}, selectedHerbId={SelectedHerbId}",
                    formulaId, herbItemId, selectedHerbId);
                return Result.Failure("验证验方药材失败");
            }
        }


        /// <summary>
        /// 获取待验证的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        public async Task<Result<List<FormulaDto>>> GetPendingValidationFormulasAsync()
        {
            try
            {
                // 查询所有Draft状态的验方（使用GetAllAsync预加载Herbs避免N+1查询）
                var allFormulas = await _repository.GetAllAsync();

                // 过滤出Draft状态的验方
                var pendingFormulas = allFormulas
                    .Where(f => f.ValidationStatus == FormulaValidationStatus.Draft)
                    .ToList();

                // 映射为DTO
                var formulaDtos = _mapper.Map<List<FormulaDto>>(pendingFormulas);

                _logger.LogInformation("查询到 {Count} 个待验证验方", formulaDtos.Count);
                return Result<List<FormulaDto>>.Success(formulaDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待验证验方列表失败");
                return Result<List<FormulaDto>>.Failure("获取待验证验方列表失败");
            }
        }


        /// <summary>
        /// 批量删除验方（软删除）(Issue #1169)
        /// </summary>
        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            const int MAX_BATCH_SIZE = 100;

            try
            {
                // 批量大小限制
                if (ids.Count > MAX_BATCH_SIZE)
                {
                    return Result<BatchOperationResultDto>.Failure($"批量操作最多支持{MAX_BATCH_SIZE}条记录");
                }

                var result = new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    IsSuccess = true,
                    Message = "批量删除完成"
                };

                foreach (var formulaId in ids)
                {
                    try
                    {
                        // 检查验方是否存在
                        var formula = await _repository.GetByIdAsync(formulaId);
                        if (formula == null)
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(formulaId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = formulaId.ToString(),
                                ErrorMessage = "验方不存在"
                            });
                            continue;
                        }

                        // TODO: 检查验方是否被处方引用（后续迭代）
                        // 现在MVP阶段直接允许删除

                        // 执行删除
                        var deleteResult = await _repository.DeleteAsync(formulaId);
                        if (deleteResult)
                        {
                            result.SuccessCount++;
                            result.SuccessfulIds.Add(formulaId);
                        }
                        else
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(formulaId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = formulaId.ToString(),
                                ErrorMessage = "删除失败"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(formulaId);
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = formulaId.ToString(),
                            ErrorMessage = ex.Message
                        });
                        _logger.LogError(ex, "批量删除验方失败: {FormulaId}", formulaId);
                    }
                }

                // 更新操作结果
                result.IsSuccess = result.FailureCount == 0;
                if (result.FailureCount > 0 && result.SuccessCount > 0)
                {
                    result.Message = $"部分成功：成功{result.SuccessCount}条，失败{result.FailureCount}条";
                }
                else if (result.FailureCount == result.TotalCount)
                {
                    result.Message = "批量删除失败";
                    result.IsSuccess = false;
                }

                _logger.LogInformation("批量删除验方完成: 总数{Total}, 成功{Success}, 失败{Failed}",
                    result.TotalCount, result.SuccessCount, result.FailureCount);

                return Result<BatchOperationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除验方异常");
                return Result<BatchOperationResultDto>.Failure("批量删除验方失败");
            }
        }


        /// <summary>
        /// 从Excel文件导入验方数据 (Issue #1347: 重写为主-从表格式，支持延迟绑定)
        /// 格式：Sheet1=验方信息，Sheet2=药材明细
        /// </summary>
        /// <summary>
        /// 从结构化数据导入验方 (Issue #1758: 架构重构 - Server端不再依赖Excel格式)
        /// Client端负责Excel解析，Server端只处理业务逻辑
        /// </summary>
        /// <param name="formulas">已解析的验方列表（由Client端从Excel解析）</param>
        /// <param name="fileName">原始文件名（用于日志记录）</param>
        public async Task<Result<FormulaImportResultDto>> ImportFromDataAsync(List<FormulaImportDto> formulas, string? fileName = null)
        {
            var result = new FormulaImportResultDto
            {
                FileName = fileName,
                ImportTime = DateTime.Now,
                StartTime = DateTime.Now,
                TotalCount = formulas.Count
            };

            try
            {
                // 逐个导入验方
                int index = 0;
                foreach (var formulaDto in formulas)
                {
                    index++;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(formulaDto.Name))
                        {
                            result.FailureCount++;
                            result.FailedItems.Add(new FormulaImportErrorDto
                            {
                                RowIndex = index,
                                FormulaName = formulaDto.Name ?? string.Empty,
                                ErrorMessage = "验方名称不能为空"
                            });
                            continue;
                        }

                        // 创建验方实体（从DTO映射）
                        var formula = new Formula
                        {
                            Name = formulaDto.Name,
                            Effect = formulaDto.Effect,
                            Usage = formulaDto.Usage,
                            Property = formulaDto.Property,
                            IsShared = formulaDto.IsShared,
                            Remark = formulaDto.Remark,
                            // Note: Indications, Contraindications, Preparation, Source exist in DTO but not in Entity
                            Status = CommonStatus.Enabled,
                            ValidationStatus = FormulaValidationStatus.Draft, // 导入的验方初始为Draft
                            CreatedAt = DateTime.Now,
                            Herbs = new List<FormulaHerbItem>()
                        };

                        // 添加药材（从DTO列表）
                        foreach (var herbDto in formulaDto.Herbs)
                        {
                            // 尝试自动匹配药材
                            var matchedHerb = await TryMatchHerbAsync(herbDto.HerbName);

                            formula.Herbs.Add(new FormulaHerbItem
                            {
                                Id = Guid.NewGuid(),
                                HerbId = matchedHerb?.Id,
                                HerbName = herbDto.HerbName,
                                OriginalHerbName = herbDto.HerbName, // 保存原始名称
                                IsValidated = matchedHerb != null, // 成功匹配则标记为已验证
                                Quantity = (int)herbDto.Quantity, // DTO是decimal，实体是int
                                Unit = herbDto.Unit ?? "g",
                                Usage = herbDto.Usage,
                                ProcessingMethod = herbDto.Preparation // DTO的Preparation映射到ProcessingMethod
                                // Note: SortOrder exists in DTO but not in Entity
                            });

                            // 统计药材匹配情况
                            if (matchedHerb != null)
                            {
                                result.MatchedHerbsCount++;
                            }
                            else
                            {
                                result.UnmatchedHerbsCount++;
                            }
                        }

                        // 自动判断验证状态：如果所有药材都已验证，则标记为Validated
                        if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
                        {
                            formula.ValidationStatus = FormulaValidationStatus.Validated;
                        }

                        var savedFormula = await _repository.AddAsync(formula);
                        var formulaResultDto = _mapper.Map<FormulaDto>(savedFormula);

                        result.SuccessCount++;
                        result.SuccessfulIds.Add(savedFormula.Id);
                        result.SuccessfulFormulas.Add(formulaResultDto);
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.FailedItems.Add(new FormulaImportErrorDto
                        {
                            RowIndex = index,
                            FormulaName = formulaDto.Name ?? string.Empty,
                            ErrorMessage = $"导入失败：{ex.Message}",
                            ErrorDetails = ex.StackTrace
                        });
                        _logger.LogError(ex, "导入验方 {FormulaName} 时发生错误", formulaDto.Name);
                    }
                }

                result.EndTime = DateTime.Now;
                result.IsSuccess = true;
                result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

                return Result<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入验方数据时发生错误");
                result.EndTime = DateTime.Now;
                result.IsSuccess = false;
                result.Message = $"导入失败：{ex.Message}";
                return Result<FormulaImportResultDto>.Failure($"导入失败：{ex.Message}");
            }
        }

        // Issue #1758: ParseHerbItems方法已移至Client端 ExcelParseHelper

        /// <summary>
        /// 尝试自动匹配药材（按名称或拼音码）
        /// </summary>
        /// <summary>
        /// 尝试匹配药材 - OpenSpec: decouple-server-modules 使用ICrossModuleQueryService
        /// 返回HerbBasicDto用于只读信息，不再返回完整Entity
        /// </summary>
        private async Task<HerbBasicDto?> TryMatchHerbAsync(string herbName)
        {
            if (string.IsNullOrWhiteSpace(herbName))
                return null;

            try
            {
                // Issue #1469 (FORMULA-8): 使用智能药材匹配
                // 优先精确匹配名称，其次模糊匹配拼音码
                // OpenSpec: decouple-server-modules - 使用ICrossModuleQueryService替代IHerbRepository
                var herb = await _crossModuleQuery.GetHerbByNameOrPinyinAsync(herbName);
                return herb;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "药材匹配失败：{HerbName}", herbName);
                return null;
            }
        }

        // Issue #1758: HerbItemData类已移至Client端 ExcelParseHelper

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        public async Task<MemoryStream> ExportAsync(string? category = null)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var formulas = (await _repository.GetAllAsync()).ToList();

                // 应用分类筛选
                if (!string.IsNullOrWhiteSpace(category))
                {
                    formulas = formulas.Where(f =>
                        !string.IsNullOrEmpty(f.Category) &&
                        f.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                }

                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("验方列表");

                    // 表头
                    worksheet.Cells[1, 1].Value = "验方名称";
                    worksheet.Cells[1, 2].Value = "分类";
                    worksheet.Cells[1, 3].Value = "功效";
                    worksheet.Cells[1, 4].Value = "用法";
                    worksheet.Cells[1, 5].Value = "性味归经";
                    worksheet.Cells[1, 6].Value = "方剂类型";
                    worksheet.Cells[1, 7].Value = "是否共享";
                    worksheet.Cells[1, 8].Value = "备注";

                    using (var range = worksheet.Cells[1, 1, 1, 8])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 数据行
                    for (int i = 0; i < formulas.Count; i++)
                    {
                        var formula = formulas[i];
                        int row = i + 2;

                        worksheet.Cells[row, 1].Value = formula.Name;
                        worksheet.Cells[row, 2].Value = formula.Category;
                        worksheet.Cells[row, 3].Value = formula.Effect;
                        worksheet.Cells[row, 4].Value = formula.Usage;
                        worksheet.Cells[row, 5].Value = formula.Property;
                        worksheet.Cells[row, 6].Value = formula.FormulaType == FormulaType.Classic ? "经典方" : "经验方";
                        worksheet.Cells[row, 7].Value = formula.IsShared ? "是" : "否";
                        worksheet.Cells[row, 8].Value = formula.Remark;
                    }

                    worksheet.Cells.AutoFitColumns();
                    package.Save();
                }

                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出验方数据时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 生成验方导入模板 (Issue #1347: 更新为主-从表格式)
        /// 格式：Sheet1=验方信息，Sheet2=药材明细
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {
                    // Sheet1：验方信息
                    var formulaSheet = package.Workbook.Worksheets.Add("验方信息");

                    // 表头
                    formulaSheet.Cells[1, 1].Value = "验方编号*";
                    formulaSheet.Cells[1, 2].Value = "验方名称*";
                    formulaSheet.Cells[1, 3].Value = "分类";
                    formulaSheet.Cells[1, 4].Value = "功效";
                    formulaSheet.Cells[1, 5].Value = "用法";
                    formulaSheet.Cells[1, 6].Value = "性味归经";
                    formulaSheet.Cells[1, 7].Value = "方剂类型";
                    formulaSheet.Cells[1, 8].Value = "是否共享";
                    formulaSheet.Cells[1, 9].Value = "备注";

                    using (var range = formulaSheet.Cells[1, 1, 1, 9])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 示例数据
                    formulaSheet.Cells[2, 1].Value = "F001";
                    formulaSheet.Cells[2, 2].Value = "小柴胡汤";
                    formulaSheet.Cells[2, 3].Value = "和解剂";
                    formulaSheet.Cells[2, 4].Value = "和解少阳，扶正祛邪";
                    formulaSheet.Cells[2, 5].Value = "水煎服，日三次";
                    formulaSheet.Cells[2, 6].Value = "性平，归肝、胆经";
                    formulaSheet.Cells[2, 7].Value = "经典方";
                    formulaSheet.Cells[2, 8].Value = "是";
                    formulaSheet.Cells[2, 9].Value = "《伤寒论》经典名方";

                    formulaSheet.Cells.AutoFitColumns();

                    // Sheet2：药材明细
                    var herbSheet = package.Workbook.Worksheets.Add("药材明细");

                    // 表头
                    herbSheet.Cells[1, 1].Value = "验方编号*";
                    herbSheet.Cells[1, 2].Value = "药材名称*";
                    herbSheet.Cells[1, 3].Value = "剂量*";
                    herbSheet.Cells[1, 4].Value = "单位";
                    herbSheet.Cells[1, 5].Value = "用法";
                    herbSheet.Cells[1, 6].Value = "炮制方法";
                    herbSheet.Cells[1, 7].Value = "备注";

                    using (var range = herbSheet.Cells[1, 1, 1, 7])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // 示例数据
                    herbSheet.Cells[2, 1].Value = "F001";
                    herbSheet.Cells[2, 2].Value = "柴胡";
                    herbSheet.Cells[2, 3].Value = "24";
                    herbSheet.Cells[2, 4].Value = "g";
                    herbSheet.Cells[2, 5].Value = "";
                    herbSheet.Cells[2, 6].Value = "";
                    herbSheet.Cells[2, 7].Value = "";

                    herbSheet.Cells[3, 1].Value = "F001";
                    herbSheet.Cells[3, 2].Value = "黄芩";
                    herbSheet.Cells[3, 3].Value = "9";
                    herbSheet.Cells[3, 4].Value = "g";
                    herbSheet.Cells[3, 5].Value = "";
                    herbSheet.Cells[3, 6].Value = "";
                    herbSheet.Cells[3, 7].Value = "";

                    herbSheet.Cells[4, 1].Value = "F001";
                    herbSheet.Cells[4, 2].Value = "半夏";
                    herbSheet.Cells[4, 3].Value = "12";
                    herbSheet.Cells[4, 4].Value = "g";
                    herbSheet.Cells[4, 5].Value = "";
                    herbSheet.Cells[4, 6].Value = "";
                    herbSheet.Cells[4, 7].Value = "";

                    herbSheet.Cells.AutoFitColumns();

                    package.Save();
                }

                stream.Position = 0;
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成导入模板时发生错误");
                throw;
            }
        }
    }
}

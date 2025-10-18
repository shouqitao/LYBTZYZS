using AutoMapper;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Interfaces;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Models.Enums;
using OfficeOpenXml;
using FormulaEntity = LYBT.Entities.Formula.Formula;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly IHerbRepository _herbRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaRepository repository,
            IHerbRepository herbRepository,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _repository = repository;
            _herbRepository = herbRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
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
                return ServiceResult<PagedResult<FormulaDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方列表失败");
                return ServiceResult<PagedResult<FormulaDto>>.Failure("获取验方列表失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含所有药材配伍
                var entity = await _repository.GetByIdWithHerbsAsync(id);
                if (entity == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                var dto = _mapper.Map<FormulaDto>(entity);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败");
                return ServiceResult<FormulaDto>.Failure("获取验方详情失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<FormulaEntity>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                return ServiceResult<FormulaDto>.Failure("创建验方失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败");
                return ServiceResult<FormulaDto>.Failure("更新验方失败");
            }
        }

        public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 简化搜索逻辑 - 直接使用分页查询，取前100个结果
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>());
                }

                var pagedResult = await _repository.GetPagedWithDetailsAsync(1, 100, keyword);
                var formulaDtos = _mapper.Map<List<FormulaDto>>(pagedResult.Items);

                return ServiceResult<List<FormulaDto>>.Success(formulaDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方时发生错误，关键字：{Keyword}", keyword);
                return ServiceResult<List<FormulaDto>>.Failure($"搜索处方失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId)
        {
            try
            {
                // 获取原始处方（包含药材信息）
                var originalFormula = await _repository.GetByIdWithHerbsAsync(formulaId);
                if (originalFormula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("未找到要克隆的处方");
                }

                // 简化克隆逻辑 - 仅复制核心信息
                var clonedFormula = new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = $"{originalFormula.Name}_副本",
                    Effect = originalFormula.Effect,
                    Usage = originalFormula.Usage,
                    Category = originalFormula.Category,
                    FormulaType = originalFormula.FormulaType,
                    IsShared = false, // 克隆的方剂默认不共享
                                      // 不复制药材配伍，让用户重新配置
                };

                await _repository.AddAsync(clonedFormula);
                await _repository.SaveChangesAsync();

                var formulaDto = _mapper.Map<FormulaDto>(clonedFormula);
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆处方时发生错误，处方ID：{FormulaId}", formulaId);
                return ServiceResult<FormulaDto>.Failure($"克隆处方失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败");
                return ServiceResult.Failure("删除验方失败");
            }
        }


        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        public async Task<ServiceResult> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId)
        {
            try
            {
                // 1. 查询验方（包含所有药材）
                var formula = await _repository.GetByIdWithHerbsAsync(formulaId);
                if (formula == null)
                {
                    return ServiceResult.Failure("验方不存在");
                }

                // 2. 查找待验证的药材项
                var herbItem = formula.Herbs.FirstOrDefault(h => h.Id == herbItemId);
                if (herbItem == null)
                {
                    return ServiceResult.Failure("药材项不存在");
                }

                // 3. 验证是否已校验
                if (herbItem.IsValidated)
                {
                    return ServiceResult.Failure("该药材已校验，无需重复操作");
                }

                // 4. 查询选定的药材
                var selectedHerb = await _herbRepository.GetByIdAsync(selectedHerbId);
                if (selectedHerb == null)
                {
                    return ServiceResult.Failure("所选药材不存在");
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

                // 8. 返回详细映射结果
                if (allValidated)
                {
                    return ServiceResult.Success($"药材\"{herbItem.OriginalHerbName}\"已映射为\"{selectedHerb.Name}\"，验方\"{formula.Name}\"所有药材已校验完成");
                }
                else
                {
                    return ServiceResult.Success($"药材\"{herbItem.OriginalHerbName}\"已映射为\"{selectedHerb.Name}\"");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证验方药材失败：formulaId={FormulaId}, herbItemId={HerbItemId}, selectedHerbId={SelectedHerbId}", 
                    formulaId, herbItemId, selectedHerbId);
                return ServiceResult.Failure("验证验方药材失败");
            }
        }


        /// <summary>
        /// 获取待验证的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetPendingValidationFormulasAsync()
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
                return ServiceResult<List<FormulaDto>>.Success(formulaDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取待验证验方列表失败");
                return ServiceResult<List<FormulaDto>>.Failure("获取待验证验方列表失败");
            }
        }


        /// <summary>
        /// 批量删除验方（软删除）(Issue #1169)
        /// </summary>
        public async Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            const int MAX_BATCH_SIZE = 100;

            try
            {
                // 批量大小限制
                if (ids.Count > MAX_BATCH_SIZE)
                {
                    return ServiceResult<BatchOperationResultDto>.Failure($"批量操作最多支持{MAX_BATCH_SIZE}条记录");
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

                return ServiceResult<BatchOperationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除验方异常");
                return ServiceResult<BatchOperationResultDto>.Failure("批量删除验方失败");
            }
        }


        /// <summary>
        /// 从Excel文件导入验方数据 (Issue #1347: 重写为主-从表格式，支持延迟绑定)
        /// 格式：Sheet1=验方信息，Sheet2=药材明细
        /// </summary>
        public async Task<ServiceResult<FormulaImportResultDto>> ImportFromExcelAsync(Stream stream, string? fileName = null)
        {
            var result = new FormulaImportResultDto
            {
                FileName = fileName,
                ImportTime = DateTime.Now,
                StartTime = DateTime.Now
            };

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage(stream);

                // 获取Sheet1：验方信息
                var formulaSheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name.Contains("验方") || ws.Index == 0);
                if (formulaSheet == null)
                {
                    result.IsSuccess = false;
                    result.Message = "未找到验方信息工作表";
                    return ServiceResult<FormulaImportResultDto>.Failure("Excel文件格式错误");
                }

                // 获取Sheet2：药材明细
                var herbSheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name.Contains("药材") || ws.Index == 1);
                if (herbSheet == null)
                {
                    result.IsSuccess = false;
                    result.Message = "未找到药材明细工作表";
                    return ServiceResult<FormulaImportResultDto>.Failure("Excel文件格式错误");
                }

                var formulaRowCount = formulaSheet.Dimension?.Rows ?? 0;
                if (formulaRowCount <= 1)
                {
                    result.IsSuccess = false;
                    result.Message = "验方信息表中没有数据行";
                    return ServiceResult<FormulaImportResultDto>.Success(result);
                }

                result.TotalCount = formulaRowCount - 1;

                // 第一步：解析Sheet2药材明细，按验方编号分组
                var herbItemsByFormulaCode = ParseHerbItems(herbSheet);

                // 第二步：逐行导入验方及其药材
                for (int row = 2; row <= formulaRowCount; row++)
                {
                    try
                    {
                        // 读取验方基础信息
                        var formulaCode = formulaSheet.Cells[row, 1].Text?.Trim();
                        var name = formulaSheet.Cells[row, 2].Text?.Trim();
                        var category = formulaSheet.Cells[row, 3].Text?.Trim();
                        var effect = formulaSheet.Cells[row, 4].Text?.Trim();
                        var usage = formulaSheet.Cells[row, 5].Text?.Trim();
                        var property = formulaSheet.Cells[row, 6].Text?.Trim();
                        var formulaTypeText = formulaSheet.Cells[row, 7].Text?.Trim();
                        var isSharedText = formulaSheet.Cells[row, 8].Text?.Trim();
                        var remark = formulaSheet.Cells[row, 9].Text?.Trim();

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.FailureCount++;
                            result.FailedItems.Add(new FormulaImportErrorDto
                            {
                                RowIndex = row,
                                FormulaName = name ?? string.Empty,
                                ErrorMessage = "验方名称不能为空"
                            });
                            continue;
                        }

                        // 解析方剂类型（默认为经验方）
                        var formulaType = FormulaType.Experience;
                        if (!string.IsNullOrWhiteSpace(formulaTypeText))
                        {
                            if (formulaTypeText.Contains("经典", StringComparison.OrdinalIgnoreCase))
                                formulaType = FormulaType.Classic;
                        }

                        // 解析是否共享（默认为否）
                        var isShared = false;
                        if (!string.IsNullOrWhiteSpace(isSharedText))
                        {
                            isShared = isSharedText.Equals("是", StringComparison.OrdinalIgnoreCase) ||
                                      isSharedText.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                      isSharedText.Equals("共享", StringComparison.OrdinalIgnoreCase);
                        }

                        // 创建验方实体
                        var formula = new FormulaEntity
                        {
                            Name = name,
                            Category = category,
                            Effect = effect,
                            Usage = usage,
                            Property = property,
                            FormulaType = formulaType,
                            IsShared = isShared,
                            Remark = remark,
                            Status = CommonStatus.Enabled,
                            ValidationStatus = FormulaValidationStatus.Draft, // 导入的验方初始为Draft
                            CreatedAt = DateTime.Now,
                            Herbs = new List<FormulaHerbItem>()
                        };

                        // 第三步：关联药材明细（如果有验方编号）
                        if (!string.IsNullOrWhiteSpace(formulaCode) && herbItemsByFormulaCode.ContainsKey(formulaCode))
                        {
                            var herbItems = herbItemsByFormulaCode[formulaCode];
                            foreach (var herbItem in herbItems)
                            {
                                // 尝试自动匹配药材
                                var matchedHerb = await TryMatchHerbAsync(herbItem.HerbName);

                                formula.Herbs.Add(new FormulaHerbItem
                                {
                                    Id = Guid.NewGuid(),
                                    HerbId = matchedHerb?.Id,
                                    HerbName = herbItem.HerbName,
                                    OriginalHerbName = herbItem.HerbName, // 保存原始名称
                                    IsValidated = matchedHerb != null, // 成功匹配则标记为已验证
                                    Quantity = herbItem.Quantity,
                                    Unit = herbItem.Unit ?? "g",
                                    Usage = herbItem.Usage,
                                    ProcessingMethod = herbItem.ProcessingMethod,
                                    Remark = herbItem.Remark
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
                        }

                        // 自动判断验证状态：如果所有药材都已验证，则标记为Validated
                        if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
                        {
                            formula.ValidationStatus = FormulaValidationStatus.Validated;
                        }

                        var savedFormula = await _repository.AddAsync(formula);
                        var formulaDto = _mapper.Map<FormulaDto>(savedFormula);

                        result.SuccessCount++;
                        result.SuccessfulIds.Add(savedFormula.Id);
                        result.SuccessfulFormulas.Add(formulaDto);
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.FailedItems.Add(new FormulaImportErrorDto
                        {
                            RowIndex = row,
                            FormulaName = string.Empty,
                            ErrorMessage = $"导入失败：{ex.Message}",
                            ErrorDetails = ex.StackTrace
                        });
                        _logger.LogError(ex, "导入第{Row}行时发生错误", row);
                    }
                }

                result.EndTime = DateTime.Now;
                result.IsSuccess = true;
                result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

                return ServiceResult<FormulaImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方数据时发生错误");
                result.EndTime = DateTime.Now;
                result.IsSuccess = false;
                result.Message = $"导入失败：{ex.Message}";
                return ServiceResult<FormulaImportResultDto>.Failure($"导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 解析Sheet2药材明细，按验方编号分组
        /// </summary>
        private Dictionary<string, List<HerbItemData>> ParseHerbItems(ExcelWorksheet herbSheet)
        {
            var result = new Dictionary<string, List<HerbItemData>>();
            var rowCount = herbSheet.Dimension?.Rows ?? 0;

            for (int row = 2; row <= rowCount; row++)
            {
                var formulaCode = herbSheet.Cells[row, 1].Text?.Trim();
                var herbName = herbSheet.Cells[row, 2].Text?.Trim();
                var quantityText = herbSheet.Cells[row, 3].Text?.Trim();
                var unit = herbSheet.Cells[row, 4].Text?.Trim();
                var usage = herbSheet.Cells[row, 5].Text?.Trim();
                var processingMethod = herbSheet.Cells[row, 6].Text?.Trim();
                var remark = herbSheet.Cells[row, 7].Text?.Trim();

                if (string.IsNullOrWhiteSpace(formulaCode) || string.IsNullOrWhiteSpace(herbName))
                    continue;

                int.TryParse(quantityText, out int quantity);
                if (quantity <= 0) quantity = 1;

                if (!result.ContainsKey(formulaCode))
                {
                    result[formulaCode] = new List<HerbItemData>();
                }

                result[formulaCode].Add(new HerbItemData
                {
                    HerbName = herbName,
                    Quantity = quantity,
                    Unit = unit,
                    Usage = usage,
                    ProcessingMethod = processingMethod,
                    Remark = remark
                });
            }

            return result;
        }

        /// <summary>
        /// 尝试自动匹配药材（按名称或拼音码）
        /// </summary>
        private async Task<LYBT.Entities.Herbs.Herb?> TryMatchHerbAsync(string herbName)
        {
            if (string.IsNullOrWhiteSpace(herbName))
                return null;

            try
            {
                // Issue #1469 (FORMULA-8): 使用智能药材匹配
                // 优先精确匹配名称，其次模糊匹配拼音码
                var herb = await _herbRepository.GetByNameOrPinyinAsync(herbName);
                return herb;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "药材匹配失败：{HerbName}", herbName);
                return null;
            }
        }

        /// <summary>
        /// 药材明细数据临时类
        /// </summary>
        private class HerbItemData
        {
            public string HerbName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string? Unit { get; set; }
            public string? Usage { get; set; }
            public string? ProcessingMethod { get; set; }
            public string? Remark { get; set; }
        }

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

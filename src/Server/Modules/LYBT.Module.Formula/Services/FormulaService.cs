using AutoMapper;
using LYBT.Entities.Formula;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Interfaces.Services;
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
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaRepository repository,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _repository = repository;
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
        /// 从Excel文件导入验方数据 (Issue #1166)
        /// </summary>
        public async Task<ServiceResult<ImportResultDto<FormulaDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
        {
            var result = new ImportResultDto<FormulaDto>
            {
                FileName = fileName,
                ImportTime = DateTime.Now
            };

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    result.IsSuccess = false;
                    result.Message = "Excel文件中没有工作表";
                    return ServiceResult<ImportResultDto<FormulaDto>>.Failure("Excel文件格式错误");
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    result.IsSuccess = false;
                    result.Message = "Excel文件中没有数据行";
                    return ServiceResult<ImportResultDto<FormulaDto>>.Success(result);
                }

                result.TotalCount = rowCount - 1;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var name = worksheet.Cells[row, 1].Text?.Trim();
                        var category = worksheet.Cells[row, 2].Text?.Trim();
                        var effect = worksheet.Cells[row, 3].Text?.Trim();
                        var usage = worksheet.Cells[row, 4].Text?.Trim();
                        var property = worksheet.Cells[row, 5].Text?.Trim();
                        var formulaTypeText = worksheet.Cells[row, 6].Text?.Trim();
                        var isSharedText = worksheet.Cells[row, 7].Text?.Trim();
                        var remark = worksheet.Cells[row, 8].Text?.Trim();

                        if (string.IsNullOrWhiteSpace(name))
                        {
                            result.FailureCount++;
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = $"第{row}行",
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
                            CreatedAt = DateTime.Now
                        };

                        var savedFormula = await _repository.AddAsync(formula);
                        var formulaDto = _mapper.Map<FormulaDto>(savedFormula);

                        result.SuccessCount++;
                        result.SuccessfulIds.Add(savedFormula.Id);
                        result.ImportedData.Add(formulaDto);
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = $"第{row}行",
                            ErrorMessage = $"导入失败：{ex.Message}"
                        });
                        _logger.LogError(ex, "导入第{Row}行时发生错误", row);
                    }
                }

                result.IsSuccess = true;
                result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

                return ServiceResult<ImportResultDto<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入验方数据时发生错误");
                result.IsSuccess = false;
                result.Message = $"导入失败：{ex.Message}";
                return ServiceResult<ImportResultDto<FormulaDto>>.Failure($"导入失败：{ex.Message}");
            }
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
        /// 生成验方导入模板 (Issue #1166)
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var stream = new MemoryStream();
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("验方信息");

                    // 表头
                    worksheet.Cells[1, 1].Value = "验方名称*";
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

                    // 示例数据
                    worksheet.Cells[2, 1].Value = "小柴胡汤";
                    worksheet.Cells[2, 2].Value = "和解剂";
                    worksheet.Cells[2, 3].Value = "和解少阳，扶正祛邪";
                    worksheet.Cells[2, 4].Value = "水煎服，日三次";
                    worksheet.Cells[2, 5].Value = "性平，归肝、胆经";
                    worksheet.Cells[2, 6].Value = "经典方";
                    worksheet.Cells[2, 7].Value = "是";
                    worksheet.Cells[2, 8].Value = "《伤寒论》经典名方";

                    worksheet.Cells.AutoFitColumns();
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

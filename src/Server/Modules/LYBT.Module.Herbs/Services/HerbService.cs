using AutoMapper;
using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Herbs.Services
{
    /// <summary>
    /// 药材服务 - 简化版，只包含基础CRUD
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<HerbService> _logger;

        public HerbService(
            IHerbRepository repository,
            IMapper mapper,
            ILogger<HerbService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dtos = _mapper.Map<List<HerbDto>>(pagedResult.Items);
                
                // Issue #1164: 应用分类筛选（在DTO级别过滤）
                if (!string.IsNullOrWhiteSpace(category))
                {
                    dtos = dtos.Where(h => 
                        !string.IsNullOrEmpty(h.Category) && 
                        h.Category.Contains(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                }
                
                var dto = new PagedResult<HerbDto>
                {
                    Items = dtos,
                    TotalCount = !string.IsNullOrWhiteSpace(category) ? dtos.Count : pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<HerbDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材列表失败");
                return ServiceResult<PagedResult<HerbDto>>.Failure("获取药材列表失败");
            }
        }

        public async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                var dto = _mapper.Map<HerbDto>(entity);
                return ServiceResult<HerbDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取药材详情失败");
                return ServiceResult<HerbDto>.Failure("获取药材详情失败");
            }
        }

        public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<Herb>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<HerbDto>(result);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建药材失败");
                return ServiceResult<HerbDto>.Failure("创建药材失败");
            }
        }

        public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<HerbDto>.Failure("药材不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<HerbDto>(result);
                return ServiceResult<HerbDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新药材失败");
                return ServiceResult<HerbDto>.Failure("更新药材失败");
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
                _logger.LogError(ex, "删除药材失败");
                return ServiceResult.Failure("删除药材失败");
            }
        }


        /// <summary>
        /// 批量删除药材（软删除）(Issue #1169)
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

                foreach (var herbId in ids)
                {
                    try
                    {
                        // 检查药材是否存在
                        var herb = await _repository.GetByIdAsync(herbId);
                        if (herb == null)
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(herbId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = herbId.ToString(),
                                ErrorMessage = "药材不存在"
                            });
                            continue;
                        }

                        // TODO: 检查药材是否被处方或验方使用（后续迭代）
                        // 现在MVP阶段直接允许删除

                        // 执行删除
                        var deleteResult = await _repository.DeleteAsync(herbId);
                        if (deleteResult)
                        {
                            result.SuccessCount++;
                            result.SuccessfulIds.Add(herbId);
                        }
                        else
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(herbId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = herbId.ToString(),
                                ErrorMessage = "删除失败"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(herbId);
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = herbId.ToString(),
                            ErrorMessage = ex.Message
                        });
                        _logger.LogError(ex, "批量删除药材失败: {HerbId}", herbId);
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

                _logger.LogInformation("批量删除药材完成: 总数{Total}, 成功{Success}, 失败{Failed}", 
                    result.TotalCount, result.SuccessCount, result.FailureCount);

                return ServiceResult<BatchOperationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除药材异常");
                return ServiceResult<BatchOperationResultDto>.Failure("批量删除药材失败");
            }
        }

        public async Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword)
        {
            try
            {
                var entities = await _repository.FindAsync(h =>
                    h.Name.Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
                var dtos = _mapper.Map<List<HerbDto>>(entities);
                return ServiceResult<List<HerbDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索药材失败: {Keyword}", keyword);
                return ServiceResult<List<HerbDto>>.Failure("搜索药材失败");
            }
        }

        /// <summary>
        /// 从Excel文件导入药材数据 (Issue #1166)
        /// </summary>
        public async Task<ServiceResult<ImportResultDto<HerbDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null)
        {
            var result = new ImportResultDto<HerbDto>
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
                    return ServiceResult<ImportResultDto<HerbDto>>.Failure("Excel文件格式错误");
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    result.IsSuccess = false;
                    result.Message = "Excel文件中没有数据行";
                    return ServiceResult<ImportResultDto<HerbDto>>.Success(result);
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
                        var herbDto = _mapper.Map<HerbDto>(savedHerb);

                        result.SuccessCount++;
                        result.SuccessfulIds.Add(savedHerb.Id);
                        result.ImportedData.Add(herbDto);
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

                return ServiceResult<ImportResultDto<HerbDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入药材数据时发生错误");
                result.IsSuccess = false;
                result.Message = $"导入失败：{ex.Message}";
                return ServiceResult<ImportResultDto<HerbDto>>.Failure($"导入失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 导出药材数据到Excel (Issue #1166)
        /// </summary>
        public async Task<MemoryStream> ExportAsync(string? category = null)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var herbs = await _repository.GetAllAsync();
                var herbDtos = _mapper.Map<List<HerbDto>>(herbs);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 生成药材导入模板 (Issue #1166)
        /// </summary>
        public MemoryStream GenerateImportTemplate()
        {
            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成导入模板时发生错误");
                throw;
            }
        }
    }
}

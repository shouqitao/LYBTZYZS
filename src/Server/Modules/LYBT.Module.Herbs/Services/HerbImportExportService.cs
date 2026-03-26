using LYBT.Entities.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
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
    /// 药材导入导出服务
    /// 从 HerbService 拆分出的导入/导出职责，包含 Excel 导入、导出、模板生成和批量导入逻辑
    /// </summary>
    public class HerbImportExportService : IHerbImportExportService
    {
        private readonly IHerbRepository _repository;
        private readonly ILogger<HerbImportExportService> _logger;
        private readonly HerbMapper _mapper = new();

        public HerbImportExportService(
            IHerbRepository repository,
            ILogger<HerbImportExportService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// 从Excel文件导入药材数据 (Issue #1166)
        /// </summary>
        public async Task<Result<ImportResultDto<HerbDetailDto>>> ImportFromExcelAsync(Stream stream, string? fileName = null, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除外层冗余try-catch，保留行级错误隔离
            var result = new ImportResultDto<HerbDetailDto>
            {
                FileName = fileName,
                ImportTime = DateTime.UtcNow
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
                cancellationToken.ThrowIfCancellationRequested();
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
                        CreatedAt = DateTime.UtcNow
                    };

                    var savedHerb = await _repository.AddAsync(herb);
                    var herbDto = _mapper.ToDetailDto(savedHerb);

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
        public async Task<MemoryStream> ExportAsync(string? category = null, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch-rethrow，异常由IExceptionHandler统一处理
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var herbs = await _repository.GetAllAsync();
            var herbDtos = _mapper.ToDetailDtos(herbs.ToList());

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

        /// <summary>
        /// 批量导入药材（Epic #1962 Task 2.2）
        /// </summary>
        public async Task<Result<HerbBatchImportResultDto>> BatchImportAsync(List<HerbInputDto> herbs, DuplicateStrategy strategy, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除外层冗余try-catch，保留行级错误隔离
            const int MAX_IMPORT_SIZE = 10000; // BR-006

            var result = new HerbBatchImportResultDto
            {
                ImportTime = DateTime.UtcNow
            };

            // BR-006: 批量导入数量限制
            if (herbs.Count > MAX_IMPORT_SIZE)
            {
                return Result<HerbBatchImportResultDto>.Failure($"批量导入最多支持{MAX_IMPORT_SIZE}条记录");
            }

            _logger.LogInformation("[SVC] Herb.BatchImport started - Count={Count} Strategy={Strategy}", herbs.Count, strategy);

            for (int i = 0; i < herbs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
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
                                    _mapper.UpdateEntity(dto, existingHerb);
                                    existingHerb.UpdatedAt = DateTime.UtcNow;
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
                    var entity = _mapper.ToEntity(dto);
                    entity.CreatedAt = DateTime.UtcNow;
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
        public async Task<Result<List<HerbDetailDto>>> GetAllForExportAsync(string? category = null, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            var herbs = await _repository.GetAllAsync();
            var herbDtos = _mapper.ToDetailDtos(herbs.ToList());

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
    }
}

using LYBT.Entities.Formulas;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Formulas.Services;

/// <summary>
/// 验方导入导出服务实现
/// OpenSpec: refactor-server-srp-patterns - 从FormulaService拆分Import/Export职责
/// Issue #1166: 验方导入导出功能
/// </summary>
public class FormulaImportExportService : IFormulaImportExportService
{
    private readonly IFormulaRepository _repository;
    private readonly IHerbCrossModuleService _crossModuleQuery;
    private readonly ILogger<FormulaImportExportService> _logger;
    private readonly FormulaMapper _mapper = new();

    public FormulaImportExportService(
        IFormulaRepository repository,
        IHerbCrossModuleService crossModuleQuery,
        ILogger<FormulaImportExportService> logger)
    {
        _repository = repository;
        _crossModuleQuery = crossModuleQuery;
        _logger = logger;
    }

    /// <summary>
    /// 从结构化数据导入验方 (Issue #1758: 架构重构 - Server端不再依赖Excel格式)
    /// Client端负责Excel解析，Server端只处理业务逻辑
    /// </summary>
    public async Task<Result<FormulaBatchImportResultDto>> ImportFromDataAsync(
        List<FormulaImportItemDto> formulas, 
        string? fileName = null)
    {
        var result = new FormulaBatchImportResultDto
        {
            FileName = fileName,
            ImportTime = DateTime.Now,
            StartTime = DateTime.Now,
            TotalCount = formulas.Count
        };

        int index = 0;
        foreach (var formulaImportItem in formulas)
        {
            index++;
            try
            {
                if (string.IsNullOrWhiteSpace(formulaImportItem.Name))
                {
                    result.FailureCount++;
                    result.Failures.Add(new FormulaImportFailureDto
                    {
                        RowIndex = index,
                        FormulaName = formulaImportItem.Name ?? string.Empty,
                        ErrorMessage = "验方名称不能为空"
                    });
                    continue;
                }

                // 创建验方实体（从DTO映射）
                var formula = new Formula
                {
                    Name = formulaImportItem.Name,
                    Effect = formulaImportItem.Effect,
                    Usage = formulaImportItem.Usage,
                    Property = formulaImportItem.Property,
                    IsShared = formulaImportItem.IsShared,
                    Remark = formulaImportItem.Remark,
                    Status = CommonStatus.Enabled,
                    ValidationStatus = FormulaValidationStatus.Draft,
                    CreatedAt = DateTime.Now,
                    Herbs = new List<FormulaHerbItem>()
                };

                // 添加药材（从DTO列表）
                foreach (var herbDto in formulaImportItem.Herbs)
                {
                    var matchedHerb = await TryMatchHerbAsync(herbDto.HerbName);

                    formula.Herbs.Add(new FormulaHerbItem
                    {
                        Id = Guid.NewGuid(),
                        HerbId = matchedHerb?.Id,
                        HerbName = herbDto.HerbName,
                        OriginalHerbName = herbDto.HerbName,
                        IsValidated = matchedHerb != null,
                        Dosage = herbDto.Dosage,
                        Unit = herbDto.Unit ?? string.Empty,
                        Usage = herbDto.Usage,
                        ProcessingMethod = herbDto.Preparation
                    });

                    if (matchedHerb != null)
                    {
                        result.MatchedHerbsCount++;
                    }
                    else
                    {
                        result.UnmatchedHerbsCount++;
                    }
                }

                // 自动判断验证状态
                if (formula.Herbs.Any() && formula.Herbs.All(h => h.IsValidated))
                {
                    formula.ValidationStatus = FormulaValidationStatus.Validated;
                }

                var savedFormula = await _repository.AddAsync(formula);
                var formulaResultDto = _mapper.ToDetailDto(savedFormula);

                result.SuccessCount++;
                result.SuccessfulIds.Add(savedFormula.Id);
                result.SuccessfulFormulas.Add(formulaResultDto);
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Failures.Add(new FormulaImportFailureDto
                {
                    RowIndex = index,
                    FormulaName = formulaImportItem.Name ?? string.Empty,
                    ErrorMessage = "数据处理异常",
                    ErrorDetails = null
                });
                _logger.LogError(ex, "[SVC] FormulaImportExport.Import → ItemError - FormulaName={FormulaName}", formulaImportItem.Name);
            }
        }

        result.EndTime = DateTime.Now;
        result.IsSuccess = true;
        result.Message = $"导入完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条，药材匹配 {result.MatchedHerbsCount} 个，未匹配 {result.UnmatchedHerbsCount} 个";

        return Result<FormulaBatchImportResultDto>.Success(result);
    }

    /// <summary>
    /// 导出验方数据到Excel (Issue #1166)
    /// </summary>
    public async Task<MemoryStream> ExportAsync(string? category = null)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var formulas = (await _repository.GetAllAsync()).ToList();

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

    /// <summary>
    /// 生成验方导入模板 (Issue #1347: 主-从表格式)
    /// </summary>
    public MemoryStream GenerateImportTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            // Sheet1：验方信息
            var formulaSheet = package.Workbook.Worksheets.Add("验方信息");

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

            herbSheet.Cells[3, 1].Value = "F001";
            herbSheet.Cells[3, 2].Value = "黄芩";
            herbSheet.Cells[3, 3].Value = "9";
            herbSheet.Cells[3, 4].Value = "g";

            herbSheet.Cells[4, 1].Value = "F001";
            herbSheet.Cells[4, 2].Value = "半夏";
            herbSheet.Cells[4, 3].Value = "12";
            herbSheet.Cells[4, 4].Value = "g";

            herbSheet.Cells.AutoFitColumns();

            package.Save();
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// 尝试匹配药材 - 使用ICrossModuleService
    /// </summary>
    private async Task<HerbBasicDto?> TryMatchHerbAsync(string herbName)
    {
        if (string.IsNullOrWhiteSpace(herbName))
            return null;

        try
        {
            var herb = await _crossModuleQuery.GetHerbByNameOrPinyinAsync(herbName);
            return herb;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SVC] FormulaImportExport.MatchHerb → MatchFailed - HerbName={HerbName}", herbName);
            return null;
        }
    }
}

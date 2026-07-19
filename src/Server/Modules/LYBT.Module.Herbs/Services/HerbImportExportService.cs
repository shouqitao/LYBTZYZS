using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace LYBT.Module.Herbs.Services;

/// <summary>
/// 药材导入导出服务实现
/// </summary>
public class HerbImportExportService : IHerbImportExportService
{
    private readonly IHerbRepository _repository;
    private readonly ILogger<HerbImportExportService> _logger;

    public HerbImportExportService(
        IHerbRepository repository,
        ILogger<HerbImportExportService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// 导出药材数据到Excel
    /// </summary>
    public async Task<MemoryStream> ExportAsync(string? category = null)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var herbs = !string.IsNullOrWhiteSpace(category)
            ? await _repository.GetByCategoryAsync(category)
            : (await _repository.GetAllAsync()).ToList();

        var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var worksheet = package.Workbook.Worksheets.Add("药材列表");

            worksheet.Cells[1, 1].Value = "药材名称";
            worksheet.Cells[1, 2].Value = "拼音码";
            worksheet.Cells[1, 3].Value = "分类";
            worksheet.Cells[1, 4].Value = "性味";
            worksheet.Cells[1, 5].Value = "产地";
            worksheet.Cells[1, 6].Value = "规格";
            worksheet.Cells[1, 7].Value = "单位";
            worksheet.Cells[1, 8].Value = "单价";
            worksheet.Cells[1, 9].Value = "成本价";
            worksheet.Cells[1, 10].Value = "功效";
            worksheet.Cells[1, 11].Value = "用法用量";
            worksheet.Cells[1, 12].Value = "备注";
            worksheet.Cells[1, 13].Value = "状态";

            using (var range = worksheet.Cells[1, 1, 1, 13])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < herbs.Count; i++)
            {
                var herb = herbs[i];
                int row = i + 2;

                worksheet.Cells[row, 1].Value = herb.Name;
                worksheet.Cells[row, 2].Value = herb.PinYinCode;
                worksheet.Cells[row, 3].Value = herb.Category;
                worksheet.Cells[row, 4].Value = herb.Properties;
                worksheet.Cells[row, 5].Value = herb.Origin;
                worksheet.Cells[row, 6].Value = herb.Spec;
                worksheet.Cells[row, 7].Value = herb.Unit;
                worksheet.Cells[row, 8].Value = herb.Price;
                worksheet.Cells[row, 9].Value = herb.CostPrice;
                worksheet.Cells[row, 10].Value = herb.Effect;
                worksheet.Cells[row, 11].Value = herb.Usage;
                worksheet.Cells[row, 12].Value = herb.Remark;
                worksheet.Cells[row, 13].Value = herb.Status == CommonStatus.Enabled ? "启用" : "禁用";
            }

            worksheet.Cells.AutoFitColumns();

            package.Save();
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// 生成药材导入模板
    /// </summary>
    public MemoryStream GenerateImportTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var worksheet = package.Workbook.Worksheets.Add("药材模板");

            worksheet.Cells[1, 1].Value = "药材名称*";
            worksheet.Cells[1, 2].Value = "分类";
            worksheet.Cells[1, 3].Value = "性味";
            worksheet.Cells[1, 4].Value = "产地";
            worksheet.Cells[1, 5].Value = "规格";
            worksheet.Cells[1, 6].Value = "单位";
            worksheet.Cells[1, 7].Value = "单价";
            worksheet.Cells[1, 8].Value = "成本价";
            worksheet.Cells[1, 9].Value = "功效";
            worksheet.Cells[1, 10].Value = "用法用量";
            worksheet.Cells[1, 11].Value = "备注";

            using (var range = worksheet.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            worksheet.Cells[2, 1].Value = "当归";
            worksheet.Cells[2, 2].Value = "补血药";
            worksheet.Cells[2, 3].Value = "甘、辛，温";
            worksheet.Cells[2, 4].Value = "甘肃";
            worksheet.Cells[2, 5].Value = "统货";
            worksheet.Cells[2, 6].Value = "克";
            worksheet.Cells[2, 7].Value = 0.15m;
            worksheet.Cells[2, 8].Value = 0.10m;
            worksheet.Cells[2, 9].Value = "补血活血，调经止痛";
            worksheet.Cells[2, 10].Value = "煎服，6-12g";
            worksheet.Cells[2, 11].Value = "常用补血药材";

            worksheet.Cells[3, 1].Value = "黄芪";
            worksheet.Cells[3, 2].Value = "补气药";
            worksheet.Cells[3, 3].Value = "甘，微温";
            worksheet.Cells[3, 4].Value = "内蒙古";
            worksheet.Cells[3, 6].Value = "克";
            worksheet.Cells[3, 7].Value = 0.08m;
            worksheet.Cells[3, 9].Value = "补气升阳，固表止汗";

            worksheet.Cells.AutoFitColumns();

            package.Save();
        }

        stream.Position = 0;
        return stream;
    }
}

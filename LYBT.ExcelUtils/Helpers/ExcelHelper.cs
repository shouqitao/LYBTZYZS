using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.FormulaTemplates.Dtos;

namespace LYBT.ExcelUtils.Helpers;

public static class ExcelHelper {
    public static List<HerbImportDto> ReadHerbs(Stream stream) {
        var result = new List<HerbImportDto>();
        IWorkbook wb = new XSSFWorkbook(stream);
        var sheet = wb.GetSheetAt(0);
        for (int i = 1; i <= sheet.LastRowNum; i++) {
            var row = sheet.GetRow(i);
            if (row == null) continue;
            if (row.Cells.All(c => c == null || string.IsNullOrWhiteSpace(c.ToString())))
                continue;
            var dto = new HerbImportDto {
                Name = row.GetCell(0)?.ToString() ?? string.Empty,
                Pinyin = row.GetCell(1)?.ToString(),
                Origin = row.GetCell(2)?.ToString(),
                Spec = row.GetCell(3)?.ToString(),
                Unit = row.GetCell(4)?.ToString(),
                Price = (decimal)(row.GetCell(5)?.NumericCellValue ?? 0),
                Stock = (int)(row.GetCell(6)?.NumericCellValue ?? 0),
                BatchNo = row.GetCell(7)?.ToString(),
                ExpireDate = row.GetCell(8)?.DateCellValue,
                Effect = row.GetCell(9)?.ToString(),
                Remark = row.GetCell(10)?.ToString()
            };
            result.Add(dto);
        }
        return result;
    }

    public static byte[] WriteHerbs(IEnumerable<HerbDetailDto> data) {
        IWorkbook wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Herbs");
        var header = sheet.CreateRow(0);
        string[] heads = {"Name","Pinyin","Origin","Spec","Unit","Price","Stock","BatchNo","ExpireDate","Effect","Remark"};
        for (int i = 0; i < heads.Length; i++)
            header.CreateCell(i).SetCellValue(heads[i]);
        int r = 1;
        foreach (var h in data) {
            var row = sheet.CreateRow(r++);
            row.CreateCell(0).SetCellValue(h.Name);
            row.CreateCell(1).SetCellValue(h.Pinyin ?? string.Empty);
            row.CreateCell(2).SetCellValue(h.Origin ?? string.Empty);
            row.CreateCell(3).SetCellValue(h.Spec ?? string.Empty);
            row.CreateCell(4).SetCellValue(h.Unit ?? string.Empty);
            row.CreateCell(5).SetCellValue((double)h.Price);
            row.CreateCell(6).SetCellValue(h.Stock);
            row.CreateCell(7).SetCellValue(h.BatchNo ?? string.Empty);
            if (h.ExpireDate.HasValue)
                row.CreateCell(8).SetCellValue(h.ExpireDate.Value);
            else
                row.CreateCell(8).SetCellValue(string.Empty);
            row.CreateCell(9).SetCellValue(h.Effect ?? string.Empty);
            row.CreateCell(10).SetCellValue(h.Remark ?? string.Empty);
        }
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }

    public static List<FormulaTemplateImportDto> ReadTemplates(Stream stream) {
        var result = new List<FormulaTemplateImportDto>();
        IWorkbook wb = new XSSFWorkbook(stream);
        var sheet = wb.GetSheetAt(0);
        for (int i = 1; i <= sheet.LastRowNum; i++) {
            var row = sheet.GetRow(i);
            if (row == null) continue;
            var herbsJson = row.GetCell(1)?.ToString() ?? "[]";
            List<HerbDto>? herbs = JsonSerializer.Deserialize<List<HerbDto>>(herbsJson);
            var dto = new FormulaTemplateImportDto {
                Name = row.GetCell(0)?.ToString() ?? string.Empty,
                Herbs = herbs ?? new List<HerbDto>(),
                Remark = row.GetCell(2)?.ToString()
            };
            result.Add(dto);
        }
        return result;
    }

    public static byte[] WriteTemplates(IEnumerable<FormulaTemplateDetailDto> data) {
        IWorkbook wb = new XSSFWorkbook();
        var sheet = wb.CreateSheet("Templates");
        var header = sheet.CreateRow(0);
        string[] heads = {"Name","Herbs","Remark"};
        for(int i=0;i<heads.Length;i++)
            header.CreateCell(i).SetCellValue(heads[i]);
        int r=1;
        foreach(var t in data) {
            var row = sheet.CreateRow(r++);
            row.CreateCell(0).SetCellValue(t.Name);
            row.CreateCell(1).SetCellValue(JsonSerializer.Serialize(t.Herbs));
            row.CreateCell(2).SetCellValue(t.Remark ?? string.Empty);
        }
        using var ms = new MemoryStream();
        wb.Write(ms, true);
        return ms.ToArray();
    }
}

using System.IO;
using LYBT.Shared.Models.Contracts.Formula;
using OfficeOpenXml;

namespace LYBT.Desktop.Formula.Utilities
{
    /// <summary>
    /// Excel解析工具类 - 负责将Excel文件解析为FormulaImportDto列表
    /// Issue #1758: 从Server端FormulaService迁移Excel解析逻辑到Client端
    ///
    /// 架构原则：
    /// - Server端不应依赖Excel格式，只处理结构化DTO
    /// - Client端负责文件格式解析和转换
    /// </summary>
    public static class ExcelParseHelper
    {
        /// <summary>
        /// 从Excel流解析验方数据
        /// Excel格式：Sheet1=验方信息，Sheet2=药材明细
        /// </summary>
        /// <param name="stream">Excel文件流</param>
        /// <returns>解析后的验方列表</returns>
        public static List<FormulaImportDto> ParseFormulasFromExcel(Stream stream)
        {
            var formulas = new List<FormulaImportDto>();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(stream);

            // 获取Sheet1：验方信息
            var formulaSheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name.Contains("验方") || ws.Index == 0);
            if (formulaSheet == null)
            {
                throw new InvalidOperationException("未找到验方信息工作表");
            }

            // 获取Sheet2：药材明细
            var herbSheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name.Contains("药材") || ws.Index == 1);
            if (herbSheet == null)
            {
                throw new InvalidOperationException("未找到药材明细工作表");
            }

            var formulaRowCount = formulaSheet.Dimension?.Rows ?? 0;
            if (formulaRowCount <= 1)
            {
                return formulas; // 空列表
            }

            // 第一步：解析Sheet2药材明细，按验方编号分组
            var herbItemsByFormulaCode = ParseHerbItems(herbSheet);

            // 第二步：逐行解析验方信息
            for (int row = 2; row <= formulaRowCount; row++)
            {
                try
                {
                    // 读取验方基础信息（与Server端原实现一致）
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
                        continue; // 跳过无效行
                    }

                    // 解析是否共享（默认为否）
                    var isShared = false;
                    if (!string.IsNullOrWhiteSpace(isSharedText))
                    {
                        isShared = isSharedText.Equals("是", StringComparison.OrdinalIgnoreCase) ||
                                  isSharedText.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                  isSharedText.Equals("共享", StringComparison.OrdinalIgnoreCase);
                    }

                    // 创建FormulaImportDto
                    var formulaDto = new FormulaImportDto
                    {
                        Name = name,
                        Effect = effect,
                        Usage = usage,
                        Property = property,
                        IsShared = isShared,
                        Remark = remark,
                        Herbs = new List<FormulaHerbImportDto>()
                    };

                    // 添加关联的药材
                    if (!string.IsNullOrWhiteSpace(formulaCode) && herbItemsByFormulaCode.ContainsKey(formulaCode))
                    {
                        var herbItems = herbItemsByFormulaCode[formulaCode];
                        foreach (var herbItem in herbItems)
                        {
                            formulaDto.Herbs.Add(new FormulaHerbImportDto
                            {
                                HerbName = herbItem.HerbName,
                                Quantity = herbItem.Quantity,
                                Unit = herbItem.Unit ?? "g",
                                Usage = herbItem.Usage,
                                Preparation = herbItem.ProcessingMethod // ProcessingMethod映射到Preparation
                            });
                        }
                    }

                    formulas.Add(formulaDto);
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理下一行
                    // TODO: 可以添加日志记录
                    Console.WriteLine($"解析第{row}行时发生错误: {ex.Message}");
                }
            }

            return formulas;
        }

        /// <summary>
        /// 解析Sheet2药材明细，按验方编号分组
        /// Issue #1758: 从Server端FormulaService.ParseHerbItems迁移
        /// </summary>
        private static Dictionary<string, List<HerbItemData>> ParseHerbItems(ExcelWorksheet herbSheet)
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
        /// 药材明细数据临时类
        /// Issue #1758: 从Server端FormulaService.HerbItemData迁移
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
    }
}

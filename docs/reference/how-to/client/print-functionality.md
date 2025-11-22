# Client端打印功能开发指南

## 📋 文档信息

- **适用范围**: LYBTZYZS项目 - Client端打印功能开发
- **技术栈**: WPF FlowDocument + PrintDialog + System.Printing
- **架构模式**: Builder模式 + Service模式
- **最后更新**: 2025-10-30
- **Issue关联**: #1379 ([PRINT-2] 标准处方模板), #1380 ([PRINT-3] 处方打印服务)

---

## 1. 概述

### 1.1 打印系统架构

LYBTZYZS项目采用WPF原生打印技术栈，遵循**"适度设计、拒绝过度工程"**原则：

```
┌─────────────────────────────────────────────┐
│  ViewModel (命令层)                         │
│  ├── PrintCommand                           │
│  └── PrintPreviewCommand                     │
└─────────────────────────────────────────────┘
              ↓ DelegateCommand
┌─────────────────────────────────────────────┐
│  IPrescriptionPrintService (服务接口)       │
│  ├── PrintPrescriptionAsync()               │
│  ├── PreviewPrescriptionAsync()             │
│  ├── BatchPrintAsync()                      │
│  ├── ExportToPdfAsync() → XPS              │
│  ├── GetAvailablePrinters()                 │
│  └── SetDefaultPrinter()                    │
└─────────────────────────────────────────────┘
              ↓ Implementation
┌─────────────────────────────────────────────┐
│  PrescriptionPrintService (服务实现)        │
│  ├── MapToPrintDtoAsync()                   │
│  │   └── PrescriptionDto → PrintDto        │
│  └── BuildFlowDocument()                    │
│      └── Builder Pattern                    │
└─────────────────────────────────────────────┘
              ↓ Builder Pattern
┌─────────────────────────────────────────────┐
│  PrescriptionFlowDocumentBuilder (构建器)   │
│  ├── AddHeader()         - 诊所抬头         │
│  ├── AddPatientInfo()    - 患者信息         │
│  ├── AddFourDiagnostics() - 四诊信息        │
│  ├── AddPrescriptionTable() - 药材表格      │
│  ├── AddUsageInstructions() - 用法说明      │
│  ├── AddPriceInfo()      - 费用信息         │
│  ├── AddSignature()      - 医生签名         │
│  └── Build() → FlowDocument                 │
└─────────────────────────────────────────────┘
              ↓ WPF Printing
┌─────────────────────────────────────────────┐
│  System.Printing (WPF打印API)               │
│  ├── PrintDialog                            │
│  ├── LocalPrintServer                       │
│  ├── PrintQueue                             │
│  └── XpsDocument (导出XPS)                  │
└─────────────────────────────────────────────┘
```

### 1.2 核心技术栈

| 技术 | 版本 | 用途 |
|-----|------|------|
| WPF FlowDocument | .NET 8.0 | 文档结构和布局 |
| System.Printing | .NET 8.0 | 打印机管理和打印队列 |
| PrintDialog | WPF | 打印对话框UI |
| XpsDocument | .NET 8.0 | XPS文档导出（MVP阶段） |
| Builder模式 | - | 构建复杂文档布局 |

### 1.3 MVP范围约束

**✅ MVP阶段支持**：
- 单个处方打印
- 打印预览
- 批量打印（多个处方连续打印）
- 导出为XPS格式（WPF原生支持）
- 打印机选择和管理

**❌ MVP阶段不支持**：
- PDF直接导出（XPS可通过第三方工具转换）
- 自定义打印模板（固定标准模板）
- 水印和背景图
- 高级打印设置（装订、分页）

---

## 2. 打印服务接口设计

### 2.1 IPrescriptionPrintService接口

```csharp
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Services.Print
{
    /// <summary>
    /// 处方打印服务接口 - 简化版本
    /// Issue #1380: [PRINT-3] 实现处方打印服务
    /// 遵循"适度设计、拒绝过度工程"原则
    /// </summary>
    public interface IPrescriptionPrintService
    {
        /// <summary>
        /// 打印处方（显示PrintDialog）
        /// </summary>
        /// <param name="prescription">处方DTO</param>
        /// <returns>是否打印成功</returns>
        Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 预览处方（显示预览窗口）
        /// </summary>
        /// <param name="prescription">处方DTO</param>
        Task PreviewPrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 批量打印处方
        /// </summary>
        /// <param name="prescriptions">处方列表</param>
        /// <returns>成功打印的数量</returns>
        Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions);

        /// <summary>
        /// 导出处方为XPS（MVP阶段）
        /// </summary>
        /// <param name="prescription">处方DTO</param>
        /// <param name="filePath">保存路径（自动添加.xps扩展名）</param>
        /// <returns>是否导出成功</returns>
        Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath);

        /// <summary>
        /// 获取可用的打印机列表
        /// </summary>
        string[] GetAvailablePrinters();

        /// <summary>
        /// 设置默认打印机
        /// </summary>
        void SetDefaultPrinter(string printerName);

        /// <summary>
        /// 获取当前默认打印机
        /// </summary>
        string? GetDefaultPrinter();
    }
}
```

### 2.2 打印选项模型

```csharp
/// <summary>
/// 打印选项（MVP阶段暂未使用）
/// </summary>
public class PrintOptions
{
    public string? PrinterName { get; set; }
    public int Copies { get; set; } = 1;
    public bool DuplexPrinting { get; set; } = false;
    public PaperSize PaperSize { get; set; } = PaperSize.A4;
    public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;
}

public enum PaperSize
{
    A4,
    A5,
    Letter,
    Legal
}

public enum PrintOrientation
{
    Portrait,      // 纵向
    Landscape      // 横向
}
```

---

## 3. 打印数据模型设计

### 3.1 PrescriptionPrintDto

```csharp
namespace LYBT.Desktop.Prescriptions.Models
{
    /// <summary>
    /// 处方打印数据模型（包含完整打印信息）
    /// Issue #1379: [PRINT-2] 实现标准处方模板
    /// </summary>
    public class PrescriptionPrintDto
    {
        // ==== 诊所信息 ====
        public string ClinicName { get; set; } = "中医门诊";
        public string? ClinicAddress { get; set; }
        public string? ClinicPhone { get; set; }

        // ==== 患者信息 ====
        public string PatientName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime ConsultationDate { get; set; }

        // ==== 四诊信息 ====
        public string? Inspection { get; set; }              // 望诊
        public string? AuscultationOlfaction { get; set; }    // 闻诊
        public string? Inquiry { get; set; }                  // 问诊
        public string? Palpation { get; set; }                // 切诊
        public string? TCMDiagnosis { get; set; }             // 中医诊断
        public string? TreatmentPrinciple { get; set; }       // 治则治法

        // ==== 处方内容 ====
        public List<PrescriptionItemPrintDto> Items { get; set; } = new();
        public int DosageCount { get; set; }
        public string Usage { get; set; } = "水煎服，日一剂，分早晚服";
        public string? Advice { get; set; }

        // ==== 费用信息 ====
        public decimal SingleDosePrice { get; set; }
        public decimal TotalPrice { get; set; }

        // ==== 医生信息 ====
        public string DoctorName { get; set; } = string.Empty;
        public DateTime PrescriptionDate { get; set; }

        // ==== 可选信息 ====
        public string? PrescriptionNumber { get; set; }       // 处方编号
        public string? FormulaSource { get; set; }            // 方剂来源
    }

    /// <summary>
    /// 处方药材打印数据
    /// </summary>
    public class PrescriptionItemPrintDto
    {
        public int SequenceNumber { get; set; }      // 序号
        public string HerbName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }        // 剂量
        public string Unit { get; set; } = "g";      // 单位
    }
}
```

---

## 4. 打印服务实现

### 4.1 核心实现流程

```csharp
using System.Printing;
using System.Windows.Controls;
using System.Windows.Documents;
using LYBT.Desktop.Prescriptions.Models;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方打印服务实现
    /// Issue #1380: [PRINT-3] 实现处方打印服务
    /// </summary>
    public class PrescriptionPrintService : IPrescriptionPrintService
    {
        private readonly ILogger<PrescriptionPrintService> _logger;
        private string? _defaultPrinterName;

        public PrescriptionPrintService(ILogger<PrescriptionPrintService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        public async Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription)
        {
            if (prescription == null)
                throw new ArgumentNullException(nameof(prescription));

            try
            {
                _logger.LogInformation("开始打印处方 ID: {PrescriptionId}", prescription.Id);

                // 1. 构建打印数据模型
                var printDto = await MapToPrintDtoAsync(prescription);

                // 2. 使用Builder构建FlowDocument
                var document = BuildFlowDocument(printDto);

                // 3. 显示打印对话框
                var printDialog = new PrintDialog();

                // 设置默认打印机
                if (!string.IsNullOrEmpty(_defaultPrinterName))
                {
                    try
                    {
                        var printQueue = FindPrintQueue(_defaultPrinterName);
                        if (printQueue != null)
                        {
                            printDialog.PrintQueue = printQueue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "设置默认打印机失败: {PrinterName}", _defaultPrinterName);
                    }
                }

                // 4. 用户确认打印
                if (printDialog.ShowDialog() == true)
                {
                    // 5. 执行打印
                    var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                    printDialog.PrintDocument(paginator, $"处方_{prescription.Id}");

                    _logger.LogInformation("处方打印成功 ID: {PrescriptionId}", prescription.Id);
                    return true;
                }

                _logger.LogInformation("用户取消打印 ID: {PrescriptionId}", prescription.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印处方失败 ID: {PrescriptionId}", prescription.Id);
                throw;
            }
        }
    }
}
```

**关键步骤**：
1. ✅ **MapToPrintDtoAsync**: 将业务DTO转换为打印专用DTO
2. ✅ **BuildFlowDocument**: 使用Builder模式构建FlowDocument
3. ✅ **PrintDialog.ShowDialog**: 显示系统打印对话框
4. ✅ **printDialog.PrintDocument**: 执行实际打印

### 4.2 预览功能实现

```csharp
/// <summary>
/// 预览处方（使用FlowDocumentScrollViewer）
/// </summary>
public async Task PreviewPrescriptionAsync(PrescriptionDto prescription)
{
    if (prescription == null)
        throw new ArgumentNullException(nameof(prescription));

    try
    {
        _logger.LogInformation("开始预览处方 ID: {PrescriptionId}", prescription.Id);

        // 1. 构建打印数据模型
        var printDto = await MapToPrintDtoAsync(prescription);

        // 2. 使用Builder构建FlowDocument
        var document = BuildFlowDocument(printDto);

        // 3. 创建预览窗口
        var previewWindow = new Window
        {
            Title = $"处方预览 - {prescription.Id}",
            Width = 900,
            Height = 1100,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        // 4. 使用FlowDocumentScrollViewer显示文档
        var viewer = new FlowDocumentScrollViewer
        {
            Document = document,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        previewWindow.Content = viewer;
        previewWindow.ShowDialog();

        _logger.LogInformation("处方预览完成 ID: {PrescriptionId}", prescription.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "预览处方失败 ID: {PrescriptionId}", prescription.Id);
        throw;
    }
}
```

**预览窗口特性**：
- ✅ 使用 `FlowDocumentScrollViewer` 显示文档
- ✅ 窗口大小固定（900x1100）模拟A4纸张比例
- ✅ 垂直滚动、禁用水平滚动
- ✅ 居中显示

### 4.3 批量打印实现

```csharp
/// <summary>
/// 批量打印处方（连续打印多个处方）
/// </summary>
public async Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions)
{
    if (prescriptions == null || prescriptions.Length == 0)
        throw new ArgumentException("处方列表不能为空", nameof(prescriptions));

    _logger.LogInformation("开始批量打印 {Count} 个处方", prescriptions.Length);

    int successCount = 0;

    foreach (var prescription in prescriptions)
    {
        try
        {
            if (await PrintPrescriptionAsync(prescription))
            {
                successCount++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量打印处方失败 ID: {PrescriptionId}", prescription.Id);
        }
    }

    _logger.LogInformation("批量打印完成，成功: {SuccessCount}/{TotalCount}",
        successCount, prescriptions.Length);

    return successCount;
}
```

**批量打印特性**：
- ✅ 逐个处方调用 `PrintPrescriptionAsync`
- ✅ 每个处方独立显示PrintDialog（用户可取消单个）
- ✅ 失败不中断后续打印
- ✅ 返回成功打印数量

### 4.4 XPS导出实现

```csharp
using System.IO.Packaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

/// <summary>
/// 导出处方为XPS（MVP阶段：原生WPF支持）
/// </summary>
public async Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath)
{
    if (prescription == null)
        throw new ArgumentNullException(nameof(prescription));

    if (string.IsNullOrEmpty(filePath))
        throw new ArgumentException("文件路径不能为空", nameof(filePath));

    try
    {
        _logger.LogInformation("开始导出处方 ID: {PrescriptionId} 到 {FilePath}",
            prescription.Id, filePath);

        // 确保文件扩展名为.xps
        if (!filePath.EndsWith(".xps", StringComparison.OrdinalIgnoreCase))
        {
            filePath = Path.ChangeExtension(filePath, ".xps");
        }

        // 1. 构建打印数据模型
        var printDto = await MapToPrintDtoAsync(prescription);

        // 2. 使用Builder构建FlowDocument
        var document = BuildFlowDocument(printDto);

        // 3. 创建XPS文档
        using (var package = Package.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            using (var xpsDocument = new XpsDocument(package, CompressionOption.Maximum))
            {
                var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
                writer.Write(paginator);
            }
        }

        _logger.LogInformation("处方导出成功 ID: {PrescriptionId}, 文件: {FilePath}",
            prescription.Id, filePath);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "导出处方失败 ID: {PrescriptionId}", prescription.Id);
        throw;
    }
}
```

**XPS导出优势**：
- ✅ WPF原生支持（无需第三方库）
- ✅ 矢量格式（高质量）
- ✅ 可使用Windows XPS Viewer查看
- ✅ 可通过第三方工具转换为PDF

### 4.5 打印机管理实现

```csharp
using System.Printing;

/// <summary>
/// 获取可用的打印机列表
/// </summary>
public string[] GetAvailablePrinters()
{
    try
    {
        var printServer = new LocalPrintServer();
        var printQueues = printServer.GetPrintQueues();

        return printQueues
            .Where(pq => pq != null && !string.IsNullOrEmpty(pq.Name))
            .Select(pq => pq.Name)
            .ToArray();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取打印机列表失败");
        return Array.Empty<string>();
    }
}

/// <summary>
/// 设置默认打印机（应用程序级别）
/// </summary>
public void SetDefaultPrinter(string printerName)
{
    if (string.IsNullOrEmpty(printerName))
        throw new ArgumentException("打印机名称不能为空", nameof(printerName));

    _defaultPrinterName = printerName;
    _logger.LogInformation("设置默认打印机: {PrinterName}", printerName);
}

/// <summary>
/// 获取当前默认打印机
/// </summary>
public string? GetDefaultPrinter()
{
    return _defaultPrinterName ?? LocalPrintServer.GetDefaultPrintQueue()?.Name;
}

/// <summary>
/// 查找指定名称的打印队列
/// </summary>
private PrintQueue? FindPrintQueue(string printerName)
{
    try
    {
        var printServer = new LocalPrintServer();
        return printServer.GetPrintQueue(printerName);
    }
    catch
    {
        return null;
    }
}
```

---

## 5. FlowDocument构建器实现

### 5.1 Builder模式设计

```csharp
using System.Windows.Documents;
using System.Windows.Media;
using LYBT.Desktop.Prescriptions.Models;

namespace LYBT.Desktop.Prescriptions.Services
{
    /// <summary>
    /// 处方FlowDocument构建器 - 使用Builder模式
    /// Issue #1379: [PRINT-2] 实现标准处方模板
    /// </summary>
    public class PrescriptionFlowDocumentBuilder
    {
        private readonly PrescriptionPrintDto _prescription;
        private readonly FlowDocument _document;

        // 字体定义
        private static readonly FontFamily DefaultFont = new FontFamily("SimSun"); // 宋体
        private static readonly double DefaultFontSize = 12;
        private static readonly double HeaderFontSize = 18;
        private static readonly double SubHeaderFontSize = 14;

        // 颜色定义
        private static readonly Brush BorderBrush = Brushes.Black;
        private static readonly double BorderThickness = 1.0;

        public PrescriptionFlowDocumentBuilder(PrescriptionPrintDto prescription)
        {
            _prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));

            // 初始化FlowDocument（A4纸张）
            _document = new FlowDocument
            {
                PageWidth = 210 * 96 / 25.4,   // A4宽度：210mm转像素（96 DPI）
                PageHeight = 297 * 96 / 25.4,  // A4高度：297mm转像素
                PagePadding = new Thickness(40), // 边距：约10mm
                FontFamily = DefaultFont,
                FontSize = DefaultFontSize,
                LineHeight = 20
            };
        }

        // Builder方法链式调用
        public PrescriptionFlowDocumentBuilder AddHeader() { /* ... */ return this; }
        public PrescriptionFlowDocumentBuilder AddPatientInfo() { /* ... */ return this; }
        public PrescriptionFlowDocumentBuilder AddFourDiagnostics() { /* ... */ return this; }
        public PrescriptionFlowDocumentBuilder AddPrescriptionTable() { /* ... */ return this; }
        public PrescriptionFlowDocumentBuilder AddUsageInstructions() { /* ... */ return this; }
        public PrescriptionFlowDocumentBuilder AddPriceInfo() { /* ... */ return this; }
        public PrescriptionFlowDocumentBuilder AddSignature() { /* ... */ return this; }

        /// <summary>
        /// 构建最终的FlowDocument
        /// </summary>
        public FlowDocument Build()
        {
            return _document;
        }
    }
}
```

**Builder模式使用示例**：
```csharp
var builder = new PrescriptionFlowDocumentBuilder(printDto);

var document = builder
    .AddHeader()               // 1. 诊所抬头
    .AddPatientInfo()          // 2. 患者信息
    .AddFourDiagnostics()      // 3. 四诊信息
    .AddPrescriptionTable()    // 4. 药材表格
    .AddUsageInstructions()    // 5. 用法说明
    .AddPriceInfo()            // 6. 费用信息
    .AddSignature()            // 7. 医生签名
    .Build();                  // 8. 构建最终文档
```

### 5.2 AddHeader - 诊所抬头

```csharp
/// <summary>
/// 添加诊所抬头
/// </summary>
public PrescriptionFlowDocumentBuilder AddHeader()
{
    // 诊所名称 - 居中加粗
    var clinicParagraph = new Paragraph(new Run(_prescription.ClinicName))
    {
        FontSize = HeaderFontSize,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 0, 0, 10)
    };
    _document.Blocks.Add(clinicParagraph);

    // "中医门诊处方笺"标题
    var titleParagraph = new Paragraph(new Run("中医门诊处方笺"))
    {
        FontSize = SubHeaderFontSize,
        FontWeight = FontWeights.Bold,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 0, 0, 5)
    };
    _document.Blocks.Add(titleParagraph);

    // 诊所地址和电话（如果有）
    if (!string.IsNullOrEmpty(_prescription.ClinicAddress) ||
        !string.IsNullOrEmpty(_prescription.ClinicPhone))
    {
        var infoText = string.Join(" | ",
            new[] { _prescription.ClinicAddress, _prescription.ClinicPhone }
            .Where(s => !string.IsNullOrEmpty(s)));

        var infoParagraph = new Paragraph(new Run(infoText))
        {
            FontSize = 10,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 15)
        };
        _document.Blocks.Add(infoParagraph);
    }

    // 分隔线
    AddSeparatorLine();

    return this;
}
```

**输出效果**：
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
              中医门诊
        中医门诊处方笺
   北京市朝阳区xxx街道 | 010-12345678
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 5.3 AddPatientInfo - 患者信息

```csharp
/// <summary>
/// 添加患者信息
/// </summary>
public PrescriptionFlowDocumentBuilder AddPatientInfo()
{
    var section = new Section
    {
        Margin = new Thickness(0, 10, 0, 10)
    };

    // 处方编号（Issue #1551: 第一行显示）
    if (!string.IsNullOrEmpty(_prescription.PrescriptionNumber))
    {
        var numberParagraph = new Paragraph();
        numberParagraph.Inlines.Add(new Run("处方编号：") { FontWeight = FontWeights.Bold });
        numberParagraph.Inlines.Add(new Run(_prescription.PrescriptionNumber)
            { Foreground = Brushes.DarkBlue });
        numberParagraph.Margin = new Thickness(0, 0, 0, 5);
        section.Blocks.Add(numberParagraph);
    }

    var paragraph = new Paragraph();

    // 姓名
    paragraph.Inlines.Add(new Run("姓名：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run($"{_prescription.PatientName}    "));

    // 性别
    paragraph.Inlines.Add(new Run("性别：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run($"{_prescription.Gender}    "));

    // 年龄
    paragraph.Inlines.Add(new Run("年龄：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run($"{_prescription.Age}岁    "));

    // 日期
    paragraph.Inlines.Add(new Run("就诊日期：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run(_prescription.ConsultationDate.ToString("yyyy年MM月dd日")));

    section.Blocks.Add(paragraph);
    _document.Blocks.Add(section);

    AddSeparatorLine();

    return this;
}
```

**输出效果**：
```
处方编号：A1B2C3D4
姓名：张三    性别：男    年龄：45岁    就诊日期：2025年10月30日
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 5.4 AddFourDiagnostics - 四诊信息

```csharp
/// <summary>
/// 添加四诊信息
/// </summary>
public PrescriptionFlowDocumentBuilder AddFourDiagnostics()
{
    var section = new Section
    {
        Margin = new Thickness(0, 10, 0, 10)
    };

    // 四诊信息标题
    var title = new Paragraph(new Run("四诊信息："))
    {
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 0, 0, 5)
    };
    section.Blocks.Add(title);

    // 望诊
    if (!string.IsNullOrEmpty(_prescription.Inspection))
    {
        AddDiagnosticItem(section, "望", _prescription.Inspection);
    }

    // 闻诊
    if (!string.IsNullOrEmpty(_prescription.AuscultationOlfaction))
    {
        AddDiagnosticItem(section, "闻", _prescription.AuscultationOlfaction);
    }

    // 问诊
    if (!string.IsNullOrEmpty(_prescription.Inquiry))
    {
        AddDiagnosticItem(section, "问", _prescription.Inquiry);
    }

    // 切诊
    if (!string.IsNullOrEmpty(_prescription.Palpation))
    {
        AddDiagnosticItem(section, "切", _prescription.Palpation);
    }

    // 中医诊断
    if (!string.IsNullOrEmpty(_prescription.TCMDiagnosis))
    {
        AddDiagnosticItem(section, "中医诊断", _prescription.TCMDiagnosis);
    }

    // 治疗原则
    if (!string.IsNullOrEmpty(_prescription.TreatmentPrinciple))
    {
        AddDiagnosticItem(section, "治疗原则", _prescription.TreatmentPrinciple);
    }

    _document.Blocks.Add(section);
    AddSeparatorLine();

    return this;
}

/// <summary>
/// 添加四诊项目（私有辅助方法）
/// </summary>
private void AddDiagnosticItem(Section section, string label, string content)
{
    var paragraph = new Paragraph
    {
        Margin = new Thickness(0, 2, 0, 2),
        TextIndent = 20  // 缩进20像素
    };

    paragraph.Inlines.Add(new Run($"{label}：") { FontWeight = FontWeights.SemiBold });
    paragraph.Inlines.Add(new Run(content));

    section.Blocks.Add(paragraph);
}
```

**输出效果**：
```
四诊信息：
    望：面色苍白，舌淡苔薄白
    闻：声音低微
    问：头痛3天，畏寒怕冷，无汗
    切：脉浮紧
    中医诊断：外感风寒
    治疗原则：辛温解表，宣肺散寒
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### 5.5 AddPrescriptionTable - 药材表格

```csharp
/// <summary>
/// 添加处方药材表格
/// </summary>
public PrescriptionFlowDocumentBuilder AddPrescriptionTable()
{
    var section = new Section
    {
        Margin = new Thickness(0, 10, 0, 10)
    };

    // 处方内容标题
    var title = new Paragraph(new Run("处方内容："))
    {
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 0, 0, 5)
    };
    section.Blocks.Add(title);

    // 创建表格
    var table = new Table
    {
        CellSpacing = 0,
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(BorderThickness)
    };

    // 定义列
    table.Columns.Add(new TableColumn { Width = new GridLength(50) });   // 序号
    table.Columns.Add(new TableColumn { Width = new GridLength(200) });  // 药材名
    table.Columns.Add(new TableColumn { Width = new GridLength(80) });   // 剂量
    table.Columns.Add(new TableColumn { Width = new GridLength(60) });   // 单位

    // 表头
    var headerGroup = new TableRowGroup();
    var headerRow = new TableRow { FontWeight = FontWeights.Bold, Background = Brushes.LightGray };

    headerRow.Cells.Add(CreateTableCell("序号", TextAlignment.Center));
    headerRow.Cells.Add(CreateTableCell("药材名称", TextAlignment.Center));
    headerRow.Cells.Add(CreateTableCell("剂量", TextAlignment.Center));
    headerRow.Cells.Add(CreateTableCell("单位", TextAlignment.Center));

    headerGroup.Rows.Add(headerRow);
    table.RowGroups.Add(headerGroup);

    // 数据行
    var dataGroup = new TableRowGroup();

    foreach (var item in _prescription.Items.OrderBy(i => i.SequenceNumber))
    {
        var dataRow = new TableRow();

        dataRow.Cells.Add(CreateTableCell(item.SequenceNumber.ToString(), TextAlignment.Center));
        dataRow.Cells.Add(CreateTableCell(item.HerbName, TextAlignment.Left));
        dataRow.Cells.Add(CreateTableCell(item.Quantity.ToString("0.##"), TextAlignment.Right));
        dataRow.Cells.Add(CreateTableCell(item.Unit, TextAlignment.Center));

        dataGroup.Rows.Add(dataRow);
    }

    table.RowGroups.Add(dataGroup);
    section.Blocks.Add(table);

    _document.Blocks.Add(section);

    return this;
}

/// <summary>
/// 创建表格单元格（私有辅助方法）
/// </summary>
private TableCell CreateTableCell(string text, TextAlignment alignment)
{
    var cell = new TableCell(new Paragraph(new Run(text))
    {
        Margin = new Thickness(5),
        TextAlignment = alignment
    })
    {
        BorderBrush = BorderBrush,
        BorderThickness = new Thickness(BorderThickness),
        Padding = new Thickness(5)
    };

    return cell;
}
```

**输出效果**：
```
处方内容：
┌─────┬────────────────┬────────┬──────┐
│序号 │   药材名称      │  剂量  │ 单位 │
├─────┼────────────────┼────────┼──────┤
│  1  │ 麻黄            │   6    │  g   │
│  2  │ 桂枝            │   9    │  g   │
│  3  │ 杏仁            │   9    │  g   │
│  4  │ 甘草            │   3    │  g   │
└─────┴────────────────┴────────┴──────┘
```

### 5.6 AddUsageInstructions - 用法说明

```csharp
/// <summary>
/// 添加用法说明
/// </summary>
public PrescriptionFlowDocumentBuilder AddUsageInstructions()
{
    var section = new Section
    {
        Margin = new Thickness(0, 10, 0, 10)
    };

    var paragraph = new Paragraph();

    // 剂数
    paragraph.Inlines.Add(new Run("剂数：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run($"{_prescription.DosageCount} 剂    "));

    // 用法
    paragraph.Inlines.Add(new Run("用法：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run(_prescription.Usage));

    section.Blocks.Add(paragraph);

    // 医嘱（如果有）
    if (!string.IsNullOrEmpty(_prescription.Advice))
    {
        var adviceParagraph = new Paragraph();
        adviceParagraph.Inlines.Add(new Run("医嘱：") { FontWeight = FontWeights.Bold });
        adviceParagraph.Inlines.Add(new Run(_prescription.Advice));
        adviceParagraph.Margin = new Thickness(0, 5, 0, 0);
        section.Blocks.Add(adviceParagraph);
    }

    _document.Blocks.Add(section);
    AddSeparatorLine();

    return this;
}
```

### 5.7 AddPriceInfo - 费用信息

```csharp
/// <summary>
/// 添加价格信息
/// </summary>
public PrescriptionFlowDocumentBuilder AddPriceInfo()
{
    var section = new Section
    {
        Margin = new Thickness(0, 10, 0, 10)
    };

    var title = new Paragraph(new Run("费用信息："))
    {
        FontWeight = FontWeights.Bold,
        Margin = new Thickness(0, 0, 0, 5)
    };
    section.Blocks.Add(title);

    var paragraph = new Paragraph();

    // 单剂价格
    paragraph.Inlines.Add(new Run("单剂价格："));
    paragraph.Inlines.Add(new Run($"¥{_prescription.SingleDosePrice:F2}    ")
        { FontWeight = FontWeights.Bold });

    // 总价（红色加粗）
    paragraph.Inlines.Add(new Run("总价："));
    paragraph.Inlines.Add(new Run($"¥{_prescription.TotalPrice:F2}")
    {
        FontWeight = FontWeights.Bold,
        FontSize = 14,
        Foreground = Brushes.Red
    });

    section.Blocks.Add(paragraph);
    _document.Blocks.Add(section);

    AddSeparatorLine();

    return this;
}
```

### 5.8 AddSignature - 医生签名

```csharp
/// <summary>
/// 添加医生签名
/// </summary>
public PrescriptionFlowDocumentBuilder AddSignature()
{
    var section = new Section
    {
        Margin = new Thickness(0, 10, 0, 0)
    };

    var paragraph = new Paragraph();

    // 医生签名
    paragraph.Inlines.Add(new Run("医生签名：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run($"_________________ ({_prescription.DoctorName})    "));

    // 日期
    paragraph.Inlines.Add(new Run("日期：") { FontWeight = FontWeights.Bold });
    paragraph.Inlines.Add(new Run(_prescription.PrescriptionDate.ToString("yyyy年MM月dd日")));

    section.Blocks.Add(paragraph);

    // 处方编号（页脚）
    if (!string.IsNullOrEmpty(_prescription.PrescriptionNumber))
    {
        var numberParagraph = new Paragraph(new Run($"处方编号：{_prescription.PrescriptionNumber}"))
        {
            FontSize = 10,
            Foreground = Brushes.Gray,
            Margin = new Thickness(0, 10, 0, 0)
        };
        section.Blocks.Add(numberParagraph);
    }

    _document.Blocks.Add(section);

    return this;
}
```

---

## 6. ViewModel集成

### 6.1 打印命令定义

```csharp
using Prism.Commands;
using LYBT.Desktop.Services.Print;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Prescriptions.ViewModels
{
    public class PrescriptionViewModel : ViewModelBase
    {
        private readonly IPrescriptionPrintService _printService;
        private readonly ILogger<PrescriptionViewModel> _logger;

        public PrescriptionViewModel(
            IPrescriptionPrintService printService,
            ILogger<PrescriptionViewModel> logger)
        {
            _printService = printService;
            _logger = logger;

            // 初始化打印命令
            PrintCommand = new DelegateCommand(ExecutePrint, CanPrint);
            PrintPreviewCommand = new DelegateCommand(async () => await ExecutePrintPreviewAsync());
        }

        // 打印命令
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand PrintPreviewCommand { get; }

        // 当前处方（绑定到UI）
        private PrescriptionDto? _currentPrescription;
        public PrescriptionDto? CurrentPrescription
        {
            get => _currentPrescription;
            set
            {
                SetProperty(ref _currentPrescription, value);
                PrintCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 执行打印
        /// </summary>
        private async void ExecutePrint()
        {
            if (CurrentPrescription == null) return;

            try
            {
                IsBusy = true;
                BusyMessage = "正在打印...";

                var success = await _printService.PrintPrescriptionAsync(CurrentPrescription);

                if (success)
                {
                    _logger.LogInformation("打印成功");
                    ShowSuccessMessage("打印成功");
                }
                else
                {
                    _logger.LogInformation("用户取消打印");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印失败");
                ShowErrorMessage($"打印失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 执行打印预览
        /// </summary>
        private async Task ExecutePrintPreviewAsync()
        {
            if (CurrentPrescription == null) return;

            try
            {
                IsBusy = true;
                BusyMessage = "正在生成预览...";

                await _printService.PreviewPrescriptionAsync(CurrentPrescription);

                _logger.LogInformation("预览完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览失败");
                ShowErrorMessage($"预览失败：{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// 是否可以打印（有处方数据）
        /// </summary>
        private bool CanPrint()
        {
            return CurrentPrescription != null;
        }
    }
}
```

### 6.2 XAML绑定

```xml
<Window x:Class="LYBT.Desktop.Prescriptions.Views.PrescriptionView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="处方管理" Height="600" Width="800">

    <DockPanel>
        <!-- 工具栏 -->
        <ToolBar DockPanel.Dock="Top">
            <!-- 打印预览按钮 -->
            <Button Command="{Binding PrintPreviewCommand}"
                    ToolTip="打印预览">
                <StackPanel Orientation="Horizontal">
                    <Image Source="/Assets/Icons/print-preview.png"
                           Width="24" Height="24" Margin="0,0,5,0"/>
                    <TextBlock Text="预览" VerticalAlignment="Center"/>
                </StackPanel>
            </Button>

            <!-- 打印按钮 -->
            <Button Command="{Binding PrintCommand}"
                    ToolTip="打印处方">
                <StackPanel Orientation="Horizontal">
                    <Image Source="/Assets/Icons/print.png"
                           Width="24" Height="24" Margin="0,0,5,0"/>
                    <TextBlock Text="打印" VerticalAlignment="Center"/>
                </StackPanel>
            </Button>

            <Separator/>

            <!-- 导出按钮 -->
            <Button Command="{Binding ExportCommand}"
                    ToolTip="导出为XPS">
                <StackPanel Orientation="Horizontal">
                    <Image Source="/Assets/Icons/export.png"
                           Width="24" Height="24" Margin="0,0,5,0"/>
                    <TextBlock Text="导出" VerticalAlignment="Center"/>
                </StackPanel>
            </Button>
        </ToolBar>

        <!-- 处方内容显示区域 -->
        <Grid DockPanel.Dock="Top">
            <!-- 处方数据绑定... -->
        </Grid>
    </DockPanel>
</Window>
```

---

## 7. 依赖注入注册

### 7.1 Prism模块注册

```csharp
using Prism.Ioc;
using Prism.Modularity;
using LYBT.Desktop.Services.Print;
using LYBT.Desktop.Prescriptions.Services;

namespace LYBT.Desktop.Prescriptions
{
    /// <summary>
    /// 处方模块（注册打印服务）
    /// </summary>
    public class PrescriptionsModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 注册打印服务（单例）
            containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();

            // 注册其他服务...
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // 模块初始化...
        }
    }
}
```

---

## 8. 常见问题与陷阱

### 8.1 FlowDocument分页问题

**❌ 错误**：未设置PageWidth和PageHeight导致分页错乱
```csharp
var document = new FlowDocument
{
    // ❌ 缺少页面尺寸设置
};
```

**✅ 正确**：明确指定A4纸张尺寸
```csharp
var document = new FlowDocument
{
    PageWidth = 210 * 96 / 25.4,   // A4宽度：210mm → 像素（96 DPI）
    PageHeight = 297 * 96 / 25.4,  // A4高度：297mm → 像素
    PagePadding = new Thickness(40), // 边距
    FontFamily = new FontFamily("SimSun"),
    FontSize = 12
};
```

### 8.2 打印机未找到

**❌ 错误**：直接设置PrintQueue导致异常
```csharp
var printDialog = new PrintDialog();
printDialog.PrintQueue = printServer.GetPrintQueue("不存在的打印机"); // ❌ 可能null
```

**✅ 正确**：使用try-catch保护
```csharp
try
{
    var printQueue = FindPrintQueue(_defaultPrinterName);
    if (printQueue != null)
    {
        printDialog.PrintQueue = printQueue;
    }
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "设置默认打印机失败");
    // 回退到系统默认打印机
}
```

### 8.3 表格边框不显示

**❌ 错误**：未设置TableCell的BorderBrush和BorderThickness
```csharp
var cell = new TableCell(new Paragraph(new Run(text)));
// ❌ 缺少边框设置
```

**✅ 正确**：为每个TableCell设置边框
```csharp
var cell = new TableCell(new Paragraph(new Run(text)))
{
    BorderBrush = Brushes.Black,
    BorderThickness = new Thickness(1),
    Padding = new Thickness(5)
};
```

### 8.4 XPS导出文件损坏

**❌ 错误**：未正确关闭Package和XpsDocument
```csharp
var package = Package.Open(filePath, FileMode.Create);
var xpsDocument = new XpsDocument(package);
// ❌ 未使用using，可能导致文件损坏
```

**✅ 正确**：使用using确保资源释放
```csharp
using (var package = Package.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
{
    using (var xpsDocument = new XpsDocument(package, CompressionOption.Maximum))
    {
        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
        writer.Write(paginator);
    }
}
```

### 8.5 字体不显示中文

**❌ 错误**：使用不支持中文的字体
```csharp
var document = new FlowDocument
{
    FontFamily = new FontFamily("Arial")  // ❌ Arial不支持中文
};
```

**✅ 正确**：使用支持中文的字体
```csharp
var document = new FlowDocument
{
    FontFamily = new FontFamily("SimSun")  // ✅ 宋体支持中文
    // 或使用: "Microsoft YaHei"（微软雅黑）
};
```

### 8.6 预览窗口不显示

**❌ 错误**：Document未正确设置到Viewer
```csharp
var viewer = new FlowDocumentScrollViewer();
// ❌ 未设置Document属性
```

**✅ 正确**：显式设置Document
```csharp
var viewer = new FlowDocumentScrollViewer
{
    Document = document,  // ✅ 设置FlowDocument
    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
};
```

### 8.7 打印数据不完整

**❌ 错误**：MapToPrintDtoAsync未获取完整数据
```csharp
private async Task<PrescriptionPrintDto> MapToPrintDtoAsync(PrescriptionDto prescription)
{
    var printDto = new PrescriptionPrintDto
    {
        PatientName = "患者姓名",  // ❌ 硬编码，未从服务获取
        DoctorName = "医生姓名"
    };
    return printDto;
}
```

**✅ 正确**：注入依赖服务获取完整数据
```csharp
private readonly IPatientService _patientService;
private readonly IUserService _userService;

private async Task<PrescriptionPrintDto> MapToPrintDtoAsync(PrescriptionDto prescription)
{
    // ✅ 从PatientService获取患者信息
    var patient = await _patientService.GetByIdAsync(prescription.PatientId);

    // ✅ 从UserService获取医生信息
    var doctor = await _userService.GetByIdAsync(prescription.DoctorId);

    var printDto = new PrescriptionPrintDto
    {
        PatientName = patient?.Name ?? "未知",
        DoctorName = doctor?.Name ?? "未知",
        // ...其他字段
    };
    return printDto;
}
```

### 8.8 批量打印内存泄漏

**❌ 错误**：批量打印未释放FlowDocument
```csharp
public async Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions)
{
    foreach (var prescription in prescriptions)
    {
        var document = BuildFlowDocument(...);
        // ❌ document未释放，内存累积
        await PrintAsync(document);
    }
}
```

**✅ 正确**：每次打印后允许GC回收
```csharp
public async Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions)
{
    foreach (var prescription in prescriptions)
    {
        var document = BuildFlowDocument(...);
        await PrintAsync(document);

        document = null;  // ✅ 释放引用
        GC.Collect();     // ✅ 强制回收（批量打印场景）
    }
}
```

---

## 9. 检查清单

### 9.1 打印服务实现检查

- [ ] IPrescriptionPrintService接口完整实现
- [ ] MapToPrintDtoAsync获取完整数据（患者、医生、诊断）
- [ ] BuildFlowDocument使用Builder模式
- [ ] PrintDialog正确显示和处理用户取消
- [ ] 异常处理和日志记录完整
- [ ] 打印机管理功能实现（获取列表、设置默认）

### 9.2 FlowDocumentBuilder检查

- [ ] A4纸张尺寸正确设置（210mm x 297mm）
- [ ] 字体支持中文（SimSun或Microsoft YaHei）
- [ ] 所有Builder方法返回this（链式调用）
- [ ] 表格边框正确设置（BorderBrush + BorderThickness）
- [ ] 分隔线格式统一
- [ ] Build()方法返回最终FlowDocument

### 9.3 ViewModel集成检查

- [ ] 打印命令使用DelegateCommand
- [ ] CanExecute验证有处方数据
- [ ] IsBusy状态正确管理
- [ ] 异常捕获和用户友好提示
- [ ] 日志记录打印操作
- [ ] RaiseCanExecuteChanged在数据变化时调用

### 9.4 XAML绑定检查

- [ ] 打印按钮Command绑定正确
- [ ] ToolTip提示友好
- [ ] 图标资源路径正确
- [ ] 按钮启用状态绑定到CanExecute

### 9.5 测试检查

- [ ] 单个处方打印测试
- [ ] 打印预览测试
- [ ] 批量打印测试（5个以上）
- [ ] XPS导出测试
- [ ] 打印机不存在场景测试
- [ ] 用户取消打印测试
- [ ] 大数据量处方打印（50味药材）

---

## 10. 最佳实践

### 10.1 打印性能优化

1. **使用单例模式注册打印服务**：
   ```csharp
   containerRegistry.RegisterSingleton<IPrescriptionPrintService, PrescriptionPrintService>();
   ```

2. **批量打印使用GC回收**：
   ```csharp
   foreach (var prescription in prescriptions)
   {
       // 打印...
       GC.Collect();  // 每10个处方回收一次
   }
   ```

3. **FlowDocument复用Builder实例**：
   ```csharp
   // ❌ 每次创建新Builder
   for (int i = 0; i < 100; i++)
   {
       var builder = new PrescriptionFlowDocumentBuilder(printDto);
   }

   // ✅ 复用Builder逻辑（静态方法）
   public static class FlowDocumentFactory
   {
       public static FlowDocument Create(PrescriptionPrintDto printDto)
       {
           var builder = new PrescriptionFlowDocumentBuilder(printDto);
           return builder.AddHeader()...Build();
       }
   }
   ```

### 10.2 用户体验优化

1. **打印前显示确认对话框**：
   ```csharp
   private async void ExecutePrint()
   {
       var result = MessageBox.Show(
           $"确认打印处方（{CurrentPrescription.Id}）？",
           "确认",
           MessageBoxButton.YesNo,
           MessageBoxImage.Question);

       if (result == MessageBoxResult.Yes)
       {
           await _printService.PrintPrescriptionAsync(CurrentPrescription);
       }
   }
   ```

2. **打印完成后提示**：
   ```csharp
   var success = await _printService.PrintPrescriptionAsync(prescription);
   if (success)
   {
       ShowSuccessMessage("打印成功！");
   }
   ```

3. **批量打印进度提示**：
   ```csharp
   IsBusy = true;
   BusyMessage = $"正在打印 {currentIndex}/{totalCount}...";
   ```

### 10.3 错误处理策略

1. **打印机不存在降级处理**：
   ```csharp
   try
   {
       printDialog.PrintQueue = FindPrintQueue(_defaultPrinterName);
   }
   catch
   {
       // 回退到系统默认打印机
       _logger.LogWarning("默认打印机不可用，使用系统默认");
   }
   ```

2. **XPS导出异常回滚**：
   ```csharp
   try
   {
       await ExportToXpsAsync(prescription, filePath);
   }
   catch
   {
       if (File.Exists(filePath))
       {
           File.Delete(filePath);  // 删除损坏文件
       }
       throw;
   }
   ```

---

## 11. 参考资料

### 11.1 项目文档

- **打印服务设计**：`docs/architecture/client/print-design.md`（待创建）
- **FlowDocument模板**：`docs/templates/prescription-template.xaml`（待创建）
- **打印测试用例**：`tests/UnitTests/Client/Desktop/Modules/Prescriptions/PrintServiceTests.cs`

### 11.2 技术文档

- **WPF FlowDocument**: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/flow-document-overview
- **System.Printing**: https://learn.microsoft.com/en-us/dotnet/api/system.printing
- **XPS Document API**: https://learn.microsoft.com/en-us/dotnet/api/system.windows.xps.packaging.xpsdocument
- **Builder Pattern**: https://refactoring.guru/design-patterns/builder

### 11.3 Issue链接

- **Issue #1379**: [PRINT-2] 实现标准处方模板
- **Issue #1380**: [PRINT-3] 实现处方打印服务
- **Issue #1551**: 处方编号显示优化

---

## 附录：完整代码示例

### A. PrescriptionPrintService完整代码

> **位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`

（完整代码略，参考4.1-4.5节）

### B. PrescriptionFlowDocumentBuilder完整代码

> **位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionFlowDocumentBuilder.cs`

（完整代码略，参考5.2-5.8节）

### C. PrescriptionViewModel集成示例

> **位置**: `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionViewModel.cs`

（完整代码略，参考6.1节）

---

**最后更新**: 2025-10-30
**维护负责**: Client端开发组
**版本**: v1.0

---

**本指南涵盖LYBTZYZS项目Client端打印功能的完整实现，从服务接口到FlowDocumentBuilder，包含完整代码示例、最佳实践和8个常见陷阱。遵循本指南可确保打印功能符合WPF标准和项目编码规范。**

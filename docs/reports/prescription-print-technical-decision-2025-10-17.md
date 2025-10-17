# 处方打印技术方案决策报告

**生成时间**: 2025-10-17
**关联Issue**: #1378 (PRINT-1)
**Epic**: #1343 - MVP "能看诊" 功能实现
**决策人**: Claude Code (AI辅助)

---

## 1. 现有代码分析

### 1.1 已定义接口

文件位置：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/IPrescriptionPrintService.cs`

```csharp
public interface IPrescriptionPrintService
{
    // 基础打印功能
    Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription);
    Task PreviewPrescriptionAsync(PrescriptionDto prescription);
    Task<int> BatchPrintAsync(PrescriptionDto[] prescriptions);

    // PDF导出
    Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath);

    // 打印机管理
    string[] GetAvailablePrinters();
    void SetDefaultPrinter(string printerName);
    string? GetDefaultPrinter();
}
```

**配套类型**：
- `PrintOptions`：打印选项（打印机名称、份数、双面打印、纸张大小、方向）
- `PaperSize` 枚举：A4, A5, Letter, Legal
- `PrintOrientation` 枚举：Portrait（纵向）, Landscape（横向）

### 1.2 打印日志实体

文件位置：`src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionPrintLog.cs`

已定义 `PrescriptionPrintLog` 实体，记录：
- 处方ID、打印版本号、打印时间
- 打印人ID和姓名
- 打印机名称
- 打印状态（成功/失败）和错误信息

### 1.3 当前状态

- ✅ **接口已定义**：完整的打印服务接口
- ✅ **日志实体已定义**：支持打印历史追踪
- ❌ **实现类缺失**：`PresentationServiceCollectionExtensions.cs` 注释显示"需要具体实现，暂时不注册"
- ❌ **ViewModel集成缺失**：`PrescriptionComposerViewModel` 有 `PrintPreviewCommand` 但未实现

---

## 2. WPF打印技术方案对比

### 方案1：FlowDocument + PrintDialog（✅ 推荐）

**技术栈**：
- `System.Windows.Documents.FlowDocument`
- `System.Windows.Controls.PrintDialog`
- 原生WPF，无需第三方库

**优点**：
- ✅ 原生WPF支持，零依赖
- ✅ 适合结构化文档（处方笺）
- ✅ 支持打印预览（`PrintPreviewDialog`）
- ✅ 自动分页、页眉页脚
- ✅ 可以导出XPS文件（类似PDF）
- ✅ 支持富文本格式（表格、粗体、下划线）

**缺点**：
- ⚠️ 需要构建FlowDocument对象模型
- ⚠️ XPS格式不如PDF通用（但可以转换）

**示例代码**：
```csharp
// 1. 构建FlowDocument
var flowDoc = new FlowDocument();
flowDoc.PageWidth = 210 * 96 / 25.4; // A4宽度（毫米转点）
flowDoc.PageHeight = 297 * 96 / 25.4; // A4高度

// 添加内容
var paragraph = new Paragraph(new Run("处方笺"));
paragraph.FontSize = 18;
paragraph.FontWeight = FontWeights.Bold;
flowDoc.Blocks.Add(paragraph);

// 添加表格
var table = new Table();
// ... 构建药材表格

// 2. 打印
var printDialog = new PrintDialog();
if (printDialog.ShowDialog() == true)
{
    IDocumentPaginatorSource paginator = flowDoc;
    printDialog.PrintDocument(paginator.DocumentPaginator, "处方笺");
}
```

**适用场景**：
- ✅ 标准处方笺打印（MVP首选）
- ✅ 需要打印预览
- ✅ 需要保存XPS文件

---

### 方案2：XpsDocumentWriter 直接打印

**技术栈**：
- `System.Windows.Xps.XpsDocumentWriter`
- `System.Printing.PrintQueue`

**优点**：
- ✅ 性能最优（直接写入XPS打印路径）
- ✅ 无需显示PrintDialog（适合批量打印）
- ✅ 完全控制打印作业

**缺点**：
- ⚠️ 仍需构建FlowDocument或Visual对象
- ⚠️ 代码复杂度高
- ⚠️ 不适合MVP阶段

**适用场景**：
- ✅ 批量打印（未来优化）
- ✅ 后台打印服务

---

### 方案3：第三方PDF库（❌ 不推荐MVP阶段）

**技术栈**：
- iTextSharp / PdfSharp / QuestPDF
- 需要NuGet引入

**优点**：
- ✅ PDF格式通用
- ✅ 精确控制布局

**缺点**：
- ❌ 引入第三方依赖（违反MVP原则）
- ❌ 学习成本高
- ❌ 许可证问题（iTextSharp需商业授权）

**Constitution合规性检查**：
- ⚠️ 引入新库需评估：Constitution禁止过度依赖，MVP阶段应避免

---

### 方案4：Visual对象直接打印

**技术栈**：
- `System.Windows.Media.DrawingVisual`
- 手动绘制内容

**优点**：
- ✅ 最大灵活性

**缺点**：
- ❌ 代码量巨大
- ❌ 维护成本高
- ❌ 不适合MVP

---

## 3. 最终决策（✅ 方案1：FlowDocument + PrintDialog）

### 3.1 技术选型

**核心方案**：
- **打印技术**：`FlowDocument` + `System.Windows.Controls.PrintDialog`
- **预览方式**：`PrintPreviewDialog` 或自定义预览窗口
- **文件导出**：XPS格式（通过XpsDocumentWriter）
- **PDF支持**：后续版本再考虑（MVP阶段XPS足够）

**实现路径**：
```
PrescriptionDto → FlowDocument构建器 → PrintDialog.PrintDocument() → 打印机
                                    ↓
                              XpsDocumentWriter → .xps文件
```

---

### 3.2 标准中医处方模板规范

#### 模板布局（A4纵向，210mm × 297mm）

```
┌────────────────────────────────────────────┐
│           [诊所抬头]                        │
│    中医门诊处方笺                           │
│                                            │
├────────────────────────────────────────────┤
│ 患者信息：                                  │
│   姓名：___  性别：_  年龄：__  日期：____  │
│                                            │
├────────────────────────────────────────────┤
│ 四诊信息：                                  │
│   望：______                                │
│   闻：______                                │
│   问：______                                │
│   切：______                                │
│   中医诊断：_______                         │
│   治疗原则：_______                         │
│                                            │
├────────────────────────────────────────────┤
│ 处方内容：                                  │
│ ┌──┬──────┬─────┬────┐                  │
│ │序│药材名│剂量  │单位 │                  │
│ ├──┼──────┼─────┼────┤                  │
│ │1 │当归  │10g   │克   │                  │
│ │2 │白芍  │15g   │克   │                  │
│ │...                                      │
│ └──┴──────┴─────┴────┘                  │
│                                            │
│ 剂数：__ 剂                                 │
│ 用法：水煎服，日一剂，分早晚服               │
│                                            │
├────────────────────────────────────────────┤
│ 费用信息：                                  │
│   单剂价格：¥___                            │
│   总价：¥___                                │
│                                            │
├────────────────────────────────────────────┤
│ 医生签名：______        日期：____          │
│                                            │
│ [防伪标识/二维码]                           │
└────────────────────────────────────────────┘
```

#### 必选字段
1. **诊所抬头**：名称、地址、电话（可配置）
2. **患者信息**：姓名、性别、年龄、就诊日期
3. **四诊信息**：望闻问切、中医诊断、治疗原则
4. **处方内容**：药材表格（序号、药材名、剂量、单位）
5. **用药说明**：剂数、用法
6. **费用信息**：单剂价格、总价
7. **医生签名**：医生姓名、开方日期

#### 可选字段
- 主诉
- 现病史
- 处方编号（CF-YYYYMMDD-XXXX）
- 防伪二维码
- 医嘱/备注

---

### 3.3 FlowDocument构建器设计

**设计模式**：Builder模式

```csharp
public class PrescriptionFlowDocumentBuilder
{
    private readonly PrescriptionDto _prescription;
    private FlowDocument _document;

    public PrescriptionFlowDocumentBuilder(PrescriptionDto prescription)
    {
        _prescription = prescription;
        _document = new FlowDocument
        {
            PageWidth = 210 * 96 / 25.4, // A4宽度
            PageHeight = 297 * 96 / 25.4, // A4高度
            PagePadding = new Thickness(40), // 边距
            FontFamily = new FontFamily("SimSun"), // 宋体
            FontSize = 12
        };
    }

    public PrescriptionFlowDocumentBuilder AddHeader() { /* 添加诊所抬头 */ }
    public PrescriptionFlowDocumentBuilder AddPatientInfo() { /* 患者信息 */ }
    public PrescriptionFlowDocumentBuilder AddFourDiagnostics() { /* 四诊信息 */ }
    public PrescriptionFlowDocumentBuilder AddPrescriptionTable() { /* 处方表格 */ }
    public PrescriptionFlowDocumentBuilder AddUsageInstructions() { /* 用法说明 */ }
    public PrescriptionFlowDocumentBuilder AddPriceInfo() { /* 费用信息 */ }
    public PrescriptionFlowDocumentBuilder AddSignature() { /* 医生签名 */ }

    public FlowDocument Build() => _document;
}
```

**使用示例**：
```csharp
var builder = new PrescriptionFlowDocumentBuilder(prescription);
var flowDoc = builder
    .AddHeader()
    .AddPatientInfo()
    .AddFourDiagnostics()
    .AddPrescriptionTable()
    .AddUsageInstructions()
    .AddPriceInfo()
    .AddSignature()
    .Build();
```

---

### 3.4 打印服务实现类设计

**文件位置**：`src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/Services/PrescriptionPrintService.cs`

```csharp
public class PrescriptionPrintService : IPrescriptionPrintService
{
    private readonly ILogger<PrescriptionPrintService> _logger;
    private string? _defaultPrinter;

    public async Task<bool> PrintPrescriptionAsync(PrescriptionDto prescription)
    {
        try
        {
            // 1. 构建FlowDocument
            var builder = new PrescriptionFlowDocumentBuilder(prescription);
            var flowDoc = builder.Build();

            // 2. 显示PrintDialog
            var printDialog = new PrintDialog();
            if (!string.IsNullOrEmpty(_defaultPrinter))
            {
                // 设置默认打印机
                var queue = LocalPrintServer.GetDefaultPrintQueue();
                printDialog.PrintQueue = queue;
            }

            if (printDialog.ShowDialog() == true)
            {
                // 3. 打印
                IDocumentPaginatorSource paginator = flowDoc;
                printDialog.PrintDocument(paginator.DocumentPaginator, $"处方-{prescription.Id}");

                // 4. 记录日志（调用Server API）
                await LogPrintAsync(prescription.Id, printDialog.PrintQueue.Name, true, null);

                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打印处方失败");
            await LogPrintAsync(prescription.Id, null, false, ex.Message);
            return false;
        }
    }

    public async Task PreviewPrescriptionAsync(PrescriptionDto prescription)
    {
        // 构建FlowDocument并显示预览窗口
        var builder = new PrescriptionFlowDocumentBuilder(prescription);
        var flowDoc = builder.Build();

        var previewWindow = new PrintPreviewWindow(flowDoc);
        previewWindow.ShowDialog();
    }

    public async Task<bool> ExportToPdfAsync(PrescriptionDto prescription, string filePath)
    {
        // MVP阶段：导出为XPS（可以用Windows自带的XPS Viewer查看）
        // 后续版本：考虑XPS → PDF转换
        var builder = new PrescriptionFlowDocumentBuilder(prescription);
        var flowDoc = builder.Build();

        using var xpsDoc = new XpsDocument(filePath, FileAccess.Write);
        var writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
        writer.Write(flowDoc.DocumentPaginator);

        return true;
    }

    // ... 其他方法实现
}
```

---

## 4. 实施计划（PRINT-2 至 PRINT-5）

### PRINT-2: 实现PrescriptionFlowDocumentBuilder
- **工作量**：3小时
- **核心**：FlowDocument构建逻辑
- **验收**：生成符合模板规范的FlowDocument

### PRINT-3: 实现PrescriptionPrintService
- **工作量**：2小时
- **核心**：打印、预览、导出XPS
- **验收**：IPrescriptionPrintService接口完整实现

### PRINT-4: 集成到PrescriptionComposerViewModel
- **工作量**：1小时
- **核心**：连接PrintPreviewCommand和打印服务
- **验收**：点击打印预览按钮可以打开打印对话框

### PRINT-5: 测试和优化
- **工作量**：2小时
- **核心**：实际打印测试、布局调整
- **验收**：打印输出符合标准处方格式

---

## 5. Constitution合规性检查

✅ **符合MVP原则**：
- 使用原生WPF，无需第三方库
- 够用即好，不引入PDF（后续版本可扩展）

✅ **符合技术黑名单**：
- 未使用禁止技术（Redis、CQRS、Docker等）

✅ **符合架构标准**：
- 遵循Client端MVVM模式
- 服务层清晰分离（Builder + Service）

---

## 6. 风险与限制

### 已知限制
1. **XPS格式**：Windows自带查看器，但不如PDF通用
   - **缓解**：后续版本可添加XPS→PDF转换（使用GhostScript或第三方库）

2. **FlowDocument复杂度**：表格布局需要精细调整
   - **缓解**：使用Builder模式简化构建，提供模板配置

3. **打印机兼容性**：不同打印机可能有差异
   - **缓解**：使用标准A4纸张，避免特殊打印功能

### 后续优化方向
1. 支持PDF导出（引入轻量级库如PdfSharp）
2. 支持自定义模板（可配置诊所抬头、布局）
3. 批量打印优化（使用XpsDocumentWriter）
4. 打印历史查看和重打印

---

## 7. 参考资料

### Microsoft官方文档
- [How to open a common dialog box](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/how-to-open-common-system-dialog-box#print-dialog-box)
- [Printing documents overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/documents/printing-overview)
- [System.Windows.Controls.PrintDialog](https://learn.microsoft.com/en-us/dotnet/api/system.windows.controls.printdialog)
- [FlowDocument Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.documents.flowdocument)

### 项目文档
- [MVP需求确认总报告](./mvp-requirements-confirmation-2025-10-16.md)
- [处方录入详细设计](./prescription-entry-requirements-2025-10-16.md)

---

## 8. 决策总结

| 维度 | 决策 |
|------|------|
| **打印技术** | FlowDocument + PrintDialog |
| **文件格式** | XPS（MVP阶段），PDF（后续版本） |
| **模板标准** | A4纵向，包含7个必选区域 |
| **实现模式** | Builder模式构建FlowDocument |
| **依赖引入** | 0个（原生WPF） |
| **总工作量** | 8小时（PRINT-2至PRINT-5） |

---

**文档版本**: v1.0
**最后更新**: 2025-10-17
**下一步**: 实施PRINT-2（创建PrescriptionFlowDocumentBuilder）

🤖 Generated with [Claude Code](https://claude.com/claude-code)

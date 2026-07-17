# LYBT.Desktop.Printing

> 处方打印服务模块：提供打印、预览、PDF导出、批量打印功能，支持A5/A4纸张及多页续方。

## 项目定位

独立 Prism 模块，封装处方笺的物理输出能力（打印机、PDF、XPS）。业务模块通过泛型接口 `IPrintService<PrescriptionPrintModel>` 调用，不直接依赖 WPF 打印 API 或 QuestPDF。默认纸张 A5（中医处方标准），A4 用于药材较多的详细处方。

## 目录结构

```
LYBT.Desktop.Printing/
├── Interfaces/
│   └── IPrintService.cs          # 泛型打印接口 + PrintOptions + 枚举
├── Models/
│   └── PrescriptionPrintModel.cs # 处方打印数据模型（诊所/患者/诊断/药材/费用/签名）
├── Services/
│   ├── PrescriptionPrintService.cs  # WPF FixedDocument + XPS 打印实现
│   └── PrescriptionPdfExporter.cs   # QuestPDF PDF 导出器
├── Templates/
│   ├── PrescriptionPrintTemplate.xaml(.cs)        # A5 首页模板
│   ├── PrescriptionPrintA4Template.xaml(.cs)      # A4 首页模板
│   ├── PrescriptionContinuationTemplate.xaml(.cs)  # A5 续页模板
│   └── PrescriptionContinuationA4Template.xaml(.cs)# A4 续页模板
└── PrintingModule.cs             # Prism IModule 入口
```

## 核心组件

| 类 | 设计依据 |
|---|---|
| **IPrintService\<TModel\>** — 泛型打印接口 | 类型安全的打印/预览/导出/批量打印/打印机管理，约束 `where TModel : class` |

| 方法 | 说明 |
|------|------|
| `PrintAsync(TModel, PrintOptions?)` | 打印文档，支持对话框/直打模式 |
| `PreviewAsync(TModel, PrintOptions?)` | 弹出预览窗口 |
| `ExportAsync(TModel, string, ExportFormat)` | 导出为 XPS 或 PDF |
| `BatchPrintAsync(TModel[], PrintOptions?)` | 批量打印，返回成功数 |
| `GetAvailablePrinters()` | 返回系统打印机列表 |
| `SetDefaultPrinter(string)` | 设置默认打印机 |
| `GetDefaultPrinter()` | 获取当前默认打印机 |

| 类 | 设计依据 |
|---|---|
| **PrintOptions** — 打印选项 | 打印机名称/份数/纸张/方向/双面/是否显示对话框 |

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `PrinterName` | `null`（系统默认） | 目标打印机 |
| `Copies` | `1` | 打印份数 |
| `PaperSize` | `A5` | 纸张大小（A4/A5/Letter/Legal） |
| `Orientation` | `Portrait` | 打印方向 |
| `DuplexPrinting` | `false` | 双面打印 |
| `ShowDialog` | `true` | 是否显示打印对话框 |

| 类 | 设计依据 |
|---|---|
| **PrescriptionPrintModel** — 处方打印数据模型 | 包含诊所信息、患者信息、四诊诊断、处方药材、费用、签名等完整处方数据 |

| 分组 | 字段 |
|------|------|
| 诊所 | `ClinicName`, `ClinicAddress`, `ClinicPhone`, `Department` |
| 患者 | `PatientName`, `Gender`, `Age`, `ConsultationDate`, `OutpatientNumber`, `PatientPhone`, `PatientAddress` |
| 诊断 | `TcmDiagnosis`, `Symptoms`, `PresentIllness`, `InspectionDiagnosis`, `AuscultationDiagnosis`, `TongueDiagnosis`, `PulseDiagnosis` |
| 处方 | `Items`(药材列表), `DosageCount`, `Usage`, `Advice`, `FormulaSource` |
| 费用 | `ConsultationFee`, `MedicineFee`, `TreatmentFee`, `SingleDosePrice`, `Discount`, `TotalPrice` |
| 签名 | `DoctorName`, `PrescriptionDate`, `Reviewer`, `Dispenser`, `PrescriptionNumber` |
| 其他 | `IsDraft`（草稿水印标记） |

| 类 | 设计依据 |
|---|---|
| **PrescriptionPrintService** — 打印服务实现 | FixedDocument + XPS 技术栈，支持多页自动分页（A5=12味/A4=20味首页阈值） |

| 常量 | 值 | 说明 |
|------|------|------|
| `A5FirstPageHerbLimit` | 12 | A5 首页最多 12 味药材 |
| `A4FirstPageHerbLimit` | 20 | A4 首页最多 20 味药材 |
| `ContinuationPageHerbLimit` | 20 | 续页最多 20 味药材 |

| 类 | 设计依据 |
|---|---|
| **PrescriptionPdfExporter** — PDF 导出器 | QuestPDF 静态类，A5 页面，Microsoft YaHei 字体，草稿水印旋转 -35° |

| 类 | 设计依据 |
|---|---|
| **PrescriptionPrintTemplate** | A5 首页 XAML 模板（559×794px，96DPI） |
| **PrescriptionPrintA4Template** | A4 首页 XAML 模板（794×1123px，96DPI） |
| **PrescriptionContinuationTemplate** | A5 续页模板，`SetAsLastPage()` 显示签名/费用区 |
| **PrescriptionContinuationA4Template** | A4 续页模板，同上 |
| **PrintingModule** | Prism `IModule` 入口，注册 `IPrintService<PrescriptionPrintModel>` 为 Singleton |

## 依赖关系

```
LYBT.Desktop.Printing
├── LYBT.Desktop.Infrastructure  (WPF 基础设施)
├── LYBT.Shared.Models           (DecocteMethod 枚举)
├── QuestPDF                     (PDF 生成，Community 许可)
├── SixLabors.Fonts              (显式引用，覆盖传递依赖)
├── SixLabors.ImageSharp         (显式引用，覆盖传递依赖)
└── Prism.Core / Prism.DryIoc    (模块注册)
```

## 设计决策

1. **泛型接口 `IPrintService<TModel>`** — 当前仅 `PrescriptionPrintModel` 实现，但接口设计支持未来扩展（如发票、报告等打印类型），新增类型只需实现接口并在 `PrintingModule` 注册。
2. **双渲染引擎** — WPF FixedDocument + XPS 用于打印预览和直打（利用系统打印驱动），QuestPDF 用于 PDF 导出（跨平台兼容性更好）。
3. **自动多页分页** — 超过首页药材阈值时自动创建续页，续页使用独立模板（头部更简洁），最后一页通过 `SetAsLastPage()` 显示签名和费用区。
4. **CloneModelWithItems** — 多页模式下克隆模型替换药材列表，避免修改原始数据。
5. **草稿水印** — `IsDraft=true` 时 PDF 导出显示旋转 -35° 的半透明红色"草稿"水印。
6. **药名截断** — `PrescriptionItemPrintModel.HerbName` 限制 10 字符，避免溢出打印区域（CODE-37）。

## 已知陷阱

- **空处方防御** — `PrintAsync`/`PreviewAsync` 对空药材列表抛出 `InvalidOperationException`，调用方（`PrescriptionPrintHandler`）应在 UI 层先检查。
- **QuestPDF License** — 每次 `Export()` 调用设置 `License = LicenseType.Community`，商业部署需更换。
- **SixLabors 显式引用** — 为覆盖 QuestPDF 传递依赖的安全漏洞，不可移除。
- **A5 尺寸硬编码** — `A5PageSize = new(559, 794)` 基于 96DPI 计算，如 DPI 不同需调整。
- **无多 PDF 合并** — PDF 导出仅支持单处方，批量场景需外部拼接。

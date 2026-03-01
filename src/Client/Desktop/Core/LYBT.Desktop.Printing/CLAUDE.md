# LYBT.Desktop.Printing 代码知识

打印服务模块 - 基于 WPF FixedDocument 实现处方打印、预览、导出，支持 A4/A5 纸张和自动分页。

## 代码文件结构

```
LYBT.Desktop.Printing/
├── Interfaces/
│   └── IPrintService.cs                          # 泛型打印服务接口 + 选项/枚举定义
├── Models/
│   ├── PrescriptionPrintModel.cs                 # 处方打印数据模型
│   └── PrintLogEntry.cs                          # 打印日志条目
├── Services/
│   └── PrescriptionPrintService.cs               # 处方打印服务实现
├── Templates/
│   ├── PrescriptionPrintTemplate.xaml(.cs)        # A5 首页模板
│   ├── PrescriptionPrintA4Template.xaml(.cs)      # A4 首页模板
│   ├── PrescriptionContinuationTemplate.xaml(.cs) # A5 续页模板
│   └── PrescriptionContinuationA4Template.xaml(.cs) # A4 续页模板
└── PrintingModule.cs                             # Prism 模块注册
```

### Interfaces/IPrintService.cs
**IPrintService\<TModel\>** (interface, generic) | 泛型打印服务接口

| 方法 | 说明 |
|------|------|
| PrintAsync(TModel, PrintOptions?) | 打印文档 |
| PreviewAsync(TModel, PrintOptions?) | 预览文档 |
| ExportAsync(TModel, string, ExportFormat) | 导出文档 (XPS/PDF) |
| BatchPrintAsync(TModel[], PrintOptions?) | 批量打印，返回成功数量 |
| GetAvailablePrinters() | 获取可用打印机列表 |
| SetDefaultPrinter(string) | 设置默认打印机 |
| GetDefaultPrinter() | 获取当前默认打印机 |

**PrintOptions** (class) | 打印选项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| PrinterName | string? | null | 打印机名称 (空=系统默认) |
| Copies | int | 1 | 份数 |
| PaperSize | PaperSize | A5 | 纸张大小 |
| Orientation | PrintOrientation | Portrait | 打印方向 |
| DuplexPrinting | bool | false | 是否双面打印 |
| ShowDialog | bool | true | 是否显示打印对话框 |

**PaperSize** (enum) | A4, A5, Letter, Legal
**PrintOrientation** (enum) | Portrait, Landscape
**ExportFormat** (enum) | Xps, Pdf (PDF 暂不支持，预留扩展)

### Models/PrescriptionPrintModel.cs
**PrescriptionPrintModel** (class) | 处方打印数据模型

| 属性分组 | 主要属性 |
|----------|----------|
| 诊所信息 | ClinicName, Department, ClinicAddress, ClinicPhone |
| 患者信息 | PatientName, Gender, Age, ConsultationDate, OutpatientNumber, PatientPhone |
| 诊断信息 | TcmDiagnosis, Symptoms, PresentIllness, TongueDiagnosis, PulseDiagnosis |
| 处方内容 | Items (List\<PrescriptionItemPrintModel\>), DosageCount, Usage, Advice |
| 费用信息 | ConsultationFee, MedicineFee, SingleDosePrice, Discount, TotalPrice |
| 签名区 | DoctorName, PrescriptionDate, Reviewer, Dispenser |

| 计算属性 | 说明 |
|----------|------|
| SymptomsText | 合并 Symptoms + PresentIllness，分号分隔 (T4-S5-07) |

**PrescriptionItemPrintModel** (class) | 处方药材打印数据

| 属性 | 说明 |
|------|------|
| SequenceNumber | 序号 |
| HerbName | 药材名 |
| Dosage | 剂量 |
| Unit | 单位 |
| DecocteMethod | 煎法 (Default 时不显示标注) |
| DisplayText | 格式化文本: "药材名 剂量单位(煎法)" |

### Models/PrintLogEntry.cs
**PrintLogEntry** (class) | 打印日志条目 (T4-S5-01)

| 属性/方法 | 说明 |
|-----------|------|
| IsSuccess | 是否成功 (init) |
| PrinterName | 打印机名称 (init) |
| ErrorMessage | 错误信息 (init) |
| PrintedAt | 打印时间 (init) |
| Succeeded(string?) | 创建成功日志 (static factory) |
| Failed(string, string?) | 创建失败日志 (static factory) |

### Services/PrescriptionPrintService.cs
**PrescriptionPrintService** : IPrintService\<PrescriptionPrintModel\> | 处方打印服务

| 方法 | 说明 |
|------|------|
| PrintAsync(PrescriptionPrintModel, PrintOptions?) | 打印处方，支持对话框/静默模式 |
| PreviewAsync(PrescriptionPrintModel, PrintOptions?) | 预览窗口 (含打印设置面板+纸张切换) |
| ExportAsync(PrescriptionPrintModel, string, ExportFormat) | 导出为 XPS 文件 |
| BatchPrintAsync(PrescriptionPrintModel[], PrintOptions?) | 逐个批量打印 |
| GetAvailablePrinters() | 通过 LocalPrintServer 获取打印机列表 |
| SetDefaultPrinter(string) | 设置默认打印机名称 |
| GetDefaultPrinter() | 获取默认打印机 (优先已设置，否则系统默认) |
| BuildFixedDocument(PrescriptionPrintModel, Size) | 构建 FixedDocument，超过首页限制时自动分页 |
| BuildMultiPageDocument(FixedDocument, PrescriptionPrintModel, Size) | 多页文档构建 (首页完整模板 + 续页模板) |
| CreateFixedPage(PrescriptionPrintModel, Size) | 创建首页 (根据纸张选择 A4/A5 模板) |
| CreateContinuationFixedPage(PrescriptionPrintModel, Size, bool) | 创建续页 (最后一页显示签名/费用) |
| CloneModelWithItems(PrescriptionPrintModel, List) | 克隆模型替换药材列表 (分页用) |

| 事件 | 说明 |
|------|------|
| PrintLogRequested | 打印成功/失败时触发，调用方订阅以记录日志 (T4-S5-01) |

| 常量 | 值 | 说明 |
|------|----|------|
| A5PageSize | 559x794 px | A5 纸张 148x210mm (96 DPI) |
| A4PageSize | 794x1123 px | A4 纸张 210x297mm (96 DPI) |
| A5FirstPageHerbLimit | 12 | A5 首页最多 12 味药材 |
| A4FirstPageHerbLimit | 20 | A4 首页最多 20 味药材 |
| ContinuationPageHerbLimit | 20 | 续页最多 20 味药材 |

### Templates/PrescriptionPrintTemplate.xaml(.cs)
**PrescriptionPrintTemplate** : UserControl | A5 普通处方笺首页模板 (XAML 布局)

### Templates/PrescriptionPrintA4Template.xaml(.cs)
**PrescriptionPrintA4Template** : UserControl | A4 普通处方笺首页模板

### Templates/PrescriptionContinuationTemplate.xaml(.cs)
**PrescriptionContinuationTemplate** : UserControl | A5 续页模板 (T4-S5-09)

| 方法 | 说明 |
|------|------|
| SetAsLastPage() | 显示服法、医嘱、签名、费用区域 (最后一页专用) |

### Templates/PrescriptionContinuationA4Template.xaml(.cs)
**PrescriptionContinuationA4Template** : UserControl | A4 续页模板

| 方法 | 说明 |
|------|------|
| SetAsLastPage() | 显示服法、医嘱、签名、费用区域 (最后一页专用) |

### PrintingModule.cs
**PrintingModule** : IModule (Prism) | 模块注册

| 方法 | 说明 |
|------|------|
| RegisterTypes(IContainerRegistry) | 注册 IPrintService\<PrescriptionPrintModel\> -> PrescriptionPrintService (Singleton) |
| OnInitialized(IContainerProvider) | 空实现 |

## 死代码与废弃标记

- `ExportFormat.Pdf` 枚举值已定义但 PrescriptionPrintService.ExportAsync 中明确标注 "PDF导出暂不支持"，会自动降级为 XPS -- 预留扩展，非死代码
- `PrintOptions.DuplexPrinting` 和 `PrintOptions.Orientation` 属性已定义但 PrescriptionPrintService 中未使用 -- 预留扩展属性
- 所有 4 个模板类仅被 PrescriptionPrintService 内部引用 (无外部直接使用) -- 正常封装设计

## 设计分析

1. **泛型接口设计**: IPrintService\<TModel\> 支持不同打印模型的扩展，当前仅实现 PrescriptionPrintModel，后续可扩展其他打印类型
2. **纸张感知模板选择**: CreateFixedPage 根据 PaperSize 在 A4/A5 模板间切换，IsA4() 方法判断纸张尺寸
3. **自动分页策略**: 药材超过首页限制 (A5=12味, A4=20味) 时自动分页，首页用完整模板，后续页用简化续页模板，最后一页通过 SetAsLastPage() 显示签名/费用区域
4. **PrintLogRequested 事件**: 打印结果通过事件通知调用方 (PrescriptionPrintHandler)，实现打印日志与打印服务的解耦
5. **预览窗口**: PreviewAsync 构建包含打印设置面板 (打印机/份数/纸张) 的 WPF Window，支持实时切换纸张尺寸重新生成文档
6. **Prism 模块化**: PrintingModule 作为独立 Prism 模块注册，通过 DI 容器提供 IPrintService 实例

## 已知陷阱

- ExportAsync 中 PDF 格式会被静默替换为 XPS，不会抛异常，调用方需注意扩展名变更
- BatchPrintAsync 默认 ShowDialog=false，避免每份都弹出对话框
- PrescriptionItemPrintModel.DisplayText 使用反射获取 DecocteMethod 枚举的 Description 特性，性能敏感场景需注意
- 预览窗口中纸张切换会重新构建 FixedDocument 并替换 DocumentViewer.Document，频繁切换可能有性能开销
- 续页模板默认隐藏签名/费用区域，仅通过 SetAsLastPage() 方法显示，遗漏调用会导致最后一页缺少必要信息

---
最后更新: 2026-03-01

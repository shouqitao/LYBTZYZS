# Printing 模块设计

> 日期: 2026-06-29
> US 数量: 4 (PRD)
> 复杂度: 6/10
> 状态: 草稿

## 模块概述

Printing 管理处方打印，支持双引擎渲染（WPF FixedDocument + QuestPDF）。

**职责边界**:
- 处方打印（WPF FixedDocument 引擎）
- 处方预览（DocumentViewer）
- PDF 导出（QuestPDF 引擎）
- XPS 导出

**依赖关系**:
- 上游: MedicalCase（处方数据）、Patients（患者信息）
- 下游: 无（纯 Desktop 模块）
- 仅 Desktop 端，无 Server 端

**关键 US 清单**:
PRINT-001 ~ PRINT-004（详见 PRD）

## 接口契约

### IPrintService<TModel>

```csharp
public interface IPrintService<TModel> where TModel : class
{
    Task<bool> PrintAsync(TModel model, PrintOptions? options = null);
    Task PreviewAsync(TModel model, PrintOptions? options = null);
    Task<bool> ExportAsync(TModel model, string filePath, ExportFormat format = ExportFormat.Xps);
    Task<int> BatchPrintAsync(TModel[] models, PrintOptions? options = null);
    string[] GetAvailablePrinters();
    void SetDefaultPrinter(string printerName);
    string? GetDefaultPrinter();
}
```

### PrintOptions

```
PrinterName: string?
Copies: int（默认 1）
PaperSize: PaperSize（A4/A5/Letter/Legal，默认 A5）
Orientation: PrintOrientation（Portrait/Landscape）
DuplexPrinting: bool
ShowDialog: bool（默认 true）
```

## 双引擎架构

```
PrescriptionPrintHandler（编排层）
  → PrescriptionPrintModel（统一数据模型）
    ├─→ PrescriptionPrintService（WPF 引擎）
    │   ├── XAML 模板（A5/A4，首页/续页）
    │   ├── FixedDocument → PrintDialog
    │   └── XPS 导出
    └─→ PrescriptionPdfExporter（QuestPDF 引擎）
        └── PDF 导出（高保真）
```

### WPF 引擎（打印/预览/XPS）

| 纸张 | 首页药材上限 | 续页药材上限 |
|------|------------|------------|
| A5 | 12 | 20 |
| A4 | 20 | 20 |

**模板**:
- `PrescriptionPrintTemplate` — A5 首页
- `PrescriptionPrintA4Template` — A4 首页
- `PrescriptionContinuationTemplate` — A5 续页
- `PrescriptionContinuationA4Template` — A4 续页

**续页逻辑**: 超出首页上限时自动分页，最后一页显示用法/医嘱/签名/费用。

### QuestPDF 引擎（PDF 导出）

- A5 页面布局
- 支持草稿水印（`IsDraft` → 红色旋转"草稿"文字 72pt）
- 字号: 标题 14, 诊所名 12, 药材 10, 值 10, 标签 9

## 数据流

### 打印处方
```
MedicalCaseCommandsViewModel.PrintCommand
  → PrescriptionPrintHandler.PrintPreviewAsync
    → 构建 PrescriptionPrintModel（从 DTO 映射）
      → 读取诊所设置（IClinicSettingsService）
      → 绑定医生姓名（ISessionManager.CurrentUser）
      → 计算年龄、费用
    → IPrintService.PrintAsync
      → PrescriptionPrintService
        → BuildFixedDocument（分页）
        → PrintDialog / XpsDocumentWriter
```

### PDF 导出
```
MedicalCaseCommandsViewModel.ExportPdfCommand
  → PrescriptionPrintHandler.ExportPdfAsync
    → PrescriptionPdfExporter.Export
      → QuestPDF Fluent API 组合页面
      → 写入文件
```

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| 打印机不可用 | GetAvailablePrinters 返回空 | 提示用户安装打印机 |
| 文件写入失败 | ExportAsync 路径无权限 | 返回 false + 错误消息 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 草稿水印 | MedicalCase Status≠Completed 时显示"草稿" | PRINT-001 |
| 分页规则 | A5 首页 12 味，续页 20 味 | PRINT-002 |
| 药材名截断 | 超过 10 字截断 | CODE-37 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| MedicalCase | 获取处方数据 | Printing ← MedicalCase |
| Patients | 获取患者信息 | Printing ← Patients |

**已知问题**:
- 打印回写（PrintLog）缺失 — 打印操作不可追溯
- IsPrinted/PrintCount 等字段在 MedicalCaseModel 中为 0 个

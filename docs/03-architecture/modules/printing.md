# Printing 模块设计
> 版本: v1.0 | 日期: 2026-08-20

> 复杂度: 6/10 | 状态: 草稿

## 概述

Printing 管理处方打印，支持双引擎渲染（WPF FixedDocument + QuestPDF）。仅 Desktop 端，无 Server 端。

**依赖**: 上游 MedicalCase+Patients，下游无。

## 双引擎架构

```
PrescriptionPrintHandler（编排层）
  → PrescriptionPrintModel（统一数据模型）
    ├─→ PrescriptionPrintService（WPF 引擎：打印/预览/XPS）
    └─→ PrescriptionPdfExporter（QuestPDF 引擎：PDF 导出）
```

### WPF 引擎

| 纸张 | 首页药材上限 | 续页药材上限 |
|------|------------|------------|
| A5 | 12 | 20 |
| A4 | 20 | 20 |

模板: A5首页/A4首页/A5续页/A4续页。超出上限自动分页。

### QuestPDF 引擎

A5 页面布局，支持草稿水印（`IsDraft` → 红色旋转"草稿"文字 72pt）。

## 接口

```csharp
public interface IPrintService<TModel> where TModel : class
{
    Task<bool> PrintAsync(TModel model, PrintOptions? options = null);
    Task PreviewAsync(TModel model, PrintOptions? options = null);
    Task<bool> ExportAsync(TModel model, string filePath, ExportFormat format);
}
```

## 业务规则

| 规则 | 描述 |
|------|------|
| 草稿水印 | Status≠Completed 时显示"草稿" |
| 分页 | A5 首页 12 味，续页 20 味 |
| 药材名截断 | 超过 10 字截断 |

## 已知问题

- 打印回写（PrintLog）缺失 — 打印操作不可追溯

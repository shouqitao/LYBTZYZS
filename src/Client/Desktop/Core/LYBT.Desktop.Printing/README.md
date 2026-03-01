# LYBT.Desktop.Printing

> WPF 处方打印服务，支持 A4/A5 双纸张模板与续页

## 项目定位

- **层级**: Desktop Core (基础设施层)
- **职责**: 提供处方打印、预览、导出功能，基于 WPF FixedDocument 实现 A4/A5 双纸张模板，支持多页续页打印
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Printing/
├── PrintingModule.cs      # Prism 模块注册
├── Interfaces/            # 泛型打印服务接口 (IPrintService<T>)
├── Models/                # 打印数据模型与日志
├── Services/              # PrescriptionPrintService 实现
└── Templates/             # XAML 打印模板 (A4/A5 主页+续页)
```

## 核心组件

| 名称 | 说明 |
|------|------|
| IPrintService\<T\> | 泛型打印服务接口，定义 Print/Preview/Export/BatchPrint 操作 |
| PrintOptions | 打印选项，支持纸张大小 (A4/A5)、方向、份数、双面打印 |
| PrescriptionPrintModel | 处方打印数据模型 |
| PrintLogEntry | 打印日志条目，记录打印成功/失败 |
| PrescriptionPrintService | 处方打印服务实现，基于 FixedDocument + PrintDialog |
| PrescriptionPrintTemplate | A5 处方主页模板 (148mm x 210mm) |
| PrescriptionPrintA4Template | A4 处方主页模板 (210mm x 297mm) |
| PrescriptionContinuationTemplate | A5 续页模板 |
| PrescriptionContinuationA4Template | A4 续页模板 |

## 设计依据

处方打印是中医诊所的核心业务需求。采用 WPF 原生 FixedDocument 方案而非第三方报表引擎，原因：
- 处方笺格式固定，无需复杂报表设计器
- WPF FixedDocument 原生支持精确打印排版
- A5 是处方笺行业标准纸张，A4 用于正式病历归档
- 续页模板处理药材数量超出单页的场景

打印日志事件 (PrintLogRequested) 采用观察者模式，由调用方订阅以实现打印记录持久化。

## 依赖关系

### 依赖
- Prism.Core / Prism.DryIoc - 模块化框架
- LYBT.Desktop.Infrastructure - 基础设施支持

### 被依赖
- LYBT.Desktop.MedicalCase - 医案模块调用打印服务
- LYBT.Desktop.Shell - 主程序模块加载

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始 README 创建 |

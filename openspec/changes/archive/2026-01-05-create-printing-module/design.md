# Design: 独立打印模块架构

## Context

当前系统使用WPF FixedDocument实现处方打印，代码位于MedicalCase模块。随着系统功能扩展，需要支持更多打印场景（收费单、患者报告等），现有架构无法满足扩展需求。

### 约束条件
- Windows Only (WPF Desktop)
- .NET 8.0
- Prism 9.0 模块化架构
- 遵循MVP原则，避免过度设计

## Goals / Non-Goals

### Goals
- 实现打印功能与业务模块解耦
- 支持多模板类型扩展
- 保持现有打印功能完整性
- 统一打印/预览/导出接口

### Non-Goals
- 不支持PDF导出（MVP阶段仅XPS）
- 不实现打印队列管理
- 不支持远程打印
- 不实现模板可视化编辑器

## Decisions

### 1. 模块位置: Core层

**决定**: 将Printing模块放置在`src/Client/Desktop/Core/`

**理由**:
- 打印是跨模块通用能力，属于Infrastructure级别
- Core层模块被所有业务模块依赖
- 与现有Infrastructure、Foundation等Core模块对齐

```
src/Client/Desktop/Core/
├── LYBT.Desktop.Contracts/
├── LYBT.Desktop.Foundation/
├── LYBT.Desktop.Infrastructure/
├── LYBT.Desktop.Models/
├── LYBT.Desktop.Presentation/
└── LYBT.Desktop.Printing/          ← 新增
    ├── Interfaces/
    │   ├── IPrintService.cs        ← 泛型打印接口
    │   └── IPrintTemplate.cs       ← 模板接口
    ├── Models/
    │   ├── PrintOptions.cs
    │   ├── PaperSize.cs
    │   └── PrescriptionPrintModel.cs  ← 从MedicalCase迁移
    ├── Services/
    │   └── PrintService.cs         ← 通用实现
    ├── Templates/
    │   └── PrescriptionPrintTemplate.xaml  ← 从MedicalCase迁移
    └── PrintingModule.cs           ← Prism模块
```

### 2. 接口设计: 泛型打印服务

**决定**: 使用泛型接口 `IPrintService<TModel>` 而非特定接口

**理由**:
- 支持多种打印模型（处方、收费单、报告等）
- 统一API设计，降低学习成本
- 类型安全，编译期检查

```csharp
public interface IPrintService<TModel> where TModel : class
{
    Task<bool> PrintAsync(TModel model, PrintOptions? options = null);
    Task PreviewAsync(TModel model, PrintOptions? options = null);
    Task<bool> ExportAsync(TModel model, string filePath, ExportFormat format);
}
```

### 3. 模板机制: 约定优于配置

**决定**: 模板通过命名约定自动关联，无需显式注册

**理由**:
- 简化配置，减少样板代码
- 遵循KISS原则
- 便于新模板添加

**约定**:
- 模型: `{Name}PrintModel.cs`
- 模板: `{Name}PrintTemplate.xaml`
- 示例: `PrescriptionPrintModel` → `PrescriptionPrintTemplate.xaml`

### 4. DI注册: Shell统一注册

**决定**: Printing模块在Shell层注册，业务模块通过接口依赖

**理由**:
- 遵循ADR-002架构标准（Infrastructure由Shell注册）
- 业务模块仅依赖接口，不依赖具体实现
- 便于测试时Mock

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| 迁移过程中断现有功能 | 分阶段迁移，保持向后兼容 |
| 模板命名约定不灵活 | 预留显式注册API作为备选 |
| Core层模块过多 | Printing功能明确，边界清晰 |

## Migration Plan

### Phase 1: 创建模块骨架 (0.5天)
1. 创建Printing模块项目
2. 添加到LYBT.All.sln
3. 定义接口和基础类型

### Phase 2: 迁移代码 (0.5天)
1. 迁移PrescriptionPrintModel
2. 迁移PrescriptionPrintTemplate.xaml
3. 迁移PrescriptionPrintService → PrintService
4. 更新命名空间

### Phase 3: 更新依赖 (0.25天)
1. MedicalCase模块移除打印代码
2. Clinical模块更新DI注入
3. Shell注册Printing模块

### Phase 4: 验证 (0.25天)
1. 全量编译验证
2. 打印功能测试
3. 预览功能测试

### Rollback
- 保留MedicalCase中原代码（注释），验证完成后删除
- Git分支隔离，问题时可快速回滚

## Open Questions

1. ~~是否需要支持PDF导出？~~ → MVP阶段仅XPS，后续可扩展
2. ~~模板是否需要支持热更新？~~ → 不需要，编译期确定

# Design: HerbCardControl UI优化与煎法字段添加

## Context

HerbCardControl是处方编辑中药材输入的核心组件，当前显示：药材名称、剂量、单位、删除按钮。

**现状问题**:
1. 单位字段在UI上显示但用户不可编辑，因其自动从药材库同步
2. 缺少煎法标注功能，而中医处方中常需标注先煎、后下等特殊煎法

**约束**:
- Unit字段需保留在数据模型中，打印时需要显示
- 煎法为可选字段，大部分药材使用默认煎法
- 需兼容现有处方数据（DecocteMethod默认为Default）

## Goals / Non-Goals

**Goals**:
- 简化HerbCardControl UI，移除用户不需要的单位显示
- 添加煎法选择功能，支持7种常见煎法
- 打印时正确显示单位和非默认煎法

**Non-Goals**:
- 不修改药材库管理功能
- 不添加自定义煎法输入（仅预设值）
- 不修改经验方相关功能

## Decisions

### 1. DecocteMethod枚举设计

```csharp
public enum DecocteMethod
{
    [Description("默认")]
    Default = 0,

    [Description("先煎")]
    PreDecoct = 1,

    [Description("后下")]
    PostAdd = 2,

    [Description("烊化")]
    MeltIn = 3,

    [Description("冲服")]
    TakeWithWater = 4,

    [Description("包煎")]
    WrapDecoct = 5,

    [Description("另煎")]
    SeparateDecoct = 6
}
```

**选择理由**: 使用枚举而非字符串，便于类型安全、数据库存储和国际化扩展。

### 2. HerbCardControl布局调整

**之前**:
```
[药材名称输入框] [剂量输入框] [单位文本] [删除按钮]
```

**之后**:
```
[药材名称输入框] [剂量输入框] [煎法下拉] [删除按钮]
```

**设计细节**:
- 移除单位显示（打印时仍显示）
- 煎法下拉宽度固定，默认显示"默认"
- 下拉选项使用Description特性显示中文

### 3. 回车键焦点跳转逻辑

**交互流程**:
```
药材名称 → [Enter] → 剂量 → [Enter] → 下一行药材名称
                              ↑
                        跳过煎法下拉
```

**设计理由**:
- 大部分药材使用默认煎法，无需每次选择
- 需要特殊煎法时，医生手动点击选择
- 保持快速录入的键盘操作流畅性

### 4. 打印格式设计

**药材项显示格式**:
- 默认煎法: `当归10g`
- 非默认煎法: `附子10g(先煎)`

**选择理由**: 仅非默认煎法加括号标注，保持打印整洁。

### 5. 数据库Schema变更

```sql
ALTER TABLE PrescriptionItems
ADD DecocteMethod INT NOT NULL DEFAULT 0;
```

**选择理由**: 使用INT存储枚举值，默认0表示Default煎法，兼容现有数据。

## Risks / Trade-offs

| Risk | Impact | Mitigation |
|------|--------|------------|
| 现有处方数据兼容性 | 低 | 默认值为Default，不影响现有数据 |
| UI布局挤压 | 中 | 煎法下拉使用紧凑设计，宽度控制在60px |
| 打印模板适配 | 低 | 已有单位字段打印逻辑，仅需添加煎法显示 |

## Migration Plan

1. 创建EF Core迁移添加DecocteMethod列
2. 部署数据库变更
3. 部署应用更新
4. 无需数据迁移脚本（默认值自动生效）

## Open Questions

无

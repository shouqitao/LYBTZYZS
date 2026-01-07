# OpenSpec Proposal: standardize-mapperly-configuration

## 背景

项目当前使用 Mapperly 作为对象映射库，但存在 **247 个 RMG012/RMG020 警告**，主要原因：

1. 每个映射方法都需要显式忽略大量审计字段（CreatedBy, UpdatedBy, RowVersion, IsDeleted）
2. Entity→DTO 和 DTO→Entity 的映射方向不同，需要不同的忽略策略
3. 缺乏统一的配置模式，各 Mapper 实现不一致

## 调研结论

### Mapperly 优势（保留理由）

| 优势 | 说明 |
|------|------|
| **编译时生成** | 无运行时反射，性能接近手动映射 |
| **类型安全** | 编译时检查，避免运行时错误 |
| **社区活跃** | 持续维护，ABP框架已迁移至Mapperly |
| **AutoMapper替代** | AutoMapper已商业化，Mapperly是最佳免费替代 |

### 当前问题根因

1. **默认策略过严**：`RequiredMappingStrategy.Both` 要求双向所有成员都映射
2. **审计字段重复**：每个方法都要忽略 CreatedBy/UpdatedBy/RowVersion/IsDeleted
3. **缺乏全局配置**：没有使用 `MapperDefaults` 或类级别配置

## 解决方案

### 推荐方案：分层配置策略

```
┌─────────────────────────────────────────────────────────────┐
│  Assembly Level: MapperDefaults (全局默认)                   │
│  - IgnoreObsoleteMembersStrategy.Both                       │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│  Mapper Class Level: RequiredMappingStrategy (映射器级别)    │
│  - Entity→DTO: RequiredMappingStrategy.Target               │
│  - DTO→Entity: RequiredMappingStrategy.Target               │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│  Method Level: MapperIgnore* (方法级别)                      │
│  - 仅处理特殊业务逻辑字段                                     │
└─────────────────────────────────────────────────────────────┘
```

### 具体实施

#### 1. 程序集级别默认配置

在 `GlobalUsings.cs` 或专用配置文件中添加：

```csharp
// Server端 - 在 LYBT.Module.Core 或各模块入口
[assembly: MapperDefaults(
    IgnoreObsoleteMembersStrategy = IgnoreObsoleteMembersStrategy.Both
)]
```

#### 2. 审计字段基类（可选）

创建抽象基类封装审计字段忽略：

```csharp
// 方案A: 使用 RequiredMappingStrategy.Target（推荐）
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class MedicalCaseMapper { }

// 方案B: 仅忽略 Source 审计字段（当需要所有 Target 都映射时）
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public partial class MedicalCaseMapper { }
```

#### 3. 标准映射模式

**Entity → ListDto/DetailDto（查询）**
```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class PatientMapper
{
    // 忽略 Target 上 Entity 没有的字段
    [MapperIgnoreTarget(nameof(PatientDetailDto.GenderDisplay))]
    public partial PatientDetailDto ToDetailDto(Patient entity);
}
```

**InputDto → Entity（创建/更新）**
```csharp
// 忽略 Entity 上需要手动设置的字段
[MapperIgnoreTarget(nameof(Patient.Id))]
[MapperIgnoreTarget(nameof(Patient.CreatedAt))]
[MapperIgnoreTarget(nameof(Patient.CreatedBy))]
// ... 审计字段统一忽略
public partial Patient ToEntity(PatientInputDto dto);
```

## 方案对比

| 方案 | 修改量 | 可维护性 | 警告清除 | 推荐度 |
|------|--------|----------|----------|--------|
| **A: RequiredMappingStrategy.Target** | 小（每Mapper 1行） | 高 | 大幅减少 | **推荐** |
| B: 逐个添加 MapperIgnore | 大（每方法多行） | 低 | 完全清除 | 不推荐 |
| C: 手动映射替代 | 巨大 | 中 | 完全清除 | 仅特殊场景 |
| D: 抑制警告 | 小 | 最低 | 隐藏但未解决 | **禁止** |

## 实施计划

### Phase 1: 配置标准化（本提案）

1. 为所有 Server 端 Mapper 添加 `RequiredMappingStrategy.Target`
2. 为所有 Client 端 Mapper 统一配置
3. 保留必要的 `[MapperIgnoreTarget]` 用于特殊字段

### Phase 2: 验证与清理

1. 重新编译验证警告数量
2. 处理剩余的特殊警告
3. 更新文档

## 预期效果

- 警告从 247 个降至 **< 20 个**
- 新增 Mapper 无需重复配置审计字段
- 代码更简洁，维护成本降低

## 决策点

请确认：
1. 是否采用 `RequiredMappingStrategy.Target` 作为默认策略？
2. 是否需要创建 Mapper 基类封装通用配置？
3. 剩余特殊警告的处理方式？

---

## 补充修复记录 (2026-01-07)

### 问题发现

配置 `RequiredMappingStrategy.Target` 后，仍存在 **27个 RMG012 警告**。原因：

| 警告类型 | 说明 | 解决方案 |
|----------|------|----------|
| RMG020 | 源成员未映射到目标 | `RequiredMappingStrategy.Target` 已消除 |
| **RMG012** | **目标成员在源中不存在** | **需逐字段添加 `[MapperIgnoreTarget]`** |

### 根因分析

`RequiredMappingStrategy.Target` 仅消除"源成员未映射"警告，但当目标类型存在源类型没有的字段时（如计算属性、Service层填充字段），仍会产生 RMG012 警告。

### 补充修复清单

| 文件 | 警告数 | 需忽略的目标字段 |
|------|--------|------------------|
| FormulaMapper.cs (Server) | 18 | Description, Source, Contraindications, SpecialInstructions, SortOrder, Processing, Price, Preparation, Herb, ValidationStatus, UserId, Indication, FormulaType |
| UserMapper.cs (Server) | 2 | Status |
| UserMapper.cs (Client) | 6 | LastLoginTime, FailedLoginCount, Remark, Id, Password, ConfirmPassword |
| HerbMapper.cs (Server) | 1 | Properties (RMG004 - 列表方法不支持) |

### 字段分类

需要 `[MapperIgnoreTarget]` 的字段通常属于以下类别：

1. **Service层计算字段**: HerbCount, TotalPrice, Description
2. **审计/系统字段**: CreatedAt, UpdatedAt, ValidationStatus, Status
3. **安全敏感字段**: Password, ConfirmPassword
4. **关联实体字段**: Herb (导航属性单独处理)
5. **DTO扩展字段**: 源Entity没有但DTO需要的展示字段

### 最佳实践总结

```csharp
// 标准Mapper配置模式
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class XxxMapper
{
    // Entity → DTO: 忽略DTO中Entity没有的字段
    [MapperIgnoreTarget(nameof(XxxDto.CalculatedField))]
    [MapperIgnoreTarget(nameof(XxxDto.DisplayOnlyField))]
    public partial XxxDto ToDto(Xxx entity);

    // DTO → Entity: 忽略Entity中由Service层管理的字段
    [MapperIgnoreTarget(nameof(Xxx.Id))]           // 创建时自动生成
    [MapperIgnoreTarget(nameof(Xxx.Status))]       // 业务逻辑控制
    [MapperIgnoreTarget(nameof(Xxx.CreatedAt))]    // 审计字段
    [MapperIgnoreTarget(nameof(Xxx.ValidationStatus))] // 验证逻辑控制
    public partial Xxx ToEntity(XxxInputDto dto);
}
```

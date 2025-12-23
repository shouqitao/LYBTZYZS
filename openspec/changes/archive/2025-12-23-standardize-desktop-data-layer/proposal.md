# Proposal: standardize-desktop-data-layer

## Status
- **Created**: 2025-12-23
- **Author**: Claude Code
- **Stage**: Draft

## Why

### 背景

Desktop端业务模块在数据分层方面存在不一致性，影响代码可维护性和团队协作效率：

1. **Repository层不统一**：
   - Patients/Herbs/Formula/MedicalCase有完整的Repository实现
   - Consultation/Prescriptions没有Repository层，完全依赖MedicalCase的DataManager

2. **DTO设计不一致**：
   - 部分模块使用标准RESTful DTO (ListDto/DetailDto/InputDto)
   - 部分模块直接在ViewModel中使用Server DTO
   - 部分模块混用DTO和UI Models

3. **DataManager使用模式不统一**：
   - 单实体模块(Patients)无DataManager，直接使用Repository
   - 聚合模块(MedicalCase/Herbs/Formula)有DataManager
   - 从属模块(Consultation/Prescriptions)通过父聚合的DataManager访问

4. **Models层命名不规范**：
   - 存在DetailModel、Item、Context、ViewState等多种命名
   - 缺乏统一的命名约定和使用场景定义

### 问题影响

- **开发效率低**：新开发人员难以理解模块间差异
- **测试困难**：无Repository的模块难以单元测试
- **维护成本高**：不同模式增加代码审查难度
- **扩展受限**：不统一的设计阻碍未来功能扩展

## What Changes

### 核心改进目标

1. **统一Repository层模式**：定义标准Repository接口规范和实现模式
2. **规范DTO设计**：统一DTO命名和分层规则
3. **标准化DataManager**：明确DataManager的使用场景和职责
4. **统一Models层**：规范UI Models的命名和结构

### 不变的设计决策

- **保持聚合根设计**：Consultation/Prescription继续作为MedicalCase的子实体
- **保持DI模式**：继续使用构造函数注入
- **保持MVVM架构**：View-ViewModel-Repository三层架构不变

## Scope

### 涉及模块

| 模块 | 改动范围 | 优先级 |
|------|---------|--------|
| LYBT.Desktop.Patients | Repository增强 + Models规范化 | P1 |
| LYBT.Desktop.MedicalCase | 保持现状 + 文档补充 | P2 |
| LYBT.Desktop.Consultation | 接口明确化 + 从属模式文档化 | P1 |
| LYBT.Desktop.Prescriptions | 接口明确化 + 从属模式文档化 | P1 |
| LYBT.Desktop.Herbs | Models规范化 | P2 |
| LYBT.Desktop.Formula | Models规范化 | P2 |
| Shared层 | DTO规范和基类 | P0 |

### 排除范围

- Auth模块（已稳定，不做改动）
- Users模块（已稳定，不做改动）
- Shell层服务注册（保持现状）
- WebAPI层（本次不涉及）

## Design Highlights

### 1. Repository层标准

```
独立实体模块:
  IXxxRepository : IRepository<TDetail, TList, TInput>
  XxxRepository : RepositoryBase<...>

聚合根模块:
  IXxxRepository : IRepository<TDetail, TList, TInput>
  XxxRepository : RepositoryBase<...>
  IXxxDataManager (管理聚合)
  XxxDataManager : IXxxDataManager

从属实体模块:
  IXxxCommandHandler (命令处理)
  XxxCommandHandler (通过父聚合DataManager)
```

### 2. DTO命名规范

```
[Entity]ListDto      - 列表查询响应
[Entity]DetailDto    - 详情查询响应
[Entity]InputDto     - 创建/更新请求
[Entity]SummaryDto   - 聚合内嵌入的简化DTO
```

### 3. Models层规范

```
Models/
├── [Entity]DetailModel.cs    - Master-Detail UI模型(可编辑)
├── [Entity]ViewState.cs      - 视图状态管理
└── Items/
    └── [Entity]Item.cs       - 列表项模型(只读)
```

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 改动范围大 | 回归风险 | 分Phase实施，每Phase独立验证 |
| 接口变更 | 编译错误 | 使用接口适配器，渐进迁移 |
| 聚合设计复杂 | 理解困难 | 补充架构文档和示例代码 |

## Success Criteria

1. 所有模块遵循统一的Repository/DataManager模式
2. DTO命名100%符合规范
3. Models层结构统一
4. 架构测试覆盖关键约束
5. 编译通过，现有测试不失败

## References

- `src/Client/Desktop/DESKTOP_ARCHITECTURE_STANDARD.md` - 现有架构标准
- `openspec/specs/client-layer-architecture/spec.md` - 客户端层架构规范
- `openspec/specs/data-layer-conventions/spec.md` - 数据层约定
- `openspec/specs/dto-architecture/spec.md` - DTO架构规范

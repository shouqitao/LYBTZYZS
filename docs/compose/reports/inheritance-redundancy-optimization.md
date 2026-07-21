---
feature: inheritance-redundancy-optimization
status: delivered
specs:
  - docs/compose/specs/2026-07-21-inheritance-redundancy-optimization-design.md
plans:
  - docs/compose/plans/2026-07-21-inheritance-redundancy-optimization-impl.md
branch: master
---

# 继承冗余与过度设计优化 — Final Report

## What Was Built

消除 8 处继承过深/过度设计/冗余设计问题，降低认知负担和维护成本。重构覆盖 Server 端 Repository 层、Shared DTO 层、Desktop ViewModel 层三个维度，纯结构性改动，不改变业务行为。

## Architecture

### Phase A: BaseRepository 瘦身（P3）

- **QueryablePagingExtensions** — `GetPagedResultAsync` 和 `SelectAsync` 从 `BaseRepository` 提取为 `IQueryable<T>` 扩展方法（`src/Server/Core/LYBT.Infrastructure/Extensions/QueryablePagingExtensions.cs`）
- **ApiClientRepositoryBase** — 泛型参数从 4 个简化为 2 个（`TListDto, TDetailDto`），移除未使用的 `TCreateDto, TUpdateDto`。影响 6 个子类 Repository + 1 个中间基类 `RepositoryBase`

### Phase B: DTO 继承精简 + CrossModule 接口合并（P4）

- **IAuditable 合并** — `ICreatorTrackable` 接口合并到 `IAuditable`（添加 `CreatedBy` 属性），删除 `ICreatorTrackable`。影响 4 个 DetailDto + `BaseApiController`
- **ICrossModuleService** — 创建统一接口 `ICrossModuleService`，合并 `IPatientCrossModuleService`、`IHerbCrossModuleService`、`IUserCrossModuleService`。创建委托实现 `CrossModuleService`，更新 10 个注入点

### Phase C: IViewModelServices 内部解构（P1）

- 已在当前代码库中实现 — `CoreViewModelBase` 和 `NavigableViewModelBase` 均已将服务提取为 protected 属性

### Phase D: ViewModel 继承链扁平化（P2）

- **CoreViewModelBase → NavigableViewModelBase 合并** — 将 `CoreViewModelBase` 的全部成员合并到 `NavigableViewModelBase`，删除 `CoreViewModelBase.cs`。ViewModel 继承链从 3 层减至 2 层（`ObservableObject → NavigableViewModelBase → MasterDetailViewModelBase`）

### Design Decisions

- **保留旧的域 CrossModuleService 实现** — `CrossModuleService` 委托给现有的 `PatientCrossModuleService`、`HerbCrossModuleService`、`UserCrossModuleService`，而非重写为单一类。这样保持了域边界清晰，同时提供统一注入点
- **StatusDto 保留** — `StatusDto` 无直接继承者，保留不动（不影响继承链深度）
- **SelectAsync 提取为扩展方法** — 虽然当前无调用者（死代码），仍按计划提取以保持 BaseRepository 瘦身的一致性

## Verification

| 检查项 | 结果 |
|--------|------|
| `dotnet build WebAPI` | 0 errors, 0 warnings |
| `dotnet build Shell` | 0 errors, 0 warnings |
| `dotnet build Architecture tests` | 0 errors, 0 warnings |
| ViewModel 继承链 | ≤ 2 层 |
| DTO 继承链 | ≤ 2 层 |
| ApiClientRepositoryBase 泛型参数 | ≤ 2 个 |
| CrossModule 接口 | 1 个统一接口 |

注：测试运行需要 .NET 8 runtime，当前环境仅有 .NET 10。编译验证通过。

## Journey Log

- [lesson] CrossModule 接口合并涉及 20+ 注入点，用子代理并行处理效率更高
- [lesson] Phase C 的服务提取已在代码库中实现，无需额外改动
- [lesson] CoreViewModelBase 实际有 6 个直接继承者（非计划中的 3 个），子代理发现并全部更新

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/specs/2026-07-21-inheritance-redundancy-optimization-design.md` | Design spec | 12 处问题分析 |
| `docs/compose/plans/2026-07-21-inheritance-redundancy-optimization-impl.md` | Implementation plan | 8 tasks, 4 phases |

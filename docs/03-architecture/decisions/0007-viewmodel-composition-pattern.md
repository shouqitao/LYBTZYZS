# ADR-0007: ViewModel 组合模式

## 状态

已接受 (2026-02-21)

## 背景

系统有两棵独立的 ViewModel 继承树:
1. **CoreViewModelBase** (Desktop.Models): IsBusy, ErrorMessage, ExecuteWithErrorHandlingAsync
2. **MasterDetailViewModelBase** (Desktop.Infrastructure): CRUD 主从模式, LoadListAsync, SaveDetailAsync

两者都继承 CommunityToolkit.Mvvm 的 `ObservableObject`，但无法合并为单一继承链。

## 技术限制

- `CoreViewModelBase` 在 Desktop.Models (底层项目)
- `MasterDetailViewModelBase` 在 Desktop.Infrastructure (依赖 Prism `IRegionManager`)
- 让 MasterDetailViewModelBase 直接继承 CoreViewModelBase 在依赖方向上可行
- 但 MasterDetailViewModelBase 通过 `IMasterDetailServices` 组合接口已实现等价能力

## 决策

保持当前**组合模式** (`IMasterDetailServices`)，通过接口组合而非继承共享能力。

## 原因

1. **SOLID I 原则**: 接口隔离优于继承 -- CRUD ViewModel 和导航 ViewModel 关注点不同
2. **测试友好**: 组合模式更容易 mock 依赖
3. **避免 "God Base Class"**: 合并后基类职责过重
4. **已验证**: 当前模式在 5 个 MasterDetail ViewModel 中运行良好

## 未来可选方案 (当前不执行)

| 方案 | 优点 | 缺点 |
|------|------|------|
| A: CoreVM 下沉到 Shared | 完全解耦 | 过度设计, Shared 不应有 MVVM 依赖 |
| B: MasterDetail 继承 CoreVM | 简化继承树 | Desktop.Infrastructure 需重构 |
| C: 保持组合 (当前) | 已验证, 无变更风险 | 两棵继承树的存在需要文档说明 |

## 标记

`OpenSpec: refactor-viewmodel-composition` -- 代码中现有标记保持，作为未来重构入口。

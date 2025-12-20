# OpenSpec Proposal: optimize-desktop-core

## Metadata
- **Change ID**: optimize-desktop-core
- **Created**: 2025-12-20
- **Status**: proposed
- **Author**: Claude Code
- **Priority**: medium

## Summary

优化Desktop Core层项目结构，将5个项目整合为4个，通过合并Presentation到Infrastructure、统一接口分布，实现功能集中管理，减少项目间依赖复杂度。

## Problem Statement

### 当前5个项目结构

| 项目 | 文件数 | 被引用次数 | 主要职责 |
|------|--------|-----------|----------|
| Contracts | ~10 | 18 | API接口定义(Refit)、服务接口 |
| Foundation | ~22 | 26 | 技术基础设施(Security/HTTP/Cache) |
| Infrastructure | ~106 | 286 | WPF基础设施(Controls/Converters/Services) |
| Models | ~15 | 40 | ViewModel基类、业务Item模型 |
| Presentation | ~7 | 17 | UI组件(HerbCard/HerbList)、通知服务 |

### 识别的问题

1. **Presentation项目过于单薄**: 清理后仅剩7个文件，维护独立项目成本过高
2. **功能边界模糊**: 
   - Presentation.Components (HerbCard等) 与 Infrastructure.Controls (DataGrid等) 都是UI控件
   - Presentation.Notifications 与 Infrastructure.Services 功能性质相同
3. **依赖方向异常**: Models 依赖 Infrastructure (应该是Infrastructure依赖Models)
4. **接口分散**: 
   - Contracts定义API接口和部分服务接口
   - Infrastructure.Interfaces定义大量服务接口
   - 两处接口混用，界限不清

### 依赖引用统计

```
Contracts    →  18次引用 (API调用)
Foundation   →  26次引用 (Security/HTTP)
Infrastructure → 286次引用 (核心使用)
Models       →  40次引用 (ViewModel基类)
Presentation →  17次引用 (UI组件)
```

## Proposed Solution

### 目标架构: 4个项目

```
┌─────────────────────────────────────────────────────────┐
│                    LYBT.Desktop.Contracts               │
│  (纯接口层 - API + 所有可被Module引用的服务接口)          │
│  - Api/*.cs (Refit接口)                                 │
│  - Services/*.cs (INotificationService等从Presentation迁入) │
│  - Components/*.cs (IComponentValidator等从Infrastructure迁入) │
└─────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────┐
│                    LYBT.Desktop.Foundation              │
│  (技术基础设施层 - HTTP/Security/Cache/Logging)          │
│  - Security/ (认证、Token管理)                           │
│  - Http/ (ApiService、DelegatingHandlers)               │
│  - Caching/ (缓存服务)                                   │
│  - Logging/ (CorrelationId等)                           │
│  - 不变，保持当前职责                                     │
└─────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────┐
│                    LYBT.Desktop.Infrastructure          │
│  (WPF基础设施层 - 合并Presentation内容)                   │
│  - Controls/ (保留 + 迁入HerbCard/HerbList)             │
│  - Converters/ (保留)                                   │
│  - Services/ (保留 + 迁入NotificationService/UserExperience) │
│  - Themes/ (保留 + 迁入MedicalCaseStyles)               │
│  - Events/Behaviors/Commands/ (保留)                    │
│  - 删除Interfaces/目录，接口迁移到Contracts             │
└─────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────┐
│                    LYBT.Desktop.Models                  │
│  (ViewModel层 - 解除对Infrastructure的依赖)             │
│  - ViewModels/Base/ (ViewModelBase依赖Contracts接口)    │
│  - Items/ (业务模型)                                    │
│  - Mappers/ (映射器)                                    │
│  - 依赖: Contracts (接口) + Foundation (技术服务)       │
└─────────────────────────────────────────────────────────┘
```

### 删除项目

- **LYBT.Desktop.Presentation** - 完全合并到Infrastructure后删除

### 变更范围

#### Phase 1: 接口迁移 (Contracts扩展)
- `Infrastructure.Interfaces/` → `Contracts.Services/`
- `Infrastructure.Interfaces/Components/` → `Contracts.Components/`
- 约17个接口文件迁移

#### Phase 2: Presentation合并 (Infrastructure扩展)
- `Presentation.Components/` → `Infrastructure.Controls/Components/`
- `Presentation.Notifications/` → `Infrastructure.Services/Notifications/`
- `Presentation.UserExperience/` → `Infrastructure.Services/UserExperience/`
- `Presentation.Theming/` → `Infrastructure.Themes/`
- 约7个文件迁移

#### Phase 3: Models解耦
- `Models.ViewModels.Base.ViewModelBase` 改为依赖 `Contracts` 接口
- 移除 `Models` 对 `Infrastructure` 的项目引用
- 修正依赖方向

#### Phase 4: 清理
- 删除 `LYBT.Desktop.Presentation` 项目
- 更新所有 `using` 语句
- 更新所有 `.csproj` 引用

## Impact Analysis

### 受影响的项目

| 项目 | 影响类型 | 变更文件数(估算) |
|------|----------|-----------------|
| Contracts | 扩展 | +17 (接口迁入) |
| Foundation | 无变化 | 0 |
| Infrastructure | 扩展+重组 | +7 (文件迁入), ~30 (命名空间调整) |
| Models | 重构 | ~6 (依赖调整) |
| Presentation | 删除 | -7 (全部迁走) |
| Shell | 引用调整 | ~10 (using调整) |
| Modules/* | 引用调整 | ~50 (using调整) |

### 收益

1. **项目数减少**: 5 → 4，减少编译和维护复杂度
2. **依赖方向正确化**: Models不再依赖Infrastructure
3. **接口集中管理**: 所有可复用接口统一在Contracts
4. **UI控件统一**: 所有WPF控件统一在Infrastructure.Controls

### 风险

1. **大量using语句调整**: 需要批量替换命名空间
2. **潜在的循环依赖**: 接口迁移时需仔细检查
3. **编译时间**: 变更期间需多次编译验证

## Alternatives Considered

### 方案A: 保持5项目，只做局部调整
- 优点: 变更小
- 缺点: 不解决根本问题(依赖方向、功能边界)
- **决定: 不采用**

### 方案B: 合并为3项目 (Contracts + Core + Models)
- 优点: 最简化
- 缺点: Foundation和Infrastructure职责差异大，强行合并降低内聚性
- **决定: 不采用**

### 方案C (推荐): 合并为4项目
- 优点: 平衡简化和职责清晰
- 缺点: 仍需较大重构工作量
- **决定: 采用**

## Dependencies

- 无外部依赖
- 与其他进行中的OpenSpec变更无冲突

## Testing Strategy

1. **编译验证**: 每个Phase完成后执行完整编译
2. **单元测试**: 确保现有测试全部通过
3. **功能验证**: 验证关键UI组件(HerbCard、Notification)正常工作
4. **回归测试**: 执行完整业务流程测试

## Rollback Plan

每个Phase完成后提交，如需回滚:
- Phase 4失败 → 还原Phase 4提交
- Phase 3失败 → 还原Phase 3+4提交
- Phase 2失败 → 还原Phase 2+3+4提交
- Phase 1失败 → 还原所有提交

## Approval

- [ ] 技术评审通过
- [ ] 用户确认执行

---

## 附录: 详细文件清单

### Contracts新增文件 (Phase 1)

```
Contracts/
├── Services/
│   ├── IActiveConsultationService.cs
│   ├── IApplicationTickService.cs
│   ├── IClinicSettingsService.cs
│   ├── ICommonDialogService.cs
│   ├── ICustomDialogAware.cs
│   ├── IFeatureToggleService.cs
│   ├── IKeyboardShortcutService.cs
│   ├── ILoginCoordinator.cs
│   ├── IMainWindowServicesFacade.cs
│   ├── IPermissionService.cs
│   ├── IPrescriptionSettingsService.cs
│   ├── IRoleNavigationService.cs
│   ├── ISessionManager.cs
│   ├── IStartupPipeline.cs
│   ├── IUserActivityTracker.cs
│   └── IUserNotificationService.cs
└── Components/
    ├── ICommandHandler.cs
    ├── IComponentValidator.cs
    ├── IDataManager.cs
    └── IValidationService.cs
```

### Infrastructure新增文件 (Phase 2)

```
Infrastructure/
├── Controls/
│   ├── Components/
│   │   ├── HerbCardControl.xaml
│   │   ├── HerbCardControl.xaml.cs
│   │   ├── HerbListEditor.xaml
│   │   ├── HerbListEditor.xaml.cs
│   │   ├── HerbListView.xaml
│   │   └── HerbListView.xaml.cs
├── Services/
│   ├── Notifications/
│   │   ├── INotificationService.cs
│   │   └── NotificationService.cs
│   └── UserExperience/
│       └── UserExperienceService.cs
└── Themes/
    └── MedicalCaseStyles.xaml (从Presentation迁入)
```

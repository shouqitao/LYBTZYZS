# unify-navigation-architecture Tasks

## Overview

- **变更类型**: Refactor
- **风险等级**: Medium
- **预估工作量**: 4-4.5小时
- **影响范围**: 整个Desktop层所有UI导航

## Phase 1: 消除GetHomeViewName重复 (30分钟)

### 1.1 确认RoleRegistry为权威源
- **文件**: `src/Client/Desktop/Shell/Config/RoleRegistry.cs`
- **变更**: 确保GetHomeViewName方法完整且公开
- **验证**: 方法签名正确，返回值覆盖所有角色

### 1.2 修改MenuManager
- **文件**: `src/Client/Desktop/Shell/Services/MenuManager.cs:120-129`
- **变更**: 删除本地GetHomeViewName实现，注入IRoleRegistry并调用其方法
- **验证**: NavigateToHomeCommand功能正常

### 1.3 修改NavigableViewModelBase
- **文件**: `src/Client/Desktop/Infrastructure/ViewModels/NavigableViewModelBase.cs:350-372`
- **变更**: 删除本地GetHomeViewName实现，注入IRoleRegistry调用
- **验证**: 继承此基类的ViewModel导航正常

### 1.4 修改UnifiedViewModelBase
- **文件**: `src/Client/Desktop/Infrastructure/ViewModels/UnifiedViewModelBase.cs:103-141`
- **变更**: 删除本地GetHomeViewName实现，注入IRoleRegistry调用
- **验证**: 继承此基类的ViewModel导航正常

### 1.5 Phase 1编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Phase 2: 引入ViewNames常量类 + 视图合并 (1.5小时)

### 2.1 创建ViewNames常量类
- **文件**: `src/Client/Desktop/Shell/Constants/ViewNames.cs` [NEW]
- **变更**: 创建包含16个视图名称的静态常量类（ADR-6规范）
- **验证**: 编译通过

### 2.2 替换Shell层硬编码
- **文件**: Shell/Services/*.cs, Shell/ViewModels/*.cs
- **变更**: 将字符串视图名称替换为ViewNames常量
- **验证**: 编译通过

### 2.3 替换Infrastructure层硬编码
- **文件**: Infrastructure/ViewModels/*.cs
- **变更**: 将字符串视图名称替换为ViewNames常量
- **验证**: 编译通过

### 2.4 替换Roles层硬编码
- **文件**: Roles/LYBT.Desktop.Admin/ViewModels/*.cs
- **文件**: Roles/LYBT.Desktop.Clinical/ViewModels/*.cs
- **变更**: 将字符串视图名称替换为ViewNames常量
- **验证**: 编译通过

### 2.5 替换Modules层硬编码
- **文件**: Modules/*/ViewModels/*.cs
- **变更**: 将字符串视图名称替换为ViewNames常量
- **验证**: 编译通过

### 2.6 合并功能到AccountSettingsView
- **文件**: AccountSettingsView及其ViewModel
- **变更**: 合并UserProfileView和ChangePasswordView的功能
- **验证**: 账户设置功能完整（个人信息、密码修改）

### 2.7 删除UserProfileView
- **文件**: Views/UserProfileView.xaml, ViewModels/UserProfileViewModel.cs
- **变更**: 确认功能已迁移后删除文件
- **依赖**: 2.6完成
- **验证**: 无编译错误，无引用断裂

### 2.8 删除ChangePasswordView
- **文件**: Views/ChangePasswordView.xaml, ViewModels/ChangePasswordViewModel.cs
- **变更**: 确认功能已迁移后删除文件
- **依赖**: 2.6完成
- **验证**: 无编译错误，无引用断裂

### 2.9 Phase 2验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 使用Grep确认无遗留硬编码（排除ViewNames.cs自身）

## Phase 3: 统一INavigationAware实现 (45分钟)

### 3.1 创建NavigationAwareViewModelBase
- **文件**: `src/Client/Desktop/Infrastructure/ViewModels/NavigationAwareViewModelBase.cs` [NEW]
- **变更**: 实现INavigationAware + IConfirmNavigationRequest
- **验证**: 编译通过

### 3.2 定义导航生命周期钩子
- **文件**: NavigationAwareViewModelBase.cs
- **变更**: 添加可覆盖的OnNavigatingTo/OnNavigatedTo/OnNavigatingFrom/OnNavigatedFrom方法
- **验证**: 编译通过

### 3.3 迁移NavigableViewModelBase
- **文件**: `src/Client/Desktop/Infrastructure/ViewModels/NavigableViewModelBase.cs`
- **变更**: 继承NavigationAwareViewModelBase或整合功能
- **验证**: 现有ViewModel行为不变

### 3.4 迁移UnifiedViewModelBase
- **文件**: `src/Client/Desktop/Infrastructure/ViewModels/UnifiedViewModelBase.cs`
- **变更**: 添加IConfirmNavigationRequest支持
- **验证**: 现有ViewModel行为不变

### 3.5 Phase 3编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Phase 4: 创建INavigationCoordinator (1小时)

### 4.1 定义INavigationCoordinator接口
- **文件**: `src/Client/Desktop/Contracts/Services/INavigationCoordinator.cs` [NEW]
- **变更**: 定义统一导航入口接口
- **验证**: 编译通过

### 4.2 实现NavigationCoordinator
- **文件**: `src/Client/Desktop/Shell/Services/NavigationCoordinator.cs` [NEW]
- **变更**: 实现INavigationCoordinator，整合NavigationManager+导航历史功能
- **验证**: 编译通过

### 4.3 注册DI服务
- **文件**: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- **变更**: 注册INavigationCoordinator服务
- **验证**: 依赖注入正常

### 4.4 迁移MenuManager使用NavigationCoordinator
- **文件**: `src/Client/Desktop/Shell/Services/MenuManager.cs`
- **变更**: 使用INavigationCoordinator替代直接调用NavigationManager
- **验证**: 菜单导航功能正常

### 4.5 迁移MainWindowViewModel
- **文件**: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- **变更**: 使用INavigationCoordinator
- **验证**: 主窗口导航功能正常

### 4.6 Phase 4编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Phase 5: 清理和文档 (30分钟)

### 5.1 评估ViewNavigationService
- **文件**: `src/Client/Desktop/Shell/Services/ViewNavigationService.cs`
- **变更**: 搜索引用，确认功能已整合到NavigationCoordinator后删除
- **验证**: 无引用断裂

### 5.2 完善RoleRegistry映射
- **文件**: `src/Client/Desktop/Shell/Config/RoleRegistry.cs`
- **变更**: 确保所有角色有完整的视图映射
- **验证**: 所有角色导航正常

### 5.3 更新Serena记忆
- **操作**: 记录新导航架构设计决策
- **验证**: 记忆文件已创建

### 5.4 最终编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误

## Validation Checklist

- [x] Desktop解决方案编译通过（0错误0警告）
- [x] Admin主页导航正常
- [x] Clinical主页导航正常
- [x] 角色切换正确导航到对应主页
- [x] 返回主页功能在所有视图可用
- [x] 所有导航按钮功能正常
- [x] 账户设置功能完整（合并后）

## Dependencies

```
Phase 1 ─────────────────────┐
                             │
Phase 2 ─────────────────────┼──> Phase 5
                             │
Phase 3 ─────────────────────┤
                             │
Phase 4 ─────────────────────┘
```

Phase 1-4可以并行开发，但建议按顺序执行以降低风险。
Phase 5依赖所有前序Phase完成。

## Phase 6: Roles层ViewModel整合INavigationCoordinator (追加)

> **追加原因**: Phase 4创建了INavigationCoordinator，但Roles层ViewModel未完成迁移。
> 发现时间: 2026-01-12，诊断"暂存医案导航失败"问题时发现。

### 6.1 整合PatientSelectionViewModel
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- **变更**:
  1. 注入`INavigationCoordinator`依赖
  2. 替换`RegionManager.RequestNavigate()`为`_navigationCoordinator.NavigateTo()`
  3. 添加`MedicalCaseId`为空时的错误处理（解决静默失败问题）
- **验证**: 导航失败时有明确错误提示

### 6.2 整合其他Clinical层ViewModel
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/*.cs`
- **变更**: 检查并迁移所有直接使用`RegionManager.RequestNavigate()`的调用
- **验证**: 编译通过

### 6.3 整合Admin层ViewModel
- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/*.cs`
- **变更**: 检查并迁移所有直接使用`RegionManager.RequestNavigate()`的调用
- **验证**: 编译通过

### 6.4 Phase 6编译验证
- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误
- 测试暂存医案导航流程

## Phase 7: 完整统一导航服务架构 (ADR-7)

> **追加原因**: 架构优先原则。发现4个导航服务存在功能重叠、层级混乱问题，决定完整统一到INavigationCoordinator。
> 追加时间: 2026-01-12

### 7.1 扩展INavigationCoordinator接口

- **文件**: `src/Client/Desktop/Contracts/Services/INavigationCoordinator.cs`
- **变更**:
  1. 添加 `NavigationHistory` 属性 (从ViewNavigationService)
  2. 添加 `ClearHistory()` 方法 (从ViewNavigationService)
  3. 添加 `NavigationChanged` 事件 (从ViewNavigationService)
  4. 添加 `ShowLoginDialog()` 方法 (从NavigationManager)
  5. 添加 `ClearLoginRegion()` 方法 (从NavigationManager)
  6. 添加 `ClearContentRegion()` 方法 (从NavigationManager)
  7. 添加 `SubscribeToRegionCollection()` 方法 (从NavigationManager)
  8. 添加 `UnsubscribeFromRegionCollection()` 方法 (从NavigationManager)
- **验证**: 编译通过

### 7.2 实现NavigationCoordinator新功能

- **文件**: `src/Client/Desktop/Shell/Services/NavigationCoordinator.cs`
- **变更**:
  1. 实现导航历史管理 (List<string> + Push/Pop逻辑)
  2. 实现NavigationChanged事件
  3. 实现ShowLoginDialog (从NavigationManager移植)
  4. 实现ClearLoginRegion/ClearContentRegion (从NavigationManager移植)
  5. 实现Subscribe/Unsubscribe (从NavigationManager移植)
- **验证**: 编译通过

### 7.3 更新MasterDetailServices依赖

- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/MasterDetailServices.cs`
- **变更**:
  1. 将 `IViewNavigationService` 依赖改为 `INavigationCoordinator`
  2. 更新 `Navigation` 属性类型为 `INavigationCoordinator`
- **验证**: 5个MasterDetail ViewModel导航正常

### 7.4 更新MainWindowViewModel依赖

- **文件**: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- **变更**:
  1. 移除 `INavigationManager` 依赖
  2. 使用 `INavigationCoordinator` 替代所有导航调用
- **验证**: 主窗口导航功能正常

### 7.5 更新LoginCoordinator依赖

- **文件**: `src/Client/Desktop/Shell/Services/LoginCoordinator.cs`
- **变更**:
  1. 移除 `INavigationManager` 依赖
  2. 使用 `INavigationCoordinator.ShowLoginDialog()` 和 `ClearLoginRegion()`
- **验证**: 登录/登出流程正常

### 7.6 删除NavigationManager

- **文件**: `src/Client/Desktop/Shell/Services/NavigationManager.cs`
- **操作**: 确认无引用后删除
- **验证**: 编译通过

### 7.7 删除ViewNavigationService

- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ViewNavigationService.cs`
- **操作**: 确认无引用后删除
- **验证**: 编译通过

### 7.8 删除RoleNavigationService

- **文件**: `src/Client/Desktop/Shell/Services/RoleNavigationService.cs`
- **操作**: 确认无引用后删除
- **验证**: 编译通过

### 7.9 删除冗余接口定义

- **文件**:
  - `src/Client/Desktop/Contracts/Services/INavigationManager.cs`
  - `src/Client/Desktop/Contracts/Services/IViewNavigationService.cs`
  - `src/Client/Desktop/Contracts/Services/IRoleNavigationService.cs`
- **操作**: 确认无引用后删除
- **验证**: 编译通过

### 7.10 清理DI注册

- **文件**: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- **变更**: 移除已删除服务的注册代码
- **验证**: 应用启动正常

### 7.11 Phase 7编译验证

- 运行 `dotnet build LYBT.All.sln -c Release --no-restore`
- 确保零编译错误
- 搜索确认无遗留引用 (`INavigationManager`, `IViewNavigationService`, `IRoleNavigationService`)

## Notes

- ~~保持向后兼容：不删除现有公开API，仅标记Obsolete~~ (Phase 7改为完整清理)
- 渐进式迁移：新代码使用新架构，旧代码逐步迁移
- 每个Phase独立验证：确保可回滚
- ADR-6视图合并：UserProfileView/ChangePasswordView合并到AccountSettingsView
- **Phase 6追加**: Roles层ViewModel需整合INavigationCoordinator，解决导航静默失败问题
- **Phase 7追加**: 完整统一所有导航服务到INavigationCoordinator，删除冗余服务和接口

---

**生成时间**: 2026-01-10
**执行完成时间**: 2026-01-10 (Phase 1-5)
**Phase 6追加时间**: 2026-01-12
**Phase 7追加时间**: 2026-01-12
**状态**: 进行中 (Phase 6执行中, Phase 7待执行)

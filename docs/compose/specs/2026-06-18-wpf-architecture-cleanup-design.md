# WPF 架构整理 — 设计规格

> 日期: 2026-06-18
> 子项目: C (架构整理)

## [S1] 问题

四个核心文件超过 590 行，职责过度集中：

1. **MainWindowViewModel (813行)**：上帝 ViewModel——登录协调、Token 生命周期、时钟更新、活动追踪、导航状态、连接模式、会话管理全部挤在一个构造函数注入 14 个依赖的类中
2. **EnhancedNavigationService (612行)**：导航执行、面包屑管理、历史记录、搜索建议四职责合一
3. **MasterDetailViewModelBase (590行)**：基类过重——分页、搜索、选择、编辑、保存、删除全在基类
4. **CredentialVault (606行)**：DPAPI 加密、凭据存储、密码策略、账户验证混在一起

## [S2] 目标

将每个大文件拆分为职责单一的小文件（目标：每个文件 <300 行）。原文件保留为协调者或瘦身后保留。不改变任何公开 API——仅内部重组。

## [S3] 原则

1. **纯提取**：只搬运代码到新文件，不改变逻辑
2. **接口不变**：所有公开的属性、方法、命令签名保持不变
3. **DI 兼容**：新提取的服务通过 DI 注册，原有消费者无感知
4. **可增量提交**：每个拆分独立提交，随时可停

## [S4] MainWindowViewModel 拆分

### 当前职责 → 提取目标

| 当前职责 | 行数估计 | 提取到 | 类型 |
|----------|---------|--------|------|
| Token 生命周期监控 | ~80行 | TokenMonitorService (已有 ITokenLifecycleService) | 增强现有服务 |
| 时钟更新 (DispatcherTimer) | ~40行 | ClockTickService (已有 IApplicationTickService) | 增强现有服务 |
| 用户活动追踪 | ~50行 | UserActivityTracker (已有 IUserActivityTracker) | 增强现有服务 |
| 连接模式管理 | ~40行 | ConnectionModeManager | 委托给 IConnectionModeService |
| 导航状态管理 | ~60行 | NavigationStateManager | 委托给 INavigationCoordinator |
| 登录/登出协调 | ~100行 | 保留在 MainWindowVM | 核心 |
| UI 状态 (IsLoggedIn 等) | ~50行 | 保留在 MainWindowVM | 核心 |
| 菜单/快捷键命令 | ~80行 | 保留在 MainWindowVM（委托 MenuManager） | 核心 |
| 构造函数 + 字段 | ~100行 | 瘦身后 ~300行 | 核心 |

### 拆分策略

将 Token 监控、时钟、活动追踪的逻辑**移到已有的服务实现中**（这些服务已注入但逻辑仍留在 ViewModel 中）。MainWindowViewModel 仅保留：
- 登录/登出流程协调
- UI 状态属性 (IsLoggedIn, IsDrawerOpen 等)
- 菜单命令委托

## [S5] EnhancedNavigationService 拆分

### 当前职责 → 提取目标

| 职责 | 提取到 |
|------|--------|
| 导航执行 | 保留在 EnhancedNavigationService |
| 面包屑管理 | BreadcrumbService |
| 历史记录管理 | NavigationHistoryService |
| 搜索建议 | NavigationSuggestionsService |

### 策略

面包屑、历史、搜索建议已经是独立的 UserControl（NavigationHistoryPanel, NavigationSuggestionsPanel, BreadcrumbControl）。将它们的 ViewModel 逻辑从 EnhancedNavigationService 中提取为独立 Service。

EnhancedNavigationService 瘦身后仅保留：NavigateTo/NavigateBack/NavigateForward + 导航事件发布。

## [S6] MasterDetailViewModelBase 瘦身

### 当前问题

基类直接实现了分页、搜索、选择、CRUD——这些本应委托给已注册的组合服务（`IListViewServices<T>`, `IMasterDetailServices<T,TDetail>`）。

### 策略

将直接实现改为委托调用：
- `CurrentPage`/`TotalPages`/`PageSize` → 委托 `IPaginationService`
- `SearchText`/`Search()` → 委托 `ISearchService`
- `Items`/`SelectedItem` → 委托 `ISelectionService<T>`
- `IsLoading`/`ErrorMessage` → 保留（UI 状态）

**不改继承层级**——只把方法体从内联实现改为 `return _paginationService.CurrentPage` 这样的委托。

## [S7] CredentialVault 拆分

### 当前职责 → 提取目标

| 职责 | 提取到 |
|------|--------|
| DPAPI 加密/解密 | DpapiProtector (internal class) |
| 凭据存储 (文件读写) | CredentialStorage |
| 密码策略 (强度检查等) | PasswordPolicy |
| 公共 API | CredentialVault (协调者，~150行) |

### 策略

`ICredentialVault` 接口不变。内部拆为 3 个 internal 类，CredentialVault 委托调用。

## [S8] 不在范围内

- 功能变更（不增加/删除功能）
- 测试重写（现有测试应该通过，因为接口不变）
- 重命名公共 API

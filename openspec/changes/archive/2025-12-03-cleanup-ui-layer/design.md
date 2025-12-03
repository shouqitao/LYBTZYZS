# Design: cleanup-ui-layer

## 架构概述

本设计文档描述UI层全面清理的架构决策和实现策略。

## 当前架构分析

### ViewModelBase继承体系

```
BindableBase (Prism - 外部库)
    │
    ▼
ViewModelBase (537行) - LYBT.Desktop.Models
    ├─ 属性: IsLoading, IsBusy, HasError, ErrorMessage
    ├─ 验证: INotifyDataErrorInfo实现
    ├─ 错误处理: ExecuteSafelyAsync<T>
    ├─ 日志: Logger属性
    └─ 资源管理: Disposable模式
    │
    ▼
UnifiedViewModelBase (576行) - LYBT.Desktop.Models
    ├─ 导航: INavigationAware实现
    ├─ 初始化: InitializeAsync抽象方法
    ├─ 消息: ShowSuccessMessageAsync等
    ├─ 会话: GetCurrentUserInfo
    └─ 服务注入: UserNotificationService等
    │
    ▼
UnifiedListViewModelBase<T> (605行) - LYBT.Desktop.Models
    ├─ 列表: Items, SelectedItem, SelectedItems
    ├─ 分页: PageSize, TotalCount, CurrentPage
    ├─ 命令: SearchCommand, RefreshCommand, AddCommand等
    ├─ 搜索: SearchWithDebounceAsync
    └─ 排序: SortColumn, SortDirection
```

**问题**:
1. 继承链过长(4层)，增加理解和维护成本
2. 基类职责过多，违反SRP
3. 并非所有ViewModel都需要完整功能栈

### 服务层职责重叠

```
LYBT.Desktop.Foundation/
├─ Security/          # 认证、Token管理
├─ Http/              # API通信
├─ Exceptions/        # 异常处理
└─ ...

LYBT.Desktop.Infrastructure/Services/
├─ UserNotificationService  # 用户通知
├─ SessionManager          # 会话管理 (与Foundation重叠?)
├─ CommonDialogService     # 对话框服务
└─ ...

LYBT.Desktop.Presentation/
├─ Notifications/     # 又一套通知机制
├─ UserExperience/    # 用户体验服务
└─ ...
```

**问题**:
- 三层都有通知/错误处理相关代码
- 服务命名和职责边界不清晰
- 部分服务功能重复

## 重构设计

### Phase 1: ViewModel继续重构

#### 1.1 拆分PrescriptionPanelViewModel (1484行)

**当前职责分析**:
```csharp
// PrescriptionPanelViewModel包含:
// 1. 处方数据管理
// 2. 药材列表操作
// 3. 剂量计算
// 4. 价格计算
// 5. 验证逻辑
// 6. 弹窗交互
// 7. 事件协调
```

**重构后结构**:
```
Prescriptions/ViewModels/Components/
├─ PrescriptionCalculator.cs      # 剂量和价格计算
├─ PrescriptionValidator.cs       # 验证逻辑
├─ PrescriptionEventHandler.cs    # 事件处理
└─ PrescriptionDataManager.cs     # 数据加载和保存

PrescriptionPanelViewModel.cs     # 协调器 (<500行)
```

#### 1.2 拆分PatientSelectionViewModel (1429行)

**当前职责分析**:
```csharp
// PatientSelectionViewModel包含:
// 1. 患者搜索
// 2. 分页加载
// 3. 队列管理
// 4. 快速创建患者
// 5. 未完成医案检测
// 6. 事件发布
```

**重构后结构**:
```
Patients/ViewModels/Components/
├─ PatientSearchHandler.cs        # 搜索和分页
├─ PatientQueueManager.cs         # 队列管理
├─ PatientValidator.cs            # 患者验证
└─ PatientEventCoordinator.cs     # 事件协调

PatientSelectionViewModel.cs      # 协调器 (<500行)
```

#### 1.3 精简ViewModelBase继承链

**目标**: 从4层减少到3层，提供更灵活的组合选项

**方案A - 合并中间层**:
```
BindableBase (Prism)
    │
    ▼
ViewModelBase (合并后 ~700行)
    ├─ 核心属性 + 验证
    ├─ 导航支持 (可选)
    └─ 消息显示 (可选)
    │
    ▼
UnifiedListViewModelBase<T> (保持 ~600行)
    └─ 列表专用功能
```

**方案B - 使用组合替代继承** (推荐):
```
ViewModelBase (精简到 ~300行)
    └─ 核心属性 + 验证

组件注入:
├─ INavigationHandler      # 导航功能
├─ IMessagePresenter       # 消息显示
├─ IAsyncOperationHandler  # 异步操作
└─ IListOperations<T>      # 列表操作

UnifiedViewModelBase - 提供默认组合
UnifiedListViewModelBase<T> - 列表专用组合
```

### Phase 2: View层样式统一

#### 2.1 全局样式库设计

```
Presentation/Themes/
├─ GlobalStyles.xaml          # 全局样式入口
├─ Colors.xaml                # 颜色定义
├─ Typography.xaml            # 字体样式
├─ Controls/
│   ├─ ButtonStyles.xaml      # 按钮样式
│   ├─ TextBoxStyles.xaml     # 文本框样式
│   ├─ DataGridStyles.xaml    # 表格样式
│   └─ CardStyles.xaml        # 卡片样式
└─ Templates/
    ├─ DialogTemplates.xaml   # 对话框模板
    └─ ListTemplates.xaml     # 列表项模板
```

#### 2.2 对话框目录统一

**当前结构**:
```
Module/Views/SomeDialog.xaml        # 部分在Views
Module/Dialogs/OtherDialog.xaml     # 部分在Dialogs
```

**统一后结构**:
```
Module/
├─ Views/           # 页面视图
├─ Dialogs/         # 所有对话框
└─ ViewModels/
    └─ Dialogs/     # 对话框ViewModel
```

### Phase 3: 基础设施层整理

#### 3.1 服务层职责划分

```
Foundation/                    # 基础设施服务
├─ Security/                  # 保持不变
├─ Http/                      # 保持不变
└─ Core/                      # 核心服务
    ├─ ExceptionHandler       # 统一异常处理
    └─ ConfigurationService   # 配置服务

Infrastructure/               # 应用级服务
├─ Session/                   # 会话相关
│   ├─ SessionManager
│   └─ UserActivityTracker
├─ Notifications/             # 统一到此
│   ├─ INotificationService   # 接口
│   └─ NotificationService    # 实现
└─ Dialogs/                   # 对话框服务
    └─ DialogService

Presentation/                 # UI相关
├─ Themes/                    # 样式
├─ Components/                # 控件
└─ Navigation/                # 导航
```

#### 3.2 删除/合并清单

**删除候选**:
- Foundation/Exceptions/ExceptionSeverity.cs (如果未使用)
- 重复的接口定义

**合并候选**:
- Presentation/Notifications/* → Infrastructure/Notifications/
- 多个ErrorHandler相关类合并

### Phase 4: 交互模式标准化

#### 4.1 对话框使用模式

**标准化接口**:
```csharp
public interface IDialogCoordinator
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task ShowInformationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message, Exception? ex = null);
    Task<TResult?> ShowDialogAsync<TResult>(string dialogName, IDialogParameters parameters);
}
```

**使用示例**:
```csharp
// 统一用法
var confirmed = await _dialogCoordinator.ShowConfirmationAsync(
    "确认删除",
    "确定要删除此患者吗？");

// 避免直接使用
// MessageBox.Show(...) // 禁止
// _dialogService.ShowDialog(...) // 封装后使用
```

#### 4.2 通知显示机制

**统一接口**:
```csharp
public interface IUserNotification
{
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    Task HandleExceptionAsync(Exception ex, string context);
}
```

## 依赖关系图

```
cleanup-ui-layer
    │
    ├─► Phase 1 (ViewModel继续重构)
    │       │
    │       ├── 依赖: refactor-viewmodel-layer
    │       │
    │       └─► Phase 3 (基础设施整理)
    │
    ├─► Phase 2 (样式统一)
    │       │
    │       └─► Phase 4 (交互模式标准化)
    │
    └─► Phase 3 ─► Phase 4
```

## 风险和缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 基类修改影响广泛 | 高 | 保持向后兼容，渐进式迁移 |
| 样式迁移遗漏 | 中 | 自动化检查脚本 |
| 服务合并破坏依赖 | 高 | 保留旧接口，标记Obsolete |
| 测试覆盖不足 | 中 | 优先为关键路径添加测试 |

## 测试策略

1. **单元测试**: 新Components独立测试
2. **集成测试**: ViewModel协调测试
3. **UI测试**: 关键流程手动验证
4. **回归测试**: 现有测试全部通过

## 决策记录

### ADR-UI-001: 为什么选择组合优于继承

**决策**: Phase 3中ViewModelBase优化采用组合模式

**原因**:
1. 降低继承链复杂度
2. 允许按需组合功能
3. 更容易测试独立组件
4. 遵循SOLID原则

**后果**:
- 需要更多DI配置
- 现有ViewModel需要逐步迁移
- 保持UnifiedViewModelBase提供默认组合

### ADR-UI-002: 样式统一策略

**决策**: 创建全局样式库，模块可扩展但不重复定义

**原因**:
1. 减少重复代码
2. 统一视觉风格
3. 易于主题切换

**后果**:
- 需要迁移现有样式
- 模块特定样式需标记为扩展

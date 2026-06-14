# LYBT.Desktop.Infrastructure

> WPF基础设施库 | 会话管理/控件/转换器/事件

## 项目定位

- **层级**: Client Core层
- **职责**: 提供WPF专属的UI基础设施(Controls/Converters/Events/Services)

## 目录结构

```
LYBT.Desktop.Infrastructure/
├── Commands/                     # 全局命令
├── Constants/                    # 常量定义(3文件)
├── Controls/                     # 自定义控件(7个)
├── Converters/                   # 数据转换器(13个)
├── Events/                       # Prism事件(11个)
├── Extensions/                   # 扩展方法(2文件)
├── Helpers/                      # 辅助类(3文件)
├── Interfaces/                   # 服务接口(11个)
├── Repositories/                 # 仓储基类
├── Services/                     # 核心服务(8个)
│   ├── ErrorHandling/
│   ├── Navigation/
│   ├── SessionManager.cs
│   └── ...
└── Templates/                    # T4模板
```

## 核心服务

| 服务 | 成员数 | 说明 |
|------|--------|------|
| ISessionManager | 27 | 用户会话/权限检查/会话事件 |
| ErrorHandlingService | 13 | 全局异常处理/友好消息 |
| EnhancedNavigationService | 6 | 页面导航/历史管理 |
| UserNotificationService | 8 | Toast通知/确认对话框 |
| KeyboardShortcutService | 11 | 全局快捷键注册 |
| FeatureToggleService | 2 | 功能开关 |

## 自定义控件

| 控件 | 说明 |
|------|------|
| VirtualizedDataGrid | 虚拟化数据网格(支持10000+行) |
| VirtualizedListView | 虚拟化列表视图 |
| GlobalStatusBar | 全局状态栏 |
| LoginStatusControl | 登录状态控件 |
| ErrorNotificationControl | 错误通知控件 |

## 数据转换器

| 转换器 | 说明 |
|--------|------|
| BooleanToVisibilityConverter | 布尔值→可见性 |
| DateTimeFormatConverter | 日期时间格式化 |
| EnumDescriptionConverter | 枚举→描述文本 |
| StatusToColorConverter | 状态→颜色 |
| NullToVisibilityConverter | 空值→可见性 |

## Prism事件

| 事件 | 说明 |
|------|------|
| PatientSelectedEvent | 患者选中 |
| LoginSuccessEvent | 登录成功 |
| LogoutEvent | 登出 |
| PrescriptionCompletedEvent | 处方完成 |
| DataRefreshEvent | 数据刷新 |

## 设计依据

- 与Foundation分层: Foundation提供平台无关能力，Infrastructure提供WPF专属基础设施，职责边界清晰
- SessionManager集中管理用户会话和权限状态，避免各模块各自维护登录状态导致的不一致
- 自定义控件(VirtualizedDataGrid等)封装WPF虚拟化技术，使业务模块无需关心大数据量渲染性能
- Prism事件总线实现模块间松耦合通信，模块通过发布/订阅事件交互而非直接引用

## 依赖关系

### 依赖
- LYBT.Shared.Models
- LYBT.Desktop.Foundation
- Prism.Core/Prism.Wpf (8.x)
- NPOI (Excel操作)

### 被依赖
- LYBT.Desktop.Shell
- 所有Desktop业务模块
- 所有Desktop工作站

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Desktop.Infrastructure 模块说明

## XAML资源加载顺序规则

**重要**: WPF资源字典中的样式定义顺序敏感，`BasedOn`引用的样式必须在被引用之前定义。

### 已知问题及解决方案

1. **BaseDataGridCell 前向引用问题** (2026-01-04修复)
   - 问题: `MasterDetailDataGridCellStyle` 使用 `BasedOn="{StaticResource BaseDataGridCell}"`，但 `BaseDataGridCell` 定义在后面
   - 解决: 将 `BaseDataGridCell` 移到 `MasterDetailDataGridCellStyle` 之前

2. **跨文件资源引用问题** (2026-01-04修复)
   - 问题: `ValidationStyles.xaml` 中的 `ValidatingTextBoxStyle` 继承自 `EditableTextBoxStyle`，但 `ValidationStyles.xaml` 在 `UnifiedComponents.xaml` 开头被合并
   - 解决: 将 `ValidatingTextBoxStyle` 迁移到 `UnifiedComponents.xaml`，放在 `EditableTextBoxStyle` 之后

### 资源文件结构

```
Themes/
├── UnifiedComponents.xaml  # 主资源字典，合并其他资源
│   ├── 合并 ValidationStyles.xaml (开头)
│   ├── EditableTextBoxStyle (第412行)
│   ├── ValidatingTextBoxStyle (第478行，继承EditableTextBoxStyle)
│   ├── BaseDataGridCell (第633行)
│   └── MasterDetailDataGridCellStyle (第658行，继承BaseDataGridCell)
└── ValidationStyles.xaml   # 验证相关样式（基础样式，无继承依赖）
```

### 添加新样式的规则

1. 如果新样式使用 `BasedOn` 继承，确保基类样式已在前面定义
2. 跨文件继承时，检查资源字典合并顺序
3. 优先在 `UnifiedComponents.xaml` 中定义有继承关系的样式

### 资源引用方式规范 (OpenSpec: cleanup-control-resource-merging, 2026-01-21)

**关键规则**：

| 资源类型 | 引用方式 | 原因 |
|----------|----------|------|
| **Style** | `DynamicResource` | 支持主题切换 |
| **Style.BasedOn** | `StaticResource` | **必须！** BasedOn不是DependencyProperty |
| **Converter** | `StaticResource` | **必须！** Binding.Converter不是DependencyProperty |
| **Brush/Color** | `DynamicResource` | 支持主题切换 |

**详细指南**: 见 `XAML-RESOURCE-GUIDE.md`

### 资源架构 (OpenSpec: migrate-to-handycontrol, 2026-01-22)

**资源层级**:
```
Shell/App.xaml (资源入口 - Application.Resources)
├── HandyControl SkinDefault.xaml + Theme.xaml  ← HC基础皮肤和控件样式
├── TCM.Theme.xaml          ← 中医五行配色覆盖 (PrimaryBrush等)
├── Theme.Light.xaml        ← 项目间距定义 (Spacing.xaml)
├── UnifiedComponents.xaml  ← 项目组件样式
├── Typography.xaml         ← 文本样式 (H1TextBlock等)
└── Controls.xaml           ← 按钮/控件样式别名

Infrastructure/Themes/ (资源定义)
├── TCM.Theme.xaml          ← 中医主题 (覆盖HC颜色键: PrimaryBrush, DangerBrush等)
├── DesignTokens/
│   └── Spacing.xaml        ← 间距定义 (唯一保留的DesignToken)
├── Theme.Light.xaml        ← 主题入口 (仅合并 Spacing.xaml)
└── UnifiedComponents.xaml  ← 组件样式 (合并 Converters/PreviewStyles/ValidationStyles)
```

**颜色来源**: HandyControl + TCM.Theme.xaml (五行配色覆盖)
- `PrimaryBrush` = 木(青) #2E8B57 - 主色调
- `DangerBrush` = 火(赤) #DC143C - 危险/错误
- `WarningBrush` = 土(黄) #DAA520 - 警告
- `SuccessBrush` = 木(青) #228B22 - 成功
- `InfoBrush` = 水(黑) #4682B4 - 信息

### 资源引用架构原则 (2026-01-22定稿)

**核心原则**: 所有资源通过 `Application.Resources` 统一提供，控件级别**禁止**合并资源字典。

**资源查找顺序** (WPF标准):
```
当前元素 → 父元素 → ... → 根元素 → Application.Resources
```

**为什么控件级合并会导致崩溃**:
1. 重复合并相同资源字典可能导致资源键冲突
2. ContentPresenter 创建独立视觉树，但 DynamicResource 仍会向上查找到 Application.Resources
3. 控件级合并打破了统一的资源查找路径

**正确做法**:

| 资源类型 | 引用方式 | 来源 |
|----------|----------|------|
| **Style** | `DynamicResource` | Application.Resources |
| **Style.BasedOn** | `StaticResource` | Application.Resources |
| **Converter** | `x:Static converters:Cvt.XXX` | 静态实例 |
| **Brush/Color** | `DynamicResource` | Application.Resources |
| **本地样式** | 定义在控件 Resources 中 | 控件自身 |

**控件开发规范**:
- 控件可以定义本地样式 (x:Key)，但**不要**合并外部资源字典
- 本地样式中的 DynamicResource 会自动从 Application.Resources 获取
- StaticResource 按钮样式等也从 Application.Resources 获取

**已修复的控件** (移除了资源字典合并):
- `MasterDetailLayout.xaml` - 保留本地样式定义，移除 Theme.Light.xaml 合并
- `BaseDetailContainer.xaml` - 保留本地样式定义，移除 UnifiedComponents.xaml 合并

---

## Mapperly 直接映射架构 (OpenSpec: standardize-api-architecture)

### 架构演进 (2026-01-07)

**已删除**: `IMappingService<TDto, TInputDto, TItem>` 接口和 `MappingServiceBase` 基类
- 原因: MappingService是Mapper的薄包装层，增加了不必要的间接性
- 方案: ViewModel直接实例化Mapper，无需DI注入

### 当前模式

**直接Mapper实例化 (唯一推荐模式)**
```csharp
public class XXXMasterDetailViewModel
{
    // 直接实例化，无需DI
    private readonly XXXMapper _mapper = new();

    // 加载时
    var item = _mapper.ToItem(dto);

    // 保存时
    var inputDto = _mapper.ToInputDto(item);
}
```

### 各模块 Mapper 位置

| 模块 | Mapper 类 | 位置 |
|------|-----------|------|
| Herbs | HerbMapper | `Mappers/HerbMapper.cs` |
| Formula | FormulaMapper, FormulaDetailModelMapper | `Mappers/` |
| MedicalCase | MedicalCaseDetailModelMapper | `Mappers/` |
| Patients | PatientMapper | `Mappers/PatientMapper.cs` |
| Users | UserMapper | `Mappers/UserMapper.cs` |

### 已废弃的 FromDto/ToDto 方法

所有 Item 类中的静态 `FromDto()` 和实例 `ToDto()` 方法已标记 `[Obsolete]`：
- 请使用对应模块的 `XXXMapper.ToItem()` / `ToDto()` / `ToInputDto()` 替代
- 这些方法将在后续版本移除

### Mapperly + CommunityToolkit.Mvvm 源生成器兼容性

**重要**: Mapperly源生成器与CommunityToolkit.Mvvm的`[ObservableProperty]`存在编译顺序冲突。

**问题**: Mapperly在编译时验证属性存在性，但`[ObservableProperty]`生成的属性尚未生成，导致RMG005/RMG006错误。

**解决方案**: 对于源生成属性，使用`[MapperIgnore*]`忽略，在包装方法中手动映射：

```csharp
// 错误模式（编译失败）
[MapProperty(nameof(Dto.CaseStatus), "CaseStatus")]
public partial Item ToItemCore(Dto dto);

// 正确模式
[MapperIgnoreTarget("CaseStatus")]  // 字符串字面量
[MapperIgnoreSource(nameof(Dto.CaseStatus))]
public partial Item ToItemCore(Dto dto);

public Item ToItem(Dto dto)
{
    var item = ToItemCore(dto);
    item.CaseStatus = dto.CaseStatus;  // 手动映射
    return item;
}
```

**详细说明**: 参见 `MedicalCase/CLAUDE.md` 的"Mapperly与CommunityToolkit.Mvvm源生成器兼容性"章节

---

## XAML 绑定最佳实践 (OpenSpec: fix-elementname-binding-architecture)

### WPF NameScope 机制

**关键概念**: WPF 的 `ContentPresenter` 会创建独立的 NameScope，导致其内部的 `ElementName` 绑定无法解析父级控件。

**失败场景**:
```xml
<UserControl x:Name="Root">  <!-- NameScope #1 -->
  <MasterDetailLayout>
    <MasterDetailLayout.DetailContent>
      <!-- ContentPresenter 创建 NameScope #2 -->
      <SomeControl Prop="{Binding X, ElementName=Root}"/>  <!-- 失败! Root 在 NameScope #1 中 -->
    </MasterDetailLayout.DetailContent>
  </MasterDetailLayout>
</UserControl>
```

### 三种绑定模式使用场景

| 模式 | 适用场景 | 示例 |
|------|----------|------|
| **DataContext 绑定** | 绑定 ViewModel 属性（推荐默认模式） | `{Binding PropertyName}` |
| **ElementName 绑定** | 同一 NameScope 内控件间绑定 | `{Binding Width, ElementName=OtherControl}` |
| **RelativeSource 绑定** | 需要向上查找祖先元素 | `{Binding DataContext.Prop, RelativeSource={RelativeSource AncestorType=UserControl}}` |

### MasterDetailLayout 内的正确绑定模式

**ContentPresenter 内容区域** (MasterContent/DetailContent/EmptyContent):
- ✅ **使用 DataContext 绑定**: `{Binding ViewModel.Property}`
- ❌ **禁止 ElementName=Root 绑定**: 因 NameScope 隔离会失败

**控件根级别属性**:
- ✅ **使用 ElementName 绑定**: 同一 NameScope，可正常工作
- 示例: SidebarControl 使用 `ElementName=Root` 绑定自身 DependencyProperty

### DataContext 透传模式

**原理**: 子控件自动继承父控件的 DataContext，无需显式传递。

**实现**:
```xml
<!-- PatientSelectionView.xaml -->
<!-- DataContext 由 Prism 自动注入 PatientSelectionViewModel -->
<patientControls:PatientSelectionControl
    Grid.Row="1"
    Margin="20"
    PatientDoubleClicked="..."/>
<!-- 无需显式绑定属性，控件内部直接绑定 DataContext -->
```

```xml
<!-- PatientSelectionControl.xaml 内部 -->
<DataGrid ItemsSource="{Binding Patients}"/>  <!-- 直接绑定 ViewModel 属性 -->
```

### 参考实现

- **正确模式**: `PatientMasterDetailControl.xaml` - 直接 DataContext 绑定
- **正确模式**: `SidebarControl.xaml` - 根级 ElementName 绑定（不跨 ContentPresenter）
- **已修复**: `PatientSelectionControl.xaml` - 原 ElementName=Root 已改为 DataContext 绑定

---

## 代码文件结构

```
LYBT.Desktop.Infrastructure/
├── Behaviors/
│   └── DataGridSelectionBehavior.cs     # DataGrid多选行为，提供checkbox列和SelectedItems同步
├── Commands/
│   └── ApplicationCommands.cs           # IApplicationCommands接口 + 实现，全局CompositeCommand
├── Configuration/
│   ├── ClinicSettings.cs                # 诊所配置POCO (Name/Address/Phone/Department)
│   └── ConfigurationExtensions.cs       # IServiceCollection扩展 AddInfrastructureConfiguration
├── Constants/
│   ├── RegionNames.cs                   # Prism Region名称常量 (ContentRegion/SidebarRegion等)
│   ├── SystemConstants.cs               # 系统常量 (版本/超时/密码策略/文件路径/错误码)
│   └── ViewNames.cs                     # 视图名称常量 (编译时类型安全的导航目标)
├── Controls/
│   ├── BadgeType.cs                     # 状态徽章类型枚举
│   ├── CardReaderStatusControl.xaml/.cs  # 读卡器状态显示控件
│   ├── DataGridToolbar.xaml/.cs          # DataGrid工具栏 (搜索/筛选/操作按钮)
│   ├── DetailToolbar.xaml/.cs            # 详情面板工具栏 (编辑/保存/取消/删除)
│   ├── EmptyState.xaml/.cs              # 空状态提示控件 (无数据时显示)
│   ├── GlobalStatusBar.xaml/.cs          # 全局状态栏 (API状态/用户信息/时间)
│   ├── InfoCard.xaml/.cs                # 信息卡片控件 (标题+内容的展示卡片)
│   ├── LoadingOverlay.xaml/.cs          # 加载遮罩层 (全屏loading动画)
│   ├── MasterDetailControlBase.cs       # MasterDetail控件非泛型基类 (DI解析ViewModel)
│   ├── MasterDetailLayout.xaml/.cs      # 主从布局控件 (左列表+右详情)
│   ├── PatientCardDisplayMode.cs        # 患者卡片显示模式枚举
│   ├── PatientDisplayModel.cs           # 患者展示模型 (DisplayModel模式)
│   ├── PatientInfoCardControl.xaml/.cs  # 患者信息卡片 (头像+姓名+年龄+性别)
│   ├── PatientSearchControl.xaml/.cs    # 患者搜索控件 (搜索框+结果列表)
│   ├── PendingQueueControl.xaml/.cs     # 候诊队列控件
│   ├── SearchBox.xaml/.cs               # 通用搜索框控件 (防抖搜索)
│   ├── SidebarControl.xaml/.cs          # 侧边栏导航控件
│   ├── StatusBadge.xaml/.cs             # 状态徽章控件 (彩色标签)
│   ├── UnifiedManagementTable.xaml/.cs  # 统一管理表格 (DataGrid封装)
│   ├── UnifiedManagementToolBar.xaml/.cs # 统一管理工具栏 (新建/删除/导入/导出)
│   ├── UnifiedPaginationBar.xaml/.cs    # 统一分页栏 (页码/翻页/每页条数)
│   ├── FormulaView/
│   │   └── FormulaViewControl.xaml/.cs  # 验方查看控件 (只读展示验方详情)
│   ├── HerbItem/
│   │   ├── HerbItemChangedEventArgs.cs  # 药材项变更事件参数
│   │   ├── HerbItemControl.xaml/.cs     # 单个药材项编辑控件
│   │   └── HerbItemControlViewModel.cs  # 药材项控件ViewModel
│   └── HerbList/
│       ├── HerbListChangedEventArgs.cs  # 药材列表变更事件参数
│       ├── HerbListControl.xaml/.cs     # 药材列表编辑控件 (增删改药材项)
│       └── HerbListControlViewModel.cs  # 药材列表控件ViewModel
├── Converters/
│   ├── ConverterInstances.cs            # Cvt 静态类，x:Static引用所有转换器实例
│   ├── Converters.xaml                  # 转换器资源字典 (XAML Key定义)
│   ├── ApiHealthStatusToColorConverter.cs   # ApiHealthStatus -> Brush
│   ├── ApiHealthStatusToTextConverter.cs    # ApiHealthStatus -> 中文文本
│   ├── BooleanToVisibilityConverter.cs      # bool -> Visibility
│   ├── BoolToBrushConverter.cs              # bool -> Brush (可配TrueBrush/FalseBrush)
│   ├── BoolToDoubleConverter.cs             # bool -> double (可配TrueValue/FalseValue)
│   ├── BoolToOpacityConverter.cs            # bool -> 透明度 (true=1.0, false=0.5)
│   ├── BoolToStringConverter.cs             # bool -> 字符串 (可配TrueText/FalseText)
│   ├── DecocteMethodToVisibilityConverter.cs # DecocteMethod -> Visibility (特殊煎法)
│   ├── EnumDescriptionConverter.cs          # Enum -> [Description]属性值
│   ├── FirstCharacterConverter.cs           # 字符串 -> 首字符
│   ├── InverseBooleanConverter.cs           # bool 取反
│   ├── InverseBooleanToVisibilityConverter.cs # !bool -> Visibility
│   ├── InverseNullToVisibilityConverter.cs  # null=Visible, 非null=Collapsed
│   ├── NullToVisibilityConverter.cs         # null=Collapsed, 非null=Visible
│   ├── PatientCardDisplayModeToVisibilityConverter.cs # PatientCardDisplayMode -> Visibility
│   ├── StatusToColorConverter.cs            # 状态值 -> Color
│   ├── StringToVisibilityConverter.cs       # 空字符串=Collapsed, 非空=Visible
│   └── ZeroToVisibilityConverter.cs         # 0=Visible, 非0=Collapsed (空状态提示)
├── DependencyInjection/
│   └── ViewModelServicesExtensions.cs   # Prism DI注册扩展
├── Events/
│   ├── CaseEvents.cs                    # 医案事件 (ConsultationCompleted/PrescriptionCompleted/WorkspaceChanged)
│   ├── EventSubscriptionManager.cs      # 事件订阅生命周期管理器 (自动Dispose取消订阅)
│   ├── PatientEvents.cs                 # 患者事件 (Created/Updated/Selected)
│   └── PatientSelectedPayload.cs        # 患者选择事件载荷 (包含完整患者信息)
├── Helpers/
│   └── BindingProxy.cs                  # Freezable绑定代理 (解决非可视化树元素绑定)
├── Http/
│   ├── LoggingHttpHandler.cs            # HTTP请求/响应日志Handler (分布式追踪)
│   ├── ProblemDetailsParser.cs          # RFC 7807 ProblemDetails解析器
│   └── ProblemDetailsResponse.cs        # ProblemDetails响应模型
├── Interfaces/
│   ├── IClinicSettingsService.cs        # 诊所配置服务接口
│   └── IMainWindowServicesFacade.cs     # 主窗口服务门面接口
├── Logging/
│   └── DesktopSerilogConfiguration.cs   # 客户端Serilog日志配置
├── Models/
│   ├── DuplicateDosageStrategy.cs       # 重复药材剂量合并策略枚举 + 扩展方法
│   ├── Options/
│   │   ├── DisplayOptions.cs            # record: 控件显示配置 (IsCompactMode/ShowHeader/ShowFooter)
│   │   └── PaginationOptions.cs         # record: 分页配置 (DefaultPageSize/PageSizeOptions)
│   └── State/
│       ├── LoadingState.cs              # 可复用加载状态 (IsLoading/LoadingMessage)
│       ├── PaginationState.cs           # 可复用分页状态 (CurrentPage/PageSize/TotalCount)
│       └── SearchState.cs               # 可复用搜索状态 (SearchText/IsSearching)
├── Repositories/
│   └── RepositoryBase.cs                # Client端Repository泛型基类
├── Roles/
│   ├── RoleDefinitionBase.cs            # 角色定义抽象基类 (共享BaseModules)
│   ├── RoleRegistry.cs                  # 角色注册表实现 (管理所有IRoleDefinition)
│   └── Definitions/
│       ├── AdminRoleDefinition.cs       # 管理员角色 (Admin主页+全模块)
│       ├── DoctorRoleDefinition.cs      # 医生角色 (Clinical主页+诊疗模块)
│       ├── ReceptionistRoleDefinition.cs # 前台角色
│       └── SuperAdminRoleDefinition.cs  # 超级管理员角色
├── Security/
│   └── SensitiveInfoFilter.cs           # 敏感信息过滤器 (正则匹配+脱敏)
├── Services/
│   ├── ActiveConsultationService.cs     # 活跃医案跟踪服务
│   ├── ApplicationTickService.cs        # 统一定时任务调度 (1秒Tick)
│   ├── AsyncExecutor.cs                 # 异步执行服务 (安全执行/重试/超时/UI线程)
│   ├── ClinicSettingsService.cs         # 诊所配置服务 (读appsettings.json)
│   ├── CommonDialogService.cs           # 通用对话框服务 (消息框/文件对话框/四选项)
│   ├── DetailEditorService.cs           # 泛型详情编辑服务 (编辑模式/新建/取消/保存)
│   ├── DialogManager.cs                 # 对话框管理服务 (Prism DialogService封装)
│   ├── ErrorHandler.cs                  # 错误处理服务 (属性验证/错误集合管理)
│   ├── ListViewServices.cs              # 列表视图服务组合 (Loading+Pagination+Search+Selection+Error)
│   ├── LoadingStateManager.cs           # 加载状态管理 (引用计数/嵌套加载)
│   ├── MainWindowServicesFacade.cs      # 主窗口服务门面
│   ├── MasterDetailServices.cs          # MasterDetail视图服务组合
│   ├── PaginationService.cs             # 分页服务 (翻页/PageSize切换)
│   ├── PrescriptionSettingsService.cs   # 处方设置服务 (重复药材合并策略)
│   ├── SearchService.cs                 # 搜索服务 (防抖/取消)
│   ├── SelectionService.cs              # 泛型选择服务 (单选/多选/切换)
│   ├── SessionManager.cs               # 会话管理器 (当前用户/权限/登录状态)
│   ├── UserActivityTracker.cs           # 用户活动追踪 (不活跃超时检测)
│   ├── UserNotificationService.cs       # 用户通知服务 (MessageBox封装)
│   ├── ViewModelServices.cs             # ViewModel服务聚合 (Logger+EventAggregator+RegionManager等)
│   ├── Interfaces/
│   │   ├── IAsyncExecutor.cs            # 异步执行器接口
│   │   ├── IDetailEditorService.cs      # 详情编辑服务接口
│   │   ├── IDialogManager.cs            # 对话框管理接口
│   │   ├── IErrorHandler.cs             # 错误处理接口
│   │   ├── IListViewServices.cs         # 列表视图服务组合接口
│   │   ├── ILoadingStateManager.cs      # 加载状态管理接口
│   │   ├── IMasterDetailServices.cs     # MasterDetail服务组合接口
│   │   ├── IPaginationService.cs        # 分页服务接口
│   │   ├── ISearchService.cs            # 搜索服务接口
│   │   └── ISelectionService.cs         # 选择服务接口
│   └── Notifications/
│       ├── INotificationService.cs      # 通知服务接口 + NotificationType枚举 + 事件参数
│       └── NotificationService.cs       # 通知服务实现 (MessageBox + 事件驱动)
├── Themes/
│   ├── Controls.xaml                    # (在Shell中引用) 按钮/控件样式别名
│   ├── Converters.xaml                  # 转换器XAML资源定义
│   ├── HomePageStyles.xaml              # 主页样式
│   ├── MedicalCaseStyles.xaml           # 医案模块样式
│   ├── PreviewStyles.xaml               # 预览样式
│   ├── TCM.Theme.xaml                   # 中医五行配色主题
│   ├── Theme.Light.xaml                 # 主题入口 (合并Spacing.xaml)
│   ├── Typography.xaml                  # (在Shell中引用) 文本样式
│   ├── UnifiedComponents.xaml           # 主组件样式资源字典
│   ├── ValidationStyles.xaml            # 验证相关样式
│   └── DesignTokens/
│       └── Spacing.xaml                 # 间距设计令牌
├── ViewModels/
│   ├── MasterDetailViewModelBase.cs     # MasterDetail泛型ViewModel基类 (组合模式)
│   ├── UnfinishedCaseDialogViewModel.cs # 未完成医案四选项对话框ViewModel
│   └── Base/
│       └── HerbItemViewModelBase.cs     # 药材项ViewModel基类 (拼音码过滤/药材选择)
└── Views/
    ├── BaseDetailContainer.xaml/.cs     # 详情容器基类视图
    ├── BaseMasterDataListView.xaml/.cs  # 主数据列表基类视图
    └── UnfinishedCaseDialog.xaml/.cs    # 未完成医案对话框视图
```

### Repository 模式

原 `DataSources/Remote/` 目录下的 `RemoteFormulaDataSource`、`RemoteHerbDataSource`、`RemoteMedicalCaseDataSource`、`RemotePatientDataSource`、`RemoteUserDataSource` 类已移除。数据访问统一由 **Repository 模式** 处理：

- **Repository 基类**: `Repositories/RepositoryBase.cs` — 泛型基类，通过 Refit API 客户端访问数据
- **Repository 契约**: `LYBT.Desktop.Contracts/Repositories/` 下的 IXxxRepository 接口
- **双模式路由**: `SwitchingApiClient` 根据连接 URL 自动路由到远程服务器 API 或本地嵌入 LocalWebAPI

### Services 方法表

**SessionManager** (ISessionManager): 会话管理

| 方法/属性 | 说明 |
|-----------|------|
| `CurrentUser` | 当前登录用户 (同步获取避免WPF死锁) |
| `IsAuthenticated` / `IsLoggedIn` | 登录状态 |
| `SetSession(UserDetailDto, string, string?)` | 设置会话 (用户+Token) |
| `ClearSession()` | 清除会话 (触发SessionChanged事件) |
| `HasPermission(UserRole)` | 角色权限检查 |
| `IsAdmin()` | 是否管理员/超管 |

**ActiveConsultationService** (IActiveConsultationService): 活跃医案跟踪

| 方法/属性 | 说明 |
|-----------|------|
| `HasActiveConsultation` | 是否有活跃医案 |
| `ActiveMedicalCaseId` | 活跃医案ID |
| `Register(Guid, Func<Task<LeaveConsultationResult>>)` | 注册活跃医案和离开处理器 |
| `Unregister()` | 注销活跃医案 |
| `RequestLeaveAsync()` | 请求离开 (调用离开处理器) |

**AsyncExecutor** (IAsyncExecutor): 异步执行

| 方法 | 说明 |
|------|------|
| `ExecuteSafelyAsync(Func<Task>, Action<Exception>?)` | 安全执行 (捕获异常) |
| `ExecuteWithRetryAsync(Func<Task>, int, int, ...)` | 带重试执行 (递增延迟) |
| `ExecuteOnUIThread(Action)` | UI线程执行 (同步) |
| `ExecuteOnUIThreadAsync(Action)` | UI线程执行 (异步) |
| `ExecuteWithTimeoutAsync(Func<CancellationToken, Task>, TimeSpan)` | 带超时执行 |

**ErrorHandler** (IErrorHandler): 错误处理

| 方法 | 说明 |
|------|------|
| `HandleException(Exception, string?)` | 处理异常 (设置ErrorMessage+日志) |
| `SetError(string, string)` | 设置属性错误 |
| `SetErrors(string, IEnumerable<string>)` | 设置属性错误列表 |
| `ClearError(string)` / `ClearAllErrors()` | 清除错误 |
| `ValidateProperty(object?, string)` | 验证单个属性 |
| `ValidateAll(object)` | 验证全部属性 |

**DetailEditorService\<TDetail\>** (IDetailEditorService): 详情编辑

| 方法 | 说明 |
|------|------|
| `EnterEditMode()` | 进入编辑模式 |
| `CancelEdit()` | 取消编辑 (恢复原始值) |
| `ConfirmSaved()` | 确认保存 (更新原始值) |
| `CreateNew(Func<TDetail>)` | 创建新记录 |
| `LoadDetail(TDetail, Func<TDetail, TDetail>?)` | 加载详情 (支持克隆) |
| `MarkAsChanged()` | 标记有未保存更改 |
| `Clear()` | 清除当前详情 |

**PaginationService** (IPaginationService): 分页

| 方法 | 说明 |
|------|------|
| `GoToPage(int)` / `GoToFirstPage()` / ... | 翻页操作 |
| `Reset()` | 重置 (回到第1页) |
| `PageChanged` 事件 | 页码变更通知 |

**SearchService** (ISearchService): 搜索

| 方法 | 说明 |
|------|------|
| `ExecuteSearchAsync(Func<string, Task>)` | 防抖搜索 (默认300ms) |
| `ExecuteSearchImmediateAsync(Func<string, Task>)` | 立即搜索 |
| `ClearSearch()` / `CancelSearch()` | 清除/取消搜索 |

**SelectionService\<T\>** (ISelectionService): 选择

| 方法 | 说明 |
|------|------|
| `Select(T?)` | 单选 |
| `SelectMultiple(IEnumerable<T>)` | 多选 |
| `ToggleSelection(T)` | 切换选择 |
| `ClearSelection()` | 清除选择 |

**ApplicationTickService** (IApplicationTickService): 定时调度

| 方法 | 说明 |
|------|------|
| `Start()` / `Stop()` | 启动/停止1秒间隔定时器 |
| `Tick` 事件 | 每秒触发，提供TickCount |

**UserActivityTracker** (IUserActivityTracker + IUserActivityState): 用户活动

| 方法/属性 | 说明 |
|-----------|------|
| `StartTracking()` / `StopTracking()` | 启动/停止追踪 (监听键盘/鼠标事件) |
| `ResetActivity()` | 重置活动计时器 |
| `IsUserActive` / `TimeUntilInactive` | 活动状态查询 |
| `SessionExpired` 事件 | 不活跃超时时触发 |

**RepositoryBase\<TDetailDto, TListDto, TCreateDto, TUpdateDto, TApi\>**: Client端Repository泛型基类

| 方法 | 说明 |
|------|------|
| `GetByIdAsync(Guid)` | 按ID查询 |
| `GetPagedAsync(int, int, string?)` | 分页查询 |
| `CreateAsync(TCreateDto)` | 创建 |
| `UpdateAsync(TUpdateDto)` | 更新 |
| `DeleteAsync(Guid)` | 删除 |
| `SearchAsync(string)` | 搜索 |

子类需实现: `CallApiGetByIdAsync` / `CallApiGetPagedAsync` / `CallApiCreateAsync` / `CallApiUpdateAsync` / `CallApiDeleteAsync` / `GetIdFromUpdateDto`

### ViewModels 方法表

**MasterDetailViewModelBase\<TListItem, TDetail\>**: MasterDetail视图ViewModel基类

| 抽象方法 | 说明 |
|----------|------|
| `LoadListAsync()` | 加载列表数据 |
| `LoadDetailAsync(TListItem)` | 加载详情数据 |
| `CreateNewDetail()` | 创建新详情实例 |
| `SaveDetailAsync(TDetail)` | 保存详情 |
| `DeleteItemAsync(TListItem)` | 删除项 |

内置命令: Refresh / Search / ClearSearch / CreateNew / Edit / Save / Cancel / Delete + 分页命令

**HerbItemViewModelBase**: 药材项ViewModel基类

| 方法 | 说明 |
|------|------|
| `FilterHerbs()` | 拼音码+名称智能过滤药材 |
| `OnHerbSelected(HerbListDto)` | 药材选中回调 (虚方法) |
| `OnDosageChanged(int)` | 剂量变更回调 (虚方法) |

### Http 组件

**LoggingHttpHandler** (DelegatingHandler): HTTP日志

| 功能 | 说明 |
|------|------|
| 请求日志 | 记录 Method/URI/CorrelationId |
| 响应日志 | 记录 StatusCode/Duration/CorrelationId |
| 错误Body | 非成功响应记录脱敏后的响应体 |
| 分布式追踪 | 自动添加 traceparent header |

**ProblemDetailsParser**: RFC 7807 解析

| 方法 | 说明 |
|------|------|
| `ParseAsync(HttpResponseMessage)` | 从HTTP响应解析 |
| `Parse(string?, int?)` | 从JSON字符串解析 |
| `TryParseAsync(HttpResponseMessage)` | 尝试解析 (返回元组) |
| `FromException(Exception, string?)` | 从异常创建 |
| `CreateNetworkError(string?)` | 创建网络错误 |
| `CreateTimeoutError(string?)` | 创建超时错误 |

### DI 注册扩展

**ViewModelServicesExtensions**:

| 方法 | 说明 |
|------|------|
| `AddViewModelServices(IContainerRegistry)` | 注册全部ViewModel服务 (含泛型服务) |
| `AddListViewServices<T>(IContainerRegistry)` | 注册列表视图服务 |
| `AddMasterDetailServices<TListItem, TDetail>(IContainerRegistry)` | 注册MasterDetail视图服务 |

---

## 对象化数据绑定规范 (OpenSpec: unify-control-data-binding)

### 核心理念

用**聚合对象**替代**分散DependencyProperty**，将相关属性封装为有意义的业务对象。

**目标**: 将293个DependencyProperty减少至约100个（-66%）

### 四种标准对象类型

| 类型 | 用途 | 继承 | 特征 |
|------|------|------|------|
| **DisplayModel** | 只读展示数据 | 无（POCO） | 从DTO映射，包含计算属性用于格式化 |
| **EditModel** | 可编辑业务数据 | ObservableObject | 使用[ObservableProperty]，支持TwoWay绑定 |
| **ViewState** | UI状态管理 | ObservableObject | 可跨控件复用（分页、加载、搜索状态） |
| **ControlOptions** | 控件配置选项 | record类型 | 不可变，提供默认值 |

### 目录结构

```
Infrastructure/Models/
├── Display/                    # 通用DisplayModel
│   └── PatientDisplayModel.cs  # 已存在
├── State/                      # 通用ViewState（待创建）
│   ├── PaginationState.cs      # 分页状态
│   ├── LoadingState.cs         # 加载状态
│   └── SearchState.cs          # 搜索状态
└── Options/                    # 通用ControlOptions（待创建）
    ├── DisplayOptions.cs       # 显示选项
    └── PaginationOptions.cs    # 分页选项

Modules/<ModuleName>/Models/
├── Display/                    # 模块特定DisplayModel
│   └── XXXDisplayModel.cs
└── Edit/                       # 模块特定EditModel
    └── XXXEditModel.cs
```

### 代码示例

**DisplayModel（只读展示）**:
```csharp
public class PatientDisplayModel
{
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }

    // 计算属性用于UI格式化
    public string AgeDisplay => Age.HasValue ? $"{Age}岁" : "未知";
}
```

**EditModel（可编辑）**:
```csharp
public partial class ConsultationEditModel : ObservableObject
{
    [ObservableProperty] private string? _presentIllness;
    [ObservableProperty] private string? _tcmDiagnosis;

    public bool IsValid => !string.IsNullOrEmpty(TcmDiagnosis);
    public void Reset() { PresentIllness = null; TcmDiagnosis = null; }
}
```

**ViewState（UI状态）**:
```csharp
public partial class PaginationState : ObservableObject
{
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;

    public int TotalPages => PageSize > 0 ? (TotalCount + PageSize - 1) / PageSize : 1;
    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;
}
```

**ControlOptions（配置选项）**:
```csharp
public record DisplayOptions(
    bool IsCompactMode = false,
    bool ShowHeader = true,
    bool ShowFooter = true
);
```

### XAML绑定迁移

**Before（分散属性）**:
```xml
<local:MedicalCaseEditControl
    PatientName="{Binding PatientName}"
    PresentIllness="{Binding PresentIllness, Mode=TwoWay}"
    TcmDiagnosis="{Binding TcmDiagnosis, Mode=TwoWay}"/>
```

**After（对象化绑定）**:
```xml
<local:MedicalCaseEditControl
    Patient="{Binding Patient}"
    Consultation="{Binding Consultation}"/>

<!-- 控件内部绑定 -->
<TextBlock Text="{Binding Patient.Name}"/>
<TextBox Text="{Binding Consultation.PresentIllness, Mode=TwoWay}"/>
```

### 参考实现

- **WorkspaceState** (`MedicalCase/ViewModels/Components/WorkspaceState.cs`) - ViewState模式
- **PatientDisplayModel** (`Infrastructure/Models/Display/PatientDisplayModel.cs`) - DisplayModel模式
- **PatientViewState** (`Patients/ViewModels/Components/PatientViewState.cs`) - ViewState模式

### 详细设计文档

完整的架构设计和任务分解见 OpenSpec:
- `openspec/changes/unify-control-data-binding/proposal.md` - 完整提案
- `openspec/changes/unify-control-data-binding/design.md` - 详细设计
- `openspec/changes/unify-control-data-binding/tasks.md` - 任务分解

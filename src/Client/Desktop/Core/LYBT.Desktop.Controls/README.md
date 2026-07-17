# LYBT.Desktop.Controls

凌隐宝堂桌面端通用 UI 控件库——数据工具栏、分页、搜索、主从布局、中药列表等可复用 WPF 控件及配套 ViewModel / Model / Converter。

## 项目定位

| 维度 | 值 |
|------|------|
| 层级 | `src/Client/Desktop/Core` |
| 职责 | 为 Desktop 各 Module 提供与业务无关的通用 UI 控件、转换器、辅助类及中药列表等半业务控件 |
| 状态 | **Active** |

## 目录结构

```
LYBT.Desktop.Controls/
├── Controls/                     # WPF 自定义控件（XAML + code-behind）
│   ├── DataGridToolbar.xaml(.cs)
│   ├── UnifiedPaginationBar.xaml(.cs)
│   ├── SearchBox.xaml(.cs)
│   ├── MasterDetailLayout.xaml(.cs)
│   ├── MasterDetailControlBase.cs
│   ├── BaseDetailContainer.xaml(.cs)
│   ├── DetailToolbar.xaml(.cs)
│   ├── BreadcrumbBar.xaml(.cs)
│   ├── EmptyState.xaml(.cs)
│   ├── InfoCard.xaml(.cs)
│   ├── StatusBadge.xaml(.cs)
│   ├── LoadingOverlay.xaml(.cs)
│   ├── PatientInfoCardControl.xaml(.cs)
│   ├── PatientDisplayModel.cs
│   ├── PatientCardDisplayMode.cs
│   ├── BadgeType.cs
│   ├── Toast/
│   │   └── ToastControl.xaml(.cs)
│   ├── FormulaView/
│   │   └── FormulaViewControl.xaml(.cs)
│   ├── HerbList/
│   │   ├── HerbListControl.xaml(.cs)
│   │   ├── HerbListControlViewModel.cs
│   │   └── HerbListChangedEventArgs.cs
│   └── HerbItem/
│       ├── HerbItemControl.xaml(.cs)
│       ├── HerbItemControlViewModel.cs
│       └── HerbItemChangedEventArgs.cs
├── Converters/                   # IValueConverter 实现 + 静态 Cvt 门面
│   ├── ConverterInstances.cs     # static Cvt 类，集中暴露所有转换器实例
│   ├── Converters.xaml           # ResourceDictionary（备用，推荐用 x:Static）
│   └── *.cs                      # 各转换器实现
├── Helpers/                      # 布局 / 绑定辅助
│   ├── ResponsiveLayoutHelper.cs
│   └── BindingProxy.cs
├── Models/                       # 控件层共享枚举 / 模型
│   ├── NavigationItem.cs
│   ├── DuplicateDosageStrategy.cs
│   └── SuggestionType.cs
└── Themes/                       # XAML 主题资源
    ├── DataGridStyles.xaml
    ├── Icons.xaml
    ├── Spacing.xaml
    └── Surfaces.xaml
```

## 核心组件

### DataGridToolbar

**设计依据**：各列表页均有「新增 / 刷新 / 导出 / 批量删除 / 启用 / 禁用」需求，抽为统一工具栏避免重复 XAML。

| 属性 | 类型 | 绑定方向 | 说明 |
|------|------|----------|------|
| `CreateCommand` | `ICommand` | OneWay | 新增命令 |
| `RefreshCommand` | `ICommand` | OneWay | 刷新命令 |
| `ExportCommand` | `ICommand` | OneWay | 导出命令 |
| `BatchDeleteCommand` | `ICommand` | OneWay | 批量删除命令 |
| `EnableCommand` | `ICommand` | OneWay | 启用命令 |
| `DisableCommand` | `ICommand` | OneWay | 禁用命令 |

### UnifiedPaginationBar

**设计依据**：统一列表页分页体验，页码 / 每页条数双向绑定。

| 属性 | 类型 | 绑定方向 | 说明 |
|------|------|----------|------|
| `CurrentPage` | `int` | TwoWay | 当前页码 |
| `TotalPages` | `int` | OneWay | 总页数 |
| `PageSize` | `int` | TwoWay | 每页条数 |
| `PageSizes` | `IReadOnlyList<int>` | OneWay | 可选每页条数列表 |

### SearchBox

| 属性 | 类型 | 绑定方向 | 说明 |
|------|------|----------|------|
| `SearchText` | `string` | TwoWay | 搜索文本 |
| `Placeholder` | `string` | OneWay | 占位提示 |
| `ClearCommand` | `ICommand` | OneWay | 清除命令 |

### MasterDetailLayout

**设计依据**：主从布局（左列表 + 右详情）为系统核心交互模式，抽为控件统一管理空态、响应式切换。

| 属性 | 类型 | 绑定方向 | 说明 |
|------|------|----------|------|
| `MasterContent` | `object` | OneWay | 主区域内容 |
| `DetailContent` | `object` | OneWay | 详情区域内容 |
| `EmptyContent` | `object` | OneWay | 无选中时的空态内容 |

- 响应式布局由 `ResponsiveLayoutHelper` 驱动，根据窗口宽度自动切换 Small / Medium / Large / ExtraLarge 四档。

### MasterDetailControlBase

**设计依据**：所有主从页面 ViewModel 的抽象基类，统一列表选中 + 详情加载 + `IAsyncInitializable` 生命周期。

```csharp
public abstract class MasterDetailControlBase : ObservableObject, IAsyncInitializable
```

- 子类只需 override 列表加载 / 详情加载方法即可。

### BaseDetailContainer

**设计依据**：详情区容器统一 view/edit 模式切换、脏检查、面包屑导航、未保存离开确认。

| 功能 | 说明 |
|------|------|
| 模式切换 | View / Edit 双模式 |
| 脏检查 | `IsDirty` 追踪变更，离开时弹确认 |
| 面包屑 | 内嵌 `BreadcrumbBar`，`GoBackCommand` 带未保存确认 |
| 导航 | `GoBackCommand` 支持未保存变更确认弹窗 |

### DetailToolbar

| 属性 | 类型 | 绑定方向 | 说明 |
|------|------|----------|------|
| EditCommand | `ICommand` | OneWay | 进入编辑 |
| SaveCommand | `ICommand` | OneWay | 保存 |
| CancelCommand | `ICommand` | OneWay | 取消 |
| DeleteCommand | `ICommand` | OneWay | 删除 |

### BreadcrumbBar

- 解析 `A > B > C` 格式路径字符串，渲染为可点击面包屑。

### EmptyState

| 属性 | 类型 | 说明 |
|------|------|------|
| `Icon` | `Geometry` | 矢量图标 |
| `Title` | `string` | 标题 |
| `Subtitle` | `string` | 副标题 |
| `ActionContent` | `object` | 操作按钮区域 |

### InfoCard

| 属性 | 类型 | 说明 |
|------|------|------|
| `Title` | `string` | 卡片标题 |
| `Content` | `string` | 卡片内容 |

### StatusBadge

- 根据 `BadgeType` 枚举自动映射颜色：Success（绿）/ Warning（黄）/ Danger（红）/ Info（蓝）/ Neutral（灰）。

### LoadingOverlay

- 延迟 200ms 显示，避免快速操作时闪烁。

### ToastControl

**设计依据**：ADR-0003 Toast 自动隐藏规范。

- 支持自动隐藏，时长由 ADR-0003 定义。

### PatientInfoCardControl

**设计依据**：患者信息卡片支持三种显示密度，适配不同布局空间。

| 模式 | 说明 |
|------|------|
| `Full` | 完整信息（头像 + 姓名 + 年龄 + 性别 + 联系方式 + 地址） |
| `Compact` | 紧凑信息（姓名 + 年龄 + 性别） |
| `Minimal` | 最小信息（仅姓名） |

- 通过 `PatientCardDisplayMode` 枚举切换。

### FormulaViewControl

- 只读验方预览控件，用于详情页展示完整方剂信息。

### HerbListControl

**设计依据**：中药列表编辑是处方录入的核心交互，需支持拼音自动补全、重复检测、君臣佐使排序。

| 属性 | 类型 | 绑定方向 | 说明 |
|------|------|----------|------|
| `Columns` | `int` | OneWay | 网格列数（默认 4） |
| `AllHerbs` | `IReadOnlyList` | OneWay | 全量药材列表（供自动补全） |
| `DuplicateStrategy` | `DuplicateDosageStrategy` | OneWay | 重复药材剂量合并策略 |

- 重复检测：添加同名药材时触发 `DuplicateDosageStrategy` 合并。
- 拼音自动补全：输入拼音首字母过滤药材。
- 双向绑定：列表变更通过 `HerbListChangedEventArgs` 通知宿主。

### HerbItemControl

**设计依据**：单味药编辑行——自动补全 + 剂量 + 煎法。

| 功能 | 说明 |
|------|------|
| 自动补全 | 基于拼音过滤药材列表 |
| 剂量编辑 | 数值输入 + 单位 |
| 煎法选择 | 下拉选择 |

## ViewModels

### HerbListControlViewModel

**设计依据**：CommunityToolkit.Mvvm 风格，与 `HerbListControl` 配对。

| 方法 | 说明 |
|------|------|
| `LoadFromDto(dto)` | 从 DTO 加载列表 |
| `ToDto()` | 转换为 DTO |
| `AddHerbsAsync(items)` | 批量添加药材（含重复检测） |
| `SortByRole()` | 按君臣佐使排序 |
| `Validate()` | 验证列表完整性 |

### HerbItemControlViewModel

- 实现 `IHerbItemEditable` 接口。
- 支持拼音过滤的药材选择。
- 管理单味药的剂量、煎法状态。

## Models

| 类型 | 说明 |
|------|------|
| `NavigationItem` | 导航项：`Title` / `ViewName` / `IconKind` / `Group` / `Command` |
| `PatientDisplayModel` | 患者展示模型 |
| `PatientCardDisplayMode` | 枚举：`Full` / `Compact` / `Minimal` |
| `BadgeType` | 枚举：`Success` / `Warning` / `Danger` / `Info` / `Neutral` |
| `DuplicateDosageStrategy` | 重复剂量合并策略：`Max` / `Min` / `Sum` / `Average` / `First`，提供 `CalculateMergedDosage()` |
| `SuggestionType` | 自动补全建议类型 |

## Converters

### 静态门面 `Cvt`

**设计依据**：通过 `ConverterInstances.cs` 中的 `static class Cvt` 集中暴露所有转换器实例，XAML 中用 `{x:Static converters:Cvt.BoolToVis}` 引用，避免 ResourceDictionary 查找开销。

| 静态属性 | 转换器类 | 说明 |
|----------|----------|------|
| `BoolToVis` | `BooleanToVisibilityConverter` | bool → Visibility |
| `InverseBoolToVis` | `InverseBooleanToVisibilityConverter` | !bool → Visibility |
| `InverseBool` | `InverseBooleanConverter` | !bool |
| `NullToVis` | `NullToVisibilityConverter` | null → Collapsed |
| `StringToVis` | `StringToVisibilityConverter` | 空字符串 → Collapsed |
| `ZeroToVis` | `ZeroToVisibilityConverter` | 0 → Collapsed |
| `EnumDesc` | `EnumDescriptionConverter` | 枚举 → Description 特性文本 |
| `ApiStatusToColor` | `ApiHealthStatusToColorConverter` | API 健康状态 → Brush |
| `ApiStatusToText` | `ApiHealthStatusToTextConverter` | API 健康状态 → 文本 |
| `TimestampFormat` | `FirstCharacterConverter` | 首字符提取 |

> 完整列表以 `Cvt` 类实际导出为准，上表为常用项。`Converters.xaml` ResourceDictionary 仍保留作为备用注入方式。

## Helpers

### ResponsiveLayoutHelper

**设计依据**：MasterDetailLayout 响应式断点管理。

| 断点 | 宽度范围 | 行为 |
|------|----------|------|
| Small | < 768px | 主从堆叠 |
| Medium | 768–1199px | 主从并排（窄主区域） |
| Large | 1200–1599px | 主从并排（宽主区域） |
| ExtraLarge | >= 1600px | 主从并排（最大主区域） |

### BindingProxy

**设计依据**：WPF DataGridColumn 不在可视树中，无法直接 `DataContext` 绑定。`BindingProxy` 继承 `Freezable`，通过 `DataContext` 桥接实现列头绑定 ViewModel 属性。

## 事件

### HerbListChangedEventArgs

| 属性 | 类型 | 说明 |
|------|------|------|
| `ChangeType` | `HerbListChangeType` | 变更类型 |

| ChangeType 值 | 说明 |
|---------------|------|
| `ItemAdded` | 新增药材 |
| `ItemRemoved` | 移除药材 |
| `ItemModified` | 修改药材 |
| `Cleared` | 清空列表 |
| `Loaded` | 批量加载完成 |
| `Moved` | 位置移动 |
| `BatchImported` | 批量导入 |
| `Sorted` | 排序 |

### HerbItemChangedEventArgs

| 属性 | 类型 | 说明 |
|------|------|------|
| `ChangeType` | `HerbItemChangeType` | 变更类型 |

| ChangeType 值 | 说明 |
|---------------|------|
| `HerbSelected` | 药材选中变更 |
| `DosageChanged` | 剂量变更 |
| `DecocteMethodChanged` | 煎法变更 |

## 依赖关系

```
LYBT.Desktop.Controls
├── CommunityToolkit.Mvvm        # MVVM 源生成器 ([ObservableProperty] / [RelayCommand])
├── MaterialDesignThemes         # 图标 (PackIconKind)
├── MDIX (MaterialDesignInXAML)  # 内置控件样式
└── .NET 8 WPF                   # 框架
```

被以下项目引用：
- `LYBT.Desktop.Modules.*` — 各业务模块
- `LYBT.Desktop.Shell` — Shell 启动项

## 设计决策

1. **静态 Cvt 门面 vs ResourceDictionary**：`{x:Static converters:Cvt.BoolToVis}` 在编译期解析，避免运行时 ResourceDictionary 查找，性能更优且无拼写错误风险。`Converters.xaml` 保留作为兼容备选。

2. **MasterDetailControlBase 抽象基类**：所有主从页面共享选中管理 + 异步初始化生命周期，避免各模块重复实现。子类 override 核心加载方法即可。

3. **HerbListControl 内置 ViewModel**：中药列表逻辑复杂（重复检测、拼音补全、君臣佐使排序），ViewModel 与控件同目录管理，降低模块层负担。

4. **BindingProxy 解决 DataGridColumn 绑定**：WPF DataGridColumn 不继承 FrameworkElement，无法直接绑定 DataContext。使用 Freezable 桥接是社区标准方案。

5. **LoadingOverlay 200ms 延迟**：避免快速操作（如本地 DB 查询 < 200ms）时出现闪烁，提升视觉体验。

6. **DuplicateDosageStrategy 策略枚举**：重复药材剂量合并有多种合理策略（取大 / 取小 / 求和 / 取均 / 取首），封装为枚举 + `CalculateMergedDosage()` 使策略可配置。

## 已知陷阱

1. **HerbListControl 的 `AllHerbs` 必须在控件初始化前设置**：拼音自动补全依赖此列表，延迟设置会导致首次输入无补全建议。

2. **BindingProxy 必须冻结（Freeze）**：未冻结的 Freezable 在跨线程场景下会抛异常。确保在 XAML 或 code-behind 中调用 `Freeze()`。

3. **ToastControl 自动隐藏时长**：遵循 ADR-0003 规范，不要在 Module 层自行设定不同延时，否则用户在同一应用中看到不一致的 Toast 行为。

4. **Converter 使用 `{x:Static}` 语法**：不要用 `{StaticResource}` 引用 `Converters.xaml` 中的转换器——前者编译期检查，后者运行时查找且可能因 ResourceDictionary 加载顺序失败。

5. **MasterDetailLayout 响应式断点依赖窗口实际宽度**：在单元测试或无窗口宿主中，`ResponsiveLayoutHelper` 可能返回默认值。测试时需 mock 或注入窗口尺寸。

6. **BaseDetailContainer 的脏检查依赖 `IsDirty` 属性**：子类必须在数据变更时正确设置 `IsDirty = true`，否则 `GoBackCommand` 的未保存确认弹窗不会触发。

7. **HerbItemControl 拼音自动补全区分多音字**：多音字可能匹配到意外的药材，业务层应使用 `DuplicateStrategy` 做二次校验。

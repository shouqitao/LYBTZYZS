# 技术设计: 药材编辑控件重构

## Context

### 背景
药材编辑是处方模块的核心功能，当前实现存在以下技术债务：

1. **职责分散** - 编辑逻辑分布在6+文件中，共约1400行代码
2. **过度协调** - PrescriptionPanelViewModel(646行)需协调5个Handler
3. **复用受限** - 方剂模块无法直接复用药材编辑逻辑
4. **测试困难** - 需要Mock多个依赖才能测试

### 当前代码分布

```
PrescriptionPanelViewModel (646行) - 协调器
├── PrescriptionItemHandler (308行) - 药材项CRUD
├── PrescriptionCalculator (~80行) - 价格计算
├── PrescriptionValidator (~120行) - 校验逻辑
├── PrescriptionImportHandler (~150行) - 导入处理
└── HerbListEditor + HerbCardControl - UI渲染
```

### 约束
- 必须保持现有UI交互行为不变
- 必须兼容Prism MVVM框架
- 控件集中定义在Infrastructure/Controls
- 使用事件通知模式（项目统一模式）

## Goals / Non-Goals

### Goals
1. **两层控件架构** - HerbItemControl(单药材) + HerbListControl(药材列表)
2. **控件集中定义** - 所有控件代码在Infrastructure/Controls
3. **高内聚输出** - 控件输出完整药材列表，调用方直接对象赋值
4. **事件通知模式** - 控件通过事件通知外部变更
5. **支持复用** - 处方模块和方剂模块共用

### Non-Goals
- 不改变UI视觉效果
- 不改变业务逻辑
- 不引入新的外部依赖

## Decisions

### Decision 1: 两层控件架构

```
┌─────────────────────────────────────────────────────────────┐
│                     HerbListControl                          │
│  (药材列表控件 - 组成list + 查重)                            │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  HerbListControlViewModel (内部)                     │    │
│  │  - ObservableCollection<HerbItemVM>                 │    │
│  │  - 重复检测与处理                                    │    │
│  │  - 空槽位管理(1个空槽)                              │    │
│  │  - 紧凑列表(删除后前靠)                             │    │
│  │  - 拖拽排序                                         │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐         │
│  │HerbItemControl│ │HerbItemControl│ │HerbItemControl│  ...   │
│  │  (药材1)     │ │  (药材2)     │ │  (药材3)     │         │
│  └──────────────┘ └──────────────┘ └──────────────┘         │
│                                                              │
│  事件: HerbListChanged                                       │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                     HerbItemControl                          │
│  (单药材控件 - 快速检索药材)                                 │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  HerbItemControlViewModel (内部)                     │    │
│  │  - 药材选择与拼音码自动补全                          │    │
│  │  - 剂量输入与校验                                    │    │
│  │  - 煎法选择                                         │    │
│  │  - 自动复制单位、单价                               │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  事件: ItemChanged                                           │
└─────────────────────────────────────────────────────────────┘
```

**原因**:
- 两个控件都有业务逻辑，采用MVVM模式更清晰
- 内部ViewModel封装逻辑，通过DependencyProperty对外交互
- 符合WPF/Prism最佳实践

### Decision 2: HerbItemControl设计

基于现有HerbCardControl重命名并完善，采用MVVM模式。

**文件结构**:
```
Infrastructure/Controls/HerbItem/
├── HerbItemControl.xaml              # 控件UI
├── HerbItemControl.xaml.cs           # Code-Behind (简洁)
├── HerbItemControlViewModel.cs       # 内部ViewModel
└── HerbItemChangedEventArgs.cs       # 事件参数
```

**控件接口**:
```csharp
public partial class HerbItemControl : UserControl
{
    #region 输入属性 (DependencyProperty)

    /// <summary>药材库数据 - 用于自动补全</summary>
    public ObservableCollection<HerbListDto> AllHerbs { get; set; }

    #endregion

    #region 输出属性 (只读DependencyProperty)

    /// <summary>药材ID</summary>
    public Guid HerbId { get; }

    /// <summary>药材名称</summary>
    public string HerbName { get; }

    /// <summary>剂量(克)</summary>
    public int Dosage { get; }

    /// <summary>单位(从药材库自动复制)</summary>
    public string Unit { get; }

    /// <summary>单价(从药材库自动复制，不显示)</summary>
    public decimal UnitPrice { get; }

    /// <summary>煎法</summary>
    public DecocteMethod DecocteMethod { get; }

    /// <summary>剂量是否有效</summary>
    public bool IsDosageValid { get; }

    /// <summary>是否为空行(未选药材)</summary>
    public bool IsEmpty { get; }

    #endregion

    #region 事件

    /// <summary>药材项数据变更事件</summary>
    public event EventHandler<HerbItemChangedEventArgs>? ItemChanged;

    /// <summary>请求删除事件</summary>
    public event EventHandler? DeleteRequested;

    /// <summary>请求跳转到下一项(Enter键)</summary>
    public event EventHandler? NextItemRequested;

    #endregion

    #region 公共方法

    /// <summary>从DTO加载数据</summary>
    public void LoadFromDto(HerbItemDto dto);

    /// <summary>导出为DTO</summary>
    public HerbItemDto ToDto();

    /// <summary>清空数据</summary>
    public void Clear();

    /// <summary>设置焦点到药材名称输入框</summary>
    public void FocusHerbName();

    #endregion
}
```

**内部ViewModel**:
```csharp
/// <summary>
/// HerbItemControl内部ViewModel
/// 封装单个药材的编辑逻辑
/// </summary>
internal class HerbItemControlViewModel : BindableBase
{
    #region 属性

    /// <summary>药材ID</summary>
    public Guid HerbId { get; set; }

    /// <summary>药材名称 - 支持拼音码过滤</summary>
    public string HerbName { get; set; }

    /// <summary>剂量</summary>
    public int Dosage { get; set; }

    /// <summary>单位(从药材库自动复制)</summary>
    public string Unit { get; set; }

    /// <summary>单价(从药材库自动复制)</summary>
    public decimal UnitPrice { get; }

    /// <summary>煎法</summary>
    public DecocteMethod DecocteMethod { get; set; }

    /// <summary>过滤后的药材建议列表</summary>
    public ObservableCollection<HerbListDto> FilteredHerbs { get; }

    /// <summary>选中的药材</summary>
    public HerbListDto? SelectedHerb { get; set; }

    #endregion

    #region 校验

    /// <summary>剂量是否有效(1-500g)</summary>
    public bool IsDosageValid { get; }

    /// <summary>剂量校验消息</summary>
    public string DosageValidationMessage { get; }

    /// <summary>是否为空行</summary>
    public bool IsEmpty => HerbId == Guid.Empty;

    #endregion

    #region 方法

    /// <summary>过滤药材列表(拼音码匹配)</summary>
    private void FilterHerbs();

    /// <summary>选中药材后的处理 - 自动复制单位、单价</summary>
    private void OnHerbSelected(HerbListDto herb);

    /// <summary>尝试自动匹配药材(输入完全匹配时)</summary>
    private void TryAutoMatchHerb();

    /// <summary>剂量变更后的处理</summary>
    private void OnDosageChanged();

    #endregion
}
```

**键盘操作**:
| 按键 | 行为 |
|------|------|
| Enter | 选中建议项 / 跳转到剂量 / 生成新槽位 |
| Tab | 在字段间跳转 |

### Decision 3: HerbListControl设计

新建控件，采用MVVM模式。

**文件结构**:
```
Infrastructure/Controls/HerbList/
├── HerbListControl.xaml              # 控件UI
├── HerbListControl.xaml.cs           # Code-Behind (简洁)
├── HerbListControlViewModel.cs       # 内部ViewModel
└── HerbListChangedEventArgs.cs       # 列表变更事件参数
```

**控件接口**:
```csharp
public partial class HerbListControl : UserControl
{
    #region 输入属性

    /// <summary>药材库数据</summary>
    public ObservableCollection<HerbListDto> AllHerbs { get; set; }

    /// <summary>列数(默认4)</summary>
    public int Columns { get; set; } = 4;

    /// <summary>重复剂量取值策略(默认最大值)</summary>
    public DuplicateDosageStrategy DuplicateStrategy { get; set; } = DuplicateDosageStrategy.Max;

    #endregion

    #region 输出属性 (只读)

    /// <summary>药材列表 - 核心输出</summary>
    public IReadOnlyList<HerbItemDto> HerbList { get; }

    /// <summary>有效药材项数量(排除空行)</summary>
    public int ItemCount { get; }

    /// <summary>是否有重复药材</summary>
    public bool HasDuplicates { get; }

    /// <summary>是否通过校验</summary>
    public bool IsValid { get; }

    #endregion

    #region 事件

    /// <summary>药材列表变更事件</summary>
    public event EventHandler<HerbListChangedEventArgs>? HerbListChanged;

    #endregion

    #region 公共方法

    /// <summary>从DTO列表加载数据</summary>
    public void LoadFromDto(IEnumerable<HerbItemDto> items);

    /// <summary>批量添加药材(供外部导入调用)</summary>
    public void AddHerbs(IEnumerable<HerbItemDto> items);

    /// <summary>清空所有药材</summary>
    public void Clear();

    /// <summary>执行校验</summary>
    public bool Validate();

    #endregion
}
```

**重复剂量取值策略枚举**:
```csharp
/// <summary>
/// 重复药材剂量取值策略
/// </summary>
public enum DuplicateDosageStrategy
{
    /// <summary>取两个剂量中的较大值(默认)</summary>
    Max,
    /// <summary>取两个剂量中的较小值</summary>
    Min,
    /// <summary>两个剂量相加</summary>
    Sum,
    /// <summary>两个剂量的平均值</summary>
    Average,
    /// <summary>保留第一个添加的剂量</summary>
    First
}
```

**内部ViewModel**:
```csharp
/// <summary>
/// HerbListControl内部ViewModel
/// 封装列表管理、重复检测等业务逻辑
/// </summary>
internal class HerbListControlViewModel : BindableBase
{
    #region 集合

    /// <summary>药材项集合(包含空槽位)</summary>
    public ObservableCollection<HerbItemControlViewModel> Items { get; }

    #endregion

    #region 计算属性

    /// <summary>有效药材项数量(排除空行)</summary>
    public int ValidItemCount { get; }

    /// <summary>是否有重复药材</summary>
    public bool HasDuplicates { get; }

    #endregion

    #region 命令

    /// <summary>删除药材命令</summary>
    public DelegateCommand<HerbItemControlViewModel> DeleteItemCommand { get; }

    /// <summary>清空全部命令</summary>
    public DelegateCommand ClearAllCommand { get; }

    #endregion

    #region 空槽位管理

    /// <summary>
    /// 确保只有1个空槽位
    /// - 剂量输入后按回车 → 生成新空槽 + 光标跳转到新空槽的药材输入框
    /// </summary>
    public void EnsureSingleEmptySlot();

    #endregion

    #region 紧凑列表

    /// <summary>
    /// 紧凑列表 - 删除药材后，后面的自动往前靠
    /// </summary>
    public void Compact();

    #endregion

    #region 重复药材处理

    /// <summary>
    /// 检测重复药材
    /// </summary>
    public bool CheckDuplicate(Guid herbId);

    /// <summary>
    /// 单个添加时的重复处理
    /// - 返回true表示已存在，禁止添加
    /// - 内嵌提示"当前药材已经存在无法添加"
    /// </summary>
    public bool HandleSingleAddDuplicate(Guid herbId);

    /// <summary>
    /// 批量导入时的重复处理
    /// - 逐个弹窗提示"xx药材已经存在"
    /// - 医生确认一个再弹窗下一个
    /// - 根据DuplicateStrategy处理剂量
    /// </summary>
    public Task HandleBatchImportDuplicates(IEnumerable<HerbItemDto> items);

    /// <summary>
    /// 根据策略计算重复药材的最终剂量
    /// </summary>
    private int CalculateMergedDosage(int existingDosage, int newDosage, DuplicateDosageStrategy strategy);

    #endregion

    #region 拖拽排序

    /// <summary>
    /// 移动药材项位置
    /// </summary>
    public void MoveItem(int oldIndex, int newIndex);

    #endregion

    #region 收集输出

    /// <summary>收集有效药材项为DTO列表</summary>
    public IReadOnlyList<HerbItemDto> CollectHerbList();

    #endregion

    #region 事件处理

    /// <summary>子项变更处理</summary>
    private void OnItemChanged(HerbItemControlViewModel item);

    /// <summary>子项删除请求处理</summary>
    private void OnItemDeleteRequested(HerbItemControlViewModel item);

    /// <summary>子项跳转请求处理(Enter键生成新槽)</summary>
    private void OnItemNextRequested(HerbItemControlViewModel item);

    #endregion
}
```

### Decision 4: 数据结构设计

**HerbItemDto (输出DTO)**:
```csharp
/// <summary>
/// 药材项数据传输对象
/// 控件的标准输出格式，可直接用于保存
/// </summary>
public record HerbItemDto
{
    /// <summary>药材ID</summary>
    public Guid HerbId { get; init; }

    /// <summary>药材名称</summary>
    public required string HerbName { get; init; }

    /// <summary>剂量(克)</summary>
    public int Dosage { get; init; }

    /// <summary>单位</summary>
    public string Unit { get; init; } = "g";

    /// <summary>煎法</summary>
    public DecocteMethod DecocteMethod { get; init; } = DecocteMethod.Default;

    /// <summary>单价(元/克) - 从药材库复制，不显示</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>是否有效</summary>
    public bool IsValid => HerbId != Guid.Empty && Dosage > 0;
}
```

**事件参数**:
```csharp
/// <summary>药材项变更事件参数</summary>
public class HerbItemChangedEventArgs : EventArgs
{
    public HerbItemChangeType ChangeType { get; init; }
    public HerbItemDto? OldValue { get; init; }
    public HerbItemDto NewValue { get; init; }
}

public enum HerbItemChangeType
{
    HerbSelected,
    DosageChanged,
    DecocteMethodChanged,
    Cleared
}

/// <summary>药材列表变更事件参数</summary>
public class HerbListChangedEventArgs : EventArgs
{
    public HerbListChangeType ChangeType { get; init; }
    public IReadOnlyList<HerbItemDto> CurrentList { get; init; }
    public int ItemCount { get; init; }
}

public enum HerbListChangeType
{
    ItemAdded,
    ItemRemoved,
    ItemModified,
    ListCleared,
    ListLoaded,
    ItemMoved
}
```

### Decision 5: 需清理的碎片化代码

重构完成后，以下文件将被删除或合并：

| 文件 | 操作 | 说明 |
|-----|------|------|
| `Infrastructure/Controls/HerbCardControl.xaml` | RENAME | 重命名为HerbItemControl |
| `Infrastructure/Controls/HerbListEditor.xaml` | DELETE | 被HerbListControl替代 |
| `Prescriptions/Components/PrescriptionItemHandler.cs` | DELETE | 逻辑迁移到HerbListControl |
| `Prescriptions/Components/PrescriptionCalculator.cs` | KEEP | 价格计算保留在处方模块 |
| `Prescriptions/Components/PrescriptionImportHandler.cs` | DELETE | 外部处理导入对话框 |

### Decision 6: 控件与模块的交互模式

```
┌─────────────────────────────────────────────────────────────┐
│                  PrescriptionPanelViewModel                  │
│  (大幅简化后约200行)                                          │
│                                                              │
│  职责:                                                       │
│  - 提供导入按钮，调用控件AddHerbs方法                         │
│  - 处理HerbListChanged事件(更新脏状态)                        │
│  - 保存时直接使用HerbList                                     │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                  HerbListControl                     │    │
│  │  (高内聚控件 - 封装药材列表管理逻辑)                  │    │
│  │                                                      │    │
│  │  输入: AllHerbs, Columns, DuplicateStrategy         │    │
│  │  输出: HerbList, ItemCount, IsValid                 │    │
│  │  事件: HerbListChanged                              │    │
│  │  方法: AddHerbs (供外部导入调用)                     │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘

保存流程:
1. 用户点击保存
2. PrescriptionPanelVM调用 _herbListControl.Validate()
3. 校验通过后，直接使用 _herbListControl.HerbList
4. 构造 PrescriptionInputDto { Items = HerbList }
5. 调用 SaveHandler.SaveAsync()

导入流程:
1. 用户点击导入按钮(外部提供)
2. PrescriptionPanelVM显示选择对话框
3. 用户选择方剂/历史处方
4. 调用 _herbListControl.AddHerbs(herbs)
5. 控件内部处理重复药材(逐个弹窗确认)
```

### Decision 7: 校验分层

| 层级 | 位置 | 校验内容 | 触发时机 |
|-----|------|----------|----------|
| L1 | HerbItemControl | 剂量范围(1-500g) | 输入时 |
| L1 | HerbItemControl | 必填字段(HerbId) | 失焦时 |
| L2 | HerbListControl | 重复药材检测 | 列表变化时 |
| L2 | HerbListControl | 最小数量(>=1) | Validate()时 |
| L3 | PrescriptionPanelVM | 与诊断关联校验 | 保存前 |
| L4 | SaveHandler | 完整性最终校验 | API调用前 |

### Decision 8: 导入时药材信息同步策略

**问题背景**:
导入药材（经验方/历史处方）时存在数据不一致问题：

| 数据源 | 价格 | 名称 | 单位 | 问题 |
|--------|------|------|------|------|
| 实时输入 | 最新 | 最新 | 最新 | 无 |
| 经验方导入 | **无** | 可能过时 | 可能过时 | 经验方不保存价格 |
| 历史处方导入 | **过时** | 可能过时 | 可能过时 | 药材信息可能已修改 |

**示例场景**:
1. 药材"红枣"价格从0.5元/g调整为0.6元/g
2. 药材"红枣"名称修改为"大枣"（系统允许小幅修改）
3. 导入旧处方时，应使用最新的价格和名称

**解决方案**:

```
导入流程（AddItem方法）:
┌─────────────────────────────────────────────────────────────┐
│ 1. 接收 HerbItemDto (可能包含过时信息)                        │
│    - HerbId: 药材唯一标识 (不变)                              │
│    - HerbName: 可能是旧名称                                  │
│    - Unit: 可能是旧单位                                      │
│    - UnitPrice: 可能是旧价格或0                               │
│    - Dosage: 用户设置的剂量 (保留)                            │
│    - DecocteMethod: 用户设置的煎法 (保留)                     │
├─────────────────────────────────────────────────────────────┤
│ 2. 根据 HerbId 从 AllHerbs 查找药材库数据                     │
│    var herbInfo = AllHerbs.FirstOrDefault(h => h.Id == HerbId)│
├─────────────────────────────────────────────────────────────┤
│ 3. 如果找到，同步最新信息:                                    │
│    dto.HerbName = herbInfo.Name     // 最新名称               │
│    dto.Unit = herbInfo.Unit         // 最新单位               │
│    dto.UnitPrice = herbInfo.Price   // 最新价格               │
├─────────────────────────────────────────────────────────────┤
│ 4. 保留原始用户设置:                                          │
│    dto.Dosage      // 保持不变                                │
│    dto.DecocteMethod // 保持不变                              │
├─────────────────────────────────────────────────────────────┤
│ 5. 创建 ViewModel 并加载同步后的数据                          │
└─────────────────────────────────────────────────────────────┘
```

**设计决策**:

| 决策点 | 选择 | 理由 |
|--------|------|------|
| 同步时机 | `AddItem` (导入) | `LoadFromDto` 用于加载已保存处方，应保持历史记录 |
| 同步字段 | Name, Unit, Price | 这些是药材库管理的属性，应保持最新 |
| 保留字段 | Dosage, DecocteMethod | 这些是处方中的用户设置，应保留导入值 |
| 找不到药材 | 保持原值 | 容错处理，不影响导入流程 |

**实现位置**: `HerbListControlViewModel.AddItem()` 方法

## File Changes Summary

| 操作 | 文件 | 说明 |
|-----|------|------|
| RENAME | `Controls/HerbCardControl.xaml` → `Controls/HerbItem/HerbItemControl.xaml` | 重命名并增强 |
| CREATE | `Controls/HerbItem/HerbItemControlViewModel.cs` | 内部ViewModel |
| CREATE | `Controls/HerbItem/HerbItemChangedEventArgs.cs` | 事件参数 |
| CREATE | `Controls/HerbList/HerbListControl.xaml` | 药材列表控件UI |
| CREATE | `Controls/HerbList/HerbListControl.xaml.cs` | Code-Behind |
| CREATE | `Controls/HerbList/HerbListControlViewModel.cs` | 内部ViewModel |
| CREATE | `Controls/HerbList/HerbListChangedEventArgs.cs` | 事件参数 |
| CREATE | `Models/HerbItemDto.cs` | 药材项DTO |
| CREATE | `Models/DuplicateDosageStrategy.cs` | 剂量取值策略枚举 |
| DELETE | `Controls/HerbListEditor.xaml(.cs)` | 被HerbListControl替代 |
| DELETE | `PrescriptionItemHandler.cs` | 迁移到控件 |
| DELETE | `PrescriptionImportHandler.cs` | 外部处理 |
| MODIFY | `PrescriptionPanelViewModel.cs` | 大幅简化 |
| MODIFY | `PrescriptionEditorPanel.xaml` | 使用新控件 |

## Risks / Trade-offs

| 风险 | 概率 | 影响 | 缓解措施 |
|-----|------|------|---------|
| 重构范围较大 | 确定 | 中 | 分Phase实施，每Phase验证 |
| HerbCardControl重命名 | 低 | 低 | 全局搜索替换 |
| 控件内部状态管理 | 中 | 中 | 使用内部ViewModel封装 |
| 方剂模块影响 | 中 | 中 | Phase 4再处理方剂复用 |
| 拖拽排序实现复杂 | 中 | 中 | 可使用现有拖拽库 |

## Migration Plan

### Step 1: Phase 1 - 完善HerbItemControl (1天)
1. 基于现有HerbCardControl重命名为HerbItemControl
2. 完善拼音码快速检索逻辑
3. 完善自动匹配药材功能
4. 选择药材后自动复制单位、单价
5. 完善剂量校验逻辑
6. 添加ItemChanged事件
7. 键盘操作(Enter/Tab)

### Step 2: Phase 2 - 创建HerbListControl (1.5天)
1. 创建HerbListControl UserControl
2. 内部使用ItemsControl + HerbItemControl
3. 实现空槽位管理(1个空槽，回车生成新槽)
4. 实现紧凑列表(删除后自动前靠)
5. 实现重复药材检测(单个禁止，批量逐个弹窗)
6. 实现重复剂量取值策略(可配置)
7. 实现拖拽排序
8. 实现删除功能(单个+清空)
9. 实现AddHerbs批量导入方法
10. 实现Columns布局配置
11. 添加HerbListChanged事件

### Step 3: Phase 3 - 集成到处方模块 (1天)
1. 替换PrescriptionEditorPanel中的控件引用
2. 简化PrescriptionPanelViewModel移除Handler协调代码
3. 更新保存逻辑使用HerbList输出
4. 外部提供导入按钮，调用控件AddHerbs方法

### Step 4: Phase 4 - 扩展复用与清理 (1天)
1. 方剂模块复用HerbListControl
2. 删除冗余Handler文件
3. 删除旧的HerbListEditor控件
4. 编写单元测试

## Success Metrics

| 指标 | 当前 | 目标 |
|------|------|------|
| 药材编辑相关代码行数 | ~1400行 | ~600行 (-57%) |
| 碎片化文件数 | 6+个 | 2个(HerbItemControl+HerbListControl) |
| PrescriptionPanelVM行数 | 646行 | ~200行 |
| 模块间复用 | 无 | 处方+方剂共用 |
| Mock依赖数(测试) | 5个 | 1个 |

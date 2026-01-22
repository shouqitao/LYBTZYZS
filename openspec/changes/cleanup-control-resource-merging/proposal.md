# OpenSpec Proposal: cleanup-control-resource-merging

## 元数据

| 字段 | 值 |
|------|-----|
| 提案ID | cleanup-control-resource-merging |
| 状态 | Draft |
| 创建日期 | 2026-01-21 |
| 影响范围 | Desktop全部模块 |
| 优先级 | P0 - 阻塞性问题 |

---

## 1. 问题陈述

### 1.1 现象

用户在Master-Detail布局中点击列表项时，应用程序频繁崩溃。错误信息：

```
System.Windows.Data Error: 'DependencyProperty.UnsetValue'
```

此问题影响所有使用Master-Detail模式的模块：
- Patients（患者管理）
- Herbs（药材管理）
- Formula（方剂管理）
- MedicalCase（医案管理）
- Users（用户管理）

### 1.2 问题持续时间

> "这个问题已经持续很长时间没有解决了。" — 用户反馈

反复修复却无法根治，说明这是**架构设计问题**，而非单个Bug。

### 1.3 根因分析

#### WPF资源系统的约束

| 属性类型 | 是否DependencyProperty | 可用引用方式 |
|----------|----------------------|--------------|
| `Style` | 是 | StaticResource / DynamicResource |
| `Style.BasedOn` | **否** | **仅StaticResource** |
| `Binding.Converter` | **否** | **仅StaticResource** |
| `Background/Foreground` | 是 | StaticResource / DynamicResource |

**关键约束**：`Binding.Converter` 和 `Style.BasedOn` **必须**使用 `StaticResource`，因为它们不是 DependencyProperty。

#### ContentPresenter的NameScope隔离

```
UserControl (NameScope #1)
└── MasterDetailLayout
    └── ContentPresenter (创建 NameScope #2)
        └── DetailContent
            └── Converter={StaticResource XXX}  ← 查找失败！
```

当控件被加载到 `ContentPresenter` 中时：
1. `StaticResource` 在XAML解析时查找资源
2. 此时控件尚未完全加入可视化树
3. 资源字典查找路径断裂
4. 返回 `DependencyProperty.UnsetValue`
5. 应用崩溃

#### 代码库中的矛盾指导

**规则A（CLAUDE.md）**：
> "不要在控件级别合并 UnifiedComponents.xaml，避免资源重复加载"

**规则B（WPF约束）**：
> "Binding.Converter 必须使用 StaticResource，而 StaticResource 要求资源在解析时可用"

这两条规则**互相矛盾**，导致开发者无论怎么做都会出问题：
- 遵循规则A → Converter找不到资源 → 崩溃
- 遵循规则B → 每个控件都要合并资源 → 违反规则A，且治标不治本

---

## 2. 解决方案

### 2.1 方案选型

| 方案 | 描述 | 优点 | 缺点 |
|------|------|------|------|
| A. 每控件合并资源 | 每个控件都合并UnifiedComponents.xaml | 简单直接 | 资源重复加载，治标不治本 |
| B. 第三方主题库 | 引入MaterialDesignInXAML/MahApps.Metro | 成熟稳定 | 大规模重构，风格变化 |
| **C. x:Static静态实例** | 使用静态类提供转换器实例 | 彻底解决，无资源查找 | 需要全局迁移 |
| D. 自定义MarkupExtension | 创建SafeConverter扩展 | 优雅 | 实现复杂 |

**选择方案C**：使用 `x:Static` 引用静态转换器实例。

### 2.2 方案详解

#### 核心思路

绕过资源字典查找机制，直接引用静态实例：

```xml
<!-- Before（问题模式）-->
Converter={StaticResource BooleanToVisibilityConverter}

<!-- After（解决方案）-->
xmlns:converters="clr-namespace:LYBT.Desktop.Infrastructure.Converters;assembly=LYBT.Desktop.Infrastructure"
Converter={x:Static converters:Cvt.BoolToVis}
```

#### 为什么有效

`x:Static` 是编译时解析的标记扩展：
1. 直接访问CLR静态成员
2. 不依赖资源字典
3. 不受NameScope影响
4. 类型安全，编译时检查

#### 已创建的静态转换器类

文件：`Infrastructure/Converters/ConverterInstances.cs`

```csharp
public static class Cvt
{
    // Boolean Converters
    public static readonly IValueConverter BoolToVis = new BooleanToVisibilityConverter();
    public static readonly IValueConverter InverseBoolToVis = new InverseBooleanToVisibilityConverter();
    public static readonly IValueConverter InverseBool = new InverseBooleanConverter();

    // Visibility Converters
    public static readonly IValueConverter StringToVis = new StringToVisibilityConverter();
    public static readonly IValueConverter NullToVis = new NullToVisibilityConverter();

    // Enum/Status Converters
    public static readonly IValueConverter EnumDesc = new EnumDescriptionConverter();
    public static readonly IValueConverter StatusToColor = new StatusToColorConverter();

    // ... 更多转换器
}
```

---

## 3. 影响范围

### 3.1 需要修改的文件

#### Infrastructure Controls（核心控件）
- `MasterDetailLayout.xaml` - 已合并资源，需迁移
- `PatientSearchControl.xaml` - 已合并资源，需迁移
- `SidebarControl.xaml` - 已合并资源，需迁移
- `PendingQueueControl.xaml`
- `StatusBadge.xaml`
- `EmptyState.xaml`
- `InfoCard.xaml`
- `DataGridToolbar.xaml`
- `DetailToolbar.xaml`
- `SearchBox.xaml`
- `PatientInfoCardControl.xaml`

#### Module Controls（模块控件）
- `HerbMasterDetailControl.xaml`
- `HerbEditControl.xaml`
- `HerbViewControl.xaml`
- `FormulaMasterDetailControl.xaml`
- `FormulaEditControl.xaml`
- `FormulaViewControl.xaml`
- `PatientMasterDetailControl.xaml`
- `PatientEditControl.xaml`
- `PatientViewControl.xaml`
- `MedicalCaseMasterDetailControl.xaml`
- `MedicalCaseEditControl.xaml`
- `MedicalCaseViewControl.xaml`
- `UserMasterDetailControl.xaml`
- `UserEditControl.xaml`
- `UserViewControl.xaml`

#### Views（视图）
- `LoginWindow.xaml`
- `AdminHomeView.xaml`
- `ClinicalHomeView.xaml`
- `SystemSettingsView.xaml`
- 其他Views

### 3.2 预估工作量

| 类别 | 文件数 | 修改复杂度 |
|------|--------|------------|
| Infrastructure Controls | ~12 | 中等 |
| Module Controls | ~18 | 中等 |
| Views | ~10 | 低 |
| **总计** | **~40** | - |

---

## 4. 迁移策略

### 4.1 分阶段执行

**Phase 1：基础设施层**
1. 确认 `ConverterInstances.cs` 编译通过
2. 迁移 `MasterDetailLayout.xaml`（核心布局）
3. 迁移 `SidebarControl.xaml`
4. 验证基础功能

**Phase 2：模块控件层**
1. 按模块逐个迁移（Herbs → Formula → Patients → MedicalCase → Users）
2. 每个模块迁移后立即测试

**Phase 3：视图层**
1. 迁移所有Views
2. 移除不必要的资源合并
3. 全面回归测试

### 4.2 迁移检查清单

每个XAML文件迁移时：

- [ ] 添加 `xmlns:converters` 命名空间声明
- [ ] 替换所有 `{StaticResource XXXConverter}` 为 `{x:Static converters:Cvt.XXX}`
- [ ] 移除控件级别的 `UnifiedComponents.xaml` 合并（如果仅为了Converter）
- [ ] 编译验证
- [ ] 运行时测试（点击列表项）

---

## 5. 验收标准

### 5.1 功能验收

- [ ] 所有Master-Detail模块点击列表项不再崩溃
- [ ] Herbs模块：点击药材显示详情正常
- [ ] Formula模块：点击方剂显示详情正常
- [ ] Patients模块：点击患者显示详情正常
- [ ] MedicalCase模块：点击医案显示详情正常
- [ ] Users模块：点击用户显示详情正常

### 5.2 架构验收

- [ ] 所有Converter引用使用 `x:Static` 模式
- [ ] 无控件级别的重复资源合并（除非有其他必要原因）
- [ ] CLAUDE.md 资源引用规范更新
- [ ] 无 `DependencyProperty.UnsetValue` 相关错误

### 5.3 回归验收

- [ ] 侧边栏展开/收缩正常
- [ ] 主题颜色显示正常
- [ ] 所有转换器功能正常（Bool→Visibility、Enum→Description等）

---

## 6. 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 遗漏某些XAML文件 | 中 | 中 | 使用Grep搜索所有Converter引用 |
| 带参数的Converter无法静态化 | 低 | 低 | 为特定参数创建预配置实例 |
| 编译时间增加 | 低 | 低 | x:Static是编译时解析，影响极小 |

---

## 7. 时间线

| 阶段 | 预计时间 |
|------|----------|
| Phase 1: 基础设施层 | 1小时 |
| Phase 2: 模块控件层 | 2小时 |
| Phase 3: 视图层 | 1小时 |
| 测试与修复 | 1小时 |
| **总计** | **5小时** |

---

## 8. 决策记录

### ADR-1: 选择x:Static而非第三方主题库

**决策**：使用x:Static静态实例模式，而非引入第三方主题库。

**原因**：
1. 用户明确要求保持当前简单明了的风格
2. 当前主题符合中医传统风格
3. 第三方库会带来大规模重构
4. x:Static方案侵入性最小，彻底解决问题

### ADR-2: 转换器命名约定

**决策**：使用简短命名（如 `Cvt.BoolToVis`）而非完整名称。

**原因**：
1. XAML中频繁使用，简短命名减少冗余
2. 通过XML注释提供完整说明
3. 类名 `Cvt` 足够表意（Converter的缩写）

---

## 附录：转换器映射表

| 原StaticResource Key | 新x:Static引用 |
|---------------------|----------------|
| `BooleanToVisibilityConverter` | `Cvt.BoolToVis` |
| `InverseBooleanToVisibilityConverter` | `Cvt.InverseBoolToVis` |
| `InverseBooleanConverter` | `Cvt.InverseBool` |
| `BoolToBrushConverter` | `Cvt.BoolToBrush` |
| `BoolToDoubleConverter` | `Cvt.BoolToDouble` |
| `BoolToStringConverter` | `Cvt.BoolToString` |
| `BoolToOpacityConverter` | `Cvt.BoolToOpacity` |
| `StringToVisibilityConverter` | `Cvt.StringToVis` |
| `NullToVisibilityConverter` | `Cvt.NullToVis` |
| `InverseNullToVisibilityConverter` | `Cvt.InverseNullToVis` |
| `EnumDescriptionConverter` | `Cvt.EnumDesc` |
| `StatusToColorConverter` | `Cvt.StatusToColor` |
| `ApiHealthStatusToColorConverter` | `Cvt.ApiStatusToColor` |
| `ApiHealthStatusToTextConverter` | `Cvt.ApiStatusToText` |
| `FirstCharacterConverter` | `Cvt.FirstChar` |
| `DecocteMethodToVisibilityConverter` | `Cvt.DecocteMethodToVis` |
| `PatientCardDisplayModeToVisibilityConverter` | `Cvt.PatientCardModeToVis` |

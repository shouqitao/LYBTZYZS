# migrate-to-handycontrol

## Why

### 发现的问题

当前自定义资源架构存在根本性设计缺陷，导致WPF应用在运行时反复崩溃：

| 位置 | 问题类型 | 当前状态 | 期望状态 |
|------|----------|----------|----------|
| 控件级资源引用 | DependencyProperty.UnsetValue崩溃 | DynamicResource在逻辑树不完整时解析失败 | 资源始终可靠解析 |
| 资源字典架构 | 复杂性高 | 多层嵌套合并(50+ Color, 200+ Style)，维护困难 | 单一来源，简洁明了 |
| 主题系统 | 缺乏标准化 | 自定义命名规范，无法复用社区资源 | 基于HandyControl标准命名 |
| 样式复用 | 重复实现 | 每个控件重写基础样式 | 复用组件库预置样式 |

### 根因分析

1. **WPF资源解析时机问题**：`Style`定义中的`DynamicResource`在样式应用时解析，此时逻辑树可能不完整
2. **ContentPresenter NameScope隔离**：`ElementName`绑定无法跨越`ContentPresenter`边界
3. **资源字典合并顺序敏感**：`BasedOn`引用的样式必须在被引用之前定义
4. **维护成本高**：每次添加新样式都需要考虑复杂的依赖关系

### 影响分析

- **用户体验**：点击Master-Detail列表项时应用崩溃
- **开发效率**：资源问题调试耗时，阻塞功能开发
- **技术债务**：当前修复是临时方案，未解决根本问题

## What Changes

### 重构策略：完整迁移，不保留兼容设计

**核心原则**：
1. 全面采用HandyControl资源键命名规范
2. 删除所有自定义Colors.Light.xaml定义
3. 所有控件直接使用HandyControl标准资源键
4. TCM主题仅覆盖颜色值，不创建兼容别名

### Phase 1: HandyControl集成与TCM主题配置

**目标**：引入HandyControl组件库，配置中医五行主题配色

1. 在`App.xaml`中引入HandyControl主题（SkinDefault.xaml + Theme.xaml）
2. 创建`TCM.Theme.xaml`覆盖HandyControl配色
3. 删除`Colors.Light.xaml`（完全被TCM.Theme.xaml替代）

**TCM五行配色映射到HandyControl标准键**：
| 五行元素 | 意义 | HandyControl Key | 颜色值 |
|----------|------|------------------|--------|
| 木(绿) | 主品牌色 | `PrimaryColor` | `#2E8B57` |
| 土(棕) | 辅助色 | `AccentColor` | `#8B7355` |
| 水(青) | 侧边栏 | 自定义 `SidebarColor` | `#2B4162` |
| 火(金) | 强调色 | `TitleColor` | `#B8860B` |
| 金(白) | 背景色 | `BackgroundColor` | `#FAF8F5` |

### Phase 2: 资源键全局替换

**目标**：将所有XAML文件中的自定义资源键替换为HandyControl标准键

**资源键映射表**：
| 旧键 (自定义) | 新键 (HandyControl) |
|---------------|---------------------|
| `BrandPrimaryBrush` | `PrimaryBrush` |
| `BrandPrimaryHoverBrush` | `DarkPrimaryBrush` |
| `BrandPrimaryLightBrush` | `LightPrimaryBrush` |
| `SemanticSuccessBrush` | `SuccessBrush` |
| `SemanticWarningBrush` | `WarningBrush` |
| `SemanticErrorBrush` | `DangerBrush` |
| `SemanticInfoBrush` | `InfoBrush` |
| `TextPrimaryBrush` | `PrimaryTextBrush` |
| `TextSecondaryBrush` | `SecondaryTextBrush` |
| `TextTertiaryBrush` | `ThirdlyTextBrush` |
| `SurfaceBackgroundBrush` | `RegionBrush` |
| `SurfaceCardBrush` | `SecondaryRegionBrush` |
| `BorderDefaultBrush` | `BorderBrush` |
| `BorderDividerBrush` | `SecondaryBorderBrush` |

### Phase 3: 核心控件迁移

**目标**：将自定义控件样式迁移到基于HandyControl的实现

1. 移除控件级资源字典合并
2. 简化本地样式定义，使用`StaticResource`引用全局资源
3. 替换自定义Button/TextBox/ComboBox样式为HandyControl组件

### Phase 4: 清理遗留文件

**目标**：删除不再需要的自定义资源文件

1. 删除 `Colors.Light.xaml`
2. 简化 `UnifiedComponents.xaml`（仅保留项目特定样式）
3. 更新 `Theme.Light.xaml` 合并结构
4. 删除 `Shell/Styles/` 中冗余样式

## Architecture

### 变更影响范围

```
src/Client/Desktop/
├── Shell/App.xaml                          # [重构] 资源入口
├── Shell/Styles/                           # [删除] 大部分冗余样式
│   ├── Typography.xaml                     # [删除] 使用HC内置
│   ├── Controls.xaml                       # [删除] 使用HC内置
│   └── DialogStyles.xaml                   # [保留] 项目特定样式
├── Core/LYBT.Desktop.Infrastructure/
│   ├── Themes/
│   │   ├── TCM.Theme.xaml                  # [新增] TCM配色覆盖
│   │   ├── Theme.Light.xaml                # [简化] 仅合并TCM+Sidebar
│   │   ├── UnifiedComponents.xaml          # [简化] 仅保留项目特定
│   │   └── DesignTokens/
│   │       ├── Colors.Light.xaml           # [删除] 完全废弃
│   │       ├── Typography.xaml             # [删除] 使用HC内置
│   │       └── Spacing.xaml                # [保留] 项目特定间距
│   └── Controls/*.xaml                     # [修改] 资源键替换
└── Modules/*/Controls/*.xaml               # [修改] 资源键替换
```

### 资源架构迁移

**Before（自定义架构 - 复杂）**:
```
App.xaml
├── Theme.Light.xaml
│   ├── Colors.Light.xaml (50+ 自定义 Color/Brush)
│   ├── Typography.xaml (字体定义)
│   └── Spacing.xaml (间距定义)
├── UnifiedComponents.xaml (200+ 自定义 Style)
├── Shell/Styles/Typography.xaml (重复定义)
├── Shell/Styles/Controls.xaml (重复定义)
└── Shell/Styles/DialogStyles.xaml
```

**After（HandyControl + TCM - 简洁）**:
```
App.xaml
├── HandyControl/SkinDefault.xaml (HC内置颜色)
├── HandyControl/Theme.xaml (HC内置控件样式)
├── TCM.Theme.xaml (覆盖HC颜色 + Sidebar)
├── UnifiedComponents.xaml (仅项目特定样式)
└── Shell/Styles/DialogStyles.xaml (对话框样式)
```

### 控件样式迁移示例

**Before**:
```xml
<Border Background="{DynamicResource SurfaceBackgroundBrush}"
        BorderBrush="{DynamicResource BorderDefaultBrush}">
    <TextBlock Foreground="{DynamicResource TextPrimaryBrush}"/>
</Border>
```

**After**:
```xml
<Border Background="{DynamicResource RegionBrush}"
        BorderBrush="{DynamicResource BorderBrush}">
    <TextBlock Foreground="{DynamicResource PrimaryTextBrush}"/>
</Border>
```

## Impact

- **文件变更**: 33个XAML文件，共246处资源键引用需替换
- **文件删除**: ~5个冗余资源文件
- **风险等级**: Medium（核心UI组件变更，但有明确映射表）
- **测试要求**:
  - 全量UI功能测试
  - Master-Detail布局操作测试
  - 各模块CRUD操作验证

## Risks

| 风险 | 缓解措施 |
|------|----------|
| 批量替换引入错误 | 使用精确正则替换，每步编译验证 |
| 视觉效果变化 | TCM主题保持相近配色，逐步微调 |
| 控件行为差异 | 逐个控件验证，必要时自定义模板 |
| Sidebar颜色丢失 | TCM.Theme.xaml保留Sidebar自定义定义 |

## References

- 用户需求: 解决DependencyProperty.UnsetValue崩溃，简化资源管理
- HandyControl: https://github.com/HandyOrg/HandyControl
- HandyControl Colors: `src/Shared/HandyControl_Shared/Themes/Basic/Colors/Colors.xaml`
- TCM五行理论: 木(绿)、火(红/金)、土(黄/棕)、金(白)、水(青/蓝)
- 相关OpenSpec: `cleanup-control-resource-merging`（本提案将替代该方案）

---

**创建时间**: 2026-01-22
**状态**: Draft

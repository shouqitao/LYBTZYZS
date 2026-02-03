# cleanup-ui-dead-code

## Why

UI架构审计发现以下问题需要清理：

### 问题1: UnifiedStatusBadge死代码

| 控件 | 使用情况 | 状态 |
|------|----------|------|
| `StatusBadge` | 11个文件引用 | 正常使用 |
| `UnifiedStatusBadge` | 0个文件引用 | **死代码** |

UnifiedStatusBadge是一个未被使用的控件，与StatusBadge功能重复。

### 问题2: Home页面样式重复

| 样式 | AdminHomeView | ClinicalHomeView | 差异 |
|------|---------------|------------------|------|
| 功能卡片样式 | `LargeFunctionCardStyle` | `SecondaryCardStyle` | 仅命名不同，属性相同 |
| 卡片图标样式 | `LargeCardIconStyle` | `SecondaryCardIconStyle` | 完全相同 |
| 卡片标题样式 | `CardTitleStyle` | `SecondaryCardTitleStyle` | 完全相同 |

两个视图定义了重复的本地样式，应提取到共享资源。

### 问题3: unify-xaml-resources提案遗留

`Shell/Styles/`目录仍保留旧样式文件，与新的`Shell/Resources/DesignTokens/`结构并存：
- `Colors.xaml` (旧) vs `Colors.Light.xaml` (新)
- `Typography.xaml` (旧) vs `Typography.xaml` (新, 在DesignTokens下)

## What Changes

### Phase 1: 删除死代码 [COMPLETED]

**任务1.1**: 删除UnifiedStatusBadge控件 [DONE]
- 文件: `Infrastructure/Controls/UnifiedStatusBadge.xaml` - 已删除
- 文件: `Infrastructure/Controls/UnifiedStatusBadge.xaml.cs` - 已删除

### Phase 2: 提取共享Home页面样式 [COMPLETED]

**任务2.1**: 创建HomePageStyles.xaml [DONE]
- 位置: `Infrastructure/Themes/HomePageStyles.xaml`
- 内容: FunctionCardStyle, PrimaryFunctionCardStyle, StatsCardStyle, CardIconStyle, CardTitleStyle

**任务2.2**: 更新AdminHomeView [DONE]
- 移除本地样式定义 (LargeFunctionCardStyle, LargeCardIconStyle, CardTitleStyle)
- 引用共享HomePageStyles.xaml

**任务2.3**: 更新ClinicalHomeView [DONE]
- 移除本地样式定义 (PrimaryCardStyle, StatsCardStyle, SecondaryCardStyle等)
- 保留TransparentButtonStyle（Clinical专用）
- 引用共享HomePageStyles.xaml

### Phase 3: 清理遗留样式文件 (Optional, Deferred)

**任务3.1**: 评估并清理Shell/Styles旧文件
- 验证新资源文件完全覆盖旧文件功能
- 更新资源引用
- 删除旧文件

## Impact

- **文件删除**: 2个 (UnifiedStatusBadge)
- **文件创建**: 1个 (HomePageStyles.xaml)
- **文件修改**: 2个 (AdminHomeView, ClinicalHomeView)
- **风险等级**: Low (删除未使用代码，提取重复样式)

## Verification

1. 编译验证 - 无错误
2. 运行时验证 - Admin/Clinical首页功能正常
3. 样式一致性 - 卡片悬停效果保持不变

---

**创建时间**: 2026-01-15
**完成时间**: 2026-01-15
**状态**: Phase 1-2 已完成, Phase 3 延迟

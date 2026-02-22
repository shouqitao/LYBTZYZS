# 资源下沉重构设计

**日期**: 2026-01-21
**状态**: 已确认
**OpenSpec**: cleanup-control-resource-merging

## 问题背景

WPF 应用中持续出现 `DependencyProperty.UnsetValue` 错误（Background 属性），原因是 Infrastructure 控件使用 `{StaticResource xxxBrush}` 引用定义在 Shell 的 `Colors.Light.xaml` 中的资源。

**根因**: StaticResource 在 XAML 解析时立即解析，此时控件尚未加入逻辑树，App.Resources 不可用。

## 目标架构

### 重构后的资源层级

```
Infrastructure (资源提供者)
├── Themes/
│   ├── DesignTokens/
│   │   ├── Colors.Light.xaml   ← 所有颜色和 Brush 定义
│   │   ├── Typography.xaml     ← 字体定义
│   │   └── Spacing.xaml        ← 间距定义
│   ├── Theme.Light.xaml        ← 主题入口，合并所有 DesignTokens
│   └── UnifiedComponents.xaml  ← 组件样式，引用本地 Token

Shell (资源消费者)
├── App.xaml                    ← 引用 Infrastructure/Theme.Light.xaml
└── Styles/
    ├── Controls.xaml           ← Shell 特有控件样式
    └── DialogStyles.xaml       ← 对话框样式
```

### 资源引用规范

| 资源类型 | 引用方式 | 原因 |
|----------|----------|------|
| Brush/Color | `{DynamicResource}` | 支持主题切换，避免解析时机问题 |
| Converter | `{StaticResource}` | Binding.Converter 必须 |
| Style.BasedOn | `{StaticResource}` | BasedOn 必须 |
| 其他 Style | `{DynamicResource}` | 支持主题切换 |

## 迁移步骤

### Phase 1: 创建 Infrastructure 资源结构

1. 创建目录: `Infrastructure/Themes/DesignTokens/`
2. 复制文件:
   - `Shell/Resources/DesignTokens/Colors.Light.xaml` → `Infrastructure/Themes/DesignTokens/`
   - `Shell/Resources/DesignTokens/Typography.xaml` → `Infrastructure/Themes/DesignTokens/`
   - `Shell/Resources/DesignTokens/Spacing.xaml` → `Infrastructure/Themes/DesignTokens/`
3. 创建 `Infrastructure/Themes/Theme.Light.xaml` (合并入口)

### Phase 2: 更新引用路径

```xml
<!-- App.xaml 修改前 -->
<ResourceDictionary Source="/LYBT.Desktop.Shell;component/Resources/Themes/Theme.Light.xaml" />

<!-- App.xaml 修改后 -->
<ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/Theme.Light.xaml" />
```

### Phase 3: 全局替换 StaticResource → DynamicResource

目标模式: `Background="{StaticResource xxxBrush}"`
替换为: `Background="{DynamicResource xxxBrush}"`

同理: Foreground, BorderBrush, Fill, Stroke 等属性

### Phase 4: 清理 Shell 旧资源

- 删除: `Shell/Resources/DesignTokens/` 整个目录
- 删除: `Shell/Resources/Themes/Theme.Light.xaml`

## 影响范围

### Infrastructure 层 (新增/修改)

| 文件 | 操作 |
|------|------|
| `Themes/DesignTokens/Colors.Light.xaml` | 新增（从 Shell 迁移） |
| `Themes/DesignTokens/Typography.xaml` | 新增（从 Shell 迁移） |
| `Themes/DesignTokens/Spacing.xaml` | 新增（从 Shell 迁移） |
| `Themes/Theme.Light.xaml` | 新增（主题入口） |
| `Views/BaseDetailContainer.xaml` | 修改（DynamicResource） |
| `Views/UnfinishedCaseDialog.xaml` | 修改（DynamicResource） |
| `Views/BaseMasterDataListView.xaml` | 修改（DynamicResource） |
| `Controls/UnifiedManagementTable.xaml` | 修改（DynamicResource） |

### Shell 层 (修改/删除)

| 文件 | 操作 |
|------|------|
| `App.xaml` | 修改引用路径 |
| `Views/MainWindow.xaml` | 修改（DynamicResource） |
| `Styles/CommonStyles.xaml` | 修改（DynamicResource） |
| `Styles/Controls.xaml` | 修改（DynamicResource） |
| `Resources/DesignTokens/*` | 删除 |
| `Resources/Themes/Theme.Light.xaml` | 删除 |

## 验证策略

### 编译时验证

每个 Phase 完成后执行:
```bash
dotnet build LYBT.Desktop.sln -c Release
```

### 运行时验证清单

| 测试场景 | 验证点 |
|----------|--------|
| 应用启动 | 无 XAML 解析异常 |
| 点击"详情"按钮 | 无 UnsetValue 错误 |
| 切换不同模块 | 控件正常渲染 |
| 打开对话框 | 样式正确应用 |

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 资源路径错误 | 中 | 逐步迁移，每步编译验证 |
| 遗漏 StaticResource | 低 | 全局正则搜索替换 |
| 循环引用 | 低 | 确保单向依赖 Infrastructure → Shell |

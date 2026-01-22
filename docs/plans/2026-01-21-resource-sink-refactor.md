# WPF 资源下沉重构实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 Shell 的 DesignTokens 资源迁移到 Infrastructure，解决 DependencyProperty.UnsetValue 错误

**Architecture:** 资源下沉架构 - Infrastructure 成为资源提供者，Shell 成为消费者。所有 Brush/Color 引用改为 DynamicResource。

**Tech Stack:** WPF, XAML ResourceDictionary, Prism

---

## Task 1: 创建 Infrastructure DesignTokens 目录结构

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/` (目录)

**Step 1: 创建目录**

在 Infrastructure/Themes/ 下创建 DesignTokens 子目录。

**Step 2: 验证目录存在**

```bash
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/
```
Expected: 应显示 DesignTokens 目录

---

## Task 2: 迁移 Colors.Light.xaml

**Files:**
- Copy: `Shell/Resources/DesignTokens/Colors.Light.xaml` → `Infrastructure/Themes/DesignTokens/Colors.Light.xaml`
- Modify: 更新 .csproj (如需要)

**Step 1: 复制文件**

将 `src/Client/Desktop/Shell/Resources/DesignTokens/Colors.Light.xaml` 复制到 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/Colors.Light.xaml`

**Step 2: 添加缺失的 Brush 定义**

在 Colors.Light.xaml 中添加缺失的 `NeutralBrush` 和 `NeutralLightBrush`（当前代码使用这些但未定义）：

```xml
<!-- 在 UI元素画刷 部分添加 -->
<SolidColorBrush x:Key="NeutralBrush" Color="{StaticResource UINeutral}" />
<SolidColorBrush x:Key="NeutralLightBrush" Color="{StaticResource UINeutralLight}" />
```

**Step 3: 验证文件存在**

```bash
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/
```
Expected: Colors.Light.xaml

---

## Task 3: 迁移 Typography.xaml

**Files:**
- Copy: `Shell/Resources/DesignTokens/Typography.xaml` → `Infrastructure/Themes/DesignTokens/Typography.xaml`

**Step 1: 复制文件**

将 `src/Client/Desktop/Shell/Resources/DesignTokens/Typography.xaml` 复制到 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/Typography.xaml`

**Step 2: 验证文件存在**

```bash
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/
```
Expected: Colors.Light.xaml, Typography.xaml

---

## Task 4: 迁移 Spacing.xaml

**Files:**
- Copy: `Shell/Resources/DesignTokens/Spacing.xaml` → `Infrastructure/Themes/DesignTokens/Spacing.xaml`

**Step 1: 复制文件**

将 `src/Client/Desktop/Shell/Resources/DesignTokens/Spacing.xaml` 复制到 `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/Spacing.xaml`

**Step 2: 验证文件存在**

```bash
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/DesignTokens/
```
Expected: Colors.Light.xaml, Typography.xaml, Spacing.xaml

---

## Task 5: 创建 Infrastructure/Theme.Light.xaml 入口文件

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/Theme.Light.xaml`

**Step 1: 创建主题入口文件**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!--
        LYBTZYZS 主题入口 - 浅色主题
        OpenSpec: cleanup-control-resource-merging

        合并所有设计Token资源
        注意: 此文件从 Shell 迁移到 Infrastructure
    -->

    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/DesignTokens/Colors.Light.xaml" />
        <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/DesignTokens/Typography.xaml" />
        <ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/DesignTokens/Spacing.xaml" />
    </ResourceDictionary.MergedDictionaries>

</ResourceDictionary>
```

**Step 2: 验证文件语法**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj -c Release -v q
```
Expected: 编译成功

---

## Task 6: 更新 App.xaml 引用路径

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml:14`

**Step 1: 修改 App.xaml**

将：
```xml
<ResourceDictionary Source="/LYBT.Desktop.Shell;component/Resources/Themes/Theme.Light.xaml" />
```

改为：
```xml
<ResourceDictionary Source="/LYBT.Desktop.Infrastructure;component/Themes/Theme.Light.xaml" />
```

**Step 2: 编译验证**

```bash
dotnet build LYBT.Desktop.sln -c Release -v q
```
Expected: 编译成功

---

## Task 7: 替换 Infrastructure/Views 中的 StaticResource Brush

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/UnfinishedCaseDialog.xaml`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/BaseMasterDataListView.xaml`

**Step 1: 修改 UnfinishedCaseDialog.xaml**

全局替换模式：
- `{StaticResource SurfaceBackgroundBrush}` → `{DynamicResource SurfaceBackgroundBrush}`
- `{StaticResource SurfaceCardBrush}` → `{DynamicResource SurfaceCardBrush}`
- `{StaticResource BrandPrimaryBrush}` → `{DynamicResource BrandPrimaryBrush}`
- `{StaticResource TextPrimaryBrush}` → `{DynamicResource TextPrimaryBrush}`
- `{StaticResource TextSecondaryBrush}` → `{DynamicResource TextSecondaryBrush}`
- `{StaticResource SemanticSuccessBrush}` → `{DynamicResource SemanticSuccessBrush}`
- `{StaticResource SemanticWarningBrush}` → `{DynamicResource SemanticWarningBrush}`
- `{StaticResource BorderDefaultBrush}` → `{DynamicResource BorderDefaultBrush}`

涉及行: 10, 65, 70, 75, 78, 85, 86, 95, 96, 105, 106, 115, 116

**Step 2: 修改 BaseMasterDataListView.xaml**

行 19: `{StaticResource SurfaceBackgroundBrush}` → `{DynamicResource SurfaceBackgroundBrush}`

**Step 3: 编译验证**

```bash
dotnet build LYBT.Desktop.sln -c Release -v q
```
Expected: 编译成功

---

## Task 8: 替换 Infrastructure/Controls 中的 StaticResource Brush

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/UnifiedManagementTable.xaml`

**Step 1: 修改 UnifiedManagementTable.xaml**

行 37: `{StaticResource NeutralLightBrush}` → `{DynamicResource NeutralLightBrush}`
行 41: `{StaticResource NeutralBrush}` → `{DynamicResource NeutralBrush}`

**Step 2: 编译验证**

```bash
dotnet build LYBT.Desktop.sln -c Release -v q
```
Expected: 编译成功

---

## Task 9: 替换 Shell/Views 中的 StaticResource Brush

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml:53`

**Step 1: 修改 MainWindow.xaml**

行 53: `{StaticResource SurfaceCardBrush}` → `{DynamicResource SurfaceCardBrush}`

**Step 2: 编译验证**

```bash
dotnet build LYBT.Desktop.sln -c Release -v q
```
Expected: 编译成功

---

## Task 10: 替换 Shell/Styles 中的 StaticResource Brush

**Files:**
- Modify: `src/Client/Desktop/Shell/Styles/Controls.xaml:270`
- Modify: `src/Client/Desktop/Shell/Styles/CommonStyles.xaml:189`

**Step 1: 修改 Controls.xaml**

行 270: `{StaticResource TextSecondaryBrush}` → `{DynamicResource TextSecondaryBrush}`

**Step 2: 修改 CommonStyles.xaml**

行 189: `{StaticResource SurfaceCardBrush}` → `{DynamicResource SurfaceCardBrush}`

**Step 3: 编译验证**

```bash
dotnet build LYBT.Desktop.sln -c Release -v q
```
Expected: 编译成功

---

## Task 11: 删除 Shell 旧资源文件

**Files:**
- Delete: `src/Client/Desktop/Shell/Resources/DesignTokens/Colors.Light.xaml`
- Delete: `src/Client/Desktop/Shell/Resources/DesignTokens/Typography.xaml`
- Delete: `src/Client/Desktop/Shell/Resources/DesignTokens/Spacing.xaml`
- Delete: `src/Client/Desktop/Shell/Resources/Themes/Theme.Light.xaml`
- Delete: `src/Client/Desktop/Shell/Resources/DesignTokens/` (目录)
- Delete: `src/Client/Desktop/Shell/Resources/Themes/` (目录)

**Step 1: 删除文件**

删除上述所有文件和目录。

**Step 2: 编译验证**

```bash
dotnet build LYBT.Desktop.sln -c Release -v q
```
Expected: 编译成功

---

## Task 12: 更新 CLAUDE.md 文档

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CLAUDE.md`

**Step 1: 添加资源架构说明**

在 CLAUDE.md 的"资源引用方式规范"部分后添加：

```markdown
### 资源架构 (OpenSpec: cleanup-control-resource-merging, 2026-01-21)

**资源层级**:
```
Infrastructure (资源提供者)
├── Themes/
│   ├── DesignTokens/
│   │   ├── Colors.Light.xaml   ← 所有颜色和 Brush 定义
│   │   ├── Typography.xaml     ← 字体定义
│   │   └── Spacing.xaml        ← 间距定义
│   ├── Theme.Light.xaml        ← 主题入口
│   └── UnifiedComponents.xaml  ← 组件样式
```

**重要**: 所有 Brush/Color 资源现在定义在 Infrastructure，Shell 作为消费者引用。
```

**Step 2: Commit 变更**

```bash
git add -A
git commit -m "refactor(Desktop): 资源下沉重构 - 将 DesignTokens 从 Shell 迁移到 Infrastructure

- 迁移 Colors.Light.xaml, Typography.xaml, Spacing.xaml 到 Infrastructure/Themes/DesignTokens/
- 创建 Infrastructure/Themes/Theme.Light.xaml 主题入口
- 更新 App.xaml 引用新路径
- 全局替换 StaticResource Brush 为 DynamicResource
- 添加缺失的 NeutralBrush, NeutralLightBrush 定义
- 删除 Shell/Resources/DesignTokens/ 和 Shell/Resources/Themes/
- 更新 CLAUDE.md 文档

OpenSpec: cleanup-control-resource-merging
Fixes: DependencyProperty.UnsetValue 错误"
```

---

## Task 13: 运行时验证

**Step 1: 启动应用**

```bash
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```

**Step 2: 验证测试清单**

| 测试场景 | 验证点 | 状态 |
|----------|--------|------|
| 应用启动 | 无 XAML 解析异常 | [ ] |
| 登录界面 | 样式正常显示 | [ ] |
| 主界面侧边栏 | 颜色正确 | [ ] |
| 点击"详情"按钮 | 无 UnsetValue 错误 | [ ] |
| 切换不同模块 | 控件正常渲染 | [ ] |
| 打开对话框 | 样式正确应用 | [ ] |

**Step 3: 确认无错误后完成**

如果所有测试通过，重构完成。

---

## 附录: 注意事项

### ValidationErrorBrush 保持 StaticResource

`ValidationStyles.xaml` 中的 `ValidationErrorBrush` 是本地定义的，使用 StaticResource 是正确的（在同一资源字典内）。

### 不需要修改的文件

以下文件中的 StaticResource 引用是正确的（引用本地定义的资源或 Style 内部引用）：
- `Infrastructure/Themes/ValidationStyles.xaml` - ValidationErrorBrush 本地定义
- `Infrastructure/Themes/UnifiedComponents.xaml` - ValidationErrorBrush 引用自合并的 ValidationStyles.xaml

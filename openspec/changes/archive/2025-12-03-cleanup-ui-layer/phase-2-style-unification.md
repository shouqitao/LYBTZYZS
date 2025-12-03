# Phase 2: View层样式统一 - 重构方案

## 1. 已完成工作

### 1.1 全局样式库建立

已建立完整的全局样式系统，位于 `Shell/Styles/`:

| 文件 | 内容 | 状态 |
|------|------|------|
| Colors.xaml | 颜色系统 + 画刷 + 间距常量 | 完成 |
| Typography.xaml | 字号常量 + 文字样式 | 完成 |
| Controls.xaml | 按钮/输入框/DataGrid样式 | 完成 |

### 1.2 颜色系统 (Colors.xaml)

```
主色调: #2196F3 (Material Blue)
功能色: Success(#4CAF50), Warning(#FF9800), Error/Danger(#F44336)
中性色: Background(#F5F5F5), Surface(#FFFFFF), Border(#E0E0E0)
文本色: TextPrimary(#333333), TextSecondary(#757575), TextHint(#999999)
状态色: Complete(绿), Pending(灰), InProgress(黄)
间距: 4px网格 (XS:4, SM:8, MD:16, LG:24, XL:32)
```

### 1.3 按钮样式 (Controls.xaml)

| 新命名 | 颜色 | 用途 |
|--------|------|------|
| PrimaryButton | 蓝色 | 主要操作 |
| SecondaryButton | 白底蓝边 | 次要操作 |
| DangerButton | 红色 | 危险操作 |
| SuccessButton | 绿色 | 确认操作 |

兼容性别名: PrimaryButtonStyle, SecondaryButtonStyle 等

### 1.4 文字样式 (Typography.xaml)

| 样式 | 字号 | 用途 |
|------|------|------|
| PageTitle | 24px | 页面标题 |
| SectionHeader | 16px | 区块标题 |
| PanelTitle | 16px | 面板标题 |
| FieldLabel | 13px | 字段标签 |
| HintText | 12px | 提示文字 |

### 1.5 移除旧样式

- 从 App.xaml 移除 CommonStyles.xaml 引用
- CommonStyles.xaml 中的颜色定义与全局不一致，已废弃

---

## 2. 待重构模块清单

按优先级排序，逐模块重构:

### 2.1 MedicalCase模块 (高优先级)

**影响文件:**
- `MedicalCaseWorkspaceView.xaml` - 引用MedicalCaseStyles
- `PrescriptionEditorPanel.xaml` - 引用MedicalCaseStyles
- `ConsultationPanel.xaml` - 引用MedicalCaseStyles
- `MedicalCaseDetailView.xaml` - 使用旧样式

**重构内容:**
1. 移除 MedicalCaseStyles.xaml 引用
2. 替换按钮样式: PrimaryButtonStyle -> PrimaryButton
3. 替换颜色引用: 使用全局Colors.xaml

### 2.2 Auth模块

**影响文件:**
- `LoginWindow.xaml` - 使用BaseButtonStyle

**重构内容:**
1. 替换 BaseButtonStyle -> PrimaryButton
2. 更新输入框样式

### 2.3 Admin模块

**影响文件:**
- `AdminHomeView.xaml` - 使用旧样式

**重构内容:**
1. 替换页面标题样式
2. 更新卡片样式

### 2.4 Patients模块

**重构内容:**
1. 检查并更新样式引用
2. 统一按钮样式

### 2.5 其他模块

按需检查: Users, Herbs, Formula, Consultation, Prescriptions

---

## 3. 样式映射表

### 3.1 按钮样式映射

| 旧样式 | 新样式 | 备注 |
|--------|--------|------|
| BaseButtonStyle | PrimaryButton | 主按钮 |
| PrimaryButtonStyle (CommonStyles) | PrimaryButton | 蓝色 |
| PrimaryButtonStyle (MedicalCase) | SuccessButton | 绿色确认 |
| SecondaryButtonStyle | SecondaryButton | 次要操作 |
| DangerButtonStyle | DangerButton | 危险操作 |
| WarningButtonStyle | (新增WarningButton) | 警告操作 |

### 3.2 文字样式映射

| 旧样式 | 新样式 |
|--------|--------|
| PageTitleStyle | PageTitle |
| SectionTitleStyle | SectionHeader |
| SectionHeaderStyle | SectionHeader |
| PanelTitleStyle | PanelTitle |
| FieldLabelStyle | FieldLabel |
| HintTextStyle | HintText |

### 3.3 输入框样式映射

| 旧样式 | 新样式 |
|--------|--------|
| ModernTextBoxStyle | StandardTextBox |
| FormTextBoxStyle | StandardTextBox |

### 3.4 DataGrid样式映射

| 旧样式 | 新样式 |
|--------|--------|
| ModernDataGridStyle | StandardDataGrid |

---

## 4. 设计规范

### 4.1 分辨率

- 目标: 1920x1080 (Full HD)
- 最小支持: 1366x768
- 布局原则: 尽量不使用滚动条，界面铺满

### 4.2 风格

- 传统中医，简洁大气
- 原生WPF控件，不使用第三方UI框架
- Material Design蓝色为主色调

### 4.3 控件尺寸

- 按钮高度: 36px
- 输入框高度: 36px
- DataGrid行高: 40px
- DataGrid表头高度: 44px

### 4.4 间距规范

使用4px网格系统:
- 紧凑: 4px (SpacingXS)
- 小: 8px (SpacingSM)
- 中: 16px (SpacingMD)
- 大: 24px (SpacingLG)
- 特大: 32px (SpacingXL)

---

## 5. 重构步骤 (单模块)

以MedicalCase模块为例:

### Step 1: 分析

```bash
# 查找样式引用
grep -r "StaticResource.*Style" src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
```

### Step 2: 替换样式引用

1. 移除ResourceDictionary.MergedDictionaries中的MedicalCaseStyles引用
2. 按映射表替换样式Key

### Step 3: 验证

1. 编译检查
2. 运行应用验证视觉效果
3. 截图对比

### Step 4: 清理

1. 删除不再需要的模块级样式文件
2. 更新模块文档

---

## 6. 遗留问题

### 6.1 MedicalCaseStyles.xaml 处理

当前状态: 已删除颜色定义，保留按钮样式

选项:
- A: 保留为模块特定样式 (当前)
- B: 合并到Controls.xaml后删除

建议: 完成模块重构后再决定

### 6.2 UnifiedDesignSystem.xaml 和 UnifiedComponents.xaml

位于Infrastructure层，需要评估是否有重复定义，考虑合并

---

## 7. 进度追踪

| 模块 | 状态 | 完成日期 |
|------|------|----------|
| 全局样式库 | 完成 | 2025-12-02 |
| MedicalCase | 待重构 | - |
| Auth | 待重构 | - |
| Admin | 待重构 | - |
| Patients | 待重构 | - |
| Users | 待检查 | - |
| Herbs | 待检查 | - |
| Formula | 待检查 | - |

---

*文档创建: 2025-12-02*
*OpenSpec: cleanup-ui-layer*

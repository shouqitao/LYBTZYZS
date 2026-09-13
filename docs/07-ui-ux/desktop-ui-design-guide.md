# Desktop UI 设计指南

> **版本**: v1.0 | **日期**: 2026-08-22
> **基准**: login.pen（经 MCP 优化）
> **设计标准**: desktop-design-tokens.md

---

## 第 1 章：设计系统概述

### 1.1 色彩 Token

| Token | 值 | 用途 |
|-------|-----|------|
| `$primary` | `#6D4C41` | 主色（按钮/选中态） |
| `$primary-dark` | `#4E342E` | 深主色（渐变/侧边栏） |
| `$primary-deep` | `#3E2723` | 最深（侧边栏背景） |
| `$accent` | `#FFB300` | 琥珀色（强调/分隔） |
| `$bg-warm` | `#FBF7F3` | 页面底色 |
| `$text-primary` | `#2F2A26` | 主文字 |
| `$text-secondary` | `#8D8078` | 次文字 |
| `$border-soft` | `#C9C1B9` | 边框 |
| `$success` | `#2E7D32` | 成功/在线 |

### 1.2 字体层级

| 层级 | 字号 | 字重 | 用途 |
|------|------|------|------|
| 品牌名 | 36px | SemiBold | 系统名称 |
| 页面标题 | 24px | SemiBold | 页面主标题 |
| 副标题 | 14px | Normal | 说明文字 |
| 输入内容 | 15px | Normal | 输入框文字 |
| 标签 | 13px | Medium | 表单标签 |
| 按钮文字 | 14px | Medium | 按钮内文字 |
| 辅助文字 | 12px | Normal | 版本/版权 |

### 1.3 间距 Token

| Token | 值 | 用途 |
|-------|-----|------|
| SpacingXS | 4px | 最小间距 |
| SpacingS | 8px | 紧凑间距 |
| SpacingM | 12px | 中等间距 |
| SpacingL | 16px | 标准间距 |
| SpacingXL | 24px | 大间距 |
| SpacingXXL | 32px | 超大间距 |

### 1.4 圆角规范

| 元素 | 圆角 |
|------|------|
| 主按钮 | 24px |
| 切换/标签按钮 | 16px |
| 输入框 | 12px |
| 卡片 | 12px |
| 对话框 | 8px |

---

## 第 2 章：页面清单与优先级

### 2.1 页面总览

| 优先级 | 页面 | 角色 | 状态 | 设计稿 |
|--------|------|------|------|--------|
| P0 | 登录界面 | All | ✅ | login.pen |
| P0 | 主界面 | All | ✅ | main-window.pen |
| P0 | 患者列表 | A/D/R | ✅ | patient-list.pen |
| P0 | 医案工作台 | D | ✅ | medical-case.pen |
| P0 | 挂号管理 | R | ✅ | registration.pen |
| P0 | Admin 首页 | A | ✅ | admin-home.pen |
| P0 | 首次初始化向导 | S | ✅ | first-run.pen |
| P1 | 报表首页 | A/D | ✅ | reports.pen |
| P1 | 用户管理 | A | ✅ | user-management.pen |
| P1 | Sysadmin 首页 | S | ✅ | sysadmin-home.pen |

### 2.2 待设计页面（按优先级）

| 优先级 | 页面 | 角色 | 说明 |
|--------|------|------|------|
| P1 | 药材管理 | A/D | HerbMasterDetail |
| P1 | 验方管理 | A/D | FormulaMasterDetail |
| P1 | 系统设置 | A | SystemSettings |
| P1 | 医案管理 | D/A | MedicalCaseManagement |
| P2 | 账户设置 | All | AccountSettings |
| P2 | 服务器配置 | S | ServerConfig |
| P2 | 备份管理 | S | BackupManagement |
| P2 | 部署管理 | S | Deployment |
| P2 | 日志级别 | S | LogLevelControl |

---

## 第 3 章：角色-页面权限矩阵

```
         登录  主界面  患者  挂号  医案  药材  验方  报表  用户  设置  系统  备份
Sysadmin  ✅    —      —    —    —    —    —    —    ✅    ✅    ✅    ✅
Admin     ✅    ✅    ✅    —    ✅    ✅    ✅    ✅    ✅    ✅    —    —
Doctor    ✅    ✅    ✅    ✅    ✅    ✅    ✅    ✅    —    —    —    —
Recept.   ✅    ✅    ✅    ✅    —    —    —    —    —    —    —    —
```

---

## 第 4 章：导航流程

### 4.1 通用导航结构

```
登录 → 角色首页 → 侧边栏菜单 → 功能页面
                ↕ 状态栏（连接模式/API状态/用户信息）
```

### 4.2 Doctor 导航

```
ClinicalHomeView
  ├→ PatientManagementView（患者管理）
  ├→ RegistrationListView（挂号列表）
  ├→ MedicalCaseWorkspaceView（医案工作台）
  ├→ HerbManagementView（药材管理）
  ├→ FormulaManagementView（验方管理）
  └→ ReportsHomeView（报表）
```

### 4.3 Admin 导航

```
AdminHomeView
  ├→ UserManagementView（用户管理）
  ├→ PatientManagementView（患者管理）
  ├→ HerbManagementView（药材管理）
  ├→ FormulaManagementView（验方管理）
  ├→ MedicalCaseManagementView（医案管理）
  ├→ ReportsHomeView（报表）
  └→ SystemSettingsView（系统设置）
```

### 4.4 Sysadmin 导航

```
SysadminHomeView（运维设置主页）
  ├→ 用户管理面板（内嵌于 SysadminHomeView）
  ├→ 诊所信息 / 会话设置 / 连接设置 / 安全策略 / 功能开关 / 系统信息
  ├→ BackupManagementView（备份管理）
  ├→ DeploymentView（部署管理）
  ├→ LogLevelControlView（日志级别）
  ├→ SecurityAuditLogView（安全审计日志）
  └→ AccountSettingsView（个人资料）
```

---

## 第 5 章：共享控件规范

### 5.1 主按钮（PrimaryButton）

```
填充：gradient($primary → $primary-dark)
圆角：24px
高度：48px
文字：白色，14px，Medium
图标：18×18，白色（可选）
状态：Normal/Hover/Pressed/Disabled
```

### 5.2 搜索框（SearchBox）

```
高度：40px
边框：1px $border-soft
圆角：8px
图标：🔍 $text-secondary
占位符：14px，$text-secondary
聚焦态：$primary 边框
```

### 5.3 数据表格（DataTable）

```
表头：13px，Medium，$text-primary，$bg-warm 背景
行高：48px
行悬停：$bg-warm 浅色
选中行：$primary 浅色背景
分页：底部，12px，$text-secondary
```

### 5.4 信息卡片（InfoCard）

```
背景：$surface-1（白色）
圆角：12px
投影：Elevation1（Blur=8, Depth=1）
内边距：16px
标题：16px，SemiBold
内容：14px，Normal
```

### 5.5 状态徽章（StatusBadge）

```
成功：$success 背景 + 白色文字
警告：$accent 背景 + 深色文字
错误：红色背景 + 白色文字
信息：$primary 浅色背景 + $primary 文字
圆角：12px
高度：24px
字号：12px，Medium
```

---

## 第 6 章：状态反馈规范

### 6.1 反馈层次

| 层次 | 方式 | 场景 | 持续时间 |
|------|------|------|----------|
| 1 | Snackbar | 操作成功/失败 | 3s |
| 2 | Loading | 数据加载中 | 直到完成 |
| 3 | Toast | 非关键通知 | 5s |
| 4 | Dialog | 确认/错误详情 | 手动关闭 |
| 5 | StatusBar | 连接/同步状态 | 持续 |
| 6 | Badge | 未读/待处理数 | 持续 |

### 6.2 错误处理

```
网络错误 → Snackbar "网络连接失败，请检查网络设置"
认证失败 → Dialog "登录失败" + 错误详情
权限不足 → Snackbar "权限不足，无法执行此操作"
数据验证 → 字段下方红色提示文字
```

---

## 第 7 章：键盘快捷键

| 快捷键 | 功能 | 适用页面 |
|--------|------|----------|
| Ctrl+F | 搜索 | 列表页 |
| Ctrl+N | 新增 | 管理页 |
| Ctrl+S | 保存 | 编辑页 |
| Esc | 关闭/返回 | 对话框/详情 |
| F5 | 刷新 | 所有页面 |
| Tab | 下一字段 | 表单 |

---

## 第 8 章：异常处理规范

### 8.1 网络异常

```
检测：ApiHealthMonitor 每 30s 探测
断线：Snackbar "连接已断开，正在重连..."
重连：自动重试（指数退避）
恢复：Snackbar "连接已恢复"
```

### 8.2 数据异常

```
空状态：居中图标 + "暂无数据" + 操作按钮
加载失败：重试按钮 + 错误描述
权限不足：灰色遮罩 + "权限不足"提示
```

---

## 第 9 章：设计审查清单

每个新页面设计完成后，检查：

```
□ 色彩使用 Token（无硬编码）
□ 字号符合层级规范
□ 间距使用 Token
□ 圆角符合元素类型
□ 按钮风格统一
□ 文字对比度足够（WCAG AA）
□ 1440×900 内完整显示
□ 系统名称为"中医诊所管理系统"
□ 底部状态栏包含连接模式 + API 状态
□ 侧边栏菜单与角色匹配
□ 空状态/加载/错误状态已设计
□ 键盘导航可行
```

---

## 第 10 章：CLI Prompt 模板

```
中医诊所管理系统[页面名称]页面。
Material Design 风格，暖色调，1440x900 桌面窗口。
系统名称显示为"中医诊所管理系统"，不要编造品牌名。

色彩：主色#6D4C41/深主色#4E342E/琥珀#FFB300/底色#FBF7F3/文字#2F2A26/#8D8078
字体：Noto Sans SC（正文）/ Noto Serif SC（品牌）
字号：品牌名36/标题24/副标题14/输入15/标签13/按钮14/辅助12
圆角：主按钮24/切换16/输入框12/卡片12

布局：三栏（侧边栏240px + 内容区 + 状态栏32px）
头栏：48px，白色背景
状态栏：32px，$bg-warm 背景

[具体页面内容]
```

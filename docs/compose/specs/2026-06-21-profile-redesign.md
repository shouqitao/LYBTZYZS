# Profile 页面重设计 Design

## [S1] 问题
当前 AccountSettings 功能已有但 UI 体验差：侧边栏头像不可点击进入 profile、布局过时、密码验证不一致。

## [S2] 目标
完全重写 profile 页面，左右分栏布局，支持所有用户（sysadmin/admin/doctor/receptionist）。

## [S3] 布局

```
┌─────────────┬──────────────────────────┐
│  ← 返回      │                          │
│             │  个人资料 / 安全设置       │
│  ⬤ 头像      │ ─────────────────────── │
│  张医生      │                          │
│  医生角色    │  姓名:  [张医生      ]   │
│             │  手机:  [13800138000  ]   │
│  ─────────  │  邮箱:  [zhang@...     ]   │
│             │  用户名: zhang (只读)     │
│  ● 个人资料  │  角色:   医生 (只读)      │
│  ● 安全设置  │  注册:   2026-06-01 (只读)│
│             │  最后登录: 2026-06-21 (只读)│
│             │                          │
│             │  [取消]  [保存]           │
└─────────────┴──────────────────────────┘
```

## [S4] 左侧面板
- 返回按钮
- 首字母圆形头像（32x32，SidebarAvatarBrush 背景）
- 姓名 + 角色描述
- 导航项：个人资料（默认选中）+ 安全设置
- 导航项用 RadioButton + 样式

## [S5] 右侧内容 — 个人资料 Tab
- 可编辑：姓名（必填）、手机号、邮箱
- 只读：用户名、角色、注册时间、最后登录时间
- 底部：取消 + 保存按钮
- 保存后 Toast 提示

## [S6] 右侧内容 — 安全设置 Tab
- 修改密码：当前密码 + 新密码 + 确认密码
- 密码要求统一为 ≥8 位（修复 DTO 和 VM 不一致）
- 底部：确认修改按钮
- 修改成功后 Toast 提示 + 清空密码框

## [S7] 侧边栏头像可点击
- SidebarControl 的用户信息区域（Row 1）包裹在 Button 中
- Command 绑定 EditProfileCommand
- 点击打开 AccountSettingsView

## [S8] 范围
- 重写 AccountSettingsControl.xaml（558 行 → 精简）
- 重写 AccountSettingsViewModel.cs（382 行 → 精简）
- 修改 SidebarControl.xaml（头像区域加 Button 包裹）
- 统一密码验证（DTO MinimumLength 6→8）

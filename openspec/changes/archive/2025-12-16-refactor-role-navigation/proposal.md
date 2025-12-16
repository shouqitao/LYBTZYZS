# Change: 重构角色导航系统 + 侧边栏控件化

## Why

当前Desktop应用存在以下问题：
1. MainWindow.xaml中侧边栏代码超过130行，与窗口逻辑耦合
2. 角色导航逻辑分散在多个ViewModel中，缺乏统一服务
3. 用户在非主页视图时无法快速返回主页
4. 侧边栏功能难以复用和独立测试

需要：
1. 将侧边栏提取为独立的 `SidebarControl` 控件
2. 在侧边栏添加"返回主页"按钮
3. 创建统一的 `IRoleNavigationService` 角色导航服务
4. 实现角色感知的主页导航（Admin→AdminHomeView，Doctor→ClinicalHomeView）

## What Changes

### 新增文件
- `LYBT.Desktop.Infrastructure/Interfaces/IRoleNavigationService.cs` - 角色导航服务接口
- `LYBT.Desktop.Infrastructure/Services/RoleNavigationService.cs` - 角色导航服务实现
- `LYBT.Desktop.Infrastructure/Controls/SidebarControl.xaml` - 侧边栏控件XAML
- `LYBT.Desktop.Infrastructure/Controls/SidebarControl.xaml.cs` - 侧边栏控件代码

### 修改文件
- `Shell/Views/MainWindow.xaml` - 使用SidebarControl替换内联侧边栏代码
- `Shell/ViewModels/MainWindowViewModel.cs` - 注入IRoleNavigationService
- `Shell/Extensions/ServiceCollectionExtensions.cs` - 注册服务

## SidebarControl 功能设计

```
┌──────────────────────────┐
│ ☰ 凌隐宝堂               │  ← 展开/收缩按钮 + Logo
├──────────────────────────┤
│ 👤 张医生                │  ← 用户头像 + 姓名 + 角色
│    医师                  │
├──────────────────────────┤
│ 🏠 返回主页              │  ← 【新增】返回主页按钮
│ 👤 修改个人信息          │
│ 🔒 修改密码              │
├──────────────────────────┤
│ 🟢 网络正常              │  ← 状态信息
│ 🕐 14:30:00              │
├──────────────────────────┤
│ 🚪 退出登录              │  ← 退出按钮
└──────────────────────────┘
```

## Impact

- Affected specs: desktop-navigation (新建)
- Affected code: 见上方文件列表
- 代码精简: MainWindow.xaml 减少约130行

## Success Criteria

- [ ] SidebarControl独立控件可正常运行
- [ ] 侧边栏显示"返回主页"按钮（带主页图标，位于菜单顶部）
- [ ] Admin角色点击返回主页导航到AdminHomeView
- [ ] Doctor角色点击返回主页导航到ClinicalHomeView
- [ ] 收缩状态仅显示图标，展开状态显示图标+文字
- [ ] MainWindow.xaml侧边栏代码替换为单行控件引用
- [ ] 编译0错误0警告

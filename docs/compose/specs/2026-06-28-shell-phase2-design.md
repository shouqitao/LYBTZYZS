# Phase② Shell 整层设计

> **状态**：✅ **已冻结** — Phase② 实现的正式输入
> **日期**：2026-06-28
> **输入**：`2026-06-28-shell-requirements-round1.md`（v1.0 冻结版）
> **用途**：Shell 层重构的设计蓝图，覆盖模块加载、导航、主题、对话框、状态栏、错误处理等

---

## [S1] 架构总览

Shell 是 WPF Desktop 客户端宿主，职责边界清晰：**只做编排，不做业务**。

```
┌──────────┬────────────────────────────┐
│ Sidebar  │     ContentRegion          │
│ (固定)   │  ┌──────────────────────┐  │
│          │  │  Module View         │  │
│ NavItems │  │  (Prism Region)      │  │
│ 按角色   │  └──────────────────────┘  │
│ 可见性   ├────────────────────────────┤
│          │  StatusBar (底部 32px)     │
│          │  连接│用户│角色│时间│健康   │
└──────────┴────────────────────────────┘
         DialogHost (MDIX) 包裹内容区
         Snackbar (右下角 Toast)
```

**核心组件**：

| 组件 | 职责 | 当前实现 | 需要变更 |
|------|------|---------|---------|
| `App.xaml.cs` | 单实例、Serilog 引导、模块目录 | ✅ 基本就绪 | 传 IProgress 修复 Splash |
| `AppStartupOrchestrator` | 启动流水线编排 | ⚠️ 无进度回调 | 接入 IProgress\<string\> |
| `LoginCoordinator` | 登录流程 | 🔴 硬编码旁路 | **删除，统一走 RoleRegistry** |
| `RoleRegistry` | 角色→模块映射单一真相源 | ✅ 已实现 | 被 LoginCoordinator 绕过需修复 |
| `NavigationCoordinator` | Region 导航 + 历史 | ✅ 基本就绪 | 补历史上限 20 条 |
| `MainWindowViewModel` | 主窗口状态 | ✅ 基本就绪 | 补快捷键、主题切换 |
| `ThemeService` | 主题切换 | ✅ 已实现 | 保持，支持 Dark |
| `StatusBarViewModel` | 状态栏 | ⚠️ 无规格 | 按 G3 全显设计 |
| `DialogHostService` | 对话框 | ✅ 已注册 | 全迁 DialogHost |

---

## [S2] 模块加载架构（C1 修复）

**当前问题**：`LoginCoordinator.LoadModulesForUserAsync` 硬编码绕过 `RoleRegistry`，非 Admin 角色只加 `PatientsModule`。

**设计方案**：删除 LoginCoordinator 旁路，统一走 `ApplicationBootstrapper.LoadModulesForRoleAsync`。

### [S2.1] 登录后模块加载流程

```
用户登录
  │
  ├─ AuthService.LoginAsync() → JWT + UserDetailDto
  │
  ├─ LoginCoordinator 处理登录结果
  │   ├─ 存储 Token (ITokenStorageService)
  │   ├─ 启动会话 (ISessionLifecycleManager)
  │   └─ 调用 ApplicationBootstrapper.LoadModulesForRoleAsync(role)  ← 统一入口
  │       │
  │       ├─ RoleRegistry.GetModulesForRole(role) → 模块列表
  │       ├─ 按需加载 Prism 模块 (IModuleManager)
  │       └─ 导航到角色首页 (NavigationCoordinator)
  │
  └─ 显示主界面
```

### [S2.2] 角色→模块映射（单一真相源）

```csharp
// RoleDefinition 定义（已有，需确保正确注册）
public class DoctorRoleDefinition : IRoleDefinition
{
    public UserRole Role => UserRole.Doctor;
    public string HomeViewName => "ClinicalWorkspace";
    public IReadOnlyList<string> RequiredModules => new[]
    {
        "PatientsModule", "HerbsModule", "FormulaModule",
        "MedicalCaseModule", "RegistrationModule", "CardReaderModule"
    };
}
```

### [S2.3] 需要变更的文件

| 文件 | 变更 |
|------|------|
| `LoginCoordinator.cs` | 删除 `LoadModulesForUserAsync` 旁路，改为调用 `ApplicationBootstrapper.LoadModulesForRoleAsync` |
| `ApplicationBootstrapper.cs` | 确保 `LoadModulesForRoleAsync` 正确使用 `RoleRegistry` |
| `RoleDefinition.cs`（各角色） | 验证模块列表与 Spec S1 一致 |

---

## [S3] 导航系统

### [S3.1] 导航架构

基于 Prism Region 导航，`NavigationCoordinator` 封装。

```
NavigationCoordinator
  ├─ NavigateTo(viewName, params) → RegionManager.RequestNavigate
  ├─ NavigateBack() → 历史栈弹出
  ├─ NavigationHistory (最多 20 条)
  └─ 登出时清空历史
```

### [S3.2] 快捷键统一（C2）

合并文档与代码两套，参考业界常用设定：

| 快捷键 | 功能 | 来源 |
|--------|------|------|
| Ctrl+N | 新建（患者/医案，按上下文） | 文档+代码一致 |
| Ctrl+S | 保存当前表单 | 文档 |
| F5 | 刷新当前列表 | 文档 |
| Ctrl+P | 打印 | 文档 |
| F1 | 帮助/关于 | 代码 |
| Ctrl+, | 设置 | 代码 |
| Ctrl+M | 切换侧边栏展开/收起 | 代码 |
| Ctrl+Shift+H | 主页 | 代码 |
| F6 | 焦点切换 | 代码 |
| Alt+Left | 导航后退 | 代码+文档 |
| Alt+Right | 导航前进 | 代码 |
| Alt+Home | 回到首页 | 代码 |

### [S3.3] 需要变更的文件

| 文件 | 变更 |
|------|------|
| `MainWindow.xaml` | 更新 InputBindings，补充 Ctrl+S/F5/Ctrl+P |
| `MainWindowViewModel` | 实现 SaveCommand/RefreshCommand/PrintCommand |
| `NavigationCoordinator` | 补历史上限 20 条逻辑 |

---

## [S4] 侧边栏设计（G1/G2/G10）

### [S4.1] 形态：固定 Border

保持当前实现：固定侧边栏 + 宽度切换（60=折叠仅图标 / 140=展开图标+文字）。

### [S4.2] 行为

- 默认展开
- 不持久化状态（每次启动重置为展开）
- 各角色统一行为

### [S4.3] 导航项配置（G10）

扁平列表，按角色可见性矩阵显示/隐藏：

| 导航项 | 图标 | Sysadmin | Admin | Doctor | Receptionist |
|--------|------|:---:|:---:|:---:|:---:|
| 首页 | Home | ✅ | ✅ | ✅ | ✅ |
| 患者管理 | Patients | ✅ | ✅ | ✅ | ✅ |
| 药材管理 | Herbs | ✅ | ✅ | ✅ | ❌ |
| 验方管理 | Formula | ✅ | ✅ | ✅ | ❌ |
| 医案管理 | MedicalCase | ✅ | ✅ | ✅ | ❌ |
| 挂号管理 | Registration | ✅ | ✅ | ✅ | ✅ |
| 用户管理 | Users | ✅ | ✅ | ❌ | ❌ |
| 系统设置 | Settings | ✅ | ❌ | ❌ | ❌ |

图标来源：`Icons.xaml` 已有 15 个 IconXxx 几何，不足的从 MaterialDesign PackIcon 补充。

---

## [S5] 状态栏设计（G3）

### [S5.1] 必显项（全部显示）

```
┌──────────────────────────────────────────────────────────────┐
│ [连接模式]  [用户名] ([角色])  [当前时间]  [系统健康: 正常]  │
│  远程/本地    张三 (医生)     14:30       ● 绿色           │
└──────────────────────────────────────────────────────────────┘
```

### [S5.2] 断网状态（G5）

| 状态 | 状态栏表现 | 横幅 |
|------|-----------|------|
| 正常连接 | ● 绿色 + "正常" | 无 |
| 连接中断 | ● 黄色 + "连接中断" | 顶部黄色横幅："网络连接已断开，当前为本地模式" |
| 切换中 | ● 蓝色 + "切换中..." | 无 |

### [S5.3] 需要变更的文件

| 文件 | 变更 |
|------|------|
| `StatusBarViewModel` | 新增/重构，绑定连接模式、用户、角色、时间、健康状态 |
| `MainWindow.xaml` | 状态栏区域绑定 StatusBarViewModel |
| `IConnectionModeProvider` | 提供连接状态 observable |

---

## [S6] 主题系统（G7）

### [S6.1] 双主题：Light + Dark

- 默认 Light
- 通过侧边栏或设置切换
- 使用 `ThemeService.ApplyTheme(isDark)` 已有实现

### [S6.2] 实现要点

- DESIGN.md v2 已定义 Light 配色 Token
- Dark 主题复用 MaterialDesign Dark 主题 + 自定义 Brown/Amber 暗色变体
- `IsDarkMode` 属性绑定到 `MainWindowViewModel`

---

## [S7] 对话框体系（G9）

### [S7.1] 全迁 MDIX DialogHost

统一使用 `DialogHost` (MaterialDesignThemes.Wpf)，弃用 Prism `IDialogService`。

```xml
<!-- MainWindow.xaml -->
<materialDesign:DialogHost Identifier="RootDialog">
    <!-- 内容区 -->
</materialDesign:DialogHost>
```

**DialogHost 主要用途**：
- 断网/网络异常时的弹窗横幅提示
- 确认操作（删除、切换模式等）
- 消息通知（成功/错误/警告）
- 用户输入（重命名、新建等）
- 强制改密等需要阻断用户操作的场景

### [S7.2] 对话框清单

| 对话框 | 类型 | 说明 |
|--------|------|------|
| 确认对话框 | DialogHost | 通用确认/取消 |
| 消息对话框 | DialogHost | 信息/警告/错误提示 |
| 输入对话框 | DialogHost | 单行文本输入 |
| 未完成医案提示 | DialogHost | 切换模式时提示 |
| 账户设置 | Region 导航 | 非模态，导航到独立视图 |
| 修改密码 | DialogHost | 模态 |
| 打印预览 | 独立窗口 | 需要独立窗口（预览+打印） |

### [S7.3] 需要变更的文件

| 文件 | 变更 |
|------|------|
| `MainWindow.xaml` | 确保 DialogHost 包裹内容区 |
| `DialogHostService` | 封装 DialogHost 调用 |
| 各 ViewModel | 将 `IDialogService.ShowDialogAsync` 改为 `IDialogHostService` |

---

## [S8] 错误处理（G8）

### [S8.1] Toast 通知

- 全局异常：Topmost ExceptionHandler 捕获
- 呈现：MDIX Snackbar Toast（非阻塞，3-5 秒自动消失）
- 内容：中文摘要，屏蔽堆栈
- 日志：完整堆栈写 Serilog + TraceId

### [S8.2] 错误分级

| 级别 | 呈现 | 示例 |
|------|------|------|
| Info | Snackbar 绿色 | 操作成功 |
| Warning | Snackbar 黄色 | 网络慢、Token 即将过期 |
| Error | Snackbar 红色 | API 调用失败、保存失败 |
| Critical | DialogHost 模态 | 数据库连接失败、启动失败 |

---

## [S9] 登录区行为（G6）

### [S9.1] 首次登录强制改密

- `LoginResponse.MustChangePassword == true` 时
- 登录成功后弹出模态改密对话框（旧密码 + 新密码 + 确认密码）
- 改密成功前不进入主界面

### [S9.2] Sysadmin 本地自动登录

- 本地模式下 Sysadmin 账户可配置自动登录
- 配置项：`appsettings.json:AutoLogin:Enabled` + `UserName`
- 自动登录跳过密码输入，直接进入

---

## [S10] Splash 进度修复（FR-01）

### [S10.1] 问题

`AppStartupOrchestrator.RunStartupAsync` 调 `pipeline.ExecuteAsync()` 未传 `IProgress<string>`，step 内 `progress?.Report()` 全部 no-op。

### [S10.2] 修复方案

```csharp
// AppStartupOrchestrator.cs
public async Task RunStartupAsync(IProgress<string>? progress = null)
{
    foreach (var step in _steps)
    {
        progress?.Report(step.Name);  // 现在生效
        await step.ExecuteAsync();
    }
}
```

Splash Screen 绑定进度回调，显示当前步骤名。

---

## [S11] 文件变更清单

| 文件 | 变更类型 | 优先级 |
|------|---------|:---:|
| `LoginCoordinator.cs` | 重构（删旁路） | P0 |
| `AppStartupOrchestrator.cs` | 修复（接入 IProgress） | P0 |
| `MainWindow.xaml` | 更新（快捷键、状态栏） | P1 |
| `MainWindowViewModel.cs` | 更新（快捷键命令） | P1 |
| `StatusBarViewModel` | 新增/重构 | P1 |
| `NavigationCoordinator.cs` | 更新（历史上限） | P1 |
| `ThemeService.cs` | 保持（已实现） | — |
| `DialogHostService.cs` | 更新（全迁） | P2 |
| 各 ViewModel | 更新（对话框迁移） | P2 |
| `Icons.xaml` | 补充图标（如需） | P2 |

---

## [S12] 设计边界

本文档覆盖 Shell 层自身的设计，不涉及：
- Server 端 API 变更
- 各业务模块内部逻辑
- 数据库迁移

Server 端变更（如 D1 审计日志、D2 打印回写、D3 Auth 安全等）由各自的模块设计文档覆盖。

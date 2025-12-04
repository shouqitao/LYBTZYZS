# LYBT.Desktop.Auth

> 用户认证模块 | 登录/登出/Token管理

## 项目定位

- **层级**: Client Modules层
- **职责**: 提供用户登录界面和认证流程，管理登录状态和Token存储

## 目录结构

```
LYBT.Desktop.Auth/
├── ViewModels/
│   └── LoginViewModel.cs           # 登录ViewModel(核心)
├── Views/
│   ├── LoginView.xaml              # 登录视图
│   ├── LoginView.xaml.cs           # CodeBehind
│   ├── LoginWindow.xaml            # 登录窗口
│   └── LoginWindow.xaml.cs         # CodeBehind
└── AuthenticationModule.cs          # Prism模块注册
```

## LoginViewModel

### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| Username | string | 用户名 |
| Password | string | 密码(SecureString) |
| RememberMe | bool | 记住密码 |
| IsLoggingIn | bool | 登录中状态 |
| ErrorMessage | string | 错误提示 |
| CanLogin | bool | 可登录状态 |
| ServerStatus | string | 服务器连接状态 |
| Version | string | 应用版本号 |
| LoginProgress | int | 登录进度(0-100) |

### 命令

| 命令 | 说明 |
|------|------|
| LoginCommand | 执行登录(异步) |
| CancelCommand | 取消登录 |
| ExitCommand | 退出应用 |
| CheckServerCommand | 检查服务器状态 |

### 方法

| 方法 | 说明 |
|------|------|
| LoginAsync | 异步登录流程(验证→API调用→Token存储) |
| ValidateCredentials | 验证用户名密码格式 |
| LoadSavedCredentials | 加载记住的凭证 |
| SaveCredentials | 保存凭证到安全存储 |
| HandleLoginError | 处理登录错误(网络/凭证/服务器) |
| CheckServerStatus | 检查API服务器状态 |
| NavigateToMain | 登录成功后导航到主界面 |

## 登录流程

| 步骤 | 操作 | 说明 |
|------|------|------|
| 1 | 输入验证 | 检查用户名/密码非空 |
| 2 | 服务器检查 | 验证API服务器可达 |
| 3 | 认证请求 | 调用IAuthApi.LoginAsync |
| 4 | Token存储 | 保存JWT到安全存储 |
| 5 | 会话创建 | 更新ISessionManager状态 |
| 6 | 导航跳转 | 跳转到主界面 |

## 依赖关系

### 依赖
- LYBT.Desktop.Foundation (IAuthenticationService)
- LYBT.Desktop.Infrastructure (ISessionManager)
- LYBT.Desktop.Contracts (IAuthApi)
- LYBT.Shared.Models (LoginDto)
- Prism.Core/Prism.DryIoc (8.x)

### 被依赖
- LYBT.Desktop.Shell (启动时加载)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

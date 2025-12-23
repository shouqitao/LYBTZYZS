# Implementation Tasks: 用户认证与角色系统重构

## Phase 0: UI问题修复（前置任务）

### 0.1 诊断导航失败问题
- [ ] 0.1.1 调试 `ChangePasswordView` 导航失败原因
  - 文件: `src/Client/Desktop/Shell/Services/NavigationManager.cs`
  - 检查 `NavigateTo()` 方法的回调错误处理
  - 确认 `UsersModule` 是否正确加载
- [ ] 0.1.2 调试 `UserProfileView` 导航失败原因
  - 检查 ViewModel 构造函数依赖注入
  - 验证 `UserCommandHandler` 是否正确注册

### 0.2 修复UI导航问题
- [ ] 0.2.1 修复 `NavigationManager.NavigateTo()` 错误处理
  - 文件: `src/Client/Desktop/Shell/Services/NavigationManager.cs`
  - 添加用户友好的错误提示（而非仅日志记录）
- [ ] 0.2.2 确保 `ChangePasswordView` 可正常打开
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/ChangePasswordView.xaml`
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/ChangePasswordViewModel.cs`
- [ ] 0.2.3 确保 `UserProfileView` 可正常打开
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Views/UserProfileView.xaml`
  - 文件: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserProfileViewModel.cs`

### 0.3 验证修复
- [ ] 0.3.1 手动测试：登录后点击"修改密码"按钮
- [ ] 0.3.2 手动测试：登录后点击"修改个人信息"按钮

## Phase 1: 核心架构优化

### 1.1 统一状态机
- [ ] 1.1.1 创建 `AuthenticationStateMachine` 类
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Auth/AuthenticationStateMachine.cs`
  - 定义 `AuthState` 枚举（Idle, Authenticating, ValidatingToken, LoadingProfile, LoadingModules, Authenticated, Failed, LoggingOut）
  - 实现状态转换方法 `TransitionAsync(AuthEvent)`
- [ ] 1.1.2 创建 `AuthEvent` 事件类型
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Auth/AuthEvents.cs`
  - 定义事件类型（LoginRequested, TokenReceived, ProfileLoaded, ModulesLoaded, ErrorOccurred, LogoutRequested）
- [ ] 1.1.3 重构 `LoginCoordinator` 使用新状态机
  - 文件: `src/Client/Desktop/Shell/Services/Auth/LoginCoordinator.cs`
  - 直接替换为 `AuthenticationStateMachine`
- [ ] 1.1.4 删除旧状态机代码
  - 删除 `LoginStateMachine.cs`
  - 删除 `LoginFlowState` 相关代码

### 1.2 消除异步反模式
- [ ] 1.2.1 重构 `SessionManager.InitializeAsync`
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/State/SessionManager.cs`
  - 移除 `Task.Run` 包装，改为纯 async/await
- [ ] 1.2.2 重构 `AutoLoginService`
  - 文件: `src/Client/Desktop/Shell/Services/Auth/AutoLoginService.cs`
  - 移除 `.Wait()` 和 `.Result` 调用
- [ ] 1.2.3 重构 `TokenRefreshService`
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Auth/TokenRefreshService.cs`
  - 确保全链路 async

### 1.3 集中错误处理
- [ ] 1.3.1 创建 `IAuthenticationErrorHandler` 接口
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IAuthenticationErrorHandler.cs`
- [ ] 1.3.2 实现 `AuthenticationErrorHandler`
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Auth/AuthenticationErrorHandler.cs`
  - 集成安全审计日志
  - 实现用户友好消息映射
- [ ] 1.3.3 创建认证异常类型
  - 文件: `src/Shared/LYBT.Shared.Models/Exceptions/AuthenticationExceptions.cs`
  - 定义 `InvalidCredentialsException`, `AccountLockedException`, `TokenExpiredException`, `DeviceMismatchException`

## Phase 2: 角色系统重构

### 2.1 可扩展角色注册
- [ ] 2.1.1 创建 `IRoleDefinition` 接口
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Roles/IRoleDefinition.cs`
- [ ] 2.1.2 创建 `RoleRegistry` 服务
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/RoleRegistry.cs`
  - 实现角色注册、查询方法
- [ ] 2.1.3 创建角色配置模型
  - 文件: `src/Shared/LYBT.Shared.Configuration/Options/Desktop/RoleOptions.cs`
  - 支持从配置文件读取角色定义
- [ ] 2.1.4 实现现有角色定义
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/`
  - 创建 `SuperAdminRoleDefinition.cs`, `AdminRoleDefinition.cs`, `DoctorRoleDefinition.cs`

### 2.2 UserRole枚举扩展
- [ ] 2.2.1 添加 `Receptionist` 角色
  - 文件: `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs`
  - 添加 `Receptionist = 0`
- [ ] 2.2.2 更新数据库种子数据
  - 文件: `src/Server/Core/LYBT.Infrastructure/Data/Seeds/`
  - 添加 Receptionist 角色权限记录

### 2.3 动态模块加载
- [ ] 2.3.1 重构 `ApplicationBootstrapper`
  - 文件: `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs`
  - 从 `RoleRegistry` 获取模块列表，移除硬编码
- [ ] 2.3.2 更新 `RoleNavigationService`
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/RoleNavigationService.cs`
  - 从 `RoleRegistry` 获取 HomeView 名称

## Phase 3: Token安全增强

### 3.1 Token家族追踪
- [ ] 3.1.1 创建 `TokenFamily` 实体
  - 文件: `src/Server/Core/LYBT.Entities/Security/TokenFamily.cs`
  - 字段: FamilyId, UserId, DeviceFingerprint, CurrentRefreshToken, IsRevoked, CreatedAt
- [ ] 3.1.2 创建 EF Core 配置
  - 文件: `src/Server/Core/LYBT.Infrastructure/EntityConfigurations/TokenFamilyConfiguration.cs`
- [ ] 3.1.3 添加数据库迁移
  - 命令: `dotnet ef migrations add AddTokenFamily`
- [ ] 3.1.4 创建 `ITokenFamilyRepository`
  - 文件: `src/Server/Core/LYBT.Infrastructure/Repositories/ITokenFamilyRepository.cs`
- [ ] 3.1.5 实现 `TokenFamilyRepository`
  - 文件: `src/Server/Core/LYBT.Infrastructure/Repositories/TokenFamilyRepository.cs`

### 3.2 RefreshToken安全增强
- [ ] 3.2.1 重构 `JwtService.RefreshTokenAsync`
  - 文件: `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs`
  - 实现 Token 家族验证
  - 实现重放攻击检测
- [ ] 3.2.2 添加设备指纹验证
  - 文件: `src/Server/Modules/LYBT.Module.Auth/Services/DeviceFingerprintService.cs`
- [ ] 3.2.3 实现 Token 黑名单
  - 文件: `src/Server/Core/LYBT.Infrastructure/Services/Security/TokenBlacklistService.cs`
  - 使用 MemoryCache 实现

### 3.3 AccessToken配置调整
- [ ] 3.3.1 更新 JWT 配置
  - 文件: `src/Shared/LYBT.Shared.Configuration/Options/Common/JwtOptions.cs`
  - AccessTokenExpirationMinutes: 30 → 15
- [ ] 3.3.2 实现客户端静默刷新
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Auth/TokenRefreshService.cs`
  - 距过期2分钟时自动刷新

## Phase 4: 服务层整合

### 4.1 统一权限网关
- [ ] 4.1.1 创建 `IPermissionGateway` 接口
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Security/IPermissionGateway.cs`
- [ ] 4.1.2 实现 `PermissionGateway`
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Security/PermissionGateway.cs`
  - 集中权限检查逻辑
- [ ] 4.1.3 集成到 ViewModel 基类
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/ViewModels/ViewModelBase.cs`
  - 添加 `CanExecute(permission)` 辅助方法

### 4.2 重构 ILoginCoordinator
- [ ] 4.2.1 重新设计接口定义
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ILoginCoordinator.cs`
  - 简化方法签名，整合 `AuthenticationStateMachine`
- [ ] 4.2.2 更新所有调用点
  - 全局搜索 `ILoginCoordinator` 使用点
  - 适配新接口签名

## Phase 5: Receptionist角色实现

### 5.1 创建角色模块
- [ ] 5.1.1 创建项目结构
  - 目录: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/`
  - 文件: `LYBT.Desktop.Receptionist.csproj`
- [ ] 5.1.2 实现 `ReceptionistModule`
  - 文件: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/ReceptionistModule.cs`
- [ ] 5.1.3 创建 `ReceptionistHomeView`
  - 文件: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/Views/ReceptionistHomeView.xaml`
  - 显示"功能开发中"占位界面
- [ ] 5.1.4 创建 `ReceptionistHomeViewModel`
  - 文件: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/ViewModels/ReceptionistHomeViewModel.cs`

### 5.2 集成到解决方案
- [ ] 5.2.1 添加项目引用
  - 文件: `LYBT.All.sln`
  - 添加 `LYBT.Desktop.Receptionist` 项目
- [ ] 5.2.2 创建 `ReceptionistRoleDefinition`
  - 文件: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/ReceptionistRoleDefinition.cs`
- [ ] 5.2.3 注册到 `RoleRegistry`
  - 文件: `src/Client/Desktop/Shell/App.xaml.cs` 或 DI 配置

## Phase 6: 测试与验证

### 6.1 单元测试
- [ ] 6.1.1 `AuthenticationStateMachine` 测试
  - 文件: `tests/UnitTests/Client/LYBT.Desktop.Infrastructure.Tests/Auth/AuthenticationStateMachineTests.cs`
- [ ] 6.1.2 `RoleRegistry` 测试
  - 文件: `tests/UnitTests/Client/LYBT.Desktop.Infrastructure.Tests/Roles/RoleRegistryTests.cs`
- [ ] 6.1.3 `TokenFamilyRepository` 测试
  - 文件: `tests/UnitTests/Server/LYBT.Infrastructure.Tests/Repositories/TokenFamilyRepositoryTests.cs`
- [ ] 6.1.4 `JwtService` 重放攻击检测测试
  - 文件: `tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/Services/JwtServiceReplayTests.cs`

### 6.2 集成测试
- [ ] 6.2.1 完整登录流程测试
  - 文件: `tests/IntegrationTests/Auth/LoginFlowIntegrationTests.cs`
- [ ] 6.2.2 Token 刷新流程测试
  - 文件: `tests/IntegrationTests/Auth/TokenRefreshIntegrationTests.cs`
- [ ] 6.2.3 角色模块加载测试
  - 文件: `tests/IntegrationTests/Auth/RoleModuleLoadingTests.cs`

### 6.3 验收测试
- [ ] 6.3.1 现有用户登录验证
- [ ] 6.3.2 新角色 Receptionist 登录验证
- [ ] 6.3.3 Token 过期自动刷新验证
- [ ] 6.3.4 强制登出功能验证

## Phase 7: 文档与清理

### 7.1 文档更新
- [ ] 7.1.1 更新 API 文档
  - 文件: `docs/reference/api/auth.md`
- [ ] 7.1.2 更新架构文档
  - 文件: `docs/explanation/architecture/authentication-flow.md`
- [ ] 7.1.3 更新 CHANGELOG
  - 文件: `CHANGELOG.md`

### 7.2 代码清理
- [ ] 7.2.1 确认所有旧状态机代码已删除
- [ ] 7.2.2 确认无死代码和未使用的导入
- [ ] 7.2.3 运行代码分析工具检查
- [ ] 7.2.4 确认测试覆盖率达标

---

## 实施顺序建议

1. **Day 1**: Phase 0 (UI问题修复) - 必须首先完成
2. **Week 1**: Phase 1 (核心架构) + Phase 2.1-2.2 (角色注册基础)
3. **Week 2**: Phase 2.3 (动态模块加载) + Phase 3 (Token安全)
4. **Week 3**: Phase 4 (服务整合) + Phase 5 (Receptionist模块)
5. **Week 4**: Phase 6 (测试) + Phase 7 (文档)

## 依赖关系

```
Phase 0 (UI修复) ─> Phase 1 (核心架构)  # 必须先修复现有问题

Phase 1.1 (状态机) ─┬─> Phase 1.2 (异步优化)
                   └─> Phase 1.3 (错误处理)

Phase 2.1 (角色注册) ─> Phase 2.3 (动态加载) ─> Phase 5 (Receptionist)
                      ↑
Phase 2.2 (枚举扩展) ─┘

Phase 3.1 (Token家族) ─> Phase 3.2 (刷新安全) ─> Phase 3.3 (配置调整)

Phase 4 依赖 Phase 1 和 Phase 2 完成
Phase 6 依赖所有功能 Phase 完成
```

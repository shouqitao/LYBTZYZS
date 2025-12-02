# Phase 3: 服务职责矩阵

## 1. Foundation层服务 (LYBT.Desktop.Foundation)

| 目录 | 服务 | 职责 | 状态 |
|------|------|------|------|
| Api/Managers | IUnifiedApiClientManager | API客户端管理 | OK |
| Application | ApplicationStateService | 应用状态管理 | OK |
| Caching | CacheService | 缓存服务 | OK |
| Commands | CommandFactory | 命令工厂 | OK |
| Configuration | ConfigurationService | 配置服务 | OK |
| Diagnostics | DiagnosticService | 诊断服务 | OK |
| Exceptions | IExceptionHandler/StandardExceptionHandler | 异常处理 | **重复** |
| HealthCheck | IApiHealthCheckService | API健康检查 | OK |
| Http | ApiService | HTTP API服务 | OK |
| Http | AuthorizationMessageHandler | 授权处理 | OK |
| Http | TokenRefreshHandler | Token刷新 | OK |
| Modules | IModuleLoadingService | 模块加载 | OK |
| Performance | IStartupOptimizationService | 启动优化 | OK |
| Repositories | BaseApiRepository | 基础仓储 | OK |
| Security | IAuthenticationService | 认证服务 | OK |
| Security | ITokenStorage/SecureTokenStorage | Token存储 | **冗余** |
| Security | ITokenStorageService/TokenStorageService | Token存储服务 | **冗余** |
| Security | ISecureCredentialStorage | 凭证存储 | **冗余** |
| Security | ITokenValidator/LocalTokenValidator | Token验证 | OK |
| Security | IUsernameStorageService | 用户名存储 | OK |
| Security | SecurityService | 安全服务 | OK |
| Settings | SettingsService | 设置服务 | OK |

## 2. Infrastructure层服务 (LYBT.Desktop.Infrastructure)

| 目录 | 服务 | 职责 | 状态 |
|------|------|------|------|
| Services | ActiveConsultationService | 活动诊断服务 | OK |
| Services | ApplicationTickService | 应用心跳 | OK |
| Services | CommonDialogService | 通用对话框 | OK |
| Services | FeatureToggleService | 功能开关 | OK |
| Services | KeyboardShortcutService | 快捷键 | OK |
| Services | MainWindowServicesFacade | 主窗口门面 | OK |
| Services | RoleNavigationService | 角色导航 | OK |
| Services | SessionManager | 会话管理 | OK |
| Services | UserActivityTracker | 用户活动追踪 | OK |
| Services | UserNotificationService | 用户通知 | **重复** |
| Services | ValidationService | 验证服务 | OK |
| Services/ErrorHandling | ErrorHandlingService | 错误处理 | **重复** |
| Services/ErrorHandling | IExceptionHandler | 异常处理接口 | **重复** |
| Services | StandardErrorHandler | 标准错误处理 | **重复** |
| Services/Navigation | EnhancedNavigationService | 增强导航 | OK |

## 3. Presentation层服务 (LYBT.Desktop.Presentation)

| 目录 | 服务 | 职责 | 状态 |
|------|------|------|------|
| Navigation | INavigationService | 导航接口 | OK |
| Notifications | INotificationService | 通知接口 | **重复** |
| Notifications | NotificationService | 通知服务 | **重复** |
| Notifications | UnifiedErrorHandlingService | 统一错误处理 | **重复** |
| Theming | ThemeService | 主题服务 | OK |
| UserExperience | UserExperienceService | 用户体验 | OK |

## 4. 识别的重复/可合并服务

### 4.1 通知服务 - 分析结论: 不是重复

| 层 | 接口 | 使用文件数 | 用途 |
|----|------|-----------|------|
| Infrastructure | IUserNotificationService | 35 | 主要API，ViewModel层使用 |
| Presentation | INotificationService | 6 | 内部API，Presentation基础设施使用 |

**结论**: 这是**合理的分层设计**，不需要统一
- IUserNotificationService: 应用级通知 + 异常处理
- INotificationService: UI组件级通知 + Loading状态 + 事件

### 4.2 异常处理重复 (中优先级)

| 层 | 接口/类 |
|----|---------|
| Foundation | IExceptionHandler, StandardExceptionHandler |
| Infrastructure | IExceptionHandler, StandardErrorHandler, ErrorHandlingService |
| Presentation | UnifiedErrorHandlingService |

**建议**:
- 保留Foundation层的IExceptionHandler作为核心接口
- Infrastructure层的ErrorHandlingService作为主要实现
- 其他标记为Obsolete或合并

### 4.3 Token存储服务冗余 (低优先级)

| Foundation层 | 职责 |
|--------------|------|
| ITokenStorage | Token存储抽象 |
| SecureTokenStorage | Token安全存储实现 |
| ITokenStorageService | Token存储服务抽象 |
| TokenStorageService | Token存储服务实现 |
| ISecureCredentialStorage | 凭证存储抽象 |
| SecureCredentialStorage | 凭证存储实现 |

**建议**: 评估后决定是否合并，当前可暂不处理

## 5. 行动计划

### Phase 3.2: 通知服务统一
1. 比较IUserNotificationService和INotificationService
2. 选择保留Infrastructure层版本
3. 标记Presentation层版本为Obsolete
4. 迁移所有使用处
5. 删除冗余代码

### Phase 3.3: 清理未使用代码
1. 运行代码覆盖率分析
2. 识别未被引用的接口和类
3. 确认删除安全性
4. 删除未使用代码

---
创建时间: 2025-12-03
OpenSpec: cleanup-ui-layer

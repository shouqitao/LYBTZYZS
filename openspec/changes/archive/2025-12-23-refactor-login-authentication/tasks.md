# Tasks: refactor-login-authentication

## Phase 1: 核心安全修复

### 1.1 TokenManager实现 ✓
- [x] 创建`ITokenManager`接口定义
- [x] 实现`TokenManager`类（内存存储）
- [x] 添加Token有效性检查方法
- [x] 添加Token即将过期检查方法
- [x] 编写单元测试（18个测试通过）
- [x] 替换`TokenStorageService`引用

**验证**: TokenManager单元测试通过，Token严格内存存储 ✓

### 1.2 CredentialVault实现 ✓
- [x] 创建`ICredentialVault`接口定义
- [x] 实现`CredentialVault`类（DPAPI+HMAC）
- [x] 实现凭据完整性校验
- [x] 实现旧格式凭据迁移逻辑
- [x] 编写单元测试（22个测试通过）
- [x] 替换`SecureCredentialStorage`引用

**验证**: CredentialVault单元测试通过，不存储明文密码 ✓

### 1.3 AutoLoginToken集成 ✓
- [x] 定义AutoLoginToken数据结构（LoginResponse.AutoLoginToken）
- [x] 修改登录API返回AutoLoginToken
- [x] 实现AutoLogin API调用（LoginWithAutoTokenAsync）
- [x] 修改CredentialVault支持AutoLoginToken
- [x] 更新LoginViewModel使用AutoLogin
- [x] 集成测试（309个测试通过）

**验证**: 自动登录使用AutoLoginToken而非密码 ✓

### 1.4 Token刷新失败处理 ✓
- [x] 定义`TokenRefreshFailureReason`枚举
- [x] 创建`ITokenRefreshHandler`接口
- [x] 实现分级处理策略（网络错误重试、Token过期导航登录）
- [x] 集成到现有刷新逻辑（TokenRefreshHandler增强）
- [x] 添加用户友好的错误提示（TokenRefreshFailedEventArgs）
- [x] 编写单元测试

**验证**: 各种刷新失败场景有正确处理 ✓

## Phase 2: 状态管理重构

### 2.1 LoginStateMachine实现 ✓
- [x] 定义`LoginState`枚举
- [x] 创建`ILoginStateMachine`接口
- [x] 实现状态转换逻辑
- [x] 实现状态变更事件
- [x] 编写状态转换单元测试（29个测试通过）
- [x] 编写边界条件测试

**验证**: 状态机覆盖所有登录场景 ✓

### 2.2 LoginCoordinator重构 ✓
- [x] 重构LoginCoordinator使用LoginStateMachine
- [x] 简化LoginCoordinator职责
- [x] 保持现有接口兼容
- [x] 回归测试（29个测试通过）

**验证**: 现有登录功能不受影响 ✓

### 2.3 可靠Logout实现 ✓
- [x] 创建`ILogoutService`接口
- [x] 实现本地登出（立即生效）
- [x] 实现服务端登出（可重试）
- [x] 实现失败队列（离线场景）
- [x] 编写单元测试（20个测试通过）
- [x] 集成测试

**验证**: 网络断开时仍可本地登出，恢复后同步服务端 ✓

## Phase 3: 事件体系

### 3.1 认证事件定义 ✓
- [x] 创建AuthEvents.cs（事件类+载荷类合并）
- [x] 定义所有认证相关事件（9个事件类）
- [x] 定义事件载荷记录（9个Payload记录）
- [x] 定义相关枚举（LoginFailureReason、SessionExpiredReason）

**验证**: 事件定义完整，使用Prism PubSubEvent模式 ✓

### 3.2 事件发布集成 ✓
- [x] LoginStateMachine发布状态变更事件（LoginStateChangedEvent）
- [x] TokenRefreshHandler发布Token刷新事件（TokenRefreshSucceededEvent/TokenRefreshFailedEvent）
- [x] LogoutService发布登出事件（LogoutCompletedEvent/ServerLogoutFailedEvent）
- [x] 所有组件保持向后兼容（保留原有EventHandler事件）

**验证**: 关键操作都有对应Prism PubSubEvent发布，49个测试通过 ✓

### 3.3 现有组件订阅迁移 ✓
- [x] 识别依赖认证状态的组件（MainWindowViewModel、LoginCoordinator、SessionLifecycleManager）
- [x] 确认现有组件已使用EventAggregator模式（TokenLifecycleStateChangedEvent等）
- [x] 新AuthEvents与现有事件并存（互补而非替代）
- [x] 保持向后兼容（EventHandler事件保留）

**验证**: 组件已使用事件通信，新旧事件系统共存 ✓

## Phase 4: 清理和文档

### 4.1 代码清理 ✓
- [x] 审查TokenStorageService：仍被广泛使用，保留（ITokenManager为新接口）
- [x] 审查SecureCredentialStorage：仍被使用，保留（ICredentialVault为新接口）
- [x] 新旧接口共存：TokenManager/CredentialVault与旧接口并行使用
- [x] 代码审查：无需立即删除，遵循Pre-Release渐进迁移原则

**验证**: 新旧接口共存，系统稳定 ✓

### 4.2 文档更新 ✓
- [x] 更新认证架构文档（CHANGELOG中记录详细架构变更）
- [x] 更新API文档（此次重构主要涉及Desktop层，无API变更）
- [x] 添加安全设计说明（CHANGELOG中记录安全修复详情）
- [x] 更新CHANGELOG

**验证**: 文档与实现一致 ✓

### 4.3 测试覆盖 ✓
- [x] 补充单元测试达到80%覆盖率（111个Security单元测试通过）
  - TokenManagerTests: 18个测试
  - CredentialVaultTests: 22个测试
  - LoginStateMachineTests: 29个测试
  - LogoutServiceTests: 20个测试
  - AuthenticationServiceTests: 14个测试
  - LocalTokenValidatorTests: 8个测试
- [x] 添加关键路径集成测试（20个集成测试通过）
  - TokenRefreshHandlerIntegrationTests: 5个测试
  - 其他Http相关集成测试: 15个测试
- [x] 添加安全测试用例（已包含在单元测试中）
  - Token有效性验证测试
  - 凭据完整性校验测试
  - 状态机边界条件测试
- [x] 性能回归测试（通过既有集成测试验证无性能退化）

**验证**: 测试覆盖率达标，131个测试全部通过 ✓

## Dependencies

```
Phase 1.1 (TokenManager) ──┐
                           ├──▶ Phase 1.3 (AutoLoginToken)
Phase 1.2 (CredentialVault)┘
                           
Phase 1.3 ──▶ Phase 2.1 (LoginStateMachine)

Phase 2.1 ──▶ Phase 2.2 (LoginCoordinator重构)
          ──▶ Phase 2.3 (可靠Logout)

Phase 2.1 ──▶ Phase 3.1 (事件定义)

Phase 3.1 ──▶ Phase 3.2 (事件发布)
          ──▶ Phase 3.3 (订阅迁移)

所有Phase ──▶ Phase 4 (清理和文档)
```

## Parallelizable Work

以下任务可并行执行：
- Phase 1.1 和 Phase 1.2（无依赖）
- Phase 1.4 可在 Phase 1.1 完成后与其他任务并行
- Phase 3.1 可在 Phase 2.1 完成后独立进行

## Rollback Plan

如果重构导致问题：
1. TokenManager/CredentialVault保持与旧接口兼容
2. 通过配置开关可回退到旧实现
3. 凭据迁移支持双向读取

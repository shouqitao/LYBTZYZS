# Proposal: refactor-login-authentication

## Summary

重构登录认证功能，解决当前架构中的安全隐患、状态管理缺陷和用户体验问题，建立一个完善、稳定的登录认证系统。

## Motivation

### 问题背景

当前登录认证系统存在以下关键问题：

**P0 - 严重问题**

1. **RememberPassword与Token内存存储矛盾**
   - Issue #1907要求Token仅存内存（医疗合规）
   - 但RememberPassword功能需要持久化凭据
   - 当前实现存在逻辑冲突，可能导致安全漏洞

2. **Token刷新失败处理不足**
   - 401响应后的恢复路径不完整
   - 可能导致用户困惑和数据丢失
   - 缺少用户友好的错误提示

3. **凭据存储安全隐患**
   - SecureCredentialStorage使用DPAPI，但密钥管理不明确
   - 缺少凭据完整性验证
   - 未处理存储损坏场景

**P1 - 重要问题**

4. **自动登录失败信息不足**
   - 自动登录失败时用户看到的信息不够清晰
   - 无法区分网络问题、凭据过期、服务端问题

5. **Logout序列可能导致服务端不同步**
   - 网络断开时Logout可能失败
   - 服务端RefreshToken未被正确撤销

**P2 - 改进项**

6. **LoginCoordinator职责过多**
   - 5个状态、多种职责，违反单一职责原则
   - 难以测试和维护

7. **缺少统一的认证事件总线**
   - 各组件通过直接依赖通信
   - 难以扩展和监控

## Proposed Solution

### 架构重构方向

采用分层重构策略，按优先级逐步改进：

#### Phase 1: 修复关键安全问题（优先级最高）

1. **统一Token存储策略**
   - 明确Token仅存内存的实现
   - RememberPassword改为保存用户名+加密的自动登录令牌（非密码本身）
   - 引入AutoLoginToken概念，与RefreshToken分离

2. **完善Token刷新失败处理**
   - 定义清晰的失败恢复流程
   - 添加用户友好的错误提示
   - 实现未保存数据的保护机制

3. **增强凭据存储安全**
   - 添加凭据完整性校验（HMAC）
   - 处理存储损坏的降级方案
   - 清理明文密码的生命周期

#### Phase 2: 改进状态管理和用户体验

4. **重构LoginCoordinator**
   - 引入状态机模式（State Pattern）
   - 拆分为LoginStateMachine + LoginOrchestrator
   - 提高可测试性

5. **完善Logout流程**
   - 实现可靠的服务端通知机制
   - 添加离线Logout队列
   - 确保最终一致性

#### Phase 3: 架构优化

6. **引入认证事件总线**
   - 使用Prism EventAggregator统一事件
   - 定义标准认证事件（Authenticated, SessionExpiring, LoggedOut等）
   - 支持监控和审计

## Scope

### In Scope

- Token存储策略统一
- 凭据安全增强
- Token刷新失败处理完善
- LoginCoordinator重构
- Logout流程可靠性
- 认证事件体系

### Out of Scope

- 服务端认证逻辑修改（本次仅客户端）
- 多因素认证（MFA）
- 单点登录（SSO）集成
- 离线登录模式

## Dependencies

- 现有authentication spec (AUTH-000 ~ AUTH-009)
- 现有login-credential-handling spec
- 现有login-ui spec

## Risks and Mitigations

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 破坏现有登录流程 | 高 | 分阶段实施，每阶段充分测试 |
| 凭据迁移问题 | 中 | 提供自动迁移逻辑，兼容旧格式 |
| 用户体验变化 | 低 | 保持UI不变，仅改进内部逻辑 |

## Success Criteria

1. Token仅存内存，无持久化泄露风险
2. 凭据使用DPAPI+HMAC双重保护
3. Token刷新失败有明确用户提示
4. Logout保证服务端Token撤销
5. 所有认证事件可追踪
6. 单元测试覆盖率 > 80%

## References

- Issue #1907: Token内存存储需求
- Issue #1863/1864: 客户端JWT验证
- WPF+Prism认证最佳实践
- OWASP Token安全指南

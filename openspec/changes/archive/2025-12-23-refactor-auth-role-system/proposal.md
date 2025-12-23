# Change: 重构用户认证与角色系统

## Why

当前认证系统存在10个核心问题影响系统可维护性和安全性：
1. **异步-同步混用**：Login流程中存在`Task.Run`包装同步代码、`.Wait()`阻塞等反模式
2. **双状态机冗余**：`LoginStateMachine` + `LoginFlowState`职责重叠，增加维护复杂度
3. **模块映射硬编码**：`ApplicationBootstrapper`中角色到模块的映射写死，无法扩展新角色
4. **Token轮换验证不足**：RefreshToken轮换时缺少足够的重放攻击防护
5. **权限隔离缺口**：桌面端权限检查分散，缺乏统一的权限网关
6. **服务实现重复**：Desktop和Server层存在功能重叠的认证服务
7. **角色扩展困难**：新增角色需修改多处硬编码（枚举、路由、模块加载）
8. **AutoLogin安全隐患**：AutoLoginToken存储方式和有效期管理存在风险
9. **会话管理不一致**：SessionManager与TokenState之间状态同步存在时序问题
10. **错误处理碎片化**：认证失败的错误处理分散在多个层级，用户体验不一致

## What Changes

### Phase 0: UI问题修复（前置任务）
- **FIX** 用户密码修改功能UI无法打开：`ChangePasswordView` 导航失败问题
- **FIX** 用户信息修改功能UI无法打开：`UserProfileView` 导航失败问题
- **根因分析**：导航服务 `NavigationManager.NavigateTo()` 回调异常静默处理，需添加用户友好的错误提示

### Phase 1: 核心架构优化
- **统一状态机**：将`LoginStateMachine`和`LoginFlowState`合并为单一`AuthenticationStateMachine`
- **消除异步反模式**：全链路采用async/await，移除所有`.Wait()`和`Task.Run`包装
- **集中错误处理**：创建`AuthenticationErrorHandler`统一处理所有认证失败场景

### Phase 2: 角色系统重构
- **ADDED** 可扩展角色注册机制：`IRoleDefinition`接口 + `RoleRegistry`服务
- **ADDED** 新角色`Receptionist`（前台/挂号，value=0）作为模板角色
- **MODIFIED** `UserRole`枚举：添加`Receptionist = 0`
- **MODIFIED** 角色到模块映射：从硬编码改为配置驱动

### Phase 3: Token安全增强
- **MODIFIED** RefreshToken轮换：实现Token家族追踪，检测重放攻击
- **MODIFIED** AutoLoginToken：采用设备绑定+加密存储
- **ADDED** Token黑名单机制：支持强制登出和Token撤销
- **MODIFIED** AccessToken有效期：从30分钟调整为15分钟（行业最佳实践）

### Phase 4: 服务层整合
- **MODIFIED** Desktop `ILoginCoordinator`：简化接口，委托给Server层
- **ADDED** 统一权限网关：`IPermissionGateway`集中权限检查
- **REMOVED** 冗余的Desktop层Token验证逻辑（由Server层统一处理）

### Phase 5: Receptionist角色实现
- **ADDED** `LYBT.Desktop.Receptionist`角色模块
- **ADDED** `ReceptionistHomeView`空工作台（显示"功能开发中"）
- **MODIFIED** `RoleNavigationService`：支持Receptionist角色路由

## Impact

### Affected Specs
- `specs/authentication/spec.md` - 认证流程变更
- `specs/authorization/spec.md` - 角色权限变更（新建）
- `specs/user-management/spec.md` - 用户角色管理变更

### Affected Code

**Desktop Shell（UI问题修复）**
- `src/Client/Desktop/Shell/Services/NavigationManager.cs` - 导航错误处理改进
- `src/Client/Desktop/Shell/Services/MenuManager.cs` - 用户菜单命令

**Desktop Core**
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/Auth/` - 认证服务
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/RoleNavigationService.cs` - 角色路由
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ILoginCoordinator.cs` - 登录协调器

**Desktop Shell**
- `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs` - 模块加载
- `src/Client/Desktop/Shell/Services/Auth/` - 认证服务实现

**Desktop Modules（新增）**
- `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/` - 前台角色模块

**Server**
- `src/Server/Modules/LYBT.Module.Auth/Services/JwtService.cs` - JWT服务
- `src/Server/Modules/LYBT.Module.Auth/Services/TokenService.cs` - Token管理
- `src/Server/Core/LYBT.Infrastructure/Services/Security/` - 安全服务

**Shared**
- `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs` - 角色枚举

### Breaking Changes（直接重构，不保留兼容）
- `ILoginCoordinator` 接口重新设计
- `LoginStateMachine` 和 `LoginFlowState` 直接删除，由 `AuthenticationStateMachine` 替代
- Token有效期从30分钟调整为15分钟

### Migration Notes
1. 部署后所有用户需重新登录（Token家族机制要求）
2. 新角色`Receptionist`需要数据库迁移添加对应权限记录
3. 配置文件需添加角色-模块映射配置项

## Technical References

### Industry Best Practices Applied
- **JWT有效期**：Access Token 15分钟，Refresh Token 7天（参考OWASP）
- **Token存储**：Refresh Token使用HttpOnly + Secure属性
- **重放攻击防护**：实现Token家族追踪（参考OAuth 2.0 Security Best Current Practice）
- **角色权限**：采用Policy-based Authorization（参考ASP.NET Core文档）

### Reference Documents
- [Microsoft: Configure JWT bearer authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [Microsoft: Role-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles)
- [OAuth 2.0 Security Best Current Practice (RFC 9700)](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| 状态机重构导致登录异常 | High | 充分的单元测试和集成测试覆盖 |
| Token有效期缩短影响用户体验 | Medium | 实现静默刷新机制，用户无感知 |
| 新角色模块集成失败 | Low | Receptionist模块独立，不影响现有功能 |
| 部署后需要全员重新登录 | Low | 提前通知用户，选择低峰期部署 |

## Success Criteria

1. 所有认证流程100%使用async/await，无阻塞调用
2. 新增角色只需修改配置+添加模块，无需修改核心代码
3. Token安全评分达到OWASP推荐标准
4. 单元测试覆盖率 > 80%（认证相关代码）
5. 现有用户无感知迁移，零登录失败

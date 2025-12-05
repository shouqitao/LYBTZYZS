# Tasks: refactor-auth-user-integration

## Phase 1: 统一错误码体系 ✅ COMPLETED

- [x] 1.1 创建AuthErrorCode枚举
  - [x] 1.1.1 在`LYBT.Shared.Models/Enums/AuthEnums.cs`添加`AuthErrorCode`枚举
  - [x] 1.1.2 定义认证错误(1xx)、Token错误(2xx)、会话错误(3xx)、系统错误(9xx)

- [x] 1.2 创建AuthResult响应类
  - [x] 1.2.1 在`LYBT.Shared.Models/Contracts/Auth/AuthResult.cs`添加AuthResult类
  - [x] 1.2.2 包含ErrorCode、Message、Data属性
  - [x] 1.2.3 扩展Result<T>添加可选ErrorCode属性保持兼容

- [x] 1.3 更新Server端AuthService
  - [x] 1.3.1 LoginAsync返回结构化错误码
  - [x] 1.3.2 LogoutAsync返回结构化错误码
  - [x] 1.3.3 RefreshTokenAsync返回结构化错误码
  - [x] 1.3.4 ValidateTokenAsync返回结构化错误码
  - [x] 1.3.5 GetSessionInfoAsync返回结构化错误码
  - [x] 1.3.6 添加UserDisabled检查

- [x] 1.4 更新AuthController错误处理
  - [x] 1.4.1 在BaseApiController添加HandleAuthResult方法
  - [x] 1.4.2 根据ErrorCode返回正确HTTP状态码(401/500/503)
  - [x] 1.4.3 响应体包含errorCode和numericCode

## Phase 2: Token重放攻击检测 ✅ COMPLETED

- [x] 2.1 扩展RefreshToken实体
  - [x] 2.1.1 添加`IsUsed`属性 (bool)
  - [x] 2.1.2 添加`UsedAt`属性 (DateTime?)
  - [x] 2.1.3 添加`MarkAsUsed()`方法
  - [x] 2.1.4 添加`IsReplayAttack`计算属性
  - [x] 2.1.5 更新IsValid()和IsActive检查IsUsed状态

- [x] 2.2 创建数据库迁移
  - [x] 2.2.1 生成迁移脚本 `Issue1864_AddTokenReplayDetection`
  - [x] 2.2.2 添加FamilyId索引
  - [x] 2.2.3 向后兼容(默认值IsUsed=false)

- [x] 2.3 实现重放检测逻辑
  - [x] 2.3.1 RefreshTokenAsync检查IsUsed状态
  - [x] 2.3.2 实现RevokeTokenFamilyAsync方法
  - [x] 2.3.3 检测到重放时使整个Family失效
  - [x] 2.3.4 记录TokenReplayAttack安全审计日志

- [x] 2.4 Token轮换使用MarkAsUsed
  - [x] 2.4.1 LoginAsync生成新FamilyId
  - [x] 2.4.2 RefreshTokenAsync使用MarkAsUsed标记旧Token
  - [x] 2.4.3 新Token继承原FamilyId

## Phase 3: 登出流程重构 ✅ COMPLETED

- [x] 3.1 修改Server端登出API
  - [x] 3.1.1 AuthController.LogoutAsync改为`[AllowAnonymous]`
  - [x] 3.1.2 修改LogoutRequest.Username为可选
  - [x] 3.1.3 使用RefreshToken作为主要登出凭据
  - [x] 3.1.4 登出时撤销整个Token Family

- [x] 3.2 更新Client端登出逻辑
  - [x] 3.2.1 AuthenticationService.LogoutAsync移除Token过期检查
  - [x] 3.2.2 始终发送RefreshToken到服务端
  - [x] 3.2.3 确保服务端会话被清理

- [x] 3.3 单元测试
  - [x] 3.3.1 测试Token过期时登出成功
  - [x] 3.3.2 测试RefreshToken无效时登出成功
  - [x] 3.3.3 测试服务端会话被正确清理

## Phase 4: Auth/User职责分离 ✅ COMPLETED

- [x] 4.1 重构密码验证逻辑
  - [x] 4.1.1 IUserService添加ValidatePasswordAsync方法
  - [x] 4.1.2 UserService实现密码验证
  - [x] 4.1.3 AuthService.VerifyCredentialsAsync调用IUserService

- [x] 4.2 重构密码修改流程
  - [x] 4.2.1 UserService实现ChangePasswordAsync
  - [x] 4.2.2 AuthService仅负责会话失效通知
  - [x] 4.2.3 更新UserController端点

## Phase 5: 客户端Token生命周期管理 ✅ COMPLETED

- [x] 5.1 创建TokenLifecycleService
  - [x] 5.1.1 定义ITokenLifecycleService接口
  - [x] 5.1.2 实现状态机(NotAuth/Active/Warning/Expired)
  - [x] 5.1.3 发布状态变更事件(TokenLifecycleStateChangedEvent)

- [x] 5.2 集成到Desktop Shell
  - [x] 5.2.1 MainWindowViewModel订阅状态事件
  - [x] 5.2.2 Warning状态显示会话即将过期对话框
  - [x] 5.2.3 Expired状态自动导航到登录页

- [x] 5.3 实现Token主动刷新
  - [x] 5.3.1 Active状态下监控Token剩余时间
  - [x] 5.3.2 剩余5分钟时自动刷新(用户活跃时)
  - [x] 5.3.3 刷新失败转为Warning状态

## Phase 6: 测试与验证 ✅ COMPLETED

- [x] 6.1 单元测试
  - [x] 6.1.1 AuthService所有方法测试 (81测试通过)
  - [x] 6.1.2 UserService密码相关测试 (31测试通过)
  - [x] 6.1.3 TokenLifecycleService状态转换测试

- [x] 6.2 集成测试
  - [x] 6.2.1 完整登录-操作-登出流程
  - [x] 6.2.2 Token过期后重新登录流程
  - [x] 6.2.3 Token重放攻击检测验证

- [x] 6.3 手动测试
  - [x] 6.3.1 Desktop端正常登录登出
  - [x] 6.3.2 会话超时警告对话框
  - [x] 6.3.3 多实例同时登录行为

## Implementation Summary

### Completed Changes (Phase 1-6)

| 文件 | 操作 | 说明 |
|------|------|------|
| `LYBT.Shared.Models/Enums/AuthEnums.cs` | MODIFY | 添加AuthErrorCode枚举 |
| `LYBT.Shared.Models/Contracts/Auth/AuthResult.cs` | ADD | 新增AuthResult类 |
| `LYBT.Shared.Models/Common/Result.cs` | MODIFY | 添加可选ErrorCode属性 |
| `LYBT.Shared.Models/Contracts/Auth/LogoutRequest.cs` | MODIFY | UserName改为可选(统一命名) |
| `LYBT.Desktop.Foundation/Security/AuthenticationService.cs` | MODIFY | 同步更新UserName字段引用 |
| `LYBT.Entities/Auth/RefreshToken.cs` | MODIFY | 添加IsUsed/UsedAt/MarkAsUsed |
| `LYBT.Infrastructure/Data/Configurations/RefreshTokenConfiguration.cs` | MODIFY | 添加FamilyId索引 |
| `LYBT.Infrastructure/Data/Migrations/Issue1864_AddTokenReplayDetection.cs` | ADD | 数据库迁移 |
| `LYBT.Infrastructure/Web/BaseApiController.cs` | MODIFY | 添加HandleAuthResult方法 |
| `LYBT.Module.Auth/Services/AuthService.cs` | MODIFY | 结构化错误码+重放检测+登出重构 |
| `LYBT.WebAPI/Controllers/AuthController.cs` | MODIFY | 使用HandleAuthResult+AllowAnonymous登出 |
| `LYBT.Module.Users/Interfaces/IUserService.cs` | MODIFY | 添加ValidatePasswordAsync方法 |
| `LYBT.Module.Users/Services/UserService.cs` | MODIFY | 实现ValidatePasswordAsync |
| `LYBT.Desktop.Foundation/Security/ITokenLifecycleService.cs` | ADD | Token生命周期服务接口 |
| `LYBT.Desktop.Foundation/Security/TokenLifecycleService.cs` | ADD | Token生命周期服务实现 |
| `LYBT.Desktop.Foundation/Security/TokenLifecycleState.cs` | ADD | 状态枚举 |
| `LYBT.Desktop.Foundation/Security/TokenLifecycleStateChangedEvent.cs` | ADD | Prism事件 |
| `LYBT.Desktop.Shell/Extensions/ServiceCollectionExtensions.cs` | MODIFY | 注册TokenLifecycleService |
| `LYBT.Desktop.Shell/ViewModels/MainWindowViewModel.cs` | MODIFY | 集成Token生命周期监控 |
| `LYBT.Module.Auth.Tests/Services/AuthServiceTests.cs` | MODIFY | 更新测试以匹配新实现 |

## Dependencies

```
Phase 1 (错误码体系) ✅
    ↓
Phase 2 (重放检测) ✅ ←── 需要数据库迁移 ✅
    ↓
Phase 3 (登出重构) ✅
    ↓
Phase 4 (职责分离) ✅
    ↓
Phase 5 (客户端生命周期) ✅
    ↓
Phase 6 (测试验证) ✅
```

## Verification Results

- **构建状态**: 0错误，0警告
- **Auth模块测试**: 81/81 通过
- **Users模块测试**: 31/31 通过
- **完成日期**: 2025-12-05

# Token认证安全重构 任务分解文档

## 📋 元数据
- **Epic**: Token认证安全重构
- **触发Issue**: #1861
- **ADR**: ADR-011 - Token认证安全重构与SuperAdmin统一认证
- **需求文档**: [token-authentication-security-requirements.md](../explanation/requirements/token-authentication-security-requirements.md)
- **设计文档**: [token-authentication-security-design.md](../explanation/design/token-authentication-security-design.md)
- **总工作量**: 38-50小时
- **实施周期**: 5-7个工作日
- **实施阶段**: Phase 1-3

---

## 🎯 任务清单（Task Checklist）

### Phase 1: Client端重构（Day 1-2，预计14-18小时）

#### Task 1.1: 实现SecureTokenStorage加密存储
- **工作量**: 3-4小时
- **依赖**: 无
- **类型**: Infrastructure / Security
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 新建: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/Security/SecureTokenStorage.cs`
  - 新建: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/Security/ITokenStorage.cs`
  - 修改: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj`（添加System.Security引用）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] `SaveTokenAsync`成功使用DPAPI加密存储到%LOCALAPPDATA%\LYBTZYZS\tokens.dat
  - [ ] `LoadTokenAsync`成功解密并反序列化UserSession
  - [ ] DPAPI失败时降级为明文存储并记录警告日志
  - [ ] 单元测试通过：加密→解密→验证数据一致性
  - [ ] 单元测试通过：DPAPI降级测试（模拟CryptographicException）
- **技术要点**:
  - 使用`ProtectedData.Protect/Unprotect`，`DataProtectionScope.CurrentUser`
  - 文件路径：`Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "LYBTZYZS", "tokens.dat")`
  - JSON序列化：`System.Text.Json.JsonSerializer`
  - 异常处理：捕获`CryptographicException`，记录日志后降级
  - 依赖注入：注册为Singleton（`services.AddSingleton<ITokenStorage, SecureTokenStorage>()`）

#### Task 1.2: 实现LocalTokenValidator JWT自验证
- **工作量**: 3-4小时
- **依赖**: 无（与Task 1.1并行）
- **类型**: Infrastructure / Security
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 新建: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/Security/LocalTokenValidator.cs`
  - 新建: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/Security/TokenValidationResult.cs`
  - 修改: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj`（添加System.IdentityModel.Tokens.Jwt引用）
  - 修改: `src/Client/Desktop/App.xaml.cs`（配置appsettings.json读取）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] `ValidateToken`成功验证有效Token，返回UserSession
  - [ ] `ValidateToken`正确拒绝过期Token（返回IsValid=false）
  - [ ] `ValidateToken`正确拒绝签名无效Token
  - [ ] `ValidateToken`正确拒绝Issuer/Audience不匹配Token
  - [ ] 单元测试通过：有效Token、过期Token、签名无效、缺少Claims
  - [ ] ClockSkew设置为5分钟容差
- **技术要点**:
  - 使用`JwtSecurityTokenHandler`
  - `TokenValidationParameters`配置：ValidateIssuerSigningKey, ValidateIssuer, ValidateAudience, ValidateLifetime
  - `SymmetricSecurityKey`从appsettings.json读取`Lybt:Jwt:SecretKey`
  - 提取Claims：sub (UserId), name (UserName), role (Role), user_type (UserType)
  - 依赖注入：注册为Singleton

#### Task 1.3: 重构AuthenticationService集成本地验证
- **工作量**: 2-3小时
- **依赖**: Task 1.1, Task 1.2
- **类型**: Service Refactoring
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 修改: `src/Client/Desktop/Foundation/LYBT.Desktop.Foundation/Services/AuthenticationService.cs`
  - 删除: `AuthenticationService.ValidateTokenAsync(string token)` - 移除Server API调用
  - 修改: `AuthenticationService.ValidateAndRestoreSessionAsync()` - 使用LocalTokenValidator
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 移除对`IAuthApi.ValidateTokenAsync` POST端点的所有调用
  - [ ] `ValidateAndRestoreSessionAsync`使用`LocalTokenValidator`进行本地验证
  - [ ] AccessToken过期时自动调用`RefreshTokenAsync`
  - [ ] RefreshToken也过期时清除本地Token并返回失败
  - [ ] 单元测试通过：有效Token恢复会话、AccessToken过期自动刷新、RefreshToken过期清除
- **技术要点**:
  - 构造函数注入`LocalTokenValidator`和`ITokenStorage`
  - 验证失败不立即清除，先尝试刷新
  - 使用`SecureTokenStorage`保存刷新后的新Token

#### Task 1.4: 实现Token清理逻辑（迁移策略）
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1
- **类型**: Migration / Cleanup
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 修改: `src/Client/Desktop/App.xaml.cs`
  - 新增方法: `ClearLegacyTokensAsync()`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 应用启动时自动清除`%LOCALAPPDATA%\LYBTZYZS\tokens.dat`（如果存在）
  - [ ] 记录日志："已清除旧Token文件（系统安全升级）"
  - [ ] 启动后导航到登录页面（因为Token已清除）
  - [ ] 手动测试通过：启动→清除Token→显示登录页
- **技术要点**:
  - 在`OnStartup`中调用`ClearLegacyTokensAsync()`
  - 使用`File.Exists`和`File.Delete`
  - 记录INFO级别日志
  - 仅执行一次（启动时），不影响正常运行

#### Task 1.5: 编写Client端单元测试
- **工作量**: 3-4小时
- **依赖**: Task 1.1, Task 1.2, Task 1.3
- **类型**: Test
- **优先级**: 🟡 中（质量保证）
- **文件范围**:
  - 新建: `tests/UnitTests/Client/Foundation/LYBT.Desktop.Foundation.Tests/Security/SecureTokenStorageTests.cs`
  - 新建: `tests/UnitTests/Client/Foundation/LYBT.Desktop.Foundation.Tests/Security/LocalTokenValidatorTests.cs`
  - 新建: `tests/UnitTests/Client/Foundation/LYBT.Desktop.Foundation.Tests/Services/AuthenticationServiceTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 所有测试用例通过（至少15个测试）
  - [ ] SecureTokenStorage测试：加密、解密、DPAPI降级、文件不存在
  - [ ] LocalTokenValidator测试：有效Token、过期Token、签名无效、缺少Claims、ClockSkew
  - [ ] AuthenticationService测试：本地验证成功、自动刷新、清除过期Token
  - [ ] 使用NSubstitute Mock依赖服务
- **技术要点**:
  - AAA模式：Arrange-Act-Assert
  - FluentAssertions用于断言
  - Mock ILogger, ITokenStorage, IAuthApi
  - 生成测试用Token使用真实JwtSecurityTokenHandler

#### Task 1.6: Client端集成测试
- **工作量**: 2-3小时
- **依赖**: Task 1.5
- **类型**: Integration Test
- **优先级**: 🟡 中（质量保证）
- **文件范围**:
  - 新建: `tests/IntegrationTests/Client/Foundation/AuthenticationIntegrationTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 端到端测试通过：登录→加密存储→重启应用→恢复会话
  - [ ] Token刷新测试通过：AccessToken过期→自动调用RefreshTokenAsync
  - [ ] 迁移测试通过：旧Token文件存在→启动清除→导航登录页
- **技术要点**:
  - 使用真实`SecureTokenStorage`和`LocalTokenValidator`
  - Mock Server API（IAuthApi）
  - 模拟应用重启（重新创建服务容器）

---

### Phase 2: Server端重构（Day 3-4，预计14-18小时）

#### Task 2.1: 创建数据库迁移（RefreshTokens表）
- **工作量**: 1-1.5小时
- **依赖**: 无
- **类型**: Database Migration
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 新建: `src/Server/Core/LYBT.Infrastructure/Migrations/20251107XXXXXX_AddRevokedFieldsToRefreshToken.cs`
  - 修改: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`（如果需要）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 迁移文件成功创建
  - [ ] `dotnet ef database update`执行成功
  - [ ] RefreshTokens表新增字段：`IsRevoked` (BIT), `RevokedAt` (DATETIME2), `RevokeReason` (NVARCHAR(500))
  - [ ] 创建索引：`IX_RefreshTokens_IsRevoked_Token`
  - [ ] 提供回滚脚本（Down方法）
- **技术要点**:
  - EF Core命令：`dotnet ef migrations add AddRevokedFieldsToRefreshToken -p LYBT.Infrastructure`
  - SQL脚本：
    ```sql
    ALTER TABLE RefreshTokens ADD IsRevoked BIT NOT NULL DEFAULT 0;
    ALTER TABLE RefreshTokens ADD RevokedAt DATETIME2 NULL;
    ALTER TABLE RefreshTokens ADD RevokeReason NVARCHAR(500) NULL;

    CREATE INDEX IX_RefreshTokens_IsRevoked_Token
    ON RefreshTokens(IsRevoked, Token)
    INCLUDE (UserId, UserType, ExpiresAt);
    ```
  - 测试：使用test数据库验证迁移

#### Task 2.2: 创建数据库迁移（SecurityAuditLogs表）
- **工作量**: 1-1.5小时
- **依赖**: 无（与Task 2.1并行）
- **类型**: Database Migration
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 新建: `src/Server/Core/LYBT.Infrastructure/Migrations/20251107XXXXXX_CreateSecurityAuditLogsTable.cs`
  - 新建: `src/Server/Core/LYBT.Entities/SecurityAuditLog.cs`
  - 修改: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`（添加DbSet）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 迁移文件成功创建
  - [ ] `dotnet ef database update`执行成功
  - [ ] SecurityAuditLogs表创建成功，包含所有必需字段
  - [ ] 创建索引：`IX_SecurityAuditLogs_EventType_CreatedAt`, `IX_SecurityAuditLogs_UserId_CreatedAt`
  - [ ] 提供回滚脚本
- **技术要点**:
  - Entity定义：Id, EventType, UserId, UserType, UserName, IpAddress, UserAgent, Success, ErrorMessage, Metadata (JSON), CreatedAt
  - SQL脚本：
    ```sql
    CREATE TABLE SecurityAuditLogs (
        Id UNIQUEIDENTIFIER PRIMARY KEY,
        EventType NVARCHAR(50) NOT NULL,
        UserId UNIQUEIDENTIFIER NULL,
        UserType NVARCHAR(50) NULL,
        UserName NVARCHAR(256) NULL,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(500) NULL,
        Metadata NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    ```

#### Task 2.3: 实现TokenRevocationService
- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Service
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 新建: `src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs`
  - 新建: `src/Server/Modules/LYBT.Module.Auth/Interfaces/ITokenRevocationService.cs`
  - 修改: `src/Server/Modules/LYBT.Module.Auth/ServiceCollectionExtensions.cs`（注册服务）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] `RevokeTokenAsync`成功将单个RefreshToken标记为撤销
  - [ ] `RevokeAllUserTokensAsync`成功批量撤销用户所有Token
  - [ ] `IsTokenRevokedAsync`正确查询Token撤销状态
  - [ ] 撤销操作记录审计日志（EventType: TokenRevoked）
  - [ ] 单元测试通过：撤销单个、撤销所有、查询状态
- **技术要点**:
  - 依赖注入：AppDbContext, ISecurityAuditService, ILogger
  - 撤销逻辑：更新`IsRevoked=true`, `RevokedAt=DateTime.UtcNow`, `RevokeReason`
  - 批量撤销：使用`Where(t => t.UserId == userId && !t.IsRevoked)`
  - 审计日志：记录UserId, UserType, RevokeReason

#### Task 2.4: 实现SecurityAuditService
- **工作量**: 3-4小时
- **依赖**: Task 2.2
- **类型**: Service
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 新建: `src/Server/Modules/LYBT.Module.Auth/Services/SecurityAuditService.cs`
  - 新建: `src/Server/Modules/LYBT.Module.Auth/Interfaces/ISecurityAuditService.cs`
  - 新建: `src/Server/Modules/LYBT.Module.Auth/Models/SecurityAuditEvent.cs`
  - 修改: `src/Server/Modules/LYBT.Module.Auth/ServiceCollectionExtensions.cs`（注册服务）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] `LogAsync`成功异步记录审计事件
  - [ ] 自动提取HttpContext信息（IP地址、UserAgent）
  - [ ] IP地址脱敏（192.168.1.* → 192.168.1.*）
  - [ ] UserAgent截断（最大500字符）
  - [ ] 单元测试通过：记录事件、脱敏、异常处理
- **技术要点**:
  - 依赖注入：AppDbContext, ILogger, IHttpContextAccessor
  - 异步记录：不阻塞主流程
  - 脱敏算法：`MaskIpAddress`（保留前3段）, `TruncateUserAgent`
  - EventType枚举：Login, Logout, RefreshToken, TokenRevoked, LoginFailed

#### Task 2.5: 修改AuthService集成撤销检查
- **工作量**: 2-3小时
- **依赖**: Task 2.3, Task 2.4
- **类型**: Service Refactoring
- **优先级**: 🔴 高（关键路径）
- **文件范围**:
  - 修改: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
  - 修改: `AuthService.LoginAsync` - 集成审计日志
  - 修改: `AuthService.RefreshTokenAsync` - 检查撤销状态 + 记录审计日志
  - 修改: `AuthService.LogoutAsync` - 撤销Token + 记录审计日志
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] `LoginAsync`成功后记录审计日志（EventType: Login）
  - [ ] `LoginAsync`失败后记录审计日志（EventType: LoginFailed）
  - [ ] `RefreshTokenAsync`检查`IsRevoked`，如果已撤销则拒绝刷新
  - [ ] `RefreshTokenAsync`成功后自动撤销旧RefreshToken（Token轮换）
  - [ ] `RefreshTokenAsync`记录审计日志（EventType: RefreshToken）
  - [ ] `LogoutAsync`撤销RefreshToken + 记录审计日志（EventType: Logout）
  - [ ] 单元测试通过：刷新已撤销Token失败、Token轮换、审计日志完整
- **技术要点**:
  - 构造函数注入：ITokenRevocationService, ISecurityAuditService
  - RefreshTokenAsync逻辑：
    ```csharp
    if (tokenRecord.IsRevoked)
    {
        await _auditService.LogAsync(new SecurityAuditEvent { EventType = "RefreshTokenRejected", ... });
        return Failure("Token已撤销");
    }
    // ... 刷新逻辑 ...
    // Token轮换：撤销旧Token
    tokenRecord.IsRevoked = true;
    tokenRecord.RevokeReason = "Token轮换（自动撤销）";
    ```

#### Task 2.6: 实现SecurityAuditCleanupService后台Job
- **工作量**: 2-3小时
- **依赖**: Task 2.4
- **类型**: Background Service
- **优先级**: 🟡 中（功能增强）
- **文件范围**:
  - 新建: `src/Server/Api/LYBT.Api/BackgroundServices/SecurityAuditCleanupService.cs`
  - 修改: `src/Server/Api/LYBT.Api/Program.cs`（注册Hosted Service）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 后台服务每24小时执行一次（凌晨3点）
  - [ ] 清理30天前的审计日志（`CreatedAt < DateTime.UtcNow.AddDays(-30)`）
  - [ ] 记录清理日志："清理了X条审计日志"
  - [ ] 应用关闭时优雅停止（CancellationToken）
  - [ ] 单元测试通过：清理逻辑、定时触发
- **技术要点**:
  - 继承`BackgroundService`
  - 使用`PeriodicTimer`（.NET 6+）或`Task.Delay`循环
  - 执行时间：凌晨3点（`DateTime.Today.AddHours(3)`）
  - 依赖注入：使用`IServiceScopeFactory`创建Scope

#### Task 2.7: 移除Server端废弃代码
- **工作量**: 1-1.5小时
- **依赖**: Task 2.5
- **类型**: Code Cleanup
- **优先级**: 🟡 中（代码清理）
- **文件范围**:
  - 修改: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
    - 删除: `ValidateTokenWithDetailsAsync` 方法
  - 修改: `src/Server/Modules/LYBT.Module.Auth/Controllers/AuthController.cs`
    - 删除: `POST /api/v1/auth/validate` 端点（ValidateTokenFromBodyAsync）
    - 保留: `GET /api/v1/auth/validate` 端点（ValidateTokenFromHeaderAsync，用于需要Server状态检查的场景）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] POST `/api/v1/auth/validate`端点已移除
  - [ ] 相关单元测试已更新或移除
  - [ ] Swagger文档不再显示POST `/api/v1/auth/validate`
  - [ ] GET `/api/v1/auth/validate`端点保留并正常工作
- **技术要点**:
  - 使用Git blame确认无其他地方调用这些方法
  - 更新单元测试：删除对ValidateTokenWithDetailsAsync的测试

#### Task 2.8: 编写Server端单元测试
- **工作量**: 3-4小时
- **依赖**: Task 2.3, Task 2.4, Task 2.5
- **类型**: Test
- **优先级**: 🟡 中（质量保证）
- **文件范围**:
  - 新建: `tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/Services/TokenRevocationServiceTests.cs`
  - 新建: `tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/Services/SecurityAuditServiceTests.cs`
  - 修改: `tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/Services/AuthServiceTests.cs`（更新RefreshTokenAsync测试）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 所有测试用例通过（至少20个测试）
  - [ ] TokenRevocationService测试：撤销单个、撤销所有、查询状态、Token不存在
  - [ ] SecurityAuditService测试：记录日志、脱敏、异常处理
  - [ ] AuthService测试：刷新已撤销Token失败、Token轮换、审计日志记录
  - [ ] 使用In-Memory SQLite数据库（真实DbContext）
- **技术要点**:
  - AAA模式
  - FluentAssertions
  - In-Memory SQLite：`UseSqlite("DataSource=:memory:")`
  - Mock ILogger, IHttpContextAccessor

#### Task 2.9: Server端集成测试
- **工作量**: 2-3小时
- **依赖**: Task 2.8
- **类型**: Integration Test
- **优先级**: 🟡 中（质量保证）
- **文件范围**:
  - 新建: `tests/IntegrationTests/Server/Auth/TokenRevocationIntegrationTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] API集成测试通过：撤销Token→刷新失败（401 Unauthorized）
  - [ ] 审计日志集成测试通过：登录→查询数据库→验证日志存在
  - [ ] Token轮换集成测试通过：刷新Token→旧Token被撤销→新Token可用
- **技术要点**:
  - 使用`WebApplicationFactory<Program>`
  - 真实SQL Server Test数据库
  - HTTP客户端调用API
  - 查询数据库验证状态

---

### Phase 3: 集成测试与验收（Day 5-7，预计10-14小时）

#### Task 3.1: 端到端功能测试
- **工作量**: 3-4小时
- **依赖**: Task 1.6, Task 2.9
- **类型**: E2E Test
- **优先级**: 🔴 高（质量保证）
- **文件范围**:
  - 新建: `tests/E2ETests/TokenAuthenticationE2ETests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试通过：登录→加密存储→重启应用→本地验证→恢复会话
  - [ ] 测试通过：AccessToken过期→自动刷新→旧RefreshToken被撤销
  - [ ] 测试通过：撤销Token→刷新失败→清除本地Token→跳转登录页
  - [ ] 测试通过：所有认证事件正确记录审计日志
  - [ ] 测试通过：审计日志包含IP地址（脱敏）、UserAgent（截断）
- **技术要点**:
  - 启动真实Server和Client
  - 使用真实数据库
  - 模拟用户操作：登录→关闭→重启
  - 验证数据库状态：RefreshTokens.IsRevoked, SecurityAuditLogs记录

#### Task 3.2: 安全测试
- **工作量**: 2-3小时
- **依赖**: Task 3.1
- **类型**: Security Test
- **优先级**: 🔴 高（安全验证）
- **文件范围**:
  - 新建: `tests/SecurityTests/TokenSecurityTests.cs`
- **验收标准**:
  - [ ] DPAPI加密验证：tokens.dat文件无法用文本编辑器读取有效信息
  - [ ] Token签名验证：篡改Token payload→验证失败
  - [ ] Token过期验证：修改系统时间→Token过期→验证失败
  - [ ] 撤销响应速度：撤销Token→< 1秒生效（刷新立即失败）
  - [ ] 敏感信息脱敏检查：数据库中IP地址已脱敏、UserAgent已截断
  - [ ] 测试报告生成：markdown格式，记录所有安全测试结果
- **技术要点**:
  - 手动测试 + 自动化测试结合
  - 使用文本编辑器打开tokens.dat验证加密
  - 使用JWT debugger（jwt.io）验证签名
  - 查询数据库验证脱敏规则

#### Task 3.3: 性能测试
- **工作量**: 1-2小时
- **依赖**: Task 3.1
- **类型**: Performance Test
- **优先级**: 🟡 中（性能验证）
- **文件范围**:
  - 新建: `tests/PerformanceTests/TokenValidationPerformanceTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 本地Token验证平均耗时 < 10ms（1000次测试）
  - [ ] Token加密存储平均耗时 < 50ms（100次测试）
  - [ ] 应用启动增量 < 500ms（加载Token并验证）
  - [ ] 审计日志异步记录不阻塞请求（< 5ms overhead）
  - [ ] 性能测试报告生成
- **技术要点**:
  - 使用`BenchmarkDotNet`或`Stopwatch`
  - 多次运行取平均值
  - 对比重构前后性能（Server验证 vs 本地验证）

#### Task 3.4: 更新文档
- **工作量**: 2-3小时
- **依赖**: Task 3.3
- **类型**: Documentation
- **优先级**: 🟢 低（文档完善）
- **文件范围**:
  - 修改: `docs/how-to/api-reference.md`（更新API端点）
  - 修改: `docs/how-to/authentication-guide.md`（更新认证流程说明）
  - 修改: `docs/explanation/architecture/shared/authentication-architecture.md`（更新架构图）
  - 新建: `docs/how-to/token-security-guide.md`（Token安全使用指南）
  - 修改: `CHANGELOG.md`（添加v1.x.x版本说明）
- **验收标准**:
  - [ ] API文档已更新：移除POST /api/v1/auth/validate，保留GET端点
  - [ ] 认证流程图已更新：反映本地验证流程
  - [ ] 架构图已更新：包含LocalTokenValidator和SecureTokenStorage
  - [ ] Token安全指南已创建：包含DPAPI、撤销、审计日志说明
  - [ ] CHANGELOG已更新：记录所有安全改进
  - [ ] 所有文档链接有效
- **技术要点**:
  - 使用Mermaid绘制架构图
  - Markdown格式
  - 截图使用PNG格式（如果需要）

#### Task 3.5: 准备发布
- **工作量**: 1-2小时
- **依赖**: Task 3.4
- **类型**: Release Preparation
- **优先级**: 🟢 低（发布准备）
- **文件范围**:
  - 新建: `docs/release-notes/token-authentication-security-refactor.md`
  - 修改: `README.md`（如果需要）
- **验收标准**:
  - [ ] 发布说明已创建：包含变更摘要、用户影响、升级指南
  - [ ] 用户通知文案已准备："系统安全升级，首次启动需重新登录"
  - [ ] 数据库迁移脚本已验证（Up + Down）
  - [ ] 回滚计划已准备（如果迁移失败）
  - [ ] 所有测试通过（单元测试 + 集成测试 + E2E测试）
- **技术要点**:
  - 发布说明模板：变更摘要、新功能、破坏性变更、升级步骤、回滚步骤
  - 用户通知：友好的语言，解释为什么需要重新登录

#### Task 3.6: 更新Issue #1861
- **工作量**: 0.5小时
- **依赖**: Task 3.5
- **类型**: Issue Management
- **优先级**: 🟢 低（追溯管理）
- **文件范围**:
  - GitHub Issue #1861
- **验收标准**:
  - [ ] Issue #1861添加评论：说明该Issue触发了系统性安全重构
  - [ ] 关联新的Epic或Issues（如果有）
  - [ ] 标记为"被新方案替代"或"已升级为Epic"
  - [ ] 不直接关闭，保留历史记录供追溯
- **技术要点**:
  - 使用GitHub UI或gh CLI
  - 评论模板：
    ```markdown
    该Issue在修复过程中暴露了架构层面的安全隐患，已升级为系统性Token认证安全重构。

    **相关文档**：
    - ADR-011: Token认证安全重构与SuperAdmin统一认证
    - 需求文档: docs/explanation/requirements/token-authentication-security-requirements.md
    - 设计文档: docs/explanation/design/token-authentication-security-design.md

    **重构范围**：
    - ✅ Token加密存储（DPAPI）
    - ✅ Client端JWT自验证
    - ✅ RefreshToken撤销机制
    - ✅ 安全审计日志
    - ✅ SuperAdmin统一认证（方案C）

    本Issue的核心问题（Token验证返回null Username）已在重构中完整解决。
    ```

---

## 📊 任务统计

- **总任务数**: 21个
- **总工作量**: 38-50小时
- **Phase数量**: 3个阶段
- **关键路径长度**: 9个任务
- **并行任务**: 4组（可加速实施）

### Phase工作量分布

| Phase | 任务数 | 工作量（小时） | 占比 |
|-------|-------|--------------|------|
| Phase 1: Client端重构 | 6个 | 14-18 | 37% |
| Phase 2: Server端重构 | 9个 | 14-18 | 37% |
| Phase 3: 测试与验收 | 6个 | 10-14 | 26% |

### 任务类型分布

| 类型 | 任务数 | 占比 |
|------|-------|------|
| Infrastructure/Security | 5个 | 24% |
| Service | 4个 | 19% |
| Test | 6个 | 29% |
| Database Migration | 2个 | 10% |
| Documentation | 2个 | 10% |
| Other | 2个 | 10% |

---

## 🔗 依赖关系图

### Phase 1依赖关系
```
Task 1.1 (SecureTokenStorage) ─┐
                               ├──> Task 1.3 (重构AuthenticationService)
Task 1.2 (LocalTokenValidator) ─┘          │
                                           ├──> Task 1.5 (单元测试) ──> Task 1.6 (集成测试)
Task 1.1 ──> Task 1.4 (Token清理)         │
```

**并行机会**：
- Task 1.1 和 Task 1.2 可以并行（不同开发者）
- Task 1.4 可以在 Task 1.1 完成后立即开始，不影响 Task 1.3

### Phase 2依赖关系
```
Task 2.1 (RefreshTokens迁移) ──> Task 2.3 (TokenRevocationService) ─┐
                                                                    ├──> Task 2.5 (AuthService集成) ──> Task 2.7 (代码清理)
Task 2.2 (SecurityAuditLogs迁移) ──> Task 2.4 (SecurityAuditService) ─┤                               │
                                          │                             └──> Task 2.8 (单元测试) ──> Task 2.9 (集成测试)
                                          └──> Task 2.6 (后台Job)
```

**并行机会**：
- Task 2.1 和 Task 2.2 可以并行（两个独立迁移）
- Task 2.3 和 Task 2.4 可以并行（Task 2.5需要两者都完成）
- Task 2.6 可以在 Task 2.4 完成后独立进行，不阻塞 Task 2.5

### Phase 3依赖关系
```
Task 3.1 (E2E测试) ──> Task 3.2 (安全测试) ──┐
                  ├──> Task 3.3 (性能测试) ──┤
                                             ├──> Task 3.4 (文档) ──> Task 3.5 (发布准备) ──> Task 3.6 (Issue管理)
```

**并行机会**：
- Task 3.2 和 Task 3.3 可以并行（不同测试维度）

### 跨Phase依赖
```
Phase 1 (Task 1.6) ──> Phase 3 (Task 3.1)
Phase 2 (Task 2.9) ──> Phase 3 (Task 3.1)
```

**关键约束**：
- Phase 3 必须等待 Phase 1 和 Phase 2 都完成
- Task 3.1 是 Phase 3 的入口，依赖 Client端和Server端都完成

---

## ⚠️ 关键路径

### 主线任务（必须按顺序完成，9个任务）

```
1. Task 1.1: SecureTokenStorage加密存储
   ↓
2. Task 1.2: LocalTokenValidator JWT自验证
   ↓
3. Task 1.3: 重构AuthenticationService
   ↓
4. Task 2.1: RefreshTokens数据库迁移
   ↓
5. Task 2.3: TokenRevocationService
   ↓
6. Task 2.5: AuthService集成撤销检查
   ↓
7. Task 2.9: Server端集成测试
   ↓
8. Task 3.1: 端到端功能测试
   ↓
9. Task 3.2: 安全测试
```

**关键路径总工作量**: 20-26小时（约3-4个工作日）

### 并行任务（可同时进行）

**并行组1（Phase 1开始）**:
- Task 1.1: SecureTokenStorage
- Task 1.2: LocalTokenValidator

**并行组2（Phase 2开始）**:
- Task 2.1: RefreshTokens迁移
- Task 2.2: SecurityAuditLogs迁移

**并行组3（Phase 2中期）**:
- Task 2.3: TokenRevocationService
- Task 2.4: SecurityAuditService

**并行组4（Phase 3测试）**:
- Task 3.2: 安全测试
- Task 3.3: 性能测试

---

## 📝 实施建议

### 优先级排序

#### 🔴 最高优先级（关键路径，必须完成）
1. Task 1.1, 1.2, 1.3（Client端核心）
2. Task 2.1, 2.3, 2.5（Server端核心）
3. Task 3.1, 3.2（核心测试）

#### 🟡 高优先级（功能完整性）
1. Task 1.4（Token清理，用户体验）
2. Task 2.2, 2.4（审计日志，安全可追溯）
3. Task 2.7（代码清理，架构清晰）

#### 🟢 中优先级（质量保证）
1. Task 1.5, 1.6, 2.8, 2.9（单元测试和集成测试）
2. Task 2.6（后台Job，自动化清理）
3. Task 3.3（性能测试）

#### ⚪ 低优先级（文档和发布）
1. Task 3.4, 3.5, 3.6（文档和发布准备）

### 团队协作策略

#### 单人开发（5-7天）
- **Day 1**: Task 1.1 + Task 1.2（上午1.1，下午1.2）
- **Day 2**: Task 1.3 + Task 1.4 + Task 1.5开始
- **Day 3**: Task 1.5完成 + Task 2.1 + Task 2.2
- **Day 4**: Task 2.3 + Task 2.4
- **Day 5**: Task 2.5 + Task 2.8 + Task 2.9
- **Day 6**: Task 3.1 + Task 3.2
- **Day 7**: Task 3.3 + Task 3.4 + Task 3.5 + Task 3.6

#### 双人协作（3-4天）
- **开发者A（Client端）**: Task 1.1 → 1.2 → 1.3 → 1.4 → 1.5 → 1.6
- **开发者B（Server端）**: Task 2.1 → 2.2 → 2.3 → 2.4 → 2.5 → 2.6 → 2.7 → 2.8 → 2.9
- **合并后（共同）**: Task 3.1 → 3.2 → 3.3 → 3.4 → 3.5 → 3.6

### 风险提示

#### 高风险（需要特别关注）
- **DPAPI加密失败**：Task 1.1需要充分测试降级策略
- **数据库迁移失败**：Task 2.1, 2.2需要提前备份并准备回滚脚本
- **Token迁移用户体验**：Task 1.4需要友好的UI提示（"系统安全升级"）
- **审计日志性能**：Task 2.4异步记录必须不阻塞主流程

#### 中风险（需要监控）
- **RefreshToken撤销延迟**：Task 2.5需要确保撤销立即生效（< 1秒）
- **JWT验证性能**：Task 1.2需要性能测试（目标 < 10ms）
- **后台Job稳定性**：Task 2.6需要异常处理和重试机制

#### 低风险（常规处理）
- **单元测试覆盖率**：确保关键逻辑有测试覆盖
- **文档同步**：及时更新文档，避免遗漏

### 实施检查点

#### Checkpoint 1（Day 2结束）
- ✅ Client端核心功能完成（Task 1.1, 1.2, 1.3, 1.4）
- ✅ Client端单元测试通过（Task 1.5）
- ✅ Token加密存储验证成功（手动测试）
- ✅ JWT本地验证成功（手动测试）

**决策点**：如果Client端出现严重问题，暂停Phase 2，优先解决

#### Checkpoint 2（Day 4结束）
- ✅ Server端核心功能完成（Task 2.1-2.5）
- ✅ 数据库迁移成功
- ✅ Token撤销机制工作
- ✅ 审计日志记录成功

**决策点**：如果Server端出现严重问题，回滚数据库迁移

#### Checkpoint 3（Day 6结束）
- ✅ 所有单元测试和集成测试通过
- ✅ E2E测试通过（Task 3.1）
- ✅ 安全测试通过（Task 3.2）

**决策点**：如果测试失败，延期发布，优先修复

---

## 🧪 测试策略

### 单元测试策略

#### Client端单元测试（Task 1.5）
- **覆盖范围**：SecureTokenStorage, LocalTokenValidator, AuthenticationService
- **Mock对象**：ILogger, ITokenStorage, IAuthApi, IConfiguration
- **测试框架**：xUnit + FluentAssertions + NSubstitute
- **覆盖率目标**：≥ 80%

**关键测试用例**：
```
SecureTokenStorage:
1. SaveTokenAsync_Success_Encrypted
2. LoadTokenAsync_Success_Decrypted
3. SaveTokenAsync_DPAPIFails_FallbackToPlaintext
4. LoadTokenAsync_FileNotExists_ReturnsNull

LocalTokenValidator:
1. ValidateToken_ValidToken_ReturnsSuccess
2. ValidateToken_ExpiredToken_ReturnsFailed
3. ValidateToken_InvalidSignature_ReturnsFailed
4. ValidateToken_MissingClaims_ReturnsFailed
5. ValidateToken_ClockSkew_StillValid

AuthenticationService:
1. ValidateAndRestoreSessionAsync_ValidToken_RestoresSession
2. ValidateAndRestoreSessionAsync_AccessTokenExpired_AutoRefresh
3. ValidateAndRestoreSessionAsync_RefreshTokenExpired_ClearToken
```

#### Server端单元测试（Task 2.8）
- **覆盖范围**：TokenRevocationService, SecurityAuditService, AuthService
- **Mock对象**：ILogger, IHttpContextAccessor, ISecurityAuditService
- **数据库**：In-Memory SQLite（真实DbContext）
- **覆盖率目标**：≥ 85%

**关键测试用例**：
```
TokenRevocationService:
1. RevokeTokenAsync_TokenExists_Success
2. RevokeTokenAsync_TokenNotExists_Failure
3. RevokeAllUserTokensAsync_MultipleTokens_AllRevoked

SecurityAuditService:
1. LogAsync_Success_RecordedInDatabase
2. LogAsync_MaskIpAddress_Desensitized
3. LogAsync_TruncateUserAgent_MaxLength500

AuthService:
1. RefreshTokenAsync_RevokedToken_Failure
2. RefreshTokenAsync_Success_OldTokenRevoked
3. LoginAsync_Success_AuditLogRecorded
4. LoginAsync_Failure_AuditLogRecorded
```

### 集成测试策略

#### Client端集成测试（Task 1.6）
- **测试范围**：AuthenticationService + SecureTokenStorage + LocalTokenValidator
- **Mock对象**：仅Mock Server API（IAuthApi）
- **真实组件**：SecureTokenStorage（真实DPAPI），LocalTokenValidator（真实JWT验证）
- **测试场景**：
  1. 登录 → 加密存储 → 重启应用 → 恢复会话
  2. AccessToken过期 → 自动刷新
  3. Token迁移（启动清除旧Token）

#### Server端集成测试（Task 2.9）
- **测试范围**：AuthController + AuthService + TokenRevocationService + SecurityAuditService
- **数据库**：真实SQL Server Test数据库
- **测试框架**：WebApplicationFactory
- **测试场景**：
  1. POST /api/v1/auth/revoke-token → 撤销成功 → 刷新失败
  2. POST /api/v1/auth/refresh → Token轮换 → 旧Token被撤销
  3. 审计日志完整性（登录/登出/刷新/撤销）

### E2E测试策略

#### 端到端功能测试（Task 3.1）
- **测试范围**：完整的Client→Server→Database流程
- **测试环境**：真实Server + 真实Client + 真实数据库
- **测试场景**：
  1. 用户登录 → Token加密存储 → 关闭应用 → 重启 → 会话恢复
  2. AccessToken过期 → 自动刷新 → 旧RefreshToken被撤销
  3. 管理员撤销Token → 用户刷新失败 → 跳转登录页
  4. 审计日志完整记录所有事件

### 安全测试清单（Task 3.2）

#### 加密验证
- [ ] 使用文本编辑器打开tokens.dat → 无法读取有效Token信息
- [ ] 使用Hex编辑器查看 → 确认DPAPI加密格式
- [ ] 删除entropy → 解密失败（如果使用entropy）

#### 签名验证
- [ ] 使用jwt.io修改Token payload → 验证失败
- [ ] 使用错误的SecretKey生成Token → 验证失败
- [ ] 使用正确Token → 验证成功

#### 过期验证
- [ ] 修改系统时间到15分钟后 → AccessToken过期 → 验证失败
- [ ] 修改系统时间到7天后 → RefreshToken过期 → 刷新失败
- [ ] ClockSkew测试：修改时间到4分钟后 → 仍然有效（容差5分钟）

#### 撤销验证
- [ ] 撤销RefreshToken → 立即刷新 → 失败（响应时间 < 1秒）
- [ ] 撤销用户所有Token → 所有刷新都失败
- [ ] 审计日志记录撤销事件

#### 脱敏验证
- [ ] 查询SecurityAuditLogs → IP地址已脱敏（192.168.1.*）
- [ ] UserAgent已截断（最大500字符）
- [ ] 敏感信息不在Metadata JSON中

### 性能测试基准（Task 3.3）

#### 性能指标
| 操作 | 目标 | 当前（重构前） | 重构后 |
|------|------|--------------|--------|
| Token本地验证 | < 10ms | N/A（调用Server API ~50-100ms） | < 5ms |
| Token加密存储 | < 50ms | N/A（明文写入 ~2ms） | < 30ms |
| 应用启动增量 | < 500ms | N/A | < 300ms |
| 审计日志记录 | < 5ms overhead | N/A | < 3ms |
| Token撤销生效 | < 1秒 | N/A | < 200ms |

#### 性能测试方法
```csharp
[Fact]
public void Performance_LocalValidation_LessThan10ms()
{
    // Arrange
    var validator = new LocalTokenValidator(config, logger);
    var token = GenerateTestToken();
    var stopwatch = Stopwatch.StartNew();

    // Act
    for (int i = 0; i < 1000; i++)
    {
        validator.ValidateToken(token);
    }
    stopwatch.Stop();

    // Assert
    var averageMs = stopwatch.ElapsedMilliseconds / 1000.0;
    averageMs.Should().BeLessThan(10);
}
```

---

## 📋 验收检查清单

### Phase 1验收（Client端）
- [ ] SecureTokenStorage加密存储成功（Task 1.1）
- [ ] LocalTokenValidator本地验证成功（Task 1.2）
- [ ] AuthenticationService移除Server API依赖（Task 1.3）
- [ ] Token清理逻辑工作（Task 1.4）
- [ ] Client端单元测试全部通过（Task 1.5）
- [ ] Client端集成测试全部通过（Task 1.6）

### Phase 2验收（Server端）
- [ ] RefreshTokens表新增字段成功（Task 2.1）
- [ ] SecurityAuditLogs表创建成功（Task 2.2）
- [ ] TokenRevocationService撤销功能工作（Task 2.3）
- [ ] SecurityAuditService审计日志记录（Task 2.4）
- [ ] AuthService集成撤销检查和审计日志（Task 2.5）
- [ ] 后台清理Job正常运行（Task 2.6）
- [ ] POST /api/v1/auth/validate端点已移除（Task 2.7）
- [ ] Server端单元测试全部通过（Task 2.8）
- [ ] Server端集成测试全部通过（Task 2.9）

### Phase 3验收（测试与发布）
- [ ] 端到端功能测试全部通过（Task 3.1）
- [ ] 安全测试全部通过（Task 3.2）
- [ ] 性能测试达到目标（Task 3.3）
- [ ] 所有文档已更新（Task 3.4）
- [ ] 发布准备已完成（Task 3.5）
- [ ] Issue #1861已更新（Task 3.6）

### 最终验收（整体）
- [ ] 所有编译警告已清除（0 warnings）
- [ ] 所有单元测试通过（覆盖率 ≥ 80%）
- [ ] 所有集成测试通过
- [ ] E2E测试通过
- [ ] 安全测试通过
- [ ] 性能测试达标
- [ ] 文档完整且链接有效
- [ ] 数据库迁移可回滚
- [ ] 用户通知文案已准备

---

## 💡 实施注意事项

### 开发环境要求

#### 软件要求
- .NET 8.0 SDK
- SQL Server 2022（或兼容版本）
- Visual Studio 2022 或 JetBrains Rider
- Git（版本控制）

#### NuGet包依赖（新增）
```xml
<!-- Client端 -->
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />

<!-- Server端 -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
```

### 配置文件变更

#### Client端 appsettings.json（新增配置）
```json
{
  "Lybt": {
    "Jwt": {
      "SecretKey": "[与Server端一致]",
      "Issuer": "LYBTZYZS-Server",
      "Audience": "LYBTZYZS-Client",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7
    }
  }
}
```

#### Server端 appsettings.json（已有配置，确认一致）
```json
{
  "Lybt": {
    "Jwt": {
      "SecretKey": "[保密，不提交到Git]",
      "Issuer": "LYBTZYZS-Server",
      "Audience": "LYBTZYZS-Client",
      "AccessTokenExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7
    }
  }
}
```

### Git提交策略

#### 提交粒度
- 每个Task完成后提交一次
- 提交信息格式：`feat(task-X.Y): 简要说明`
- 示例：`feat(task-1.1): 实现SecureTokenStorage DPAPI加密存储`

#### 分支策略
- 主分支：`master`
- 功能分支：`feature/token-authentication-security-refactor`（如果需要PR）
- 提交顺序：按Phase顺序提交，确保每个提交都可编译

#### 关联Issue
- 每个提交关联相关任务：`Task X.Y: 任务描述 (#IssueNumber)`

### 回滚计划

#### 数据库回滚
```sql
-- 回滚RefreshTokens字段
ALTER TABLE RefreshTokens DROP COLUMN RevokeReason;
ALTER TABLE RefreshTokens DROP COLUMN RevokedAt;
ALTER TABLE RefreshTokens DROP COLUMN IsRevoked;
DROP INDEX IX_RefreshTokens_IsRevoked_Token ON RefreshTokens;

-- 回滚SecurityAuditLogs表
DROP TABLE SecurityAuditLogs;
```

#### 代码回滚
- 使用Git回滚到重构前版本
- 确保数据库也回滚到匹配版本

#### 用户数据处理
- Token已清除，用户需重新登录（无需特殊处理）
- 审计日志保留（即使回滚，历史记录有价值）

---

## 📚 相关文档

### 核心文档
- [Token认证安全重构 - 需求讨论](../explanation/architecture/shared/token-authentication-security-discussion.md)
- [Token认证安全重构 - 需求文档](../explanation/requirements/token-authentication-security-requirements.md)
- [Token认证安全重构 - 技术设计](../explanation/design/token-authentication-security-design.md)
- [ADR-011: Token认证安全重构与SuperAdmin统一认证](../explanation/architecture/decisions/adr-011-token-authentication-security-refactor.md)

### 架构文档
- [ADR-010: SuperAdmin属于Auth模块](../explanation/architecture/decisions/adr-010-superadmin-belongs-to-auth-module.md)
- [ADR-005: 渐进式演进原则](../explanation/architecture/decisions/adr-005-gradual-evolution-principle.md)
- [三层对齐架构指南](../explanation/architecture/server/README.md)

### 技术参考
- [Microsoft JWT Authentication文档](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication)
- [Windows DPAPI文档](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata)

---

**文档版本**: 1.0
**创建日期**: 2025-11-06
**维护者**: Claude Code
**审核状态**: ✅ 准备就绪

**下一步操作**: 使用`lybtzyzs-issue-template` Skill批量生成GitHub Issues

# Token认证安全重构 - 需求文档

**文档状态**: ✅ 已确认
**创建日期**: 2025-11-06
**需求来源**: Issue #1861触发的安全隐患分析
**架构方案**: 方案C - 当前设计简化版
**配置决策**: 推荐配置（已确认）

---

## 一、需求概述

### 1.1 需求背景

在修复Issue #1861（RefreshToken用户类型路由）过程中，深度安全分析揭示了当前Token认证架构存在**7大安全隐患**，包括Token明文存储、缺少撤销机制、过度依赖Server验证等问题。

**触发事件**：Token验证返回null Username → 架构层面安全问题
**决策依据**：安全是系统基石，不能妥协
**方案选择**：方案C - 数据源分离 + Token策略统一

### 1.2 需求目标

#### 核心安全目标
1. **消除Token明文存储风险** - 使用Windows DPAPI加密
2. **实现Token主动撤销能力** - Server端黑名单机制
3. **建立安全审计追溯能力** - 记录所有认证事件
4. **简化并加固认证架构** - 客户端自验证 + 统一Token策略

#### 架构优化目标
5. **保持模块边界清晰** - SuperAdmin（Auth）≠ User（User模块）
6. **降低认证复杂度** - 统一Token策略，简化RefreshToken路由
7. **提升性能体验** - 本地验证替代Server API调用

### 1.3 约束与边界

#### MVP范围内（必须实现）
- ✅ Token加密存储（Windows DPAPI）
- ✅ Client端JWT自验证
- ✅ RefreshToken撤销机制（黑名单）
- ✅ 安全审计日志（基础存储）
- ✅ SuperAdmin和User统一Token策略
- ✅ 强制重新登录（Token迁移）

#### MVP范围外（技术债记录）
- ❌ 非对称签名算法RS256（当前HS256）
- ❌ SuperAdmin差异化Token策略（5分钟vs15分钟）
- ❌ 安全审计日志UI查询功能
- ❌ IP地址绑定验证
- ❌ 设备指纹识别
- ❌ "撤销所有Token"的管理员UI功能

---

## 二、功能需求

### FR-1：Token安全存储

**需求描述**：Token必须加密存储在客户端本地，防止其他程序读取

#### FR-1.1 Token加密
- **加密方式**：Windows DPAPI (`ProtectedData.Protect`)
- **加密范围**：`DataProtectionScope.CurrentUser`
- **存储位置**：`%LOCALAPPDATA%\LYBTZYZS\tokens.dat`（加密文件）
- **加密对象**：AccessToken + RefreshToken + 元数据（UserType, ExpiresAt等）

#### FR-1.2 降级策略
- **场景**：DPAPI不可用的Windows环境
- **处理**：降级为明文存储 + 记录警告日志
- **日志内容**：`"警告: Token加密失败，使用明文存储。建议检查系统DPAPI配置。"`

#### FR-1.3 Token清理
- **应用启动时**：清除所有本地Token（包括加密和明文）
- **结果**：用户必须重新登录（强制Token迁移）
- **提示**：显示"系统安全升级，请重新登录"

#### 验收标准
- [ ] Token文件使用DPAPI加密，无法用记事本打开
- [ ] 应用启动时清除所有旧Token
- [ ] DPAPI失败时降级为明文+警告日志
- [ ] 加密失败率 < 0.1%（监控指标）

---

### FR-2：客户端JWT自验证

**需求描述**：Client端使用JWT标准库本地验证Token，移除Server API调用

#### FR-2.1 本地验证流程
```
Token验证请求
    ↓
使用JwtSecurityTokenHandler
    ↓
验证签名（使用Server公开的密钥配置）
    ↓
验证Issuer、Audience
    ↓
验证Expiration（当前时间 vs exp claim）
    ↓
验证必需Claims（sub, name, role, user_type）
    ↓
返回验证结果 + 用户信息
```

#### FR-2.2 验证参数配置
- **Issuer**：从Client配置读取（appsettings.json）
- **Audience**：从Client配置读取
- **SecretKey**：从Client配置读取（与Server一致）
- **ClockSkew**：5分钟时钟偏差容忍

#### FR-2.3 移除Server端点
- **移除**：`POST /api/v1/auth/validate` 端点
- **移除**：`AuthService.ValidateTokenWithDetailsAsync` 方法
- **保留**：`GET /api/v1/auth/validate` 端点（用于需要Server状态检查的场景）

#### FR-2.4 Claims提取
从JWT提取以下信息：
- `sub` (NameIdentifier) → UserId
- `name` (Name) → UserName
- `role` (Role) → UserRole
- `user_type` (自定义) → "superadmin" 或 "user"
- `exp` (Expiration) → 过期时间

#### 验收标准
- [ ] Client端无需调用Server API即可验证Token
- [ ] 验证性能 < 10ms（本地计算）
- [ ] 签名验证、过期检查、Claims提取全部正确
- [ ] Server端`POST /api/v1/auth/validate`端点已移除
- [ ] 所有原调用此端点的代码已重构

---

### FR-3：RefreshToken撤销机制

**需求描述**：Server端支持主动撤销RefreshToken，实现强制下线能力

#### FR-3.1 数据库Schema变更
```sql
-- RefreshTokens表新增字段
ALTER TABLE RefreshTokens ADD IsRevoked BIT NOT NULL DEFAULT 0;
ALTER TABLE RefreshTokens ADD RevokedAt DATETIME2 NULL;
ALTER TABLE RefreshTokens ADD RevokeReason NVARCHAR(500) NULL;

-- 索引优化（查询撤销状态）
CREATE INDEX IX_RefreshTokens_IsRevoked_Token
ON RefreshTokens(IsRevoked, Token)
INCLUDE (UserId, UserType);
```

#### FR-3.2 撤销API（Server端）
```csharp
// POST /api/v1/auth/revoke-token
public async Task<ServiceResult> RevokeTokenAsync(RevokeTokenRequest request)
{
    // request.RefreshToken: 要撤销的RefreshToken
    // request.Reason: 撤销原因（可选）
}

// POST /api/v1/auth/revoke-all-user-tokens
public async Task<ServiceResult> RevokeAllUserTokensAsync(RevokeAllTokensRequest request)
{
    // request.UserId: 要撤销所有Token的用户ID
    // request.Reason: 撤销原因（可选）
}
```

#### FR-3.3 刷新流程集成
RefreshTokenAsync方法必须检查撤销状态：
```csharp
var tokenRecord = await _dbContext.RefreshTokens
    .FirstOrDefaultAsync(t => t.Token == refreshToken);

if (tokenRecord == null || tokenRecord.IsRevoked)
{
    await _auditService.LogAsync(new SecurityEvent
    {
        EventType = "RefreshTokenRejected",
        Reason = tokenRecord?.IsRevoked == true ? "Token已撤销" : "Token不存在"
    });
    return Failure("RefreshToken无效或已撤销");
}
```

#### FR-3.4 UI功能（MVP范围外）
- ❌ SuperAdmin管理界面的"撤销Token"按钮
- ❌ 查看用户当前有效Token列表
- ⚠️ 仅提供API能力，UI后续版本实现

#### 验收标准
- [ ] 数据库迁移成功，新增3个字段
- [ ] `RevokeTokenAsync` API正常工作
- [ ] `RevokeAllUserTokensAsync` API正常工作
- [ ] RefreshToken时检查IsRevoked标记
- [ ] 撤销后的Token无法刷新（返回401）

---

### FR-4：安全审计日志

**需求描述**：记录所有认证相关事件，支持安全追溯和异常检测

#### FR-4.1 数据库Schema
```sql
CREATE TABLE SecurityAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EventType NVARCHAR(50) NOT NULL,           -- Login, Logout, RefreshToken, TokenRevoked, LoginFailed
    UserId UNIQUEIDENTIFIER NULL,
    UserType NVARCHAR(50) NULL,                -- superadmin, user
    UserName NVARCHAR(256) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(500) NULL,
    Metadata NVARCHAR(MAX) NULL,               -- JSON: 额外信息
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    INDEX IX_SecurityAuditLogs_EventType_CreatedAt (EventType, CreatedAt DESC),
    INDEX IX_SecurityAuditLogs_UserId_CreatedAt (UserId, CreatedAt DESC)
);
```

#### FR-4.2 记录的事件类型

| EventType | 触发场景 | Success | 记录内容 |
|-----------|---------|---------|---------|
| `Login` | 用户登录 | true/false | UserName, IpAddress, UserAgent |
| `LoginFailed` | 登录失败 | false | UserName, ErrorMessage（密码错误/用户不存在） |
| `Logout` | 用户登出 | true | UserId, UserName |
| `RefreshToken` | Token刷新 | true/false | UserId, UserType |
| `TokenRevoked` | Token撤销 | true | UserId, RevokeReason |
| `RefreshTokenRejected` | 刷新被拒绝 | false | Token已撤销/不存在 |

#### FR-4.3 日志记录规则
- **同步记录**：Login、LoginFailed、TokenRevoked
- **异步记录**：RefreshToken（不阻塞业务流程）
- **保留期**：30天（自动清理31天前的日志）
- **清理策略**：每日凌晨3点运行清理Job

#### FR-4.4 敏感信息处理
- ❌ **不记录**：密码、Token完整内容、个人敏感信息
- ✅ **记录**：UserName、IpAddress（前3段）、UserAgent（前100字符）
- ✅ **脱敏**：IP `192.168.1.100` → `192.168.1.*`

#### FR-4.5 UI查询（MVP范围外）
- ❌ 不实现UI查询功能
- ✅ 仅存储数据，后续版本提供查询界面
- 📝 数据可通过数据库直接查询（运维需求）

#### 验收标准
- [ ] SecurityAuditLogs表创建成功
- [ ] Login/LoginFailed/Logout/RefreshToken/TokenRevoked事件正常记录
- [ ] 敏感信息已脱敏（密码、完整Token不记录）
- [ ] 30天自动清理Job正常工作
- [ ] 性能影响 < 5%（异步记录）

---

### FR-5：SuperAdmin与User统一认证

**需求描述**：基于方案C，SuperAdmin和User使用统一Token策略，但数据源分离

#### FR-5.1 数据源分离（保持现有设计）
- **SuperAdmin** → `AdminSecrets`表（Auth模块）
- **User** → `Users`表（User模块）
- **RefreshToken** → 保留`UserType`字段（"superadmin" / "user"）

#### FR-5.2 统一Token策略
| Token类型 | SuperAdmin | User | 说明 |
|----------|-----------|------|------|
| AccessToken过期 | 15分钟 | 15分钟 | ✅ 统一 |
| RefreshToken过期 | 7天 | 7天 | ✅ 统一 |
| 签名算法 | HS256 | HS256 | ✅ 统一 |
| Claims结构 | 标准Claims + user_type | 标准Claims + user_type | ✅ 统一 |

#### FR-5.3 认证流程统一入口
```csharp
// 单一登录端点：POST /api/v1/auth/login
public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
{
    // 自动识别用户类型
    var isSuperAdmin = await _adminSecretsRepo.ExistsAsync(request.UserName);

    if (isSuperAdmin)
    {
        return await AuthenticateAsSuperAdminAsync(request);
    }
    else
    {
        return await AuthenticateAsUserAsync(request);
    }
}
```

#### FR-5.4 RefreshToken路由简化
```csharp
public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
{
    var tokenRecord = await _dbContext.RefreshTokens
        .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked);

    // UserType仅用于数据路由，Token策略完全统一
    UserDto userDto;
    if (tokenRecord.UserType == "superadmin")
    {
        userDto = await GetSuperAdminInfoAsync(tokenRecord.UserId);
    }
    else
    {
        userDto = await _userRepository.GetByIdAsync(tokenRecord.UserId);
    }

    // 统一生成Token（15分钟AccessToken + 7天RefreshToken）
    var token = _jwtService.GenerateToken(userDto, tokenRecord.UserType);
    // ...
}
```

#### FR-5.5 技术债记录
⚠️ **未来可选升级**：SuperAdmin差异化安全策略（5分钟AccessToken）
- **触发条件**：安全审计要求 或 MVP发布后评估
- **升级路径**：修改`TokenPolicyProvider`，根据UserType返回不同策略
- **影响范围**：`JwtService.GenerateToken`，RefreshToken逻辑

#### 验收标准
- [ ] SuperAdmin和User使用相同的Token过期时间
- [ ] 单一登录端点自动识别用户类型
- [ ] RefreshToken路由逻辑简洁清晰
- [ ] AdminSecrets和Users表数据隔离保持不变

---

## 三、非功能需求

### NFR-1：性能要求

| 指标 | 目标值 | 测量方法 |
|-----|--------|---------|
| Client端Token验证 | < 10ms | 性能测试：1000次验证平均耗时 |
| Token加密存储 | < 50ms | 登录成功到Token保存完成 |
| Server端RefreshToken | < 100ms | 包含撤销检查和审计日志 |
| 审计日志异步写入 | < 200ms | 不阻塞业务流程 |
| 应用启动时间增量 | < 500ms | 清除旧Token + DPAPI初始化 |

### NFR-2：可靠性要求

- **DPAPI成功率** > 99.9%（降级策略覆盖失败场景）
- **Token验证准确率** = 100%（签名、过期、Claims验证）
- **审计日志完整性** > 99%（允许异步丢失 < 1%）
- **数据库迁移成功率** = 100%（提供回滚脚本）

### NFR-3：安全要求

- **Token加密强度**：Windows DPAPI（AES-256 + CurrentUser绑定）
- **敏感信息脱敏**：密码、完整Token、个人信息不记录
- **撤销响应时间**：调用撤销API后，下一次刷新立即生效（< 1秒）
- **审计日志防篡改**：只追加，不修改，不删除（30天内）

### NFR-4：兼容性要求

- **Windows版本**：Windows 10及以上（DPAPI支持）
- **降级支持**：Windows 7/8.1使用明文存储+警告
- **.NET版本**：.NET 8.0
- **数据库**：SQL Server 2022

### NFR-5：可维护性要求

- **代码注释覆盖率**：关键方法100%（DPAPI、JWT验证、撤销逻辑）
- **单元测试覆盖率** > 80%（核心认证流程）
- **API文档完整性**：100%（所有认证相关端点）
- **ADR记录**：架构决策和技术债完整记录

---

## 四、配置决策确认

基于推荐配置，以下决策已确认：

| 决策项 | 选定方案 | 理由 |
|-------|---------|------|
| Token迁移策略 | A - 强制重新登录 | 最简单、最安全、一次性影响 |
| 审计日志保留期 | 30天 | 平衡存储和追溯需求 |
| 撤销Token UI功能 | MVP范围外 | 仅提供API，UI后续实现 |
| DPAPI降级策略 | A - 降级为明文+警告 | 保证系统可用性 |
| 现有Token处理 | A - 清除所有 | 与迁移策略一致 |
| 审计日志UI查询 | MVP范围外 | 仅存储数据，不提供UI |
| 实施策略 | 一次性实施 | 5-7天完成，减少兼容性问题 |

---

## 五、验收标准总结

### 功能验收
- [ ] FR-1：Token加密存储（DPAPI + 降级）
- [ ] FR-2：Client端JWT自验证（移除Server API）
- [ ] FR-3：RefreshToken撤销机制（黑名单）
- [ ] FR-4：安全审计日志（存储30天）
- [ ] FR-5：SuperAdmin和User统一Token策略

### 性能验收
- [ ] Token验证 < 10ms
- [ ] 启动时间增量 < 500ms
- [ ] 审计日志不阻塞业务

### 安全验收
- [ ] Token文件无法明文读取（DPAPI加密）
- [ ] 撤销Token后无法刷新
- [ ] 敏感信息已脱敏

### 用户体验验收
- [ ] 首次升级后提示"系统安全升级，请重新登录"
- [ ] 登录流程无变化（用户无感知）
- [ ] 性能无明显下降

---

## 六、风险与缓解

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| DPAPI环境兼容性 | 中 | 低 | 降级为明文+警告 |
| 数据库迁移失败 | 高 | 低 | 提供回滚脚本，测试环境验证 |
| 现有Token失效 | 高 | 必然 | 提示用户"安全升级" |
| 性能影响 | 中 | 低 | 异步审计日志，本地验证 |
| 用户投诉重新登录 | 低 | 中 | 说明安全升级必要性 |

---

## 七、依赖与约束

### 外部依赖
- Windows DPAPI（操作系统级别）
- System.IdentityModel.Tokens.Jwt NuGet包
- SQL Server 2022

### 内部依赖
- Issue #1861的RefreshToken.UserType字段（已实现）
- 现有的JwtService基础设施
- 现有的AuthService和UserRepository

### 技术约束
- 保持HS256对称签名（非对称升级为技术债）
- SuperAdmin和User统一Token策略（差异化为技术债）
- 审计日志仅存储不提供UI（UI为技术债）

---

## 八、时间规划

### 预计时间
**总计：5-7个工作日**

| Phase | 工作内容 | 时间 |
|-------|---------|------|
| Day 1 | Client端：Token加密存储 + DPAPI实现 | 1天 |
| Day 2 | Client端：JWT自验证 + 移除Server API调用 | 1天 |
| Day 3 | Server端：RefreshToken撤销机制 + 数据库迁移 | 1天 |
| Day 4 | Server端：安全审计日志 + 自动清理Job | 1天 |
| Day 5 | 集成测试 + Bug修复 | 1天 |
| Day 6-7 | 手动安全测试 + 文档更新 | 1-2天 |

### 里程碑
- ✅ Day 2结束：Client端重构完成
- ✅ Day 4结束：Server端重构完成
- ✅ Day 7结束：全功能验收通过

---

## 九、交付物

### 代码交付
1. Client端代码
   - `SecureTokenStorage.cs` - DPAPI加密存储
   - `LocalTokenValidator.cs` - JWT自验证
   - 重构`AuthenticationService.cs`

2. Server端代码
   - `TokenRevocationService.cs` - 撤销服务
   - `SecurityAuditService.cs` - 审计服务
   - 重构`AuthService.cs`，移除`ValidateTokenWithDetailsAsync`
   - 新增`RevokeTokenAsync`、`RevokeAllUserTokensAsync` API

3. 数据库迁移
   - `20251106_AddTokenRevocation.sql`
   - `20251106_CreateSecurityAuditLogs.sql`

### 文档交付
1. 技术设计文档（`token-authentication-security-design.md`）
2. ADR文档（`adr-011-token-authentication-security-refactor.md`）
3. API文档更新（Swagger注释）
4. 单元测试（> 80%覆盖率）

### 其他交付
5. GitHub Issues（task-breakdown生成）
6. 更新Issue #1861（标记为被新方案替代）

---

**需求确认**: ✅ 已确认（推荐配置）
**下一步**: 创建技术设计文档
**版本**: 1.0

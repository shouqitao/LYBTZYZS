# Token认证安全重构 - 需求讨论

**文档状态**: 🟡 待确认
**创建日期**: 2025-11-06
**最后更新**: 2025-11-06
**关联Issue**: #1861 (触发因素)
**架构决策**: 基于方案C（当前设计简化版）

---

## 一、问题背景

### 1.1 触发事件

**Issue #1861修复过程中发现的安全隐患**：
- Token验证返回null Username，导致认证失败
- 深度分析揭示了**架构层面的安全问题**，而非单纯的bug

### 1.2 当前架构的安全隐患

#### 🔴 高风险问题

1. **Token明文存储**
   - 本地Token文件未加密
   - 其他程序可以读取Token内容
   - Token泄露后可使用到过期（最长7天）

2. **缺少Token撤销机制**
   - RefreshToken无法主动撤销
   - 用户无法"强制下线所有设备"
   - 安全事件响应能力不足

3. **过度依赖Server验证但无状态检查**
   - Client调用Server API验证Token（网络往返）
   - 但Server验证不检查撤销状态
   - 最差的"两头不靠"架构

#### 🟡 中风险问题

4. **SuperAdmin和User混合处理**
   - 虽然Issue #1861添加了UserType字段
   - 但安全策略未差异化（相同的Token过期时间）
   - SuperAdmin Token泄露的影响范围更大

5. **缺少安全审计日志**
   - Token使用、刷新、失败尝试未记录
   - 无法追溯安全事件
   - 无法检测异常登录

#### 🟢 低风险但不规范

6. **对称密钥HS256**
   - 当前使用对称签名算法
   - Microsoft推荐使用非对称RS256/ES256
   - 对MVP阶段可接受，但记录为技术债

7. **客户端Token验证架构冗余**
   - JWT本身支持客户端自验证
   - 无需每次调用Server API
   - 浪费网络资源和Server负载

### 1.3 安全威胁场景

- **场景1**：Token文件被其他恶意程序读取 → 可以伪装成用户访问系统
- **场景2**：员工离职但Token未撤销 → 7天内仍可访问系统
- **场景3**：SuperAdmin账号泄露 → 系统完全失控
- **场景4**：无法追溯谁在何时做了什么 → 安全事件无法调查

---

## 二、SuperAdmin架构方案选择

### 2.1 方案对比总结

| 方案 | 架构原则 | 认证复杂度 | MVP成本 | ADR-010符合度 |
|-----|---------|----------|---------|-------------|
| **A. 统一认证** | SuperAdmin = 特殊User | ⭐ 极简 | ⭐ 低 | ❌ 违背 |
| **B. 完全独立** | SuperAdmin ⊥ User | ⭐⭐⭐ 高 | ⭐⭐⭐ 高 | ✅ 完全符合 |
| **C. 当前简化（选定）** | SuperAdmin ≠ User | ⭐⭐ 中等 | ⭐⭐ 中等 | ⚠️ 部分符合 |

### 2.2 方案C核心设计

**数据源分离 + Token策略统一**

#### 保留的设计
- ✅ SuperAdmin存储在`AdminSecrets`表（Auth模块）
- ✅ User存储在`Users`表（User模块）
- ✅ RefreshToken表保留`UserType`字段用于路由
- ✅ 单一登录端点，自动识别用户类型

#### 简化的设计
- ⚠️ **统一Token策略**：SuperAdmin和User使用相同的过期时间
  - AccessToken: 15分钟（统一）
  - RefreshToken: 7天（统一）
- ⚠️ UserType仅用于数据路由，不影响安全策略

#### 权衡说明
- **放弃**：SuperAdmin差异化安全策略（如5分钟AccessToken）
- **保留**：模块边界（数据源分离）
- **简化**：认证流程和Token管理
- **未来**：可升级到方案B（完全独立认证）

---

## 三、重构目标

### 3.1 核心安全目标

1. **Token安全存储**
   - 使用Windows DPAPI加密本地Token
   - 保护范围：CurrentUser级别
   - 文件路径：`%LOCALAPPDATA%\LYBTZYZS\tokens.dat`（加密）

2. **Token撤销机制**
   - Server端维护RefreshToken黑名单
   - 支持撤销单个Token
   - 支持撤销用户所有Token（强制下线）

3. **客户端JWT自验证**
   - 移除Server API调用（`/api/v1/auth/validate` POST端点）
   - 使用`JwtSecurityTokenHandler`本地验证
   - 验证：签名、Issuer、Audience、Expiration、必需Claims

4. **安全审计日志**
   - 记录所有认证事件（登录、登出、刷新）
   - 记录失败尝试（错误密码、无效Token）
   - 记录Token撤销操作

### 3.2 架构优化目标

5. **简化认证流程**
   - 统一Token策略（SuperAdmin和User）
   - 清晰的RefreshToken路由逻辑
   - 消除冗余的Server验证

6. **保持模块边界**
   - SuperAdmin数据属于Auth模块
   - User数据属于User模块
   - Token服务作为共享基础设施

---

## 四、技术方案概述

### 4.1 Client端变更

#### 移除
- ❌ `AuthenticationService.ValidateTokenAsync(string token)` - 调用Server API
- ❌ 对`IAuthApi.ValidateTokenAsync` POST端点的依赖

#### 新增
- ✅ `LocalTokenValidator` - 客户端JWT验证器
- ✅ `SecureTokenStorage` - 使用DPAPI的加密存储
- ✅ Token自动清理逻辑（启动时清除过期Token）

#### 修改
- 🔄 `AuthenticationService.ValidateAndRestoreSessionAsync` - 调用本地验证
- 🔄 `TokenStorageService` - 集成DPAPI加密

### 4.2 Server端变更

#### 新增
- ✅ `TokenRevocationService` - Token撤销服务
- ✅ `SecurityAuditService` - 安全审计日志
- ✅ `RefreshToken.IsRevoked`字段 - 撤销标记

#### 移除
- ❌ `AuthService.ValidateTokenWithDetailsAsync` - 不再需要
- ❌ `AuthController.ValidateTokenFromBodyAsync` POST端点

#### 保留
- ✅ `AuthController.ValidateTokenFromHeaderAsync` GET端点 - 用于需要Server状态检查的场景

#### 修改
- 🔄 `RefreshTokenAsync` - 检查撤销状态
- 🔄 `LoginAsync`/`LogoutAsync` - 集成审计日志

### 4.3 数据库变更

```sql
-- RefreshToken表新增字段
ALTER TABLE RefreshTokens ADD IsRevoked BIT NOT NULL DEFAULT 0;
ALTER TABLE RefreshTokens ADD RevokedAt DATETIME2 NULL;
ALTER TABLE RefreshTokens ADD RevokeReason NVARCHAR(500) NULL;

-- 新增SecurityAuditLog表
CREATE TABLE SecurityAuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    EventType NVARCHAR(50) NOT NULL,  -- Login, Logout, RefreshToken, TokenRevoked, LoginFailed
    UserId UNIQUEIDENTIFIER NULL,
    UserType NVARCHAR(50) NULL,       -- superadmin, user
    UserName NVARCHAR(256) NULL,
    IpAddress NVARCHAR(50) NULL,
    UserAgent NVARCHAR(500) NULL,
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(500) NULL,
    Metadata NVARCHAR(MAX) NULL,      -- JSON: 额外信息
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

---

## 五、技术约束与边界

### 5.1 MVP范围内（本次实施）

✅ **必须实现**：
1. Token加密存储（DPAPI）
2. Client端JWT自验证
3. RefreshToken撤销机制
4. 安全审计日志（基础）
5. 统一SuperAdmin和User的Token策略

✅ **技术选型**：
- 加密：Windows DPAPI (`ProtectedData.Protect`)
- JWT验证：`System.IdentityModel.Tokens.Jwt`
- 签名算法：保持HS256（对称密钥）
- 数据库：SQL Server 2022

### 5.2 MVP范围外（技术债记录）

❌ **不实施但记录为技术债**：
1. 升级到非对称签名RS256/ES256
2. IP地址绑定验证
3. 设备指纹识别
4. SuperAdmin差异化Token策略（5分钟vs15分钟）
5. 完整的安全审计报表和查询功能

⚠️ **降级方案**：
- 如果DPAPI在某些环境不可用 → 降级为明文+警告日志

---

## 六、风险与依赖

### 6.1 技术风险

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| DPAPI平台兼容性 | 中 | 提供降级方案，记录警告日志 |
| 客户端验证性能 | 低 | JWT验证极快，无需担心 |
| 数据库迁移失败 | 中 | 提供回滚脚本 |
| 现有Token失效 | 高 | 提供Token迁移工具或强制重新登录 |

### 6.2 架构影响

| 影响范围 | 程度 | 说明 |
|---------|------|------|
| Client端认证流程 | 中 | `AuthenticationService`重构 |
| Server端API端点 | 低 | 移除1个端点，修改2个方法 |
| 数据库Schema | 低 | 新增2个字段+1个表 |
| 现有Token兼容性 | 高 | 需要迁移策略 |

### 6.3 用户影响

- **正常情况**：用户无感知，体验不变
- **Token迁移**：可能需要重新登录一次
- **性能提升**：Token验证从网络往返 → 本地计算（更快）

---

## 七、待确认事项

### 7.1 架构决策确认

- [x] **SuperAdmin方案选择**：方案C - 当前设计简化版 ✅ 已确认
- [ ] **Token迁移策略**：强制重新登录 vs 提供迁移工具？
- [ ] **审计日志保留期**：建议30天，是否需要更长？
- [ ] **是否需要"撤销所有Token"的管理员功能**？（UI入口）

### 7.2 实施细节确认

- [ ] **DPAPI降级策略**：如果不可用，是否接受明文存储+警告？
- [ ] **现有Token处理**：
  - 选项A：启动时清除所有本地Token，强制重新登录
  - 选项B：尝试验证现有Token，失败则清除
- [ ] **安全审计日志是否需要UI查询功能**？（MVP范围外，但需确认）

### 7.3 时间与资源确认

- [ ] **预计实施时间**：5-7个工作日（含测试）
- [ ] **是否需要分Phase实施**？
  - Phase 1: Client端自验证 + Token加密存储
  - Phase 2: Server端撤销机制 + 审计日志
- [ ] **测试策略**：单元测试 + 集成测试 + 手动安全测试

---

## 八、下一步行动

### 待用户确认后

1. ✅ 创建正式需求文档（`token-authentication-security-requirements.md`）
2. ✅ 创建技术设计文档（`token-authentication-security-design.md`）
3. ✅ 创建ADR记录架构决策和技术债
4. ✅ 使用`lybtzyzs-task-breakdown` Skill生成task清单
5. ✅ 使用`lybtzyzs-issue-template` Skill批量创建GitHub Issues
6. ✅ 开始实施

### 关联文档

- **ADR-010**: SuperAdmin属于Auth模块（保持有效）
- **新建ADR**: Token认证安全重构决策（方案C选择、技术债记录）
- **Constitution**: 需要更新安全相关约束

---

## 九、附录

### A. Microsoft JWT安全最佳实践摘要

基于 [ASP.NET Core JWT Authentication文档](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication)：

1. ✅ **使用标准**：遵循OpenID Connect/OAuth标准
2. ⚠️ **使用非对称密钥**：推荐RS256，当前HS256（技术债）
3. ✅ **Token完整验证**：签名、Issuer、Audience、Expiration、Claims
4. ✅ **Token轮换**：RefreshToken机制已实现
5. ✅ **安全存储**：本次重构实现DPAPI加密

### B. Desktop应用特殊性说明

WPF Desktop应用与Web应用的差异：
- ❌ 无法使用HttpOnly Cookie（无浏览器沙箱）
- ✅ Token必须本地存储（使用DPAPI加密）
- ✅ 用户名密码登录是主流（无法重定向到OIDC Provider）
- ✅ 需要长期Token（RefreshToken 7天）
- ✅ 客户端需要读取Token内容（显示用户信息）

这些特殊性导致某些Web最佳实践不适用，但我们通过DPAPI加密和客户端验证达到等效的安全水平。

---

**文档版本**: 1.0
**审核状态**: 待用户确认
**下次更新**: 确认后创建需求文档

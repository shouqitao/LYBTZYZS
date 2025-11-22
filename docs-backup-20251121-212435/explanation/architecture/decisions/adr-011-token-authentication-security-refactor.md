# ADR-011: Token认证安全重构与SuperAdmin统一认证

**状态**: ✅ 已接受
**决策日期**: 2025-11-06
**决策者**: 项目负责人
**相关Issue**: #1861 (触发因素)
**技术债追踪**: TD-001 (对称密钥→非对称密钥), TD-002 (差异化Token策略)

---

## 上下文与问题陈述

### 1. 触发事件

在修复Issue #1861（SuperAdmin/User RefreshToken路由问题）过程中，深度分析发现了**架构层面的安全隐患**，而非单纯的代码bug。Token验证返回null Username暴露了以下问题：

**高风险安全隐患**：
1. **Token明文存储** - 本地Token文件未加密，其他程序可读取，泄露后可使用到过期（最长7天）
2. **缺少Token撤销机制** - RefreshToken无法主动撤销，用户无法"强制下线所有设备"
3. **过度依赖Server验证但无状态检查** - Client调用Server API验证Token（网络往返），但Server验证不检查撤销状态，形成"两头不靠"架构

**中风险问题**：
4. **SuperAdmin和User混合处理** - 虽然Issue #1861添加了UserType字段，但安全策略未差异化（相同Token过期时间），SuperAdmin Token泄露影响范围更大
5. **缺少安全审计日志** - Token使用、刷新、失败尝试未记录，无法追溯安全事件或检测异常登录

**低风险但不规范**：
6. **对称密钥HS256** - 当前使用对称签名算法，Microsoft推荐非对称RS256/ES256
7. **客户端Token验证架构冗余** - JWT本身支持客户端自验证，无需每次调用Server API，浪费网络资源

### 2. SuperAdmin架构挑战

**核心矛盾**：
- SuperAdmin存储在`AdminSecrets`表（Auth模块）
- User存储在`Users`表（User模块）
- 需要统一认证入口，但数据源分离

**ADR-010的要求**：
- SuperAdmin属于Auth模块（已明确）
- 必须保持模块边界清晰
- 不能因认证需求破坏模块隔离

### 3. 安全威胁场景

- **场景1**: Token文件被其他恶意程序读取 → 可以伪装成用户访问系统
- **场景2**: 员工离职但Token未撤销 → 7天内仍可访问系统
- **场景3**: SuperAdmin账号泄露 → 系统完全失控
- **场景4**: 无法追溯谁在何时做了什么 → 安全事件无法调查

### 4. 业务需求

项目负责人明确指出：

> "我觉得安全问题还是需要着重考虑的。虽然之前MVP中提到够用就行。但是安全方面我觉得需要认真对待。既然这个认证token的问题目前已经暴露。我觉得完整重构这个功能可以作为MVP的重要功能。"

这表明：
- **安全不容妥协**，即使在MVP阶段
- Token认证是系统的基础设施，影响所有功能
- 先文档后实施的系统重构方法

---

## 决策

### 1. SuperAdmin架构方案：选择方案C

**三个备选方案**：

| 方案 | 架构原则 | 认证复杂度 | MVP成本 | ADR-010符合度 |
|-----|---------|----------|---------|-------------|
| A. 统一认证 | SuperAdmin = 特殊User | ⭐ 极简 | ⭐ 低 | ❌ 违背 |
| B. 完全独立 | SuperAdmin ⊥ User | ⭐⭐⭐ 高 | ⭐⭐⭐ 高 | ✅ 完全符合 |
| **C. 当前简化** | **SuperAdmin ≠ User** | **⭐⭐ 中等** | **⭐⭐ 中等** | **⚠️ 部分符合** |

**选择方案C的原因**：

✅ **保持模块边界**：
- SuperAdmin数据存储在`AdminSecrets`表（Auth模块）
- User数据存储在`Users`表（User模块）
- 通过`UserType`字段路由到不同数据源

✅ **统一Token策略（简化设计）**：
- AccessToken过期时间：15分钟（SuperAdmin = User）
- RefreshToken过期时间：7天（SuperAdmin = User）
- 签名算法：HS256（统一）
- JWT Claims结构：统一（sub, name, role, user_type）

✅ **单一登录端点**：
- POST `/api/v1/auth/login` - 自动识别用户类型
- 内部通过UserType路由到`AdminSecretsRepository`或`UserRepository`
- 对外暴露统一接口

✅ **平衡架构与MVP成本**：
- 避免方案A破坏模块边界（违背ADR-010）
- 避免方案B的高实施成本（2套完全独立的认证系统）
- 可在未来升级到方案B（数据源已分离，仅需拆分Token策略）

**权衡说明**：
- **放弃**: SuperAdmin差异化安全策略（如5分钟AccessToken）→ 记录为技术债TD-002
- **保留**: 模块边界（数据源分离）→ 符合ADR-010核心原则
- **简化**: 认证流程和Token管理 → 降低MVP实施成本
- **未来**: 可渐进式演进到方案B → 架构债可控

### 2. 安全改进决策：五大核心目标

#### FR-1: Token安全存储
**决策**: 使用Windows DPAPI加密本地Token

```
加密方式: Windows DPAPI (ProtectedData.Protect)
加密范围: DataProtectionScope.CurrentUser
存储位置: %LOCALAPPDATA%\LYBTZYZS\tokens.dat (加密)
降级策略: DPAPI失败 → 明文存储 + 警告日志
```

**理由**：
- WPF Desktop应用无法使用HttpOnly Cookie
- DPAPI是.NET官方推荐的本地加密方案
- CurrentUser级别平衡安全性与易用性
- 降级策略确保功能可用性

#### FR-2: 客户端JWT自验证
**决策**: 移除Server API调用，使用本地JWT验证

```
移除: POST /api/v1/auth/validate 端点
移除: AuthService.ValidateTokenWithDetailsAsync 方法
新增: LocalTokenValidator 类（使用JwtSecurityTokenHandler）
验证内容: 签名、Issuer、Audience、Expiration、必需Claims
```

**理由**：
- JWT设计初衷就是支持无状态自验证
- 减少网络往返，提升性能（<10ms）
- 降低Server负载
- 简化架构（Client不依赖Server验证Token有效性）

**保留的Server验证**：
- 保留GET `/api/v1/auth/validate` - 用于需要检查撤销状态的场景

#### FR-3: RefreshToken撤销机制
**决策**: Server端维护RefreshToken黑名单

```sql
ALTER TABLE RefreshTokens ADD IsRevoked BIT NOT NULL DEFAULT 0;
ALTER TABLE RefreshTokens ADD RevokedAt DATETIME2 NULL;
ALTER TABLE RefreshTokens ADD RevokeReason NVARCHAR(500) NULL;
```

**功能**：
- 撤销单个Token（用户手动登出）
- 撤销用户所有Token（强制下线所有设备）
- RefreshToken轮换时自动撤销旧Token

**理由**：
- 响应安全事件的能力（员工离职、账号泄露）
- 符合OWASP Token撤销最佳实践
- MVP阶段仅后端功能，UI入口作为技术债

#### FR-4: 安全审计日志
**决策**: 创建SecurityAuditLogs表记录所有认证事件

```sql
CREATE TABLE SecurityAuditLogs (
    EventType: Login, Logout, RefreshToken, TokenRevoked, LoginFailed
    保留期: 30天
    清理策略: 后台Job每日凌晨3点执行
    数据脱敏: IP地址脱敏（192.168.1.*）, UserAgent截断（500字符）
)
```

**理由**：
- 安全事件可追溯性
- 异常登录检测基础
- 合规要求（未来扩展）
- 性能优化（异步记录）

#### FR-5: SuperAdmin与User统一认证（方案C）
**决策**: 数据源分离 + Token策略统一

```
数据源路由:
- UserType = "superadmin" → AdminSecretsRepository（Auth模块）
- UserType = "user" → UserRepository（User模块）

Token策略统一:
- AccessToken: 15分钟（SuperAdmin = User）
- RefreshToken: 7天（SuperAdmin = User）
- 签名算法: HS256（统一）

登录端点统一:
- POST /api/v1/auth/login - 自动识别UserType
```

**理由**：
- 符合ADR-010（模块边界保持）
- 简化认证流程（单一入口）
- MVP成本可控（5-7天实施）
- 可渐进演进（已为方案B铺路）

### 3. 配置决策（按推荐配置）

| 决策项 | 选择 | 理由 |
|-------|------|------|
| Token迁移策略 | 强制重新登录 | 最简单，确保安全性 |
| 审计日志保留期 | 30天 | 平衡存储与可追溯性 |
| 撤销Token UI | MVP范围外 | 后端功能优先，UI后续迭代 |
| DPAPI降级策略 | 明文+警告 | 确保功能可用性 |
| 现有Token处理 | 启动时清除所有 | 统一迁移，避免兼容性问题 |
| 审计日志UI查询 | MVP范围外 | 数据库查询即可，UI非必需 |
| 实施方式 | 一次性（5-7天） | 避免Phase拆分的复杂性 |

---

## 后果

### 正面影响

✅ **安全性大幅提升**：
- Token加密存储（CurrentUser保护）
- Token撤销能力（安全事件响应）
- 安全审计日志（可追溯性）
- 客户端自验证（减少网络攻击面）

✅ **性能优化**：
- Token验证从网络往返（~50-100ms）→ 本地计算（<10ms）
- 减少Server负载（每次验证不再调用API）

✅ **架构清晰化**：
- SuperAdmin和User的数据源边界明确
- 统一的认证流程和Token策略
- 符合JWT无状态设计理念

✅ **可维护性提升**：
- 安全逻辑集中在Client端LocalTokenValidator和Server端TokenRevocationService
- 审计日志提供运维可观测性

### 负面影响与缓解措施

⚠️ **现有Token全部失效**：
- **影响**: 所有用户需要重新登录一次
- **缓解**: 应用启动时显示友好提示"系统安全升级，请重新登录"

⚠️ **DPAPI平台依赖**：
- **影响**: 仅支持Windows平台（当前Desktop应用已限定Windows）
- **缓解**: 提供降级方案（明文+警告），未来跨平台可替换为其他加密方案

⚠️ **实施成本（5-7天）**：
- **影响**: 短期内影响其他功能开发
- **缓解**: 安全基础设施投资，长期回报高

⚠️ **SuperAdmin策略未差异化**：
- **影响**: SuperAdmin使用15分钟AccessToken（理想5分钟）
- **缓解**: 记录为技术债TD-002，可根据实际安全需求调整

### 技术债

**TD-001: 对称密钥HS256 → 非对称密钥RS256/ES256**
- **当前**: 使用对称签名算法HS256
- **未来**: 升级到非对称RS256或ES256（Microsoft推荐）
- **触发条件**: 多Server实例部署 OR 安全审计要求
- **预计工作量**: 2-3天（密钥管理基础设施）

**TD-002: SuperAdmin差异化Token策略**
- **当前**: SuperAdmin和User使用相同过期时间（15分钟/7天）
- **未来**: SuperAdmin使用5分钟AccessToken + 更严格的刷新策略
- **触发条件**: 安全审计要求 OR SuperAdmin权限滥用事件
- **预计工作量**: 1-2天（TokenPolicyProvider扩展）

**TD-003: 审计日志UI查询功能**
- **当前**: 仅后端功能，需通过数据库查询
- **未来**: 管理员UI查询、筛选、导出审计日志
- **触发条件**: 运维需求 OR 合规要求
- **预计工作量**: 3-5天（UI开发 + 查询API）

**TD-004: Token撤销管理UI**
- **当前**: 仅后端功能，需通过API调用
- **未来**: 管理员UI管理所有Token、批量撤销
- **触发条件**: 运维需求 OR 安全事件频发
- **预计工作量**: 3-5天（UI开发 + 管理API）

### 风险与缓解

| 风险 | 可能性 | 影响 | 缓解措施 |
|-----|-------|------|---------|
| DPAPI加密失败 | 低 | 中 | 降级为明文+警告，确保功能可用 |
| 客户端验证性能问题 | 极低 | 低 | JWT验证极快（<10ms），无需担心 |
| 数据库迁移失败 | 中 | 高 | 提供回滚脚本，迁移前备份 |
| 用户投诉重新登录 | 高 | 低 | 友好提示"安全升级"，说明原因 |
| Token撤销滥用 | 低 | 中 | 仅管理员权限，记录审计日志 |

---

## 实施计划

### Phase 1: Client端重构（Day 1-2）
- **Day 1**: SecureTokenStorage（DPAPI加密存储）
- **Day 2**: LocalTokenValidator（JWT自验证） + 移除Server API依赖

### Phase 2: Server端重构（Day 3-4）
- **Day 3**: TokenRevocationService + 数据库迁移（IsRevoked字段）
- **Day 4**: SecurityAuditService + 后台清理Job（SecurityAuditCleanupService）

### Phase 3: 测试与验收（Day 5-7）
- **Day 5**: 功能集成测试（登录、刷新、撤销、验证）
- **Day 6**: 安全测试（Token泄露模拟、DPAPI降级测试、撤销状态检查）
- **Day 7**: 文档更新（API文档、开发指南、部署文档）+ 生产部署

### 验收标准

**功能验收**：
- ✅ Token加密存储成功率 > 99.9%（DPAPI）
- ✅ Token验证时间 < 10ms（本地验证）
- ✅ Token撤销后RefreshToken无法使用
- ✅ 审计日志记录所有认证事件
- ✅ SuperAdmin和User使用统一Token策略

**性能验收**：
- ✅ 应用启动增量 < 500ms
- ✅ Token验证平均耗时 < 10ms
- ✅ 审计日志异步记录不阻塞请求

**安全验收**：
- ✅ Token文件未加密无法读取有效信息
- ✅ 撤销的Token无法刷新或使用
- ✅ 审计日志完整记录失败尝试

---

## 相关决策与文档

### 关联ADR
- **ADR-010**: SuperAdmin属于Auth模块 - 本决策遵循此约束，保持模块边界
- **ADR-005**: 渐进式演进原则 - 方案C为未来升级到方案B铺路

### 关联文档
- [Token认证安全重构 - 需求讨论](../shared/token-authentication-security-discussion.md) - 架构方案对比与选择过程
- [Token认证安全重构 - 需求文档](../../requirements/token-authentication-security-requirements.md) - 完整需求规格说明
- [Token认证安全重构 - 技术设计](../../design/token-authentication-security-design.md) - 详细实现设计

### Constitution更新
本决策需要更新`.spec-workflow/steering/constitution.md`：

```markdown
### 5.2 安全约束（新增）

**Token认证安全（ADR-011）**：
- ✅ Token必须加密存储（DPAPI，Windows平台）
- ✅ Client端自验证JWT（减少Server依赖）
- ✅ RefreshToken必须支持撤销
- ✅ 所有认证事件必须记录审计日志

**MVP阶段简化（技术债）**：
- ⚠️ 对称密钥HS256（未来升级RS256）
- ⚠️ SuperAdmin与User统一Token策略（未来差异化）
- ⚠️ 审计日志UI查询功能（MVP范围外）
```

---

## 决策批准

**决策者**: 项目负责人
**批准日期**: 2025-11-06
**决策依据**:
- Issue #1861修复过程中发现的安全隐患
- 项目负责人明确要求"安全问题需要认真对待"
- 选择方案C平衡架构纯度与MVP成本
- 所有配置决策采用推荐配置

**决策生效**: 立即生效，从文档创建到实施完成（5-7天）

---

**版本**: 1.0
**最后更新**: 2025-11-06
**维护者**: Claude Code（文档生成） + 项目负责人（决策确认）
**状态**: ✅ 已接受，等待实施

---

## 附录

### A. Microsoft JWT安全最佳实践参考

基于[ASP.NET Core JWT Authentication文档](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication)：

1. ✅ **使用标准**: 遵循OpenID Connect/OAuth 2.0标准
2. ⚠️ **使用非对称密钥**: 推荐RS256，当前HS256（技术债TD-001）
3. ✅ **Token完整验证**: 签名、Issuer、Audience、Expiration、必需Claims
4. ✅ **Token轮换**: RefreshToken机制已实现
5. ✅ **安全存储**: 本次重构实现DPAPI加密

### B. Desktop应用特殊性说明

WPF Desktop应用与Web应用的安全差异：

| 特性 | Web应用 | Desktop应用（WPF） |
|-----|--------|------------------|
| Token存储 | HttpOnly Cookie | 本地文件（需加密） |
| 验证位置 | Server端（每次请求） | Client端（JWT自验证） |
| 登录方式 | 重定向到OIDC Provider | 用户名密码表单 |
| Token有效期 | 短（5-15分钟） | 中（15分钟 + 7天RefreshToken） |
| 安全沙箱 | 浏览器沙箱 | 操作系统用户权限（DPAPI） |

这些特殊性导致某些Web最佳实践不适用，但我们通过DPAPI加密和客户端自验证达到**等效的安全水平**。

### C. 未来演进路径（如需升级到方案B）

**当前方案C → 未来方案B的升级路径**：

```
Phase 1（已完成）: 数据源分离
- AdminSecrets表（Auth模块）
- Users表（User模块）
- UserType字段路由

Phase 2（技术债TD-002）: Token策略差异化
- SuperAdmin: 5分钟AccessToken
- User: 15分钟AccessToken
- 引入TokenPolicyProvider

Phase 3（如需）: 完全独立认证
- POST /api/v1/auth/admin/login（SuperAdmin专用）
- POST /api/v1/auth/user/login（User专用）
- 两套独立的JwtService配置
```

**触发条件**：
- 安全审计要求SuperAdmin使用更严格策略
- SuperAdmin权限滥用事件频发
- 团队规模扩大需要更细粒度的权限管理

---

**签名**: 本ADR记录了Token认证安全重构的完整决策过程，包括方案选择、配置确认、技术债记录。所有决策基于安全优先原则与MVP成本平衡，为未来演进保留了清晰路径。

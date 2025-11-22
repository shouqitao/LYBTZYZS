# ADR-010: 超级管理员属于Auth模块而非User模块

**日期**: 2025-11-06
**状态**: Accepted
**决策者**: 项目架构团队
**标签**: #架构 #认证 #权限管理 #模块职责

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-010 |
| **创建日期** | 2025-11-06 |
| **最后更新** | 2025-11-06 |
| **状态** | Accepted |
| **决策者** | 项目架构团队 |
| **影响范围** | Auth模块、User模块、JWT Token刷新机制 |
| **相关Issue** | #1838, #1861 |
| **取代ADR** | 无 |

---

## 🎯 背景（Context）

### 问题描述

系统需要一个具有最高权限的**超级管理员（SuperAdmin）**角色，用于：
1. **系统初始化**：创建第一个管理员账号、配置系统参数
2. **权限管理**：管理所有用户和角色权限
3. **系统维护**：执行敏感操作（如数据库清理、日志查看）
4. **应急处理**：在普通管理员无法处理时介入

### 当前状态

项目有两个关键模块：
- **Auth模块**：负责身份认证、JWT Token管理、登录/登出
- **User模块**：负责普通用户管理（医生、护士、管理员等）

### 问题影响

**关键架构决策**：超级管理员应该属于哪个模块？

如果归属不清，会导致：
- **职责混乱**：Auth和User模块都可能管理超级管理员
- **数据不一致**：超级管理员可能同时存在于Auth表和User表
- **权限验证复杂**：需要检查多个数据源
- **Token刷新失败**：RefreshToken在User表中找不到超级管理员（Issue #1861）

---

## ✅ 决策（Decision）

**超级管理员属于Auth模块，不属于User模块**：

### 核心原则

1. **模块归属**：
   - ✅ 超级管理员在 **Auth 模块** 中管理
   - ❌ 超级管理员 **不在 User 表** 中存储
   - ✅ 不通过 User 模块的 Repository/Service 访问超级管理员

2. **权限特性**：
   - ✅ 超级管理员拥有 **最高权限**
   - ✅ 拥有所有普通用户（User）该有的权限
   - ✅ 还拥有额外的系统级权限（如数据库管理、系统配置）

3. **数据隔离**：
   - ✅ 超级管理员有独立的数据存储
   - ✅ 与普通用户数据物理隔离
   - ✅ 避免误操作影响超级管理员账号

### 技术实现

**数据存储方案**：

```csharp
// Auth模块 - 超级管理员实体（独立表）
public class SuperAdmin
{
    public Guid Id { get; set; }
    public string Username { get; set; }  // 固定："superadmin"
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

// User模块 - 普通用户实体（独立表）
public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string PasswordHash { get; set; }
    public UserRole Role { get; set; }  // Doctor, Nurse, Admin（但不包括SuperAdmin）
    public CommonStatus Status { get; set; }
    // ... 其他用户字段
}
```

**JWT Token生成（区分用户类型）**：

```csharp
// Auth模块 - JWT Token生成
public class JwtTokenService
{
    public TokenResponse GenerateToken(string userId, string username, string userType)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim("user_type", userType),  // "superadmin" 或 "user"
            // SuperAdmin额外Claim
            new Claim(ClaimTypes.Role, "SuperAdmin"),
            new Claim("permission", "all")  // 特殊标记：所有权限
        };

        // 生成AccessToken和RefreshToken
        return new TokenResponse
        {
            AccessToken = GenerateAccessToken(claims),
            RefreshToken = GenerateRefreshToken(userId, userType)
        };
    }
}
```

**Token刷新逻辑（根据用户类型路由）**：

```csharp
// Auth模块 - RefreshToken处理
public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
{
    // 1. 验证RefreshToken
    var tokenData = ValidateRefreshToken(refreshToken);
    var userId = tokenData.UserId;
    var userType = tokenData.UserType;  // "superadmin" 或 "user"

    // 2. 根据用户类型路由到不同的验证逻辑
    if (userType == "superadmin")
    {
        // 从Auth模块的SuperAdmin表验证
        var superAdmin = await _superAdminRepository.GetByIdAsync(userId);
        if (superAdmin == null)
            throw new UnauthorizedException("超级管理员不存在");

        return GenerateToken(superAdmin.Id, superAdmin.Username, "superadmin");
    }
    else
    {
        // 从User模块的User表验证
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new UnauthorizedException("用户不存在");

        return GenerateToken(user.Id, user.UserName, "user");
    }
}
```

**权限验证中间件**：

```csharp
// Auth模块 - 权限验证
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userType = context.User.FindFirst("user_type")?.Value;

        // SuperAdmin绕过所有权限检查
        if (userType == "superadmin")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 普通用户按正常权限检查
        if (HasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ **职责清晰**：Auth模块负责所有身份认证相关，包括超级管理员
- ✅ **数据隔离**：超级管理员与普通用户数据物理隔离，降低误操作风险
- ✅ **权限简化**：SuperAdmin自动拥有所有权限，无需复杂的权限表
- ✅ **Token刷新正确**：RefreshToken根据用户类型路由到正确的数据源
- ✅ **安全性提升**：超级管理员账号不在User表中，降低被篡改风险
- ✅ **扩展性强**：未来可添加更多系统级账号类型（如审计员、系统监控员）

### 缺点（Cons）

- ❌ **代码复杂度增加**：需要区分用户类型，路由到不同的验证逻辑
- ❌ **数据重复**：超级管理员和普通管理员的部分字段重复（如Username、PasswordHash）
- ❌ **查询复杂**：全局用户列表需要合并Auth表和User表
- ❌ **迁移困难**：如果已有系统将SuperAdmin存在User表，需要数据迁移

### 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Token刷新逻辑bug | 用户无法长时间使用 | 完善单元测试，覆盖SuperAdmin和User两种场景 |
| 超级管理员账号丢失 | 系统无法管理 | 提供初始化脚本，可重新创建SuperAdmin |
| 权限验证遗漏 | SuperAdmin权限未生效 | 在所有权限验证点检查user_type，确保SuperAdmin绕过 |
| 数据迁移失败 | 现有SuperAdmin无法登录 | 编写迁移脚本和回滚方案 |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: 超级管理员存储在User表（统一管理）

**描述**: 将SuperAdmin作为特殊角色存储在User表中，通过Role字段区分

**示例**：
```csharp
public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public UserRole Role { get; set; }  // 包含 SuperAdmin
    // ...
}

public enum UserRole
{
    Doctor = 1,
    Nurse = 2,
    Admin = 3,
    SuperAdmin = 999  // 特殊角色
}
```

**优点**:
- ✅ 数据结构统一，无需额外的SuperAdmin表
- ✅ 查询简单，所有用户在一张表
- ✅ Token刷新逻辑统一，无需区分用户类型

**缺点**:
- ❌ **职责混乱**：User模块需要处理系统级账号
- ❌ **安全风险**：SuperAdmin与普通用户在同一表，容易被误操作
- ❌ **权限复杂**：需要在User模块中硬编码SuperAdmin的特殊权限逻辑
- ❌ **违反模块职责**：User模块应该只管理业务用户，不应管理系统账号

**为什么未采纳**: 违反模块职责单一原则，增加安全风险

---

### 方案B: 超级管理员存储在配置文件（硬编码）

**描述**: 将SuperAdmin账号密码写在appsettings.json中，不存数据库

**示例**：
```json
{
  "SuperAdmin": {
    "Username": "superadmin",
    "PasswordHash": "hashed_password_here"
  }
}
```

**优点**:
- ✅ 简单直接，无需数据库表
- ✅ 配置灵活，可通过环境变量修改

**缺点**:
- ❌ **安全性极差**：密码存在配置文件，容易泄露
- ❌ **无法审计**：无法记录SuperAdmin的操作日志和登录历史
- ❌ **无法修改**：无法通过UI修改SuperAdmin密码
- ❌ **无Token刷新**：配置文件无法存储RefreshToken

**为什么未采纳**: 安全性和可维护性极差

---

### 方案C: 超级管理员存储在Auth表，但实现User接口（混合方案）

**描述**: SuperAdmin在Auth模块存储，但实现IUser接口，可被User模块部分访问

**优点**:
- ✅ 数据隔离，但保持接口统一

**缺点**:
- ❌ **接口污染**：IUser需要包含SuperAdmin的特殊字段
- ❌ **职责不清**：User模块需要知道SuperAdmin的存在
- ❌ **维护复杂**：接口变更影响两个模块

**为什么未采纳**: 增加模块耦合，违反清晰的职责划分

---

## 🏗️ 架构例外（Architecture Exceptions）

**无架构例外**：此决策符合三层架构和模块职责单一原则。

**模块职责明确**：
- **Auth模块**：负责身份认证和授权，包括SuperAdmin
- **User模块**：负责业务用户管理，不涉及系统账号

---

## 📚 参考资料（References）

- **相关Issue**:
  - #1838: 实现JWT Token自动刷新机制
  - #1861: Token刷新失败 - "用户不存在"错误
- **架构文档**:
  - `docs/explanation/architecture/server/README.md`
  - `docs/explanation/architecture/server/auth-architecture.md`
- **代码位置**:
  - `src/Server/LYBT.Module.Auth/` - Auth模块
  - `src/Server/LYBT.Module.Users/` - User模块
- **数据库**:
  - `SuperAdmins` 表（Auth模块）
  - `Users` 表（User模块）

---

## 📝 实施计划（Implementation Plan）

### Phase 1: 数据迁移（如需要）
- [ ] 检查现有SuperAdmin是否在User表中
- [ ] 创建SuperAdmins表迁移脚本
- [ ] 执行数据迁移
- [ ] 验证迁移结果

### Phase 2: Token刷新逻辑修复（Issue #1861）
- [ ] 在RefreshToken中添加user_type字段
- [ ] 实现用户类型路由逻辑
- [ ] 修复"用户不存在"错误
- [ ] 测试SuperAdmin和User的Token刷新

### Phase 3: 权限验证增强
- [ ] 在所有权限验证点检查user_type
- [ ] 确保SuperAdmin绕过所有权限检查
- [ ] 添加权限验证单元测试

### Phase 4: 文档更新
- [x] 创建ADR-010记录架构决策
- [ ] 更新Auth架构文档
- [ ] 更新API文档说明SuperAdmin特殊性
- [ ] 编写SuperAdmin管理指南

---

## ✅ 验收标准（Acceptance Criteria）

- [ ] SuperAdmin数据存储在Auth模块的独立表
- [ ] Token刷新支持SuperAdmin和User两种类型
- [ ] SuperAdmin自动拥有所有权限
- [ ] RefreshToken不再返回"用户不存在"错误
- [ ] 编译通过（0 errors, 0 warnings）
- [ ] 相关单元测试通过
- [ ] 文档更新完成

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-11-06 | v1.0 | 创建ADR-010，记录超级管理员架构决策 | Claude/项目团队 |

---

**创建者**: Claude Code
**审核者**: 待人工审核
**批准者**: 项目架构团队

---

## 💡 最佳实践建议

### SuperAdmin管理规范

1. **账号数量限制**：
   - 建议只保留 **1个** SuperAdmin账号
   - 如需多个，应明确划分职责（如主SuperAdmin、备用SuperAdmin）

2. **密码安全**：
   - SuperAdmin密码必须 **强密码**（至少16位，包含大小写、数字、特殊字符）
   - 定期修改密码（建议3个月一次）
   - 禁止与个人账号共用密码

3. **操作审计**：
   - 记录所有SuperAdmin操作日志
   - 关键操作需要二次验证（如删除大量数据）
   - 定期审查SuperAdmin操作记录

4. **应急恢复**：
   - 提供SuperAdmin密码重置脚本
   - 备份SuperAdmin账号信息
   - 确保至少2人知道如何恢复SuperAdmin

### Token刷新最佳实践

1. **用户类型识别**：
   - 始终在JWT Claim中包含 `user_type` 字段
   - RefreshToken验证时优先检查用户类型

2. **错误处理**：
   - 区分"用户不存在"和"权限不足"错误
   - 提供清晰的错误提示帮助定位问题

3. **安全考虑**：
   - SuperAdmin的RefreshToken有效期可以更短（如7天）
   - 记录所有Token刷新行为，用于安全审计

### 示例：完整的Token刷新实现

```csharp
public class AuthService : IAuthService
{
    private readonly ISuperAdminRepository _superAdminRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            // 1. 验证RefreshToken基本有效性
            var tokenData = _jwtTokenService.ValidateRefreshToken(refreshToken);
            var userId = tokenData.UserId;
            var userType = tokenData.UserType;

            _logger.LogInformation("开始刷新Token: UserId={UserId}, UserType={UserType}", userId, userType);

            // 2. 根据用户类型路由
            if (userType == "superadmin")
            {
                return await RefreshSuperAdminTokenAsync(userId);
            }
            else if (userType == "user")
            {
                return await RefreshUserTokenAsync(userId);
            }
            else
            {
                throw new UnauthorizedException($"未知用户类型: {userType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token刷新失败: RefreshToken={Token}", refreshToken);
            throw;
        }
    }

    private async Task<TokenResponse> RefreshSuperAdminTokenAsync(Guid userId)
    {
        // 从Auth模块验证SuperAdmin
        var superAdmin = await _superAdminRepository.GetByIdAsync(userId);

        if (superAdmin == null)
        {
            _logger.LogWarning("SuperAdmin不存在: UserId={UserId}", userId);
            throw new UnauthorizedException("超级管理员不存在");
        }

        _logger.LogInformation("SuperAdmin Token刷新成功: Username={Username}", superAdmin.Username);

        return _jwtTokenService.GenerateToken(
            superAdmin.Id.ToString(),
            superAdmin.Username,
            "superadmin"
        );
    }

    private async Task<TokenResponse> RefreshUserTokenAsync(Guid userId)
    {
        // 从User模块验证普通用户
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("用户不存在: UserId={UserId}", userId);
            throw new UnauthorizedException("用户不存在");
        }

        if (user.Status != CommonStatus.Enabled)
        {
            _logger.LogWarning("用户已禁用: UserId={UserId}", userId);
            throw new UnauthorizedException("用户已被禁用");
        }

        _logger.LogInformation("User Token刷新成功: Username={Username}", user.UserName);

        return _jwtTokenService.GenerateToken(
            user.Id.ToString(),
            user.UserName,
            "user"
        );
    }
}
```

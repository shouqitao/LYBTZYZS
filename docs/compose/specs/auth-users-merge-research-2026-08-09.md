# 任务：Auth+Users 模块合并前深度调研

> 范围：只读调研，不修改代码
> 工具：serena（符号引用精确分析）+ codebase-memory + sequentialthinking
> 产出：调研报告 docs/compose/reports/auth-users-merge-research-2026-08-09.md

## 调研维度（按模块颗粒）

### 1. Auth 模块（LYBT.Module.Auth）
- **全部文件/类型/方法清单**（每个方法的签名、可见性、行号）
- **实体关系**：AuthSession（外键指向 ApplicationUser？）、SecurityAuditLog、SystemLog
- **DbContext 布局**：AuthDbContext（3 DbSet + OnModelCreating 配置细节）
- **接口清单**：IJwtService / IAuthSessionRepository / ISecurityAuditRepository / ISecurityAuditService — 每个接口的方法签名 + 实现类位置
- **跨模块调用点**：IUserCrossModuleService 在 Auth 内部的使用（serena 精确查注入点和调用方法）
- **DI 注册**：AuthModule.cs（AddAuthModule）的完整注册内容
- **测试覆盖**：tests/ 中 Auth 相关测试文件 + 测试用例数

### 2. Users 模块（LYBT.Module.Users）
- **全部文件/类型/方法清单**（每个方法的签名、可见性、行号）
- **实体关系**：ApplicationUser（继承 IdentityUser<Guid>，业务字段完整列表）
- **DbContext 布局**：UsersDbContext（继承 IdentityDbContext）
- **接口清单**：IUserService / IUserRepository / IAuthCrossModuleService — 每个接口的方法签名 + 实现类位置
- **Command/Handler 完整列表**（每个 Command 的 Handler + Validator + 对应 API 端点）
- **跨模块调用点**：IAuthCrossModuleService 在 Users 内部的使用
- **DI 注册**：UsersModule.cs（AddUsersModule）的完整注册内容
- **Identity 集成**：UserManager/SignInManager 的注册和使用情况
- **测试覆盖**：tests/ 中 Users 相关测试文件 + 测试用例数

### 3. 跨模块关系（serena 符号引用核心）
- Auth→Users 的所有依赖链（serena 精确查：每个 IUserCrossModuleService 的注入点和调用方法）
- Users→Auth 的所有依赖链（serena 精确查：每个 IAuthCrossModuleService 的注入点和调用方法）
- **外部模块对 Auth/Users 的依赖**：MedicalCase / Registration / 其他模块
- **Desktop 侧对应**：IApiClientAuth / IApiClientUsers 接口和实现
- **LocalWebAPI 侧对应**：AuthController / UsersController 的双轨实现

### 4. 基础设施层
- **SecurityAudit**：SecurityAuditRepository / SecurityAuditService — 完整职责
- **JwtService**：Token 生成/刷新/验证 — 完整方法列表
- **IdentitySeedData**：初始化逻辑

### 5. 历史重构记录
- 用户提到"之前要求重构过一次"——grep 查 ADR / 蓝图/ 总账中 Auth/Users 相关的重构记录

## 约束
- 只写 docs/compose/reports/ 报告
- 不修改任何 src/tests 代码
- 用 serena 做精确符号引用（注入点/调用链/实现位置）

# LYBTZYZS WebApi 架构审查报告

**审查日期**: 2026-03-31
**审查工具**: dotnet-architect skill, explore agent
**审查范围**: WebApi 项目完整架构

---

## 一、总体评价: ⭐⭐⭐⭐ (4/5)

| 维度 | 评分 | 说明 |
|------|------|------|
| **模块化** | ⭐⭐⭐⭐⭐ | 8个业务模块独立封装，DI注册标准化 |
| **安全性** | ⭐⭐⭐⭐ | JWT+策略授权+CSP+速率限制 |
| **数据访问** | ⭐⭐⭐⭐ | Repository模式+软删除+EF Core优化 |
| **CQRS** | ⭐⭐⭐⭐ | MedicalCase模块使用Command/Query/State分离 |
| **跨模块解耦** | ⭐⭐⭐⭐⭐ | 通过CrossModuleService避免直接依赖 |
| **健康检查** | ⭐⭐⭐⭐⭐ | DB连接, Redis, 启动诊断 |
| **测试覆盖** | ⭐⭐⭐⭐ | 完整的架构/单元/集成/E2E测试金字塔 |

---

## 二、架构优点

### 2.1 模块化架构

- **8个业务模块**: Auth, Formula, Herbs, MedicalCase, Patients, Registration, Sync, Users
- **DI 注册标准化**: 每模块有独立的扩展方法 (AddAuthModule, AddFormulaModule 等)
- **中间件管道**: 6阶段 (错误处理→性能→路由→认证→缓存→健康检查)

### 2.2 安全层

- **认证**: JWT Bearer, 多密钥支持, 32字符最小密钥强度
- **授权策略**: AdminOnly, DoctorOrAdmin, PatientAccess, SuperAdminOnly
- **安全头**: CSP (生产严格/开发报告模式), HSTS, X-Frame-Options
- **速率限制**: 登录5次/60秒/IP, API 100次/分钟/IP
- **输入验证**: FluentValidation 全局注册, ApiResponse 统一格式
- **敏感数据**: SensitiveDataMasker, BCrypt 密码哈希

### 2.3 数据访问

- **DbContext**: CoreDbContext, LYBTDbContext (基础设施层)
- **Repository 模式**: 每模块独立 Repository (I{Entity}Repository)
- **软删除**: IsDeleted 标志 + 全局查询过滤器
- **优化**: AsNoTracking, AsSplitQuery, 编译查询

### 2.4 测试架构

#### 测试金字塔

| 层级 | 项目 | 框架 | 覆盖范围 |
|------|------|------|----------|
| **架构** | LYBT.Tests.Architecture | NetArchTest | 边界约束、命名规范 |
| **单元** | LYBT.Tests.Server.Unit | xUnit+NSubstitute+FluentAssertions | 控制器、验证器、实体 |
| **集成** | LYBT.Tests.Server | WebApplicationFactory+Respawn | API端点、数据库交互 |
| **桌面单元** | LYBT.Tests.Desktop/PureLogic | xUnit+NSubstitute | ViewModel、业务逻辑 |
| **桌面E2E** | LYBT.Tests.Desktop/EndToEnd | FlaUI | 完整用户流程 |

#### 测试基础设施

- **WebApplicationFactory**: Server和Desktop都有独立实现
- **Respawn**: 用于集成测试的数据库重置
- **TestDataBuilders**: UserBuilder, PatientBuilder, MedicalCaseBuilder等
- **DomainFixtures**: AuthUsersFixture, ClinicalDataFixture等

---

## 三、需要改进的问题

### 3.1 高优先级

#### 1. CORS 策略过宽 (安全问题)

```csharp
// 当前: AllowAnyOrigin - 过于宽松
policy.AllowAnyOrigin()
      .AllowAnyHeader()
      .AllowAnyMethod();

// 建议: 白名单策略
policy.WithOrigins("https://app.lybt.com", "https://localhost:3000")
      .AllowAnyHeader()
      .AllowMethods("GET", "POST", "PUT", "DELETE");
```

**文件**: `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs`

#### 2. 大型服务类 (God Class 反模式)

| 服务 | 行数 | 建议拆分 |
|------|------|----------|
| AuthService | **845行** | AuthLoginService, AuthTokenService, AuthPolicyService |
| UserService | **400+行** | UserQueryService, UserCommandService |
| MedicalCaseCommandService | **过大** | 拆分为多个Command Handler |

**文件**:
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
- `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

#### 3. DbContext 直接使用

```csharp
// 当前: 部分服务绕过Repository
public class SomeService
{
    private readonly DbContext _db; // ❌ 直接注入DbContext
}

// 建议: 统一使用Repository
public class SomeService
{
    private readonly ISomeRepository _repo; // ✅ 通过Repository
}
```

**文件**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`

### 3.2 中优先级

#### 4. 方法命名不一致

- `GetPagedAsync` vs `GetPagedListAsync` - 统一为 `GetPagedAsync`

#### 5. 死代码清理

- 部分Repository方法标记为 `[Obsolete]` 但仍在调用
- 建议: 清理或移除废弃方法

#### 6. 异步模式不统一

- 部分方法缺少 `CancellationToken` 参数
- 部分方法使用 `Task.FromResult` 而非真正异步

---

## 四、关键文件路径

### 4.1 WebApi 核心

| 文件 | 作用 |
|------|------|
| `src/Server/Services/LYBT.WebAPI/Program.cs` | 应用入口点 |
| `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs` | 中间件管道配置 |
| `src/Server/Services/LYBT.WebAPI/Extensions/AuthenticationServiceCollectionExtensions.cs` | 认证配置 |
| `src/Server/Services/LYBT.WebAPI/Middleware/SecurityHeadersMiddleware.cs` | 安全头中间件 |

### 4.2 模块 DI

| 模块 | DI 文件 |
|------|---------|
| Auth | `src/Server/Modules/LYBT.Module.Auth/AuthModule.cs` |
| Formula | `src/Server/Modules/LYBT.Module.Formula/FormulaModule.cs` |
| Herbs | `src/Server/Modules/LYBT.Module.Herbs/HerbsModule.cs` |
| MedicalCase | `src/Server/Modules/LYBT.Module.MedicalCase/MedicalCaseModule.cs` |
| Patients | `src/Server/Modules/LYBT.Module.Patients/PatientsModule.cs` |
| Registration | `src/Server/Modules/LYBT.Module.Registration/RegistrationModule.cs` |
| Sync | `src/Server/Modules/LYBT.Module.Sync/SyncModule.cs` |
| Users | `src/Server/Modules/LYBT.Module.Users/UsersModule.cs` |

### 4.3 测试核心

| 文件 | 作用 |
|------|------|
| `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs` | 集成测试基类 |
| `tests/LYBT.Tests.Server/_Infrastructure/ServerFixture.cs` | 服务器集成测试夹具 |
| `tests/LYBT.Tests.Desktop/Integration/Fixtures/WebApiFixture.cs` | 桌面WebApi集成夹具 |
| `tests/LYBT.Tests.Architecture/ArchTests.cs` | 架构边界测试 |

---

## 五、改进行动项

### 5.1 立即修复 (1天内)

| 任务 | 优先级 | 负责人 | 状态 |
|------|--------|--------|------|
| CORS 策略收紧 - 安全问题 | P0 | - | 待办 |
| 清理废弃代码 | P1 | - | 待办 |

### 5.2 短期优化 (1-2周)

| 任务 | 优先级 | 预计工时 | 状态 |
|------|--------|----------|------|
| 拆分 AuthService | P1 | 2天 | 待办 |
| 拆分 UserService | P1 | 1天 | 待办 |
| 统一 Repository 使用规范 | P2 | 1天 | 待办 |
| 统一方法命名 | P2 | 0.5天 | 待办 |

### 5.3 长期改进 (1-2月)

| 任务 | 优先级 | 预计工时 | 状态 |
|------|--------|----------|------|
| 引入 MediatR 或其他 CQRS 框架 | P2 | 1周 | 规划中 |
| 统一异步模式 (CancellationToken) | P2 | 1周 | 规划中 |
| 扩展测试覆盖度 | P3 | 2周 | 规划中 |

---

## 六、架构约束 (来自 AGENTS.md)

1. **Architecture First** - 优先保障架构完整性
2. **Root Cause Analysis** - 不做表面补丁
3. **Test Coverage** - 新功能必须包含测试
4. **Documentation** - 架构决策和 API 变更更新 `docs/`

### 常见坑点

- `FindAsync` 应用全局查询过滤器 (`IsDeleted`) - 软删除记录需 `IgnoreQueryFilters()`
- WPF Desktop 测试需要 `net8.0-windows` 目标框架 - 不能与 Server 测试混用
- `MedicalCase.HasPrescription` 是计算属性，依赖 `PrescriptionId.HasValue` - Mapper 必须显式设置

---

## 七、下一步建议

1. **立即**: 处理 CORS 安全问题
2. **本周**: 拆分 AuthService 和 UserService
3. **本月**: 统一 Repository 使用规范
4. **下月**: 评估 CQRS 框架引入

---

**审查工具版本**: v1.0
**下次审查建议**: 2026-04-30

# 测试策略: Postman 与 .NET 集成测试的互补定位

**创建日期**: 2026-04-01  
**决策**: 保留所有 .NET 集成测试,与 Postman 测试形成互补

---

## 决策摘要

**结论**: **不移除** `tests/LYBT.Tests.Server/Features/` 中的 HTTP 集成测试

**原因**: Postman 测试与 .NET 集成测试测试**不同层面**,两者互补而非重复

---

## 测试分层对比

| 维度 | Postman Collection | .NET Integration Tests |
|------|-------------------|----------------------|
| **测试类型** | 黑盒 API 端到端测试 | 白盒集成测试 (WebApplicationFactory) |
| **测试焦点** | API 契约、响应格式、基本流程 | 业务逻辑、权限边界、数据库状态 |
| **权限测试** | 有限 (仅测试有效令牌路径) | 完整 (系统性测试 403/401 边界) |
| **数据库** | 共享测试数据,无自动清理 | Respawn 自动回滚,完全隔离 |
| **边界条件** | 手工覆盖 (依赖人工维护) | 系统性覆盖 (Theory + 参数化) |
| **业务逻辑** | 仅验证 HTTP 响应 | 深度验证业务规则 (如重复用户名拒绝) |
| **执行环境** | 外部 CLI (Newman),需启动 WebAPI | xUnit 内嵌,WebApplicationFactory 自托管 |
| **CI/CD 集成** | 独立步骤,需独立 Docker 服务 | dotnet test 一步完成 |
| **回归保护** | 基础契约保护 | 深度业务逻辑保护 |

---

## Postman 的独特价值

### 1. API 契约测试
- **响应格式验证**: 确保 ApiResponse 结构一致性
- **分页格式验证**: 验证 totalCount、currentPage、pageSize 字段存在
- **错误响应格式**: ProblemDetails / ValidationProblemDetails 一致性

### 2. 外部集成视角
- **模拟真实客户端**: 与 Desktop/Mobile 客户端相同的 HTTP 调用方式
- **环境变量管理**: 测试不同环境 (dev/staging/prod) 的配置
- **跨域测试**: 验证 CORS 配置 (Desktop 无法测试)

### 3. 手工探索性测试
- **Setup 辅助请求**: 快速创建测试数据 (如挂号、医案)
- **调试工具**: 开发时验证新增端点
- **文档化**: 可导出为 Swagger 补充文档

### 4. 覆盖率指标
- **端点覆盖率**: 确保所有 Controller 端点至少可达 (100% 覆盖)
- **版本兼容性**: 测试 API v1/v2 共存场景

---

## .NET 集成测试的独特价值

### 1. 权限策略边界测试

**Postman 局限性**:
- 仅能测试"有效令牌"路径 (如 Doctor 访问 /patients)
- 难以系统性测试"无效令牌"路径 (如 Doctor 访问 /users)

**示例** (US_User_MustHaveTests.cs):
```csharp
[Fact]
public async Task US_USER_001_CreateUser_DoctorCannotCreate_Returns403()
{
    // Arrange
    var doctorClient = await LoginAsDoctorAsync();
    var payload = UserBuilder.Default().Build();

    // Act
    var response = await doctorClient.PostAsJsonAsync("/api/v1/users", payload);

    // Assert
    response.ShouldBeForbidden(); // 验证 403 禁止访问
}
```

**Postman 等价测试成本**:
- 需为每个 Forbidden 场景创建独立请求
- 需手工维护多个用户角色的令牌 (Admin、Doctor、Patient、Guest)
- 缺少参数化能力,无法批量测试权限矩阵

### 2. 数据库事务和状态验证

**Postman 局限性**:
- 无法回滚数据库状态 (依赖手工清理或 Setup 脚本)
- 无法验证数据库中间状态 (如软删除标记、审计日志)

**示例** (软删除验证):
```csharp
[Fact]
public async Task US_USER_003_DeleteUser_SetsIsDeletedFlag()
{
    // Arrange
    var adminClient = await LoginAsAdminAsync();
    var user = await CreateUserAsync();

    // Act
    var response = await adminClient.DeleteAsync($"/api/v1/users/{user.Id}");

    // Assert
    response.ShouldBeOk(); // HTTP 200 (软删除)
    
    // 验证数据库状态 (Postman 无法做到)
    var dbUser = await Fixture.ExecuteDbContextAsync(async db =>
        await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id));
    dbUser.IsDeleted.Should().BeTrue();
    dbUser.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
}
```

**Postman 只能验证**: HTTP 200 + ApiResponse.Success = true  
**无法验证**: IsDeleted 标记、DeletedAt 时间戳、全局查询过滤器生效

### 3. 业务逻辑边界测试

**示例** (重复用户名拒绝):
```csharp
[Fact]
public async Task US_USER_001_CreateUser_DuplicateUsername_ReturnsError()
{
    // Arrange
    var adminClient = await LoginAsAdminAsync();
    var uniqueName = $"dup_{Guid.NewGuid():N}"[..12];
    var payload1 = UserBuilder.Default().WithUserName(uniqueName).Build();
    var payload2 = UserBuilder.Default().WithUserName(uniqueName).Build();

    // Act - 创建第一个用户
    var resp1 = await adminClient.PostAsJsonAsync("/api/v1/users", payload1);
    resp1.StatusCode.Should().Be(HttpStatusCode.Created);

    // Act - 尝试创建重名用户
    var resp2 = await adminClient.PostAsJsonAsync("/api/v1/users", payload2);

    // Assert
    resp2.StatusCode.Should().BeOneOf(
        new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict },
        "US-USER-001: duplicate username should be rejected");
}
```

**Postman 等价测试成本**:
- 需手工创建第一个用户 (Setup 请求)
- 需记录用户名用于第二次请求
- 测试后需手工清理数据 (否则影响后续运行)
- 无法自动化验证"拒绝原因"字段内容

### 4. 并发和事务测试

**示例** (批量操作原子性):
```csharp
[Fact]
public async Task US_USER_004_BatchDelete_PartialFailure_RollsBack()
{
    // Arrange
    var adminClient = await LoginAsAdminAsync();
    var user1 = await CreateUserAsync();
    var user2 = await CreateUserAsync();
    var nonExistentId = Guid.NewGuid();

    // Act - 批量删除,其中一个 ID 不存在
    var response = await adminClient.PostAsJsonAsync("/api/v1/users/batch-delete",
        new { Ids = new[] { user1.Id, nonExistentId, user2.Id } });

    // Assert - 验证全部回滚 (全有或全无)
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var user1AfterRollback = await GetUserByIdAsync(user1.Id);
    user1AfterRollback.Should().NotBeNull("rollback should keep user1 alive");
}
```

**Postman 无法测试**:
- 事务回滚行为 (需验证数据库状态)
- 部分失败的原子性保证

---

## 保留 .NET 集成测试的理由

### 1. 测试金字塔合规性

```
       ╱╲
      ╱UI╲        ← Postman (E2E API 契约测试)
     ╱════╲
    ╱ Intg ╲      ← .NET Integration Tests (业务逻辑 + 权限 + DB)
   ╱════════╲
  ╱  Unit   ╲    ← .NET Unit Tests (纯逻辑,无依赖)
 ╱══════════╲
```

**移除 .NET 集成测试 = 金字塔塌陷**:
- Unit Tests 无法覆盖跨层交互 (Controller → Service → Repository)
- Postman 成本过高且无法验证内部状态

### 2. 回归保护深度

| 场景 | Postman 保护 | .NET 保护 |
|------|-------------|----------|
| API 契约变更 | ✅ 强 | ✅ 强 |
| 响应格式变更 | ✅ 强 | ✅ 强 |
| 权限策略变更 | ⚠️ 弱 (仅测试有效路径) | ✅ 强 (测试所有边界) |
| 业务规则变更 | ⚠️ 弱 (需手工维护) | ✅ 强 (参数化测试) |
| 数据库约束变更 | ❌ 无法检测 | ✅ 强 (直接验证 DB) |
| 事务行为变更 | ❌ 无法检测 | ✅ 强 (回滚验证) |

### 3. 开发效率

**.NET 集成测试优势**:
- **快速反馈**: `dotnet test` 一步完成 (无需启动 WebAPI)
- **IDE 集成**: Visual Studio / Rider 内调试断点
- **并行执行**: xUnit Collection 隔离,测试并行运行
- **自动清理**: Respawn 自动回滚,无需手工维护测试数据

**Postman 劣势**:
- 需启动 WebAPI (增加 CI 步骤)
- 需独立 Newman CLI (额外依赖)
- 测试数据污染 (需手工清理或 Setup 脚本)

---

## 推荐测试策略

### Postman Collection 覆盖范围

✅ **应该测试**:
- **Happy Path**: 每个端点的标准成功路径
- **响应格式**: ApiResponse 结构、分页格式、ProblemDetails
- **基础权限**: 验证需要的令牌策略 (如 AdminOnly 需要 admin 令牌)
- **端点可达性**: 确保所有端点至少可访问 (100% 覆盖率)

❌ **不应测试**:
- 权限边界 (403/401 系统性测试 → .NET 集成测试)
- 业务逻辑边界 (重复用户名、软删除等 → .NET 集成测试)
- 数据库状态验证 → .NET 集成测试
- 并发和事务行为 → .NET 集成测试

### .NET 集成测试覆盖范围

✅ **应该测试**:
- **权限矩阵**: 所有角色 × 所有端点的权限边界 (403/401)
- **业务规则**: 唯一性约束、状态转换规则、软删除等
- **数据库状态**: 验证 IsDeleted、CreatedAt、UpdatedAt、审计日志等
- **事务行为**: 批量操作原子性、回滚验证
- **边界条件**: 参数化测试 (Theory) 覆盖边界值

❌ **不应测试** (委托给 Unit Tests):
- 纯业务逻辑 (无数据库交互)
- DTO 映射 (AutoMapper 单元测试)
- 工具类方法 (PasswordHasher、TokenGenerator 等)

---

## CI/CD 集成建议

### 推荐流程

```yaml
# .github/workflows/ci.yml
jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - run: dotnet test tests/LYBT.Tests.Desktop
      - run: dotnet test tests/LYBT.Tests.Architecture

  integration-tests:
    runs-on: ubuntu-latest
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
    steps:
      - run: dotnet test tests/LYBT.Tests.Server  # 包含所有集成测试

  postman-tests:
    runs-on: ubuntu-latest
    needs: integration-tests  # 依赖集成测试通过
    steps:
      - run: dotnet run --project src/Server/Services/LYBT.WebAPI &
      - run: npm install -g newman
      - run: newman run docs/06-operations/LYBTZYZS_API_Collection.json

  coverage-report:
    needs: [unit-tests, integration-tests]
    steps:
      - run: reportgenerator -reports:**/coverage.cobertura.xml
```

**关键决策**:
1. **.NET 集成测试先行**: 确保业务逻辑正确后再测试 API 契约
2. **Postman 作为冒烟测试**: 验证生产环境可达性 (staging/prod 部署后)
3. **覆盖率报告合并**: .NET 测试贡献主要覆盖率,Postman 贡献端点覆盖率

---

## 附录: 保留的 .NET 测试文件清单

### Features 目录 (User Story 驱动)

| 文件 | 测试范围 | 保留原因 |
|------|---------|---------|
| `US_User_MustHaveTests.cs` | 用户 CRUD、权限、批量操作 | ✅ 权限边界 + 业务规则 + DB 状态 |
| `US_Patient_MustHaveTests.cs` | 患者 CRUD、身份证验证 | ✅ 唯一性约束 + 软删除 |
| `US_Herb_MustHaveTests.cs` | 草药 CRUD、名称唯一性 | ✅ 唯一性约束 + 分页验证 |
| `US_Formula_MustHaveTests.cs` | 方剂 CRUD、草药关联 | ✅ 关联数据完整性 |
| `US_MedicalCase_MustHaveTests.cs` | 医案 CRUD、聚合根验证 | ✅ DDD 聚合根规则 + 事务 |
| `US_Registration_MustHaveTests.cs` | 挂号流程、状态转换 | ✅ 状态机验证 + 并发控制 |
| `US_Sync_MustHaveTests.cs` | 离线同步、冲突解决 | ✅ 事务性同步逻辑 |
| `US_Auth_MustHaveTests.cs` | 登录、令牌刷新、权限 | ✅ JWT 生命周期 + 权限策略 |

### PureLogic 目录 (Service 层单元测试)

| 目录 | 测试范围 | 保留原因 |
|------|---------|---------|
| `PureLogic/Repositories/` | Repository 层逻辑 | ✅ EF Core 查询优化验证 |
| `PureLogic/Auth/` | 密码哈希、令牌生成 | ✅ 安全逻辑单元测试 |
| `PureLogic/Utilities/` | 工具类 (日期、字符串等) | ✅ 纯函数单元测试 |

### UserJourneys 目录 (端到端业务流程)

| 文件 | 测试范围 | 保留原因 |
|------|---------|---------|
| `UJ_CompleteRegistrationFlow.cs` | 挂号 → 诊断 → 开方 → 收费 | ✅ 跨模块业务流程验证 |

**总测试数**: ~1185 个 (保留全部)

---

## 结论

**最终决策**: ✅ **保留所有 .NET 集成测试**

**理由总结**:
1. Postman 与 .NET 测试**互补而非重复** (黑盒 vs 白盒)
2. .NET 测试提供**深度回归保护** (权限、业务规则、DB 状态、事务)
3. .NET 测试成本**更低** (WebApplicationFactory 自托管、Respawn 自动清理)
4. 符合**测试金字塔**最佳实践 (Unit → Integration → E2E)

**行动项**:
- ✅ Postman Collection 已达到 100% 端点覆盖
- ✅ .NET 集成测试保持当前覆盖范围 (~1185 tests)
- ⏸️ 暂不移除任何 HTTP 测试
- 📝 更新 `docs/05-development/testing.md` 引用此文档

**下一步**: 运行 Newman 验证 Postman Collection,确保所有端点可达。

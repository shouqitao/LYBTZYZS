# Test Architecture Refactoring Design

## Date: 2026-03-03
## Status: APPROVED
## Strategy: Infrastructure-First (策略 B)

---

## Background

上次审计 (2026-03-01~03) 完成了测试项目合并 (25 -> 5)，2366 tests 全绿。但运行时登录失败暴露了一个结构性问题: **测试全绿不代表代码正确**。

全量审计发现 26 个偏差 (2 CRITICAL, 12 HIGH, 12 MEDIUM)，分 4 大类:

1. **Mock 与生产实现不一致** (13 个) -- mock 方法签名/返回值已脱节
2. **测试适配了代码 Bug** (4 个) -- 修改期望值而非修复代码
3. **测试配置与生产分离** (6 个) -- appsettings.Test.json 禁用生产特性
4. **覆盖盲区** (3 个关键) -- 核心路径无测试

---

## Design Goals

1. **Mock 偏差消除**: Mock 行为与生产实现严格对齐
2. **测试与生产路径统一**: 种子数据、配置、启动路径复用生产逻辑
3. **防护机制**: 减少未来偏差再次出现的可能

---

## Phase Design

### Phase 1: 修复生产代码 Bug (先修代码，再修测试)

**原则**: 当测试期望与正确行为不符时，修复代码使其返回正确行为。

| Task | 描述 | 影响文件 |
|------|------|----------|
| 1.1 | MedicalCase CreateWhenActiveCase: InvalidOperationException -> BusinessException (500 -> 422) | MedicalCaseCommandService.cs, MedicalCaseIntegrationTests.cs |
| 1.2 | Users GetUsers_InvalidPage: 添加 page >= 1 验证 (500 -> 400) | UsersController.cs/UserService.cs, UserIntegrationTests.cs |
| 1.3 | DuplicateUsername 状态码统一为 409 Conflict | UserService.cs, UserIntegrationTests.cs |

### Phase 2: 统一测试配置与生产路径

| Task | 描述 | 影响文件 |
|------|------|----------|
| 2.1 | appsettings.Test.json 补全 PasswordPolicy/Session/UserManagement + ForceChangeOnFirstLogin=true | appsettings.Test.json |
| 2.2 | WebApiFixture 启用 AutoCreateOnStartup=true，复用生产初始化路径 | WebApiFixture.cs, appsettings.Test.json |
| 2.3 | 添加 RateLimiting 专项集成测试 (独立 Fixture) | 新文件: RateLimitingTests.cs |

### Phase 3: 修复 Server Unit Test Mock 偏差

| Task | 描述 | 偏差 ID |
|------|------|---------|
| 3.1 | AuthServiceTests 配置键修复 (Username -> UserName) | DIV-U01 |
| 3.2 | UserServiceTests async mock + 验证失败路径 | DIV-U02, DIV-U05 |
| 3.3 | PatientServiceTests GetPagedWithStatusFilterAsync mock | DIV-U03 |
| 3.4 | MedicalCaseCommandServiceTests 完善 mock 配置 | DIV-U04 |
| 3.5 | FormulaServiceTests 重载区分 | DIV-U07 |
| 3.6 | HerbServiceTests 引用检查补全 | DIV-U06 |

### Phase 4: 修复 Desktop 测试 + Architecture Tests

| Task | 描述 | 偏差 ID |
|------|------|---------|
| 4.1 | LoginCoordinator Local mode 测试补全 | DIV-D01 |
| 4.2 | Desktop E2E Fixture Repository 生命周期修复 | DIV-D02 |
| 4.3 | LoginViewModelTests UsernameChange 真实测试 | DIV-D04 |
| 4.4 | AuthenticationService LoginWithAutoTokenAsync 补全 | DIV-D05 |
| 4.5 | Architecture Tests 修复 (DataContext/ConfigDirectRead/白名单) | DIV-A01, DIV-A02, DIV-A04 |

### Phase 5: 全量验证 + 文档

| Task | 描述 |
|------|------|
| 5.1 | dotnet build + dotnet test 全量验证 |
| 5.2 | 26 个偏差逐项确认修复 |
| 5.3 | 更新项目文档 |

---

## Execution Dependencies

```
Phase 1 ──→ Phase 2
              ├──→ Phase 3 (可与 4 并行)
              └──→ Phase 4 (可与 3 并行)
                    └──→ Phase 5
```

---

## Risk Mitigation

| 风险 | 缓解措施 |
|------|----------|
| ForceChangeOnFirstLogin=true 导致大量测试失败 | 集成测试用户标记 MustChangePassword=false |
| AutoCreateOnStartup=true 与手动种子冲突 | WebApiFixture 使用 Upsert 模式覆盖 |
| RateLimiting 测试与并行执行冲突 | 独立 Fixture 和 Collection，串行执行 |
| Desktop E2E Transient 生命周期导致测试失败 | 逐步修改并验证 |

---

## Success Criteria

- [ ] 26 个偏差全部修复或标记为设计决策
- [ ] 所有测试通过 (2366+ tests)
- [ ] Mock 方法签名与当前生产接口完全匹配
- [ ] 测试配置包含所有生产必要配置节
- [ ] WebApiFixture 复用生产初始化路径

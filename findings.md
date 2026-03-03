# Architecture & Test Deep Audit - Findings

## Audit Date: 2026-03-01 ~ 2026-03-02
## Scope: 39 source projects + 25 test projects (~2,200 tests)

---

## CRITICAL: 测试置信度危机诊断 (Phase 1 已修复)

### 根因链 (3 层) -- 已修复

**Layer 1: Mock 掩盖真实依赖** -- 集成测试已补充边界场景
**Layer 2: 测试数据与生产不同步** -- WebApiFixture 已添加 sysadmin 种子
**Layer 3: Desktop 测试占位符** -- LoginViewModelTests 已重写 (27 个真实测试)

---

## Phase 2 规划分析 (2026-03-02)

### 测试项目清单 (25 个)

| 类别 | 项目数 | 测试数 | 处理 |
|------|--------|--------|------|
| Structure A (LYBT.Tests.*) | 5 | 1,173 | 保留为合并目标 |
| Structure B - UnitTests | 13 | 701 | 合并到 Tests.Unit |
| Structure B - IntegrationTests | 3 | 326 | 合并到 Tests.Server/Desktop.Integration |
| 其他 (Compat/Bench/Perf/Config) | 4 | 8+0+0+0 | 按需合并或保留 |
| **Total** | **25** | **~2,200** | **-> 10 项目** |

### 重叠分析

#### 完全重复 (16 tests, 必须去重)

| Structure A 文件 | Structure B 文件 | 测试数 |
|------------------|------------------|--------|
| Tests.Unit/Infrastructure/Services/BaseServiceTests.cs | Infrastructure.Tests/BaseServiceTests.cs | 12 |
| Tests.Unit/Infrastructure/Serialization/SensitiveDataJsonConverterTests.cs | Infrastructure.Tests/Serialization/SensitiveDataJsonConverterTests.cs | 4 |

#### Server 集成测试重叠 (互补为主)

| 端点 | Structure A | Structure B | 重叠 | B 独有 |
|------|-------------|-------------|------|--------|
| Auth | 19 | 3 | 0 | Token撤销/审计/轮换 |
| Herbs | 18 | 31 | ~8 CRUD | 导出/引用/批量 |
| Formulas | 16 | 28 | ~6 CRUD | 导出/引用/状态 |
| Patients | 23 | 17 | ~10 | 少量独有 |
| MedicalCases | 24 | 54 | ~12 | 权限/待完成/Issue修复 |
| Users | 24 | 17 | ~10 | 少量独有 |
| Sync | 25 | 17 | ~8 | 少量独有 |
| 其他 (B独有) | - | 65 | 0 | Diagnostics/Health/Middleware/Logging/Batch/Performance |

#### 无重叠的单元测试 (13 个项目)

Module 级 Service/Repository 测试仅在 Structure B，与 Structure A 零重叠:
- Auth (66), Users (34), Herbs (52), Patients (47), MedicalCase (39), Formula (28), Sync (63)
- WebAPI (38), Validators (125), Configuration (50), ExceptionHandling (70), Models (4)

### 集成测试基础设施对比

| 特性 | WebApiFixture (A, 保留) | IntegrationTestBase (B, 废弃) |
|------|------------------------|-------------------------------|
| 数据库 | LYBT_Test (Drop+Migrate) | LYBTDB (EnsureCreated) |
| 客户端 | Admin/Doctor/SysAdmin/Anonymous | 单一 Admin (随机UserId) |
| 共享模式 | xunit Collection + IAsyncLifetime | 每类独立 Factory (继承) |
| 种子数据 | Upsert 固定ID | 无统一种子 |
| 隔离性 | 更好 | 更差 |
| HostedService | 移除全部 | 按名称移除 3 个 |

**决策**: WebApiFixture 作为统一基础设施。吸收 IntegrationTestBase 的 `CreateJsonContent<T>()` 等辅助方法。

### 关键风险

1. **测试数据隔离**: WebAPI.IntegrationTests 每个类独立 Factory+DB，合并后共享 DB 可能交叉污染
   - 缓解: 每测试使用 Guid.NewGuid() 唯一标识
2. **csproj 依赖膨胀**: 合并 13 个项目到 Tests.Unit 会增加依赖
   - 缓解: 监控编译时间，>30s 则考虑拆分
3. **Namespace 冲突**: Structure B 用 `LYBT.Module.*.Tests`，需统一为 `LYBT.Tests.Unit.*`

### 去重规则

1. 完全相同: 保留断言更丰富版本
2. 功能重叠: 保留覆盖更深版本 (持久化验证、异常路径)
3. 互补: 全部保留，合并到同一文件
4. 原则: **两边取长补短**

---

## Phase 4-5 调研 (2026-03-03)

### 魔法常量分析

| 类别 | 数量 | 高优先 | 低优先 |
|------|------|--------|--------|
| 角色名 ("Doctor", "Admin"...) | 15+ | RoleConstants | - |
| 策略名 ("AdminOnly"...) | 20+ | PolicyConstants | - |
| HTTP Headers | 5+ | HttpHeaderConstants | - |
| 安全时间常量 (15, 5) | 6 | - | 后续迭代 |
| 缓存时间 | 6 | - | 后续迭代 |

**已有常量类**: SystemConstants (Desktop), ValidationConstants (Shared), ConfigurationSections (Shared)

### Guard 模式分析 -> SKIP

- 70+ null 检查，30+ 文件
- 模式高度分化: return null / throw BusinessException / return Result.Failure
- .NET 8 内置 `ArgumentNullException.ThrowIfNull()` 已覆盖参数验证
- YAGNI: 创建统一 Guard 反增复杂度

### HTTP 状态码分析

- 整体一致性: 92%
- 2 处直接 `UnprocessableEntity()` 绕过 `BusinessFail()`: PatientsController:163, HerbsController:134
- BusinessFail 所有调用正确返回 422
- NotFound 所有调用正确返回 404

### 日志级别分析

| 问题 | 数量 | 修复方向 |
|------|------|----------|
| 异常用 LogWarning | 2 | -> LogError (AuthService) |
| 非关键审计用 LogError | 1 | -> LogWarning (TokenRevocationService) |
| 权限拒绝用 LogWarning | 2 | -> LogInformation (BaseService) |
| 业务验证用 LogWarning | 5 | -> LogInformation (MedicalCase/Patient) |
| 缺少结构化参数 | 3 | 添加 [SVC] 前缀 (SyncService) |
| 缺失异常日志 | 1 | 添加 LogWarning (TokenManagementService) |
| 字符串拼接日志 | 1 | -> @Errors 结构化 (PatientService) |

### ViewModel Handler 提取 -> SKIP

- MedicalCaseWorkspaceViewModel: 1,275 行
- 已提取 3 个 Handler (1,197 行功能)
- 第 52 行注释: "MedicalCaseNavigationHandler已删除，逻辑内联到ViewModel"
- 前期已尝试并回退，导航逻辑与 ViewModel 状态耦合过深

---

## 架构审计 (已完成)

### 优势
- 层次隔离完美 (零违规, 零循环依赖)
- 构建配置集中化 (Directory.Build.props)
- DI 注册 95% 活跃
- 死代码近乎零

### 已修复项
- Authorization Handler XML 注释残留 (Phase 3 已清理)
- PrescriptionPrintService 裸 catch (Phase 3 已修复)
- LoginViewModelTests 占位符 (Phase 1 已重写)
- DatabaseInitializationService 无测试 (Phase 1 已创建)
- 集成测试缺少边界场景 (Phase 1 已补充)

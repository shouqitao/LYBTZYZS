# 测试代码审查报告（T1 只读调研）

> 日期：2026-08-11 | 范围：tests/ 三项目（只读，未修改任何代码）
> 动机：怀疑现有测试「为测试而测试」（形式主义、不测真业务）
> 基线：Architecture 12 文件 3,189 行 / Desktop 108 文件 22,032 行 / Server 65 文件 10,901 行（合计 36,122 行，1,303 个 Fact/Theory，20 处 Skip）

---

## 〇、总览（结论摘要）

**结论：测试质量两极分化——「为测试而测试」确实存在但非主流；结构性问题是测试环境依赖失控，而非断言形式主义。**

| 项目 | 判定 | 核心证据 |
|---|---|---|
| Architecture（83 规则） | ✅ **高质量** | 真实 NetArchTest 层约束断言 + AntiMock（Testing Trophy）规则 |
| Desktop（777 测试） | ⚠️ 中等，形式主义集中地 | 50 纯交互断言 + 恒真断言 + MasterDetail 模板复制 + 假业务 Integration |
| Server（359 方法/571 用例） | ✅ 中等偏上 | 零 mock + EF InMemory 真实实现；~25 trivial/恒真 + 结构性死代码 |

**三个 P0 结构性发现（非断言形式主义，但直接使测试失去价值）：**
1. **P0-06 根因确认**：Desktop 148 个纯 VM 单元测试被 `UserJourneyTestBase` 耦合到 SQL LocalDB；146 个 Integration 测试硬依赖 `localhost:5000` 运行中服务——环境不可用则全挂，P0-06「104 失败」的直接原因
2. **Server `_Infrastructure/` 9 个 SQL Server+Respawn 集成基建零消费者**（无任何测试类继承 `ServerFixture`/`IntegrationTestBase`）——宣称「真 SQL Server 集成测试」实际 100% 走 EF InMemory；`tests/AGENTS.md` 宣称 ~1185 tests 与实际 359 方法/571 用例脱节
3. **Desktop Integration 假业务测试**：`MedicalCaseTests.GetPermissions`/`GetAuditLogs` 只建数据+写日志、从不调用目标 API；`GetPendingCases` 无断言空壳

---

## 一、审查方法

- 三项目并行扫描（Architecture 自查 + Desktop/Server scout），依据 9 类形式主义证据清单：
  1. 无断言测试（方法体无 Assert/Should）
  2. 恒真/恒假断言（`true.Should().BeTrue()`、断言自赋值）
  3. Trivial 测试（构造不抛/getter setter 直通/空方法）
  4. 过度 mock（单测试 ≥8 mock）
  5. 测实现细节（反射调 protected/断言内部状态/Verify 调用顺序）
  6. 重复测试（同断言模板复制粘贴）
  7. Skip 测试（原因与占比）
  8. 假业务测试（mock 全链路后无真实逻辑被验证）
  9. 断言质量差（只断言不抛、宽松 Arg.Any 滥用）

---

## 二、统计基线

| 项目 | 文件 | 行数 | Fact/Theory | 断言调用 | Skip |
|---|---|---|---|---|---|
| LYBT.Tests.Architecture | 12（9 含测试） | 3,189 | 83 | — | 0 |
| LYBT.Tests.Desktop | 108 | 22,032 | 777（751 Fact+26 Theory） | 1,757（98 NSubstitute Received） | 19（12 活跃+7 注释死测试） |
| LYBT.Tests.Server | 65（46 含测试） | 10,901 | 359 方法/571 用例 | — | 1 |
| **合计** | **185** | **36,122** | **~1,303** | | **20** |

---

## 三、逐项目发现

### 3.1 LYBT.Tests.Architecture — 非形式主义，高质量

83 个 [Fact] 全部为真实架构约束断言：
- **NetArchTest 层规则**：Shell→Roles→Modules→Core→Shared 依赖方向、P07 模块间禁止直接引用、P08 跨模块接口、P10 Service 禁注入 DbContext（`ServerArchTests`/`DesktopLayerArchTests`）
- **DP10**：VM 禁止注入 IApiClient 子接口（反射检查构造函数参数，无豁免清单）
- **AntiMockRuleTests**（Testing Trophy 方法论）：`AM01`/`AM02` 断言 Server 测试程序集**零 NSubstitute 引用**；`AM03` 断言 Integration 测试禁用 EF InMemory——**这条规则与现状脱节**（见 3.3 P0-2：实际 Server 集成基建零消费者，规则保护的"真 SQL"从未被使用）
- 已知：`AggregateRootArchTests` 用 `Assert.Null` 断言已删除控制器不存在（测历史演进，有效）

### 3.2 LYBT.Tests.Desktop — 形式主义集中地

**P0 结构性：**
- **148 个纯 VM 单元测试挂 SQL LocalDB**：`Unit/` 下 12 个测试类继承 `UserJourneyTestBase` → `UserJourneyFixture` 每个实例建独立 `(localdb)\MSSQLLocalDB` 库——纯 VM 逻辑被无谓耦合到数据库
- **146 个 Integration 测试依赖运行中服务**：`WebApiE2ETestBase` 硬依赖 `appsettings.Test.json` 的 `http://localhost:5000` + 种子账号（Refit 客户端直连）——无服务即全挂（P0-06 失败面）
- **假业务测试**：`Integration/Modules/MedicalCaseTests.cs` 的 `GetPermissions`/`GetAuditLogs` 只建医案+写日志、**从不调用目标 API**；`GetPendingCases` 无断言空壳

**P1 断言质量：**
- **恒真断言**：`Integration/FrameworkVerificationTests.cs:175` `true.Should().BeTrue()` 收尾
- **纯交互断言（50 个）**：只 `Received(n)` 验证 mock 收到调用、零状态断言——
  - `Unit/ViewModels/NavigableViewModelBaseTests.cs` `ShowXxxMessageAsync_CallsToastService` 系列 4 个纯转发断言
  - `Unit/Clinical/CardReaderPureTests.cs` 6/11 为纯交互断言（测试名承诺行为、断言只验调用）
  - `Unit/Auth/LoginViewModelTests.cs` 5 个纯交互 + 3 个 `Task.Delay` 时序脆弱
- **过度 mock 装配**：`Unit/Formula/FormulaMasterDetailViewModelTests.cs` 构造 ~21 个 mock，仅 7 断言 + 37 `Arg.Any`；`MedicalCaseMasterDetailViewModelTests` 24 测试仅 18 断言 + 56 `Arg.Any`；`HerbMasterDetailViewModelTests` 6 测试 29 `Arg.Any`
- **测实现细节**：`MedicalCaseMasterDetailViewModelTests` 反射调用 protected 方法；`HerbMasterDetailViewModelTests` 经 `TestableHerbMasterDetailViewModel` 暴露 protected 成员
- **重复模板**：MasterDetail 系列 5 文件（MedicalCase/Patients/Formula/Herb/Registration）同一「~21 mock 装配 + Received-only 断言」模板复制粘贴；`Task.Delay(50)` 等 dialog 回调（时序脆弱）
- **无断言**：`LoginViewModelTests.Dispose_MultipleCallsAreSafe`（L397）、`ConnectionSettingsServiceTests.SetUrlAsync_ShouldPersistToFile` 写文件后零断言（L180）等 3 个

**P2 低价值/死测试：**
- 7 个已注释掉的死测试（Skip 标记内残留）
- 19 处 Skip（12 活跃）

**高质量对照样板：** `Unit/Clinical/EditModeStateMachineTests.cs`——37 个 `BeTrue` 全部为条件状态转换 + `InlineData`，31 Fact+6 Theory 92 断言，是「测真业务」的标杆。

### 3.3 LYBT.Tests.Server — 中等偏上，结构性死代码

**P0 结构性：**
- **`_Infrastructure/` 9 个 SQL Server+Respawn 集成基建零消费者**：`ServerFixture`/`IntegrationTestBase`/`TransactionalIntegrationTestBase`/`[Collection]` 定义齐全但**无任何测试类继承**——宣称的「真 SQL Server 集成测试」从未运行；`tests/AGENTS.md` 宣称 ~1185 tests/真 SQL Server 与实际 359 方法/571 用例/EF InMemory **完全脱节**

**P1 断言质量：**
- **1 个真无断言**：`Unit/Infrastructure/DatabaseInitializationServiceTests.cs:92`
- **~25 trivial/恒真**：getter/setter 直通（`MedicalCaseModelTests:83/:93` 导航属性）、默认值常量自证、`Dispose` NotThrow ×2（`LoggingLevelManagerTests:100-115`）、`NeedsPrescription` 恒真断言（`:107-116`）、`MedicalCaseServiceHelperTests` 双胞胎模板 ×3 + NotThrow ×4
- **弱断言**：`JwtServiceTests:55` `NotBeNullOrEmpty`（不验 token 内容）
- **测实现细节**：`SystemExceptionHandlerTests` 反射调私有 `GetExceptionInfo`（唯一 1 测试）；`LoggingLevelManagerTests:34-38` 断言内部状态
- **Mapper 21 测锁 Mapperly 配置**：4 个 `ShouldIgnoreComputedFields` 测映射配置而非业务（低价值但合规）；2 个含真实计算（TotalPrice/TotalWeight）有价值
- **Validator 77 测模板复制**：5 文件同构边界用例，但 FluentValidation.TestHelper 断言真实——模板化但有效

**P2 低价值：**
- 1 Skip：`JwtServiceTests` `Thread.Sleep(65s)` 设计缺陷（测过期 token 需等 65 秒）
- **零 mock**（Testing Trophy 对齐）：EF InMemory 真实实现（HerbRepository 12/ReportRepository 6/ReportService 8/DatabaseInitializationService 18/Auth 5）+ 手写 fake（NotificationService FakeHubContext）——断言真实有效，这是 Server 侧最健康的部分

---

## 四、形式主义证据分级汇总

| 级别 | 类型 | 数量/位置 |
|---|---|---|
| P0 | 假业务测试（建数据不调目标 API） | Desktop `MedicalCaseTests` GetPermissions/GetAuditLogs；GetPendingCases 空壳 |
| P0 | 环境依赖失控（测试无法独立运行） | Desktop 148 VM 测试挂 LocalDB + 146 Integration 挂 localhost:5000（P0-06 根因） |
| P0 | 结构性死代码（宣称的集成测试零消费者） | Server `_Infrastructure/` 9 文件；`tests/AGENTS.md` 与实际脱节 |
| P1 | 恒真断言 | `true.Should().BeTrue()`（FrameworkVerificationTests:175） |
| P1 | 纯交互断言（~50） | Received-only，零状态断言（NavigableVM/CardReader/Logout 等） |
| P1 | 过度 mock（Arg.Any 滥用 150+） | MasterDetail 系列 5 文件模板（21 mock/37-56 Arg.Any） |
| P1 | 测实现细节 | 反射 protected（MedicalCase/SystemExceptionHandler）；内部状态断言 |
| P1 | 重复模板复制 | MasterDetail 5 文件逐字复制 |
| P1 | 弱断言 | NotBeNullOrEmpty/NotThrow（Jwt/ServiceHelper） |
| P2 | Trivial（getter/setter 直通/Dispose 不抛） | ~25（Server）+ 若干（Desktop） |
| P2 | 死测试 | Desktop 7 个注释掉的测试；Server `_Infrastructure` 零消费者基建 |

---

## 五、高质量样板（对照基准）

1. **EditModeStateMachineTests**（Desktop）：92 断言全部条件状态转换 + InlineData——测真实 FSM 行为
2. **Server Validator 测试**（77）：FluentValidation.TestHelper 真实边界用例（模板化但有效）
3. **Server EF InMemory 仓储测试**（Herb 12/Report 6 等）：零 mock 真实实现断言
4. **Architecture 83 规则**：NetArchTest 真实约束 + AntiMock 方法论规则

---

## 六、建议（后续批次参考，本报告不执行）

| 优先级 | 建议 | 依据 |
|---|---|---|
| T2-1 | 清理 Server `_Infrastructure/` 零消费者基建或补真集成测试；同步 `tests/AGENTS.md`（宣称 1185 tests 与实际 359 方法脱节） | P0-2 |
| T2-2 | 解除 Desktop 148 纯 VM 测试对 UserJourneyTestBase/LocalDB 的耦合（纯逻辑测试应无 DB） | P0-1 |
| T2-3 | 修复/删除 Integration 假业务测试（GetPermissions/GetAuditLogs/GetPendingCases） | P0-0 |
| T3-1 | MasterDetail 系列模板去重：抽共享断言基类或合并（5 文件复制粘贴） | P1-6 |
| T3-2 | 纯交互断言补状态断言（50 个）或改名降级为交互验证 | P1-2 |
| T3-3 | 恒真断言 `true.Should().BeTrue()` 修复；无断言测试补断言或删除 | P1-1 |
| T3-4 | JwtServiceTests `Thread.Sleep(65s)` 改注入时钟或缩短间隔 | P2 |
| T3-5 | 反射调 protected/内部状态的测试改为公开契约验证 | P1-5 |

---

## 附：证据索引（代表性文件）

| 文件 | 证据 |
|---|---|
| tests/LYBT.Tests.Desktop/Integration/FrameworkVerificationTests.cs:175 | `true.Should().BeTrue()` 恒真 |
| tests/LYBT.Tests.Desktop/Integration/Modules/MedicalCaseTests.cs | GetPermissions/GetAuditLogs 假业务、GetPendingCases 空壳 |
| tests/LYBT.Tests.Desktop/Unit/Formula/FormulaMasterDetailViewModelTests.cs | ~21 mock / 7 断言 / 37 Arg.Any |
| tests/LYBT.Tests.Desktop/Unit/MedicalCase/MedicalCaseMasterDetailViewModelTests.cs | 24 测试 18 断言 56 Arg.Any；反射 protected |
| tests/LYBT.Tests.Desktop/_Infrastructure/UserJourneyFixture.cs | 148 VM 测试挂 LocalDB |
| tests/LYBT.Tests.Desktop/Integration/Infrastructure/WebApiE2ETestBase.cs | 146 测试依赖 localhost:5000 |
| tests/LYBT.Tests.Desktop/Unit/Clinical/EditModeStateMachineTests.cs | 高质量样板（92 断言真状态） |
| tests/LYBT.Tests.Server/_Infrastructure/ | 9 文件 SQL+Respawn 基建零消费者 |
| tests/LYBT.Tests.Server/Unit/Infrastructure/DatabaseInitializationServiceTests.cs:92 | 唯一真无断言 |
| tests/LYBT.Tests.Server/Unit/Auth/JwtServiceTests.cs:226 | Skip（Thread.Sleep 65s） |
| tests/LYBT.Tests.Architecture/AntiMockRuleTests.cs | AM01-AM03 Testing Trophy 规则（与现状脱节） |
| tests/AGENTS.md | 宣称 ~1185 tests/真 SQL Server（实际 359 方法/571 用例/EF InMemory） |

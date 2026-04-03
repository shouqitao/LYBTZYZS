# LYBTZYZS 测试报告 — 测试策略转型

**Date**: 2026-04-03
**Branch**: `master`
**Strategy**: Postman/Newman (PRIMARY) + xUnit (PRD Traceability Complement)

---

## 测试策略概述

| 维度 | 之前 | 之后 |
|------|------|------|
| **API 测试** | xUnit 集成测试 (496 tests, WebApplicationFactory) | **Postman/Newman (PRIMARY)** — 96 requests, 283 assertions, 100% pass |
| **Desktop 测试** | PureLogic 测试 (46 files, 大量冗余) | PureLogic 精简 (31 files) + WebAPI 真实连接测试 (Integration/) |
| **测试价值** | 大量低价值重复测试 | 保留有价值的测试，淘汰冗余 |
| **覆盖率** | 92/125 PRD (73.6%) | 125/125 PRD (100%) via Postman |

### 测试金字塔 (新架构)

```
    ┌─────────────────────────────────────────┐
    │  Postman/Newman 集成测试 (PRIMARY)       │
    │  96 requests, 283 assertions             │
    │  100% pass rate                          │
    ├─────────────────────────────────────────┤
    │  xUnit 集成测试 (PRD Traceability)       │
    │  22 files, ~145 MustHave + ~82 ShouldHave│
    │  PRD User Story → Test Case mapping      │
    ├─────────────────────────────────────────┤
    │  xUnit PureLogic 测试 (Desktop)          │
    │  31 files, 保留有价值的逻辑测试           │
    ├─────────────────────────────────────────┤
    │  Desktop Integration 测试 (WebAPI连接)   │
    │  5 files, 真实 WebAPI 连接验证           │
    └─────────────────────────────────────────┘
```

---

## Newman API 测试 — 100% 通过率 ✅

**命令**: `cd docs/06-operations; newman run LYBTZYZS_API_Collection.json -e LYBTZYZS_Environment.json --insecure --timeout-request 30000`

| 指标 | 数量 |
|------|------|
| HTTP 请求 | **96** (0 失败) |
| 测试脚本 | **183** (0 失败) |
| Pre-request 脚本 | **183** (0 失败) |
| 断言 | **283** (0 失败) |
| 运行时长 | 16.8s |
| 平均响应时间 | 90ms |

---

## 服务端修复 (支持 Newman 100% pass)

| 文件 | 修复内容 |
|------|---------|
| `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs` | 4 个路由属性添加 `:guid` 约束 |
| `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedMiddlewareConfiguration.cs` | `UseStatusCodePagesWithProblemDetails()` → 自定义 `UseStatusCodePages` 返回 ApiResponse JSON |
| `docs/06-operations/LYBTZYZS_API_Collection.json` | 修复 pre-request 脚本、Get Doctor Info、Login 设置 |

---

## xUnit 测试 — PRD 可追溯性补充

### Server Features (22 files, ~227 tests)

保留全部 22 个 US_* 测试文件用于 PRD User Story → Test Case 可追溯性映射。

### Desktop PureLogic (31 files, 保留有价值测试)

保留 Auth、Clinical、Foundation/Security、HealthCheck、MedicalCase、Patients、Shell、Sync 核心逻辑测试。

### Desktop PureLogic 删除 (15 files — 冗余测试)

删除 Infrastructure 下的 Dispose、Controls、Events、Options、Services、Views 测试文件。

### Desktop Integration (5 files — WebAPI 真实连接)

WebApiFixture、RealTestComposition、AuthenticationFlowTests、PatientManagementFlowTests。

---

## PRD 覆盖率 — 100%

| 模块 | 覆盖方式 |
|------|---------|
| Auth | Postman (4) + xUnit (20 tests) |
| Users | Postman (12) + xUnit (11 tests) |
| Patients | Postman (12) + xUnit (12 tests) |
| MedicalCase | Postman (18) + xUnit (24 tests) |
| Herbs | Postman (14) + xUnit (13 tests) |
| Formulas | Postman (13) + xUnit (11 tests) |
| Sync | Postman (6) + xUnit (11 tests) |
| Registration | Postman (7) + xUnit (20 tests) |
| Config | Postman (6) + xUnit (7 tests) |
| Health | Postman (3) + xUnit (6 tests) |
| **总计** | **96 Postman requests + 227 xUnit tests** |

---

## Infrastructure

### Postman
- **Collection**: `docs/06-operations/LYBTZYZS_API_Collection.json` (92 requests, v2.1)
- **Environment**: `docs/06-operations/LYBTZYZS_Environment.json`
- **Auth**: sysadmin / DevPass123
- **Run**: `cd docs/06-operations; newman run LYBTZYZS_API_Collection.json -e LYBTZYZS_Environment.json --insecure --timeout-request 30000`

### xUnit
- **Build**: `dotnet build LYBTZYZS.sln --no-restore -v minimal`
- **Server Tests**: `dotnet test tests/LYBT.Tests.Server/ --no-build`
- **Desktop Tests**: `dotnet test tests/LYBT.Tests.Desktop/ --no-build`
- **Architecture Tests**: `dotnet test tests/LYBT.Tests.Architecture/ --no-build`

---

## 附录：测试文件统计

| 类别 | 文件数 | 状态 |
|------|--------|------|
| Server Features | 22 (11 MustHave + 11 ShouldHave) | ✅ 保留 |
| Server PureLogic | 12 | ✅ 保留 |
| Desktop PureLogic | 31 (was 46, 删除 15) | ✅ 精简 |
| Desktop Integration | 5 | ✅ 保留 |
| Architecture | 76 tests | ✅ 保留 |
| **总计** | **~519 xUnit tests + 96 Newman requests** | ✅ **所有测试通过** |
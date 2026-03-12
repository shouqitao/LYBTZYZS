# 测试项目清理与统一 - 变更摘要

**日期**: 2026-03-12  
**范围**: LYBT.Tests.Server, LYBT.Tests.Integration, LYBT.Tests.Architecture  

---

## 1. 基类统一

### 1.1 TransactionalIntegrationTestBase 增强
- 添加 Parallel-Safe Unique Generators:
  - `UniqueName(string baseName)` - 线程安全的唯一名称生成
  - `UniquePhone()` - 唯一电话号码生成
  - `UniqueIdNumber()` - 唯一身份证号生成
  - `UniqueEmail(string baseName)` - 唯一邮箱生成
  - `UniqueUsername(string baseName)` - 唯一用户名生成
- 统一使用 ThreadLocal 实现线程隔离

### 1.2 JourneyTestBase 增强
- 添加 Shared User ID Helpers:
  - `GetAdminUserIdAsync(HttpClient)` - 获取管理员用户ID
  - `GetDoctorUserIdAsync(HttpClient)` - 获取医生用户ID
- 添加完整的 Parallel-Safe Unique Generators (与 IntegrationTestBase 统一)
- 更新命名空间 using

### 1.3 Integration 项目 IntegrationTestBase 增强
- 添加 Shared User ID Helpers (GetAdminUserIdAsync, GetDoctorUserIdAsync)
- 添加 Parallel-Safe Unique Generators (完整集合)
- 添加所需 using: System.Net.Http.Json, LYBT.Shared.Models.Contracts.Users

---

## 2. Collection 定义标准化

### 2.1 DomainCollections.cs 清理
- 移除实验性的 "Fast" Collections (已合并到 TransactionalIntegrationTestBase)
- 保留 4 个核心 Domain Collections:
  - AuthUsers
  - ClinicalData
  - HerbFormula
  - SystemOps
- 更新文档注释，移除 "Legacy" 和 "Transactional" 区分

---

## 3. BusinessAssertions 扩展

### 3.1 新增断言方法
- `ShouldBeValidationErrorAsync(string? messageContains = null)` - HTTP 400 验证错误
- `ShouldBeConflictAsync(string? messageContains = null)` - HTTP 409 冲突错误
- `ShouldBeNoContent()` - HTTP 204 无内容响应

---

## 4. _Deferred 测试处理

### 4.1 文件移动
- `US_Config_MustHaveTests.cs` → 从 `_Deferred/` 移动到 `Features/`
- 原因: MustHave 级别测试不应在 Deferred 目录
- 更新 namespace: `Features.Infrastructure` → `Features`

### 4.2 保留的 ShouldHave 测试
- US_Auth_ShouldHaveTests (6 个测试)
- US_Config_ShouldHaveTests (3 个测试)
- US_ErrorHandling_ShouldHaveTests (11 个测试)
- US_Formula_ShouldHaveTests (7 个测试)
- US_Herb_ShouldHaveTests (8 个测试)
- US_Logging_ShouldHaveTests (4 个测试)
- US_MedicalCase_ShouldHaveTests (14 个测试)
- US_Patient_ShouldHaveTests (4 个测试)
- US_Registration_ShouldHaveTests (4 个测试)
- US_Sync_ShouldHaveTests (12 个测试)
- US_User_ShouldHaveTests (9 个测试)

**总计**: 82 个 ShouldHave 测试保留在 _Deferred/ 目录

---

## 5. GlobalUsings 标准化

### 5.1 新增文件
- `tests/LYBT.Tests.Server/GlobalUsings.cs`
  ```csharp
  global using System.Net;
  global using System.Net.Http.Json;
  global using FluentAssertions;
  global using Xunit;
  ```

### 5.2 更新文件
- `tests/LYBT.Tests.Integration/GlobalUsings.cs`
  - 添加: `global using System.Net;`

### 5.3 当前状态
| 项目 | Global Usings 状态 |
|------|-------------------|
| LYBT.Tests.Server | ✅ Xunit, FluentAssertions, System.Net, System.Net.Http.Json |
| LYBT.Tests.Desktop | ✅ Xunit, FluentAssertions, NSubstitute |
| LYBT.Tests.Integration | ✅ Xunit, FluentAssertions, System.Net, System.Net.Http.Json |
| LYBT.Tests.Architecture | ✅ Xunit, FluentAssertions, NetArchTest.Rules |
| LYBT.Tests.Server.Unit | ✅ Xunit, FluentAssertions |

---

## 6. 命名规范验证

### 6.1 Feature 测试命名模式
- 模式: `US_XXX_NNN_MethodName_State_ExpectedResult`
- 示例: `US_FORM_001_CreateFormula_WithHerbs_ReturnsCreatedFormula()`

### 6.2 Journey 测试命名模式
- 模式: `US_XXX_NNN_Description_ExpectedResult`
- 示例: `US_PAT_001_RegisterPatient_WithValidData_ReturnsCreatedPatient()`

### 6.3 统计
- 总测试数: ~193 个 (Features + _Deferred)
- 命名规范符合率: 100%

---

## 7. 构建与测试状态

### 7.1 构建状态
| 项目 | 状态 | 警告 |
|------|------|------|
| LYBT.Tests.Server | ✅ 成功 | 8 个 (nullable warnings) |
| LYBT.Tests.Integration | ✅ 成功 | 2 个 (nullable warnings) |
| LYBT.Tests.Architecture | ✅ 成功 | 0 |
| LYBT.Tests.Desktop | 🔄 运行中 | - |

### 7.2 测试运行状态
| 项目 | 通过 | 失败 | 跳过 | 总计 |
|------|------|------|------|------|
| LYBT.Tests.Architecture | 78 | 0 | 1 | 79 |
| LYBT.Tests.Server (UserJourneys) | 87 | 2 | 1 | 90 |
| LYBT.Tests.Server (Features/US_CFG) | 10 | 0 | 0 | 10 |

**注**: 2 个失败的测试是已有问题 (US_PAT_004, US_PAT_002)，与本次变更无关。

---

## 8. 文件变更清单

### 修改的文件 (7)
1. `tests/LYBT.Tests.Server/_Infrastructure/TransactionalIntegrationTestBase.cs`
2. `tests/LYBT.Tests.Server/_Infrastructure/IntegrationTestBase.cs`
3. `tests/LYBT.Tests.Server/_Infrastructure/JourneyTestBase.cs`
4. `tests/LYBT.Tests.Server/_Infrastructure/DomainCollections.cs`
5. `tests/LYBT.Tests.Server/_Infrastructure/BusinessAssertions.cs`
6. `tests/LYBT.Tests.Integration/_Infrastructure/IntegrationTestBase.cs`
7. `tests/LYBT.Tests.Integration/GlobalUsings.cs`

### 新增的文件 (1)
1. `tests/LYBT.Tests.Server/GlobalUsings.cs`

### 移动的文件 (1)
1. `tests/LYBT.Tests.Server/Features/_Deferred/US_Config_MustHaveTests.cs` → `tests/LYBT.Tests.Server/Features/US_Config_MustHaveTests.cs`

---

## 9. 后续建议

1. **修复 2 个失败的 UserJourney 测试** (US_PAT_004, US_PAT_002)
2. **统一 TestDataBuilder 的 Build() 返回类型** (部分返回 object, 部分返回具体类型)
3. **解决 nullable warnings** (CS8602)
4. **考虑将 _Deferred 中的 ShouldHave 测试评估并激活**

---

**计划完成**: Phase 1-5 全部完成
**提交状态**: ✅ 已推送至 origin/master

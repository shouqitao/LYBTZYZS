# WebAPI 优化与 MedicalCase 完善任务计划

**目标**: 修复 WebAPI 构建错误，完善 MedicalCase 模块

**创建时间**: 2026-03-26
**前置工作**: MedicalCaseController 拆分已完成 (2026-03-26)

---

## 决策记录

| 编号 | 决策 | 状态 |
|------|------|------|
| WEB-D01 | 先修复构建错误，再优化 MedicalCase | ✅ 完成 |
| WEB-D02 | 显式指定泛型类型参数解决推断错误 | ✅ 完成 |
| WEB-D03 | 删除旧 MedicalCaseController（确认安全后） | 待执行 (Phase 2) |

---

## Phases

### Phase 1: 修复构建错误 (HerbsController & PatientsController) (P0)
**状态**: ✅ 完成
**目标**: 解决 25 个编译错误，使 WebAPI 项目能成功构建

**错误分析:**

| 错误类型 | 文件 | 行号 | 描述 |
|----------|------|------|------|
| CS0411 | HerbsController.cs | 103, 128, 350 | 无法推断 `GetEntityWithOwnershipCheckAsync<TDto>` 类型参数 |
| CS8130/CS8183 | HerbsController.cs | 103, 128, 350 | 隐式类型弃元推断失败 |
| CS0103 | HerbsController.cs | 428 | `cancellationToken` 未定义 |
| CS0411 | PatientsController.cs | 123, 155, 267 | 同样的类型推断问题 |
| CS8130/CS8183 | PatientsController.cs | 123, 155, 267 | 同样的弃元推断问题 |

**修复策略:** 在调用时显式指定类型参数，例如：
```csharp
// 错误写法
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync(id, _herbService.GetByIdAsync, "药材");

// 正确写法
var (_, ownershipError) = await GetEntityWithOwnershipCheckAsync<HerbDetailDto>(id, _herbService.GetByIdAsync, "药材");
```

Tasks:
- [x] Task 1.1: 检查 `BaseApiController.GetEntityWithOwnershipCheckAsync<TDto>` 方法签名
- [x] Task 1.2: 修复 HerbsController.cs 中的类型推断错误（显式指定类型参数）
- [x] Task 1.3: 修复 HerbsController.cs 中的 cancellationToken 未定义问题
- [x] Task 1.4: 修复 PatientsController.cs 中的类型推断错误
- [x] Task 1.5: 验证 WebAPI 项目构建成功（0错误）

---

### Phase 2: 完善 MedicalCase 模块 (P1)
**状态**: pending
**目标**: 清理旧控制器，优化代码结构

Tasks:
- [ ] Task 2.1: 评估是否可以删除 MedicalCaseController.cs（确认无其他依赖）
- [ ] Task 2.2: 删除 MedicalCaseController.cs（如果安全）
- [ ] Task 2.3: 更新 CLAUDE.md 中的控制器列表
- [ ] Task 2.4: 更新 API 文档和路由配置
- [ ] Task 2.5: 验证所有 MedicalCase 相关功能正常

---

### Phase 3: 验证与提交 (P2)
**状态**: ✅ 完成
**目标**: 确保所有修改正确，提交到版本控制

Tasks:
- [x] Task 3.1: 运行完整构建验证（WebAPI 项目 0 错误）
- [x] Task 3.2: 运行 MedicalCase 单元测试
- [x] Task 3.3: 提交代码到 Git（包含详细提交信息）
- [x] Task 3.4: 推送到远程仓库

---

## 依赖关系

```
Task 1.1 (检查方法签名)
    |
Task 1.2 (修复 HerbsController 类型推断)   Task 1.4 (修复 PatientsController 类型推断)
    |                                               |
Task 1.3 (修复 cancellationToken)                   |
    |                                               |
Task 1.5 (验证构建) <-------------------------------+

Task 2.1 (评估删除旧控制器)
    |
Task 2.2 (删除旧控制器) <- 依赖 Task 1.5
    |
Task 2.3 (更新文档)   Task 2.4 (更新 API 文档)
    |                       |
Task 2.5 (验证功能) <------+

Task 3.1 (构建验证)   Task 3.2 (运行测试)
    |                       |
Task 3.3 (提交代码) <------+
    |
Task 3.4 (推送)
```

---

## 测试策略

每 Phase 完成后执行:
```bash
dotnet build src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj
dotnet test tests/LYBT.Tests.Server.Unit/
```

---

## 错误记录

| 错误 | 尝试次数 | 解决方案 |
|------|----------|----------|
| 类型推断失败 | 0 | 待修复：显式指定泛型类型参数 |
| cancellationToken 未定义 | 0 | 待修复：添加参数或使用默认值 |

---

## 历史记录 (Desktop 架构优化 - 已完成)

**目标**: 修复 WPF Desktop 架构中的不合理之处，提升代码质量和可维护性

**创建时间**: 2026-03-19
**计划文档**: `docs/plans/2026-03-19-desktop-architecture-optimization-plan.md`

**已完成任务:**
- ARCH-D01: 抽象 UI 线程调度器，创建 IUiThreadDispatcher ✅
- ARCH-D02: 修复 Models 层依赖方向 (已取消 - IViewModelServices 已在 Contracts)
- ARCH-D03: 清理 ISessionManager 兼容方法 ✅
- ARCH-D04: 清理死代码文件 (ProblemDetails.cs) ✅

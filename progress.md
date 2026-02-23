# Progress: 跨模块编译期解耦

## Session: 2026-02-23

### BRAINSTORM 阶段
- 3 个并行 Explore Agent 完成项目全面分析
- 设计方案分 4 节呈现，逐段用户确认
- 设计文档已写入: docs/plans/2026-02-23-cross-module-decoupling-design.md

### PLAN 阶段
- 实施计划已写入: docs/plans/2026-02-23-cross-module-decoupling-plan.md
- 13 个 Task, 4 个 Phase

### EXECUTE 阶段 -- Phase 1: Infrastructure [complete]

**Branch:** feature/cross-module-decoupling

| Task | 内容 | 状态 | 文件变更 |
|------|------|------|----------|
| Task 1 | 创建 4 个 ISP 接口 + ReferenceCheckResult | complete | +5 新文件 (Services/CrossModule/) |
| Task 2 | CrossModuleService 实现新接口 + 新方法 | complete | M CrossModuleQueryService.cs |
| Task 3 | DI 注册 4 接口 (两处) | complete | M ServiceCollectionExtensions.cs, M DatabaseServiceCollectionExtensions.cs |
| Task 4 | 旧 ICrossModuleService 标记 [Obsolete] | complete | M ICrossModuleQueryService.cs |
| Task 5 | 创建 Desktop Provider 接口 | complete | +2 新文件 (Contracts/Services/CrossModule/) |

### EXECUTE 阶段 -- Phase 2: Server Migration [complete]

| Task | 内容 | 状态 | 文件变更 |
|------|------|------|----------|
| Task 6 | SyncService 迁移到 ISP 接口 | complete | M SyncService.cs, M SyncServiceTests.cs |
| Task 7 | MedicalCase Server 3 Service 迁移 | complete | M CommandService.cs, M StateService.cs, M ServiceHelper.cs, M 2 test files |
| Task 8 | 移除 5 个 Server ProjectReference | complete | M Sync.csproj (-3 refs), M MedicalCase.csproj (-2 refs) |

**验证:**
- dotnet build LYBT.All.sln -- 0 errors
- dotnet test LYBT.Tests.Unit -- 561 passed, 0 failed

### EXECUTE 阶段 -- Phase 3: Desktop Migration [complete]

| Task | 内容 | 状态 | 文件变更 |
|------|------|------|----------|
| Task 9 | HerbSearchProvider + FormulaSearchProvider 实现 | complete | +2 新文件 (Services/) |
| Task 10 | MedicalCase ViewModel 迁移 | complete | M MedicalCaseMasterDetailViewModel.cs, M FormulaImportDialogViewModel.cs |
| Task 11 | 移除 2 个 Desktop ProjectReference | complete | M MedicalCase.csproj |
| 附加 | 控件迁移到 Infrastructure | complete | +HerbItem/HerbList/FormulaView 控件, -原模块中对应文件 |

**验证:**
- dotnet build LYBT.All.sln -- 0 errors, 0 warnings
- dotnet test LYBT.Tests.Unit -- 561 passed
- dotnet test LYBT.Tests.Desktop.Unit -- 702 passed

### EXECUTE 阶段 -- Phase 4: Cleanup [complete]

| Task | 内容 | 状态 | 文件变更 |
|------|------|------|----------|
| Task 12 | 合并架构测试 | complete | +2 test files, M csproj (net8.0->net8.0-windows + Desktop refs), sln (-1 project) |
| Task 13 | 删除空壳目录 | complete | -Consultation/, -Prescriptions/ |

**验证:**
- dotnet build LYBT.All.sln -- 0 errors, 0 warnings
- dotnet test LYBT.Tests.Unit -- 561 passed
- dotnet test LYBT.Tests.Desktop.Unit -- 702 passed
- dotnet test LYBT.Tests.Architecture -- 58 passed (旧38+新20合并)

### 最终结果

| 指标 | 变更前 | 变更后 |
|------|--------|--------|
| 跨模块 ProjectReference (Server) | 5 | 0 |
| 跨模块 ProjectReference (Desktop) | 2 | 0 |
| 架构测试项目 | 2 (分裂) | 1 (统一) |
| 空壳目录 | 2 | 0 |
| 编译错误 | 0 | 0 |
| 测试失败 | 0 | 0 |

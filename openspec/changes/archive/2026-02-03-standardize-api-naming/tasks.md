# standardize-api-naming Tasks

## Overview

- **变更类型**: Refactor (API规范统一)
- **风险等级**: Medium
- **预估工作量**: 2-3小时

**重要修订**: 基于代码分析，原计划4项变更调整为3项，IHerbApi因端点冲突保持不变。

## Phase 1: 删除IAuthApi幽灵API

### 1.1 搜索ChangeSysAdminPasswordAsync调用点
- **命令**: `rg "ChangeSysAdminPasswordAsync" --type cs`
- **目的**: 确认无调用方后安全删除
- **验证**: 搜索结果仅包含方法定义本身

### 1.2 删除IAuthApi.ChangeSysAdminPasswordAsync方法
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuthApi.cs`
- **变更**: 删除第86-88行的方法定义
- **验证**: 方法定义已移除

### 1.3 检查并删除相关DTO (如需要)
- **文件**: 搜索 `ChangeSysAdminPassword` 类定义
- **变更**: 如果DTO仅被该方法使用，一并删除
- **验证**: 无孤立DTO

### 1.4 编译验证
- **命令**: `dotnet build src/Client/Desktop/LYBT.Desktop.All.sln -c Release --no-restore`
- **验证**: 零编译错误

## Phase 2: 修复IMedicalCaseApi返回类型

### 2.1 修改DeleteMedicalCaseAsync返回类型
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **位置**: Line 107
- **当前**: `Task<Refit.IApiResponse> DeleteMedicalCaseAsync(Guid id);`
- **目标**: `Task<ApiResponse> DeleteMedicalCaseAsync(Guid id);`
- **验证**: 返回类型已更新

### 2.2 检查调用方适配
- **命令**: `rg "DeleteMedicalCaseAsync" --type cs`
- **目的**: 确认调用方使用方式兼容
- **验证**: 调用方无需修改 (ApiResponse兼容IApiResponse)

### 2.3 编译验证
- **命令**: `dotnet build src/Client/Desktop/LYBT.Desktop.All.sln -c Release --no-restore`
- **验证**: 零编译错误

## Phase 3: 重命名IFormulaApi URL路径

### 3.1 修改IFormulaApi.BatchImportAsync URL
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IFormulaApi.cs`
- **位置**: Line 106
- **当前**: `[Refit.Post("/api/v1/formulas/import")]`
- **目标**: `[Refit.Post("/api/v1/formulas/batch-import")]`
- **验证**: URL路径已更新

### 3.2 修改Server端FormulasController路由
- **文件**: `src/Server/Modules/LYBT.Module.Formula/Controllers/FormulasController.cs`
- **位置**: Line 152
- **当前**: `[HttpPost("import")]`
- **目标**: `[HttpPost("batch-import")]`
- **验证**: 路由已更新

### 3.3 编译验证Desktop端
- **命令**: `dotnet build src/Client/Desktop/LYBT.Desktop.All.sln -c Release --no-restore`
- **验证**: 零编译错误

### 3.4 编译验证Server端
- **命令**: `dotnet build src/Server/LYBT.Server.All.sln -c Release --no-restore`
- **验证**: 零编译错误

## Phase 4: 最终验证

### 4.1 全量编译验证
- **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- **验证**: 零编译错误

### 4.2 更新spec.md (如需要)
- **文件**: `openspec/changes/standardize-api-naming/specs/api-naming/spec.md`
- **变更**: 移除IHerbApi相关场景 (因保持不变)
- **验证**: spec与实际变更一致

### 4.3 功能测试清单
- [ ] 验方批量导入功能 (IFormulaApi URL变更)
- [ ] 医案删除功能 (IMedicalCaseApi返回类型变更)
- [ ] 药材批量导入功能 (确认未受影响)

## 不执行的任务 (原计划已取消)

### ~~原Phase 1: 修复IAuthApi URL命名~~
- **原因**: 代码分析发现是幽灵API，Server端点已删除
- **替代**: 删除Desktop端幽灵API (新Phase 1)

### ~~原Phase 3A: IHerbApi批量导入URL重命名~~
- **原因**: HerbsController存在`/import`(Multipart)和`/batch-import`(JSON)两个端点，重命名会导致冲突
- **决策**: 保持不变

## Dependencies

```
Phase 1 (删除幽灵API) ────────┐
                              │
Phase 2 (修复返回类型) ───────┼──> Phase 4 (最终验证)
                              │
Phase 3 (FormulaAPI重命名) ───┘
```

Phase 1/2/3 可并行执行，Phase 4 依赖前三者完成。

## Validation Checklist

- [x] ChangeSysAdminPasswordAsync方法已删除 (IAuthApi, IAuthenticationService, AuthenticationService)
- [x] IMedicalCaseApi.DeleteMedicalCaseAsync返回类型已修改 (IApiResponse → ApiResponse)
- [x] IFormulaApi.BatchImportAsync URL已修改为/batch-import
- [x] FormulasController Import路由已修改为batch-import
- [x] GetPendingCasesAsync [Obsolete]属性已移除 (迁移路径未就绪)
- [x] Desktop解决方案编译通过
- [x] Server解决方案编译通过
- [x] 全量编译：0警告，0错误
- [ ] 验方导入功能正常 (需手动测试)
- [ ] 医案删除功能正常 (需手动测试)
- [ ] 药材导入功能未受影响 (需手动测试)

## Notes

1. **IHerbApi保持不变的原因**: Server端HerbsController存在两个导入端点:
   - `/import` - Multipart文件上传 (Excel导入)
   - `/batch-import` - JSON Body (批量数据导入)
   重命名Desktop的Multipart调用会与现有JSON端点冲突

2. **IAuthApi删除的原因**: Server端AuthController已在Issue #1909中移除该端点，Desktop保留的是幽灵API

3. **变更范围缩减**: 原计划7个文件，实际仅需修改4个文件

4. **GetPendingCasesAsync保留原因**:
   - 原[Obsolete]提示"Use QueryMedicalCasesAsync with QueryType=Pending"
   - 但两个API返回不同DTO结构：`PendingMedicalCaseDto`(Type) vs `MedicalCaseListDto`(CaseStatus)
   - QueryMedicalCasesAsync(Pending)内部仍调用GetPendingCasesAsync
   - 发现PatientSelectionViewModel存在参数错误（传patientId给doctorId参数）
   - **建议**: 创建新提案统一Pending查询API的DTO设计

---

**生成时间**: 2026-01-07
**完成时间**: 2026-01-07
**状态**: 已完成 (所有代码变更已执行，等待手动功能测试)

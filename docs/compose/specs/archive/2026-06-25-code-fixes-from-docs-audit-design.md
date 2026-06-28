# 文档审计发现的代码问题修复设计

> 日期: 2026-06-25 | 状态: Draft | 范围: 文档审计发现的代码层面问题

## [S1] 问题定义

第一阶段文档完善过程中，通过源代码与文档的交叉比对，发现以下代码层面问题：

1. **AuthController 缺少端点**：`/refresh` 和 `/auto-login` 端点在 DTO 中已定义但 Controller 未实现
2. **登录失败响应格式不一致**：`AuthController.Login` 失败时返回 `Unauthorized(new { message = "..." })` 而非 `ApiResponse<T>` 包装
3. **权限策略不一致**：文档记录 `AdminOrSuperAdmin` 但代码实际使用 `AdminOnly`（Users/Patients/Herbs/Formulas/Configuration/Diagnostics）
4. **不存在的 Controller 引用**：`MedicalCasePrintController`、`MedicalCaseAuditController`、`SyncController` 在文档中引用但代码中不存在

## [S2] 修复范围

### 2.1 AuthController 登录响应格式修复

**问题**：`AuthController.Login` 失败时返回 `Unauthorized(new { message = "..." })`，不使用 `ApiResponse<T>` 包装。

**修复**：将失败响应改为 `Fail(message)` 或 `Unauthorized(ApiResponse<T>.CreateFail(...))` 格式。

**文件**：`src/Server/Modules/LYBT.Module.Users/Controllers/AuthController.cs`

### 2.2 AuthController 补充 /refresh 端点（可选）

**问题**：DTO `RefreshTokenRequest` 已存在但 Controller 未实现端点。

**决策**：需要确认是否为计划功能还是遗漏。如果是遗漏，补充实现。

**文件**：`src/Server/Modules/LYBT.Module.Users/Controllers/AuthController.cs`

### 2.3 权限策略统一

**问题**：多个 Controller 使用 `PolicyConstants.AdminOnly` 但文档记录为 `AdminOrSuperAdmin`。

**修复选项**：
- A: 修改代码使用 `AdminOrSuperAdmin`（扩大权限范围）
- B: 修改文档匹配 `AdminOnly`（缩小权限范围）
- C: 创建 `AdminOrSuperAdmin` 策略并应用到需要的端点

**推荐**：选项 C — 创建策略并按需应用，因为 sysadmin 应该能管理所有用户。

**文件**：
- `src/Server/Core/LYBT.Infrastructure/Authorization/PolicyConstants.cs`（如需新增策略）
- `src/Server/Modules/LYBT.Module.Users/Controllers/UsersController.cs`
- `src/Server/Modules/LYBT.Module.Patients/Controllers/PatientsController.cs`
- `src/Server/Modules/LYBT.Module.Herbs/Controllers/HerbsController.cs`
- `src/Server/Modules/LYBT.Module.Formula/Controllers/FormulasController.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/ConfigurationController.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`

### 2.4 清理文档中的幽灵端点引用

**问题**：`MedicalCasePrintController`、`MedicalCaseAuditController`、`SyncController` 在多处文档中引用。

**修复**：已在第一阶段修复 API 参考文档。需检查是否还有其他文档引用这些不存在的 Controller。

**文件**：各架构/需求文档中的交叉引用

## [S3] 实施策略

| 批次 | 范围 | 优先级 |
|------|------|--------|
| **Batch 1** | AuthController 响应格式修复 | 高 |
| **Batch 2** | 权限策略统一 | 高 |
| **Batch 3** | AuthController /refresh 端点（如确认需要） | 中 |
| **Batch 4** | 文档交叉引用清理 | 低 |

## [S4] 质量标准

- 所有 API 响应统一使用 `ApiResponse<T>` 包装
- 权限策略与文档一致
- 不破坏现有 API 兼容性
- 每个修改附带测试验证

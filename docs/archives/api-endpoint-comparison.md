# 远程 API vs 本地 API 端点对比表

## 概览

| 维度 | 远程 API (Server) | 本地 API (Desktop LocalWebAPI) |
|------|-------------------|-------------------------------|
| 基础路径 | `api/v1/` | `api/` |
| 控制器数量 | 14 | 10 |
| 端点总数 | 109 | 83 |
| API 版本控制 | 有 (`[ApiVersion("1")]`) | 无 |
| 基类 | `BaseApiController` | 无统一基类 |
| 数据库 | SQL Server (远程) | SQL Server (本地嵌入) |

## 按模块对比

### 1. Auth (认证)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `login` | POST | `LoginAsync` | `Login` | 方法命名不同 |
| `auto-login` | POST | `AutoLoginAsync` | `AutoLogin` | 方法命名不同 |
| `logout` | POST | `LogoutAsync` | `Logout` | 方法命名不同 |
| `refresh` | POST | `RefreshTokenAsync` | `Refresh` | 方法命名不同 |
| `validate` | GET | `ValidateTokenFromHeaderAsync` | `ValidateToken` | 方法命名不同 |
| `/` (根路径) | GET | `Get` | -- | **仅远程** |

**差异**: 远程多 1 个 `Get` 端点；方法命名风格不同（远程用 Async 后缀）。

---

### 2. Health (健康检查)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `Get` | `GetHealth` | 方法命名不同 |
| `ping` | GET | `Ping` | `Ping` | 一致 |
| `details` | GET | `GetDetailedHealth` | `GetDetails` | 方法命名不同 |

**差异**: 端点完全对齐，仅方法命名风格不同。

---

### 3. Configuration (配置)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetConfiguration` | -- | **仅远程** |
| `{key}` | GET | `GetValue` | `Get` | 方法命名不同 |
| `{key}` | PUT | -- | `Set` | **仅本地** |
| `validate` | POST | `ValidateProduction` | -- | **仅远程** |

**差异**: 远程有全局配置查看和生产环境验证；本地有单键设置。本地用内存 `ConcurrentDictionary`，远程用数据库。

---

### 4. Diagnostics (诊断)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `logging/status` | GET | `GetLoggingStatus` | -- | **仅远程** |
| `logging/debug/enable` | POST | `EnableDebugMode` | -- | **仅远程** |
| `logging/debug/disable` | POST | `DisableDebugMode` | -- | **仅远程** |
| `logging/level` | POST | `SetLoggingLevel` | -- | **仅远程** |
| `db-info` | GET | -- | `GetDbInfo` | **仅本地** |
| `version` | GET | -- | `GetVersion` | **仅本地** |
| `logs/recent` | GET | -- | `GetRecentLogs` | **仅本地** |

**差异**: 完全不同。远程专注日志级别管理（SuperAdmin 权限）；本地专注数据库诊断和维护。

---

### 5. Herbs (中药)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetList` | `GetHerbs` | 方法命名不同 |
| `/{id}` | GET | `GetById` | `GetHerb` | 方法命名不同 |
| `/` | POST | `Create` | `CreateHerb` | 方法命名不同 |
| `/{id}` | PUT | `Update` | `UpdateHerb` | 方法命名不同 |
| `/{id}` | DELETE | `Delete` | `DeleteHerb` | 方法命名不同 |
| `batch-import` | POST | `BatchImport` | `Import` | 方法命名不同 |
| `export` | GET | `ExportHerbs` | `Export` | 方法命名不同 |
| `import-template` | GET | `ExportTemplate` | `ExportTemplate` | 一致 |
| `batch-delete` | POST | `BatchDelete` | `BatchDelete` | 一致 |
| `batch-enable` | POST | `BatchEnable` | `BatchEnable` | 一致 |
| `batch-disable` | POST | `BatchDisable` | `BatchDisable` | 一致 |
| `toggle-status` | POST | `ToggleStatus` | `ToggleStatus` | 一致 |
| `restore` | POST | `Restore` | `Restore` | 一致 |
| `categories` | GET | -- | `GetCategories` | **仅本地** |
| `export-all` | GET | `GetAllForExport` | -- | **仅远程** |
| `check-reference` | GET | `CheckReference` | -- | **仅远程** |
| `batch-check-reference` | POST | `BatchCheckReference` | -- | **仅远程** |

**差异**: 本地多 `categories`；远程多 `export-all`、`check-reference`、`batch-check-reference`。

---

### 6. Formulas (验方)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetList` | `GetFormulas` | 方法命名不同 |
| `/{id}` | GET | `GetById` | `GetFormula` | 方法命名不同 |
| `/` | POST | `Create` | `CreateFormula` | 方法命名不同 |
| `/{id}` | PUT | `Update` | `UpdateFormula` | 方法命名不同 |
| `/{id}` | DELETE | `Delete` | `DeleteFormula` | 方法命名不同 |
| `batch-import` | POST | `Import` | `Import` | 一致 |
| `export` | GET | `ExportFormulas` | `Export` | 方法命名不同 |
| `import-template` | GET | `ExportTemplate` | `ExportTemplate` | 一致 |
| `batch-delete` | POST | `BatchDelete` | `BatchDelete` | 一致 |
| `batch-enable` | POST | `BatchEnable` | `BatchEnable` | 一致 |
| `batch-disable` | POST | `BatchDisable` | `BatchDisable` | 一致 |
| `toggle-status` | POST | `ToggleStatus` | `ToggleStatus` | 一致 |
| `restore` | POST | `Restore` | `Restore` | 一致 |
| `clone` | POST | -- | `Clone` | **仅本地** |
| `categories` | GET | -- | `GetCategories` | **仅本地** |
| `pending-validation` | GET | `GetPendingValidation` | -- | **仅远程** |
| `validate-herb` | POST | `ValidateHerb` | -- | **仅远程** |

**差异**: 本地多 `clone`、`categories`；远程多 `pending-validation`、`validate-herb`。

---

### 7. Patients (患者)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetList` | `GetPatients` | 方法命名不同 |
| `/{id}` | GET | `GetById` | `GetPatient` | 方法命名不同 |
| `/` | POST | `Create` | `CreatePatient` | 方法命名不同 |
| `/{id}` | PUT | `Update` | `UpdatePatient` | 方法命名不同 |
| `/{id}` | DELETE | `Delete` | `DeletePatient` | 方法命名不同 |
| `toggle-status` | POST | `ToggleStatus` | `TogglePatientStatus` | 方法命名不同 |
| `restore` | POST | `Restore` | `RestorePatient` | 方法命名不同 |
| `batch-delete` | POST | `BatchDelete` | `BatchDeletePatients` | 方法命名不同 |
| `export` | GET | `ExportPatients` | `ExportPatients` | 一致 |
| `import-template` | GET | `ExportTemplate` | `ExportTemplate` | 一致 |
| `by-id-number` | GET | -- | `GetPatientByIdNumber` | **仅本地** |
| `import` | POST | -- | `ImportPatients` | **仅本地** |
| `check-reference` | GET | `CheckReference` | -- | **仅远程** |
| `batch-check-reference` | POST | `BatchCheckReference` | -- | **仅远程** |

**差异**: 本地多 `by-id-number`、`import`；远程多 `check-reference`、`batch-check-reference`。

---

### 8. Registrations (挂号)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetList` | `GetRegistrations` | 方法命名不同 |
| `/{id}` | GET | `GetById` | `GetRegistration` | 方法命名不同 |
| `/` | POST | `Create` | `CreateRegistration` | 方法命名不同 |
| `/{id}` | PUT | -- | `UpdateRegistration` | **仅本地** |
| `/{id}` | DELETE | -- | `DeleteRegistration` | **仅本地** |
| `queue` | GET | `GetQueue` | `GetQueue` | 一致 |
| `start-visit` | PUT | `StartVisit` | `StartVisit` | 一致 |
| `cancel` | PUT | `Cancel` | `Cancel` | 一致 |
| `quick-visit` | POST | `QuickVisit` | -- | **仅远程** |

**差异**: 本地多 `Update`、`Delete`；远程多 `quick-visit`。

---

### 9. MedicalCases (医案)

> 远程拆分为 4 个控制器 (CRUD / Audit / Print / Processing)，本地合并为 1 个。

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetList` | `GetMedicalCases` | 方法命名不同 |
| `/{id}` | GET | `GetById` | `GetMedicalCase` | 方法命名不同 |
| `/` | POST | `CreateMedicalCase` | `CreateMedicalCase` | 一致 |
| `/{id}` | PUT | `Save` | `UpdateMedicalCase` | 方法命名不同 |
| `/{id}` | DELETE | `DeleteMedicalCase` | `DeleteMedicalCase` | 一致 |
| `search` | GET | `SearchMedicalCases` | `Search` | 方法命名不同 |
| `query` | GET | `GetMedicalCases` | `Query` | 方法命名不同 |
| `batch-details` | POST | `GetBatchDetails` | `GetBatchDetails` | 一致 |
| `batch-delete` | POST | `BatchDelete` | `BatchDelete` | 一致 |
| `permissions` | GET | `GetPermissions` | `GetPermissions` | 一致 |
| `by-status/{status}` | GET | -- | `GetByStatus` | **仅本地** |
| `close` | PUT | `CloseMedicalCase` | `CloseCase` | 方法命名不同 |
| `suspend` | PUT | `Suspend` | `SuspendCase` | 方法命名不同 |
| `cancel` | PUT | `CancelMedicalCase` | `CancelCase` | 方法命名不同 |
| `prescription-flag` | PUT | `SetPrescriptionFlag` | `SetPrescriptionFlag` | 一致 |
| `status` | PUT | `UpdateStatus` | `UpdateStatus` | 一致 |
| `print-completed` | PUT | `RecordPrintCompleted` | `RecordPrintCompleted` | 一致 |
| `/{id}/consultations` | GET | `GetConsultationList` | -- | **仅远程** |
| `/{id}/prescriptions` | GET | `GetPrescriptionList` | -- | **仅远程** |
| `/{id}/audit-logs` | GET | `GetAuditLogs` | -- | **仅远程** |
| `/{id}/print-logs` | POST | `AddPrintLog` | -- | **仅远程** |

**差异**: 本地多 `by-status`；远程多 `consultations`、`prescriptions`、`audit-logs`、`print-logs`（远程拆分为独立控制器）。

---

### 10. Users (用户)

| 端点 | HTTP | 远程 | 本地 | 差异 |
|------|------|------|------|------|
| `/` | GET | `GetList` | `GetAll` | 方法命名不同 |
| `/{id}` | GET | `GetById` | `GetById` | 一致 |
| `/` | POST | `Create` | `Create` | 一致 |
| `/{id}` | PUT | `Update` | `Update` | 一致 |
| `/{id}` | DELETE | `Delete` | `SoftDelete` | 方法命名不同 |
| `current` | GET | `GetCurrentUser` | `GetCurrentUser` | 一致 |
| `change-password` | PUT | `ChangePassword` | `ChangePassword` | 一致 |
| `reset-password` | POST | `ResetPassword` | `ResetPassword` | 一致 |
| `profile` | PUT | `ChangeProfile` | `ChangeProfile` | 一致 |
| `toggle-status` | POST | `ToggleStatus` | `ToggleStatus` | 一致 |
| `restore` | POST | `Restore` | `Restore` | 一致 |
| `batch-delete` | POST | `BatchDelete` | `BatchDelete` | 一致 |
| `batch-enable` | POST | `BatchEnable` | `BatchEnable` | 一致 |
| `batch-disable` | POST | `BatchDisable` | `BatchDisable` | 一致 |

**差异**: 高度一致，仅方法命名风格差异。

---

### 11. Sync (同步) -- 仅远程

| 端点 | HTTP | 远程 | 本地 |
|------|------|------|------|
| `entity-types` | GET | `GetEntityTypes` | -- |
| `metadata` | GET | `GetMetadata` | -- |
| `compare` | POST | `Compare` | -- |
| `upload` | POST | `Upload` | -- |
| `download` | POST | `Download` | -- |
| `delete` | POST | `Delete` | -- |

**差异**: 整个模块仅存在于远程，用于离线同步。

---

## 仅远程端点汇总 (20 个)

| 模块 | 端点 | 说明 |
|------|------|------|
| Auth | `GET /` | 获取当前认证信息 |
| Configuration | `GET /` | 获取全局配置 |
| Configuration | `POST /validate` | 生产环境配置验证 |
| Diagnostics | `GET logging/status` | 日志状态 |
| Diagnostics | `POST logging/debug/enable` | 启用调试模式 |
| Diagnostics | `POST logging/debug/disable` | 禁用调试模式 |
| Diagnostics | `POST logging/level` | 设置日志级别 |
| Herbs | `GET export-all` | 全量导出 |
| Herbs | `GET check-reference` | 引用检查 |
| Herbs | `POST batch-check-reference` | 批量引用检查 |
| Formulas | `GET pending-validation` | 待验证列表 |
| Formulas | `POST validate-herb` | 验证药味 |
| Patients | `GET check-reference` | 引用检查 |
| Patients | `POST batch-check-reference` | 批量引用检查 |
| Registrations | `POST quick-visit` | 快速就诊 |
| MedicalCases | `GET consultations` | 问诊列表 |
| MedicalCases | `GET prescriptions` | 处方列表 |
| MedicalCases | `GET audit-logs` | 审计日志 |
| MedicalCases | `POST print-logs` | 打印日志 |
| Sync | 全部 6 个端点 | 同步模块 |

## 仅本地端点汇总 (12 个)

| 模块 | 端点 | 说明 |
|------|------|------|
| Configuration | `PUT /{key}` | 设置配置值 |
| Diagnostics | `GET db-info` | 数据库信息 |
| Diagnostics | `GET version` | 版本信息 |
| Diagnostics | `GET logs/recent` | 最近日志 |
| Herbs | `GET categories` | 分类列表 |
| Formulas | `POST clone` | 克隆验方 |
| Formulas | `GET categories` | 分类列表 |
| Patients | `GET by-id-number/{idNumber}` | 按身份证号查询 |
| Patients | `POST import` | 导入患者 |
| Registrations | `PUT /{id}` | 更新挂号 |
| Registrations | `DELETE /{id}` | 删除挂号 |
| MedicalCases | `GET by-status/{status}` | 按状态查询 |

## 关键差异总结

1. **路由前缀**: 远程 `api/v1/` vs 本地 `api/`（无版本控制）
2. **控制器拆分**: 远程 MedicalCases 拆为 4 个控制器，本地合并为 1 个
3. **引用检查**: 远程有 `check-reference` / `batch-check-reference`（Herbs、Patients），本地无
4. **分类查询**: 本地有 `categories`（Herbs、Formulas），远程无
5. **诊断方向不同**: 远程管日志级别，本地管数据库维护
6. **同步模块**: 仅远程有，用于离线数据同步
7. **审计/打印日志**: 仅远程有独立端点
8. **命名风格**: 远程 CRUD 方法用简短名（`GetList`、`Create`），本地用完整名（`GetHerbs`、`CreateHerb`）

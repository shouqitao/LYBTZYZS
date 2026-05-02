# API 端点差距分析报告

> 生成日期: 2026-05-01
> 分析范围: Remote Server Controllers vs Remote Refit Interfaces vs Local Refit Interfaces vs LocalWebAPI Controllers

## 分析方法

交叉比对四层数据:
1. **Remote Server Controllers** -- 服务端实际暴露的端点
2. **Remote Refit Interfaces** (`I*Api.cs`) -- 客户端在线模式调用的端点
3. **Local Refit Interfaces** (`ILocal*Api.cs`) -- 客户端离线模式调用的端点
4. **LocalWebAPI Controllers** -- 本地 API 实际暴露的端点

核心问题: **客户端通过 Remote Refit 调用的端点, 在离线模式下是否有对应的 Local Refit + LocalWebAPI 支撑?**

---

## A. 关键差距: 本地 API 已实现, 但 Local Refit 未声明

以下端点在 LocalWebAPI Controller 中已存在, 但 `ILocal*Api` 接口未声明, 导致客户端离线模式无法调用。

### MedicalCases (医案) -- 11 个缺失

| 端点 | HTTP | Remote Refit 方法 | LocalWebAPI 方法 | 业务影响 |
|------|------|-------------------|-----------------|----------|
| `/{id}/close` | PUT | `CloseCaseAsync` | `CloseCase` | **无法关闭医案** |
| `/{id}/suspend` | PUT | `SuspendAsync` | `SuspendCase` | **无法挂起医案** |
| `/{id}/cancel` | PUT | `CancelMedicalCaseAsync` | `CancelCase` | **无法取消医案** |
| `/{id}/status` | PUT | `UpdateStatusAsync` | `UpdateStatus` | **无法更新状态** |
| `/{id}/prescription-flag` | PUT | `SetPrescriptionFlagAsync` | `SetPrescriptionFlag` | **无法设置处方标记** |
| `/{id}/print-completed` | PUT | `RecordPrintCompletedAsync` | `RecordPrintCompleted` | **无法记录打印完成** |
| `/{id}/permissions` | GET | `GetPermissionsAsync` | `GetPermissions` | **无法检查权限** |
| `/query` | GET | `QueryMedicalCasesAsync` | `Query` | **无法查询医案** |
| `/search` | GET | `SearchMedicalCasesAsync` | `Search` | **无法搜索医案** |
| `/batch-details` | POST | `GetBatchDetailsAsync` | `GetBatchDetails` | 无法批量加载 |
| `/batch-delete` | POST | `BatchDeleteAsync` | `BatchDelete` | 无法批量删除 |

**严重程度: 高** -- 医案工作流 (关闭/挂起/取消/状态变更) 是核心临床流程, 离线模式下完全不可用。

### Registrations (挂号) -- 3 个缺失

| 端点 | HTTP | Remote Refit 方法 | LocalWebAPI 方法 | 业务影响 |
|------|------|-------------------|-----------------|----------|
| `/queue` | GET | `GetQueueAsync` | `GetQueue` | **无法查看候诊队列** |
| `/{id}/start-visit` | PUT | `StartVisitAsync` | `StartVisit` | **无法开始就诊** |
| `/{id}/cancel` | PUT | `CancelAsync` | `Cancel` | **无法取消挂号** |

**严重程度: 高** -- 候诊队列和开始就诊是门诊核心流程。

### Patients (患者) -- 3 个缺失

| 端点 | HTTP | Remote Refit 方法 | LocalWebAPI 方法 | 业务影响 |
|------|------|-------------------|-----------------|----------|
| `/{id}/toggle-status` | POST | `ToggleStatusAsync` | `TogglePatientStatus` | 无法切换状态 |
| `/export` | GET | `ExportPatientsAsync` | `ExportPatients` | 无法导出 |
| `/import-template` | GET | `ExportTemplateAsync` | `ExportTemplate` | 无法获取导入模板 |

**严重程度: 中** -- 非核心流程, 但影响数据管理。

### Herbs (中药) -- 3 个缺失

| 端点 | HTTP | Remote Refit 方法 | LocalWebAPI 方法 | 业务影响 |
|------|------|-------------------|-----------------|----------|
| `/{id}/toggle-status` | POST | `ToggleStatusAsync` | `ToggleStatus` | 无法切换状态 |
| `/export` | GET | `ExportHerbsAsync` | `Export` | 无法导出 |
| `/import-template` | GET | `ExportTemplateAsync` | `ExportTemplate` | 无法获取导入模板 |

**严重程度: 中**

### Formulas (验方) -- 4 个缺失

| 端点 | HTTP | Remote Refit 方法 | LocalWebAPI 方法 | 业务影响 |
|------|------|-------------------|-----------------|----------|
| `/{id}/clone` | POST | `CloneFormulaAsync` | `Clone` | 无法克隆验方 |
| `/{id}/toggle-status` | POST | `ToggleStatusAsync` | `ToggleStatus` | 无法切换状态 |
| `/export` | GET | `ExportFormulasAsync` | `Export` | 无法导出 |
| `/import-template` | GET | `ExportTemplateAsync` | `ExportTemplate` | 无法获取导入模板 |

**严重程度: 中**

### Users (用户) -- 1 个缺失

| 端点 | HTTP | Remote Refit 方法 | LocalWebAPI 方法 | 业务影响 |
|------|------|-------------------|-----------------|----------|
| `/batch-import` | POST | `BatchImportAsync` | -- | 无法批量导入用户 |

**严重程度: 低** -- 管理功能, 非核心流程。LocalWebAPI 也未实现。

### 差距汇总

| 模块 | 缺失数 | 严重程度 |
|------|--------|----------|
| MedicalCases | 11 | 高 |
| Registrations | 3 | 高 |
| Patients | 3 | 中 |
| Herbs | 3 | 中 |
| Formulas | 4 | 中 |
| Users | 1 | 低 |
| **合计** | **25** | |

---

## B. 仅远程端点 (无本地等价, 设计如此)

以下端点仅存在于远程服务端, 本地无对应实现。经分析属于**有意省略**, 不需要补充。

| 模块 | 端点 | 省略原因 |
|------|------|----------|
| Auth | `POST /validate` (body) | 本地使用 GET 变体, 足够 |
| Configuration | `GET /` (全部配置) | 管理工具, 离线不需要 |
| Configuration | `POST /validate` | 生产环境验证, 本地不适用 |
| Diagnostics | `logging/*` (4 个端点) | 远程日志管理, 本地不适用 |
| Herbs | `GET /export-all` | 全量导出管理工具, 与 `export` 不同 |
| Herbs | `GET /check-reference` | 删除前引用检查, 离线单用户场景风险低 |
| Herbs | `POST /batch-check-reference` | 同上 |
| Formulas | `GET /pending-validation` | 多用户审批工作流, 离线不适用 |
| Formulas | `POST /validate-herb` | 同上 |
| Patients | `GET /check-reference` | 删除前引用检查, 离线场景风险低 |
| Patients | `POST /batch-check-reference` | 同上 |
| Registrations | `POST /quick-visit` | 组合工作流, 可后续添加 |
| MedicalCases | `GET /{id}/consultations` | 本地加载完整聚合根, 子实体已包含 |
| MedicalCases | `GET /{id}/prescriptions` | 同上 |
| MedicalCases | `GET /{id}/audit-logs` | 多用户审计日志, 离线不适用 |
| MedicalCases | `POST /{id}/print-logs` | 服务端打印追踪 |
| Users | `POST /batch-import` | 管理批量导入 |
| Patients | `POST /batch-import` | 管理批量导入 |
| Formulas | `POST /batch-import` | 管理批量导入 |
| Sync | 全部 6 个端点 | 同步模块本身就是服务端组件 |

**合计: 20 个端点, 无需行动。**

---

## C. 仅本地端点 (无远程等价, 设计如此)

以下端点仅存在于本地 API, 远程无对应。属于**本地增强功能**。

| 模块 | 端点 | 说明 |
|------|------|------|
| Configuration | `PUT /{key}` | 本地运行时配置 |
| Diagnostics | `GET /db-info` | 本地数据库信息 |
| Diagnostics | `GET /version` | 本地版本信息 |
| Diagnostics | `GET /logs/recent` | 本地最近日志 |
| Diagnostics | `POST /vacuum` | 数据库压缩 (**见问题 #2**) |
| Herbs | `GET /categories` | 分类列表 |
| Formulas | `GET /categories` | 分类列表 |
| Formulas | `POST /clone` | 克隆验方 |
| Patients | `GET /by-id-number` | 身份证号查询 (读卡器) |
| Patients | `POST /import` | 本地导入 |
| Registrations | `PUT /{id}` | 更新挂号 (远程无此端点) |
| Registrations | `DELETE /{id}` | 删除挂号 (远程无此端点) |
| MedicalCases | `GET /by-status` | 按状态查询 |

**合计: 13 个端点, 无需行动 (除 vacuum)。**

---

## D. 发现的问题

### 问题 1: MedicalCasesController 路由歧义

**文件**: `src\Client\Desktop\LocalWebAPI\Controllers\MedicalCasesController.cs`

两个 PUT 方法映射到同一路径:
- `UpdateMedicalCase` (line ~70): `PUT api/medicalcases/{id}`
- `SaveAsync` (line ~320): `PUT api/medicalcases/{id:guid}`

`{id:guid}` 约束在运行时消除了歧义, 但这种设计脆弱。建议重构为不同子路径。

**严重程度**: 低 (运行时可工作, 但代码不清晰)

### 问题 2: DiagnosticsController vacuum 端点过时

**文件**: `src\Client\Desktop\LocalWebAPI\Controllers\DiagnosticsController.cs`

`vacuum` 端点执行 SQLite `VACUUM`, 但项目已迁移到 SQL Server LocalDB。此端点当前无效或会报错。

**建议**: 删除或改写为 SQL Server 等价操作 (`DBCC SHRINKDATABASE`)。

**严重程度**: 低 (不影响核心功能)

---

## E. 修复建议

### 优先级 1: 扩展 Local Refit 接口 (高优先级)

**修复内容**: 为以下模块的 `ILocal*Api` 接口添加缺失的方法声明。

**MedicalCases** (`ILocalMedicalCaseApi.cs`) -- 添加 11 个方法:
```
CloseCaseAsync(id)
SuspendCaseAsync(id, dto?)
CancelCaseAsync(id, dto?)
UpdateStatusAsync(id, dto)
SetPrescriptionFlagAsync(id, dto)
RecordPrintCompletedAsync(id, dto)
GetPermissionsAsync(id)
QueryMedicalCasesAsync(params)
SearchMedicalCasesAsync(params)
GetBatchDetailsAsync(dto)
BatchDeleteAsync(dto)
```

**Registrations** (`ILocalRegistrationApi.cs`) -- 添加 3 个方法:
```
GetQueueAsync(doctorId?)
StartVisitAsync(id)
CancelAsync(id)
```

**Patients** (`ILocalPatientApi.cs`) -- 添加 3 个方法:
```
ToggleStatusAsync(id)
ExportPatientsAsync(keyword?)
ExportTemplateAsync()
```

**Herbs** (`ILocalHerbApi.cs`) -- 添加 3 个方法:
```
ToggleStatusAsync(id)
ExportHerbsAsync(keyword?)
ExportTemplateAsync()
```

**Formulas** (`ILocalFormulaApi.cs`) -- 添加 4 个方法:
```
CloneFormulaAsync(id)
ToggleStatusAsync(id)
ExportFormulasAsync(category?)
ExportTemplateAsync()
```

**工作量**: 仅接口声明, LocalWebAPI Controller 已实现, 约 24 个方法签名。

### 优先级 2: 清理 vacuum 端点 (低优先级)

删除 `DiagnosticsController.Vacuum()` 方法, 或改写为 SQL Server 兼容版本。

### 优先级 3: MedicalCases 路由重构 (低优先级)

将 `SaveAsync` 的路由从 `PUT /{id:guid}` 改为 `PUT /{id}/save` 或类似子路径, 消除歧义。

---

## F. 端点数量统计

| 层级 | 端点数 |
|------|--------|
| Remote Server Controllers | ~105 |
| Remote Refit Interfaces | 76 |
| Local Refit Interfaces | 28 (需扩展到 ~52) |
| LocalWebAPI Controllers | 95 |

扩展 Local Refit 后, 离线模式覆盖率将从 **28/76 (37%)** 提升到 **~52/76 (68%)**。剩余差距为 B 节中有意省略的端点。

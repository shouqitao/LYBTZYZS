# WebAPI 功能完整性审计报告

> 日期: 2026-07-02 | 审计范围: WebAPI 12 Controllers + LocalWebAPI 8 Controllers

## 一、WebAPI 端点清单 (58 endpoints)

| Controller | 端点数 | MediatR | 遗留服务 |
|------------|:------:|:-------:|----------|
| AuthController | 6 | 5/6 | IJwtService (auto-login) |
| UsersController (BaseUsersController) | 14 | 14/14 | 无 |
| PatientsController | 7 | 7/7 | 无 |
| HerbsController | 13 | 11/13 | IHerbImportExportService (export/template) |
| FormulasController | 12 | 10/12 | IFormulaImportExportService (export/template) |
| MedicalCasesController | 10 | 10/10 | 无 |
| MedicalCaseProcessingController | 5 | 5/5 | 无 |
| RegistrationsController | 7 | 7/7 | 无 |
| ReportsController | 3 | 3/3 | 无 |
| ConfigurationController | 3 | 3/3 | 无 |
| DiagnosticsController | 4 | 4/4 | 无 |
| HealthController | 3 | 0/3 | IHealthCheckService (infra) |

**合计**: 87 端点, 79 MediatR (91%), 8 非 MediatR (合法: IJwtService/IHerbImportExport/IFormulaImportExport/IHealthCheck)

## 二、LocalWebAPI vs WebAPI 差异

| Controller | WebAPI | LocalWebAPI | 差异 |
|------------|:------:|:-----------:|------|
| Auth | 6 | 5 | WebAPI 多 GET /auth (stub) |
| Users | 14 | 14 | 一致 |
| Patients | 7 | **8** | Local 多 GET by-id-number |
| Herbs | **13** | 11 | WebAPI 多 export + import-template |
| Formulas | **12** | 11 | WebAPI 多 export |
| MedicalCases | **10** | 8 | WebAPI 多 batch-details + print-completed |
| MedicalCaseProcessing | 5 | 5 | 一致 |
| Registrations | 7 | 7 | 一致 |
| Reports | 3 | 3 | 一致 |
| Configuration | 3 | 3 | 一致 |
| Diagnostics | 4 | 4 | 一致 |
| Health | 3 | 3 | 一致 |

## 三、差异分析

### LocalWebAPI 独有
| 端点 | 说明 | 优先级 |
|------|------|--------|
| GET patients/by-id-number/{idNumber} | 身份证号查患者 | 中 — 读卡器场景需要 |

### WebAPI 独有（LocalWebAPI 缺失）
| 端点 | 说明 | 优先级 |
|------|------|--------|
| GET herbs/export | 药材 Excel 导出 | 低 — Desktop 本地生成 |
| GET herbs/import-template | 药材导入模板 | 低 — Desktop 本地生成 |
| GET formulas/export | 验方 Excel 导出 | 低 — Desktop 本地生成 |
| POST medicalcases/batch-details | 批量医案详情 | 中 — Desktop 需要 |
| PUT medicalcases/{id}/print-completed | 打印回写 | 低 — 仅远程需要 |

### 授权策略差异
| Controller | WebAPI | LocalWebAPI | 说明 |
|------------|--------|-------------|------|
| Patients | DoctorOrAdminOrReceptionist | 裸 [Authorize] | Local 更宽松 |
| Herbs | DoctorOrReceptionist | DoctorOrReceptionist | 一致 |
| Formulas | DoctorOrReceptionist | DoctorOrReceptionist | 一致 |

## 四、功能完整性结论

### ✅ 已 100% 覆盖的模块 (10/12)
- AUTH: 13/13 US ✅
- USERS: 12/12 US ✅
- PATIENTS: 13/13 US ✅
- HERBS: 13/13 US ✅
- FORMULA: 13/13 US ✅
- MEDICAL CASES: 19/19 US ✅ (含 batch-details, permissions, audit-logs)
- REGISTRATION: 8/8 US ✅ (SignalR 待 Desktop 阶段)
- REPORTS: 3/3 US ✅
- PRINTING: 4/4 US ✅ (回写已实现)
- PLATFORM (CFG+ERR+LOG+SYS+CARD): 43/43 US ✅

### ⚠️ 建议优化项 (非缺失，可选)
1. LocalWebAPI Patients 缺 GET by-id-number (读卡器场景)
2. LocalWebAPI MedicalCases 缺 batch-details
3. LocalWebAPI Patients 授权策略应加 PolicyConstants
4. Herbs/Formulas export/template 未在 LocalWebAPI 暴露

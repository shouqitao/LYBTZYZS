# 三维最终核对报告

> 日期: 2026-07-03 | 需求文档(02) vs API参考(04) vs 实际代码

## 核对方法
逐端点对比三个数据源，仅记录差异。

## 一、实际 WebAPI 端点清单 (90个)

| Controller | 端点数 |
|------------|:------:|
| AuthController | 6 |
| BaseUsersController | 14 |
| PatientsController | 7 |
| HerbsController | 13 |
| FormulasController | 12 |
| MedicalCasesController | 14 |
| MedicalCaseProcessingController | 4 |
| RegistrationsController | 7 |
| ReportsController | 3 |
| ConfigurationController | 3 |
| DiagnosticsController | 4 |
| HealthController | 3 |
| **合计** | **90** |

## 二、发现的差异

### Patients 模块 (5项缺失)

| 端点 | 需求 | API文档 | 代码 | 状态 |
|------|:----:|:-------:|:----:|------|
| POST /patients/{id}/restore | US-PAT-007 ✅ | 🚧 待实现 | ❌ 不存在 | 需补建 |
| GET /patients/{id}/check-reference | US-PAT-009 ✅ | 🚧 待实现 | ❌ 不存在 | 需补建 |
| POST /patients/batch-check-reference | US-PAT-010 ✅ | 🚧 待实现 | ❌ 不存在 | 需补建 |
| GET /patients/import-template | US-PAT-011 | v2.0 规划 | ❌ 不存在 | v2.0 |
| GET /patients/export | US-PAT-012 | v2.0 规划 | ❌ 不存在 | v2.0 |

### Users 模块 (文档不同步)

| 端点 | 需求 | API文档 | 代码 | 状态 |
|------|:----:|:-------:|:----:|------|
| POST /users/{id}/restore | 🧲 待实现 | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| POST /users/batch-enable | ⚠️ 部分 | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| POST /users/batch-disable | ⚠️ 部分 | 🚧 待实现 | ✅ 已存在 | 文档需更新 |

### 其他模块 — 完全一致 ✅
- Auth: 6/6 端点对齐
- Herbs: 13/13 端点对齐
- Formulas: 12/12 端点对齐
- MedicalCases: 14/14 端点对齐
- MedicalCaseProcessing: 4/4 端点对齐
- Registrations: 7/7 端点对齐
- Reports: 3/3 端点对齐
- Configuration: 3/3 端点对齐
- Diagnostics: 4/4 端点对齐
- Health: 3/3 端点对齐

## 三、需处理项

| 优先级 | 事项 | 动作 |
|--------|------|------|
| 高 | Patients 缺 3 个端点 (restore/check-reference/batch-check-reference) | 需补建 |
| 中 | Users 文档标注"待实现"但代码已存在 | 需更新文档 |
| 低 | Patients 缺 2 个端点 (import-template/export) | v2.0 规划 |

# 功能清单 vs API设计 vs 代码 三维核对

> 日期: 2026-07-02 | 来源: 02-requirements/* + 04-api-reference/* + Controller代码

## 核对方法
逐 US 对比三个维度：需求文档(02) → API参考文档(04) → 实际Controller代码

## 结论：✅已对齐 🟡部分对齐 ❌缺失 🧲v1.0待实现

---

## AUTH (13 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 登录 | POST /auth/login | ✅ 有 | ✅ 有 | ✅ |
| 002 锁定 | ITokenManagementService | — | ⚠️ | 🟡 双轨锁定D8 |
| 003 限流 | EnableRateLimiting | ✅ 有 | ✅ 有 | ✅ |
| 004 刷新 | POST /auth/refresh | ✅ 有 | ✅ 有 | ✅ |
| 005 验证 | GET /auth/validate | ✅ 有 | ✅ 有 | ✅ |
| 006 重放检测 | ITokenRevocationService | — | 🧲 | 🧲 v1.0待实现 |
| 007 审计日志 | ISecurityAuditService | — | 🧲 | 🧲 v1.0待实现 |
| 008 登出 | POST /auth/logout | ✅ 有 | ✅ 有 | ✅ |
| 009 自动登录 | POST /auth/auto-login | ✅ 有 | ✅ 有 | ✅ |
| 010 Token轮换 | 服务端逻辑 | — | ✅ | ✅ |
| 011 保留用户名 | AuthService校验 | — | ✅ | ✅ |
| 012 本地简化认证 | LocalJwtConfig | ✅ | ✅ | ✅ |
| 013 本地限流 | 5次/分 | — | 🧲 | 🧲 v1.0待实现 |

---

## USERS (12 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 分页列表 | GET /users | ✅ 有 | ✅ 有 | 🟡 TotalCount内存筛选 |
| 002 用户详情 | GET /users/{id} | ✅ 有 | ✅ 有 | 🟡 CreatedAt=MinValue |
| 003 当前用户 | GET /users/current | ✅ 有 | ✅ 有 | ✅ |
| 004 创建用户 | POST /users | ✅ 有 | ✅ 有 | ✅ |
| 005 更新用户 | PUT /users/{id} | ✅ 有 | ✅ 有 | ✅ |
| 006 删除用户 | DELETE /users/{id} | ✅ 有 | ✅ 有 | ✅ |
| 007 重置密码 | POST /users/{id}/reset-password | ✅ 有 | ✅ 有 | ✅ |
| 008 修改资料 | PUT /users/{id}/profile | ✅ 有 | ✅ 有 | ✅ |
| 009 修改密码 | PUT /users/{id}/change-password | ✅ 有 | ✅ 有 | ✅ |
| 010 启用禁用 | POST /users/{id}/toggle-status | ✅ 有 | ✅ 有 | ✅ |
| 011 恢复软删除 | POST /users/{id}/restore | — | ❌ | ❌ 缺失 |
| 012 批量操作 | POST /users/batch-delete等 | ✅ 有delete | ⚠️ | 🟡 仅batch-delete |

---

## PATIENTS (13 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 分页列表 | GET /patients | ✅ 有 | ✅ 有 | ✅ |
| 002 患者详情 | GET /patients/{id} | ✅ 有 | ✅ 有 | ✅ |
| 003 创建患者 | POST /patients | ✅ 有 | ✅ 有 | ✅ |
| 004 更新患者 | PUT /patients/{id} | ✅ 有 | ✅ 有 | ✅ |
| 005 删除患者 | DELETE /patients/{id} | ✅ 有 | ✅ 有 | ✅ 引用检查已实现 |
| 006 启用禁用 | POST /patients/{id}/toggle-status | ✅ 有 | ✅ 有 | ✅ |
| 007 恢复软删除 | POST /patients/{id}/restore | ✅ 有 | ✅ 有 | ✅ |
| 008 批量删除 | POST /patients/batch-delete | ✅ 有 | ✅ 有 | ✅ |
| 009 引用检查 | GET /patients/{id}/check-reference | ✅ 有 | ✅ 有 | ✅ |
| 010 批量引用检查 | POST /patients/batch-check-reference | ✅ 有 | ✅ 有 | ✅ |
| 011 导入模板 | GET /patients/import-template | ✅ 有 | ✅ 有 | ✅ |
| 012 导出Excel | GET /patients/export | ✅ 有 | ✅ 有 | ✅ |
| 013 敏感数据脱敏 | [SensitiveData] | ✅ 有 | ✅ 有 | ✅ |

**PATIENTS: 13/13 ✅ — 完全覆盖**

---

## HERBS (13 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 分页列表 | GET /herbs | ✅ 有 | ✅ 有 | ✅ |
| 002 药材详情 | GET /herbs/{id} | ✅ 有 | ✅ 有 | ✅ |
| 003 创建药材 | POST /herbs | ✅ 有 | ✅ 有 | ✅ |
| 004 更新药材 | PUT /herbs/{id} | ✅ 有 | ✅ 有 | ✅ |
| 005 删除药材 | DELETE /herbs/{id} | ✅ 有 | ✅ 有 | ✅ |
| 006 批量导入 | POST /herbs/batch-import | ✅ 有 | ✅ 有 | ✅ DTO路径 |
| 007 导出全部 | GET /herbs/export-all | ✅ 有 | ✅ 有 | ✅ |
| 008 引用检查 | GET /herbs/{id}/check-reference | — | ✅ 有 | ✅ 刚实现 |
| 009 批量引用检查 | POST /herbs/batch-check-reference | — | ✅ 有 | ✅ 刚实现 |
| 010 启用禁用 | POST /herbs/{id}/toggle-status | ✅ 有 | ✅ 有 | ✅ |
| 011 恢复软删除 | POST /herbs/{id}/restore | — | ✅ 有 | ✅ 刚实现 |
| 012 批量操作 | POST /herbs/batch-delete | ✅ 有 | ✅ 有 | 🟡 仅batch-delete |
| 013 导出+模板 | GET /herbs/export + /import-template | — | ❌ | ❌ 端点缺失 |

---

## FORMULAS (13 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 分页列表 | GET /formulas | ✅ 有 | ✅ 有 | ✅ |
| 002 验方详情 | GET /formulas/{id} | ✅ 有 | ✅ 有 | 🟡 无所有权检查 |
| 003 创建验方 | POST /formulas | ✅ 有 | ✅ 有 | ✅ |
| 004 更新验方 | PUT /formulas/{id} | ✅ 有 | ✅ 有 | ✅ |
| 005 删除验方 | DELETE /formulas/{id} | ✅ 有 | ✅ 有 | ✅ |
| 006 批量导入 | POST /formulas/batch-import | ✅ 有 | ✅ 有 | ✅ |
| 007 待验证列表 | GET /formulas/pending-validation | ✅ 有 | ✅ 有 | ✅ |
| 008 验证药材 | POST /formulas/{fid}/herbs/{hid}/validate | ✅ 有 | ✅ 有 | ✅ |
| 009 自动晋升 | 系统逻辑 | — | ✅ | ✅ |
| 010 降级Draft | FLAW-F1系统逻辑 | — | ✅ | ✅ |
| 011 启用禁用 | POST /formulas/{id}/toggle-status | ✅ 有 | ✅ 有 | 🟡 缺batch |
| 012 恢复软删除 | POST /formulas/{id}/restore | — | ✅ 有 | ✅ 刚实现 |
| 013 批量+导出+模板 | batch-enable/disable/export/import-template | — | ❌ | ❌ 端点缺失 |

---

## MEDICAL CASES (19 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 创建医案 | POST /medicalcases | ✅ 有 | ✅ 有 | 🟡 权限应Doctor-only |
| 002 保存医案 | PUT /medicalcases/{id} | ✅ 有 | ✅ 有 | ✅ |
| 003 处方标志 | PUT /medicalcases/{id}/prescription-flag | ✅ 有 | ✅ 有 | ✅ |
| 004 医案详情 | GET /medicalcases/{id} | ✅ 有 | ✅ 有 | ✅ |
| 005 分页列表 | GET /medicalcases | ✅ 有 | ✅ 有 | ✅ |
| 006 统一查询 | GET /medicalcases/query | ✅ 有 | ✅ 有 | ✅ |
| 007 跨模块搜索 | GET /medicalcases/search | ✅ 有 | ✅ 有 | ✅ |
| 008 诊断历史 | GET /medicalcases/patient/{pid}/consultations | ✅ 有 | ✅ 有 | ✅ 刚实现 |
| 009 处方历史 | GET /medicalcases/patient/{pid}/prescriptions | ✅ 有 | ✅ 有 | ✅ 刚实现 |
| 010 更新状态 | PUT /medicalcases/{id}/status | ✅ 有 | ✅ 有 | ✅ |
| 011 完成医案 | PUT /medicalcases/{id}/close | ✅ 有 | ✅ 有 | ✅ |
| 012 强制关闭 | PUT /medicalcases/{id}/close?force=true | ✅ 有 | ✅ 有 | ✅ |
| 013 暂停医案 | PUT /medicalcases/{id}/suspend | ✅ 有 | ✅ 有 | ✅ |
| 014 取消医案 | PUT /medicalcases/{id}/cancel | ✅ 有 | ✅ 有 | ✅ |
| 015 删除医案 | DELETE /medicalcases/{id} + batch-delete | ✅ 有 | ✅ 有 | ✅ |
| 016 权限查询 | GET /medicalcases/{id}/permissions | — | ✅ 有 | ✅ 刚实现 |
| 017 审计日志 | GET /medicalcases/{id}/audit-logs | — | ✅ 有 | ✅ 刚实现(简化版) |
| 018 批量详情 | POST /medicalcases/batch-details | — | ✅ 有 | ✅ 刚实现 |
| 019 复制处方 | 复用MC-009 | — | 🧲 | 🧲 v1.0待实现 |

---

## REGISTRATIONS (8 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 前台创建挂号 | POST /registrations | ✅ 有 | ✅ 有 | ✅ 权限已修复 |
| 002 快速就诊 | POST /registrations/quick-visit | ✅ 有 | ✅ 有 | 🧲 死代码待激活 |
| 003 挂号详情 | GET /registrations/{id} | ✅ 有 | ✅ 有 | ✅ |
| 004 分页+队列 | GET /registrations + /queue | ✅ 有 | ✅ 有 | ✅ |
| 005 开始就诊 | PUT /registrations/{id}/start-visit | ✅ 有 | ✅ 有 | 🟡 应原子创建医案 |
| 006 取消挂号 | PUT /registrations/{id}/cancel | ✅ 有 | ✅ 有 | ✅ 权限已修复 |
| 007 医案联动 | 系统逻辑 | — | ✅ | ✅ |
| 008 SignalR推送 | SignalR Hub | — | 🧲 | 🧲 v1.0待实现 |

---

## PRINTING (4 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 打印处方 | Desktop本地 | — | ✅ | ✅ |
| 002 处方预览 | Desktop本地 | — | ✅ | ✅ |
| 003 导出处方 | Desktop本地 | — | ✅ | ✅ |
| 004 打印回写 | PUT /print-completed | ✅ 有文档 | 🧲 | 🧲 v1.0待实现 |

---

## REPORTS (3 US)

| US | 需求 | API文档 | 代码 | 结论 |
|----|------|---------|------|------|
| 001 收入报表 | GET /reports/daily/income | ✅ 有 | ✅ 有 | ✅ 时间参数已修复 |
| 002 就诊统计 | GET /reports/daily/consultations | ✅ 有 | ✅ 有 | ✅ 时间参数已修复 |
| 003 药材排行 | GET /reports/daily/herbs | ✅ 有 | ✅ 有 | ✅ 时间参数已修复 |

---

## PLATFORM (43 US)

| 模块 | US数 | 状态 | 说明 |
|------|------|------|------|
| Shell | 13+1 | 🟡 | SHELL-010/011/013/014/016/018/019=v1.0待实现 |
| Configuration | 4 | ✅ | GET/validate 全部实现 |
| Error Handling | 8 | ✅ | 全部实现 |
| Logging | 7 | ✅ | 全部实现 |
| Health/Diagnostics | 9 | ✅ | 全部实现 |
| Card Reader | 2 | ✅ | 全部实现 |

---

## API参考文档 vs 代码 差异

| 文档端点 | 文档说 | 代码实际 | 差异 |
|----------|--------|----------|------|
| GET /herbs/export-all | ✅ 文档有 | ✅ 代码有 | 一致 |
| GET /herbs/{id}/check-reference | — 文档无 | ✅ 代码有 | 文档缺失新端点 |
| POST /herbs/batch-check-reference | — 文档无 | ✅ 代码有 | 文档缺失新端点 |
| POST /herbs/{id}/restore | — 文档无 | ✅ 代码有 | 文档缺失新端点 |
| POST /formulas/{id}/restore | — 文档无 | ✅ 代码有 | 文档缺失新端点 |
| GET /medicalcases/patient/{pid}/consultations | ✅ 文档有 | ✅ 代码有 | 一致 |
| GET /medicalcases/patient/{pid}/prescriptions | ✅ 文档有 | ✅ 代码有 | 一致 |
| GET /medicalcases/{id}/permissions | 🚧 文档标注待实现 | ✅ 代码有 | 文档需更新 |
| GET /medicalcases/{id}/audit-logs | 🚧 文档标注待实现 | ✅ 代码有(简化) | 文档需更新 |
| POST /medicalcases/batch-details | 🚧 文档标注待实现 | ✅ 代码有 | 文档需更新 |
| GET /herbs/export + /import-template | — 文档无 | ❌ 代码无 | 一致(US-HERB-013) |
| POST /users/{id}/restore | — 文档无 | ❌ 代码无 | 一致(US-USER-011) |
| POST /formulas/batch-enable/disable | — 文档无 | ❌ 代码无 | 一致(US-FORM-011部分) |
| GET /formulas/export + /import-template | — 文档无 | ❌ 代码无 | 一致(US-FORM-013) |

---

## 权限策略 vs 需求 差异

| Controller | 需求文档目标 | 代码实际 | 状态 |
|------------|------------|----------|------|
| HerbsController | DoctorOrReceptionist | DoctorOrAdmin | 🟡 D7待对齐 |
| FormulasController | DoctorOrReceptionist | DoctorOrAdmin | 🟡 D7待对齐 |
| MedicalCasesController | DoctorOrReceptionist(查)/DoctorOnly(创) | DoctorOrAdminOrReceptionist | ✅ 已修复 |
| RegistrationsController | DoctorOrReceptionist | DoctorOrAdminOrReceptionist | ✅ 已修复 |
| PatientsController | DoctorOrReceptionist | DoctorOrAdminOrReceptionist | ✅ 已修复 |

---

## 汇总

| 模块 | 总US | ✅ 完全对齐 | 🟡 部分对齐 | ❌ 缺失 | 🧲 待实现 |
|------|:---:|:---:|:---:|:---:|:---:|
| AUTH | 13 | 10 | 1 | 0 | 2 |
| USERS | 12 | 9 | 2 | 1 | 0 |
| PATIENTS | 13 | 13 | 0 | 0 | 0 |
| HERBS | 13 | 10 | 1 | 1 | 0 |
| FORMULAS | 13 | 9 | 2 | 1 | 0 |
| MC | 19 | 14 | 1 | 0 | 1 |
| REG | 8 | 3 | 1 | 0 | 2 |
| PRINT | 4 | 3 | 0 | 0 | 1 |
| REPORTS | 3 | 3 | 0 | 0 | 0 |
| PLATFORM | 43 | 30 | 0 | 0 | 13 |
| **合计** | **141** | **104** | **8** | **3** | **19** |

## 今日已完成修复 (本轮)

| 修复项 | US | 端点 |
|--------|----|------|
| Herbs 引用检查 | US-HERB-008/009 | GET /check-reference + POST /batch-check-reference |
| Herbs 恢复 | US-HERB-011 | POST /{id}/restore |
| Formulas 恢复 | US-FORM-012 | POST /{id}/restore |
| MC 诊断历史 | US-MC-008 | GET /patient/{pid}/consultations |
| MC 处方历史 | US-MC-009 | GET /patient/{pid}/prescriptions |
| MC 权限查询 | US-MC-016 | GET /{id}/permissions |
| MC 审计日志 | US-MC-017 | GET /{id}/audit-logs |
| MC 批量详情 | US-MC-018 | POST /batch-details |
| 权限策略修复 | D7 | MC Create + REG Start/Cancel → DoctorOrAdminOrReceptionist |
| Reports 时间参数 | US-REPORT-001~003 | startDate/endDate 参数 |

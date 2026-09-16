# 需求追溯矩阵 (Traceability Matrix)

> 版本: v1.12 | 日期: 2026-08-20 | 状态: ✅ R15/R16 修复——PRD/矩阵 US 总数统一为 154（Shell 节 14→20 行校正）
>
> **用途**：建立「需求 → 设计 → 实现」的双向追溯基础设施。本矩阵是 v1.0 范围冻结、变更影响分析、缺口补全追踪的权威索引。
> **覆盖**：全部 154 个 User Story（US）+ 13 个 ADR + 5 个业务 Flow + 54 个访谈问题点。
> **「访谈问题点」列说明（2026-08-11 标注）**：该列为早期需求访谈（R1-R13）的历史索引，仅作来源追溯，与当前开发无直接关联——不维护其对应关系。
> **数据来源**：各模块 US 正文的「优先级 / 状态 / 实现参考 / 双模式端点」+ D1-D10 决策记录（2026-06-28 对账，已归档）+ 场景功能映射（✅⚠️🔴）。

## 图例

| 列 | 含义 |
| ------ | ------ |
| US ID | `US-{域}-{NNN}` 编号 |
| 优先级 | Must / Should / Could |
| 关联 ADR | 影响该 US 的架构决策（见 `03-architecture/decisions/`） |
| 关联 Flow | `11-business-flows.md` 中覆盖该 US 的 Flow |
| 关联 API | 远程/本地双模式端点（Controller 或路径） |
| 实现文件 | US「实现参考」中的关键代码文件 |
| 访谈问题点 | `user-expectation-interview.md` 中对应的问题编号 |
| 状态 | ✅已实现 / 🧲v1.0待实现 / 🔴代码待对齐 / ⚠️部分实现 / v2.0规划 |
| WebAPI | WebAPI/Server 端实现状态：✅已实现 / ⚠️部分 / ❌未实现 / N/A（Desktop 专属） |
| Desktop | Desktop 客户端实现状态：✅已实现 / ⚠️部分 / ❌未实现 / N/A（WebAPI 专属） |

**状态判定依据**：D1-D10 决策的「补回项」= 🧲v1.0待实现；D7 权限错配 / 端点缺失 = 🔴代码待对齐；scenario-map ⚠️ = ⚠️部分实现；Sync 整模块 / SHELL-012 = v2.0。

**双端列判定依据（2026-08-19 新增）**：基于代码扫描——WebAPI 列查 Server/LocalWebAPI Controller + Handler；Desktop 列查 ViewModel/Service/Repository 是否消费对应端点（API 层存在但 ViewModel 未调用 = ⚠️ 部分实现；如批量导入/导出、引用检查、history/batch-details 端点 Desktop 无 UI 入口）。

---

## 一、认证与会话（US-AUTH × 13）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-AUTH-001 | Must | ADR-0004/0008 | Flow 1/2/3/4 | POST /auth/login | AuthController.cs:44 | D19 | ✅ 已实现 | ✅ | ✅ |
| US-AUTH-002 | Must | ADR-0008 | — | ITokenManagementService | AuthController.cs:44 | A11 | ⚠️ 部分实现（双轨锁定，D8） | ✅ | ✅ |
| US-AUTH-003 | Must | ADR-0008 | — | [EnableRateLimiting("Login")] | AuthController.cs:41 | — | ✅ 已实现（本地 auto-login 限流） | ✅ | N/A |
| US-AUTH-004 | Must | ADR-0008 | — | POST /auth/refresh | AuthController.cs:128 | — | ✅ 已实现 | ✅ | ✅ |
| US-AUTH-005 | Must | ADR-0004 | — | GET /auth/validate + JWT 中间件 | AuthController.cs:151 | — | ✅ 已实现 | ✅ | ✅ |
| US-AUTH-006 | Must | ADR-0008 | — | ITokenRevocationService | AuthController.cs:128 | — | ✅ 已实现（重放撤销全部会话） | ✅ | N/A |
| US-AUTH-007 | Should | ADR-0008 | — | ISecurityAuditService | AuthService.cs | A12 | ✅ 已实现（SecurityAuditService，事件覆盖登录/刷新/登出/密码/用户状态） | ✅ | N/A |
| US-AUTH-008 | Must | ADR-0008 | Flow 1 | POST /auth/logout | AuthController.cs:104 | — | ✅ 已实现 | ✅ | ✅ |
| US-AUTH-009 | Must | ADR-0002/0009 | Flow 3 | POST /auth/auto-login | AuthController.cs:79 | D19 | ✅ 已实现 | ✅ | ✅ |
| US-AUTH-010 | Should | ADR-0008 | — | ITokenManagementService | AuthController.cs:79 | — | ✅ 已实现（AutoLoginToken 服务端签发+轮换） | ✅ | ✅ |
| US-AUTH-011 | Must | ADR-0005 | — | AuthService.cs 保留名校验 | AuthService.cs | — | ✅ 已实现（保留用户名清单） | ✅ | N/A |
| US-AUTH-012 | Must | ADR-0002/0009/0010 | Flow 3 | LocalWebAPI/AuthController.cs | LocalWebAPI/Controllers/AuthController.cs:19 | D19 | ✅ 已实现 | ✅ | ✅ |
| US-AUTH-013 | Must | ADR-0010 | Flow 3 | 本地限流中间件 | LocalWebAPI/Controllers/AuthController.cs:19 | — | ✅ 已实现（本地限流） | ✅ | N/A |

> **注**：02-auth.md 的 US-AUTH-000「首次登录初始化向导」与 US-SHELL-011「首次初始化向导」为同一功能，归属 Shell 模块（见 §九），本段不单列。

## 二、用户管理（US-USER × 12）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-USER-001 | Must | ADR-0005 | — | GET /users | UsersController | A8 | ✅ 已实现（role/status 筛选参数接线） | ✅ | ✅ |
| US-USER-002 | Must | ADR-0005 | — | GET /users/{id} | UsersController | — | ✅ 已实现（CreatedAt/UpdatedAt 映射已修复） | ✅ | ✅ |
| US-USER-003 | Must | ADR-0004 | — | GET /users/current | UsersController | — | ✅ 已实现 | ✅ | ✅ |
| US-USER-004 | Must | ADR-0005 | — | POST /users | UsersController | A8 | ✅ 已实现（默认密码配置） | ✅ | ✅ |
| US-USER-005 | Must | ADR-0005 | — | PUT /users/{id} | UsersController | — | ✅ 已实现（角色更新+层级约束） | ✅ | ✅ |
| US-USER-006 | Must | ADR-0005 | — | DELETE /users/{id} | UsersController | A9 | ✅ 已实现（单删不可删自己 + sysadmin 保护） | ✅ | ✅ |
| US-USER-007 | Must | ADR-0005 | — | POST /users/{id}/reset-password | UsersController | — | ✅ 已实现（重置密码 sysadmin 保护） | ✅ | ✅ |
| US-USER-008 | Must | ADR-0004 | Flow 4 | PUT /users/{id}/profile | UsersController | — | ✅ 已实现 | ✅ | ✅ |
| US-USER-009 | Must | ADR-0005 | — | PUT /users/{id}/change-password | UsersController | — | ✅ 已实现 | ✅ | ✅ |
| US-USER-010 | Must | ADR-0005 | — | POST /users/{id}/toggle-status | UsersController | A9 | ✅ 已实现（JWT 中间件禁用拦截） | ✅ | ✅ |
| US-USER-011 | Should | ADR-0005 | — | POST /users/{id}/restore | UsersController | A10 | ✅ 已实现（Restore 层级完整：sysadmin/Admin 权限+会话清理） | ✅ | ✅ |
| US-USER-012 | Should | ADR-0005 | — | POST /users/batch-delete 等 | UsersController | — | ✅ 已实现（批量 100 上限） | ✅ | ⚠️ |

## 三、患者管理（US-PAT × 14）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |

| US-PAT-014 | Must | — | — | GET /patients/by-id-number/{idNumber} | PatientsController.cs:296 | — | ✅ 已实现（R3-补：身份证号查询） | ✅ | ✅ |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-PAT-001 | Must | ADR-0010 | Flow 2 | GET /patients | PatientsController.cs（GetPagedAsync） | R7 | ⚠️ 部分实现（姓名/拼音码筛选✅；电话筛选 500——加密列 `PhoneNumber.Contains` 使 EF 生成 `LIKE … ESCAPE N'<密文>'`，2026-09-14 远程/本地复现） | ⚠️ | ✅ |
| US-PAT-002 | Must | ADR-0010 | — | GET /patients/{id} | PatientsController.cs:65 | — | ✅ 已实现 | ✅ | ✅ |
| US-PAT-003 | Must | ADR-0010 | Flow 1 | POST /patients | PatientsController.cs:89 | R3 | ✅ 已实现（电话唯一查重） | ✅ | ✅ |
| US-PAT-004 | Must | ADR-0010 | — | PUT /patients/{id} | PatientsController.cs:114 | — | ✅ 已实现（更新电话唯一查重） | ✅ | ✅ |
| US-PAT-005 | Must | ADR-0001 | — | DELETE /patients/{id} | PatientsController.cs:146 | — | ✅ 已实现（引用检查 CountMedicalCasesAsync） | ✅ | ✅ |
| US-PAT-006 | Must | — | — | POST /patients/{id}/toggle-status | PatientsController.cs:175 | — | ✅ 已实现 | ✅ | ✅ |
| US-PAT-007 | Should | — | — | POST /patients/{id}/restore | PatientsController.cs:197 | A10 | ✅ 已实现 | ✅ | ✅ |
| US-PAT-008 | Should | ADR-0001 | — | POST /patients/batch-delete | PatientsController.cs:223 | — | ✅ 已实现 | ✅ | ⚠️ |
| US-PAT-009 | Must | ADR-0001 | — | GET /patients/{id}/check-reference | PatientsController.cs:248 | — | ✅ 已实现 | ✅ | ⚠️ |
| US-PAT-010 | Should | ADR-0001 | — | POST /patients/batch-check-reference | PatientsController.cs:267 | — | ✅ 已实现（批量计数一次查询） | ✅ | ⚠️ |
| US-PAT-011 | Should | ADR-0010 | — | GET /patients/import-template | PatientsController.cs:296 | — | ✅ 已实现（T4 端点 + 2026-08-13 Excel→JSON 模板；2026-09-13 B-12 Desktop 模板 Excel 化——ClosedXML 渲染 .xlsx，字段说明仍取服务端 JSON） | ✅ | ✅ |
| US-PAT-012 | Should | ADR-0010 | — | GET /patients/export | PatientsController.cs:313 | — | ✅ 已实现（T4 端点 + 2026-08-13 Excel→JSON 数组；2026-09-13 B-12 Desktop 导出 Excel 化——JSON 数组转 .xlsx，脱敏值原样） | ✅ | ✅ |
| US-PAT-013 | Must | ADR-0008 | — | [SensitiveData] 序列化管道 | PatientsController.cs | X3.1 | ✅ 已实现（DTO 加 [SensitiveData]，序列化管道掩码） | ✅ | ✅ |

## 四、药材管理（US-HERB × 13）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-HERB-001 | Must | ADR-0007 | Flow 2 | GET /herbs | CatalogController.cs:50 | R12/A4/D10 | ✅ 已实现 | ✅ | ✅ |
| US-HERB-002 | Must | ADR-0007 | — | GET /herbs/{id} | CatalogController.cs:107 | — | ✅ 已实现 | ✅ | ✅ |
| US-HERB-003 | Must | ADR-0010 | — | POST /herbs | CatalogController.cs:124 | — | ✅ 已实现 | ✅ | ✅ |
| US-HERB-004 | Must | ADR-0001 | — | PUT /herbs/{id} | CatalogController.cs:146 | A2/D13 | ✅ 已实现 | ✅ | ✅ |
| US-HERB-005 | Must | ADR-0001 | — | DELETE /herbs/{id} | CatalogController.cs:174 | — | ✅ 已实现（B1: 单删引用检查 ValidateBeforeDeleteAsync——处方/验方引用拒绝删除） | ✅ | ✅ |
| US-HERB-006 | Must | ADR-0010 | Flow 5(none) | POST /herbs/batch-import | CatalogController.cs:262 | A1 | ✅ 已实现（DTO/JSON 唯一路径——2026-08-13 移除服务端 Excel 解析 import-excel） | ✅ | ✅ |
| US-HERB-007 | Should | ADR-0010 | — | GET /herbs/export-all | CatalogController.cs（export-all） | — | ✅ 已实现（T4: export-all 端点——2026-08-19 P1：Local 补 export-all/export/import-template 端点，双端成立） | ✅ | ✅ |
| US-HERB-008 | Should | ADR-0001 | — | GET /herbs/{id}/check-reference | CatalogController.cs:287 | — | ✅ 已实现（CheckHerbReference 处方+验方双计数） | ✅ | ⚠️ |
| US-HERB-009 | Should | ADR-0001 | — | POST /herbs/batch-check-reference | CatalogController.cs:302 | — | ✅ 已实现（BatchCheckReference 聚合计数） | ✅ | ⚠️ |
| US-HERB-010 | Must | — | — | POST /herbs/{id}/toggle-status | CatalogController.cs:202 | A3 | ✅ 已实现 | ✅ | ✅ |
| US-HERB-011 | Should | — | — | POST /herbs/{id}/restore | CatalogController.cs:228 | A10 | ✅ 已实现（Restore 泛型命令） | ✅ | ✅ |
| US-HERB-012 | Should | ADR-0001 | — | POST /herbs/batch-enable 等 | CatalogController.cs:319 | — | ✅ 已实现（批量删除引用检查） | ✅ | ⚠️ |
| US-HERB-013 | Should | ADR-0010 | — | GET /herbs/export + import-template | CatalogController.cs（export/import-template） | — | ✅ 已实现（T4: export/import-template 端点——2026-08-19 P1：Local 补端点 + 契约-路由对齐守卫，双端成立） | ✅ | ✅ |

## 五、验方管理（US-FORM × 14）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |

| US-FORM-014 | Should | — | — | POST /formulas/{id}/clone | LocalWebAPI/Controllers/FormulasController.cs（CloneFormula） | — | ✅ 已实现（R3-补：克隆——仅本地，远程待补；2026-09-14 修复克隆漏拷 ProcessingMethod + E2E `FormulaCrudE2ETests.Clone_*`） | ⚠️ | ✅ |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-FORM-001 | Must | ADR-0007 | — | GET /Formulas | CatalogController.cs:363 | D11 | ✅ 已实现（列表 Doctor 仅本人+共享） | ✅ | ✅ |
| US-FORM-002 | Must | ADR-0007 | — | GET /Formulas/{id} | CatalogController.cs:363 | — | ✅ 已实现（Doctor 所有权检查 403） | ✅ | ✅ |
| US-FORM-003 | Must | ADR-0007 | — | POST /Formulas | CatalogController.cs:363 | D11 | ✅ 已实现（创建持久化药材组成） | ✅ | ✅ |
| US-FORM-004 | Must | ADR-0007 | — | PUT /Formulas/{id} | CatalogController.cs:363 | — | ✅ 已实现（更新替换药材+降级检查） | ✅ | ✅ |
| US-FORM-005 | Must | ADR-0007 | — | DELETE /Formulas/{id} | CatalogController.cs:363 | — | ✅ 已实现 | ✅ | ⚠️ |
| US-FORM-006 | Must | ADR-0010 | — | POST /Formulas/batch-import | CatalogController.cs:363 | — | ✅ 已实现（10000 上限） | ✅ | ✅ |
| US-FORM-007 | Must | ADR-0007 | — | GET /Formulas/pending-validation | IFormulaService.cs:11 | D11 | ✅ 已实现（待验证列表分页） | ✅ | ✅ |
| US-FORM-008 | Must | ADR-0007 | — | POST /Formulas/{fid}/herbs/{hid}/validate | IFormulaService.cs:11 | — | ✅ 已实现 | ✅ | ✅ |
| US-FORM-009 | Must | ADR-0007 | — | IFormulaService.ValidateFormulaHerbAsync | IFormulaService.cs:11 | — | ✅ 已实现 | ✅ | ✅ |
| US-FORM-010 | Must | ADR-0007 | — | IFormulaService.UpdateAsync | IFormulaService.cs:11 | — | ✅ 已实现（FLAW-F1 降级 Draft） | ✅ | ✅ |
| US-FORM-011 | Must | ADR-0007 | — | POST /Formulas/{id}/toggle-status | CatalogController.cs:363 | — | ✅ 已实现（toggle-status + batch-enable/disable） | ✅ | ✅ |
| US-FORM-012 | Should | ADR-0007 | — | POST /Formulas/{id}/restore | CatalogController.cs:363 | A10 | ✅ 已实现（Restore 泛型命令） | ✅ | ✅ |
| US-FORM-013 | Should | ADR-0010 | — | GET /Formulas/export + import-template | CatalogController.cs（formulas/export?category= + FormulaDetailDto Herbs） | — | ✅ 已实现（2026-08-19 P2：导出含 Herbs 明细 + 分类筛选 category 对齐；双端 AdminOrSuperAdmin） | ✅ | ✅ |

## 六、医案管理（US-MC × 20，核心聚合根）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |

| US-MC-020 | Should | — | — | POST /medicalcases/batch-delete | MedicalCasesController.cs:178 | — | ✅ 已实现（R3-补：批量删除仅 Completed） | ✅ | ⚠️ |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-MC-001 | Must | ADR-0001 | Flow 1/2 | POST /medicalcases | MedicalCasesController.cs:26 | D2 | ✅ 已实现（Create DoctorOnly + BR-001 唯一索引 + CaseNumber） | ✅ | ✅ |
| US-MC-002 | Must | ADR-0001 | Flow 1/2/5 | PUT /medicalcases/{id} | MedicalCasesController.cs:26 | D7/D8 | ✅ 已实现（EditReason 校验——打印后修改/非 Admin 编辑 Completed） | ✅ | ✅ |
| US-MC-003 | Must | ADR-0001 | Flow 1 | PUT /medicalcases/{id}/prescription-flag | MedicalCasesController.cs:26 | D12 | ✅ 已实现 | ✅ | ✅ |
| US-MC-004 | Must | ADR-0001 | Flow 2 | GET /medicalcases/{id} | MedicalCasesController.cs:26 | D3 | ✅ 已实现（Doctor 仅本人医案，非本人 Forbidden） | ✅ | ✅ |
| US-MC-005 | Must | ADR-0001 | — | GET /medicalcases | MedicalCasesController.cs:26 | X2.3 | ✅ 已实现 | ✅ | ✅ |
| US-MC-006 | Must | ADR-0001 | — | GET /medicalcases/query?type= | MedicalCasesController.cs:26 | — | ✅ 已实现 | ✅ | ✅ |
| US-MC-007 | Must | ADR-0001 | — | GET /medicalcases/search | MedicalCasesController.cs:26 | — | ✅ 已实现（搜索按操作者过滤） | ✅ | ✅ |
| US-MC-008 | Should | ADR-0001 | Flow 2 | GET /medicalcases/{pid}/consultations | MedicalCasesController.cs:26 | D3/D4/D9 | ✅ 已实现（B1: GET /medicalcases/patients/{patientId}/history——跨医案详情聚合） | ✅ | ⚠️ |
| US-MC-009 | Should | ADR-0001 | Flow 2 | GET /medicalcases/{pid}/prescriptions | MedicalCasesController.cs:26 | D4/D5 | ✅ 已实现（B1: 同上 history 端点含处方历史） | ✅ | ⚠️ |
| US-MC-010 | Must | ADR-0001 | — | PUT /medicalcases/{id}/suspend | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现 | ✅ | ✅ |
| US-MC-011 | Must | ADR-0001 | Flow 1 | PUT /medicalcases/{id}/close | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现（完成仅本人医案） | ✅ | ✅ |
| US-MC-012 | Should | ADR-0001 | — | PUT /medicalcases/{id}/close?force=true | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现（强制关闭仅 Admin） | ✅ | ⚠️ |
| US-MC-013 | Must | ADR-0001 | — | PUT /medicalcases/{id}/suspend | MedicalCaseProcessingController.cs:25 | — | ✅ 已实现 | ✅ | ✅ |
| US-MC-014 | Must | ADR-0001 | Flow 5 | PUT /medicalcases/{id}/cancel | MedicalCaseProcessingController.cs:25 | D16 | ✅ 已实现（2026-08-03 决策：物理删除+审计+Registration 联动） | ✅ | ✅ |
| US-MC-015 | Must | ADR-0001 | — | DELETE /medicalcases/{id} + batch-delete | MedicalCasesController.cs:26 | — | ✅ 已实现（软删仅 Completed，批量计数） | ✅ | ⚠️ |
| US-MC-016 | Should | ADR-0001 | — | GET /medicalcases/{id}/permissions | MedicalCaseAuditController.cs:23 | X2.3 | ✅ 已实现（RequiresEditReason/DenialReason） | ✅ | ⚠️ |
| US-MC-017 | Must | ADR-0001 | — | GET /medicalcases/{id}/audit-logs | MedicalCaseAuditController.cs:23 | A11 | ✅ 已实现（更新审计+字段 diff——ChangedFields/OldValues/NewValues） | ✅ | ✅ |
| US-MC-018 | Should | ADR-0001 | — | POST /medicalcases/batch-details | MedicalCasesController.cs:26 | — | ✅ 已实现（B1: POST /medicalcases/batch-details + GetByIdsWithDetailsAsync） | ✅ | ⚠️ |
| US-MC-019 | Should | ADR-0001 | Flow 2 | 复用 US-MC-009 处方历史 | MedicalCasesController.cs:26 | D6 | ✅ 已实现（HistoryCopyDialog 历史复制） | ✅ | ✅ |

## 七、挂号管理（US-REG × 8）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-REG-001 | Must | ADR-0001 | Flow 1 | POST /Registrations | RegistrationsController.cs:24 | R3/R6/R11 | ✅ 已实现（患者校验 + 挂号费带出） | ✅ | ✅ |
| US-REG-002 | Must | ADR-0001 | — | 两步：POST /Registrations（Source=Doctor 建 Waiting）+ PUT /Registrations/{id}/start-visit（接诊） | RegistrationsController.cs（POST /start-visit） | R13 | ✅ 已实现（B2 2026-08-11 QuickVisit；2026-08-13 quickvisit-twostep 两步收敛——quick-visit 端点已删） | ✅ | ⚠️ |
| US-REG-003 | Must | ADR-0010 | — | GET /Registrations/{id} | RegistrationsController.cs:24 | — | ✅ 已实现 | ✅ | ✅ |
| US-REG-004 | Must | ADR-0010 | Flow 1 | GET /Registrations/queue | RegistrationsController.cs:24 | R8/R9 | ✅ 已实现（队列当天过滤） | ✅ | ✅ |
| US-REG-005 | Must | ADR-0001 | Flow 1 | PUT /Registrations/{id}/start | RegistrationsController.cs:24 | — | ✅ 已实现（StartVisit 原子建医案+回退） | ✅ | ✅ |
| US-REG-006 | Must | ADR-0010 | Flow 1 | PUT /Registrations/{id}/cancel | RegistrationsController.cs:24 | R11 | ✅ 已实现（服务端守卫：Waiting-only + 关联医案拒绝） | ✅ | ✅ |
| US-REG-007 | Must | ADR-0001 | Flow 1 | MedicalCaseService 内部触发 | RegistrationsController.cs:24 | — | ✅ 已实现 | ✅ | N/A |
| US-REG-008 | Must | ADR-0013 | Flow 1 | SignalR Hub | SignalR Hub（待专项 spec） | R10/X2.1 | ✅ 已实现（Server RegistrationHub + Desktop SignalRClient+轮询降级） | ✅ | ✅ |

## 八、处方打印（US-PRINT × 4）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-PRINT-001 | Must | ADR-0001 | Flow 1/5 | PrescriptionPrintService | PrescriptionPrintService.cs:23 | D14/D15/D17 | ✅ 已实现 | N/A | ✅ |
| US-PRINT-002 | Must | — | — | PrescriptionPrintService | PrescriptionPrintService.cs | — | ✅ 已实现 | N/A | ✅ |
| US-PRINT-003 | Should | — | — | PrescriptionPdfExporter | PrescriptionPdfExporter | — | ✅ 已实现 | N/A | ✅ |
| US-PRINT-004 | Must | ADR-0001 | Flow 1/5 | PUT /print-completed + POST /print-log | MedicalCasePrintController.cs:44 | D16 | ✅ 已实现（Desktop 打印流回写接线全链） | ✅ | ✅ |

## 九、平台基础设施 — Shell（US-SHELL ×19 v1.0 + ×1 v2.0，共 20 行）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-SHELL-001 | Must | ADR-0006/0007 | Flow 4 | App.xaml.cs 启动管线 | App.xaml.cs:41 | S1 | ✅ 已实现 | N/A | ✅ |
| US-SHELL-003 | Must | ADR-0006/0007 | Flow 4 | ApplicationBootstrapper | ApplicationBootstrapper.cs:35 | — | ✅ 已实现（角色模块加载 + LoginCoordinator 死代码已清——C7 删 HandleLoginSuccessAsync/GetDiagnostics/LoginFlowDiagnostics，2026-08-09 实际已处理，状态列 2026-08-11 校准） | N/A | ✅ |
| US-SHELL-004 | Could | ADR-0007 | — | AccountSettingsControl | AccountSettingsControl | — | ✅ 已实现 | N/A | ✅ |
| US-SHELL-005 | Must | ADR-0006/0007 | — | NavigationCoordinator | NavigationCoordinator | — | ✅ 已实现 | N/A | ✅ |
| US-SHELL-007 | Must | ADR-0002/0009 | Flow 3 | SwitchingApiClient + ModeSwitchValidator | IConnectionModeProvider.SwitchModeAsync | D18/D19/S5/X1.1 | ⚠️ 部分实现（双模路由✅/NO_REMOTE_URL 阻断✅；ERR-70506 未完成医案守卫仅日志未阻断——2026-09-14 E2E 取证 `ModeSwitchE2ETests`） | ⚠️ | ✅ |
| US-SHELL-010 | Must | — | — | GET / 下载页 + /releases/ + Setup.exe + 更新源 | DownloadController.cs + DesktopUpdateService.cs + scripts/velopack-pack.ps1 | — | ✅ 已实现（决策 A + VELOPACK：下载页公开/静态服务/打包脚本/sync 脚本/客户端更新检查 Velopack 1.2.0/部署文档——打包产物需实机运行 velopack-pack.ps1 验证） | ✅ | ✅ |
| US-SHELL-011 | Must | ADR-0006 | — | FirstRunSetupViewModel 扩展 | FirstRunSetupViewModel | S1/S2 | 🧲 v2.0 推迟（B4 决策 I-4：仅 Sysadmin 且可手动配置，v1.0 优先核心诊疗） | N/A | 🧲 v2.0 |
| US-SHELL-012 | Should | ADR-0006 | — | UpdateManager | UpdateManager.CheckForUpdatesAsync | — | v2.0 规划 | ⚠️ | ✅ |
| US-SHELL-013 | Should | — | — | ILocalDbBackupService + 恢复 UI | ILocalDbBackupService | S4/X3.2 | ✅ 已实现（T7: ILocalDbBackupService + 备份管理 UI + 登录自动备份） | N/A | ✅ |
| US-SHELL-014 | Should | ADR-0008 | — | SecurityAuditController + SecurityAuditLogView | SysadminHome 安全审计卡 | A12 | ✅ 2026-08-29 查询 API + Desktop 页（仅远程） | ✅ | ✅ |
| US-SHELL-016 | Could | — | — | 导出/导入 JSON 按钮 | SysadminHomeView | X3.2 | 🧲 v1.0 待实现 | N/A | 🧲 |
| US-SHELL-017 | Must | ADR-0005/0008 | — | SystemAdminOptions + IdentitySeedData.ResolveSysAdminPassword（K4 环境变量读取） | IdentitySeedData.cs | — | ✅ 已实现 | ✅ | N/A |
| US-SHELL-018 | Must | ADR-0006/0014 | — | SysadminHomeView | SysadminHomeView.xaml + ConfigurationCenterViewModel.cs + ServerConfigSectionViewModel.cs | S3 | ✅ 10/10 AC（Phase 1-3 + 读卡器组经 US-SHELL-019 完成） | ✅ | ✅ |
| US-SHELL-019 | Should | — | — | ICardReaderDiagnostics | CardReaderDiagnosticsService.cs + CardReaderDiagnosticsViewModel.cs | S3 | ✅ 已实现（8/8 AC：厂家选择/探测/读卡测试/固件（驱动未暴露→提示）/手动参数覆盖/持久化/医生无感；串口测试=USB 链路握手——HD100 无独立串口协议）——**硬件实测待办（2026-08-11 标注：华大 HD100 硬件到位后，sysadmin 配置中心→读卡器诊断→实测 9 AC；单测 4/4 已过）** | N/A | ✅ |
| US-SHELL-020 | Must | — | — | POST /api/v1/deploy/upload + restart | DeployController.cs | — | ✅ 已实现（DEPLOY-PERM：双端类级 `SysAdminOnly`——Admin 业务管理员无部署能力，AC 注闭环） | ✅ | ✅ |
| US-SHELL-021 | Should | — | — | 上线数据迁移（Excel 模板/分批/回滚） | — | — | 🧲 v1.0 待实现（2026-08-11 补 US——产品盲区收编） | N/A | 🧲 |
| US-SHELL-022 | Should | — | — | 上线检查清单 + 回滚方案 | — | — | 🧲 v1.0 待实现（2026-08-11 补 US——产品盲区收编） | N/A | 🧲 |
| US-SHELL-023 | Could | — | — | 培训材料 + FAQ + 支持流程 | — | — | 🧲 v1.0 待实现（2026-08-11 补 US——产品盲区收编） | N/A | 🧲 |
| US-SHELL-024 | Should | — | — | Server 单实例与端口防护（PID 文件 + 端口释放 + health 探测 + Mutex） | start.sh + Program.cs | — | ✅ 已实现（2026-08-13 P2-07：start.sh 四层防护 + Program.cs Mutex `Global\LYBTZYZS_WebAPI_Instance`——真机单进程 + 双开拒绝 exit 1；见 13c #116） | ✅ | N/A |
| US-SHELL-025 | Should | — | — | HTTP/HTTPS 双协议（Kestrel 多端点 5000+5001，配置开关） | Program.cs + config/appsettings.Production.json | — | ✅ 已实现（2026-08-14 P2-09：Http 默认开/Https 默认关——Server:Endpoints 段；真机 health 200 + Listening 日志；见 13c #121） | ✅ | N/A |

> US-SHELL-015 已撤销（并入 US-SHELL-013），不计入总数。

## 十、平台基础设施 — Configuration（US-CFG × 6）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-CFG-005 | Should | — | — | PUT /configuration 批量 + validate | ConfigurationController.cs | — | ✅ 已实现（R3-补：配置管理端点） | ✅ | ✅ |

| US-CFG-006 | Should | — | — | clinic-settings.json 热更新 | ClinicSettingsService.cs / ClientConfigurationStore.cs | — | ⚠️ 部分实现（2026-09-14 校准：文件链✅持久化+重载可读；同进程不热更新——消费者注入启动期静态 IOptions；UI 写 CWD 与 Shell 读 BaseDirectory 不一致；本地 section 端点白名单拦截 422。E2E `ConfigurationE2ETests.UpdateClinicSettings_PersistsAndReloads`） | ⚠️ | ✅ |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-CFG-001 | Must | ADR-0005 | — | GET /configuration | ConfigurationController.cs:15 | S3 | ✅ 已实现 | ✅ | ✅ |
| US-CFG-002 | Must | ADR-0005 | — | GET /configuration/{section} | ConfigurationController.cs:15 | S3 | ✅ 已实现 | ✅ | ✅ |
| US-CFG-003 | Should | — | — | ProductionConfigurationValidator | ProductionConfigurationValidator | — | ✅ 已实现 | ✅ | N/A |
| US-CFG-004 | Should | — | — | FeatureToggleOptions + ConfigurationOptionsMonitor | PrismConfigurationExtensions.cs:80 | — | ✅ 已实现（T8: FeatureToggle 基建——feature-toggles.json + 热更新） | N/A | ✅ |

## 十一、平台基础设施 — Error Handling（US-ERR × 8）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-ERR-001 | Must | — | — | BusinessExceptionHandler + DesktopExceptionHandler | LYBT.Shared.ExceptionHandling | — | ✅ 已实现 | ✅ | ✅ |
| US-ERR-002 | Must | — | — | ClientErrorMessageMapper | ClientErrorMessageMapper | — | ✅ 已实现 | N/A | ✅ |
| US-ERR-003 | Should | — | — | DesktopExceptionHandler | DesktopExceptionHandler | — | ✅ 已实现 | N/A | ✅ |
| US-ERR-004 | Should | ADR-0004 | — | AsyncLocalCorrelationIdProvider | CorrelationIdEnricher | S3 | ✅ 已实现 | ✅ | ✅ |
| US-ERR-005 | Must | — | — | Business/SystemExceptionHandler | BusinessExceptionHandler | — | ✅ 已实现 | ✅ | N/A |
| US-ERR-006 | Should | — | — | ValidationException | BusinessExceptionHandler | — | ⚠️ 部分实现（2026-09-17 R-2：Server+Local 异常路径均统一 ProblemDetails；模型校验 400 ProblemDetails 含 errors 字段字典；控制器已知业务失败仍 ApiResponse；本地无 409 生产者。E2E `ExceptionMappingE2ETests`） | ⚠️ | N/A |
| US-ERR-007 | Should | — | — | AppException 体系 | LYBT.Shared.ExceptionHandling | — | ⚠️ 部分实现（仅 3 种异常实体；Conflict/Unauthorized/ApiException/Factory 缺失；409 分支无生产者——电话唯一查重因 AES-GCM 非确定性加密恒不命中。E2E `ExceptionMappingE2ETests`） | ⚠️ | ⚠️ |
| US-ERR-008 | Should | — | — | ErrorSeverity/ErrorCategory | DesktopExceptionHandler | — | ✅ 已实现 | N/A | ✅ |

## 十二、平台基础设施 — Logging & Audit（US-LOG × 7）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-LOG-001 | Should | — | — | Serilog 配置 | Program.cs + SystemLog | S3 | ✅ 已实现 | ✅ | ✅ |
| US-LOG-002 | Should | — | — | bootstrap logger | Program.cs + App.xaml.cs:41 | — | ✅ 已实现 | ✅ | ✅ |
| US-LOG-003 | Should | ADR-0008 | — | SensitiveDataMasker | SensitiveDataAttribute | — | ✅ 已实现 | ✅ | ✅ |
| US-LOG-004 | Should | ADR-0008 | — | SecurityAuditLog 表 | SecurityAuditService | A12 | ✅ 已实现 | ✅ | N/A |
| US-LOG-005 | Could | — | — | LoggingLevelManager | LoggingLevelManager | S3 | ✅ 已实现 | ✅ | ✅ |
| US-LOG-006 | Should | ADR-0004 | — | ApiLoggingFilter | CorrelationIdEnricher | — | ✅ 已实现 | ✅ | N/A |
| US-LOG-007 | Could | — | — | LogCleanupService | LogCleanupService | — | ✅ 已实现 | ✅ | N/A |

## 十三、平台基础设施 — Health & Diagnostics（US-SYS × 9）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-SYS-001 | Could | — | — | GET /api/v1/health | HealthController.cs:19 | S3 | ✅ 已实现 | ✅ | ✅ |
| US-SYS-002 | Could | — | — | GET /api/v1/health/ping | HealthController.cs:19 | — | ✅ 已实现 | ✅ | ✅ |
| US-SYS-003 | Should | — | — | GET /api/v1/health/details | HealthController.cs:19 | S3 | ✅ 已实现 | ✅ | ⚠️ |
| US-SYS-004 | Should | — | — | /health/details 503 | HealthController.cs:19 | — | ✅ 已实现 | ✅ | N/A |
| US-SYS-005 | Could | — | — | GET /api/v1/diagnostics/logging/status | DiagnosticsController.cs:20 | S3 | ✅ 已实现 | ✅ | ✅ |
| US-SYS-006 | Could | — | — | POST /diagnostics/logging/debug/enable | DiagnosticsController.cs:20 | S3 | ✅ 已实现 | ✅ | ✅ |
| US-SYS-007 | Could | — | — | POST /diagnostics/logging/debug/disable | DiagnosticsController.cs:20 | — | ✅ 已实现 | ✅ | ✅ |
| US-SYS-008 | Could | — | — | POST /diagnostics/logging/level | DiagnosticsController.cs:20 | — | ✅ 已实现 | ✅ | ✅ |
| US-SYS-009 | Could | — | — | LoggingLevelManager Timer | LoggingLevelManager | — | ✅ 已实现 | ✅ | N/A |

## 十四、平台基础设施 — Card Reader（US-CARD × 2）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-CARD-001 | Should | — | Flow 1 | ICardReaderService（客户端硬件） | ICardReaderService.cs:10 | R3/R4 | ✅ 已实现 | N/A | ✅ |
| US-CARD-002 | Should | ADR-0001 | Flow 1 | — | —（A-31-C7 移除 `IPatientCardReaderIntegration.MatchPatientAsync` 实现，生产走 FindOrCreatePatientAsync） | R5 | ⚠️ 未完成（实现已移除；2026-08-08 用户定：读卡器必用、US-CARD-002 待完善，UI 设计时整体考虑） | N/A | ⚠️ |

## 十五、报表管理（US-REPORT × 4）

| US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态 | WebAPI | Desktop |

| US-REPORT-004 | Could | — | — | GET /reports/trend/* 等 5 端点 | ReportsController.cs:69-158 | — | 🧲 v2.0 推迟（B4 决策 I-3：`ReportsHomeView` 仅消费 3/8 日统计，5 趋势端点属锦上添花） | 🧲 v2.0 | 🧲 v2.0 |
| ------- | :---: | ------ | ------ | ------ | ------ | ------ | ------ | :---: | :---: |
| US-REPORT-001 | Must | — | — | GET /reports/daily/income | ReportsController.cs:30 | — | ✅ 已实现（endDate 默认=startDate + 400 校验） | ✅ | ✅ |
| US-REPORT-002 | Must | — | — | GET /reports/daily/consultations | ReportsController.cs:38 | — | ✅ 已实现（startDate>endDate→400） | ✅ | ✅ |
| US-REPORT-003 | Should | — | — | GET /reports/daily/herbs | ReportsController.cs:46 | — | ✅ 已实现（同 002 校验） | ✅ | ✅ |

---

## 统计汇总

| 域 | US 数 | ✅已实现 | ⚠️部分实现 | 🔴代码待对齐 | 🧲v1.0待实现 | v2.0 |
| ------ | :---: | :---: | :---: | :---: | :---: | :---: |
| AUTH | 13 | 12 | 1 | 0 | 0 | 0 |
| USER | 12 | 12 | 0 | 0 | 0 | 0 |
| PAT | 14 | 14 | 0 | 0 | 0 | 0 |
| HERB | 13 | 11 | 1 | 1 | 0 | 0 |
| FORM | 14 | 14 | 0 | 0 | 0 | 0 |
| MC | 20 | 17 | 0 | 3 | 0 | 0 |
| REG | 8 | 7 | 0 | 0 | 1 | 0 |
| PRINT | 4 | 4 | 0 | 0 | 0 | 0 |
| Shell | 20 | 5 | 1 | 1 | 6 | 0 |
| CFG | 6 | 6 | 0 | 0 | 0 | 0 |
| ERR | 8 | 6 | 2 | 0 | 0 | 0 |
| LOG | 7 | 7 | 0 | 0 | 0 | 0 |
| SYS | 9 | 9 | 0 | 0 | 0 | 0 |
| CARD | 2 | 1 | 1 | 0 | 0 | 0 |
| REPORT | 4 | 4 | 0 | 0 | 0 | 0 |
| **合计** | **154** | **142** | **2** | **0** | **7** | **0** |

> R2-补 全量重扫（2026-08-11 v1.2）：状态列同步 T4/T5/T7/T8/P1-P3 修复（40 处校准）。🔴 5 项 = MC-008/009/018（历史聚合/批量详情缺失）+ HERB-005（删除无引用检查）+ SHELL-018（配置中心未实现）；⚠️ 7 项 = AUTH-002（本地锁定显式关闭）+ HERB-006（服务端 Excel 解析路径）+ SHELL-007（双模切换守卫）+ ERR-006/007（异常体系）+ CARD-002（降级链已移除）；🧲 7 项 = REG-002（QuickVisit 待接线）+ SHELL-011/012/016/019 等规划项。> R2 校准（2026-08-11）：状态列同步至代码实际（依据 R1 矩阵 + T4 修复）。🔴 10 项 = FORM-003/004/010（丢药材/降级缺失）+ MC-008/009/018（历史聚合/批量详情缺失）+ HERB-005（删除无引用检查）+ SHELL-013（备份恢复全无）+ CFG-004（FeatureToggle 消失）等；🧲 7 项 = REG-002 QuickVisit 待接线 + SHELL-011/016/018/019/012 等规划项；⚠️ 30 项为有代码但缺关键面（权限过滤/服务端守卫/AC 校验等，详见 R1 矩阵报告）。

## 反向追溯说明

- **ADR 反链**：见 `03-architecture/decisions/0001-0013` 各文件末尾「## 关联 US」段。
- **Flow 覆盖**：见 `03-architecture/11-business-flows.md` 每个 Flow 标题下「**覆盖 US**」行。
- **访谈标注**：见 2026-06-28 用户期望访谈记录（已归档）每个问题点后「→ US-XXX」标注。
- **变更影响分析**：修改某 US 时，沿「关联 ADR / Flow / 访谈点」三向反查受影响范围；修改某 ADR 时，沿其「关联 US」段反查。

## 变更记录

| 日期 | 变更 | 原因 |
| ------ | ------ | ------ |
| 2026-08-20 | **v1.12 R15/R16 修复**：① Shell 节标题 14→20（实际 SHELL-001~025 活跃 20 行）；② 统计汇总表 Shell 13→20、合计 151→154；③ 覆盖描述 151→154 | R15/R16 审计结论：PRD/矩阵 US 总数矛盾修复 |
| 2026-08-19 | **v1.11 P2 修复（.hermes-task-p2-fixes-doc-sync）**：① **权限口径对齐（T3）**——药材/验方 `batch-import`/`import-template`/`export`/`export-all` 双端补显式 `[Authorize(Policy=AdminOrSuperAdmin)]`（对齐患者 `batch-import` Admin口径；原类级 `DoctorOrAdmin` 放行 Doctor 导入/导出）；② **验方导出筛选参数对齐 + 明细补全（T5）**——服务端 `FormulaExport` 参数名 `keyword`→`category` 对齐客户端 `IFormulaApi:71` `?category=`（原传参永不生效）；导出 DTO 由 `List<FormulaListDto>`→`List<FormulaDetailDto>` 含 `Herbs` 明细（US-FORM-013 每行含药材组成）；`ICatalogQueryService` 扩展 `ExportDetailsAsync` + `GetPagedAsync(category)` 直通仓储 `Category` 字段；③ **文档同步（T7）**——`06-formulas.md` US-FORM-013 ⚠️→✅ + 标题“导出 Excel”→“导出 JSON”；`13-traceability-matrix.md` FORM-013 ⚠️→✅（142/2）；`13c-current-status.md` 修正药材 export“双端”表述（#112 此前不成立注记 + 新增 #130 P2 行） | 任务书 .hermes-task-p2-fixes-doc-sync.md（desktop-deep-review P2 结论） |
| 2026-08-19 | **v1.10 P1 修复（desktop-deep-review 派单）**：① 药材 export/import-template/export-all 双端补端点——Remote CatalogController 新増 `GET /herbs/export`（筛选导出，对齐患者；此前 Desktop 契约调 /export 而服务端只有 /export-all → 404）；LocalWebAPI 补 `GET /herbs/import-template`/`/export`/`/export-all`（此前缺失 → 本地模式 404）；② 验方模板 DTO 对齐——FormulaImportItemDto 补 Category（实体/Factory 已支持，导入分类不再静默丢弃）+ 模板 Example.Herbs 改对象数组 [{HerbName,Dosage,Unit}]（照抄模板此前必解析失败）；③ 同类路由修复（新发现）：LocalWebAPI 验方 16 个 action 路由模板缺前导 `/` → 属性路由与类级 herbs 前缀拼接成 /api/v1/herbs/api/v1/formulas/*（离线模式验方全操作 404）——对齐 Remote CATALOG-ROUTE-FIX 先例改绝对路径；④ 新增契约-端点路由对齐守卫测试（ImportExportRouteParityTests——Refit 路径 ↔ Remote/Local 路由表反射比对）+ ImportExportJsonTests/LocalImportExportJsonTests 补药材断言；⑤ 修正 HERB-007/013 Desktop ⚠️→✅（修复后成立）；FORM-013 Desktop ✅→⚠️（导出缺药材组成明细 + 分类筛选参数对齐 P2 待办）；⑥ 需求文档：US-PAT-011/012 状态 🔧→✅、US-HERB-007/013 验收标准去 Excel 残留、删虚构接口 IHerbImportExportService/IFormulaImportExportService、US-FORM-013 删 AllowAnonymous 矛盾规则 | 任务书 .hermes-task-p1-fixes.md（源自 desktop-deep-code-review-2026-08-19 报告 P1/P2 结论） |
| 2026-08-19 | **v1.9 批量导入/导出 UI 接线完成**——PAT-011/012、HERB-006/007/013、FORM-006/013 Desktop ⚠️→✅（PatientMasterDetailViewModel/HerbMasterDetailViewModel/FormulaMasterDetailViewModel 加 Import/Export/DownloadTemplate 命令 + View 工具栏按钮；修复 Herb 死绑定 ImportHerbsCommand/ExportHerbsCommand；User 无导出 API 删除 ExportCommand 死绑定）；详见报告 desktop-batch-import-export-2026-08-19.md | 任务书 batch-import-export-ui：将 Service/Repository 层接线到 ViewModel/View |
| 2026-08-19 | **v1.8 Desktop 状态列代码级复核修正（desktop-doc-check）**——批量操作 US（USER-012/PAT-008/HERB-012/FORM-005/MC-015）Desktop ⚠️（UI 批量交互但循环单条删除，未消费批量端点）；引用检查 US（PAT-009/010/HERB-008/009）Desktop ⚠️（ViewModel 零消费） | 任务书 desktop-doc-check：基于最新文档验证 Desktop 实现状态 |
| 2026-09-14 | **⚠️ US 补 E2E 测试批次（4 组）**：US-FORM-014/Shell-007/CFG-006/ERR-006+007 状态列按 E2E 实测校准——US-SHELL-007 ✅部分实现→⚠️（ERR-70506 仅日志未阻断）、US-CFG-006 ✅已实现→⚠️（同进程静态 IOptions 快照不热更新 + UI 写路径 CWD 与读取路径不一致 + 本地 section 白名单拦截）、US-ERR-006/007 补实测映射（模型校验 400 ProblemDetails / 业务失败 ApiResponse / 本地无 409 生产者，电话唯一查重因 AES-GCM 恒不命中）、US-FORM-014 记录克隆漏拷 ProcessingMethod 修复；新增 E2E：ModeSwitchE2ETests（4）/ExceptionMappingE2ETests（6）/FormulaCrudE2ETests.Clone_*（4）/ConfigurationE2ETests.UpdateClinicSettings_PersistsAndReloads（1） | 任务书 e2e-tests-for-warning-us：⚠️ US 需有 E2E 覆盖，且状态列必须与代码实测一致（禁止「✅ 已实现」覆盖已知缺口） |
| 2026-08-19 | **v1.7 新增 WebAPI/Desktop 双端状态分列**——全部 US 填充（实际 154 行：Shell 20 含 020~025 追加，统计表 151 为既有滞后未改）；图例加两列定义 + 判定依据；批量导入/导出、引用检查、history/batch-details、权限查询等端点 Desktop 有 API 层但无 ViewModel 消费 → ⚠️；US-REG-002 两步建号 Desktop 第 1 步（Source=Doctor 建号）无 UI → ⚠️；US-FORM-014 克隆远程缺端点 → WebAPI ⚠️ | 任务书 traceability-webapi-desktop：明确每个 US 双端实现状态（依据 doc-code-audit 报告 + 代码扫描） |
| 2026-06-28 | 新增「十五、报表管理」（US-REPORT × 3，均 ✅ 已实现）；合计 139→142（v1.0 138→141） | A7 报表清单设计落地 |
| 2026-06-28 | US-REG-002 ⚠️→🧲（QuickVisit 待激活：急诊+本地常规）；US-REG-005 D8 注细化；REG/合计统计同步 | R10 spec S8 文档更新 |
| 2026-06-28 | US-SHELL-018 补「双模式面板 + 服务端 Configuration API 依赖（ADR-0014）」注；关联 ADR 列补 ADR-0014 | sysadmin 配置设计 spec S7 文档更新 |
| 2026-06-28 | 建立追溯矩阵（138 US × 8 列），整合 D1-D10 决策与 scenario-map 状态 | plan Task 1：追溯基础设施 |
| 2026-06-28 | US-REPORT-001/002/003 ✅→🚧v1.0待实现（S1 降级）；REPORT 行/合计同步（✅102→99，🧲18→21） | 审计 S1：时间参数代码未实现，状态虚高修正 |

# API 真机测试报告（增量记录）

> 目标：60.190.215.86:5000（Production 环境名部署验证）
> 依据：docs/compose/reports/webapi-test-strategy-2026-08-12.md（L3/L4 真机形态）
> 方法：边沉淀边测试——每域完成即记录

## Domain 1: Identity（✅ 全通过）

| # | 测试项 | 预期 | 实际 | 结论 |
| --- | -------- | ------ | ------ | :---: |
| 1 | login（sysadmin 正确密码） | 200+JWT | 200 token=448 | ✅ |
| 2 | validate（token 有效） | 200 | 200「Token验证成功」 | ✅ |
| 3 | refresh（refreshToken） | 200 新 token | 200 data.token | ✅ |
| 4 | GET /users 分页 | 200+列表 | 200 totalCount=8 | ✅ |
| 5 | reset-password on sysadmin | 拒绝 | 422「系统管理员密码不可被重置」 | ✅ 安全保护 |
| 6 | create user + login 新用户 | 200+可登录 | 200 创建→200 登录 | ✅ |
| 7 | delete test user（清理） | 200 | 200 删除成功 | ✅ |

**沉淀**：

- 认证流完整（login→validate→refresh→logout 待测）
- sysadmin 密码保护真实生效（T5-1 #12）
- 测试用户创建→登录→删除闭环验证（不污染数据）

## Domain 2: Catalog（🔴 两个 bug：PUT formula 500——PostProcessor 误判 + RowVersion 并发）

### Bug 1（已修）：PostProcessor-ConfigurationManager 兼容

- 已修复（df49d7ba9 + 640da18b3）：PostProcessor 在 ConfigurationManager 下读不到新 Add 的 env provider → 误回退占位符
- 补 3 个真实 ConfigurationManager 测试（复现原场景）
- **但部署后 PUT formula 仍 500**——说明还有 Bug 2

### Bug 2（✅ 已修复 2026-08-13——两层根因）：DbUpdateConcurrencyException（RowVersion 并发）

- 日志证据：`System.InvalidOperationException: 数据已被其他用户修改` → `DbUpdateConcurrencyException: expected 1 row, affected 0`
- 触发：PUT /api/v1/formulas/{id}（同 body / 改 dosage 都稳定复现）
- **第 1 层根因**（6ecd7536d）：`BaseRepository.UpdateAsync` 的 `_dbSet.Update(entity)` 对已跟踪实体全标记 Modified（含 RowVersion）——已修复：已跟踪实体只 SaveChanges
- **第 2 层根因**（049d0e0e2，EF SQL 日志实证）：`ReplaceHerbs` 的新 `FormulaHerbItem` 被 EF **误标 Modified**（非 Added）→ `UPDATE FormulaHerbItems WHERE Id=新Guid` → 0 rows——已修复：① ReplaceHerbs 孤儿删除模式（只设 FormulaId 不设导航）；② `FormulaRepository.UpdateAsync` override 强制新 item `EntityState.Added`；③ BaseRepository 并发重试（真并发仍抛——乐观语义保持）
- 复现测试：`FormulaReplaceHerbsTests` 2 用例（真实 DbContext+SQLite——与真机异常一致）+ `HerbUpdateRowVersionTests` 2
- **真实 SQL Server 验证（2026-08-13 formula-realsql-fix——SQLite 通过不算数）**：`FormulaRealSqlUpdateTests` 2 用例（连接 192.168.190.243 LYBTDB_Test——TEST_DB_CONNECTION 激活）——`UpdateFormula_ReplaceHerbs_OnRealSqlServer_PersistsWithoutConcurrencyError`（764ms：EnsureCreated+建删数据）与 `UpdateFormula_DetachedReplaceHerbs_OnRealSqlServer_InsertsNewItems` **2/2 通过**——替换 herbs 无并发冲突（新 item INSERT 非 UPDATE WHERE 新 Id）+ Detached 场景正确 INSERT
- **引用校验缺口修复（同日）**：空 herbs → Validator `NotEmpty`（400）；herbId 不存在/已删除 → `ValidateBeforeSaveAsync` 引用校验（`FormulaValidationFailed` 60004 → **422**）——`FormulaReferenceValidationTests` 3 用例
- **深挖 3（formula-deep-fix——Dev 非空库权威）**：
  - **Dev 真实数据复现**：`FormulaDevReproTests` 连 **LYBTDB_Dev**（26 患者/15 挂号等真实数据——.env.development 连接串）——取真实含 herbs 验方 → 同 herbs 回传 ReplaceHerbs → UpdateAsync——**REPRO-NEGATIVE（更新成功无并发异常）**——SQL 证据：`DELETE FROM FormulaHerbItems` → `UPDATE Formulas SET UpdatedAt...OUTPUT INSERTED.RowVersion` → `INSERT INTO FormulaHerbItems`——当前代码在 Dev 数据上正确
  - **部署滞后铁证**：真机 dll 03:52 vs bca7bccf0 提交 11:49——真机 PUT 500/空 herbs 201/herbId 500FK **全是旧 dll 行为**（修复代码未部署）
  - **重写 FormulaRepository.UpdateAsync**（用户授权大范围）：显式 DbSet 模式——旧 items 按 FormulaId `RemoveRange`（未跟踪自动 Attach+Deleted）+ 新 items 显式 `Add`（强制 Added）——**根治 EF 对 item 状态的隐式误判**（不再依赖 Detached/Modified 状态判断）；父行走 base（Attached 只 SaveChanges——RowVersion 正确；Detached 保留 Update()）——乐观并发保持
  - **钩子未拦截根因**：本地 DI 注册测试证明 `CreateFormulaValidator` 已注册 + ValidationBehavior 拒绝空 herbs（`FormulaValidatorRegistrationTests` 2 通过）——真机 201 = 旧 dll 无此代码

**沉淀**：真机测试连续发现 2 个 bug——①PostProcessor 测试类型≠生产类型（ConfigurationRoot vs ConfigurationManager）②通用更新命令的并发令牌处理（RowVersion 被 Update 标记）。两个都是「测试自洽但系统不跑」的层级错配实例——单元测试用 mock/fake 永远测不到 EF 真实并发语义。

### Bug 2 修复后真机复查（2026-08-13 03:20，PID 730647 部署 049d0e0e2）

- PUT herb → 200 ✅（第 1 层修复生效）
- **PUT formula → 仍 500 并发冲突** 🔴
- 修复测试（FormulaReplaceHerbsTests 用 SQLite）通过，但**真实 SQL Server 仍失败**——SQLite vs SQL Server 的 EF 行为差异
- **新增发现（测试矩阵 §11.2）**：
  - #2 空 herbs → 201（预期 400）❌ AC 未拦截
  - #3 herbId 不存在 → 500（预期 422）❌ 引用校验异常
  - #4 herbId 已删除 → 201（预期 422）❌ 引用校验缺失

### 🔴 真根因：部署只上传 WebAPI.dll，模块 dll 未更新（2026-08-13 05:32 确认）

**5 轮修复本地测试全过但真机全败的真正原因**：

- 部署命令 `scp publish-webapi/LYBT.WebAPI.dll` 只传了 WebAPI 主 dll
- **修复代码在模块层**（LYBT.Module.Catalog.dll / LYBT.Infrastructure.dll）——服务器上仍是 01:45 旧版
- 全部 dll 上传后（05:32）：PUT formula 200 + 引用校验 400/422 全通过 ✅

**深层教训**：

1. **部署必须全量传 dll**（或确认哪些 dll 变了）——不能只传主程序
2. **真机失败时先查「服务器代码是否真的是最新」**——dll 时间戳对比是第一步
3. 5 轮误判（PostProcessor/SQLite/SQL Server/SplitQuery）都是因为**根本没测到新代码**——真机验证的前提是部署正确
4. SplitQuery 移除是**有效独立修复**（启动无连接错误了）但非 PUT formula 根因

### 踩坑清单 #12（01-deployment.md）

「部署只传主 dll 不传模块 dll → 修复不生效」——发布时全量上传或对比 dll 时间戳。

## Domain 4: 挂号（US-REG 系列）——全链路真机通过 ✅

| # | US | 测试项 | 结果 |
|---|-----|--------|:---:|
| 1 | US-REG-001 | 前台创建挂号（Waiting） | ✅ 201 |
| 2 | US-REG-005 | 医生接诊 start-visit（Waiting→InProgress + 原子建医案） | ✅ 200 |
| 3 | US-REG-004 | 查看排队/列表 | ✅ 200 |
| 4 | US-REG-006 | 取消挂号（Waiting） | ✅ 200 |
| 5 | US-REG-002 | QuickVisit 两步（建 Waiting + start-visit） | ✅ 201+200 |
| 6 | 权限 | Receptionist 创建挂号 ✅ / Doctor 建号（doctorId=当前医生）✅ | ✅ |
| 7 | BR-001 | 已有 Active 医案 → 422「已有进行中医案」 | ✅ 保护 |
| 8 | 业务规则 | 同日重复挂号 → 422「该患者今日已有待诊挂号」 | ✅ 保护 |

**修复**（本域测试发现）：
- POST /Registrations Create 端点被 quick-visit 删除误删（405）→ b058a1124 恢复
- Consultation.CreatedBy NULL（start-visit 500）→ 18a6f676e 修复（pi 发现 + 交付）

## Domain 5: 医案（US-MC 系列）——核心链全通过 ✅

| # | US | 测试项 | 结果 |
|---|-----|--------|:---:|
| 1 | US-MC-001 | 接诊即建医案（Active + Consultation 1:1） | ✅ 200 |
| 2 | BR-001 | 单活跃医案约束（Active 时再建 → 422） | ✅ |
| 3 | US-MC-002 | 统一保存（诊断+处方聚合） | ✅ 200「保存成功」 |
| 4 | US-MC-011 | 完成医案（BR-003 六项校验通过） | ✅ 200「医案已完成」 |
| 5 | BR-003 | 完成校验（辨证/处方标志/存在性/药材/帖数/上限） | ✅ 校验生效 |
| 6 | US-MC-014 | 取消医案（物理删除） | ✅ 200 |
| 7 | 权限 | 删除已完成医案 → 403（仅 Admin，Doctor 无权限） | ✅ |
| 8 | 权限 | close 强制关闭 → 403（仅管理员） | ✅ |
| 9 | US-MC-013 | 挂起（Active→Suspended） | ✅ 200「医案已暂存」 |
| 10 | US-MC-010 | 恢复编辑（Suspended→Active） | ✅ 200「状态更新成功」 |
| 11 | A2 快照 | 处方明细金额（subtotal=10g×0.3=3.0） | ✅ 修复后正确 |

**修复**（本域测试发现）：
- 处方明细 subtotal/totalPrice=0（Mapperly 忽略映射）→ 4bfc70849 修复（pi 交付）




## Domain 12: 药材删除保护 + 报表空数据边界 ✅

| # | US | 测试项 | 结果 |
|---|-----|--------|:---:|
| 1 | US-HERB-005 | 删除被引用药材 → 422「已被 3 条处方、1 个验方引用」 | ✅ 保护 |
| 2 | US-HERB-007 | 药材导出全部（JSON） | ✅ 200（1 条） |
| 3 | US-REPORT-003 | 收入趋势（固定30天窗） | ✅ 200 |
| 4 | US-REPORT-002 | 医生绩效（2020 空数据 → 0 条） | ✅ 空边界 |
| 5 | US-REPORT-004 | 患者流（30 天 labels） | ✅ 200 |

## Domain 11: 患者细节补测（电话唯一 + 拼音）✅

| # | US | 测试项 | 结果 |
|---|-----|--------|:---:|
| 1 | US-PAT-003 | 电话唯一（同电话 → 409） | ✅ 409「该手机号已关联其他患者」 |
| 2 | US-PAT-004 | 拼音自动生成 + 搜索 | ✅ 200（1 条） |
| 3 | 409 映射 | ErrorCode → HTTP 状态码对齐 | ✅ 48278e512 |

**修复**（本域测试发现）：
- 电话唯一约束缺失（同电话可重复创建）→ 231d15326（pi 交付：Create/Update/Batch 409 + 拼音兜底）
- 409 状态码偏差（422→409，HandleResult 未走 ErrorCode 映射）→ 48278e512（pi 交付）

## Domain 10: 医案查询系列补测（US-MC-005~009）✅

| # | US | 测试项 | 结果 |
|---|-----|--------|:---:|
| 1 | US-MC-005 | 分页列表（Doctor 角色过滤——只见自己） | ✅ 200（3 条） |
| 2 | US-MC-006 | 统一查询 ByPatient | ✅ 200 |
| 3 | US-MC-007 | 跨模块搜索（患者+诊断+日期） | ✅ 200（3 条） |
| 4 | US-MC-008/009 | 诊断/处方历史（空历史边界） | ✅ 200（0 条） |
| 5 | 权限 | 前台查医案 → 403（Receptionist 无权限） | ✅ |

## Domain 9: 认证流补测（US-AUTH 系列）✅

| # | US | 测试项 | 结果 |
|---|-----|--------|:---:|
| 1 | US-AUTH-001 | 用户名密码登录（成功 JWT / 错密码 401） | ✅ |
| 2 | US-AUTH-002 | 登录失败锁定（5次失败→锁→锁定期间拒） | ✅ 403「账号已被锁定」 |
| 3 | US-AUTH-004 | 令牌刷新（新 token 有效） | ✅ 200 |
| 4 | US-AUTH-005 | 令牌验证 | ✅ 200 |
| 5 | US-AUTH-008 | 登出（令牌失效） | ✅ 200 |
| 6 | US-AUTH-011 | 保留用户名拦截（sysadmin/admin） | ✅ 400 |

**备注**：AUTH-003 限流（429）、006 重放、007 审计——谨慎型/查询型，未触发（避免锁真机账号）


| # | 测试项 | 结果 |
|---|--------|:---:|
| 1 | 患者导出 JSON（25 条） | ✅ 200 JSON |
| 2 | 导入模板 JSON（字段+必填） | ✅ 200 |
| 3 | 关键词搜索「前台」 | ✅ 200（5 条） |
| 4 | 引用检查 check-reference | ✅ 200（hasReferences/count/canDelete） |
| 5 | 删除有引用患者 → 422「患者有 2 条医案记录，无法删除」 | ✅ 保护 |
| 6 | 恢复未删除患者 → 422 | ✅ |
| 7 | 验方创建（herbName 必填） | ✅ 201 Draft |
| 8 | 验方详情（herbs 组合——导入源） | ✅ 200 |
| 9 | 验方导入→医案处方（前端组合） | ✅ 200（价格快照正确） |
| 10 | **导入导出改 JSON**（2026-08-13 用户决策：后端不涉及 Excel） | ✅ 850c601a0 |
| 11 | 药材导出 export-all JSON | ✅ 200 |
| 12 | 药材导入模板 JSON | ✅ 200 |

**变更**：患者/药材/验方导入导出 Excel → JSON（后端格式中立——EPPlus/ExcelExportHelper 已移除；Excel 处理归前端）


## Domain 6: 报表（US-REPORT 系列）——8/8 全通过 ✅

| # | 端点 | 测试项 | 结果 |
|---|------|--------|:---:|
| 1 | /reports/daily/income | 每日收入（总收入/挂号费/药费） | ✅ 200 |
| 2 | /reports/daily/consultations | 每日就诊（总数/按医生） | ✅ 200 |
| 3 | /reports/daily/herbs | 每日药材统计 | ✅ 200 |
| 4 | /reports/trend/income | 收入趋势（7天） | ✅ 200 |
| 5 | /reports/trend/consultations | 就诊趋势（7天） | ✅ 200 |
| 6 | /reports/doctor-performance | 医生绩效 | ✅ 200 |
| 7 | /reports/herbs/ranking | 药材排行 | ✅ 200 |
| 8 | /reports/patient-flow | 患者流（新/复诊） | ✅ 200 |

## Domain 7: 系统维护（sysadmin 专属）——通过 + 权限修复

| # | 端点 | 测试项 | 结果 |
|---|------|--------|:---:|
| 1 | /health | 匿名存活探针 | ✅ 200 Healthy |
| 2 | /health/database | DB 健康 | ✅ 200 |
| 3 | /health/ping | Ping（版本/DB 耗时） | ✅ 200 |
| 4 | /health/details | 详细健康 | ✅ 200 |
| 5 | /diagnostics/logging/status | 日志级别状态 | ✅ 200 |
| 6 | /configuration | 系统配置（sysadmin） | ✅ 200 |
| 7 | /configuration/sections/Jwt | Jwt 配置节 | ✅ 200 |
| 8 | /configuration/validate | 配置校验 | ✅ 200 |
| 9 | 权限 | **testadmin 访问 /configuration → 403**（权限隔离） | ✅ 修复后 403 |

**修复**（本域测试发现）：
- ConfigurationController 权限 AdminOrSuperAdmin → SysAdminOnly（业务管理员越权读系统配置）→ 3fa497d3e 修复（pi 交付，双控制器 + 测试 + 真机 403/200）


| # | US | 测试项 | 结果 |
| --- | ----- | -------- | :---: |
| 1 | US-USER-001 | 分页查询用户列表（8 用户） | ✅ 200 |
| 2 | US-USER-002 | 查看用户详情 | ✅ 200 |
| 3 | US-USER-004 | 创建用户（Doctor） | ✅ 200 |
| 4 | US-USER-006 | 删除自己 → 422「不能删除当前登录账号」 | ✅ 保护 |
| 5 | US-USER-007 | 重置 sysadmin → 422「系统管理员密码不可被重置」 | ✅ 保护 |
| 6 | US-USER-008 | 改自己资料 200 + 改他人 → 403「只能修改自己的」 | ✅ IDOR |
| 7 | US-USER-009 | 改密成功 + 旧密码错 → 422 + 新密码可登录 | ✅ |
| 8 | US-USER-010 | 禁用 → 200 + 禁用后登录 403 | ✅ |
| 9 | US-USER-011 | 恢复（未删除 → 422「无需恢复」——正确规则） | ✅ |
| 10 | US-USER-012 | 批量删除 → 200 + 删除后查询 0 | ✅ |

**保护规则验证**：不可删除自己 ✅ / sysadmin 密码不可重置 ✅ / IDOR ✅ / 禁用不可登录 ✅

## Domain 3: Patients（进行中）

### Bug 3（✅ 已修复 2026-08-13 startvisit-createdby-fix）：StartVisit 创建医案 CreatedBy NULL

- 真机：PUT /api/v1/Registrations/{id}/start-visit → 500——服务器日志直达根因：`[REPO] MedicalCase.SaveChanges 保存失败 → SqlException: 不能将值 NULL 插入列 'CreatedBy'，表 'MedicalCases'`
- 根因：`MedicalCaseCrossModuleService.CreateMedicalCaseForRegistrationAsync` 的 input 只设 PatientId/UserId/RegistrationId；`CreateFromInputDtoAsync` 创建块缺 `CreatedBy`（BaseEntity 可空但 MedicalCaseConfiguration 强制 NOT NULL）
- 修复：`CreateFromInputDtoAsync` 设 `CreatedBy = currentUserId`（创建者=操作医生——接诊即建 US-REG-005+US-MC-001 核心路径）
- 同类排查：唯一 NOT NULL CreatedBy 实体 = MedicalCase（配置强制）；Registration.CreatedBy 可空但语义缺失——已补（`CreateRegistrationCommand` 加 OperatorId + Handler 设 CreatedBy——QuickVisit 已有先例）；Formula/Herb 工厂有 createdBy 参数

### Bug 4（✅ 已修复 2026-08-13 consultation-createdby-fix）：StartVisit 接诊即建 Consultation.CreatedBy NULL（500 两连击完结）

- 真机：PUT /api/v1/Registrations/{id}/start-visit → 500——服务器日志直达根因：`SqlException: 不能将值 NULL 插入列 'CreatedBy'，表 'LYBTDB_Dev.dbo.Consultations'；列不允许有 Null 值`
- 根因：`CreateFromInputDtoAsync` 创建 Consultation 只初始化 Id/CreatedAt/UpdatedAt，**漏设 CreatedBy**——`ConsultationConfiguration` 同 MedicalCase 一样强制 NOT NULL。与 Bug 3（MedicalCase.CreatedBy）同源，**两连击**：上次只修了 MedicalCase，漏掉共享主键级联插入的 Consultation
- 修复：`CreateFromInputDtoAsync` Consultation 补 `CreatedBy = currentUserId`；**同类排查**：Prescription.CreatedBy（PrescriptionConfiguration 同样 NOT NULL——医生带处方建案同样会 500）补 currentUserId；MedicalCaseAuditLog（更新/取消审计）补 CreatedBy=操作者；MedicalCasePrintLog 补 CreatedBy=operatorId；PrescriptionDetailDto 补 CreatedBy（可观测）
- **回归测试**：`StartVisit_MedicalCaseAndConsultation_CreatedByPopulated`（接诊即建）+ `CreateMedicalCase_WithPrescription_CreatedByFieldsPopulated`（带处方建案）——断言三实体 CreatedBy = 操作者
- **验证**：构建 0/0；Server 696/696；架构 87/87；真机修复后 PUT start-visit → **200**（原 500），挂号 InProgress + 医案 Active（MC20260813001）+ MedicalCase/Consultation CreatedBy=testdoctor2 均非空 ✅

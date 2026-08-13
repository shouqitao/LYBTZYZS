# WebAPI 完整测试计划（2026-08-13 整理，待用户审核）

> 目标：系统化覆盖 102 端点/14 分组，验证「系统可交付 + 权限矩阵真实生效」
> 原则：**角色分层测试**——sysadmin 测系统维护、Admin 测业务维护、Doctor 测开方、Receptionist 测挂号（不再只用 sysadmin 散测）
> 依据：需求文档 US 清单 + 12-permissions-matrix.md + 04-permissions.md + 端点清单（swagger 102 端点）

---

## 一、测试账号矩阵（角色分层）

| 账号 | 角色 | 用途 | 状态 |
|------|------|------|------|
| sysadmin / SysAdmin@2026! | 系统维护 | 用户管理/配置/诊断/部署 | ✅ 已有 |
| testadmin / Test@12345 | Admin（业务维护） | 患者/药材/验方/报表/医案编辑 | ✅ 已建 |
| testdoctor / Test@12345 | Doctor（开方） | 医案创建/处方/挂号 QuickVisit | ⚠️ 需 Admin 创建 |
| testrecep / Test@12345 | Receptionist（前台） | 挂号创建/队列 | ⚠️ 需 Admin 创建 |

> 注意：sysadmin 只能创建 Admin（USER-D04 已修）；Doctor/Receptionist 需 testadmin 创建——这也顺带验证 Admin 层级。

---

## 二、测试域划分（14 分组 → 7 测试域）

### 域 A：认证与用户（auth 5 + users 14 = 19 端点）——**已测 10/19**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| login/refresh/validate/logout 流 | sysadmin | US-AUTH-001/002 | ✅ 已测 |
| 用户 CRUD + 保护规则（删自己/重置 sysadmin） | sysadmin | US-USER-001~012 | ✅ 已测 |
| **CreateUser 层级校验**（sysadmin→Admin 仅） | sysadmin | USER-D04 | ✅ 已修+测 |
| **Admin 创建 Doctor/Receptionist** | testadmin | USER-D04 | ⏳ 待测 |
| **Admin 创建 Admin → 拒绝** | testadmin | USER-D04 | ⏳ 待测 |
| users/current 端点 | sysadmin | US-USER-003 | ⏳ 待测 |
| 批量启用/禁用（batch-enable/disable） | sysadmin | US-USER-012 | ⏳ 待测 |
| **登录锁定**（5 次错误 → 锁 15 分） | testdoctor | US-AUTH-002 | ⏳ 待测 |

### 域 B：药材与验方（herbs 16 + formulas 15 = 31 端点）——**已测 13/31**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| Herb CRUD + 引用检查 + 批量 | sysadmin | US-HERB-001~012 | ✅ 已测 |
| Formula CRUD + 引用校验 + 空 herbs 400 | sysadmin | US-FORM-003/004 | ✅ 已测（矩阵 #1-6） |
| **Herb 批量导入/导出/模板**（Excel 双路径） | testadmin | US-HERB-006/007/013 | ⏳ 待测 |
| **Formula 批量导入/导出/待验证列表** | testadmin | US-FORM-006/007 | ⏳ 待测 |
| **Formula 验证流程**（pending→validate 单味→晋升 Validated） | testadmin | US-FORM-007/008/009 | ⏳ 待测 |
| **Formula 降级**（更新含未验证 herb → Draft） | testadmin | US-FORM-010 | ⏳ 待测 |
| **禁用/恢复 + 批量启用/禁用** | testadmin | US-FORM-011/013 | ⏳ 待测 |
| **Doctor 权限**（只看自己+共享，改他人 403） | testdoctor | US-FORM-001/004 | ⏳ 待测 |

### 域 C：患者（Patients 14 端点）——**已测 11/14**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| CRUD + 引用检查 + 批量删除 + 导出/模板 | sysadmin | US-PAT-001~013 | ✅ 已测 |
| **by-id-number 查询** | testadmin | US-PAT-002 | ⏳ 待测 |
| **batch-import**（Excel） | testadmin | US-PAT-006 | ⏳ 待测 |
| **restore 权限验证**（仅 Admin 业务管理——sysadmin 403 已证） | testadmin | 矩阵:12 | ⏳ 待测 |

### 域 D：挂号（Registrations 12 端点）——**未测**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| 创建挂号（普通） | testrecep | US-REG-001 | ⏳ 待测 |
| **创建挂号权限**（Receptionist only——sysadmin/Admin 应 403） | testrecep | 矩阵:13-15 | ⏳ 待测 |
| QuickVisit（快速就诊） | testrecep | US-REG-002 | ⏳ 待测 |
| 队列查询 queue | testrecep | US-REG-003 | ⏳ 待测 |
| 取消/start-visit/状态流转 | testrecep | US-REG-005/006 | ⏳ 待测 |
| **Doctor start-visit 权限** | testdoctor | 接诊即建 | ⏳ 待测 |

### 域 E：医案与处方（medicalcases 20 端点）——**未测**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| **创建医案（Doctor only——sysadmin 403 已证）** | testdoctor | US-MC-001 | ⏳ 待测 |
| 创建含处方（价格快照）+ 查询 | testdoctor | US-MC-002/A2 | ⏳ 待测 |
| **价格快照隔离**（调价后旧处方不变） | testdoctor | A2 | ⏳ 待测 |
| 编辑（当天）/关闭/取消/挂起/状态 | testdoctor | US-MC-003~008 | ⏳ 待测 |
| 患者历史 history | testdoctor | US-MC-009 | ⏳ 待测 |
| audit-logs 审计 | sysadmin | US-MC-017 | ⏳ 待测 |
| search/query 搜索 | testdoctor | US-MC-010 | ⏳ 待测 |
| **Admin 编辑权限（纠偏/EditReason）** | testadmin | 矩阵:18 | ⏳ 待测 |

### 域 F：报表（reports 8 端点）——**已测 1/8**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| daily/income | sysadmin | US-REP-001 | ✅ 已测 |
| daily/consultations + daily/herbs | testadmin | US-REP-002/003 | ⏳ 待测 |
| doctor-performance / herbs-ranking | testadmin | US-REP-004/005 | ⏳ 待测 |
| patient-flow / trend×2 | testadmin | US-REP-006/007 | ⏳ 待测 |
| **报表权限**（Receptionist/Doctor 权限边界） | 各角色 | 矩阵 | ⏳ 待测 |

### 域 G：系统维护（health 3 + configuration 8 + diagnostics 4 + deploy 2 = 17 端点）——**已测 5/17**
| 测试项 | 账号 | 覆盖 US | 状态 |
|--------|------|---------|:---:|
| health / health/details / health/ping | 匿名 | — | ✅ 已测 |
| configuration GET 全部 + 脱敏 | sysadmin | US-SHELL-017 | ✅ 已测 |
| **configuration PUT 白名单**（业务分区可改/敏感 403） | sysadmin | ADR-0014 | ⏳ 待测 |
| **configuration validate / sections / {key}** | sysadmin | US-SHELL-017 | ⏳ 待测 |
| **configuration restart（延迟重启）** | sysadmin | ADR-0014 | ⏳ 待测（谨慎） |
| **diagnostics 日志开关/级别/状态** | sysadmin | US-SHELL-019 | ⏳ 待测 |
| **deploy upload/restart** | sysadmin | US-SHELL-020 | ⏳ 待测（谨慎） |
| **系统端点权限**（非 sysadmin 访问 configuration/deploy → 403） | testadmin | 矩阵 | ⏳ 待测 |
| **下载页 / + swagger 导航** | 匿名 | — | ✅ 已测 |

---

## 三、横切测试（跨域）

| 测试项 | 说明 | 状态 |
|--------|------|:---:|
| **无 token → 401**（各域代表端点） | 认证拦截 | ✅ 已测 4 端点 |
| **角色权限矩阵抽查** | 每域选 1 个「仅特定角色」端点，验证其他角色 403 | ⏳ 待测 |
| **输入校验**（缺字段 400/超长 400/非法枚举） | 每域 1-2 例 | ⏳ 待测 |
| **404 语义**（不存在 ID） | 每域 1 例 | ✅ 部分 |
| **分页边界**（page=0/pageSize=101/超大） | 列表端点 | ⏳ 待测 |
| **登录锁定**（5 次错误 → 15 分锁） | 认证 | ⏳ 待测 |

---

## 四、执行顺序（依赖关系）

```
Phase 1（已完成）：sysadmin 冒烟 + 用户模块 + formula 修复 + CreateUser 层级
Phase 2：补测 域A 剩余（Admin 层级/current/批量/锁定）+ 域B 剩余（批量导入导出/验证流/Doctor 权限）
Phase 3：域C 剩余 + 域D（挂号——先建 testrecep）
Phase 4：域E（医案/处方——先建 testdoctor + Doctor 开方流）
Phase 5：域F 报表 + 域G 系统维护
Phase 6：横切（角色矩阵抽查/输入校验/分页边界）
```

---

## 五、需要先做的准备

1. **创建 testdoctor / testrecep**——用 testadmin 创建（同时验证 Admin 层级）
2. **测试数据准备**：批量导入 Excel 模板（herbs/patients/formulas）
3. **谨慎端点标注**：configuration restart / deploy restart 会重启服务——放最后单独验证
4. **测试结果沉淀**：每域完成更新 docs/compose/reports/api-manual-test-2026-08-13.md

---

## 六、验收标准

- [ ] 102 端点全部覆盖（每端点至少 1 次成功调用 + 权限/边界抽查）
- [ ] 角色矩阵抽查：sysadmin 业务端点 403 确认 + Admin 业务 200 + Doctor 开方 200 + Receptionist 挂号 200
- [ ] 无「未测域」（当前 D/E 完全未测）
- [ ] 发现的 bug 全部进 backlog 或已修复
- [ ] 测试报告完整沉淀

---

**请审核**——尤其关注：
1. 测试域划分是否合理（7 域）
2. 角色矩阵是否完整（sysadmin/admin/doctor/receptionist 四角色）
3. 执行顺序是否有遗漏
4. 哪些端点不需要测（如 deploy/restart 风险高可跳过？）

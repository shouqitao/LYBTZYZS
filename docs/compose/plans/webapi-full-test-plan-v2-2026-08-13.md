# WebAPI 完整测试计划 v2（User Story 模式，2026-08-13 用户审核）

> **组织方式**：以 **User Story（用户故事）为主线**——每个 US 是一个业务场景，测试验证其**验收标准（AC）**。不是按端点机械分组。
> **覆盖范围**：全部 154 个 US 中 **WebAPI 可测的 ~120 个**（排除纯 Desktop/安装/培训类）。每个 US 列 AC + 角色 + 边界。
> **角色分层**：sysadmin=系统维护、Admin=业务维护、Doctor=开方、Receptionist=前台——按权限矩阵，每故事用正确角色测。
> **边界覆盖**：每个 US 至少测「主路径 + 权限边界 + 输入边界 + 数据边界」。

---

## 零、测试边界定义（完整覆盖的原则）

| 边界类型 | 说明 | 例子 |
|---------|------|------|
| **功能边界** | AC 逐条验证（成功路径） | US-FORM-003 创建成功 Draft |
| **权限边界** | 角色矩阵——谁可/谁不可 | sysadmin 创建 Doctor → 422 |
| **输入边界** | 缺字段/超长/非法枚举/格式错 | 空 herbs → 400 |
| **数据边界** | 不存在/已删除/禁用/关联数据 | herbId 已删除 → 422 |
| **状态边界** | 状态机流转（Waiting→InProgress→Completed） | 挂号取消仅当天 Waiting |
| **并发边界** | 乐观并发/重复提交 | PUT formula 并发 → 并发异常 |
| **安全边界** | 认证/IDOR/敏感脱敏/限流 | 改他人资料 → 403 |
| **引用边界** | 被引用数据不可删 | 删除被处方引用的药材 → 422 |

---

## 一、认证与账户（auth 13 US + users 12 US = 25 个故事）

### A1. 认证流（US-AUTH-000~013）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-AUTH-000 | 首次登录初始化 | 强制改密 | sysadmin | 首登流程 | ⏳ |
| US-AUTH-001 | 用户名密码登录 | 成功返回 JWT；失败通用错误 | 全员 | 错密码/不存在用户 | ✅ 部分 |
| US-AUTH-002 | 登录失败锁定 | 5 次失败→锁 15 分；锁定期拒绝；期满自动解锁 | 全员 | 阈值边界 | ✅ |
| US-AUTH-003 | 登录限流 | 超阈值→429 | 匿名 | 频繁请求 | ⏳ |
| US-AUTH-004 | 令牌刷新 | 新 token+旧失效；无效→401 | 全员 | 重放 | ✅ |
| US-AUTH-005 | 令牌验证 | /validate 校验 | 全员 | 过期/伪造 | ✅ 部分 |
| US-AUTH-006 | 重放检测 | 旧 refresh 重放→撤销全族 | 全员 | 令牌族 | ⏳ |
| US-AUTH-007 | 安全审计日志 | 登录/刷新/锁定记录 | sysadmin | 审计查询 | ⏳ |
| US-AUTH-008 | 登出 | 令牌失效 | 全员 | 重复登出 | ✅ |
| US-AUTH-009 | 本地自动登录 | AutoLoginToken | Desktop | — | ⏳（Desktop） |
| US-AUTH-010 | AutoLoginToken 轮换 | 使用后轮换 | Desktop | — | ⏳（Desktop） |
| US-AUTH-011 | 保留用户名拦截 | 保留名不可用 | sysadmin | 保留名单 | ✅ |
| US-AUTH-012 | 本地简化认证 | 1 年令牌 | Desktop | — | ⏳（Desktop） |
| US-AUTH-013 | 本地登录限流 | 5 次/分 | Desktop | — | ⏳（Desktop） |

### A2. 用户管理（US-USER-001~012）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-USER-001 | 分页查询用户 | 分页/关键字 | sysadmin | 分页边界 | ✅ |
| US-USER-002 | 用户详情 | 按 ID | sysadmin | 不存在 404 | ✅ |
| US-USER-003 | 当前用户资料 | /users/current | 全员 | 自身 | ✅ |
| US-USER-004 | 创建用户 | **USER-D04 层级** | sysadmin/Admin | 越级→422 | ✅ 已修 |
| US-USER-005 | 更新用户 | 用户名不可变；层级约束 | sysadmin/Admin | 角色变更 | ✅ |
| US-USER-006 | 删除用户 | 不可删自己 | sysadmin/Admin | 删自己→422 | ✅ |
| US-USER-007 | 重置密码 | sysadmin 不可重置 | sysadmin | 重置 sysadmin→422 | ✅ |
| US-USER-008 | 修改个人资料 | **IDOR 防护** | 全员 | 改他人→403 | ✅ |
| US-USER-009 | 修改密码 | 需旧密码 | 全员 | 旧密码错→422 | ✅ |
| US-USER-010 | 启用/禁用 | 禁用后不可登录 | sysadmin/Admin | 禁用登录→403 | ✅ |
| US-USER-011 | 恢复软删除 | 层级管理 | sysadmin/Admin | 未删除→422 | ✅ |
| US-USER-012 | 批量操作 | 逐项权限/跳过自己 | sysadmin/Admin | 上限 100 | ✅ 部分 |

---

## 二、药材与验方（herbs 13 + formulas 14 = 27 个故事）

### B1. 药材（US-HERB-001~013）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-HERB-001 | 分页查询 | 拼音/分类筛选 | Doctor/Admin | 分页边界 | ✅ |
| US-HERB-002 | 详情 | 按 ID | Doctor/Admin | 不存在 | ✅ |
| US-HERB-003 | 创建 | Admin+；名称唯一 | Admin | 重复名→422 | ✅ |
| US-HERB-004 | 更新 | Admin+ | Admin | 名称冲突 | ✅ |
| US-HERB-005 | 删除 | **引用检查** | Admin | 被处方引用→422 | ✅ |
| US-HERB-006 | 批量导入 | Skip/Update/Error | Admin | Excel 双路径 | ✅ |
| US-HERB-007 | 导出全部 | Excel | Admin | 空库 | ✅ |
| US-HERB-008/009 | 引用检查（单/批量） | 引用数 | Admin | 单/批 | ✅ 部分 |
| US-HERB-010 | 启用/禁用 | 状态切换 | Admin | 禁用后不可选 | ✅ |
| US-HERB-011 | 恢复软删除 | 仅 Admin 业务 | Admin | 未删除→422 | ✅ |
| US-HERB-012 | 批量操作 | 启用/禁用/删除 | Admin | 混合状态 | ✅ |
| US-HERB-013 | 导出+模板 | 下载 | Admin | 模板格式 | ✅ 部分 |

### B2. 验方（US-FORM-001~014）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-FORM-001 | 分页查询（所有权） | Doctor 仅自己+共享 | Doctor/Admin | 他人→过滤 | ✅ |
| US-FORM-002 | 详情 | 含 Herbs+验证状态 | Doctor/Admin | 非本人→403 | ✅ |
| US-FORM-003 | 创建 | **空药材→400**；Draft | Doctor/Admin | 引用校验 | ✅ |
| US-FORM-004 | 更新 | **替换 Herbs**；非本人 403 | Doctor/Admin | 并发 | ✅ |
| US-FORM-005 | 删除 | 软删除 | Doctor/Admin | 非本人→403 | ✅ |
| US-FORM-006 | 批量导入 | 匹配机制 | Admin | 10000 上限 | ✅ |
| US-FORM-007 | 待验证列表 | Draft to-do | Doctor | 分页 | ✅ |
| US-FORM-008 | 验证单味药材 | 绑定系统药材 | Doctor | 已绑定→422 | ✅ |
| US-FORM-009 | 全验证晋升 | →Validated | Doctor | 全验证自动 | ✅ |
| US-FORM-010 | 降级 Draft | 更新含未验证→Draft | Doctor | FLAW-F1 | ✅ |
| US-FORM-011 | 启用/禁用 | 状态 | Admin | — | ✅ |
| US-FORM-012 | 恢复软删除 | 层级 | Admin | 未删除→422 | ✅ |
| US-FORM-013 | 批量+导出+模板 | 批量操作 | Admin | 上限 | ✅ |
| US-FORM-014 | 克隆验方 | 复制 | Doctor | 克隆字段 | ✅ |

---

## 三、患者（US-PAT-001~014 = 14 个故事）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-PAT-001 | 分页查询 | 关键字/分页 | 全员 | 分页边界 | ✅ |
| US-PAT-002 | 详情 | 按 ID | 全员 | 不存在 404 | ✅ |
| US-PAT-003 | 创建 | 必填 | 全员 | 缺 name→400 | ✅ |
| US-PAT-004 | 更新 | 修改 | 全员 | IDOR | ✅ |
| US-PAT-005 | 删除 | **引用检查** | Admin+ | 被医案引用→422 | ✅ |
| US-PAT-006 | 启用/禁用 | 状态 | Admin+ | 禁用后操作 | ✅ |
| US-PAT-007 | 恢复 | **仅 Admin 业务** | Admin | sysadmin→403 | ✅ 已证 |
| US-PAT-008 | 批量删除 | 上限 | Admin+ | 部分成功 | ✅ |
| US-PAT-009/010 | 引用检查（单/批） | 引用数 | Admin | 单/批 | ✅ |
| US-PAT-011 | 导入模板 | 下载 | Admin | 格式 | ✅ |
| US-PAT-012 | 导出 | Excel | Admin | 空库 | ✅ |
| US-PAT-013 | **敏感脱敏** | 身份证/电话 | 全员 | 传输/存储 | ✅ |
| US-PAT-014 | by-id-number | 身份证查询 | 全员 | 不存在 | ✅ |

---

## 四、挂号（US-REG-001~008 = 8 个故事）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-REG-001 | 前台创建挂号 | **Receptionist only**；Waiting | Receptionist | 其他角色→403 | ✅ |
| US-REG-002 | 快速就诊 | **QuickVisit 原子事务** | Doctor | 事务性 | ✅ |
| US-REG-003 | 挂号详情 | 按 ID | Receptionist | 不存在 | ✅ |
| US-REG-004 | 分页+队列 | 排队查看 | Receptionist | 状态过滤 | ✅ |
| US-REG-005 | 开始就诊 | Waiting→InProgress | Doctor | 状态边界 | ✅ |
| US-REG-006 | 取消挂号 | **仅当天 Waiting** | Receptionist | 非当天/已就诊→422 | ✅ |
| US-REG-007 | 医案联动 | 完成/取消自动回写 | 系统 | 联动一致性 | ⏳ |
| US-REG-008 | 工作台实时列表 | 待诊实时 | Doctor | 实时 | ⏳ |

---

## 五、医案与处方（US-MC-001~020 = 20 个故事）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-MC-001 | 创建医案 | **Doctor only** | Doctor | 其他→403 | ✅ 已证 |
| US-MC-002 | 保存（聚合） | 诊断+处方原子 | Doctor | 事务 | ✅ |
| US-MC-003 | 处方需求标志 | 3 步工作流 | Doctor | 标志切换 | ✅ |
| US-MC-004 | 详情 | 诊断+处方 | Doctor/Admin | 非本人 | ✅ |
| US-MC-005 | 分页列表 | **按角色过滤** | Doctor/Admin | 过滤边界 | ✅ |
| US-MC-006 | 统一查询 | ByPatient/Pending/Recent | Doctor | 各查询类型 | ✅ |
| US-MC-007 | 跨模块搜索 | 患者+诊断+日期 | Doctor | 组合条件 | ✅ |
| US-MC-008/009 | 诊断/处方历史 | 患者历史 | Doctor | 空历史 | ⏳ |
| US-MC-010 | 更新状态 | Active/Suspended | Doctor | 状态机 | ✅ |
| US-MC-011 | 完成医案 | **工作流验证** | Doctor | 未完成处方→422 | ✅ |
| US-MC-012 | 强制关闭 | 管理操作 | Admin | EditReason | ✅ |
| US-MC-013 | 暂停 | 状态 | Doctor | — | ✅ |
| US-MC-014 | 取消 | 物理删除 | Doctor | 权限 | ✅ |
| US-MC-015 | 删除/批量 | 软删除管理 | Admin | 上限 | ✅ |
| US-MC-016 | 查询权限 | 操作权限位 | 全员 | 权限矩阵 | ✅ |
| US-MC-017 | 审计日志 | 20 字段 | Admin | 差异 | ✅ |
| US-MC-018 | 批量详情 | ≤50 N+1 | Doctor | 上限 | ✅ |
| US-MC-019 | 复制上次处方 | 微调 | Doctor | 无上次→提示 | ✅ |
| US-MC-020 | 批量删除 | 已实现未文档化 | Admin | 上限 | ✅ |

---

## 六、报表（US-REPORT-001~004 = 4 个故事）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-REPORT-001 | 收入报表 | 时间范围/默认当日 | Admin | 空数据/跨月 | ✅ 部分 |
| US-REPORT-002 | 就诊统计 | 时间范围 | Admin | 空数据 | ✅ |
| US-REPORT-003 | 药材使用排行 | 时间范围 | Admin | 空数据 | ✅ |
| US-REPORT-004 | 趋势与绩效 | 分析 | Admin | 无数据 | ✅ |
| — | **报表权限** | 非 Admin 访问→403 | 各角色 | 权限边界 | ⏳ |

---

## 七、系统维护（configuration 6 + observability 16 + shell 相关 = 22 个故事）
| US | 故事 | 关键 AC | 角色 | 边界 | 状态 |
|----|------|--------|------|------|:---:|
| US-CFG-001 | 查询所有配置 | **敏感脱敏** | sysadmin | 敏感黑名单 | ✅ |
| US-CFG-002 | 查询单个 | 按 key | sysadmin | 不存在 | ✅ |
| US-CFG-003 | 验证生产配置 | ValidateOrThrow | sysadmin | 缺失→报错 | ✅ |
| US-CFG-004 | 功能开关 | FeatureToggles | sysadmin | 开关生效 | ✅ |
| US-CFG-005 | 配置管理端点 | GET/PUT | sysadmin | 白名单/黑名单 | ✅ |
| US-CFG-006 | 诊所信息热更新 | 热更新 | sysadmin | 无重启生效 | ✅ |
| US-SYS-001/002 | /health /ping | 匿名探针 | 匿名 | 无认证 | ✅ |
| US-SYS-003 | /details | 含 DB | 匿名 | DB 状态 | ✅ |
| US-SYS-004 | 503 返回 | 不健康→503 | 匿名 | 模拟故障 | ⏳ |
| US-SYS-005~009 | 日志级别/调试模式 | 动态调整/自动过期 | sysadmin | 定时过期 | ⏳ |
| US-SHELL-017 | 生产门控 | AllowAutoCreate=false 等 | sysadmin | 安全 | ✅ 已证 |
| US-SHELL-020 | 部署上传/重启 | upload/restart | sysadmin | 谨慎端点 | ✅ |
| — | **系统端点权限** | 非 sysadmin→403 | 各角色 | 权限 | ⏳ |

---

## 八、横切测试（跨所有故事）

| 横切项 | 覆盖方式 | 状态 |
|--------|---------|:---:|
| 无 token → 401 | 每域代表端点 | ✅ 4 端点 |
| 角色矩阵抽查 | 每域 1 个「仅特定角色」端点，其他角色 403 | ⏳ |
| 输入校验 | 缺字段 400 / 超长 400 / 非法枚举 | ⏳ |
| 404 语义 | 不存在 ID | ✅ 部分 |
| 分页边界 | page=0 / pageSize=101 / 超大 | ⏳ |
| 登录锁定 | 5 次错 → 15 分锁 | ⏳ |
| 敏感脱敏 | 患者身份证/配置密码 | ⏳ |
| 状态机流转 | 挂号/医案状态流转 | ⏳ |

---

## 九、执行阶段（依赖排序）

```
Phase 1（✅ 已完成）：sysadmin 冒烟 + 用户模块 + formula 修复 + CreateUser 层级
Phase 2：域 A 剩余（Admin 层级/current/批量/锁定/限流）+ 域 B 剩余（批量导入导出/验证流/Doctor 权限）
Phase 3：域 C 剩余（脱敏/by-id-number）+ 域 D 挂号（先建 testrecep）
Phase 4：域 E 医案处方（先建 testdoctor——Admin 创建，验证层级）
Phase 5：域 F 报表 + 域 G 系统维护（谨慎端点最后）
Phase 6：横切（角色矩阵/输入/分页/状态机）
```

## 十、需要先做

1. **创建 testdoctor / testrecep**（用 testadmin——顺带验证 Admin 层级）
2. 批量导入 Excel 模板准备（herbs/patients/formulas）
3. 谨慎端点（restart/upload）标注——放最后
4. 每域完成更新 api-manual-test 报告

---

## 十一、验收标准

- [ ] WebAPI 可测 US（~120 个）全部覆盖（每个至少 1 主路径用例）
- [ ] 边界覆盖：每 US 至少测「主路径 + 权限 + 输入/数据边界」之一
- [ ] 四角色矩阵验证：sysadmin 业务 403 / Admin 业务 200 / Doctor 开方 200 / Receptionist 挂号 200
- [ ] 发现的 bug 全部进 backlog 或修复
- [ ] 测试报告完整沉淀（api-manual-test-2026-08-13.md）

---

**请审核**——重点确认：
1. User Story 组织方式是否符合预期
2. 边界覆盖是否完整（8 类边界）
3. 各域 US 取舍（哪些 Desktop-only 不测）
4. 谨慎端点（restart/upload）测不测

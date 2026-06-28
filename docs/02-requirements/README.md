# 需求文档 (02-requirements)

> 版本: v2.1 | 日期: 2026-06-28 | 状态: ✅ 已更新（v1.0 范围冻结）

## v1.0 范围决策（2026-06-28）

基于 PRD-代码对账矩阵（D1-D10）+ Sync/N1 + SHELL 扩展判定，以下 US 状态已更新：

| US ID / 决策 | 标题 | 决策 | 状态 |
|-------|------|------|------|
| D1 / US-MC-017 | 医案审计日志 | A 补回 v1.0 | 🧲 v1.0 待实现 |
| D2 / US-PRINT-004 | 打印保护/回写（IsPrinted/PrintVersion/PrintCount/LastPrintedAt） | A 补回 v1.0 | 🧲 v1.0 待实现 |
| D3 / US-AUTH-006 | 令牌族旋转撤销 | B+ 补回 v1.0（重放检测 v2.0） | 🧲 v1.0 待实现 |
| D3 / US-AUTH-007 | 安全审计日志 | B+ 补回 v1.0 | 🧲 v1.0 待实现 |
| D3 / US-AUTH-013 | 本地限流 | B+ 补回 v1.0 | 🧲 v1.0 待实现 |
| D4 / US-USER-011 等 | Restore 软删除恢复（Users/Patients/Herbs/Formulas） | A 补回 v1.0（基础设施已就绪） | 🧲 v1.0 待实现 |
| D5 / BR-DEL-001 | 引用检查（Patients 单删 / Herbs） | A 必做 v1.0 | 🧲 v1.0 待实现 |
| D6 / US-HERB-006 等 | Excel 导入导出（Herbs 补 Excel；Patients 导入 v2.0） | A Herbs 补 v1.0 | 🧲 v1.0 待实现 |
| D7 | 权限策略错配（Registration/Patients/Herbs/MC） | A 按文档修代码（文档权威） | ⚠️ 代码待对齐 |
| D8 | P0 数据/安全 Bug（7 个） | A 全修 | ⚠️ 代码待对齐 |
| D9 / US-MC-008 | 诊断历史聚合 | A 补回 v1.0 | 🧲 v1.0 待实现 |
| D9 / US-MC-009 | 处方历史聚合 | A 补回 v1.0 | 🧲 v1.0 待实现 |
| D10 | 字段级加密 | 拉回 v1.0 | 🧲 v1.0 待实现 |
| Sync 整模块 | 数据同步（8 US） | v2.0（v1.0 数据孤立 N1） | v2.0 规划 |
| SHELL-010~019 | Shell 扩展 10 项 | 010/011/013/014/016/017/018/019 = v1.0；012 = v2.0；015 = 撤销并入 013 | 见 11-platform.md |
| US-SHELL-003 | 模块加载 | A 修复 v1.0 | ⚠️ v1.0 修复 |

## 文件索引

| 文件 | 模块 | US 数 | 状态 |
|------|------|-------|------|
| [01-prd.md](01-prd.md) | 顶层 PRD | — | ✅ 已完成 |
| [02-auth.md](02-auth.md) | 认证与会话 | 13 | ✅ 已完成 |
| [03-users.md](03-users.md) | 用户管理 | 12 | ✅ 已完成 |
| [04-patients.md](04-patients.md) | 患者管理 | 13 | ✅ 已完成 |
| [05-herbs.md](05-herbs.md) | 药材管理 | 13 | ✅ 已完成 |
| [06-formulas.md](06-formulas.md) | 验方管理 | 13 | ✅ 已完成 |
| [07-medical-cases.md](07-medical-cases.md) | 医案管理（核心聚合根） | 19 | ✅ 已完成 |
| [08-registration.md](08-registration.md) | 挂号管理 | 8 | ✅ 已完成 |
| [09-printing.md](09-printing.md) | 处方打印 | 4 | ✅ 已完成 |
| [11-platform.md](11-platform.md) | 平台基础设施（Shell/Config/Err/Log/Sys/Card） | 43 | ✅ 已完成 |
| [12-nfr.md](12-nfr.md) | 非功能需求 | — | ✅ 已完成 |
| [13-traceability-matrix.md](13-traceability-matrix.md) | 需求追溯矩阵（US→ADR/Flow/API/实现） | 138 | ✅ 已完成 |
| **合计** | | **138** | |

## US 编号体系

所有需求统一使用 `US-{DOMAIN}-{NNN}` 格式（User Story）。原 `FR-` 前缀已全部迁移为 `US-`，编号保持不变。`NFR-` 前缀（非功能需求）保留不变。

| 前缀 | 域 | 文件 |
|------|-----|------|
| US-AUTH | 认证与会话 | 02-auth.md |
| US-USER | 用户管理 | 03-users.md |
| US-PAT | 患者管理 | 04-patients.md |
| US-HERB | 药材管理 | 05-herbs.md |
| US-FORM | 验方管理 | 06-formulas.md |
| US-MC | 医案管理 | 07-medical-cases.md |
| US-REG | 挂号管理 | 08-registration.md |
| US-PRINT | 处方打印 | 09-printing.md |
| US-SHELL | 平台-Shell | 11-platform.md |
| US-CFG | 平台-配置 | 11-platform.md |
| US-ERR | 平台-异常处理 | 11-platform.md |
| US-LOG | 平台-日志审计 | 11-platform.md |
| US-SYS | 平台-健康诊断 | 11-platform.md |
| US-CARD | 平台-读卡器 | 11-platform.md |

## US 总览（138 项）

### 认证与会话（US-AUTH × 13）

| US ID | 标题 |
|-------|------|
| US-AUTH-001 | 用户名密码登录 |
| US-AUTH-002 | 登录失败锁定 |
| US-AUTH-003 | 登录限流 |
| US-AUTH-004 | 令牌刷新 |
| US-AUTH-005 | 令牌验证 |
| US-AUTH-006 | 重放攻击检测（令牌族撤销） |
| US-AUTH-007 | 安全审计日志 |
| US-AUTH-008 | 登出（含过期令牌） |
| US-AUTH-009 | 本地自动登录（AutoLoginToken） |
| US-AUTH-010 | AutoLoginToken 轮换 |
| US-AUTH-011 | 保留用户名拦截 |
| US-AUTH-012 | 本地简化认证（1 年令牌） |
| US-AUTH-013 | 本地登录限流（5 次/分） |

### 用户管理（US-USER × 12）

| US ID | 标题 |
|-------|------|
| US-USER-001 | 分页查询用户列表 |
| US-USER-002 | 查看用户详情 |
| US-USER-003 | 查看当前用户资料 |
| US-USER-004 | 创建用户 |
| US-USER-005 | 更新用户（用户名不可变） |
| US-USER-006 | 删除用户（软删除，不可删自己） |
| US-USER-007 | 重置用户密码（SuperAdmin） |
| US-USER-008 | 修改个人资料（IDOR 防护） |
| US-USER-009 | 修改密码（需旧密码） |
| US-USER-010 | 启用/禁用用户 |
| US-USER-011 | 恢复软删除用户（SuperAdmin） |
| US-USER-012 | 批量操作（删除/启用/禁用） |

### 患者管理（US-PAT × 13）

| US ID | 标题 |
|-------|------|
| US-PAT-001 | 分页查询患者列表 |
| US-PAT-002 | 查看患者详情 |
| US-PAT-003 | 创建患者 |
| US-PAT-004 | 更新患者 |
| US-PAT-005 | 删除患者（软删除，引用检查） |
| US-PAT-006 | 启用/禁用患者 |
| US-PAT-007 | 恢复软删除患者 |
| US-PAT-008 | 批量删除患者 |
| US-PAT-009 | 单个引用检查 |
| US-PAT-010 | 批量引用检查 |
| US-PAT-011 | 下载导入模板 |
| US-PAT-012 | 导出患者 Excel |
| US-PAT-013 | 敏感数据脱敏（存储与传输） |

### 药材管理（US-HERB × 13）

| US ID | 标题 |
|-------|------|
| US-HERB-001 | 分页查询药材列表 |
| US-HERB-002 | 查看药材详情 |
| US-HERB-003 | 创建药材 |
| US-HERB-004 | 更新药材 |
| US-HERB-005 | 删除药材（软删除，引用检查） |
| US-HERB-006 | 批量导入药材（Skip/Update/Error 策略） |
| US-HERB-007 | 导出全部药材 |
| US-HERB-008 | 单个引用检查 |
| US-HERB-009 | 批量引用检查 |
| US-HERB-010 | 启用/禁用药材 |
| US-HERB-011 | 恢复软删除药材 |
| US-HERB-012 | 批量操作（启用/禁用/删除） |
| US-HERB-013 | 导出 Excel + 下载模板 |

### 验方管理（US-FORM × 13）

| US ID | 标题 |
|-------|------|
| US-FORM-001 | 分页查询验方列表（按所有权） |
| US-FORM-002 | 查看验方详情 |
| US-FORM-003 | 创建验方（Draft 初始状态） |
| US-FORM-004 | 更新验方（触发状态重新评估） |
| US-FORM-005 | 删除验方（软删除） |
| US-FORM-006 | 批量导入验方 |
| US-FORM-007 | 查询待验证验方（Doctor to-do） |
| US-FORM-008 | 验证单个药材（绑定系统药材） |
| US-FORM-009 | 全部药材验证后自动晋升 Validated |
| US-FORM-010 | 药材变更后降级 Draft（FLAW-F1） |
| US-FORM-011 | 启用/禁用验方 |
| US-FORM-012 | 恢复软删除验方 |
| US-FORM-013 | 批量操作 + 导出 + 模板 |

### 医案管理（US-MC × 19，核心聚合根）

| US ID | 标题 |
|-------|------|
| US-MC-001 | 创建医案（含诊断+处方聚合） |
| US-MC-002 | 保存医案（统一聚合保存） |
| US-MC-003 | 设置处方需求标志（3步工作流第2步） |
| US-MC-004 | 查询医案详情（含诊断+处方） |
| US-MC-005 | 分页查询医案列表（按角色过滤） |
| US-MC-006 | 统一查询（ByPatient/Pending/Recent 等） |
| US-MC-007 | 跨模块搜索（患者+诊断+日期） |
| US-MC-008 | 查询诊断历史 |
| US-MC-009 | 查询处方历史 |
| US-MC-010 | 更新医案状态（Active/Suspended） |
| US-MC-011 | 完成医案（工作流验证） |
| US-MC-012 | 强制关闭医案 |
| US-MC-013 | 暂停医案 |
| US-MC-014 | 取消医案（软删除+打印保护） |
| US-MC-015 | 删除/批量删除医案 |
| US-MC-016 | 查询医案权限 |
| US-MC-017 | 查询审计日志（20字段差异） |
| US-MC-018 | 批量详情查询（≤50，解决 N+1） |
| US-MC-019 | 复制上次处方微调（D6） |

### 挂号管理（US-REG × 8）

| US ID | 标题 |
|-------|------|
| US-REG-001 | 前台创建挂号（Waiting 排队） |
| US-REG-002 | 医生快速就诊（QuickVisit 原子事务） |
| US-REG-003 | 查看挂号详情 |
| US-REG-004 | 分页查询挂号 + 查看排队 |
| US-REG-005 | 开始就诊（Waiting→InProgress） |
| US-REG-006 | 取消挂号（仅 Waiting） |
| US-REG-007 | 医案联动（完成/取消自动回写） |
| US-REG-008 | 医生工作台待诊列表实时更新（SignalR） |

### 处方打印（US-PRINT × 4）

| US ID | 标题 |
|-------|------|
| US-PRINT-001 | 打印处方（A5/A4，对话框/直打印） |
| US-PRINT-002 | 处方预览 |
| US-PRINT-003 | 导出处方（XPS/PDF） |
| US-PRINT-004 | 打印记录回写服务器（成功/失败） |

### 平台基础设施（US-SHELL/CFG/ERR/LOG/SYS/CARD × 43）

#### Shell（v1.0 有效 13：原 5 + SHELL-010~019 补充 8；另有 012=v2.0、015=撤销）

| US ID | 标题 |
|-------|------|
| US-SHELL-001 | 应用启动（单实例） |
| US-SHELL-003 | 角色基础模块加载 |
| US-SHELL-004 | 账户设置（个人资料+密码） |
| US-SHELL-005 | 菜单导航 |
| US-SHELL-007 | 双模式连接切换 |
| US-SHELL-010 | Desktop 安装（Velopack）🧲 v1.0 |
| US-SHELL-011 | 首次初始化向导 🧲 v1.0 |
| US-SHELL-012 | Desktop 自动更新（v2.0 规划） |
| US-SHELL-013 | 数据库备份恢复（含备份状态+手动备份，原 015 并入）🧲 v1.0 |
| US-SHELL-014 | 安全审计日志查看 🧲 v1.0 |
| ~~US-SHELL-015~~ | ~~备份状态与手动备份~~（撤销，并入 013） |
| US-SHELL-016 | 配置导出/导入 🧲 v1.0 |
| US-SHELL-017 | 生产环境安全门控（v1.0 ✅） |
| US-SHELL-018 | sysadmin 配置中心 🧲 v1.0 |
| US-SHELL-019 | 读卡器诊断测试工具 🧲 v1.0 |

#### Configuration（4）

| US ID | 标题 |
|-------|------|
| US-CFG-001 | 查询所有配置 |
| US-CFG-002 | 查询单个配置 |
| US-CFG-003 | 验证生产配置 |
| US-CFG-004 | 功能开关 |

#### Error Handling（8）

| US ID | 标题 |
|-------|------|
| US-ERR-001 | 全局异常处理（Dispatcher+AppDomain） |
| US-ERR-002 | 中文友好错误消息 |
| US-ERR-003 | 追踪 ID（TraceId） |
| US-ERR-004 | CorrelationId 端到端追踪 |
| US-ERR-005 | 生产环境堆栈屏蔽 |
| US-ERR-006 | 验证错误统一格式（422） |
| US-ERR-007 | 业务异常分类 |
| US-ERR-008 | 异常层级（Validation/NotFound/Conflict → Business） |

#### Logging & Audit（7）

| US ID | 标题 |
|-------|------|
| US-LOG-001 | 结构化日志（Serilog） |
| US-LOG-002 | 两阶段 Serilog 引导 |
| US-LOG-003 | 敏感数据脱敏 |
| US-LOG-004 | 审计日志（可配置保留期） |
| US-LOG-005 | 日志级别动态调整 |
| US-LOG-006 | CorrelationId 注入 |
| US-LOG-007 | 日志自动清理（默认 365 天） |

#### Health & Diagnostics（9）

| US ID | 标题 |
|-------|------|
| US-SYS-001 | 匿名存活探针（/health） |
| US-SYS-002 | Ping 端点（/ping） |
| US-SYS-003 | 详细健康检查（/details，含 DB） |
| US-SYS-004 | 健康状态 503 返回 |
| US-SYS-005 | 日志级别状态查询 |
| US-SYS-006 | 启用调试模式（定时，≤120 分钟） |
| US-SYS-007 | 禁用调试模式 |
| US-SYS-008 | 设置显式日志级别 |
| US-SYS-009 | 调试模式自动过期 |

#### Card Reader（2）

| US ID | 标题 |
|-------|------|
| US-CARD-001 | 身份证读卡（初始化+读取+自动读） |
| US-CARD-002 | 患者去重查找或创建（PRD-15） |

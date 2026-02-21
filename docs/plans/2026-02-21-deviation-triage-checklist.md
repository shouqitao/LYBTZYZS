# PRD vs Code 偏差分类确认清单

> **创建时间**: 2026-02-21
> **确认状态**: **已确认** (2026-02-21 用户逐项确认完毕)
> **输入文档**: `docs/plans/2026-02-21-prd-code-deep-scan-report.md` (257 项偏差)
> **实际条目**: 259 项 (medical-cases P3 实际 9 项 +1, sync P2 实际 9 项 +1)
> **分类维度**: CODE (代码修复) / PRD (PRD 修订) / DEFER (延期)

---

## 一、分类统计摘要

### 1.1 总体分类分布

| 分类 | 数量 | 占比 | 说明 |
|------|------|------|------|
| **CODE** | **201** | **77.6%** | 需要代码侧修复 |
| **PRD** | **40** | **15.4%** | 需要 PRD 文档修订 |
| **DEFER** | **18** | **6.9%** | 延期到后续 Epic/Sprint |
| **合计** | **259** | **100%** | |

### 1.2 按模块分类统计

| 模块 | 总数 | CODE | PRD | DEFER |
|------|------|------|-----|-------|
| auth | 21 | 12 | 5 | 4 |
| users | 30 | 29 | 1 | 0 |
| patients | 28 | 28 | 0 | 0 |
| herbs | 24 | 22 | 2 | 0 |
| formulas | 18 | 17 | 1 | 0 |
| medical-cases | 38 | 32 | 3 | 3 |
| printing | 27 | 23 | 3 | 1 |
| sync | 19 | 8 | 4 | 7 |
| card-reader | 1 | 0 | 1 | 0 |
| desktop-shell | 14 | 8 | 5 | 1 |
| configuration | 8 | 5 | 2 | 1 |
| error-handling | 9 | 6 | 3 | 0 |
| logging | 8 | 4 | 4 | 0 |
| health-diagnostics | 5 | 2 | 3 | 0 |
| nfr | 9 | 5 | 3 | 1 |
| **合计** | **259** | **201** | **40** | **18** |

### 1.3 按严重度分类统计

| P级 | CODE | PRD | DEFER | 合计 |
|-----|------|-----|-------|------|
| P1 | 62 | 2 | 8 | 72 |
| P2 | 106 | 7 | 8 | 121 |
| P3 | 33 | 31 | 2 | 66 |
| **合计** | **201** | **40** | **18** | **259** |

> P1-CODE (62项) 是最高优先级修复工作量。P3-PRD (31项) 主要是文档层面的细节对齐。

---

## 二、横切面分类表

| # | 横切面 | 分类 | 命中偏差数 | 涉及模块 | 理由 |
|---|--------|------|-----------|---------|------|
| X1 | 错误码体系脱节 | **CODE** | ~15 | auth, users, patients, herbs, formulas, medical-cases, sync, error-handling | 统一到 MCCEE 5 位格式，Service 层使用 ErrorCode 枚举 |
| X2 | 本地模式功能缺口 | **CODE** | ~22 | auth, users, patients, herbs, formulas, printing, desktop-shell, nfr | v1.0 阶段本地模式需要完整功能 |
| X3 | Token Family 撤销 | **CODE** | ~6 | auth, users | 安全漏洞，5 场景必须修复 |
| X4 | Service 层硬编码字符串 | **CODE** | ~6 | auth, users, herbs, formulas, logging, nfr | X1 子集，枚举已就绪 |
| X5 | 字段验证值不一致 | **逐项** | ~14 | auth, users, patients, herbs, formulas, medical-cases, desktop-shell, configuration, nfr | 密码长度 CODE / Price 范围 PRD / 字段长度逐一确认 |
| X6 | 分页筛选内存过滤 | **CODE** | ~5 | users, herbs, formulas, medical-cases | 技术实现问题，Where 条件移到 IQueryable 链 |
| X7 | 引用检查 CanDelete=true | **CODE** | ~10 | patients, herbs | 数据完整性问题，实现实际引用计数查询 |
| X8 | 打印层级重构 | **CODE** | ~16 | medical-cases, printing | PRD v2.3 已设计，文档已更新 |

---

## 三、逐模块逐偏差分类表

### 3.1 auth 模块 (21 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| AUTH-01 | FR-AUTH-001 | P1 | 单会话登录未实现，登录时未撤销已有 Token Family | CODE | X3 | Token Family 撤销是安全漏洞 |
| AUTH-02 | FR-AUTH-007 | P1 | 登出前警告功能被 simplify-auth 整体移除 | PRD | - | simplify-auth 简化决策已接受，PRD 修订 |
| AUTH-03 | FR-AUTH-001 | P2 | 远程模式 FailedLoginCount 未实现 | CODE | - | 暴力破解防护核心功能 |
| AUTH-04 | FR-AUTH-001 | P2 | UserDisabled 返回 401 应为 403 | CODE | - | HTTP 语义错误 |
| AUTH-05 | FR-AUTH-001 | P2 | 错误码 3 位数 vs PRD 5 位数 | CODE | X1 | 错误码体系统一 |
| AUTH-06 | FR-AUTH-002 | P2 | HMAC 校验失败未清除篡改凭据文件 | CODE | - | 安全漏洞防重放 |
| AUTH-07 | FR-AUTH-003 | P2 | 30 天绝对过期未实现 | CODE | - | 防止 Token 无限续期 |
| AUTH-08 | FR-AUTH-005 | P2 | 服务端登出失败重试队列未实现 | DEFER | - | 重试队列复杂度高，非 MVP 核心 |
| AUTH-09 | FR-AUTH-011 | P2 | TokenExpired 时未尝试 AutoLogin 降级 | CODE | - | 记住密码场景用户体验核心 |
| AUTH-10 | FR-AUTH-013 | P2 | 缺少 4 个 PRD 定义事件 | DEFER | - | 事件总线扩展非 MVP 必要 |
| AUTH-11 | 数据模型 | P2 | AuthSession 实体未实现 | PRD | - | 当前 Token 表够用，独立表过度设计 |
| AUTH-12 | 配置 | P2 | 密码最小长度 PRD=8, 代码=6 | CODE | X5 | 安全基线，8 位是行业最低标准 |
| AUTH-13 | FR-AUTH-001 | P2 | 内部网络限流未区分 | PRD | - | 内外网统一限流更简单 |
| AUTH-14 | FR-AUTH-004 | P3 | TokenRevoked 提示语义差异 | CODE | X1 | 错误码/消息体系统一 |
| AUTH-15 | FR-AUTH-006 | P3 | 触摸事件未追踪 | PRD | - | WPF 触摸追踪过度设计 |
| AUTH-16 | FR-AUTH-008 | P3 | validate 端点不返回剩余有效时间 | DEFER | - | 非 MVP 必要 |
| AUTH-17 | FR-AUTH-008 | P3 | 过期 Token 错误码不精确 | CODE | X1 | 应区分 Expired vs Invalid |
| AUTH-18 | FR-AUTH-009 | P3 | "记住密码"未自动勾选"记住用户名" | CODE | - | 逻辑 Bug |
| AUTH-19 | FR-AUTH-010 | P3 | 状态名称差异 | PRD | - | 代码命名更精确 |
| AUTH-20 | FR-AUTH-010 | P3 | 本地模式简化版状态机未实现 | CODE | X2 | 本地模式功能缺口 |
| AUTH-21 | FR-AUTH-012 | P3 | "记住密码"后无安全警告文案 | DEFER | - | UI 文案非 MVP 核心 |

> auth 小计: CODE=12, PRD=5, DEFER=4, BOTH=0

---

### 3.2 users 模块 (30 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| USER-01 | FR-USER-001 | P1 | CanManageUser 遗漏 Receptionist | CODE | - | 权限矩阵缺失 Bug |
| USER-02 | FR-USER-004 | P1 | 角色变更后未撤销 Token Family | CODE | X3 | 安全漏洞 |
| USER-03 | FR-USER-004 | P1 | CanManageUser 遗漏 Receptionist (角色变更) | CODE | - | 同 USER-01 |
| USER-04 | FR-USER-005 | P1 | 单条删除缺少"不能删除自己"检查 | CODE | - | 管理员可删除自己导致系统锁死 |
| USER-05 | FR-USER-005 | P1 | 删除后未撤销 Token Family | CODE | X3 | 已删除用户 Token 仍有效 |
| USER-06 | FR-USER-008 | P1 | 重置密码后未撤销 Token Family | CODE | X3 | 密码重置后旧会话应失效 |
| USER-07 | FR-USER-009 | P1 | ChangePasswordAsync 未调用 PasswordPolicyValidator | CODE | - | 密码策略绕过是安全漏洞 |
| USER-08 | FR-USER-009 | P1 | 修改密码后未撤销 Token Family | CODE | X3 | 密码修改后旧会话应失效 |
| USER-09 | FR-USER-009 | P1 | 密码哈希 BUG (旧密码覆盖新密码) | CODE | - | 明确的代码 Bug |
| USER-10 | FR-USER-009 | P1 | Desktop ChangePasswordAsync 占位实现 | CODE | X2 | 本地模式功能缺口 |
| USER-11 | FR-USER-009 | P1 | AdminOnly 阻止非管理员修改自己密码 | CODE | - | 权限过严 Bug |
| USER-12 | FR-USER-010 | P1 | AdminOnly 阻止非管理员修改个人资料 | CODE | - | 权限过严 Bug |
| USER-13 | FR-USER-011 | P1 | ToggleStatus 未检查最后管理员保护 | CODE | - | 禁用最后管理员导致系统锁死 |
| USER-14 | FR-USER-011 | P1 | 禁用用户后未撤销 Token Family | CODE | X3 | 禁用用户 Token 仍有效 |
| USER-15 | FR-USER-011 | P1 | BatchUpdateStatus 缺少权限检查和最后管理员保护 | CODE | - | 批量操作缺少安全检查 |
| USER-16 | FR-USER-012 | P1 | GetCurrentUser 继承 AdminOnly | CODE | - | 任何已认证用户应可查询自己信息 |
| USER-17 | 全局 | P1 | 结构化错误码未使用 (硬编码字符串) | CODE | X4 | ErrorCode 枚举已定义但未使用 |
| USER-18 | FR-USER-001 | P2 | 用户名重复未返回 409 | CODE | X1 | 错误码体系脱节 |
| USER-19 | FR-USER-001 | P2 | 密码最小长度 6 位 vs PRD 8 位 | CODE | X5 | 安全基线要求 |
| USER-20 | FR-USER-002 | P2 | role/status 筛选内存过滤 | CODE | X6 | 分页不准确 |
| USER-21 | FR-USER-005 | P2 | LocalUserDataSource 删除保护不完整 | CODE | X2 | 本地模式功能缺口 |
| USER-22 | FR-USER-006 | P2 | IUserDataSource 缺少 RestoreAsync | CODE | X2 | 本地模式功能缺口 |
| USER-23 | FR-USER-007 | P2 | IUserDataSource 缺少 BatchDeleteAsync | CODE | X2 | 本地模式功能缺口 |
| USER-24 | FR-USER-008 | P2 | MustChangeOnNextLogin 标记被忽略 | CODE | - | 密码重置后应强制修改 |
| USER-25 | FR-USER-008 | P2 | IUserDataSource 缺少 ResetPasswordAsync | CODE | X2 | 本地模式功能缺口 |
| USER-26 | FR-USER-010 | P2 | ChangeProfileAsync 未重新生成 PinYinCode | CODE | - | 姓名修改后拼音码不同步 |
| USER-27 | FR-USER-011 | P2 | LocalUserDataSource 状态切换保护不完整 | CODE | X2 | 本地模式功能缺口 |
| USER-28 | FR-USER-011 | P2 | IUserDataSource 缺少批量启用/禁用 | CODE | X2 | 本地模式功能缺口 |
| USER-29 | FR-USER-012 | P2 | 本地模式 GetCurrentUser 缺失 | CODE | X2 | 本地模式功能缺口 |
| USER-30 | FR-USER-009 | P3 | 旧密码错误消息差异 | PRD | - | 代码提示更精确 |

> users 小计: CODE=29, PRD=1, DEFER=0, BOTH=0

---

### 3.3 patients 模块 (28 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| PAT-01 | FR-PAT-013 | P1 | 患者状态管理功能完全缺失 | CODE | - | 核心功能，Receptionist 筛选依赖 |
| PAT-02 | FR-PAT-001 | P1 | 身份证号必填+唯一性检查未实现 | CODE | - | 数据完整性核心 |
| PAT-03 | FR-PAT-004 | P1 | 更新时手机号唯一性检查缺失 | CODE | - | 更新路径与创建路径应一致 |
| PAT-04 | FR-PAT-004 | P1 | 更新时身份证号唯一性检查缺失 | CODE | - | 同上 |
| PAT-05 | FR-PAT-005 | P1 | 删除时引用检查未调用 | CODE | X7 | 有医案的患者可被删除 |
| PAT-06 | FR-PAT-007 | P1 | 批量删除缺少引用检查 | CODE | X7 | 同 PAT-05 批量路径 |
| PAT-07 | FR-PAT-008 | P1 | 本地模式批量导入返回 null | CODE | X2 | 本地模式功能缺口 |
| PAT-08 | FR-PAT-010 | P1 | 本地模式导出返回 null | CODE | X2 | 本地模式功能缺口 |
| PAT-09 | FR-PAT-011 | P1 | Controller 缺少 check-reference 端点 | CODE | X7 | 删除前校验的前提 |
| PAT-10 | FR-PAT-011 | P1 | CheckReferenceAsync CanDelete 硬编码 true | CODE | X7 | 引用检查形同虚设 |
| PAT-11 | FR-PAT-012 | P1 | Controller 缺少 batch-check-reference 端点 | CODE | X7 | 批量路径 |
| PAT-12 | FR-PAT-012 | P1 | BatchCheckReference CanDelete 硬编码 true | CODE | X7 | 同 PAT-10 |
| PAT-13 | FR-PAT-002 | P1 | Receptionist 查询未过滤 Disabled 患者 | CODE | - | 权限控制缺失 |
| PAT-14 | FR-PAT-008 | P1 | 导入时缺少身份证号唯一性检查 | CODE | - | 可绕过唯一性约束 |
| PAT-15 | 错误码 | P1 | ERR-20002 未实现 | CODE | X1 | 错误码体系脱节 |
| PAT-16 | 错误码 | P1 | ERR-20004 未实现 | CODE | X1 | 同上 |
| PAT-17 | 错误码 | P1 | ERR-20005 未实现 | CODE | X1 | 同上 |
| PAT-18 | 错误码 | P1 | ERR-20006 未实现 | CODE | X1 | 同上 |
| PAT-19 | FR-PAT-001 | P2 | 创建 API 返回 200 而非 201 | CODE | - | HTTP 语义错误 |
| PAT-20 | DTO | P2 | IdNumber/PhoneNumber/Address DTO 为 nullable | CODE | X5 | PRD Required 合理 |
| PAT-21 | 权限 | P2 | Receptionist 应有 CRU 权限 | CODE | - | 权限矩阵遗漏 |
| PAT-22 | FR-PAT-005 | P2 | 删除失败返回 404 而非 422 | CODE | X1 | 错误码不精确 |
| PAT-23 | FR-PAT-011 | P2 | Desktop 端无引用检查 | CODE | X2 | 本地模式功能缺口 |
| PAT-24 | FR-PAT-012 | P2 | Desktop 端批量引用检查缺失 | CODE | X2 | 同上 |
| PAT-25 | FR-PAT-001 | P3 | CreateAsync (DTO 版) 缺少手机号唯一性检查 | CODE | - | 备用路径遗漏 |
| PAT-26 | FR-PAT-008 | P3 | 导入行数限制 off-by-one | CODE | - | 轻微 Bug |
| PAT-27 | FR-PAT-009 | P3 | 导入模板 IdNumber 列未标记必填 | CODE | - | 模板与验证规则不一致 |
| PAT-28 | FR-PAT-013 | P3 | PatientStatus/CommonStatus 不一致 | CODE | - | 应复用 CommonStatus |

> patients 小计: CODE=28, PRD=0, DEFER=0, BOTH=0

---

### 3.4 herbs 模块 (24 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| HERB-01 | FR-HERB-005 | P1 | 删除缺少处方引用检查 | CODE | X7 | 有处方引用的药材可被误删 |
| HERB-02 | FR-HERB-008 | P1 | 批量删除未检查处方引用 | CODE | X7 | 同上批量路径 |
| HERB-03 | FR-HERB-013 | P1 | CanDelete 硬编码 true | CODE | X7 | 前端无法禁用删除按钮 |
| HERB-04 | FR-HERB-001 | P2 | CreateAsync 缺少拼音码自动生成 | CODE | - | 导入路径已实现可复用 |
| HERB-05 | FR-HERB-001 | P2 | Price 验证 >0.01 vs PRD >0 | PRD | X5 | 代码 >0.01 更合理 |
| HERB-06 | FR-HERB-001 | P2 | Price 最大值 100000 vs PRD 999999.99 | PRD | X5 | 10 万上限更务实 |
| HERB-07 | FR-HERB-002 | P2 | 分类筛选内存过滤 | CODE | X6 | 分页不准确 |
| HERB-08 | FR-HERB-004 | P2 | 名称变更未重新生成拼音码 | CODE | - | 搜索失效 |
| HERB-09 | FR-HERB-005 | P2 | 删除被引用药材未返回 422 | CODE | X7 | 配套错误码 |
| HERB-10 | FR-HERB-006 | P2 | 本地模式不支持批量启用/禁用 | CODE | X2 | 本地模式功能缺口 |
| HERB-11 | FR-HERB-009 | P2 | 本地模式不支持 Excel 导入 | CODE | X2 | v1.0 本地模式需完整功能 (用户确认) |
| HERB-12 | FR-HERB-010 | P2 | 本地模式不支持 JSON 导入 | CODE | X2 | 同上 |
| HERB-13 | FR-HERB-011 | P2 | 本地模式不支持导出 | CODE | X2 | 同上 |
| HERB-14 | FR-HERB-013 | P2 | 本地模式未实现引用检查 | CODE | X2 | 数据完整性需求 |
| HERB-15 | 全局 | P2 | 错误码编号体系不匹配 | CODE | X1 | 错误码体系脱节 |
| HERB-16 | 全局 | P2 | Service 层未使用 ErrorCode 枚举 | CODE | X4 | 硬编码字符串 |
| HERB-17 | 全局 | P2 | Effect 字段 DTO 1000 vs 实体 500 | CODE | X5 | DTO 过宽导致截断 |
| HERB-18 | 全局 | P2 | Usage 字段 Validator 200 vs PRD 500 | CODE | X5 | Validator 过严拒绝合法输入 |
| HERB-19 | FR-HERB-002 | P3 | 缺少 ERR-50106 | CODE | X1 | 错误码体系统一 |
| HERB-20 | FR-HERB-007 | P3 | 缺少 ERR-50104 | CODE | X1 | 同上 |
| HERB-21 | FR-HERB-010 | P3 | 缺少 ERR-50202 | CODE | X1 | 同上 |
| HERB-22 | FR-HERB-012 | P3 | 本地模式不支持模板下载 | CODE | X2 | 随本地导入一并实现 (用户确认) |
| HERB-23 | 全局 | P3 | Spec 字段 DTO 50 vs PRD 100 | CODE | X5 | DTO 过严截断合法数据 |
| HERB-24 | 全局 | P3 | Unit 字段 DTO 20 vs 实体 10 | CODE | X5 | DTO 过宽导致写入截断 |

> herbs 小计: CODE=22, PRD=2, DEFER=0

---

### 3.5 formulas 模块 (18 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| FORM-01 | FR-FORM-003 | P1 | FormulaMapper 不映射 Herbs 列表 | CODE | - | 功能 Bug，API 返回空药材 |
| FORM-02 | 全局 | P1 | PRD 17 个错误码 vs 代码 6 个不匹配 | CODE | X1 | 错误码体系脱节 |
| FORM-03 | FR-FORM-001 | P2 | Server 未校验 Herbs 列表为空 | CODE | - | 验方无药材无业务意义 |
| FORM-04 | FR-FORM-001 | P2 | Effect DTO=200 vs PRD/Entity=500 | CODE | X5 | DTO 过严拒绝合法输入 |
| FORM-05 | FR-FORM-002 | P2 | TotalPrice 始终为 0 | CODE | - | 功能 Bug |
| FORM-06 | FR-FORM-009 | P2 | Desktop 端已删除延迟绑定验证方法 | CODE | X2 | 本地模式功能缺口 |
| FORM-07 | FR-FORM-010 | P2 | Desktop 端已删除待验证列表方法 | CODE | X2 | 同上 |
| FORM-08 | FR-FORM-011 | P2 | Desktop 端无本地批量导入 | CODE | X2 | v1.0 本地模式需完整功能 (用户确认) |
| FORM-09 | FR-FORM-012 | P2 | 导出 Excel 不含药材组成详情 | CODE | - | 缺少核心信息 |
| FORM-10 | FR-FORM-012 | P2 | Desktop 端无本地导出 | CODE | X2 | v1.0 本地模式需完整功能 (用户确认) |
| FORM-11 | 全局 | P2 | Service 层硬编码字符串 | CODE | X4 | 未使用 ErrorCode |
| FORM-12 | FR-FORM-001 | P2 | Desktop Validator 功效/用法设为必填 vs PRD 选填 | CODE | X5 | 代码比 PRD 更严格 |
| FORM-13 | FR-FORM-001 | P3 | Usage DTO=200 vs FluentValidation=500 | CODE | X5 | 同一字段两处不一致 |
| FORM-14 | FR-FORM-001 | P3 | Entity Name=200 vs PRD/DTO=100 | PRD | X5 | 实体 200 更宽松无害 |
| FORM-15 | FR-FORM-002 | P3 | 分类筛选内存过滤 | CODE | X6 | 分页不准确 |
| FORM-16 | FR-FORM-006 | P3 | 本地模式不支持批量启用/禁用 | CODE | X2 | 基础功能缺口 |
| FORM-17 | FR-FORM-010 | P3 | 待验证列表无分页全量加载 | CODE | X6 | 性能问题 |
| FORM-18 | FR-FORM-013 | P3 | 本地模式无内置导入模板 | CODE | X2 | 随本地导入一并实现 (用户确认) |

> formulas 小计: CODE=17, PRD=1, DEFER=0

---

### 3.6 medical-cases 模块 (38 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| MC-01 | FR-MC-001 | P1 | 缺少患者状态检查，禁用患者可创建医案 | CODE | - | 数据完整性漏洞 |
| MC-02 | FR-MC-005 | P1 | 打印保护逻辑未实现 | CODE | X8 | 打印层级重构核心 |
| MC-03 | FR-MC-005 | P1 | MedicalCase 缺少 IsPrinted/PrintVersion 字段 | CODE | X8 | PRD v2.0 核心字段 |
| MC-04 | FR-MC-005 | P1 | EditReason 未在写操作中强制校验 | CODE | - | 审计追溯核心保障 |
| MC-05 | FR-MC-007 | P1 | 缺少 TcmDiagnosis 非空校验 | CODE | - | 无诊断不应可完成 |
| MC-06 | FR-MC-013 | P1 | EditReason 写操作强制校验缺失 (同源) | CODE | - | 编辑保护核心逻辑 |
| MC-07 | FR-MC-015 | P1 | PrescriptionPrintLog 未重构 | CODE | X8 | C6 确认 |
| MC-08 | FR-MC-001 | P2 | 初始状态 Active 而非 Draft | PRD | - | 保持 Active，UI 层未保存表单替代 Draft (用户确认) |
| MC-09 | FR-MC-001 | P2 | 缺少医案编号自动生成 | CODE | - | 业务标识基础 |
| MC-10 | FR-MC-001 | P2 | 错误码未使用 ERR-3xxxx | CODE | X1 | 错误码体系脱节 |
| MC-11 | FR-MC-003 | P2 | HasPrescription=false 时未清除已有处方 | CODE | - | 数据完整性 |
| MC-12 | FR-MC-004 | P2 | 缺少处方编号自动生成 | CODE | - | 业务追溯基础 |
| MC-13 | FR-MC-004 | P2 | 缺少 Items 为空时的验证 | CODE | - | 空处方无业务意义 |
| MC-14 | FR-MC-005 | P2 | 审计日志中 EditReason 未传递 | CODE | - | 审计完整性 |
| MC-15 | FR-MC-007 | P2 | 缺少 Items.Count>0 校验 | CODE | - | 同 MC-13 |
| MC-16 | FR-MC-008 | P2 | 取消前未自动保存诊断数据 | DEFER | - | UX 复杂度高需独立规划 |
| MC-17 | FR-MC-008 | P2 | 非当天本人取消缺少 Reason 检查 | CODE | - | 审计完整性 |
| MC-18 | FR-MC-009 | P2 | GetListDtoAsync 内存过滤 | CODE | X6 | 分页不准确 |
| MC-19 | FR-MC-011 | P2 | EditModeStateMachine 不存在 | DEFER | - | 状态机复杂度高需独立设计 |
| MC-20 | FR-MC-013 | P2 | RequiresEditReason 遗漏"非本人"和"当天已完成" | CODE | - | 编辑保护场景覆盖不足 |
| MC-21 | FR-MC-015 | P2 | PrintHandler 未设置 IsPrinted=true | CODE | X8 | 打印后回写链断开 |
| MC-22 | FR-MC-015 | P2 | 缺少 PrintCount++ 和 LastPrintedAt | CODE | X8 | 同上 |
| MC-23 | FR-MC-016 | P2 | 验方导入未过滤 ValidationStatus | CODE | - | 未验证验方不应导入 |
| MC-24 | FR-MC-016 | P2 | 验方导入未过滤 Status=Enabled | CODE | - | 禁用验方不应导入 |
| MC-25 | FR-MC-016 | P2 | 验方导入未跳过禁用药材 | CODE | - | 禁用药材进入处方是安全隐患 |
| MC-26 | FR-MC-016 | P2 | 验方导入价格未实时获取 | CODE | - | 价格应以药材库当前值为准 |
| MC-27 | FR-MC-018 | P2 | 历史复制未跳过禁用药材 | CODE | - | 同 MC-25 |
| MC-28 | FR-MC-018 | P2 | 历史复制价格未实时获取 | CODE | - | 同 MC-26 |
| MC-29 | FR-MC-018 | P2 | 历史复制未记录 ReferencedFormulas | CODE | - | 审计追溯 |
| MC-30 | FR-MC-004 | P3 | PrescriptionItem.Usage 错误赋值 | CODE | - | Bug，字段赋值错误 |
| MC-31 | FR-MC-006 | P3 | 错误消息基本一致 | PRD | - | PRD 过度细分错误消息 |
| MC-32 | FR-MC-007 | P3 | 缺少 DosageCount>0 校验 | CODE | X5 | 剂数为 0 无业务意义 |
| MC-33 | FR-MC-011 | P3 | Clinical/Management 模式区分需验证 | DEFER | - | 与 MC-19 同源 |
| MC-34 | FR-MC-012 | P3 | OperationType int 枚举 vs PRD string | PRD | - | int 枚举更高效 |
| MC-35 | FR-MC-012 | P3 | OperatorName MaxLength 50 vs PRD 100 | CODE | X5 | 50 可能截断长名字 |
| MC-36 | FR-MC-012 | P3 | 审计字段少 Prescription.Usage | CODE | - | 审计完整性 |
| MC-37 | FR-MC-017 | P3 | pending 端点缺少 doctorId 参数 | CODE | - | 医生应只看自己的待处理医案 |
| MC-38 | FR-MC-018 | P3 | DosageCount/Discount 未从历史复制 | CODE | - | 复制应包含完整处方 |

> medical-cases 小计: CODE=32, PRD=3, DEFER=3

---

### 3.7 printing 模块 (27 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| PRINT-01 | FR-PRINT-001 | P1 | PrintCount 递增缺失 | CODE | X8 | 打印后回写链断开 |
| PRINT-02 | FR-PRINT-001 | P1 | IsPrinted=true 逻辑缺失 | CODE | X8 | 编辑保护前提 |
| PRINT-03 | FR-PRINT-001 | P1 | LastPrintedAt 更新缺失 | CODE | X8 | 审计追溯基础 |
| PRINT-04 | FR-PRINT-003 | P1 | 打印层级未迁移 (C6 核心) | CODE | X8 | 打印层级重构核心 |
| PRINT-05 | FR-PRINT-003 | P1 | PrintVersion 递增缺失 | CODE | X8 | 打印后编辑追踪核心 |
| PRINT-06 | FR-PRINT-003 | P1 | 版本号快照记录缺失 | CODE | X8 | 版本比对基础 |
| PRINT-07 | FR-PRINT-004 | P1 | MedicalCasePrintLog 未创建 | CODE | X8 | 打印层级重构核心实体 |
| PRINT-08 | FR-PRINT-004 | P1 | PrintType 枚举不存在 | CODE | X8 | 日志分类基础 |
| PRINT-09 | FR-PRINT-004 | P1 | 打印日志写入缺失 | CODE | X8 | 审计合规必要 |
| PRINT-10 | FR-PRINT-004 | P1 | 打印失败日志缺失 | CODE | X8 | 运维排障基础 |
| PRINT-11 | FR-PRINT-004 | P1 | 远程模式无打印日志 API | CODE | X2/X8 | 双模式+打印重构 |
| PRINT-12 | FR-PRINT-004 | P1 | 本地模式打印日志存储缺失 | CODE | X2/X8 | 同上 |
| PRINT-13 | FR-PRINT-001 | P2 | 模板字体楷体 vs PRD 宋体 | CODE | - | PRD 明确规定 |
| PRINT-14 | FR-PRINT-001 | P2 | 模板边距 15mm vs PRD 8mm | CODE | - | 排版不符 |
| PRINT-15 | FR-PRINT-001 | P2 | 诊所信息区缺失 | CODE | - | 处方笺法规要求 |
| PRINT-16 | FR-PRINT-001 | P2 | 诊断信息区不完整 | CODE | - | 应含完整诊断 |
| PRINT-17 | FR-PRINT-001 | P2 | 煎法标注未渲染 | CODE | - | 用药安全 |
| PRINT-18 | FR-PRINT-001 | P2 | 分页规则未实现 (>12 味溢出) | CODE | - | 用药安全 |
| PRINT-19 | FR-PRINT-001 | P2 | 草稿水印未实现 | DEFER | - | 非 MVP 核心 |
| PRINT-20 | FR-PRINT-001 | P2 | DoctorName 未绑定 | CODE | - | 应自动填充 |
| PRINT-21 | FR-PRINT-001 | P2 | 费用计算 Discount 未参与 | CODE | - | 收费准确性 |
| PRINT-22 | FR-PRINT-002 | P2 | A4/A5 排版无差异处理 | CODE | - | 统一参数导致排版异常 |
| PRINT-23 | P3 | P3 | 字号大小与 PRD 微差 | PRD | - | 微小差异可接受 |
| PRINT-24 | P3 | P3 | 药材名称过长截断缺失 | CODE | - | 影响用药安全 |
| PRINT-25 | P3 | P3 | 空处方打印校验缺失 | CODE | - | 防御性校验 |
| PRINT-26 | P3 | P3 | 打印日期格式差异 | PRD | - | 微小差异可接受 |
| PRINT-27 | P3 | P3 | 其他排版细节偏差 | PRD | - | 非功能性微调 |

> printing 小计: CODE=23, PRD=3, DEFER=1, BOTH=0

---

### 3.8 sync 模块 (19 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| SYNC-01 | FR-SYNC-001/005 | P1 | MedicalCase 同步完全未实现 | DEFER | - | 独立 Epic 规划，复杂度极高 |
| SYNC-02 | FR-SYNC-007 | P1 | SyncConflictDetailDto 未实现 | DEFER | - | 依赖同步基础架构 |
| SYNC-03 | FR-SYNC-007 | P1 | 冲突对话框仅展示 Checksum | DEFER | - | 同上 |
| SYNC-04 | FR-SYNC-008 | P1 | 运行时模式切换未实现 | DEFER | - | MVP 阶段登录时选择够用 |
| SYNC-05 | FR-SYNC-008 | P1 | 切换前未同步变更检查 | DEFER | - | 依赖运行时模式切换 |
| SYNC-06 | FR-SYNC-002 | P2 | SyncMetadataDto 缺少字段 | CODE | - | DTO 字段不完整影响前端 |
| SYNC-07 | FR-SYNC-002 | P2 | GetMetadataAsync 未用 IgnoreQueryFilters | CODE | - | 软删除记录同步丢失是 Bug |
| SYNC-08 | FR-SYNC-003 | P2 | ChangedFields 始终为 null | DEFER | - | 变更检测复杂度高 |
| SYNC-09 | FR-SYNC-004 | P2 | 硬编码 OverwriteConflicts=false | CODE | - | 应暴露为配置 |
| SYNC-10 | FR-SYNC-007 | P2 | 同步前检查未实现 (网络/Token) | CODE | - | 直接同步导致体验差 |
| SYNC-11 | FR-SYNC-007 | P2 | 进度 UI 简化为进度条 | PRD | - | 简单进度条 MVP 够用 |
| SYNC-12 | FR-SYNC-007 | P2 | 结果汇总不完整 | CODE | - | 无法定位同步失败原因 |
| SYNC-13 | FR-SYNC-008 | P2 | 切换失败回退策略未实现 | DEFER | - | 依赖运行时模式切换 |
| SYNC-14 | 全局 | P2 | PRD 20 个错误码全部未实现 | CODE | X1 | 错误码体系脱节 |
| SYNC-15 | P3 | P3 | DTO 命名与 PRD 不一致 | PRD | - | 命名属文档层面 |
| SYNC-16 | P3 | P3 | 字段名差异 | PRD | - | 不影响功能 |
| SYNC-17 | P3 | P3 | Checksum 字段类型/长度差异 | CODE | - | 代码对齐 PRD 最新规格 (用户确认) |
| SYNC-18 | P3 | P3 | 状态栏同步标识缺失 | CODE | - | 影响用户体验 |
| SYNC-19 | P3 | P3 | 其他命名规范差异 | PRD | - | 不影响功能 |

> sync 小计: CODE=8, PRD=4, DEFER=7

---

### 3.9 card-reader 模块 (1 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| CARD-01 | FR-CARD-002 | P3 | PRD "姓名->RealName" vs 代码 Name | PRD | - | 代码 Name 更简洁合理 |

> card-reader 小计: CODE=0, PRD=1, DEFER=0, BOTH=0

---

### 3.10 desktop-shell 模块 (14 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| SHELL-01 | FR-SHELL-005 | P1 | 菜单可见性矩阵未实现 | CODE | - | 权限控制核心功能 |
| SHELL-02 | FR-SHELL-002 | P2 | 登出时未清除导航历史 | CODE | - | 切换用户后残留信息 |
| SHELL-03 | FR-SHELL-002 | P2 | 模块加载仅区分 Admin/非 Admin | CODE | - | 角色粒度不足 |
| SHELL-04 | FR-SHELL-003 | P2 | 超时前警告已被 simplify-auth 移除 | PRD | - | 接受移除，PRD 修订 (用户确认) |
| SHELL-05 | FR-SHELL-003 | P2 | InactivityTimeout=5 应为 15 | CODE | X5 | 5 分钟过短影响使用 |
| SHELL-06 | FR-SHELL-004 | P2 | 导航历史无 20 条上限 | CODE | - | 内存持续增长 |
| SHELL-07 | FR-SHELL-005 | P2 | 本地模式菜单不可用逻辑未实现 | CODE | X2 | 本地模式功能缺口 |
| SHELL-08 | FR-SHELL-007 | P2 | 缺少最后登录时间/IP 信息 | DEFER | - | 非 MVP 必要 |
| SHELL-09 | FR-SHELL-007 | P2 | 缺少 Email 编辑支持 | CODE | - | 基本信息编辑功能 |
| SHELL-10 | FR-SHELL-007 | P2 | 本地模式账户设置未分支处理 | CODE | X2 | 本地模式功能缺口 |
| SHELL-11 | P3 | P3 | 状态枚举命名差异 | PRD | - | 代码命名更合理 |
| SHELL-12 | P3 | P3 | 登录协调依赖计数不匹配 | PRD | - | 代码行为更准确 |
| SHELL-13 | P3 | P3 | StartupReport 返回类型差异 | PRD | - | 代码类型更实用 |
| SHELL-14 | P3 | P3 | 启动诊断信息格式差异 | PRD | - | 代码格式更合理 |

> desktop-shell 小计: CODE=8, PRD=5, DEFER=1

---

### 3.11 configuration 模块 (8 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| CFG-01 | FR-CFG-001 | P2 | DefaultRole "Staff" vs PRD "Doctor" | CODE | X5 | 中医诊所默认 Doctor 更合理 |
| CFG-02 | FR-CFG-002 | P2 | ClientSessionOptions 默认值不一致 | CODE | X5 | 与 SHELL-05 同源 |
| CFG-03 | FR-CFG-002 | P2 | FeatureToggle 缺少 CardReaderEnabled | CODE | - | 功能开关缺失 |
| CFG-04 | FR-CFG-004 | P2 | JWT SecretKey 验证方式差异 | CODE | - | 字符串长度不能确保密钥强度 |
| CFG-05 | FR-CFG-004 | P2 | Important 配置缺失错误阻止启动 | CODE | - | Bug，应仅警告 |
| CFG-06 | P3 | P3 | Swagger/Json 注册方式差异 | PRD | - | 实现细节 |
| CFG-07 | P3 | P3 | FeatureToggle 热更新未支持 | DEFER | - | MVP 阶段重启够用 |
| CFG-08 | P3 | P3 | 配置错误输出格式差异 | PRD | - | 不影响功能 |

> configuration 小计: CODE=5, PRD=2, DEFER=1, BOTH=0

---

### 3.12 error-handling 模块 (9 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| ERR-01 | FR-ERR-006 | P1 | ErrorCode 7xxxx 语义不对应 | CODE | X1 | 错误码体系脱节 |
| ERR-02 | FR-ERR-006 | P1 | ClientErrorMessageMapper 无法解析 ERR-10004 | CODE | X1 | 解析 Bug 导致映射失效 |
| ERR-03 | FR-ERR-008 | P1 | 异常到通知类型映射未实现 | CODE | - | 核心功能缺失 |
| ERR-04 | FR-ERR-006 | P2 | HTTP 429 映射缺失 | CODE | X1 | 错误码映射缺口 |
| ERR-05 | FR-ERR-006 | P2 | Token 相关错误码无消息映射 | CODE | X1 | 同上 |
| ERR-06 | FR-ERR-007 | P2 | 追踪码未与 Severity 自动关联 | CODE | - | 影响错误追踪能力 |
| ERR-07 | P3 | P3 | 错误消息文案细节差异 | PRD | - | 文案属过度规范 |
| ERR-08 | P3 | P3 | 错误分类枚举值差异 | PRD | - | 不影响功能 |
| ERR-09 | P3 | P3 | 错误日志格式差异 | PRD | - | 现有格式可接受 |

> error-handling 小计: CODE=6, PRD=3, DEFER=0, BOTH=0

---

### 3.13 logging 模块 (8 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| LOG-01 | FR-LOG-006 | P1 | 审计日志保留 30 天 vs PRD 365 天 | CODE | X4 | 审计合规风险，12 倍差距 |
| LOG-02 | FR-LOG-003 | P2 | SensitiveDataAttribute 两份定义冲突 | CODE | - | 脱敏可能失效是安全 Bug |
| LOG-03 | FR-LOG-006 | P2 | CleanupService 无 Options 配置 | CODE | X4 | 硬编码无配置化 |
| LOG-04 | FR-LOG-006 | P2 | CleanupService 全量加载未分批删除 | CODE | - | 内存溢出风险 |
| LOG-05 | P3 | P3 | 日志级别配置差异 | PRD | - | 运行时可调整 |
| LOG-06 | P3 | P3 | 日志格式模板差异 | PRD | - | 属过度规范 |
| LOG-07 | P3 | P3 | 日志轮转配置差异 | PRD | - | 不影响功能 |
| LOG-08 | P3 | P3 | 结构化日志字段命名差异 | PRD | - | 属过度规范 |

> logging 小计: CODE=4, PRD=4, DEFER=0, BOTH=0

---

### 3.14 health-diagnostics 模块 (5 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| SYS-01 | FR-SYS-003 | P2 | Unhealthy 映射为 "Degraded" | CODE | - | 状态语义错误影响运维 |
| SYS-02 | FR-SYS-003 | P2 | 详细响应缺少字段 | CODE | - | 诊断信息不完整 |
| SYS-03 | P3 | P3 | 健康检查响应格式差异 | PRD | - | 格式细节属过度规范 |
| SYS-04 | P3 | P3 | 健康检查超时配置差异 | PRD | - | 可运行时调整 |
| SYS-05 | P3 | P3 | 诊断端点路径差异 | PRD | - | 不影响功能 |

> health-diagnostics 小计: CODE=2, PRD=3, DEFER=0, BOTH=0

---

### 3.15 nfr 模块 (9 项)

| # | FR编号 | P级 | 偏差描述(简) | 分类 | 横切面 | 理由 |
|---|--------|-----|-------------|------|--------|------|
| NFR-01 | NFR-SEC-004 | P1 | SQLite 字段级加密整体未实现 | DEFER | - | 架构影响面大需独立设计 |
| NFR-02 | NFR-SEC-005 | P1 | 审计日志保留 30 天 vs 365 天 (同 LOG-01) | CODE | X4 | 审计合规风险 |
| NFR-03 | NFR-SEC-001 | P2 | 不活跃超时 5 分钟 vs PRD 15 分钟 | CODE | X5 | 5 分钟过于激进 |
| NFR-04 | NFR-SEC-002 | P2 | 密码过期 30 天 vs 90 天，两处矛盾 | CODE | X5 | 内部配置自相矛盾是 Bug |
| NFR-05 | 缓存 5.3 | P2 | Server 端缓存失效映射未实现 | CODE | - | 缓存一致性问题 |
| NFR-06 | 缓存 5.4 | P2 | Desktop 端写后缓存失效未实现 | CODE | X2 | 本地模式功能缺口 |
| NFR-07 | P3 | P3 | 并发连接数配置差异 | PRD | - | 可运行时调整 |
| NFR-08 | P3 | P3 | 响应时间 SLA 差异 | PRD | - | 需实测确定 |
| NFR-09 | P3 | P3 | 其他 NFR 细节差异 | PRD | - | 非功能性细节 |

> nfr 小计: CODE=5, PRD=3, DEFER=1, BOTH=0

---

## 四、基于分类的修复优先级重排

### 4.1 CODE 类修复优先级 (193 项)

#### Tier 1: 安全漏洞 + 数据完整性 (预计 Sprint 1)

| 优先级 | 横切面/专项 | CODE 偏差数 | 涉及模块 |
|--------|------------|-----------|---------|
| 1 | X3 Token Family 撤销 | ~6 | auth, users |
| 2 | X7 引用检查修复 | ~10 | patients, herbs |
| 3 | 密码哈希 Bug | 1 | users (USER-09) |
| 4 | 权限矩阵修复 (AdminOnly/CanManageUser) | ~8 | users, patients |
| 5 | EditReason 强制校验 | ~4 | medical-cases |

#### Tier 2: 核心功能修复 (预计 Sprint 2)

| 优先级 | 横切面/专项 | CODE 偏差数 | 涉及模块 |
|--------|------------|-----------|---------|
| 6 | X8 打印层级重构 (C6) | ~16 | medical-cases, printing |
| 7 | X5 字段验证值对齐 (CODE 项) | ~10 | 多模块 |
| 8 | 功能 Bug 修复 (Mapper/TotalPrice/Usage 赋值) | ~4 | formulas, medical-cases |
| 9 | 审计日志保留期 (30→365 天) | ~2 | logging, nfr |
| 10 | 患者状态管理 | ~1 | patients |

#### Tier 3: 体系统一 (预计 Sprint 3)

| 优先级 | 横切面/专项 | CODE 偏差数 | 涉及模块 |
|--------|------------|-----------|---------|
| 11 | X1 错误码体系 MCCEE 统一 | ~15 | 全模块 |
| 12 | X4 Service 层 ErrorCode 替代硬编码 | ~6 | users, herbs, formulas, logging |
| 13 | X6 分页筛选迁移到 Repository | ~5 | users, herbs, formulas, medical-cases |

#### Tier 4: 本地模式补齐 (预计 Sprint 4)

| 优先级 | 横切面/专项 | CODE 偏差数 | 涉及模块 |
|--------|------------|-----------|---------|
| 14 | X2 本地模式 IDataSource 补齐 (核心方法) | ~15 | users, herbs, formulas, patients |
| 15 | X2 本地模式导入/导出/模板 (用户确认不延期) | ~7 | herbs, formulas |
| 16 | 打印模板完善 | ~10 | printing |
| 17 | Desktop-shell 功能完善 | ~5 | desktop-shell |

#### Tier 5: 细节完善 (预计 Sprint 5+)

| 内容 | CODE 偏差数 |
|------|-----------|
| P2 功能完善 (编号生成/验方过滤/历史复制等) | ~20 |
| P3 细节修复 (截断/校验/格式等) | ~10 |

### 4.2 PRD 修订清单 (38 项)

需要修订 PRD 文档的条目，按模块分组:

| 模块 | PRD 项数 | 关键修订点 |
|------|---------|-----------|
| auth | 5 | 移除登出前警告(simplify-auth)、AuthSession 独立表、内网限流、触摸事件追踪、状态命名 |
| herbs | 2 | Price 验证 >0.01 vs >0、Price 上限 100000 |
| formulas | 1 | Entity Name 200 vs PRD 100 |
| medical-cases | 2 | 错误消息细分、OperationType 存储格式 |
| printing | 3 | 字号微差、日期格式、排版细节 |
| sync | 4 | 进度 UI 4 步简化、DTO 命名、字段名、命名规范 |
| card-reader | 1 | RealName vs Name |
| desktop-shell | 4 | 状态枚举、登录协调计数、StartupReport 类型、启动诊断格式 |
| configuration | 2 | Swagger 注册方式、错误输出格式 |
| error-handling | 3 | 错误文案、枚举值、日志格式 |
| logging | 4 | 日志级别/格式/轮转/字段命名 |
| health-diagnostics | 3 | 响应格式、超时配置、端点路径 |
| nfr | 3 | 并发连接数、响应时间 SLA、其他细节 |
| users | 1 | 旧密码错误消息 |

### 4.3 DEFER 延期清单 (18 项)

| 延期分组 | 偏差数 | 独立 Epic/Sprint |
|---------|--------|-----------------|
| MedicalCase 同步 + 冲突对话框 | 7 | Epic: 同步体系完善 |
| 运行时模式切换 + 回退 | 3 | 合并入同步 Epic |
| EditModeStateMachine (Clinical/Management) | 2 | Sprint: 编辑模式重构 |
| SQLite 字段级加密 | 1 | Epic: 安全加固 |
| 其他 (重试队列/事件总线/热更新等) | 5 | 各自归入相关 Sprint |

> 注: 原 BOTH 3 项已确认方向 -- MC-08 初始状态 → PRD (保持 Active), SHELL-04 超时警告 → PRD (接受移除), SYNC-17 Checksum → CODE (对齐 PRD)。
> 注: 原 DEFER 中本地导入导出 7 项 → 用户确认归入 CODE (v1.0 本地模式全部实现)。

---

## 五、验证

| 检查项 | 结果 |
|--------|------|
| 分类总数 | 259 (=201+40+18) |
| vs 报告总数 (257) | +2 (MC P3 实际 9 项, Sync P2 实际 9 项) |
| 每个偏差有分类 | 全部标注 |
| 每个偏差有理由 | 全部标注 |
| 用户确认状态 | **全部确认** (244 项自动确认 + 15 项人工确认) |
| CODE 数量 (实际修复量) | 201 项 |
| PRD 数量 (文档修订量) | 40 项 |
| DEFER 数量 (延期量) | 18 项 |
| BOTH 数量 | 0 项 (全部已确认方向) |

### 用户确认记录 (15 项)

| 编号 | 原分类 | 确认后 | 用户决策 |
|------|--------|--------|----------|
| HERB-11/12/13/22 | DEFER | CODE | 本地导入导出不延期，v1.0 全部实现 |
| FORM-08/10/18 | DEFER | CODE | 同上 |
| MC-08 | BOTH | PRD | 保持 Active，UI 层未保存表单替代 Draft |
| SHELL-04 | BOTH | PRD | 接受 simplify-auth 移除，PRD 修订 |
| SYNC-17 | BOTH | CODE | 代码对齐 PRD 最新 Checksum 规格 |
| HERB-05 | PRD | PRD | 确认保持代码 >0.01 |
| HERB-06 | PRD | PRD | 确认保持代码 100000 |
| HERB-17 | CODE | CODE | 确认 DTO 改为 500 |
| HERB-24 | CODE | CODE | 确认 DTO 改为 10 |
| FORM-14 | PRD | PRD | 确认保持实体 200，PRD/DTO 放宽 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-21 | v1.0 | 初始版本: 259 项偏差逐一分类 (193 CODE / 38 PRD / 25 DEFER / 3 BOTH) |
| 2026-02-21 | v1.1 | 用户确认: 15 项人工确认 + 244 项自动确认。最终 201 CODE / 40 PRD / 18 DEFER / 0 BOTH |

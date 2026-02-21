# PRD vs Code 全模块深度扫描报告

> **创建时间**: 2026-02-21
> **扫描范围**: 14 模块 + NFR / 131+ FR / ~150 功能项
> **扫描维度**: 存在性 / 完整度 / 准确性 / 双模式覆盖
> **数据来源**: 逐 FR 代码审查 (每个 FR 对照 PRD 定义检查四个维度)

---

## 一、执行摘要

### 1.1 总体统计

| 模块 | 总 FR | 偏差数 | P1 | P2 | P3 | 通过 | 偏差密度 |
|------|-------|--------|-----|-----|-----|------|----------|
| auth | 13 | 21 | 2 | 11 | 8 | 3 | 1.62 |
| users | 12 | 30 | 17 | 12 | 1 | 1 | 2.50 |
| patients | 13 | 28 | 18 | 6 | 4 | 2 | 2.15 |
| herbs | 13 | 24 | 3 | 16 | 5 | 1 | 1.85 |
| formulas | 13 | 18 | 2 | 10 | 6 | 3 | 1.38 |
| medical-cases | 18 | 37 | 7 | 22 | 8 | 2 | 2.06 |
| printing | 4 | 27 | 12 | 10 | 5 | 0 | 6.75 |
| sync | 8 | 18 | 5 | 8 | 5 | 1 | 2.25 |
| card-reader | 2 | 1 | 0 | 0 | 1 | 2 | 0.50 |
| desktop-shell | 7 | 14 | 1 | 9 | 4 | 1 | 2.00 |
| configuration | 4 | 8 | 0 | 5 | 3 | 1 | 2.00 |
| error-handling | 8 | 9 | 3 | 3 | 3 | 4 | 1.13 |
| logging | 7 | 8 | 1 | 3 | 4 | 3 | 1.14 |
| health-diagnostics | 9 | 5 | 0 | 2 | 3 | 7 | 0.56 |
| nfr | ~20 | 9 | 2 | 4 | 3 | 14 | 0.45 |
| **合计** | **~151** | **~257** | **73** | **121** | **63** | **45** | **1.70** |

> 偏差密度 = 偏差数 / 总 FR。一个 FR 可能有多个不同维度的偏差。

### 1.2 质量排名 (偏差密度从高到低)

| 排名 | 模块 | 偏差密度 | 质量评级 | 说明 |
|------|------|----------|----------|------|
| 1 | printing | 6.75 | F | 打印层级重构(C6)未执行，打印回写链完全断开 |
| 2 | users | 2.50 | D | Token 撤销 5 场景未联动，本地模式大量缺口 |
| 3 | sync | 2.25 | D | MedicalCase 同步完全未实现，冲突对话框功能不足 |
| 4 | patients | 2.15 | D | 患者状态管理完全缺失，引用检查形同虚设 |
| 5 | medical-cases | 2.06 | C | 打印保护+EditReason 机制断裂，初始状态不符 PRD |
| 6 | desktop-shell | 2.00 | C | 菜单权限矩阵未实现，会话超时配置偏差 |
| 7 | configuration | 2.00 | C | 默认值偏差，FeatureToggle 缺少开关 |
| 8 | herbs | 1.85 | C | 本地模式缺口+错误码脱节+引用检查缺失 |
| 9 | auth | 1.62 | C | 单会话登录未实现，Token 过期策略偏差 |
| 10 | formulas | 1.38 | B | Mapper 不映射 Herbs，本地模式验证缺失 |
| 11 | logging | 1.14 | B | 审计日志保留期硬编码偏差 12 倍 |
| 12 | error-handling | 1.13 | B | 错误码 7xxxx 语义完全不对应 |
| 13 | health-diagnostics | 0.56 | A | 实现质量优秀，仅细节偏差 |
| 14 | card-reader | 0.50 | A | 与 PRD 高度一致，仅文档级偏差 |
| 15 | nfr | 0.45 | A | 大部分 NFR 达标，2 个 P1 需关注 |

> 质量评级: A (密度<1) B (1~1.5) C (1.5~2.5) D (2.5~5) F (>5)

### 1.3 关键发现 (Top 8)

1. **打印层级重构 (C6) 完全未执行**: IsPrinted/PrintVersion 仍在 Prescription 上，MedicalCase 无此字段，打印回写链完全断开
2. **Token Family 撤销未联动**: users 模块 5 个场景 (角色变更/删除/禁用/修改密码/重置密码) 均未撤销 Token
3. **MedicalCase 同步完全未实现**: PRD v3.0 约 220 行详细规格全部空白
4. **引用检查 CanDelete 硬编码 true**: patients 和 herbs 的引用检查形同虚设，有关联数据可被直接删除
5. **错误码体系全面脱节**: PRD 5 位 MCCEE 编码 vs 代码简单 int/text，几乎所有模块受影响
6. **EditReason 机制断裂**: DTO 有字段、Permission 返回标记，但写操作 Service 层从未检查
7. **本地模式功能缺口**: users/herbs/formulas/patients 的 IDataSource 方法大量缺失
8. **审计日志保留期偏差 12 倍**: 硬编码 30 天 vs PRD 365 天

---

## 二、横切面问题 (影响多模块)

### X1: 错误码体系脱节

**PRD**: 5 位 MCCEE 结构化编码 (1xxxx~7xxxx)，~90 个错误码
**代码**: ErrorCode 枚举使用简单 int (10001, 50001 等)，Service 层多数使用硬编码字符串
**影响**: auth, users, patients, herbs, formulas, medical-cases, sync, error-handling
**修复方式**: 统一 ErrorCode 枚举为 MCCEE 编码，Service 层 Result.Failure 统一传入 ErrorCode

### X2: 本地模式 (Desktop) 功能缺口

**PRD**: 远程/本地双模式功能对等
**代码**: IUserDataSource 缺少 5+ 方法，IHerbDataSource/IFormulaDataSource 类似
**影响**: users, herbs, formulas, patients, sync
**修复方式**: 逐模块补齐 IDataSource 方法

### X3: Token Family 撤销未联动

**PRD**: AUTH-D06/D07 要求角色变更等 5 场景撤销 Token
**代码**: TokenRevocationService 存在但 UserService 未注入
**影响**: users (5 场景)
**修复方式**: UserService 注入 TokenRevocationService，5 个方法统一补齐调用

### X4: Service 层未使用 ErrorCode 枚举

**代码**: `Result.Failure("硬编码字符串")` 而非 `Result.Failure(ErrorCode.Xxx)`
**影响**: herbs, formulas, users
**修复方式**: 统一使用 ErrorCode 枚举

### X5: 字段验证值不一致

| 字段 | PRD 值 | 代码值 | 模块 |
|------|--------|--------|------|
| 密码最小长度 | 8 | 6 | auth, users |
| AccessToken 过期 | 30 分钟 | 15 分钟 | auth |
| InactivityTimeout | 15 分钟 | 5 分钟 | desktop-shell, configuration |
| Herb.Spec MaxLength | 100 | DTO=50 | herbs |
| Herb.Effect MaxLength | 500 | DTO=1000 | herbs |
| Formula.Effect MaxLength | 500 | DTO=200 | formulas |
| DefaultRole | "Doctor" | "Staff" | configuration |

### X6: 分页筛选内存过滤

**模式**: 先 ToListAsync 全量加载，再内存 Where 过滤，导致分页 TotalCount 不准
**影响**: users, herbs, formulas, medical-cases
**修复方式**: 将 Where 条件移到 IQueryable 链上

### X7: 引用检查 CanDelete 硬编码 true

**代码**: CheckReferenceAsync/CanDelete 直接返回 true
**PRD**: 有医案引用时 CanDelete=false (PAT-D04)，有处方引用时 CanDelete=false (HERB BR-DEL-001)
**影响**: patients, herbs
**修复方式**: 实现实际引用计数查询

### X8: 打印层级重构 (C6) 未执行

**PRD**: IsPrinted/PrintVersion/PrintedAt 在 MedicalCase 实体上
**代码**: 仍在 Prescription 实体上，PrescriptionPrintLog 未重命名为 MedicalCasePrintLog
**影响**: medical-cases, printing
**修复方式**: 实体迁移 + EF Migration + Service/ViewModel 适配

---

## 三、各模块偏差详细清单

### 3.1 auth 模块 (13 FR)

**统计**: 3 通过 / 10 有偏差 / 13 总 FR | 21 偏差 (2 P1 + 11 P2 + 8 P3)

#### P1 (功能缺失/安全风险)

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-AUTH-001 | 完整度 | 单会话登录 (AUTH-D06) 未实现: 登录时未撤销用户已有 Token Family，允许多设备同时在线 | Server 安全策略 |
| FR-AUTH-007 | 存在性 | 登出前警告功能被整体移除 (simplify-auth-architecture): 用户无法在超时前保持登录 | Desktop UX |

#### P2 (部分实现/行为偏差)

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-AUTH-001 | 完整度 | 远程模式 FailedLoginCount 未实现 (本地模式已实现) | Server 暴力破解防护 |
| FR-AUTH-001 | 准确性 | UserDisabled 返回 401 应为 403 | Server HTTP 语义 |
| FR-AUTH-001 | 准确性 | 错误码编号 3 位数 vs PRD 5 位数 (所有 7 个错误码均受影响) | Both |
| FR-AUTH-002 | 完整度 | HMAC 校验失败未自动清除被篡改的凭据文件 | Desktop 安全 |
| FR-AUTH-003 | 完整度 | 30 天绝对过期时间未实现 (长期活跃用户会话永不过期) | Server 安全 |
| FR-AUTH-005 | 完整度 | 服务端登出失败重试队列未实现 (事件已定义但无实现) | Desktop |
| FR-AUTH-011 | 完整度 | TokenExpired 时未尝试 AutoLogin 降级 | Desktop |
| FR-AUTH-013 | 完整度 | 缺少 4 个 PRD 定义事件: LoginStarted, SessionExpiring, SessionExtended, LogoutStarted | Both |
| 数据模型 | 存在性 | AuthSession 实体未实现 (PRD 定义独立表) | Server |
| 配置 | 准确性 | 密码最小长度: PRD=8, 代码 LoginRequestValidator=6 | Both |
| FR-AUTH-001 | 完整度 | 内部网络限流未区分 (统一 5次/60秒 vs PRD 内网 20次/60秒) | Server |

#### P3 (细节不一致)

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-AUTH-004 | 准确性 | TokenRevoked 时提示语义差异 ("登录凭证已失效" vs "会话已在其他设备终止") | Desktop |
| FR-AUTH-006 | 完整度 | 触摸事件 (TouchEventArgs) 未追踪 | Desktop |
| FR-AUTH-008 | 准确性 | validate 端点不返回剩余有效时间 | Server |
| FR-AUTH-008 | 准确性 | 过期 Token 错误码不精确 (不区分 TokenExpired vs TokenInvalid) | Server |
| FR-AUTH-009 | 完整度 | "记住密码"未自动勾选"记住用户名" | Desktop |
| FR-AUTH-010 | 准确性 | 状态名称差异: PRD Idle/Validating/Active vs 代码 Idle/Authenticating/Authenticated | Both |
| FR-AUTH-010 | 完整度 | 本地模式简化版状态机未实现 | Desktop |
| FR-AUTH-012 | 完整度 | "记住密码"后无安全警告文案 | Desktop |
| 配置 | 准确性 | AccessToken 默认过期: PRD=30分钟, 代码=15分钟 | Server |

---

### 3.2 users 模块 (12 FR)

**统计**: 1 通过 / 11 有偏差 / 12 总 FR | 30 偏差 (17 P1 + 12 P2 + 1 P3)

#### 横切面问题

- **A. Token Family 撤销完全缺失**: UserService 未注入 TokenRevocationService，角色变更/删除/重置密码/修改密码/禁用 5 个场景均未撤销 Token
- **B. CanManageUser 遗漏 Receptionist**: Admin 分支仅匹配 Doctor，Admin 无法管理 Receptionist 用户
- **C. AdminOnly 策略过严**: UsersController 整体 AdminOnly，但 GetCurrentUser/ChangePassword/ChangeProfile 应对所有已认证用户开放
- **D. 结构化错误码未使用**: ErrorCode 枚举已定义但 UserService 全部返回 Result.Failure(string)
- **E. 本地模式功能缺口**: IUserDataSource 缺少 Restore/BatchDelete/BatchEnable/BatchDisable/ResetPassword/ChangeProfile 方法

#### P1 (功能缺失/安全风险) -- 17 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-USER-001 | 完整度 | CanManageUser 遗漏 Receptionist，Admin 无法创建 Receptionist | Server |
| FR-USER-004 | 完整度 | 角色变更后未撤销 Token Family (AUTH-D07) | Server |
| FR-USER-004 | 完整度 | CanManageUser 遗漏 Receptionist (同上) | Server |
| FR-USER-005 | 完整度 | 单条删除缺少"不能删除自己"检查 (Server API 层) | Server |
| FR-USER-005 | 完整度 | 删除后未撤销 Token Family / 清理 RefreshToken | Server |
| FR-USER-008 | 完整度 | 重置密码后未撤销 Token Family | Server |
| FR-USER-009 | 完整度 | ChangePasswordAsync 未调用 PasswordPolicyValidator (任意弱密码可设) | Server |
| FR-USER-009 | 完整度 | 修改密码后未撤销 Token Family | Server |
| FR-USER-009 | 准确性 | **ChangePasswordAsync 密码哈希 BUG**: 第458行旧密码重哈希值可能覆盖新密码 | Server |
| FR-USER-009 | 存在性 | Desktop ChangePasswordAsync 是占位实现 (已知缺口 C2 确认) | Desktop |
| FR-USER-009 | 双模式 | AdminOnly 策略阻止 Doctor/Receptionist 修改自己密码 | Server |
| FR-USER-010 | 双模式 | AdminOnly 策略阻止 Doctor/Receptionist 修改个人资料 | Server |
| FR-USER-011 | 完整度 | ToggleStatusAsync 未检查最后一个管理员保护 (USER-D03) | Server |
| FR-USER-011 | 完整度 | 禁用用户后未撤销 Token Family | Server |
| FR-USER-011 | 完整度 | BatchUpdateStatusAsync 缺少权限检查和最后管理员保护 | Server |
| FR-USER-012 | 完整度 | GetCurrentUser 继承 AdminOnly，非管理员返回 403 | Server |

#### P2 (部分实现/行为偏差) -- 12 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-USER-001 | 准确性 | 用户名重复未返回 409+ERR-10002，走 BusinessFail 200 | Server |
| FR-USER-001 | 准确性 | 密码最小长度验证 6 位 vs PRD 8 位 | Shared |
| FR-USER-002 | 完整度 | role/status 筛选内存过滤，分页不准确 | Server |
| FR-USER-005 | 双模式 | LocalUserDataSource 删除保护不完整 | Desktop |
| FR-USER-006 | 双模式 | IUserDataSource 缺少 RestoreAsync | Desktop |
| FR-USER-007 | 双模式 | IUserDataSource 缺少 BatchDeleteAsync | Desktop |
| FR-USER-008 | 完整度 | MustChangeOnNextLogin 标记被忽略 | Server |
| FR-USER-008 | 双模式 | IUserDataSource 缺少 ResetPasswordAsync | Desktop |
| FR-USER-010 | 完整度 | ChangeProfileAsync 未重新生成 PinYinCode | Server |
| FR-USER-011 | 双模式 | LocalUserDataSource 状态切换保护不完整 | Desktop |
| FR-USER-011 | 双模式 | IUserDataSource 缺少批量启用/禁用方法 | Desktop |

#### P3 -- 1 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-USER-009 | 准确性 | 旧密码错误消息"原密码错误" vs PRD "用户名或密码错误" | Server |

---

### 3.3 patients 模块 (13 FR)

**统计**: 2 通过 / 11 有偏差 / 13 总 FR | 28 偏差 (18 P1 + 6 P2 + 4 P3)

#### P1 -- 18 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-PAT-013 | 存在性 | **患者状态管理功能完全缺失**: Controller 无端点, Service 无方法, 业务规则全部未实现 | Server + Desktop |
| FR-PAT-001 | 完整度 | 身份证号 (IdNumber) 必填+唯一性检查完全未实现: DTO 为 nullable, Validator 无 NotEmpty, 无唯一索引 | Server + Desktop |
| FR-PAT-004 | 完整度 | 更新时手机号唯一性检查缺失 (Create 有但 Update 缺失) | Server |
| FR-PAT-004 | 完整度 | 更新时身份证号唯一性检查缺失 | Server |
| FR-PAT-005 | 完整度 | 删除时引用检查未调用: CheckReferenceAsync 存在但未在删除流程中调用，有医案的患者可被直接删除 | Server |
| FR-PAT-007 | 完整度 | 批量删除同样缺少引用检查 (与 FR-PAT-005 同源) | Server |
| FR-PAT-008 | 双模式 | 本地模式批量导入返回 null ("本地模式不支持")，PRD 明确要求支持 | Desktop |
| FR-PAT-010 | 双模式 | 本地模式导出返回 null，PRD 明确要求支持 | Desktop |
| FR-PAT-011 | 存在性 | Controller 缺少 check-reference 端点 (Service 已实现但未暴露) | Server |
| FR-PAT-011 | 准确性 | CheckReferenceAsync 中 CanDelete 硬编码 true，违背 PRD (MC-D04) | Server |
| FR-PAT-012 | 存在性 | Controller 缺少 batch-check-reference 端点 | Server |
| FR-PAT-012 | 准确性 | BatchCheckReferenceAsync 继承 CanDelete=true 问题 | Server |
| FR-PAT-002 | 完整度 | Receptionist 查询未过滤 Status=Disabled 患者 | Server |
| FR-PAT-008 | 完整度 | 导入时缺少身份证号唯一性检查 | Server |
| 错误码 | 存在性 | ERR-20002 (PatientIdCardExists/409) 未实现 | Server |
| 错误码 | 存在性 | ERR-20004 (PatientHasReferencedCases/422) 未实现 | Server |
| 错误码 | 存在性 | ERR-20005 (PatientDisabled/403) 未实现 | Server |
| 错误码 | 存在性 | ERR-20006 (InvalidPatientStatus/400) 未实现 | Server |

#### P2 -- 6 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-PAT-001 | 准确性 | 创建 API 返回 200 而非 PRD 要求的 201 Created | Server |
| DTO | 准确性 | IdNumber/PhoneNumber/Address PRD 为 Required，DTO 为 nullable (string?) | Shared |
| 权限 | 完整度 | Receptionist 应有 CRU 权限，但所有端点统一 DoctorOrAdmin | Server |
| FR-PAT-005 | 准确性 | 删除失败统一返回 404 而非区分 422 | Server |
| FR-PAT-011 | 双模式 | Desktop 端无引用检查功能 | Desktop |
| FR-PAT-012 | 双模式 | Desktop 端批量引用检查缺失 | Desktop |

#### P3 -- 4 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-PAT-001 | 完整度 | CreateAsync (DTO版) 缺少手机号唯一性检查 (备用路径) | Server |
| FR-PAT-008 | 准确性 | 导入行数限制 off-by-one (rowCount>1000 含表头) | Server |
| FR-PAT-009 | 准确性 | 导入模板表头 IdNumber 列未标记必填 * | Server |
| FR-PAT-013 | 准确性 | PatientStatus 枚举 (Active/Inactive) 与实际使用的 CommonStatus (Enabled/Disabled) 不一致，冗余代码 | Shared |

---

### 3.4 herbs 模块 (13 FR)

**统计**: 1 通过 / 12 有偏差 / 13 总 FR | 24 偏差 (3 P1 + 16 P2 + 5 P3)

#### 横切面问题

- 错误码体系不匹配: PRD 501xx/502xx/503xx 三段分区 vs 代码 5000x 平铺
- Service 层未使用 ErrorCode 枚举
- 字段长度不一致: Spec(PRD 100/DTO 50), Effect(PRD 500/DTO 1000), Usage(PRD 500/Validator 200), Unit(PRD 10/DTO 20)

#### P1 -- 3 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-HERB-005 | 完整度 | 删除操作缺少处方引用检查 (BR-DEL-001): 有处方引用的药材可被删除 | Server |
| FR-HERB-008 | 准确性 | 批量删除同样未检查处方引用 | Server |
| FR-HERB-013 | 准确性 | CanDelete 硬编码 true 与 PRD BR-DEL-001 矛盾 | Server |

#### P2 -- 16 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-HERB-001 | 完整度 | CreateAsync 缺少拼音码自动生成 (仅导入路径有) | Server |
| FR-HERB-001 | 准确性 | Price 验证: FluentValidation >0.01 vs PRD >0 | Shared |
| FR-HERB-001 | 准确性 | Price 最大值: ValidationConstants 100000 vs PRD 999999.99 | Shared |
| FR-HERB-002 | 完整度 | 分类筛选内存过滤，分页计数不准确 | Server |
| FR-HERB-004 | 完整度 | 名称变更时未自动重新生成拼音码 | Server |
| FR-HERB-005 | 准确性 | 删除被引用药材未返回 422 / 未使用 HerbInUse 错误码 | Server |
| FR-HERB-006 | 双模式 | 本地模式不支持批量启用/禁用 | Desktop |
| FR-HERB-009 | 双模式 | 本地模式不支持 Excel 导入 (PRD 明确要求) | Desktop |
| FR-HERB-010 | 双模式 | 本地模式不支持 JSON 批量导入 | Desktop |
| FR-HERB-011 | 双模式 | 本地模式不支持导出 | Desktop |
| FR-HERB-013 | 双模式 | 本地模式未实现引用检查 | Desktop |
| 全局 | 准确性 | 错误码编号体系不匹配 (PRD 501xx vs Code 5000x) | Both |
| 全局 | 完整度 | Service 层未使用 ErrorCode 枚举 | Server |
| 全局 | 准确性 | Effect 字段: DTO 允许 1000 字符但实体只存 500 | Shared |
| 全局 | 准确性 | Usage 字段: Validator 限制 200 字符 vs PRD/实体 500 | Shared |

#### P3 -- 5 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-HERB-002 | 准确性 | 分页验证缺少 ERR-50106 错误码 | Server |
| FR-HERB-007 | 准确性 | 恢复操作缺少 ERR-50104 错误码 | Server |
| FR-HERB-010 | 准确性 | 批量导入超限缺少 ERR-50202 错误码 | Server |
| FR-HERB-012 | 双模式 | 本地模式不支持模板下载 | Desktop |
| 全局 | 准确性 | Spec 字段长度 DTO 50 vs PRD/实体 100; Unit 字段 DTO 20 vs 实体 10 | Shared |

---

### 3.5 formulas 模块 (13 FR)

**统计**: 3 通过 / 10 有偏差 / 13 总 FR | 18 偏差 (2 P1 + 10 P2 + 6 P3)

#### 关键发现

- **FormulaMapper 不映射 Herbs**: `[MapperIgnoreTarget(nameof(FormulaDetailDto.Herbs))]` 导致所有 API 返回空药材组成
- 错误码体系脱节: PRD 17 个 (ERR-60101~60304) vs 代码 6 个 (60001~60006)

#### P1 -- 2 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-FORM-003 | 完整度 | **FormulaMapper.ToDetailDto 不映射 Herbs 列表**: 所有返回 FormulaDetailDto 的 API 端点 Herbs 为空列表 | Server 全部端点 |
| 全局 | 准确性 | PRD 错误码 (ERR-60101~60304, 17个) 与代码 ErrorCode (60001~60006, 6个) 完全不匹配 | Both |

#### P2 -- 10 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-FORM-001 | 完整度 | Server 未校验 Herbs 列表为空 (FluentValidation 条件有漏洞) | Server |
| FR-FORM-001 | 准确性 | Effect StringLength DTO=200 vs PRD/Entity=500 | Shared |
| FR-FORM-002 | 完整度 | FormulaListDto.TotalPrice 始终为 0，未从药材库计算价格 | Server |
| FR-FORM-009 | 双模式 | Desktop 端已删除延迟绑定验证方法，本地模式无法执行药材验证 | Desktop |
| FR-FORM-010 | 双模式 | Desktop 端已删除待验证列表方法 | Desktop |
| FR-FORM-011 | 双模式 | Desktop 端无本地批量导入实现 (PRD 明确要求) | Desktop |
| FR-FORM-012 | 完整度 | 导出 Excel 不含药材组成详情 | Server |
| FR-FORM-012 | 双模式 | Desktop 端无本地导出实现 | Desktop |
| 全局 | 完整度 | Service 层使用硬编码字符串返回错误，未使用 ErrorCode | Server |
| FR-FORM-001 | 准确性 | Desktop FormulaValidator 将功效/用法设为必填，PRD 定义选填 | Desktop |

#### P3 -- 6 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-FORM-001 | 准确性 | Usage StringLength DTO=200 vs FluentValidation=500 | Shared |
| FR-FORM-001 | 准确性 | Entity Name StringLength=200 vs PRD/DTO=100 | Entity |
| FR-FORM-002 | 完整度 | 分类筛选内存过滤，分页不准确 | Server |
| FR-FORM-006 | 双模式 | 本地模式不支持批量启用/禁用 | Desktop |
| FR-FORM-010 | 完整度 | 待验证列表无分页，全量加载 | Server |
| FR-FORM-013 | 双模式 | 本地模式无内置导入模板 | Desktop |

---

### 3.6 medical-cases 模块 (18 FR)

**统计**: 2 通过 / 16 有偏差 / 18 总 FR | 37 偏差 (7 P1 + 22 P2 + 8 P3)

#### 关键发现

- 打印层级重构 (C6) 完全未执行
- EditReason 机制断裂: DTO 有字段, Permission 返回标记, 但写操作 Service 层从未检查
- 初始状态 Active 而非 Draft
- 错误码体系未映射: PRD ERR-3xxxx (29个) 全部未使用

#### P1 -- 7 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-MC-001 | 完整度 | 缺少患者状态检查 (ERR-30105): 禁用患者可创建医案 | Server |
| FR-MC-005 | 完整度 | 打印保护逻辑未实现 (ERR-30403): IsPrinted 检查和 PrintVersion++ 缺失 | Server |
| FR-MC-005 | 完整度 | MedicalCase 实体缺少 IsPrinted/PrintVersion 字段 (PRD v2.0 核心, C6 确认) | Entity |
| FR-MC-005 | 完整度 | EditReason 未在写操作中强制校验 (完成/隔天/非本人/打印后 4 场景均无保护) | Server |
| FR-MC-007 | 完整度 | BR-003 缺少 TcmDiagnosis 非空校验: 可完成无诊断的医案 | Server |
| FR-MC-013 | 完整度 | EditReason 写操作强制校验缺失 (与 MC-005 同源) | Server |
| FR-MC-015 | 完整度 | PrescriptionPrintLog 未重构为 MedicalCasePrintLog (C6 确认) | Entity/DB |

#### P2 -- 22 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-MC-001 | 完整度 | 初始状态 Active 而非 PRD 规定的 Draft | Server |
| FR-MC-001 | 完整度 | 缺少医案编号自动生成 (MC20260210001 格式) | Server |
| FR-MC-001 | 准确性 | 错误码未使用 ERR-3xxxx 编码体系 | Server |
| FR-MC-003 | 完整度 | 设为 false 时未清除已有处方 | Server |
| FR-MC-004 | 完整度 | 缺少处方编号自动生成 (RX-YYYYMMDD-NNNN) | Server |
| FR-MC-004 | 完整度 | 缺少 Items 为空时的验证 (至少1个处方项) | Server |
| FR-MC-005 | 完整度 | 审计日志中 EditReason 未传递 | Server |
| FR-MC-007 | 完整度 | BR-003 缺少 Items.Count > 0 校验 | Server |
| FR-MC-008 | 完整度 | 取消前未自动保存诊断数据 | Server |
| FR-MC-008 | 完整度 | 非当天本人取消缺少 Reason 强制检查 | Server |
| FR-MC-009 | 完整度 | GetListDtoAsync 内存过滤导致分页不准确 | Server |
| FR-MC-011 | 存在性 | EditModeStateMachine 不存在 (Clinical/Management 模式未独立实现) | Desktop |
| FR-MC-013 | 完整度 | RequiresEditReason 仅检查 IsLocked, 遗漏"非本人编辑"和"当天编辑已完成"场景 | Server |
| FR-MC-015 | 完整度 | PrescriptionPrintHandler 未设置 MedicalCase.IsPrinted=true | Desktop |
| FR-MC-015 | 完整度 | 缺少打印后 PrintCount++ 和 LastPrintedAt 更新的服务端逻辑 | Server |
| FR-MC-016 | 完整度 | 验方导入未过滤 ValidationStatus=Validated | Desktop |
| FR-MC-016 | 完整度 | 验方导入未过滤 Status=Enabled | Desktop |
| FR-MC-016 | 完整度 | 验方导入未跳过禁用药材 (MC-D09) | Desktop |
| FR-MC-016 | 完整度 | 验方导入价格未从药材库实时获取 | Desktop |
| FR-MC-018 | 完整度 | 历史复制未跳过禁用药材 (MC-D09) | Desktop |
| FR-MC-018 | 完整度 | 历史复制价格未从药材库实时获取 (MC-D13) | Desktop |
| FR-MC-018 | 完整度 | 历史复制未记录 ReferencedFormulas 来源 | Desktop |

#### P3 -- 8 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-MC-004 | 准确性 | PrescriptionItem.Usage 错误赋值为 Prescription.Usage | Server |
| FR-MC-006 | 准确性 | 错误消息基本一致 | - |
| FR-MC-007 | 完整度 | BR-003 缺少 DosageCount > 0 校验 | Server |
| FR-MC-011 | 完整度 | Clinical/Management 模式按钮组区分逻辑需验证 | Desktop |
| FR-MC-012 | 准确性 | OperationType 使用 int 枚举存储 vs PRD string(20) | Entity |
| FR-MC-012 | 准确性 | OperatorName MaxLength 50 vs PRD 100 | Entity |
| FR-MC-012 | 完整度 | 审计字段少 Prescription.Usage (5/6 个 Prescription 字段) | Server |
| FR-MC-017 | 准确性 | pending 端点缺少 doctorId 查询参数 | Server |
| FR-MC-018 | 完整度 | DosageCount/Discount 未从历史处方复制 | Desktop |

---

### 3.7 printing 模块 (4 FR)

**统计**: 0 通过 / 4 有偏差 / 4 总 FR | 27 偏差 (12 P1 + 10 P2 + 5 P3)

**根因**: C6 (打印层级重构) 完全未执行 + 打印后回写链完全断开

#### P1 -- 12 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-PRINT-001 | 完整度 | 打印后 PrintCount 递增逻辑缺失 (永远为0) | Desktop |
| FR-PRINT-001 | 完整度 | 打印后 MedicalCase.IsPrinted=true 逻辑缺失 (打印保护机制未生效) | Desktop |
| FR-PRINT-001 | 完整度 | 打印后 LastPrintedAt 更新缺失 (永远为null) | Desktop |
| FR-PRINT-003 | 存在性 | 打印层级未迁移: IsPrinted/PrintVersion 仍在 Prescription 上 (C6 确认) | Entity |
| FR-PRINT-003 | 存在性 | PrintVersion 递增逻辑完全缺失 (一旦 IsPrinted=true 处方完全不可修改) | Server |
| FR-PRINT-003 | 完整度 | 打印时版本号快照记录缺失 | Both |
| FR-PRINT-004 | 存在性 | MedicalCasePrintLog 实体未创建 (仍为 PrescriptionPrintLog) | Entity |
| FR-PRINT-004 | 存在性 | PrintType 枚举不存在 | Entity |
| FR-PRINT-004 | 完整度 | 打印日志写入逻辑完全缺失 (仅记 ILogger 日志) | Desktop |
| FR-PRINT-004 | 完整度 | 打印失败日志记录缺失 | Desktop |
| FR-PRINT-004 | 双模式 | 远程模式无打印日志 API 端点 | Server |
| FR-PRINT-004 | 双模式 | 本地模式打印日志存储缺失 | Desktop |

#### P2 -- 10 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-PRINT-001 | 准确性 | 模板字体: 楷体 vs PRD 宋体 | Desktop |
| FR-PRINT-001 | 准确性 | 模板边距: 15mm vs PRD A5 8mm | Desktop |
| FR-PRINT-001 | 完整度 | 诊所信息区缺失 (地址/电话字段未绑定) | Desktop |
| FR-PRINT-001 | 完整度 | 诊断信息区不完整 (仅 TcmDiagnosis, 缺 PresentIllness/TongueDiagnosis/PulseDiagnosis) | Desktop |
| FR-PRINT-001 | 完整度 | 煎法标注未渲染 (未使用 DisplayText) | Desktop |
| FR-PRINT-001 | 完整度 | 分页规则未实现 (>12味药材溢出) | Desktop |
| FR-PRINT-001 | 完整度 | 草稿水印未实现 | Desktop |
| FR-PRINT-001 | 准确性 | DoctorName 未绑定 (需手写) | Desktop |
| FR-PRINT-001 | 准确性 | 费用计算: Discount 未参与, 引入未定义的 TreatmentFee | Desktop |
| FR-PRINT-002 | 完整度 | A4/A5 排版参数无差异处理 | Desktop |

#### P3 -- 5 项 (字号微差、药材截断、空处方校验等)

---

### 3.8 sync 模块 (8 FR)

**统计**: 1 通过 / 7 有偏差 / 8 总 FR | 18 偏差 (5 P1 + 8 P2 + 5 P3)

#### 关键发现

- MedicalCase 同步完全未实现: PRD v3.0 约 220 行详细规格全部空白
- 冲突对话框仅展示 Checksum: 无实际字段值对比
- 模式切换几乎空白: 仅登录时可选，无运行时切换

#### P1 -- 5 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-SYNC-001/005 | 存在性 | MedicalCase 同步完整设计完全未实现 (SupportedTypes 仅 3 种) | 全栈 |
| FR-SYNC-007 | 存在性 | SyncConflictDetailDto 未实现，无 LocalVersion/ServerVersion 字段值对比 | Both |
| FR-SYNC-007 | 完整度 | 冲突对话框仅展示 Checksum 和时间戳，用户无法做有意义的冲突决策 | Desktop |
| FR-SYNC-008 | 存在性 | 运行时模式切换功能完全未实现 (仅登录时选择) | Desktop |
| FR-SYNC-008 | 存在性 | 切换前未同步变更检查未实现 | Desktop |

#### P2 -- 8 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-SYNC-002 | 准确性 | SyncMetadataDto 缺少 EntityType 和 DisplayName 字段 | Shared |
| FR-SYNC-002 | 完整度 | GetMetadataAsync 未用 IgnoreQueryFilters(), 软删除记录不出现 | Server |
| FR-SYNC-003 | 完整度 | ChangedFields 始终为 null，未实现变更字段检测 | Server |
| FR-SYNC-004 | 完整度 | Desktop 硬编码 OverwriteConflicts=false | Desktop |
| FR-SYNC-007 | 完整度 | 同步前检查未实现 (网络/Token) | Desktop |
| FR-SYNC-007 | 完整度 | 进度 UI 简化为简单进度条，缺少 4 步指示器 | Desktop |
| FR-SYNC-007 | 完整度 | 结果汇总不完整，缺少按实体类型分组和 FailedItems | Desktop |
| FR-SYNC-008 | 存在性 | 切换失败回退策略未实现 | Desktop |
| 全局 | 完整度 | PRD 20 个错误码 (ERR-70101~70505) 全部未实现 | Both |

#### P3 -- 5 项 (DTO命名/字段名不一致, Checksum字段差异, 状态栏标识等)

---

### 3.9 card-reader 模块 (2 FR)

**统计**: 2 通过 / 0 有偏差 / 2 总 FR | 1 偏差 (P3)

实现质量优秀，与 PRD 高度一致。

| FR编号 | 维度 | 偏差描述 | 严重度 | 影响范围 |
|--------|------|----------|--------|----------|
| FR-CARD-002 | 准确性 | PRD 写 "姓名->RealName" 但代码字段名为 Name (文档描述问题) | P3 | 文档 |

---

### 3.10 desktop-shell 模块 (7 FR)

**统计**: 1 通过 / 6 有偏差 / 7 总 FR | 14 偏差 (1 P1 + 9 P2 + 4 P3)

#### P1 -- 1 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-SHELL-005 | 完整度 | 菜单可见性矩阵未实现: PRD 定义 11 菜单x4角色权限矩阵，代码无角色到菜单可见性映射 | Desktop |

#### P2 -- 9 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-SHELL-002 | 完整度 | 登出时未显式清除导航历史 | Desktop |
| FR-SHELL-002 | 完整度 | 模块加载仅区分 Admin/非Admin, Doctor 不会额外加载临床模块 | Desktop |
| FR-SHELL-003 | 完整度 | 超时前警告功能已被 simplify-auth-architecture 移除 | Desktop |
| FR-SHELL-003 | 准确性 | ClientSessionOptions 默认值: InactivityTimeout=5(应15), Warning=0(应2) | Desktop |
| FR-SHELL-004 | 完整度 | 导航历史无 20 条上限 | Desktop |
| FR-SHELL-005 | 完整度 | 本地模式部分菜单不可用逻辑未实现 | Desktop |
| FR-SHELL-007 | 完整度 | 缺少最后登录时间/登录IP 只读信息 | Desktop |
| FR-SHELL-007 | 完整度 | 缺少 Email 属性编辑支持 | Desktop |
| FR-SHELL-007 | 双模式 | 本地模式下账户设置未分支处理 | Desktop |

#### P3 -- 4 项 (状态枚举差异, 登录协调依赖计数, StartupReport 类型, 启动诊断)

---

### 3.11 configuration 模块 (4 FR)

**统计**: 1 通过 / 3 有偏差 / 4 总 FR | 8 偏差 (0 P1 + 5 P2 + 3 P3)

#### P2 -- 5 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-CFG-001 | 准确性 | UserManagementOptions.DefaultRole="Staff" vs PRD "Doctor" | Server |
| FR-CFG-002 | 准确性 | ClientSessionOptions 默认值与 PRD 不一致 | Desktop |
| FR-CFG-002 | 完整度 | FeatureToggleOptions 缺少 CardReaderEnabled 开关 | Desktop |
| FR-CFG-004 | 准确性 | JWT SecretKey 验证用字符串长度 vs PRD Base64 解码字节数 | Server |
| FR-CFG-004 | 准确性 | Important 级别配置缺失会错误阻止启动 (应仅警告) | Server |

#### P3 -- 3 项 (Swagger/Json 注册方式, FeatureToggle 热更新, 错误输出格式)

---

### 3.12 error-handling 模块 (8 FR)

**统计**: 4 通过 / 4 有偏差 / 8 总 FR | 9 偏差 (3 P1 + 3 P2 + 3 P3)

#### P1 -- 3 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-ERR-006 | 完整度 | ErrorCode 7xxxx: PRD 定义"数据同步", 代码定义"问诊/Consultation", 完全不对应 | Both |
| FR-ERR-006 | 完整度 | ClientErrorMessageMapper 无法解析 "ERR-10004" 格式: int.TryParse 失败导致精确映射失效 | Desktop |
| FR-ERR-008 | 存在性 | 异常到通知类型映射完全未实现 (已知缺口 C5): 统一 MessageBox | Desktop |

#### P2 -- 3 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-ERR-006 | 准确性 | HTTP 429 映射缺失 | Desktop |
| FR-ERR-006 | 完整度 | TokenExpired/DeviceMismatch/SessionExpired (10013-15) 无消息映射 | Desktop |
| FR-ERR-007 | 完整度 | 追踪码附加未与 ExceptionSeverity 自动关联 | Desktop |

#### P3 -- 3 项

---

### 3.13 logging 模块 (7 FR)

**统计**: 3 通过 / 4 有偏差 / 7 总 FR | 8 偏差 (1 P1 + 3 P2 + 4 P3)

#### P1 -- 1 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-LOG-006 | 准确性 | SecurityAuditCleanupService 硬编码 30 天保留 vs PRD 365 天 (12倍差距, 审计合规问题) | Server |

#### P2 -- 3 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-LOG-003 | 准确性 | SensitiveDataAttribute 两份定义在不同命名空间, 实体标记脱敏可能失效 | Both |
| FR-LOG-006 | 完整度 | SecurityAuditCleanupService 无 Options 配置 | Server |
| FR-LOG-006 | 完整度 | SecurityAuditCleanupService 未分批删除 (全量加载到内存) | Server |

#### P3 -- 4 项

---

### 3.14 health-diagnostics 模块 (9 FR)

**统计**: 7 通过 / 2 有偏差 / 9 总 FR | 5 偏差 (0 P1 + 2 P2 + 3 P3)

#### P2 -- 2 项

| FR编号 | 维度 | 偏差描述 | 影响范围 |
|--------|------|----------|----------|
| FR-SYS-003 | 完整度 | Unhealthy 被映射为 "Degraded" 而非 "Unhealthy" | Server |
| FR-SYS-003 | 完整度 | 详细响应缺少 server/database/pendingMigrations/error 字段 | Server |

#### P3 -- 3 项

---

### 3.15 nfr 模块 (~20条)

**统计**: 14 通过 / 6 有偏差 / ~20 条 | 9 偏差 (2 P1 + 4 P2 + 3 P3)

#### P1 -- 2 项

| NFR编号 | 维度 | 偏差描述 | 影响范围 |
|---------|------|----------|----------|
| NFR-SEC-004 | 存在性 | **SQLite 字段级加密 (AES-256+DPAPI) 整体未实现**: 无 EncryptedStringConverter, 患者身份证/手机明文存储 | Desktop |
| NFR-SEC-005 | 准确性 | 审计日志保留期硬编码 30 天 vs PRD 365 天 (与 FR-LOG-006 同源) | Server |

#### P2 -- 4 项

| NFR编号 | 维度 | 偏差描述 | 影响范围 |
|---------|------|----------|----------|
| NFR-SEC-001 | 准确性 | 不活跃超时配置 5 分钟 vs PRD 15 分钟 | Desktop |
| NFR-SEC-002 | 准确性 | 密码过期天数: DefaultPasswordOptions=30天 vs PasswordPolicyValidator=90天 | Server |
| 缓存 5.3 | 存在性 | Server 端缓存失效映射未实现 (无 EvictByTagAsync 调用) + MedicalCase/User 策略死配置 | Server |
| 缓存 5.4 | 完整度 | Desktop 端写后缓存失效 (RemoveByPrefix) 未实现 | Desktop |

#### P3 -- 3 项

---

## 四、优先级行动建议

### 4.1 横切面专项修复 (ROI 最高)

| # | 专项 | 涉及模块 | 预估消除偏差数 | 预估工时 |
|---|------|---------|---------------|---------|
| X3 | Token Family 撤销联动 | users | ~8 | 1 天 |
| X7 | 引用检查实际查询 | patients, herbs | ~6 | 0.5 天 |
| X5 | 字段验证值对齐 | herbs, formulas, auth, users, configuration | ~12 | 0.5 天 |
| X4 | Service 层 ErrorCode 替代硬编码 | users, herbs, formulas | ~6 | 0.5 天 |
| X6 | 分页筛选迁移到 Repository 层 | users, herbs, formulas, medical-cases | ~4 | 1 天 |
| X1 | 错误码体系统一 (MCCEE) | 全部模块 | ~40 | 3-4 天 |
| X8 | 打印层级重构 (C6) | medical-cases, printing | ~15 | 2-3 天 |
| X2 | 本地模式 IDataSource 补齐 | users, herbs, formulas, patients | ~20 | 3-4 天 |

### 4.2 Sprint 规划建议

**Sprint 1 (安全加固)**: X3 + X7 + 安全类 P1 (Token/引用/密码)
**Sprint 2 (核心修复)**: X8 + X5 + X4 + medical-cases P1
**Sprint 3 (体系统一)**: X1 + X6 (错误码+分页)
**Sprint 4 (本地模式)**: X2 + Desktop 功能缺口
**Sprint 5+**: P2 功能完善 + P3 细节对齐

### 4.3 不建议立即修复的 P1 项

| 项目 | 原因 |
|------|------|
| MedicalCase 同步 (sync P1) | 功能复杂度高，建议独立 Epic 规划 |
| SQLite 字段级加密 (NFR-SEC-004) | 架构影响面大，需独立设计 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-21 | v1.0 | 初始版本: 14 模块 + NFR 全量深度扫描, 257 项偏差 (73 P1 / 121 P2 / 63 P3) |

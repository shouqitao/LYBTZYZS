# sysadmin 逻辑完整审核报告

> 日期：2026-08-13 | 状态：审核完成（任务书 7 项逐项验证——3 项文档过时待修）
> 依据：docs/compose/plans/sysadmin-logic-audit-2026-08-13.md + 05-security-password-management.md（权威）+ 用户确认逻辑（2026-08-13）

---

## 审核结论摘要

| # | 审核项 | 文档要求 | 代码现状 | 结论 |
|---|--------|---------|---------|:---:|
| 1 | 初始密码读取 | 配置读取（开发）/ 环境变量 K4（生产） | `IdentitySeedData.ResolveSysAdminPassword`——开发读配置、生产读 `DefaultPasswords__SysAdminPassword`（缺失抛异常禁回退） | ✅ 符合 |
| 2 | ForceResetOnStartup | 「仅开发/测试生效，生产始终忽略」（05-security:118-130） | 修复后（bb9500cfa）：开发直接触发；**非开发需 InitialSetupToken 验证可触发**（安全门控——不要求 AllowAutoCreate）；PBKDF2 真哈希 | ⚠️ **文档过时**（05-security 未同步 ForceReset 修复） |
| 3 | IdentitySeedData 幂等 | 「仅 LastLoginAt==null 重置密码」（铁律 #4） | `FindByNameAsync` 不存在才创建（天然幂等——已登录用户不被覆盖）——**无 LastLoginAt 条件代码** | ⚠️ **文档过时**（描述已移除的旧实现细节——语义等价） |
| 4 | ResetPassword 拒绝 sysadmin | sysadmin 密码不可由他人重置 | `ResetPasswordCommandHandler:35-36`——`user.IsSysAdmin` 拦截 ✓ | ✅ 符合 |
| 5 | PasswordHashGenerator | PBKDF2 混淆密码 → 更新 DB（唯一恢复途径） | PBKDF2 ✓（`PasswordHasher<ApplicationUser>`——AQAAAA 前缀）；**只生成哈希（无 DB 连接——人工 SQL 更新）** | ✅ 符合（任务书预期如实） |
| 6 | 锁定策略统一 | sysadmin 与普通用户统一锁定（US-AUTH-002） | LoginCommandHandler 统一 LockoutEnd 逻辑（5 次/15 分——本地/远程同） | ✅ 符合 |
| 7 | 文档 BCrypt→PBKDF2 | 铁律 #3 明确 PBKDF2 全部走 UserManager | 代码全 PBKDF2（BCrypt 已删——A-27）；**05-security line 5/15/174 仍写 BCrypt** | ⚠️ **文档过时**（内部矛盾——line 5/15/174 vs 218-234） |

**结论**：代码实现 7 项中 **6 项正确**（含用户权威逻辑——初始密码配置读取 + 遗忘仅工具恢复）；**3 处文档过时**（#2 ForceReset 语义 / #3 幂等细节 / #7 BCrypt 描述）——文档地位第一原则：先修文档对齐代码。

---

## 逐项详情

### #1 初始密码读取 —— ✅ 符合
```
文档：appsettings.json DefaultPasswords:SysAdminPassword（开发）；生产环境变量 DefaultPasswords__SysAdminPassword（K4）
代码：IdentitySeedData.ResolveSysAdminPassword（环境变量 → 缺失抛异常；非生产读配置）
```
配置唯一化（76f082b07）后单一变量名——代码与用户权威设计一致。

### #2 ForceResetOnStartup —— ⚠️ 文档过时
```
文档（05-security:118-130）：「仅开发/测试环境生效」「生产环境始终忽略，即使配置为 true」
代码（修复后）：
  ForceReset 触发 = ForceResetOnStartup && (IsDevelopment() || ValidateSetupToken(InitialSetupToken))
  非开发需 token 验证（安全门控）——测试部署（Production 名）可触发；生产默认 false 不受影响
  重置 = UserManager.ResetPasswordAsync（PBKDF2 真哈希）
```
**差异**：文档「生产始终忽略」过时——修复（bb9500cfa）后非开发 + token 可触发（更安全语义：显式开启 + 验证）。**修文档**（对齐 SystemAdminOptions 新注释）。

### #3 IdentitySeedData 幂等 —— ⚠️ 文档过时（语义等价）
```
文档（铁律 #4）：「仅 LastLoginAt==null 时重置密码（幂等）」
代码：SeedRolesAndAdminAsync → FindByNameAsync——不存在才创建；已存在完全跳过（无覆盖逻辑）
```
**差异**：代码无 LastLoginAt 条件——但**行为等价**（不存在才创建 = 已登录用户天然不被覆盖）。文档描述的 LastLoginAt 条件是旧实现残留描述——**修文档**（幂等语义 = 不存在才创建）。

### #4 ResetPassword 拒绝 sysadmin —— ✅ 符合
`ResetPasswordCommandHandler:35-36`——`if (user.IsSysAdmin)` 拒绝（管理员界面排除 sysadmin——其改密走 change-password）——T5-1 #12 已实施。

### #5 PasswordHashGenerator —— ✅ 符合（如实记录）
`src/Tools/PasswordHashGenerator`——`PasswordHasher<ApplicationUser>`（PBKDF2——AQAAAA 前缀——与 UserManager 登录同款）；**无 DB 连接**——只生成哈希字符串，人工 SQL 更新 DB（`UPDATE Users SET PasswordHash='...' WHERE UserName='sysadmin'`）——任务书预期「唯一恢复途径」如实。

### #6 锁定策略统一 —— ✅ 符合
LoginCommandHandler 统一 LockoutEnd（5 次/15 分——`_securityOptions.AccountLockout`）——sysadmin 与普通用户同路径（US-AUTH-002——本地锁定对齐已核实 AUTH-002 批次）。

### #7 文档 BCrypt → PBKDF2 —— ⚠️ 文档过时
05-security **内部矛盾**：line 5/15/174 写 BCrypt（`IPasswordService`/WorkFactor=11），line 218-234 铁律 #3 写「全部走 UserManager（PBKDF2），禁 BCrypt 落库」——代码实际全 PBKDF2（BCrypt 已删——A-27 技术栈减法）——**修 line 5/15/174**（BCrypt 描述 → PBKDF2 Identity）。

---

## 修复计划（先文档后代码）

| 文档 | 修复 |
|------|------|
| 05-security:5 | 「系统采用 BCrypt 密码哈希」→「系统采用 ASP.NET Core Identity PBKDF2 密码哈希」 |
| 05-security:15 | `HashPassword` BCrypt(WorkFactor=11) → `UserManager` PBKDF2（Identity 内置） |
| 05-security:118-130 | ForceReset「仅开发/测试」「生产始终忽略」→ 新语义（开发直接 + 非开发 InitialSetupToken 验证门控——SystemAdminOptions 注释同源） |
| 05-security:174 | BCrypt WorkFactor=11 描述 → PBKDF2（Identity 内置——AQAAAA 前缀） |
| 05-security 铁律 #4 | 「仅 LastLoginAt==null 重置」→「不存在才创建（幂等——已登录用户不被覆盖）」 |

**代码零差异需改**（#1/#4/#5/#6 正确；#2/#3/#7 文档过时）——纯文档批次。

## 关联
- 05-security-password-management.md（权威——修复目标）
- 13c §五：ForceReset 修复（bb9500cfa）/ A-27 技术栈减法（BCrypt 删）
- US-AUTH-002 / US-USER-007 / US-SHELL-017

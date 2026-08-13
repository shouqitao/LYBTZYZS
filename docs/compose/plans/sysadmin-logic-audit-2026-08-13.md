# 任务：sysadmin 逻辑完整审核（文档要求 vs 代码实现）

## 背景
用户要求审核 sysadmin 逻辑。权威文档要求已读取（见下），需对照代码实现逐项验证，找出差异并修复。

## 用户确认的正确逻辑（2026-08-13）
1. **初始密码从配置文档读取**（appsettings.json DefaultPasswords:SysAdminPassword；生产由环境变量 DefaultPasswords__SysAdminPassword 覆盖——K4 加固）
2. **sysadmin 密码遗忘只能用密码初始化工具恢复**（src/Tools/PasswordHashGenerator 生成 PBKDF2 混淆密码 → 更新 DB）——这是唯一恢复途径

## 权威文档要求（已读取）

### 05-security-password-management.md（密码管理权威）
- ForceResetOnStartup（line 95/118-130）：仅开发/测试环境生效，重置密码 + FailedLoginCount + LockoutEnd + Status；生产始终忽略
- 铁律 #3（line 218-234）：BCrypt 与 PBKDF2 不兼容——全部走 UserManager（PBKDF2），禁 BCrypt 落库
- 铁律 #4（line 236-249）：IdentitySeedData 仅在 LastLoginAt==null 时重置密码（幂等）
- **文档内部矛盾**：line 5/15/174 说「BCrypt」，但 line 218-234 说「PBKDF2 全部走 UserManager」——需确认文档过时处并修正（代码实际用 PBKDF2）

### 02-auth.md US-AUTH-000（首登初始化）
- 默认密码登录（sysadmin/SysAdmin@2026!）→ 强制改密（ForceChangeOnFirstLogin）→ 填诊所信息 → 创建 admin → 交权（向导 UI ⚠️ 待开发）

### 03-users.md US-USER-007（重置密码）
- sysadmin 密码不可由他人重置（ResetPassword 拒绝）

### personas.md 约束
- sysadmin 独立用户（IsSysAdmin=true）；仅管理 Admin；不可自管；密码遗忘用离线工具

### 02-configuration.md
- 开发占位明文（DevP@ssw0rd!）；生产环境变量注入（K4）

## 审核清单（逐项验证）

1. **初始密码读取**：IdentitySeedData.ResolveSysAdminPassword 是否正确（开发读配置、生产读环境变量 K4）
2. **ForceResetOnStartup**：是否仅开发/测试生效 + 重置密码哈希（用 UserManager PBKDF2 而非 BCrypt）+ 生产忽略
3. **IdentitySeedData 幂等**：是否仅 LastLoginAt==null 重置（已登录用户不被覆盖）
4. **ResetPassword 拒绝 sysadmin**：ResetPasswordCommandHandler 是否正确拦截
5. **PasswordHashGenerator**：是否 PBKDF2（已确认）——能否连库更新（还是只生成哈希）
6. **锁定策略**：sysadmin 与普通用户统一锁定（US-AUTH-002）——代码正确
7. **文档修正**：05-security-password-management.md 的 BCrypt 过时描述 → PBKDF2（若代码确实 PBKDF2）

## 交付
- 审核报告：docs/compose/reports/sysadmin-logic-audit-2026-08-13.md（每项：文档要求 → 代码现状 → 符合/差异 → 修复建议）
- 差异项修复（先文档后代码：先改文档再改代码）
- build 0 错误 0 警告 + 相关测试 + 架构 87/87
- commit + push

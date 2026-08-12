# WebAPI 配置逻辑 Review（2026-08-12）

> 触发：登录 401 排查中用户质疑「现在是开发环境，配置逻辑是否没生效」
> 范围：环境判定 → 配置加载链 → 密码链路 → 校验门控 → 测试环境适配
> 方法：代码实证（Program.cs / IdentitySeedData / ProductionConfigurationValidator / DatabaseInitializationService）+ 服务器实况（start.sh / 日志）

---

## 一、结论先行

**配置逻辑本身是生效的**（覆盖链完整、校验器工作正常）——**问题在于测试环境错误地使用了 Production 环境名**，触发了一整套「生产安全加固」逻辑（K4 密码门控 + AllowAutoCreate 门控 + 种子跳过），导致 sysadmin 默认密码初始化未按预期执行。

**用户判断方向正确**：确实「配置逻辑让测试环境走了生产路径」。

---

## 二、配置加载链（实证）

```
Program.cs (line 70-113)
├── .env 文件加载（Development → .env.development；其他 → .env）✅ 存在
├── WebApplication.CreateBuilder（内置 AddEnvironmentVariables）✅ 环境变量覆盖
├── runtime-overrides.json（config/，热更新 IOptionsMonitor）✅ B-02
└── appsettings.json + appsettings.{Environment}.json（按环境名加载）✅ 标准
```

**覆盖优先级**（高→低）：环境变量（双下划线）> runtime-overrides.json > appsettings.{env}.json > appsettings.json

**验证**：start.sh 的 `Jwt__SecretKey` 等环境变量**能正确覆盖** appsettings.Production.json 的 `${JWT_SECRET}` 占位符（ASP.NET Core 标准机制，实测登录返回 JWT 证明 Jwt 配置生效）。

---

## 三、环境判定（核心问题）

| 环境 | 判定依据 | 影响 |
|------|---------|------|
| **当前服务器**（60.190.215.86:5000 测试） | start.sh `ASPNETCORE_ENVIRONMENT=Production` | 走全部生产加固 |
| 测试应设 | `Development` 或专用 `Test`（appsettings.Test.json 已存在！） | 走开发逻辑 |

**关键发现**：项目**已有 `appsettings.Test.json`**（之前测试发布时创建的），但 start.sh 用的是 Production——**测试环境配置存在但没被启用**。

---

## 四、密码链路（K4 安全加固，设计正确但测试环境误触发）

```
IdentitySeedData.SeedRolesAndAdminAsync（启动无条件执行）
└── ResolveSysAdminPassword(environment, configuredPassword)
    ├── 非 Production → 返回配置默认密码（开发逻辑）
    └── Production → 必须环境变量 DefaultPasswords__SysAdminPassword（K4 加固）
                     缺失 → 抛异常（禁止默认密码回退）✅ 安全设计
└── EnsureUserAsync（LastLoginTime==null 且密码非默认 → ResetPasswordAsync 重置）
```

**实测**：start.sh 的 `DefaultPasswords__SysAdminPassword` = 某值（14 字符，非 Admin@Lybt2026）→ 种子**已把 sysadmin 密码重置为该值**（我们置 LastLoginAt=NULL 后重启触发）→ 登录 401 是因为**我用错了密码**（start.sh 值 ≠ 测试默认值）。

---

## 五、校验门控（DatabaseInitializationService）

```
EnsureSystemAdminExistsAsync
├── 生产 + AllowAutoCreateInProduction=false → 跳过创建（日志：已证实）
└── ForceResetOnStartup（仅 Development + true）→ 重置状态（不重置密码哈希！）
    ⚠️ 注释说「密码已重置」但代码只重置 IsSysAdmin/Lockout/Status——文档与代码不一致
```

---

## 六、发现的问题清单

| # | 严重度 | 问题 | 证据 |
|---|--------|------|------|
| 1 | 🔴 | **测试环境误用 Production 环境名**——appsettings.Test.json 存在但 start.sh 用 Production，触发全部生产加固（K4/门控/种子跳过），sysadmin 密码初始化路径错误 | start.sh vs appsettings.Test.json |
| 2 | 🟡 | **ForceResetOnStartup 注释与代码不一致**——注释「密码及锁定状态已重置」，代码只重置锁定状态不重置密码哈希（会误导运维以为重置了密码） | DatabaseInitializationService.cs:166-176 |
| 3 | 🟡 | **K4 密码来源链复杂**——ResolveSysAdminPassword 支持 `DefaultPasswords__SysAdminPassword` 和 `SYSADMIN_PASSWORD` 两个环境变量名 + start.sh 值未文档化，测试者难以知道正确密码 | IdentitySeedData.cs:22-50 |
| 4 | 🟢 | **测试密码未文档化**——start.sh 的 DefaultPasswords__SysAdminPassword 值（14 字符）没有在任何文档中说明，API 测试不知道用哪个密码 | 01-deployment.md 无测试密码记录 |

---

## 七、建议（待用户决策）

**方案 A（推荐）：测试环境改用 Test 环境名**
- start.sh 改 `ASPNETCORE_ENVIRONMENT=Test`（appsettings.Test.json 已存在）
- 效果：走非生产逻辑（密码用配置默认值）、不触发 K4 生产加固、种子正常初始化
- 注意：需确认 Test 环境下 ValidateOnStart/校验器是否仍工作（测试也要测配置）

**方案 B：保持 Production 但文档化测试密码**
- start.sh 保持 Production（保留生产门控测试价值）
- 01-deployment.md 记录测试密码来源（start.sh 值 = 测试密码）
- API 测试用 start.sh 的实际值登录

**方案 C：临时改 start.sh 密码为已知测试值**
- 最简单，但污染生产配置语义（测试值进 start.sh）

**我的推荐**：**A**——appsettings.Test.json 已存在（设计意图就是测试环境），测试环境就该用 Test 环境名，让配置逻辑走「非生产」路径（用户说的「开发环境配置逻辑」就生效了）。

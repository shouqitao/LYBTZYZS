# WebAPI 后端测试方案设计（专家评估 + 分层方案）

> 日期：2026-08-12 | 状态：设计稿（待用户确认） | 范围：**仅 WebAPI 后端**（Desktop 测试另行设计）
> 依据：上线实战 10 坑 + 测试实证盘点 + ASP.NET Core 官方 WebApplicationFactory 模式 + Respawn 测试隔离

---

## 一、现状评估（实证）

### 1.1 现有测试体系盘点

| 层 | 数量 | 覆盖 | 评价 |
|----|------|------|------|
| Server Unit | 57 文件 / 620+ 用例 | Service/Validator/Repository 逻辑 | ✅ 守护业务逻辑（0 mock 是优点） |
| Desktop Unit | 54 文件 | VM/StateMachine | ✅ 桌面逻辑 |
| Desktop Integration | 34 文件 | **LocalWebAPI** 真实启动（5300） | ⚠️ 守护本地端，非 Remote |
| Architecture | 7 文件 / 87 规则 | 依赖方向/架构约束 | ✅ 架构守门 |
| **Remote WebAPI 系统级测试** | **0** | **从未真实启动 Remote WebAPI** | ❌ **核心盲区** |

### 1.2 关键事实

- `Microsoft.AspNetCore.Mvc.Testing`（WebApplicationFactory）**已引用但从未使用**
- `Respawn`（数据库重置）**已引用但从未使用**
- InMemory 数据库 **11 处**（不校验 EF 列映射 → 掩盖 DbContext 配置错误）
- 真实 SQL Server **0 处**
- Program.cs 为 `public class Program`（注释明示「确保 WebApplicationFactory 完全兼容性」——**工具链早已备好，只差用**）

### 1.3 上线 10 坑 vs 测试覆盖

| # | 上线问题 | 类型 | 现有测试能否拦截 |
|---|---------|------|:---:|
| 1 | 路由 version 重复（启动崩溃） | 启动契约 | ❌（无全量路由注册测试） |
| 2 | IdentityDbContext 漏映射（登录崩溃） | 集成契约 | ❌（InMemory 不校验列映射） |
| 3 | SqlServerHealthCheck fallback | 集成契约 | ❌（无连接串解析测试） |
| 4 | JWT 必须 Base64 | 配置校验 | ❌（测试不跑 Production 校验器） |
| 5 | 缺 NEWUSER_PASSWORD | 配置校验 | ❌ |
| 6 | Encrypt=True 握手失败 | 环境差异 | ⚠️（需真实 SQL） |
| 7 | 环境变量双下划线 | 配置语义 | ❌（测试不模拟环境变量覆盖） |
| 8 | Swagger 生产关 | 设计决策 | ❌ |
| 9 | Swagger 401（FallbackPolicy） | 中间件契约 | ❌ |
| 10 | Swagger 空白（CSP） | 安全头契约 | ❌ |

**结论**：10 坑中 **9 个现有测试无法拦截**——因为全是「系统级」问题，而系统级测试 = 0。

---

## 二、问题诊断（为什么全绿但系统裸奔）

**测试守护了「组件正确性」，上线死的是「系统集成」**：

```
现有：Service 逻辑 → ✅ 单点正确
缺：  真实启动 → 路由表 → 中间件链 → 配置生效 → 真实 SQL 映射
```

**三个结构性盲区**：
1. **启动盲区**：无 WebApplicationFactory → 路由注册/启动校验/中间件装配从未被验证
2. **配置盲区**：测试跑 Development 配置，上线跑 Production + 环境变量 → 配置校验从未被验证
3. **映射盲区**：InMemory 不校验 EF 列映射 → DbContext 配置错误从未暴露

---

## 三、目标（新测试标准）

**测试价值 = 守护「系统能交付」**，而不只是「组件能工作」。四层守护：

```
L1 单元层（现有，保留）→ 业务逻辑正确
L2 集成层（新建）     → 真实 SQL + 真实 DbContext 配置
L3 系统层（新建）     → WebApplicationFactory 真实启动 + 真实请求
L4 部署层（新建）     → Production 配置 + 占位符 + 环境变量模拟
```

---

## 四、WebAPI 后端测试方案设计

### L2 集成层（真实 SQL 映射守护）

**目标**：杀掉 InMemory 盲区——验证 EF 配置（列映射/关系/索引）与真实 SQL Server 一致

| 测试 | 验证点 | 覆盖坑 |
|------|--------|:---:|
| DbContext 映射一致性 | 每个 DbContext（App/Identity/Catalog/MedicalCase）对真实 SQL 建表 → 无 Invalid column | #2 |
| 全实体映射冒烟 | `EnsureCreatedAsync` 真实库 → 查询每个实体表 | #2 |

**技术**：真实 SQL Server（本机 LocalDB 或 192.168.190.243 测试库）；`CreateScope` + `EnsureCreated` 后断言表存在

### L3 系统层（真实启动守护）——**核心**

**目标**：WebApplicationFactory 真实启动 Remote WebAPI，验证「能启动 + 能响应」

| 测试 | 验证点 | 覆盖坑 |
|------|--------|:---:|
| **启动冒烟** | Factory 启动成功（无启动崩溃） | #1 |
| **全量路由注册** | GET /swagger/v1/swagger.json 200（隐含 MapControllers 成功） | #1 |
| **健康检查** | GET /health → Healthy；/health/database → 可达 | #3 |
| **登录链路** | POST /api/v1/auth/login → token 非空 | #2 |
| **中间件链** | X-Correlation-ID 响应头存在 | — |
| **FallbackPolicy** | 未认证访问 /api/v1/patients → 401 | #9 |
| **安全头** | 业务路径响应含 CSP（严格）；/swagger 路径宽松 | #10 |
| **下载页** | GET / → 200 含下载链接 | — |

**技术**：`WebApplicationFactory<Program>` + `WithWebHostBuilder`（注入测试配置：真实 SQL 连接串 + JWT 测试密钥 + Swagger:Enabled=true）；`Respawn` 每用例重置数据库

### L4 部署层（配置契约守护）

**目标**：Production 配置 + 占位符 + 环境变量模拟——让配置校验在测试中跑一遍

| 测试 | 验证点 | 覆盖坑 |
|------|--------|:---:|
| **占位符校验** | 模拟 Production 配置（含 ${} 占位符）→ 校验器拦截未展开 | #4 #5 #7 |
| **环境变量注入** | 设置 `Jwt__SecretKey`（Base64）等 → 校验通过 | #4 #7 |
| **JWT Base64 校验** | 非法 Base64 → 启动拦截 | #4 |
| **密码策略校验** | 缺 NEWUSER_PASSWORD → 启动拦截 | #5 |

**技术**：`WithWebHostBuilder(b => b.UseEnvironment("Production"))` + `SetBasePath` 指向真实 appsettings + 注入环境变量

---

## 五、优先级与工作量（按 ROI）

| 批次 | 内容 | 拦截坑 | 工作量 |
|------|------|--------|--------|
| **P0** | L3 启动冒烟 + 健康 + 登录（WebApplicationFactory 基建） | #1 #2 #3 #9 | 0.5-1 天 |
| **P1** | L2 DbContext 映射（真实 SQL 建表冒烟） | #2 | 0.5 天 |
| **P2** | L4 配置契约（Production 模拟 + 环境变量） | #4 #5 #7 | 0.5-1 天 |
| **P3** | L3 扩展（中间件/安全头/下载页/CSP） | #6 #10 | 0.5 天 |

**总计约 2-3 天**——让 10 坑中 9 个进入自动检查。

---

## 六、技术要点

1. **WebApplicationFactory<Program>**：Program 已 public（兼容性已保证），无需改动生产代码
2. **测试数据库**：本机 SQL Server（真实实例）——用 `Respawn` 每用例重置，不污染数据；连接串注入测试配置
3. **配置注入**：`WithWebHostBuilder` + `UseSetting` / 环境变量——不写死测试配置到代码
4. **隔离**：每个测试类独立数据库名（如 LYBT_Test_{ClassName}），并行安全
5. **不需要新包**：Mvc.Testing / Respawn / SqlServer 均已引用

---

## 七、验收标准

- P0 完成后：**发布前跑 L3 冒烟 = 能拦截 #1 #2 #3 #9**（上线 10 坑中 4 个进自动检查）
- 全部完成后：10 坑中 **9 个进自动检查**（#6 环境差异部分覆盖、#8 设计决策除外）
- 测试运行时间：P0 套件 < 60s（可并入常规测试）

---

## 八、待确认决策

1. **测试数据库用哪个**：本机 SQL Server（推荐，快）/ 192.168.190.243 测试库（需网络）
2. **P0 先做还是全做**：建议 P0（0.5-1 天）先落地验证方案，再按 ROI 推进
3. **是否纳入 CI/发布门禁**：P0 完成后，发布前必须跑 L3 冒烟（进 Runbook 第 0 步）

# 任务 A-18 第 2 子批次：契约统一 + Desktop 边界守卫（高风险）

> 前置：A-18 第 1 子批次完成（P1-7 文档/P1-3 CorrelationId/P1-4 Mapper/P1-6 本地配置）
> 本批次为 A-18 剩余三项：P1-1 契约双套统一、P1-2 领域客户端双实现收敛、P1-5 Desktop 模块边界守卫
> **风险最高，先勘察出方案，技术总监确认后再执行**

## 执行原则

1. **先勘察出方案，禁止直接动手**：每项先输出方案（选项 + 影响面 + 风险），技术总监确认后执行
2. **行为等价优先**：收敛只改结构，不改业务语义
3. **双轨是设计意图**：SwitchingApiClient 的 IsLocal 分支语义不变

---

## 任务 1：P1-5 Desktop 模块边界守卫（先做，独立低风险）

### 1a. 解决 Registration→MedicalCase 违规

**现状**：`LYBT.Desktop.Registration.csproj:67` 直接引用 `LYBT.Desktop.MedicalCase`，只因 `RegistrationListViewModel.cs` 用导航契约：
- `MedicalCaseNavigationParameters`（定义在 `Modules/LYBT.Desktop.MedicalCase/Models/`）
- `WorkspaceMode` / `EditState`
- `ViewNames.MedicalCaseWorkspace`（定义在 `Infrastructure/Constants/ViewNames.cs`）

**动作**（对齐 A-10 先例：接口/契约下沉 Contracts）：
- 将 `MedicalCaseNavigationParameters`/`WorkspaceMode`/`EditState` 下沉 `LYBT.Desktop.Contracts`（与 ViewNames 所在层级一致）
- Registration 改引用 Contracts，去掉对 MedicalCase 模块的 ProjectReference
- 检查 Clinical 等其它使用方同步更新 using

### 1b. 新增架构测试（Desktop 模块间禁止直接引用）

**现状**：P07 只守 Server（`P07_ServerModules_Should_Not_Reference_Other_ServerModules`）；Desktop AGENTS.md 声明「Business modules MUST NOT reference each other」但无测试强制

**动作**：
- 参照 P07 写法新增 `DP07_DesktopModules_Should_Not_Reference_Other_DesktopModules`
- 判定范围：`Modules/LYBT.Desktop.*` 之间（排除 Core/、Roles/、Shell/、LocalWebAPI/）
- 豁免清单：Roles(Admin/Clinical)→Modules、Shell→全部 是角色编排/组合根，明确豁免并注释理由
- 注意：Registration→MedicalCase 修复后应零违规，测试通过

---

## 任务 2：P1-1 契约双套统一（最高风险，先出方案）

### 现状
- `Contracts/Api/*`：11 个 Refit 特性接口（`IHerbApi` 等）
- `Contracts/ApiClient/*`：12 个统一无特性接口（`IApiClientHerbs` 等）
- 使用矩阵：
  - Api 套：RefitApiClient（远程模式 RestService.For\<T\>）+ Admin 3 个 ViewModel 直用（SystemSettings/LogLevelControl/Deployment）
  - ApiClient 套：SwitchingApiClient/HttpClientApiClient/全部桌面 Repository（55 文件引用）

### 方向（交叉验证一致）
**以 ApiClient 为统一面，Api 套内化为其远程实现细节**

### 必须先勘察的方案选项
| 选项 | 内容 | 影响 |
|------|------|------|
| A | 保留 RefitApiClient 作为内部实现（Api 接口不删，但标记 internal/移入 Foundation），对外只暴露 ApiClient | 改动小，Admin 3 VM 需改走 ApiClient |
| B | 删 Api 套接口，Refit 直接生成适配 ApiClient（RestService.For 需要 Refit 特性接口，需在 ApiClient 接口加 [Refit.Get] 等或改方式） | 改动大，11 接口 + 12 adapter 都要动 |

**执行顺序**：
1. 勘察：Admin 3 个 ViewModel 直用的 Refit 接口调用点清单
2. 勘察：RefitApiClient 内部 11 个包装方法 vs ApiClient 接口方法面是否 1:1
3. 输出方案（A/B 对比 + 影响面 + 风险），**等技术总监确认后执行**

---

## 任务 3：P1-2 领域客户端双实现收敛（与任务 2 联动）

### 现状
`Foundation/Http/Clients/` 12 对 adapter（`HerbApiClient` Refit + `HerbsHttpApiClient` HttpClient），24 个文件

### 动作
- 与任务 2 联动：契约统一后，评估 adapter 层是否可收敛
- 约束：不改 SwitchingApiClient 切换语义（IsLocal 分支保留）
- 若任务 2 选 A：双实现暂保留但确认方法面 1:1（消除 A6 漂移风险），或收敛为单实现
- 若任务 2 选 B：双实现自然坍缩，评估能否合并

---

## 验证（每项）

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：83/83 + 新 DP07 测试通过
3. 相关模块单测
4. 每项独立 commit + push

## 不做

- ❌ 不改 SwitchingApiClient 切换语义
- ❌ 不删 Admin 3 个 ViewModel 功能（只改它们走统一契约）
- ❌ 不处理 DbContext 五套并存

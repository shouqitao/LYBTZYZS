# 任务 A-21：模块级审计 P1 修复批次

> 依据：`structure-audit-module-level-crosscheck-2026-08-08.md` 交叉验证 §二 P1 清单
> 产品负责人已拍板执行

## 任务清单（6 项，按顺序）

### M4：Shell RoleDefinitionBase 模块名 bug（真实代码 bug，先修）

**现状**：`src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/RoleDefinitionBase.cs:17` 写 `"AuthModule"`，但实际模块是 `AuthenticationModule`（`LYBT.Desktop.Auth/AuthenticationModule.cs`）→ 每次登录触发被吞异常

**动作**：
- 读 RoleDefinitionBase.cs 全文，确认模块名用途（角色→模块映射？）
- `"AuthModule"` → `"AuthenticationModule"`
- 检查是否有其他模块名写错（对照各模块实际 Module 类名逐一核对：AuthenticationModule/UsersModule/PatientsModule/HerbsModule/FormulaModule/MedicalCaseModule/RegistrationModule）

### M5：3 个 VM 越层（改走 Service 层）

**现状**：
- `ReportsHomeViewModel.cs` 注入 `IApiClient`（越层直连 HTTP）
- `AuditLogViewModel.cs` 注入 `IApiClientMedicalCases`
- `RegistrationListViewModel.cs` 注入 `IApiClientPatients`

**动作**（对齐既有分层：VM→Service→Repository→IApiClient）：
- 读各 VM 使用的 API 方法，判断应走哪个 Service：
  - AuditLog → 是否有 AuditLogService？若无，在模块内新建 Service 封装 IApiClientMedicalCases 的审计日志方法
  - ReportsHome → 是否有 ReportService？Desktop Reports 应走已有 Service
  - RegistrationList → 已用 RemoteRegistrationService（A-17 后）？核实并注入 Service 而非 IApiClientPatients
- 约束：VM 只注入 Service 接口（或已有的 Repository），不直连 IApiClient

### M3：领域事件空转（11 个事件 0 订阅者）

**现状**：Herb/Formula/Auth 的 Created/Deleted/SessionCreated 等 11 个事件发布，`INotificationHandler<XxxEvent>` 订阅者 0

**动作**：
- 读领域事件机制：`InMemoryDomainEventDispatcher`（SharedKernel/Events/）+ MediatR `INotificationHandler`
- **评估**：这些事件是「未来扩展预留」还是「应删除」？
  - 若当前无消费需求（无跨模块需要响应创建/删除）：**删除事件发布代码**（CreateHerbCommandHandler 等 11 处 `new XxxEvent` + Domain/Events 文件）——YAGNI，消除空转
  - 若某事件有明确未来用途：保留并文档化「预留」
- 技术总监判断：**删除**（无订阅者的事件是死代码；未来需要时按 MediatR 模式再加）

### M2：Desktop 映射统一（F-02）

**现状**：Server 已统一 Mapperly（A-18 P1-4），Desktop DTO↔Model 仍手写（重复 2-3 处/模块）+ 3 个 Mapper 文件零引用

**动作**：
- 用 serena/codebase-memory 找 Desktop 各模块手写 DTO↔Model 映射点（MedicalCase 最多）
- 3 个零引用 Mapper 文件删除
- 手写映射改为 Mapperly `partial class + [Mapper(RequiredMappingStrategy.Target)]`（行为等价，签名不变）
- 约束：不重构业务逻辑，只改映射机制

### C1：Infrastructure 职责过载

**现状**：`Desktop.Infrastructure/` 14 类职责（Http/ViewModels/Views/CardReader/Navigation/Behaviors/Roles/Security/Helpers/Commands/Events/LocalData/Performance）

**动作**（分两个动作）：
- **LocalData 废弃**：`LocalDbContext` 生产 0 引用（已确认休眠），删除 LocalData 目录或标注废弃——与 F-03 联动
- **CardReader 独立**：评估是否移出 Infrastructure（是否值得新建 Desktop.CardReader 项目，或保留但文档化）——**技术总监倾向：LocalData 删，CardReader 暂留（改动大收益小，记录 P2）**，本次只做 LocalData 清理

### F-01 + Tools：清理批次

- **F-01 删 FeatureToggle**：14 开关已缩至 2 且无有效消费——删除 FeatureToggleOptions/相关配置/引用（YAGNI）
- **Tools 删 3 留 1**：保留 PasswordHashGenerator（运维用），删 ApiTester/LoginTester/UserInfoVerifier（3 个项目从 sln 移除 + 目录删除）

## 验证（每项独立）

1. `dotnet build LYBTZYZS.sln --no-incremental`：0 错误 0 警告
2. `dotnet test tests/LYBT.Tests.Architecture/`：85/85
3. 相关模块单测
4. 每项独立 commit + push

## 不做

- ❌ 不改 AppDbContext/模块 DbContext（A-20 已完成）
- ❌ 不重写业务逻辑（M2 只改映射机制、M5 只改注入层）
- ❌ 不新建项目（CardReader 独立为 P2，本次不做）
- ❌ 不改 Remote/Local 切换语义

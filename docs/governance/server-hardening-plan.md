# LYBT.Server 安全与架构优化实施计划

> 范围：不新增功能，仅进行安全修复、依赖治理、分层与工程优化，强化门禁与可运维性。

## 目标与优先级
- P0 立即：安全基线（移除明文凭据/密钥）
- P1 高：依赖版本治理、分层依赖清理
- P2 中：构建与门禁增强（编译/告警/ArchTests）
- P3 低：配置直读统一与重复入口清理

## 逐项变更清单 · 风险 · 验证 · 回滚

### P0 安全基线（立即执行）
- 变更
  - 移除明文连接串/密码：`src/Server/Core/LYBT.Infrastructure/appsettings.json`
  - 移除设计时默认连接串（含密码）：`src/Server/Core/LYBT.Infrastructure/Data/AppDbContextFactory.cs`
  - 生产连接串改为环境变量占位：`src/Server/Services/LYBT.WebAPI/appsettings.Production.json`
  - 移除/替换明文 JWT 密钥：`src/Server/Core/LYBT.Infrastructure/appsettings.json`
- 风险
  - 未配置环境变量/Secret 将导致启动失败；CI 与本地需预置密钥。
- 验证
  - `dotnet build LYBT.Server.sln -c Release`
  - 本地注入密钥并启动：`dotnet user-secrets set ConnectionStrings:DefaultConnection "..."`；运行 WebAPI 并连库冒烟。
- 回滚
  - 回退对应提交；触发条件：短期无法完成环境密钥配置且上线窗口受限。

### P1 依赖版本治理
- 变更
  - 将 `Microsoft.Extensions.*` 统一至 8.0.x：`Directory.Packages.props`
  - 移除 ASP.NET Core 2.x 包，改用 8.0 等价且仅 WebAPI 引用：`Directory.Packages.props`
  - `System.Text.Json` 对齐 8.0.x：`Directory.Packages.props`
  - 基础设施层移除 Web 依赖：`src/Server/Core/LYBT.Infrastructure/LYBT.Infrastructure.csproj`
  - 共享工具库移除 Swagger 依赖：`src/Shared/LYBT.Shared.Utilities/LYBT.Shared.Utilities.csproj`
- 风险
  - 编译/运行期装配冲突；少量接口变更引发编译错误。
- 验证
  - `dotnet restore && dotnet build -c Release`
  - 启动 WebAPI + 集成测试：`dotnet test tests/IntegrationTests/WebAPI.IntegrationTests -c Release`
- 回滚
  - 回退 `Directory.Packages.props` 与涉及的 `.csproj`；触发：广泛编译错误或绑定失败。

### P1 分层依赖清理
- 变更
  - 移除领域层对 DTO 的依赖：`src/Server/Core/LYBT.Entities/LYBT.Entities.csproj`
  - 保持映射在应用层（不改变行为）。
- 风险
  - 个别类型混用导致编译错误，需最小重构（不引入新功能）。
- 验证
  - 全量编译 + 核心用例冒烟（用户/患者/病案/处方）。
- 回滚
  - 回退项目引用调整；触发：短时无法修复大面积编译失败。

### P2 构建与工程
- 变更
  - 恢复默认中间输出至项目 `obj/`：`Directory.Build.props`
  - CI 开启“告警即错误”（本地可保留宽松）：`Directory.Build.props`
  - 锁定 SDK 前滚策略为 `minor` 或移除：`global.json`
- 风险
  - 个别生成器/脚本依赖非默认 `obj/`；告警治理工作量上升。
- 验证
  - `dotnet build -c Release`，EF 迁移链路：`dotnet ef migrations list --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`
- 回滚
  - 回退 `Directory.Build.props` 与 `global.json`；触发：CI/工具链不可用。

### P2 架构门禁补强
- 变更
  - 新增 ArchTest：Entities 不得依赖 `Shared.*`；共享工具库不得依赖 `Microsoft.AspNetCore.*`、`Swashbuckle.*`：`tests/Architecture/ArchTests.cs`
- 风险
  - 首次执行可能红灯，暴露历史债务。
- 验证
  - `dotnet test tests/Architecture/LYBT.ArchTests.csproj -c Release`
- 回滚
  - 临时设为非阻塞或标记忽略；触发：短期内需不阻塞发布。

### P3 配置直读统一
- 变更
  - 统一通过既有入口读取连接串/敏感配置，清理重复直读：`src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`
- 风险
  - 删除旧路径后遗漏调用点导致启动失败。
- 验证
  - 全文检索直读方法；启动 WebAPI 观察初始化日志链路。
- 回滚
  - 暂缓删除，仅标记过时；触发：仍有依赖未切换。

## 里程碑与验收
- M0（当日）：完成 P0，仓库无明文密钥；WebAPI 在 Dev 环境可启动。
- M1（+2 天）：完成 P1，构建/测试通过，无装配冲突。
- M2（+3 天）：完成 P2，CI 门禁稳定；EF 工具链正常。
- M3（+4 天）：完成 P3，配置入口统一，无重复直读。

## 执行分工（占位）
- 负责人 A：P0 安全基线（配置与密钥）
- 负责人 B：P1 依赖治理（包版本与 csproj 清理）
- 负责人 C：P1 分层清理（项目引用与最小映射）
- 负责人 D：P2 构建/CI 与 ArchTests 补强

## 验证清单（交付前）
- `dotnet build LYBT.Server.sln -c Release` 通过
- `dotnet test tests -c Release` 与 `tests/Architecture` 全绿
- WebAPI 使用环境密钥可启动并连库
- 关键用例冒烟：用户、患者、病案、处方无回归

---

备注：若需要，我可补充对应变更的 PR 切片模板与回滚脚本清单，便于分阶段合并与快速恢复。


# EPIC｜服务端单元测试全覆盖与测试报告（凌隐宝堂中医诊所）

- 关联 PRD：docs/ccpm/PRD-server-coverage-20250921.md
- 技术负责人：待指派

## 一、技术方案

- 覆盖率与报告
  - 工具链：Coverlet（XPlat Code Coverage）+ ReportGenerator
  - 输出：Cobertura（CI 消费）+ HTML（人工阅览），统一至 `BIN/TestResults/coverage`
  - 项目级 runsettings 或 `Directory.Build.props` 统一配置采集与排除规则

- 测试分层与数据策略
  - 单元测试优先：模块服务层（Auth/Users/Patients/...）与基础设施组件（配置/缓存/日志）
  - 数据库相关：
    - 关系型语义用 SQLite In-Memory + 迁移（支持唯一索引/关系约束）
    - 纯内存逻辑可用 EF InMemory（快）
  - 时间/随机：注入 IClock/RandomProvider，确保确定性

- Mock 策略
  - Moq/AutoFixture 组合，隔离外部依赖（IAuthService、仓储、缓存适配器等）
  - Verify 用于快照断言（DTO/响应模型），减少脆弱断言

- 架构门禁
  - 现有 `LYBT.ArchTests` 保持/扩展，禁止 UI 依赖渗透、限制命名与禁用框架

- CI 集成
  - `dotnet test` 执行后触发 `reportgenerator` 生成报告
  - 设定覆盖阈值（Line ≥90%，Branch ≥80%），低于阈值任务失败

## 二、目录与产物

- 产物目录：`BIN/TestResults/coverage`
  - `coverage.cobertura.xml`
  - `index.html`（HTML 报告入口）

- 脚本（建议）：
  - `scripts/test-coverage.ps1`（Windows）
  - `scripts/test-coverage.sh`（Unix）

## 三、关键决策

- EF 测试默认 SQLite In-Memory（关系型特性更贴近生产）
- 覆盖阈值落地到 CI（Fail fast），防止回退
- 面向接口编程，允许为可测试性最小重构（非功能性）

## 四、任务拆分

- 001｜覆盖工具与报告通路打通（统一配置+脚本）
- 002｜基础设施层测试（配置绑定/缓存/日志/DbInit）
- 003｜认证模块（登录/登出/刷新/锁定/异常）
- 004｜用户模块（启禁用/重置密码/资料/分页）
- 005｜患者模块（增改查/边界）
- 006｜病历/问诊/处方（一对一/状态机/并发/价格精度）
- 007｜WebAPI 控制器（最小单元，尽量以集成覆盖）
- 008｜报告归档与 CI 门禁

## 五、验收

- 本地与 CI 生成一致报告（Cobertura+HTML）
- 覆盖率达到 PRD 指标；阈值集成至 CI
- 所有测试在 Release 配置下通过

## 六、风险与回退

- SQLite 与 SQL Server 差异：若出现差异用例，增加 SQL Server 容器化集成测试覆盖关键约束
- 不稳定测试：引入 TestCategory/Skip 策略并限期修复

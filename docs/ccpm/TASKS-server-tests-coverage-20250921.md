# TASKS｜服务端单元测试全覆盖与测试报告（凌隐宝堂中医诊所）

- 关联 EPIC：docs/ccpm/EPIC-server-tests-coverage-20250921.md

## 任务清单（按优先级）

- 001｜覆盖与报告通路
  - 交付：统一 runsettings / Directory.Build.props；脚本 `scripts/test-coverage.ps1/.sh`
  - DoD：执行脚本产出 `BIN/TestResults/coverage/index.html` 与 `coverage.cobertura.xml`
  - 验证：`dotnet test tests -c Release --no-build` + `reportgenerator`

- 002｜Infrastructure 测试
  - 覆盖：配置绑定（IOptions）；缓存适配（命中/失效/过期）；DbInit 服务（成功/失败路径）
  - DoD：关键分支断言与异常路径覆盖到位，行覆盖≥90%

- 003｜Auth 模块
  - 覆盖：登录/登出/刷新/令牌校验/失败与锁定策略/异常映射
  - DoD：服务层与边界覆盖≥95%

- 004｜Users 模块
  - 覆盖：启禁用、重置密码（默认密码服务）、资料变更、分页筛选、错误码
  - DoD：服务层行覆盖≥95%；DTO 映射快照通过

- 005｜Patients 模块
  - 覆盖：创建/更新/查询/边界与异常
  - DoD：行覆盖≥90%

- 006｜MedicalCase/Consultation/Prescription
  - 覆盖：一对一关系、状态流转（Draft/Active/Completed/Cancelled）、并发控制（RowVersion）、价格精度
  - 数据：SQLite In-Memory + 迁移
  - DoD：行覆盖≥95%，核心状态机分支覆盖≥80%

- 007｜WebAPI 控制器（最小）
  - 策略：以集成为主，控制器仅补充参数验证与异常到 ProblemDetails 的映射
  - DoD：关键端点可通过集成测试稳定覆盖

- 008｜CI 门禁与报告归档
  - 交付：将覆盖阈值写入 CI；归档 HTML/Cobertura 到制品库
  - DoD：低于阈值 CI 失败；报告可下载/浏览

## 估时与并行化建议

- T001/T008 串行（工具与 CI）
- 模块任务（002–007）并行，按模块负责人拆解

## 命令与产物（示例）

- 本地执行（示例）：
  - `dotnet restore LYBT.Server.sln`
  - `dotnet test tests -c Release --collect:"XPlat Code Coverage" --results-directory BIN/TestResults`
  - `reportgenerator -reports:BIN/TestResults/**/coverage.cobertura.xml -targetdir:BIN/TestResults/coverage -reporttypes:Html;Cobertura`

- 输出：
  - `BIN/TestResults/coverage/index.html`
  - `BIN/TestResults/coverage/Cobertura.xml`

> 注意：以上为文档与实施计划，不对代码做改动。执行前需团队确认阈值与报告产物位置。

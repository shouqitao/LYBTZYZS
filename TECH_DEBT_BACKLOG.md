# 技术债务待办清单（TECH_DEBT_BACKLOG）

说明

- 优先级：P0（最高）/ P1 / P2
- 预估工作量：S（<0.5d）/ M（0.5–2d）/ L（>2d）
- 不改业务逻辑即可先落地的事项，优先安排到最近两次迭代

## 安全与配置（Security & Config）

- [P0][S] 移除 .gitignore 反忽略发布配置，防止敏感回流
  - 证据：`.gitignore:30`（!BIN/LybtWebApi/Release/appsettings*.json）
  - 修复：删除该条；强制忽略 BIN/**、bin/**、obj/**、logs/**、out/**
- [P0][S] 限制开发配置仅本地使用；生产加载安全模板
  - 证据：`src/Server/Services/LYBT.WebAPI/appsettings.json:3,6`
  - 修复：生产仅加载 `appsettings.Security.json` + 环境变量；给开发文件加醒目标注
- [P0][S] 禁止日志输出默认口令/敏感值
  - 证据：`Infrastructure/DatabaseInitializationService.cs:459`
  - 修复：掩码或删除此类日志；在 PR 检查中加入敏感关键字扫描

## 持续集成与构建（CI/CD & Build）

- [P0][M] 新增 GitHub Actions：build/test/format + 覆盖率门槛（Line/Branch/Method ≥70%）
  - 证据：`.github/workflows` 缺失
  - 修复：添加最小 ci.yml，启用 Coverlet total 阈值
- [P1][S] 锁定 SDK 版本
  - 证据：`global.json` 缺失
  - 修复：添加 `global.json` 与 CI 对齐
- [P1][M] 统一输出路径至根 `BIN/`（或 artifacts/）
  - 证据：多处 `<BaseOutputPath>`（Modules/Core 若干 csproj）
  - 修复：`Directory.Build.props` 统一 `BaseOutputPath/BaseIntermediateOutputPath`，逐项目移除分散配置
- [P1][S] 统一行尾/编码策略（跨平台）
  - 修复：`.gitattributes` 指定 `*.yml/*.sh LF`、`*.ps1/*.cmd CRLF`

## EF/数据库一致性（Schema Alignment）

- [P0][M] Users：列名与长度一致化
  - 证据：`UserModel.cs:26 [Column("Username")]` vs `AppDbContext.cs:116 HasColumnName("UserName")`；`RealName/PasswordHash` 长度不一致
  - 修复：统一列名/长度并生成迁移；索引同步
- [P0][M] Herbs：多字段长度一致化
  - 证据：`HerbModel.cs:30,35,40,61,66` vs `AppDbContext.cs:259–264`
  - 修复：统一至业务需要上限（如 500），生成迁移
- [P1][S] 统一 decimal 精度/类型（HasPrecision/HasColumnType）

## 契约与分层（API/DTO/Interfaces/Repositories）

- [P0][S] 移除重复接口，Shared 为唯一协议层
  - 证据：`IPrescriptionApi`（Shared vs Module）
- [P0][S] Controller 禁止定义 *Dto
  - 证据：`MedicalCaseController.cs:380,385,395` vs Shared IMedicalCaseApi Dto
- [P1][S] 仓储接口继承统一：`I{Domain}Repository : IBaseRepository<TEntity>`
- [P1][S] 模块接口目录统一至 `Modules/{Module}/Interfaces`（清理 `Services/Interfaces`）

## 命名与风格（Naming/Style/Nullable/StyleCop）

- [P0][S] 统一 Username 命名（禁止 UserName）
  - 证据：AppDbContext 列名 `UserName`
  - 修复：以 `Username` 为主，保留/迁移列别名；PR 扫描阻止 `\bUserName\b`
- [P0][S] 修复 `.editorconfig` 控制字符/乱码
- [P1][M] Nullable 治理：新代码零 CS86xx，分期移除 `NoWarn`（CS8618/CS8625/CS8622）
- [P1][M] StyleCop 收紧：先警告收集 SA1025/SA1202，后提升为 error

## WPF/Prism 架构与性能（UI/Perf）

- [P0][M] 去除 .Wait()/GetResult()/同步 Dispatcher.Invoke
  - 证据：`SystemWorkbenchNavigator.cs:160–195`、`PrismDialogService.cs:210`、多处 `Dispatcher.Invoke`
- [P1][L] 巨型 VM 拆分（Patients/Prescriptions/Shell/Consultation）
  - 目标：<400 行/VM，命令 ≤10；业务下沉至 Facade/Service
- [P1][M] 虚拟化控件与大列表性能复核（批量更新/DeferRefresh/增量加载）

## 测试与覆盖率（Testing & Coverage）

- [P0][M] 覆盖率门槛落地（total 行/分支/方法 ≥70%）
- [P1][M] 关系型 Provider 替换：用 SQLite In‑Memory 覆盖 ExecuteUpdate 分支
  - 证据：Users.UnitTests 执行失败（InMemory Provider 不支持）
- [P1][S] 清理 tests/**/TestResults；PR/CI 禁止产物入库

## 日志与异常（Logging & Exceptions）

- [P0][S] 敏感日志清理：禁止输出密码/令牌/连接串
  - 证据：`DatabaseInitializationService.cs:459`
- [P1][S] Console/Debug 替换为结构化日志（服务端/客户端）
- [P1][S] 统一全局异常返回体扩展字段（避免敏感信息）

## 依赖与许可证（Dependencies & Licenses）

- [P1][S] 统一 AutoMapper 版本（生产/测试一致）并记录许可证（MIT）
- [P1][S] 生成依赖/许可证清单（SBOM）；启用漏洞/过期检查
  - 命令：`dotnet list LYBT.All.sln package --include-transitive --vulnerable --outdated`

## 文档一致性（Docs vs Code）

- [P1][S] 修复 src/**/README.md 断链与过时示例（UserName/GetDescription 等）
- [P2][M] 公共 API/服务 XML 注释与 README 对齐一次（标注已实现/规划/弃用）

---

里程碑建议

- 里程碑 M1（安全与门禁）：P0 全部+CI 最小落地（1–2 周）
- 里程碑 M2（一致性与覆盖）：EF 对齐、命名与 Style/Nullable、测试 Provider（2–3 周）
- 里程碑 M3（UI/性能与文档）：巨型 VM 首批拆分、资源与虚拟化复核、文档对齐（>3 周，滚动推进）

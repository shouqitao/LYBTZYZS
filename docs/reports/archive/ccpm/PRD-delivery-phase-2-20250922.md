# PRD——项目交付（Phase 2：WebAPI 与 Desktop 可运行）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：在“可编译”基础上，实现 WebAPI 与 Desktop 可运行的最小交付）

## 背景（Problem & Context）
- All 解决方案已在 Release 模式编译通过（0 错误）。
- 下一目标是“可运行交付”：WebAPI 能启动并提供基本接口；Desktop 能启动并完成最小 API 访问与界面展示。

## 目标（Goals）
- WebAPI：本地可启动（Dev 环境），Swagger 可访问，核心列表接口可 200 返回（允许空数据）。
- Desktop：可启动到主壳，至少 1 个模块数据加载动作执行成功（允许空列表）。
- 文档：运行手册与配置项补全（开发机最短路径），问题排障清单。

## 非目标（Non-Goals）
- 不新增业务功能与大型 UI 变更。
- 不引入新框架或重构领域模型。
- 不要求打包安装程序/容器镜像（可在后续 Phase 3 处理）。

## 范围（Scope）
- In Scope：
  - WebAPI 本地启动、端口/证书/路由校验、Swagger/OpenAPI 导出
  - Desktop 本地启动、API 基址配置、最小数据拉取
  - 运行文档更新、排障（端口占用、证书、CORS/防火墙）
- Out of Scope：
  - 生产级部署脚本、容器化/CI 发布、安装包

## 需求（Requirements）
- R1 WebAPI 启动与访问：
  - 运行方式：
    ```bash
    dotnet run --project src/Server/Services/LYBT.WebAPI
    ```
  - 端口与协议：遵循 `Program.cs` 中的 `ASPNETCORE_URLS`，如未设置默认 `http://localhost:8080`（或依据现有配置）。
  - 可访问：
    - Swagger: `http://localhost:8080/swagger/index.html`
    - OpenAPI: `http://localhost:8080/swagger/v1/swagger.json`
    - 核心接口（示例）: `GET /api/v1/users`、`GET /api/v1/patients`（200 返回，允许空集合）
- R2 Desktop 启动与最小动作：
  - 运行方式：
    ```bash
    dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
    ```
  - 配置要求：在 Desktop 配置（或环境变量）中设置 `ApiBaseUrl` 指向 WebAPI 基址（与 R1 一致）。
  - 可验证：启动到主壳，用户/患者等列表页可执行“刷新/搜索”并获得 200 响应（允许空集合）。
- R3 文档与排障：
  - `docs/runbook.md` 增补“端口/证书/防火墙”排障段落（端口占用、Kestrel 证书、CORS 提示）。
  - `docs/api/README.md` 增补最小 OpenAPI 导出命令与示例。
  - `README.md` 在“快速开始/运行”段落补充 Phase 2 运行说明链接。

## 成功度量（Success Metrics）
- 本地启动：WebAPI/Swagger 可访问，Desktop 主壳可见。
- 核心接口：`/api/v1/users`、`/api/v1/patients` 触发 200 响应（允许空集合）。
- 文档：最短运行路径与排障可独立复现（第三方同事可按文档成功启动）。

## 验收标准（Acceptance Criteria）
- 启动与验证：
  ```bash
  # WebAPI
  dotnet run --project src/Server/Services/LYBT.WebAPI
  curl -i http://localhost:8080/swagger/v1/swagger.json
  curl -i http://localhost:8080/api/v1/users

  # Desktop（手工启动，观察主界面与列表动作）
  dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
  ```
- 文档更新：`docs/runbook.md`、`docs/api/README.md`、`README.md` 已补全并可复现。

## 里程碑（Milestones）
- 提交 1：运行手册与 API 文档更新；最小配置校对（ApiBaseUrl 与端口一致）。
- 提交 2：本地启动验证并记录排障清单；更新 README 快速链接。

## 风险与缓解（Risks & Mitigations）
- 端口/证书：端口被占用或证书问题 → 修改 `ASPNETCORE_URLS` 或使用 HTTP 本地端口；临时禁用证书校验（仅本地）。
- CORS/防火墙：跨域或防火墙阻拦 → 本地放行端口、在开发配置中允许本地来源（仅开发环境）。
- 数据库：需要空数据亦可运行 → 允许空列表响应，不强制迁移/种子（若迁移失败则不阻塞本 PRD）。

## 测试计划（Testing）
- 手动验证：按“验收标准”执行；记录失败截图与日志路径。
- 可选脚本：添加 `scripts/validation/run-webapi.ps1` 简化启动与 curl 验证（非必须）。

## 交付物（Deliverables）
- 可运行的 WebAPI 与 Desktop（开发机）
- 更新后的运行文档与排障清单


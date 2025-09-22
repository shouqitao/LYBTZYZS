name: PRD-delivery-phase-2-20250922
status: backlog
description: 项目交付 Phase 2（WebAPI 与 Desktop 可运行）

# PRD——项目交付（Phase 2：WebAPI 与 Desktop 可运行）

- 文档日期：2025-09-22
- 项目：ccpm（Claude Code Project Manager）
- 关联范围：`LYBT.All.sln`（目标：在“可编译”基础上，实现 WebAPI 与 Desktop 可运行的最小交付）

## 需求（Requirements）
- R1：WebAPI 本地启动，Swagger 与核心接口可访问（200，允许空）。
- R2：Desktop 本地启动，最小数据拉取动作完成（允许空集合）。
- R3：运行文档与排障补全（runbook/api/README）。

## 验收（Acceptance）
```bash
# WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI
curl -i http://localhost:8080/swagger/v1/swagger.json
curl -i http://localhost:8080/api/v1/users

# Desktop
dotnet run --project src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj
```


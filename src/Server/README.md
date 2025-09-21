# Server 入口（后端）

本页作为后端解决方案的入口与导航。详细规范以根 `README.md` 和 docs/ 下专题为准。

## 结构
- Core
 - `LYBT.Entities`：实体与基础模型
 - `LYBT.基础设施（基础设施（Infrastructure））`：数据访问、配置、安全、仓储、Web 基础
- Modules（8 个业务模块）
 - Auth / Users / Patients / MedicalCase / Consultation / Prescriptions / Herbs / Formula
- Services
 - `LYBT.WebAPI`：统一 API 网关，模块注册与对外暴露

## 运行与调试
```bash
# 运行 WebAPI（默认开发环境）
dotnet run --project src/Server/Services/LYBT.WebAPI
# Swagger: https://localhost:7001/swagger/index.html
```

## 模块注册（与文档一致）
- 在 `LYBT.WebAPI` 中通过扩展方法注册模块，例如：
 - `services.AddAuthModule();`
 - `services.AddUsersModuleServices();`
 - `services.AddPatientsModuleServices();`
 - 其余模块：`AddMedicalCaseModule()`、`AddConsultationModule()`、`AddPrescriptionsModule()`、`AddHerbsModule()`、`AddFormulaModule()`

## 路由与版本
- 控制器特性：`[ApiVersion("1")]` + `[Route("api/v{version:apiVersion}/[controller]")]`
- 前端固定 `/api/v1/*` 前缀，与上述约定天然匹配

## 参考
- 架构概览: docs/architecture/overview.md
- API 总览: docs/api/README.md
- 配置与环境: docs/configuration.md
- 运行手册: docs/runbook.md
- PRD 工作流: 根 README 的“PRD 工作流（CCPM）”小节



## 🎯 项目概述
- [待补充] 简要描述 Server 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

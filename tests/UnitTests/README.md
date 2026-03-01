# Legacy 单元测试

> 按模块组织的精细化单元测试 | 独立于核心测试项目

本目录包含按源码模块一一对应组织的单元测试项目，测试粒度精确到
Service、Repository、Controller、Middleware 等各层级。

## 项目列表

| 项目 | 被测模块 | 测试文件数 |
|------|----------|-----------|
| Server/Core/LYBT.Infrastructure.Tests | 基础设施层 (BaseService, BaseRepository, Serialization, CrossModuleQuery) | 4 |
| Server/Modules/LYBT.Module.Auth.Tests | 认证模块 (AuthService, JwtService, SecurityAudit, TokenRevocation) | 6 |
| Server/Modules/LYBT.Module.Formula.Tests | 验方模块 (FormulaService) | 1 |
| Server/Modules/LYBT.Module.Herbs.Tests | 中药模块 (HerbRepository, HerbService) | 2 |
| Server/Modules/LYBT.Module.MedicalCase.Tests | 医案模块 (CommandService, QueryService, StateService) | 3 |
| Server/Modules/LYBT.Module.Patients.Tests | 患者模块 (PatientsController, PatientRepository, PatientService) | 3 |
| Server/Modules/LYBT.Module.Sync.Tests | 同步模块 (ChecksumHelper, SyncService) | 2 |
| Server/Modules/LYBT.Module.Users.Tests | 用户模块 (UserService) | 1 |
| Server/WebAPI/LYBT.WebAPI.Tests | Web API 层 (Authorization, Controllers, Extensions, Middleware) | 6 |
| Shared/LYBT.Shared.Configuration.Tests | 配置扩展、Options 验证、配置加载集成 | 6 |
| Shared/LYBT.Shared.ExceptionHandling.Tests | 错误码、异常类型、ProblemDetails 工厂 | 4 |
| Shared/LYBT.Shared.Models.Tests | 分页查询基础 DTO | 1 |
| Shared/LYBT.Shared.Validators.Tests | 各模块输入 DTO 验证器 (Auth, Consultation, Formula, Herbs, MedicalCase, Patients, Prescriptions, Users) | 10 |

**总计**: 13 个测试项目, 49 个测试文件

## 与核心测试项目的关系

`LYBT.Tests.Unit` 是整合测试入口，聚合跨模块通用测试场景。
此目录下的项目按模块独立组织，每个项目单独引用被测模块，依赖关系更清晰。
两者互补: 核心项目覆盖整合场景，Legacy 测试覆盖模块内部细节。

## 运行方式

全部运行: `dotnet test tests/UnitTests/ --recursive`

单模块: `dotnet test tests/UnitTests/Server/Modules/LYBT.Module.Auth.Tests/`

## 补充说明

- 所有项目使用 xUnit 测试框架，目标框架为 net8.0
- 依赖 LYBT.Tests.Configuration 提供的 TestBase、InMemory 数据库等基础设施
- `ComprehensiveTestSuite.md` 记录了完整测试套件的规划与覆盖目标

## 更新记录

- 2026-03-01: 创建 README 文档

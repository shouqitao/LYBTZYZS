# LYBT.Desktop.IntegrationTests

> Desktop 端端到端集成测试，覆盖业务流程、导航流程、HTTP 弹性策略、本地模式数据源

## 项目定位

- **层级**: IntegrationTests
- **被测模块**: Desktop (Auth, MedicalCase, Patients, Herbs, Formula, Users, Sync, Shell)
- **状态**: Active

## 测试文件

| 文件 | 被测类/端点 |
|------|------------|
| BusinessFlowE2ETests | 业务流程端到端 |
| FormulaE2ETests | 验方模块端到端 |
| HerbE2ETests | 药材模块端到端 |
| MedicalCaseAggregateE2ETests | 医案聚合根端到端 |
| MedicalCaseE2ETests | 医案模块端到端 |
| NavigationFlowE2ETests | 导航流程端到端 |
| PatientE2ETests | 患者模块端到端 |
| PrescriptionE2ETests | 处方端到端 |
| UserE2ETests | 用户模块端到端 |
| RetryPolicyIntegrationTests | HTTP 弹性重试策略 |
| TokenRefreshHandlerIntegrationTests | Token 刷新处理器 |
| AuthenticationIntegrationTests | 认证流程集成 |
| DataSourceIntegrationTests | 本地/远程数据源切换 |
| LoginFlowIntegrationTests | 登录流程集成 |

## 运行方式

```bash
dotnet test tests/IntegrationTests/Client/Desktop/LYBT.Desktop.IntegrationTests/
```

## 依赖

- xUnit, FluentAssertions, NSubstitute
- LYBT.Desktop.Auth, LYBT.Desktop.MedicalCase, LYBT.Desktop.Patients
- LYBT.Desktop.Herbs, LYBT.Desktop.Formula, LYBT.Desktop.Users
- LYBT.Desktop.Sync, Shell
- LYBT.Desktop.LocalData, LYBT.Desktop.Foundation
- LYBT.Desktop.Infrastructure, LYBT.Desktop.Models, LYBT.Desktop.Contracts
- 目标框架: net8.0-windows

## 更新记录

- 2026-03-01: 创建 README

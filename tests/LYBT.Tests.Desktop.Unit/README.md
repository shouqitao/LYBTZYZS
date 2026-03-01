# LYBT.Tests.Desktop.Unit

> Desktop 端单元测试 | ViewModel、Service、Repository、状态管理、本地数据源

## 覆盖范围

| 被测项目 | 测试类数 | 说明 |
|----------|----------|------|
| Desktop.Foundation | 7 | 认证状态机、Token 管理、凭证保险库、登出服务 |
| Desktop.Infrastructure | 12 | 分页/搜索/选择服务、加载状态、控件、事件 |
| Desktop.Shell | 5 | ShellViewModel、启动管线、Session 生命周期、登录协调 |
| Desktop.LocalData | 4 | 本地认证、SQLite DataSource (药材/患者/验方) |
| Desktop.MedicalCase | 4 | 医案表单、处方药材价格、编辑模式状态机 |
| Desktop.Patients | 4 | 患者列表 ViewModel、DetailDisplayModel、Repository、Service |
| Desktop.Users | 3 | 用户列表 ViewModel、Repository、Service |
| Desktop.Auth | 1 | LoginViewModel |
| Desktop.Herbs | 1 | HerbItemViewModelBase |
| Desktop.Formula | 2 | FormulaHerbItemViewModel、编辑回归测试 |
| Desktop.Admin / Clinical | 2 | AdminHome / ClinicalHome ViewModel |

## 测试策略

- 框架: xUnit + NSubstitute + FluentAssertions + Xunit.StaFact
- 模式: AAA (Arrange-Act-Assert)
- 目标框架: net8.0-windows (WPF 依赖)
- 本地数据测试使用 SQLite InMemory
- WPF STA 线程测试通过 Xunit.StaFact 支持

## 运行方式

```
dotnet test tests/LYBT.Tests.Desktop.Unit/
```

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 初始创建 README |

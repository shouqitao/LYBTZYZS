# ADR-0003: 集成优先测试策略

**状态**: 已采纳
**日期**: 2026-02-08
**来源**: test-restructure-plan

## 背景

传统单元测试在三层架构中大量 mock，测试价值低。项目测试覆盖率需要提升，但资源有限，需要最大化测试投入回报。

## 决策

采用集成优先的测试金字塔:

| 层级 | 项目 | 覆盖范围 | 优先级 |
|------|------|----------|--------|
| Server 集成 | LYBT.Tests.Server.Integration | Controller -> Service -> Repository -> InMemoryDb | 最高 |
| Desktop 集成 | LYBT.Tests.Desktop.Integration | ViewModel -> Repository -> MockApi | 高 |
| 单元测试 | LYBT.Tests.Unit | 实体验证、纯逻辑 | 中 |
| Desktop 单元 | LYBT.Tests.Desktop.Unit | ViewModel 逻辑 | 中 |
| 架构测试 | LYBT.Tests.Architecture | 依赖方向、命名规范 | 低频 |

### 关键原则
- 集成测试使用 EF Core InMemory 或 SQLite，不 mock Repository
- 测试命名: `方法名_场景_期望结果`
- AAA 模式: Arrange-Act-Assert
- WPF Desktop 测试需要 net8.0-windows 目标框架，不与 Server 混合

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-02-08 | 初始决策，5 个测试项目结构 |

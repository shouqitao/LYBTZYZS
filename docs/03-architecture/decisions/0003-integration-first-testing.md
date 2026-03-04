# ADR-0003: 集成优先测试策略

**状态**: 已采纳
**日期**: 2026-02-08
**来源**: test-restructure-plan

## 背景

传统单元测试在三层架构中大量 mock，测试价值低。项目测试覆盖率需要提升，但资源有限，需要最大化测试投入回报。

## 决策

采用 Testing Trophy 架构 (2026-03-04 升级):

| 层级 | 项目 | 覆盖范围 | Tests |
|------|------|----------|-------|
| Server 全量 | LYBT.Tests.Server | 真实 HTTP + SQL Server + Respawn (零 mock) | 1185 |
| Desktop 全量 | LYBT.Tests.Desktop | SQLite InMemory + 真实 Repository (最小 WPF mock) | 715 |
| 架构防护 | LYBT.Tests.Architecture | 层依赖 + AntiMockRules | 76 |

### 关键原则
- Server 测试零 mock: 真实 SQL Server + Respawn 每测试重置 + 真实 JWT 登录
- Desktop 测试最小 mock: 仅限 WPF Runtime 边界接口 (IRegionManager 等)
- AntiMockRuleTests 架构测试强制 Server 项目不引用 NSubstitute
- 测试命名: `方法名_场景_期望结果`
- AAA 模式: Arrange-Act-Assert
- WPF Desktop 测试需要 net8.0-windows 目标框架，不与 Server 混合

### 演进历史
- 2026-02-08: 初始 "集成优先" 策略 (5 项目, EF InMemory + Mock)
- 2026-03-04: 升级为 Testing Trophy (3 项目, 真实 DB + Respawn, 零 Mock)

## 变更记录

| 日期 | 变更 |
|------|------|
| 2026-02-08 | 初始决策，5 个测试项目结构 |
| 2026-03-04 | 升级为 Testing Trophy: 5->3 项目, Server 零 mock, Respawn 隔离 |

---
type: concept
title: 测试策略 (Testing Strategy)
tags: [concept, testing, quality]
created: 2026-06-10
updated: 2026-06-10
source: docs/05-development/testing.md
---

## 概述

本项目采用 **Testing Trophy** 架构，以真实组件的端到端集成测试为主、纯逻辑单元测试为辅。Server 端实现零 Mock 测试（直接访问 SQL Server + Respawn 隔离），Desktop 端使用 SQLite InMemory + 真实 Repository，确保测试结果贴近生产行为。

> **注意**: Desktop 测试使用 SQLite InMemory；生产本地模式使用 SQL Server LocalDB。两者数据库引擎不同，但共享同一套 Service/Repository 层。

---

## 核心内容

### Testing Trophy 三层结构

| 层级 | 项目 | 目标框架 | 测试数量 | 职责 |
|------|------|----------|----------|------|
| **集成测试** | LYBT.Tests.Server | net8.0 | ~1185 | 真实 HTTP + SQL Server 端到端流程 |
| **集成测试** | LYBT.Tests.Desktop | net8.0-windows | ~715 | 真实 Repository + SQLite InMemory 数据流 |
| **架构防护** | LYBT.Tests.Architecture | net8.0 | ~76 | 层依赖约束、命名规范、AntiMockRules |

### 关键原则

**Server 端：零 Mock**
- 所有测试通过真实 HTTP 管线 + SQL Server + Respawn 执行
- AntiMockRuleTests 架构测试强制禁止引用 NSubstitute
- Controller -> Service -> Repository -> DbContext 全链路验证

**Desktop 端：最小 Mock**
- 仅 Mock WPF Runtime 边界接口（IRegionManager、IDialogService 等）
- Repository/DataSource 必须使用真实组件
- SQLite InMemory 每测试独立连接，自动隔离

### 测试编写规范

**AAA 模式**
```csharp
[Fact]
public void Patient_Create_WithValidData_ShouldSetDefaults()
{
    // Arrange
    var patient = new Patient();

    // Act
    patient.Name = "张三";
    patient.Gender = Gender.Male;

    // Assert
    Assert.NotEqual(Guid.Empty, patient.Id);
    Assert.Equal("张三", patient.Name);
    Assert.False(patient.IsDeleted);
}
```

**命名约定**
```
{ClassName}_{Method}_{Scenario}_{Expected}

示例：
  GetByIdAsync_WithExistingId_ShouldReturnEntity
  Create_WithDuplicateName_ShouldReturnFail
  FullSyncFlow_Upload_ThenDownload_ShouldReturnSameData
```

### 职责划分决策树

```
需要真实 HTTP 请求?  → 是 → 集成测试
需要多组件协作?      → 是 → 集成测试
可以完全隔离 Mock?    → 是 → 单元测试
其他                  → 集成测试
```

### 覆盖率目标

| 层级 | 单元测试覆盖率 | 集成测试场景 |
|------|---------------|--------------|
| Service | 80%+ | 核心端到端流程 |
| Repository | 70%+ | DI 解析、数据持久化 |
| Helper | 90%+ | - |
| Controller | 20%+ | 全部端点测试 |
| DataSource | 70%+ | CRUD 端到端 |

### 常见陷阱与解决方案

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| Desktop 测试在 CI/Linux 失败 | net8.0-windows 仅支持 Windows | 使用 Windows Agent 或 --filter 排除 |
| 集成测试数据污染 | 未重置数据库状态 | Server 用 Respawn，Desktop 用独立连接 |
| 架构测试报"禁止引用"错误 | AntiMockRuleTests 检查失败 | 用真实集成测试替代 Mock 测试 |

---

## 相关链接

- [[ADR-003-integration-first-testing]]
- [[overview]]
- [[medical-case-module]]

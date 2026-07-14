---
feature: test-cleanup
status: delivered
plans:
  - docs/compose/plans/2026-07-14-test-cleanup-merge.md
branch: master
commits: bfaded8a9..ea6bb5e52
---

# 测试清理 — 最终报告

## 已完成内容

删除了冗余的 C# 集成测试和用户旅程测试，这些测试已被 Newman API 测试（86 个端点）完全覆盖。保留了单元测试、架构测试和速率限制测试。

### 删除的测试

| 目录 | 文件数 | 删除行数 |
|------|--------|----------|
| `tests/LYBT.Tests.Server/Integration/` | 32 | 6,524 |
| `tests/LYBT.Tests.Server/UserJourneys/` | 13 | 4,207 |
| **总计** | **45** | **10,731** |

### 保留的测试

| 目录 | 用途 |
|------|------|
| `tests/LYBT.Tests.Server/Unit/` | 单元测试（validators, mappers, entities, infrastructure） |
| `tests/LYBT.Tests.Server/RateLimiting/` | 速率限制测试 |
| `tests/LYBT.Tests.Server/_Infrastructure/` | 测试基础设施 |
| `tests/LYBT.Tests.Architecture/` | 架构测试 |
| `tests/LYBT.Tests.Desktop/` | Desktop 单元测试 |

## 验证

- 构建：0 警告，0 错误
- 所有保留的测试项目正常

## Journey Log

- [lesson] Newman API 测试（86 个端点）已完全覆盖集成测试场景，C# 集成测试成为冗余
- [lesson] 用户旅程测试（UserJourneys）与 Newman 端到端测试重复，删除后测试套件更精简

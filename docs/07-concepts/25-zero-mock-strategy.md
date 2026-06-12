---
type: concept
title: 零 Mock 策略 (Zero Mock)
tags: [testing, best-practice, server]
related: [testing-strategy, lybt-tests-server, respawn, anti-mock-rule-tests]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/03-architecture/decisions/0003-integration-first-testing.md"]
---
# 零 Mock 策略 (Zero Mock)

**零 Mock 策略**是指在服务端集成测试中，完全不使用 Mock 对象，而是直接调用真实的 HTTP 接口、连接真实的数据库并执行真实的业务逻辑。

## 核心理念

传统单元测试中大量使用 Mock 往往导致测试只验证了 Mock 的配置是否正确，而非业务逻辑本身。零 Mock 策略认为：
1.  **真实性优于速度**：测试应尽可能接近生产环境，以发现集成错误（如 SQL 映射、序列化、事务处理）。
2.  **集成测试即单元测试**：在良好的架构下，端到端的集成测试足以覆盖大部分业务逻辑，且维护成本更低。

## 实施要点

*   **真实数据库**：使用真实 SQL Server 实例。
*   **快速重置**：利用 Respawn 等工具在测试间快速重置数据库状态，解决数据隔离问题。
*   **真实认证**：通过真实登录接口获取 Token，验证安全链路。
*   **物理隔离**：通过 lybt-tests-architecture 中的 anti-mock-rule-tests 禁止引用 Mock 库，防止策略退化。

## 优势

*   高置信度：测试失败通常代表真实 Bug。
*   低维护：无需随代码变更调整 Mock 配置。
*   发现集成问题：能捕获纯单元测试无法发现的层间交互错误。

## 挑战

*   执行速度较慢：需通过并行测试和优化重置逻辑缓解。
*   环境依赖：CI/CD 需配置真实数据库实例。

## 参见

*   [测试策略](24-testing-strategy.md)
*   [Server 集成测试](../05-development/05-testing.md) — LYBT.Tests.Server (真实 SQL Server + Respawn)
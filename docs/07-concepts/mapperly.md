---
type: concept
title: Mapperly 映射器框架
created: 2026-06-10
updated: 2026-06-10
tags: [工具, 映射器, 源生成器, 性能]
related: [ef-core-data-model]
sources: ["docs/01-product/glossary.md"]
---

# Mapperly 映射器框架

Mapperly 是凌隐宝堂中医诊所管理系统中采用的编译时源生成器，用于替代 AutoMapper，负责对象之间的映射（例如 Entity 与 DTO 之间的转换）。其核心优势在于通过编译时代码生成提供高性能和类型安全的映射。

## 核心特性

*   **编译时源生成**：在编译阶段生成映射代码，避免了运行时反射带来的性能开销。
*   **类型安全**：映射关系在编译时检查，减少了运行时错误。
*   **替代 AutoMapper**：作为更现代、高性能的替代方案被引入系统。

## 在系统中的应用

Mapperly 主要用于 Server 端，在 [[ef-core-data-model]] 定义的实体（Entity）和 API 层使用的数据传输对象（DTO）之间进行映射。这确保了数据在不同层之间传递时的高效和准确转换。

## 相关页面

*   [[ef-core-data-model]] - 包含 BaseEntity 和 Repository 模式，Mapperly 在此数据访问层之上工作。
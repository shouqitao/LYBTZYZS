# 共享架构文档

## 📋 概述

跨Server端和Client端共享的架构决策、设计模式和技术标准。

## 🏗️ 核心内容

### 🎯 架构决策记录 (ADR)
- **ADR目录**: `adr/` - 所有架构决策的详细记录
- **决策目录**: `decisions/` - 重要的技术决策和选型说明

### 🧪 测试标准
- **测试指南**: `testing/` - 跨端测试架构和标准

## 🔗 主要文档

### 架构决策 (ADR)
- [`ADR-001`](./adr/ADR-001-cqrs-mediatr-rejection.md) - CQRS和MediatR模式拒绝
- [`ADR-002`](./adr/ADR-002-technology-roadmap-suggestion.md) - 技术路线建议
- [`ADR-005`](./adr/ADR-005-desktop-modular-architecture.md) - 桌面端模块化架构

### 技术决策
- [`过度工程化拒绝`](./decisions/ADR-001-reject-overengineering.md)
- [`桌面端服务移除`](./decisions/ADR-002-desktop-services-removal.md)
- [`服务接口统一设计标准`](./decisions/ADR-004-service-interface-unified-design-standard.md)

## 📚 使用指南

1. **新项目启动**: 先阅读ADR了解技术选型背景
2. **架构决策**: 参考decisions目录中的决策记录
3. **测试实施**: 遵循testing目录的统一标准
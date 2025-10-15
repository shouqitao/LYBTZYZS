# Server端架构文档

## 📋 概述

Server端采用三层架构模式，遵循统一的模块化设计标准，确保代码的可维护性和扩展性。

## 🏗️ 核心架构

- **设计标准**: `design-standard.md` - Server端三层架构设计规范
- **模块模板**: `module-template/` - 标准模块开发模板和脚手架
- **测试指南**: `testing/` - Server端测试架构和规范

## 📁 目录结构

```
src/Server/
├── Core/              # 核心基础设施
├── Modules/           # 业务模块
├── Services/          # 服务层
└── GlobalUsings.cs    # 全局引用
```

## 🔗 相关文档

- **共享架构**: `../shared/` - 跨端共享的架构决策和标准
- **Client端架构**: `../client/` - Client端架构文档
- **开发指南**: `../../development/server/` - Server端开发指南

## 📚 快速开始

1. 阅读 [`design-standard.md`](./design-standard.md) 了解架构规范
2. 使用 [`module-template/`](./module-template/) 创建新模块
3. 参考 [`testing/`](./testing/) 编写测试代码
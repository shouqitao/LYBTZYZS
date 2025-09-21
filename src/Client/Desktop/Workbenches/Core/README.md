# Workbench Core

This directory contains the core infrastructure for workbench routing and navigation.

## Core Components

- **IWorkbenchRouter** - Interface for role-to-workbench mapping
- **WorkbenchRouter** - Implementation of workbench routing logic
- **NavigationItem** - Model for navigation menu items
- **IWorkbenchNavigator** - Interface for workbench-specific navigation

## Workbench Mapping

- Administrator → SystemWorkbench
- Doctor → ConsultationWorkbench
- Reception (Future) → ReceptionWorkbench

## 🎯 项目概述
- [待补充] 简要描述 Core 的职责、边界及与其他模块关系。

## 📦 项目结构
- [待补充] 列出子目录/关键文件与职责（如 Controllers/Services/Repositories 等）。

## 🛠 技术栈
- [待补充] 框架/库/运行时示例：.NET 8、ASP.NET Core、EF Core、Prism、Refit、AutoMapper 等。

## 🚀 快速开始
- [待补充] 基本操作：dotnet restore/build/test；如何运行/调试当前模块。

## 🔌 API 接口
- [待补充] 集成的 API/Refit 客户端：例如 ICoreApi
- [待补充] 关键调用路径与鉴权方式（JWT Bearer）

## 📚 相关文档
- docs/architecture/overview.md
- docs/api/README.md
- docs/modules/index.md
- [待补充] 本模块相关的设计/实现文档链接

# LYBT.Module.Herbs - 中药材管理模块

## 🎯 项目概述

**药材管理模块 (Herbs Module)** 是系统的药材管理核心模块，采用分层架构设计。它提供完整的中药材信息管理、价格维护、拼音检索和批量导入导出功能。作为处方系统的基础数据支撑，本模块采用 **Record-Only** 模式，即只管理药材档案信息，不涉及库存，以简化流程，特别适合小型诊所的需求。

## 📦 项目结构

```
LYBT.Module.Herbs/
├── HerbsModule.cs             # 模块依赖注入注册
├── Interfaces/                # 模块内部接口定义
│   ├── IHerbRepository.cs
│   └── IHerbQueryService.cs
├── Services/                  # 业务逻辑实现
│   ├── HerbService.cs           # 主服务 (实现IHerbService)
│   └── HerbQueryService.cs
├── Repositories/              # 数据仓储实现
│   └── HerbRepository.cs
└── Mapping/                   # AutoMapper映射配置
    └── HerbMappingProfile.cs
```

## 🛠 技术栈

- **.NET 8**: 基础框架。
- **Entity Framework Core**: 通过仓储模式间接使用，用于数据持久化。
- **AutoMapper**: 用于在实体（Entity）和数据传输对象（DTO）之间进行映射。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.Herbs\LYBT.Module.Herbs.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `HerbsController` 对外暴露。

- **API路由前缀**: `/api/v1/herbs`

所有API的详细定义请参考 `IHerbApi` 接口和 `HerbsController` 的实现。

---

*（详细的内部架构、业务流程、数据结构等信息请参考本文档后续章节。）*
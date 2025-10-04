# LYBT.Module.Formula - 验方管理模块

## 🎯 项目概述

**验方管理模块 (Formula Module)** 是系统的验方（经典方剂和经验方）管理核心模块，采用分层架构设计。它提供完整的验方管理、药材组成配置、方剂分享和从处方创建验方等功能，是处方系统的模板支撑，旨在提高医生开方效率，积累诊疗经验。

## 📦 项目结构

```
LYBT.Module.Formula/
├── FormulaModule.cs           # 模块依赖注入注册
├── Interfaces/                # 模块内部接口定义
│   ├── IFormulaRepository.cs
│   └── IFormulaQueryService.cs
├── Services/                  # 业务逻辑实现
│   ├── FormulaService.cs        # 主服务 (实现IFormulaService)
│   └── FormulaQueryService.cs
├── Repositories/              # 数据仓储实现
│   └── FormulaRepository.cs
└── Mapping/                   # AutoMapper映射配置
    └── FormulaMappingProfile.cs
```

## 🛠 技术栈

- **.NET 8**: 基础框架。
- **Entity Framework Core**: 通过仓储模式间接使用，用于数据持久化。
- **AutoMapper**: 用于在实体（Entity）和数据传输对象（DTO）之间进行映射。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.Formula\LYBT.Module.Formula.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `FormulasController` 对外暴露。

- **API路由前缀**: `/api/v1/formulas`

所有API的详细定义请参考 `IFormulaApi` 接口和 `FormulasController` 的实现。

---

*（详细的内部架构、业务流程、数据结构等信息请参考本文档后续章节。）*
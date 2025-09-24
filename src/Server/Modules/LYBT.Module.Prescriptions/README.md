# LYBT.Module.Prescriptions - 处方管理模块

## 🎯 项目概述

**处方管理模块 (Prescriptions Module)** 是系统的核心业务模块，采用分层架构设计。它提供完整的中医处方开具、管理、验证和统计功能，并与医案、诊断和药材模块紧密集成，支撑完整的诊疗流程。其业务特色包括处方复制、剂量自动计算和价格预览。

## 📦 项目结构

```
LYBT.Module.Prescriptions/
├── PrescriptionsModule.cs     # 模块依赖注入注册
├── Interfaces/                # 模块内部接口定义
│   ├── IPrescriptionRepository.cs
│   └── IPrescriptionQueryService.cs
├── Services/                  # 业务逻辑实现
│   ├── PrescriptionService.cs   # 主服务 (实现IPrescriptionService)
│   └── PrescriptionQueryService.cs
├── Repositories/              # 数据仓储实现
│   └── PrescriptionRepository.cs
└── Mapping/                   # AutoMapper映射配置
    └── PrescriptionMappingProfile.cs
```

## 🛠 技术栈

- **.NET 8**: 基础框架。
- **Entity Framework Core**: 通过仓储模式间接使用，用于数据持久化。
- **AutoMapper**: 用于在实体（Entity）和数据传输对象（DTO）之间进行映射。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.Prescriptions\LYBT.Module.Prescriptions.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `PrescriptionsController` 对外暴露。

- **API路由前缀**: `/api/v1/prescriptions`

所有API的详细定义请参考 `IPrescriptionApi` 接口和 `PrescriptionsController` 的实现。

---

*（详细的内部架构、业务流程、数据结构等信息请参考本文档后续章节。）*
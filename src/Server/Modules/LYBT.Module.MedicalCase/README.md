# LYBT.Module.MedicalCase - 医疗案例管理模块

## 🎯 项目概述

**医疗案例管理模块 (MedicalCase Module)** 是系统的核心业务模块，采用分层架构设计。它作为整个诊疗流程的**管理容器**和聚合根，每一个`MedicalCase`代表一次完整的看诊会话，1:1关联`Consultation`诊断记录，统一管理患者从接诊到完成的全程诊疗状态。

## 📦 项目结构

```
LYBT.Module.MedicalCase/
├── MedicalCaseModule.cs       # 模块依赖注入注册
├── Interfaces/                # 模块内部接口定义
│   ├── IMedicalCaseRepository.cs
│   └── IMedicalCaseQueryService.cs
├── Services/                  # 业务逻辑实现
│   ├── MedicalCaseService.cs    # 主服务 (实现IMedicalCaseService)
│   └── MedicalCaseQueryService.cs
├── Repositories/              # 数据仓储实现
│   └── MedicalCaseRepository.cs
└── Mapping/                   # AutoMapper映射配置
    └── MedicalCaseMappingProfile.cs
```

## 🛠 技术栈

- **.NET 8**: 基础框架。
- **Entity Framework Core**: 通过仓储模式间接使用，用于数据持久化。
- **AutoMapper**: 用于在实体（Entity）和数据传输对象（DTO）之间进行映射。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.MedicalCase\LYBT.Module.MedicalCase.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `MedicalCasesController` 对外暴露。

- **API路由前缀**: `/api/v1/medicalcases`

所有API的详细定义请参考 `IMedicalCaseApi` 接口和 `MedicalCasesController` 的实现。

---

*（详细的内部架构、业务流程、数据结构等信息请参考本文档后续章节。）*
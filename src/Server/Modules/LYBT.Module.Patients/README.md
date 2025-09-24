# LYBT.Module.Patients - 患者档案管理模块

## 🎯 项目概述

**患者档案管理模块 (Patients Module)** 是系统的核心业务模块，采用分层架构设计。它提供完整的患者档案管理、就诊历史记录、健康信息维护等功能。专为中医诊所场景优化，支持中医特色的体质辨识、过敏史记录等功能。

## 📦 项目结构

```
LYBT.Module.Patients/
├── PatientModule.cs           # 模块依赖注入注册
├── Interfaces/                # 模块内部接口定义
│   ├── IPatientRepository.cs
│   └── IPatientQueryService.cs
├── Services/                  # 业务逻辑实现
│   ├── PatientService.cs        # 主服务 (实现IPatientService)
│   └── PatientQueryService.cs
├── Repositories/              # 数据仓储实现
│   └── PatientRepository.cs
└── Mapping/                   # AutoMapper映射配置
    └── PatientMappingProfile.cs
```

## 🛠 技术栈

- **.NET 8**: 基础框架。
- **Entity Framework Core**: 通过仓储模式间接使用，用于数据持久化。
- **AutoMapper**: 用于在实体（Entity）和数据传输对象（DTO）之间进行映射。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.Patients\LYBT.Module.Patients.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `PatientsController` 对外暴露。

- **API路由前缀**: `/api/v1/patients`

所有API的详细定义请参考 `IPatientApi` 接口和 `PatientsController` 的实现。

---

*（详细的内部架构、业务流程、数据结构等信息请参考本文档后续章节。）*
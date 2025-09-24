# LYBT.Module.Consultation - 看诊模块

## 🎯 项目概述

**看诊模块 (Consultation Module)** 是凌隐宝堂中医诊所系统的核心业务模块，专门处理中医看诊过程中的四诊合参（望、闻、问、切）诊断记录管理。基于分层架构设计，提供完整的看诊生命周期管理功能。

### 核心功能

- **四诊合参记录**：系统化记录中医四诊信息（望诊、闻诊、问诊、切诊）。
- **看诊生命周期管理**：从开始看诊到诊断完成的完整工作流。
- **患者就诊历史**：多维度查询患者历史就诊记录。

## 📦 项目结构

```
LYBT.Module.Consultation/
├── ConsultationModule.cs      # 模块依赖注入注册
├── Interfaces/                # 模块内部接口定义
│   ├── IConsultationRepository.cs
│   └── IConsultationQueryService.cs
├── Services/                  # 业务逻辑实现
│   ├── ConsultationService.cs     # 主服务 (实现IConsultationService)
│   └── ConsultationQueryService.cs
├── Repositories/              # 数据仓储实现
│   └── ConsultationRepository.cs
└── Mapping/                   # AutoMapper映射配置
    └── ConsultationMappingProfile.cs
```

## 🛠 技术栈

- **.NET 8**: 基础框架。
- **Entity Framework Core**: 通过仓储模式间接使用，用于数据持久化。
- **AutoMapper**: 用于在实体（Entity）和数据传输对象（DTO）之间进行映射。

## 🚀 快速开始

此项目是一个类库，作为后端服务的一部分被 `LYBT.WebAPI` 项目引用和托管。无法独立运行。

```bash
# 构建此项目
dotnet build src\Server\Modules\LYBT.Module.Consultation\LYBT.Module.Consultation.csproj
```

## 🔌 API 接口

此模块的业务逻辑通过 `LYBT.WebAPI` 项目中的 `ConsultationsController` 对外暴露。

- **API路由前缀**: `/api/v1/consultations`

所有API的详细定义请参考 `IConsultationApi` 接口和 `ConsultationsController` 的实现。

---

*（详细的内部架构、业务流程、数据结构等信息请参考本文档后续章节。）*
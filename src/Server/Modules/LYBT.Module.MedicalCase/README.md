# LYBT.Module.MedicalCase

> 医案管理(核心聚合根) | CQRS模式 | 状态机驱动

## 项目定位

- **层级**: Server端
- **架构模式**: CQRS(读写分离，状态服务)
- **跨模块通信**: IPatientCrossModuleService(查询PatientBasicDto)

## 目录结构

```
LYBT.Module.MedicalCase/
├── MedicalCaseModule.cs
├── Interfaces/
│   ├── IMedicalCaseService.cs
│   └── IMedicalCaseRepository.cs
├── Services/
│   ├── MedicalCaseService.cs
│   └── MedicalCaseRules.cs
├── Repositories/
│   └── MedicalCaseRepository.cs
└── Mapping/
    └── MedicalCaseMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IMedicalCaseService | 19 | CRUD/诊断/处方/状态迁移 |
| IMedicalCaseRepository | 11+ | 继承BaseRepository，详情/分页/动态查询 |

## 状态机

```
Pending(待接诊) → InProgress(诊疗中) → Completed(已完成)
       ↓                   ↓
    Closed             Closed
```

## 业务规则(MedicalCaseRules)

| 规则 | 说明 |
|------|------|
| CanCreateNewCase | 患者只能有一个未完成医案 |
| CanEdit | 非终态可编辑 |
| CanDelete | 仅草稿状态可删除 |
| CanComplete | 必须有诊断记录 |

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext, IPatientCrossModuleService)
- LYBT.Entities (MedicalCase, Consultation, Prescription)
- LYBT.Shared.Models (MedicalCaseDto, ConsultationDto等)

### 被依赖
- LYBT.WebAPI (MedicalCaseController)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/medicalcases | GET | 分页查询医案列表 |
| /api/medicalcases/{id} | GET | 医案详情 |
| /api/medicalcases | POST | 创建医案 |
| /api/medicalcases/{id} | PUT | 更新医案 |
| /api/medicalcases/{id}/consultation | PUT | 更新诊断记录 |
| /api/medicalcases/{id}/prescription-flag | PUT | 设置处方标志 |
| /api/medicalcases/{id}/prescription | POST | 创建处方 |
| /api/medicalcases/{id}/prescription | PUT | 更新处方 |
| /api/medicalcases/{id}/prescription | DELETE | 删除处方 |
| /api/medicalcases/{id}/status | PUT | 更新状态 |
| /api/medicalcases/{id}/complete | POST | 完成医案 |
| /api/medicalcases/{id}/close | POST | 关闭医案 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-11-20 | Repository Include策略优化 |
| 2025-10-29 | 初始版本 |

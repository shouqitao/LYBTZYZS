# LYBT.Module.Patients

> 患者信息管理 | 传统三层 | 独立模块

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 无(被MedicalCase模块通过IPatientCrossModuleService查询)

## 目录结构

```
LYBT.Module.Patients/
├── PatientsModule.cs
├── Interfaces/
│   ├── IPatientService.cs
│   ├── IPatientServiceOptimized.cs
│   └── IPatientRepository.cs
├── Services/
│   └── PatientService.cs
├── Repositories/
│   └── PatientRepository.cs
└── Mapping/
    └── PatientMappingProfile.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IPatientService | 9 | CRUD、搜索、批量导入/导出 |
| IPatientServiceOptimized | - | Entity直接返回策略(性能优化) |
| IPatientRepository | 4+ | 继承BaseRepository，按名字/电话/日期范围查询 |

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext)
- LYBT.Entities (Patient实体)
- LYBT.Shared.Models (PatientDto, CreatePatientRequest等)

### 被依赖
- LYBT.WebAPI (PatientController)
- LYBT.Module.MedicalCase (通过IPatientCrossModuleService查询PatientBasicDto)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/patients | GET | 分页查询患者列表 |
| /api/patients/{id} | GET | 按ID获取患者详情 |
| /api/patients | POST | 创建新患者 |
| /api/patients/{id} | PUT | 更新患者信息 |
| /api/patients/{id} | DELETE | 软删除患者 |
| /api/patients/search | GET | 按关键字搜索 |
| /api/patients/import | POST | 批量导入患者(Excel) |
| /api/patients/export | GET | 导出患者数据(Excel) |
| /api/patients/template | GET | 下载导入模板 |
| /api/patients/statistics | GET | 获取患者统计信息 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-11-19 | 修复DI注册缺失导致的500错误(IPatientServiceOptimized) |
| 2025-10-29 | 初始版本 |

# LYBT.Module.Registration

> 挂号排队管理 | 传统三层 | 支持接待员挂号和医生直接挂号

## 项目定位

- **层级**: Server端
- **架构模式**: 传统三层
- **跨模块通信**: 通过IPatientCrossModuleService访问患者数据

## 目录结构

```
LYBT.Module.Registration/
├── RegistrationModule.cs
├── Interfaces/
│   ├── IRegistrationService.cs
│   └── IRegistrationRepository.cs
├── Services/
│   └── RegistrationService.cs
├── Repositories/
│   └── RegistrationRepository.cs
└── Mapping/
    └── RegistrationMapper.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IRegistrationService | 7 | CRUD/排队/就诊/取消 |
| IRegistrationRepository | 2 | 获取排队列表/按ID查询含患者信息 |

## 挂号字段

| 字段 | 说明 |
|------|------|
| PatientId | 患者ID |
| DoctorId | 医生ID |
| VisitDate | 就诊日期 |
| Status | 状态(Waiting/InProgress/Completed/Cancelled) |
| Source | 挂号来源(Receptionist/Doctor/Online) |
| Remark | 备注 |

## 设计特点

| 特点 | 说明 |
|------|------|
| 多来源挂号 | 支持接待员挂号、医生直接挂号、在线挂号(预留) |
| 排队管理 | 按挂号时间排序的等待队列 |
| 状态流转 | Waiting → InProgress → Completed 或 Waiting → Cancelled |
| 跨模块查询 | 通过IPatientCrossModuleService获取患者信息 |

## 设计依据

- 挂号模块是诊所工作流的入口，患者挂号后才能进入就诊流程
- 使用IPatientCrossModuleService避免直接依赖Patients模块，保持模块隔离
- 支持多种挂号来源，适应不同场景（接待员前台挂号、医生直接接诊）
- 排队功能帮助医生了解当前等待患者，优化就诊流程

## 依赖关系

### 依赖
- LYBT.Infrastructure (BaseRepository, AppDbContext, BaseService)
- LYBT.Entities (Registration实体)
- LYBT.Shared.Models (RegistrationDto等)
- IPatientCrossModuleService (跨模块患者查询)

### 被依赖
- LYBT.WebAPI (RegistrationsController)
- LYBT.Module.MedicalCase (就诊时关联挂号)

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| /api/registrations | POST | 创建挂号 |
| /api/registrations/quick-visit | POST | 快速就诊(自动创建挂号+患者) |
| /api/registrations/{id} | GET | 按ID查询挂号详情 |
| /api/registrations | GET | 分页查询挂号列表 |
| /api/registrations/queue | GET | 获取等待队列 |
| /api/registrations/{id}/start-visit | PUT | 开始就诊(状态→InProgress) |
| /api/registrations/{id}/cancel | PUT | 取消挂号 |

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-05-27 | 初始版本 |

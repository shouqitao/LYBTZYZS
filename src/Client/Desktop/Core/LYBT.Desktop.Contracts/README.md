# LYBT.Desktop.Contracts

> Desktop端API契约层 | Refit接口定义 | 类型安全HTTP客户端

## 项目定位

- **层级**: Client Core层
- **职责**: 定义Desktop端调用WebAPI的所有Refit接口契约，确保Client与Server端API类型同步

## 目录结构

```
LYBT.Desktop.Contracts/
├── Api/                          # Refit API接口(8个模块)
│   ├── IAuthApi.cs               # 认证API(6方法)
│   ├── IUserApi.cs               # 用户管理API(5方法)
│   ├── IPatientApi.cs            # 患者管理API
│   ├── IMedicalCaseApi.cs        # 医案管理API
│   ├── IConsultationApi.cs       # 诊断记录API
│   ├── IPrescriptionApi.cs       # 处方管理API
│   ├── IHerbApi.cs               # 中药材管理API
│   └── IFormulaApi.cs            # 验方管理API
└── Services/                     # 跨模块服务契约
    └── IPrescriptionEditorService.cs
```

## 核心接口

| 接口 | 方法数 | 说明 |
|------|--------|------|
| IAuthApi | 6 | 登录/登出/Token验证/修改密码/健康检查 |
| IUserApi | 5 | 用户CRUD/分页查询 |
| IPatientApi | 8 | 患者CRUD/搜索/档案管理 |
| IMedicalCaseApi | 10 | 医案CRUD/状态管理/查询 |
| IConsultationApi | 6 | 诊断记录/四诊录入 |
| IPrescriptionApi | 8 | 处方CRUD/状态管理 |
| IHerbApi | 8 | 药材CRUD/搜索/批量导入 |
| IFormulaApi | 8 | 验方CRUD/搜索/克隆 |

## 设计特点

| 特点 | 说明 |
|------|------|
| Refit框架 | 通过特性标注自动生成HTTP客户端实现 |
| 类型安全 | 所有API方法使用强类型DTO，编译时检查 |
| 异步优先 | 全异步方法(async/await) |
| 认证集成 | 通过HttpMessageHandler统一添加JWT Token |

## 依赖关系

### 依赖
- LYBT.Shared.Models (共享DTO)
- Refit (7.x)
- Refit.HttpClientFactory (7.x)

### 被依赖
- LYBT.Desktop.Foundation (Refit客户端注册)
- LYBT.Desktop.Models (Repository层调用)
- 所有Desktop业务模块

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-12 | 从Shared.Interfaces迁移至Desktop.Contracts |

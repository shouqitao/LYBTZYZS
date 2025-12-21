# Tasks: refactor-server-ddd-aggregates

## Phase 1: 删除反向导航属性

### 1.1 修改实体定义

- [ ] 删除 `Consultation.MedicalCase` 导航属性
  - 文件: `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`
  - 删除: `public virtual MedicalCases.MedicalCase MedicalCase { get; set; }`
  - 删除: `using LYBT.Entities.MedicalCases;` (如果不再需要)

- [ ] 删除 `Prescription.MedicalCase` 导航属性
  - 文件: `src/Server/Core/LYBT.Entities/Prescriptions/PrescriptionModel.cs`
  - 删除: `public virtual MedicalCases.MedicalCase? MedicalCase { get; set; }`

- [ ] 编译验证
  - 执行: `dotnet build LYBT.All.sln`
  - 预期: 编译错误（引用了已删除的导航属性）

### 1.2 修复编译错误

- [ ] 搜索并修复所有引用 `.MedicalCase` 的代码
  - 执行: `rg "\.MedicalCase" src/Server --type cs -l`
  - 逐个修复，改用ID查询或Query Service

- [ ] 搜索并修复所有 `Include(x => x.MedicalCase)` 调用
  - 执行: `rg "Include.*MedicalCase" src/Server --type cs`
  - 改用单独查询或Query Service

## Phase 2: 修改EF Core配置

### 2.1 更新配置类

- [ ] 修改 MedicalCaseConfiguration
  - 文件: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/MedicalCaseConfiguration.cs`
  - 使用 `.HasOne<T>().WithOne()` 无反向导航配置

- [ ] 修改或创建 ConsultationConfiguration
  - 文件: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/ConsultationConfiguration.cs`
  - 移除反向导航相关配置

- [ ] 修改或创建 PrescriptionConfiguration
  - 文件: `src/Server/Core/LYBT.Infrastructure/Data/Configurations/PrescriptionConfiguration.cs`
  - 移除反向导航相关配置
  - 添加backing field访问配置

### 2.2 验证EF迁移

- [ ] 检查是否需要新迁移
  - 执行: `dotnet ef migrations has-pending-model-changes`
  - 预期: 无需新迁移（只是代码层面变更）

- [ ] 运行集成测试验证
  - 执行: `dotnet test tests/IntegrationTests`

## Phase 3: 创建Query Service

### 3.1 定义Query Models

- [ ] 创建 MedicalCaseDetailQueryModel
  - 文件: `src/Server/Modules/LYBT.Module.MedicalCase/Queries/Models/MedicalCaseDetailQueryModel.cs`

- [ ] 创建 MedicalCaseListQueryModel
  - 文件: `src/Server/Modules/LYBT.Module.MedicalCase/Queries/Models/MedicalCaseListQueryModel.cs`

### 3.2 实现Query Service

- [ ] 创建 IMedicalCaseQueryService 接口
  - 文件: `src/Server/Modules/LYBT.Module.MedicalCase/Queries/IMedicalCaseQueryService.cs`

- [ ] 实现 MedicalCaseQueryService
  - 文件: `src/Server/Modules/LYBT.Module.MedicalCase/Queries/MedicalCaseQueryService.cs`
  - 使用子查询替代Include

### 3.3 注册依赖注入

- [ ] 在模块DI配置中注册Query Service
  - 文件: `src/Server/Modules/LYBT.Module.MedicalCase/DependencyInjection.cs`

### 3.4 迁移现有查询

- [ ] 迁移 MedicalCaseService 中的查询逻辑
  - 识别使用Include的方法
  - 改用Query Service

- [ ] 迁移 Controller 中的直接查询
  - 识别直接使用DbContext的地方
  - 改用Query Service

## Phase 4: 领域事件（可选）

### 4.1 基础设施

- [ ] 创建 IDomainEvent 接口
  - 文件: `src/Server/Core/LYBT.Infrastructure/DomainEvents/IDomainEvent.cs`

- [ ] 创建 IAggregateRoot 接口
  - 文件: `src/Server/Core/LYBT.Infrastructure/DomainEvents/IAggregateRoot.cs`

- [ ] 修改 BaseEntity 实现领域事件
  - 文件: `src/Server/Core/LYBT.Entities/Common/BaseEntity.cs`

### 4.2 事件定义

- [ ] 创建 MedicalCaseCompletedEvent
  - 文件: `src/Server/Core/LYBT.Entities/MedicalCases/Events/MedicalCaseCompletedEvent.cs`

### 4.3 事件发布

- [ ] 修改 LybtDbContext.SaveChangesAsync
  - 在保存后发布领域事件

### 4.4 事件处理

- [ ] 创建 UpdatePatientLastVisitHandler
  - 文件: `src/Server/Modules/LYBT.Module.Patients/EventHandlers/UpdatePatientLastVisitHandler.cs`

## Phase 5: 验证与文档

### 5.1 测试验证

- [ ] 运行所有单元测试
  - 执行: `dotnet test tests/UnitTests`

- [ ] 运行所有集成测试
  - 执行: `dotnet test tests/IntegrationTests`

- [ ] 手动验证关键功能
  - 创建医案
  - 添加诊断
  - 添加处方
  - 完成医案

### 5.2 更新文档

- [ ] 更新 server-layer-architecture spec
- [ ] 创建 entity-conventions spec

### 5.3 代码审查

- [ ] 执行 `/lybtzyzs-code-review`
- [ ] 修复发现的问题

## 完成标准

- [ ] 编译通过 (0错误)
- [ ] 所有测试通过
- [ ] 无反向导航属性
- [ ] Query Service覆盖主要查询场景
- [ ] 文档更新完成

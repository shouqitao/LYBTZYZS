## AGENTS.md — 系统设置模块（LYBT.Module.Settings）

### 1. Agent 概述

系统设置模块负责维护全局运行参数、诊断目录、治疗目录、枚举映射及其他系统配置项，支撑各模块配置化运行及灵活扩展。

### 2. 核心能力

- 管理全局设置（如数据同步模式、病历默认共享策略等）
- 管理诊断目录与治疗目录（支持结构化维护、增删改查）
- 系统枚举映射（供前端枚举下拉、展示等使用）
- 读取/保存各类通用配置

### 3. 输入输出规范

#### 输入

- `GlobalSettingsDto`：全局设置项
- `DiagnosisCatalogDto` / `TreatmentCatalogDto`：目录维护

#### 输出

- `GlobalSettingsDto`：全局配置详情
- `(IList<DiagnosisCatalogDto>, int)`：诊断目录分页
- `(IList<TreatmentCatalogDto>, int)`：治疗目录分页
- `(IList<EnumMappingDto>, int)`：枚举映射

### 4. 协作与依赖模块

- **全部业务模块**：依赖全局配置和目录，如共享策略、默认挂号方式等
- **基础设施模块**：数据持久化
- **日志模块**：重要配置变更写入日志

### 5. 示例场景

#### 查询全局设置

```csharp
var global = await _globalSettingsService.GetAsync();
```

#### 新增诊断目录

```csharp
var dto = new DiagnosisCatalogDto { Name = "呼吸系统" };
bool ok = await _diagnosisCatalogService.AddAsync(dto);
```

### 6. 接口列表

- `Task<GlobalSettingsDto?> GetAsync()`
- `Task<bool> SaveAsync(GlobalSettingsDto dto)`
- `Task<List<DiagnosisCatalogDto>> GetDiagnosisCatalogsAsync()`
- `Task<bool> AddDiagnosisCatalogAsync(DiagnosisCatalogCreateDto dto)`
- `Task<bool> UpdateDiagnosisCatalogAsync(DiagnosisCatalogEditDto dto)`
- `Task<bool> DeleteDiagnosisCatalogAsync(Guid id)`
- `Task<List<TreatmentCatalogDto>> GetTreatmentCatalogsAsync()`
- `Task<bool> AddTreatmentCatalogAsync(TreatmentCatalogCreateDto dto)`
- `Task<bool> UpdateTreatmentCatalogAsync(TreatmentCatalogEditDto dto)`
- `Task<bool> DeleteTreatmentCatalogAsync(Guid id)`
- `Task<Dictionary<string, Dictionary<int,string>>> GetEnumMappingsAsync()`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/settings` | 已实现 |
| `GET /api/settings` | 已实现 |
| `POST /api/diagnosiscatalog` | 已实现 |
| `GET /api/diagnosiscatalog` | 已实现 |



## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```

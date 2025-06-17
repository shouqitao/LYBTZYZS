# LYBT.Module.Settings

系统设置模块，维护应用级配置项，同时提供诊断目录、治疗项目及全局设置等管理能力。

## 主要服务及接口
- `ISettingsService` / `SettingsService`
- `ISettingsRepository` / `SettingsRepository`
- `IDiagnosisCatalogService` / `DiagnosisCatalogService`
- `ITreatmentCatalogService` / `TreatmentCatalogService`
- `IGlobalSettingsService` / `GlobalSettingsService`
- `IEnumMappingsService` / `EnumMappingsService`

## 重要模型和DTO
- `SettingsModel`
- `DiagnosisCatalogModel`、`TreatmentCatalogModel`、`GlobalSettingsModel`
- `SettingsDto`、`SettingsCreateDto`、`SettingsEditDto`、`SettingsDetailDto`
- `DiagnosisCatalogDto`、`TreatmentCatalogDto`、`GlobalSettingsDto`

## 用法
执行 `SettingsModule.Register(services)` 后，可通过 `ISettingsService` 读取和保存系统设置。

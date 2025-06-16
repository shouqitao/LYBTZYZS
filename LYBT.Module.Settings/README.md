# LYBT.Module.Settings

系统设置模块，维护应用级配置项。

## 主要服务及接口
- `ISettingsService` / `SettingsService`
- `ISettingsRepository` / `SettingsRepository`

## 重要模型和DTO
- `SettingsModel`
- `SettingsDto`、`SettingsCreateDto`、`SettingsEditDto`、`SettingsDetailDto`

## 用法
执行 `SettingsModule.Register(services)` 后，可通过 `ISettingsService` 读取和保存系统设置。

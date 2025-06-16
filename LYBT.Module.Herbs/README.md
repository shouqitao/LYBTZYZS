# LYBT.Module.Herbs

药材管理模块，提供药材信息的维护和查询功能。

## 主要服务及接口
- `IHerbService` / `HerbService`
- `IHerbRepository` / `HerbRepository`

## 重要模型和DTO
- `HerbModel`
- `HerbDto`、`HerbCreateDto`、`HerbEditDto`、`HerbDetailDto`

## 用法
调用 `HerbsModule.Register(services)` 后即可通过 `IHerbService` 增删改查药材数据。

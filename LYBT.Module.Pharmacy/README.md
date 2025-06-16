# LYBT.Module.Pharmacy

药房任务模块，管理处方抓药及代煎流程。

## 主要服务及接口
- `IPharmacyService` / `PharmacyService`
- `IPharmacyRepository` / `PharmacyRepository`

## 重要模型和DTO
- `PharmacyModel`
- `PharmacyDto`、`PharmacyCreateDto`、`PharmacyEditDto`、`PharmacyDetailDto`

## 用法
启动时通过 `PharmacyModule.Register(services)` 注册依赖，之后使用 `IPharmacyService` 处理药房任务。

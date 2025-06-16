# LYBT.Module.TreatmentRoom

治疗室执行记录模块，跟踪各类治疗计划的执行情况。

## 主要服务及接口
- `ITreatmentRoomService` / `TreatmentRoomService`
- `ITreatmentRoomRepository` / `TreatmentRoomRepository`

## 重要模型和DTO
- `TreatmentRoomModel`
- `TreatmentRoomDto`、`TreatmentRoomCreateDto`、`TreatmentRoomEditDto`、`TreatmentRoomDetailDto`

## 用法
通过 `TreatmentRoomModule.Register(services)` 注入依赖，使用 `ITreatmentRoomService` 管理治疗室记录。

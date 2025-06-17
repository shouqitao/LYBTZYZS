# LYBT.Module.TreatmentRoom

诊疗室模块负责辅助治疗任务的执行与资源调度，不再仅是简单的空间记录。除了管理诊疗室基础信息，还负责治疗任务队列的流转与状态控制。

## 主要服务及接口
- `ITreatmentRoomService` / `TreatmentRoomService`
- `ITreatmentRoomRepository` / `TreatmentRoomRepository`

## 重要模型和DTO
- `TreatmentRoomModel` – 诊疗室基础信息
- `TreatmentTaskModel` – 具体治疗任务记录
- `TreatmentRoomDto`、`TreatmentRoomCreateDto`、`TreatmentRoomEditDto`、`TreatmentRoomDetailDto`

## 用法
通过 `TreatmentRoomModule.Register(services)` 注入依赖，使用 `ITreatmentRoomService` 管理治疗室记录。

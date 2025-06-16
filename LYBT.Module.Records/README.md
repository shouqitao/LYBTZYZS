# LYBT.Module.Records

病历模块，管理患者诊疗记录与病历文档。

## 主要服务及接口
- `IRecordService` / `RecordService`
- `IRecordRepository` / `RecordRepository`

## 重要模型和DTO
- `RecordModel`
- `RecordDto`、`RecordCreateDto`、`RecordEditDto`、`RecordDetailDto`

## 用法
调用 `RecordsModule.Register(services)` 注册后，使用 `IRecordService` 对病历数据进行增删改查。

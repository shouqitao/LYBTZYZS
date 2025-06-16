# LYBT.Module.Logs

操作日志模块，用于记录系统内各类操作行为。

## 主要服务及接口
- `ILogService` / `LogService`
- `ILogRepository` / `LogRepository`

## 重要模型和DTO
- `LogModel`
- `LogDto`、`LogQueryDto`、`LogCreateDto`

## 用法
启动时执行 `LogsModule.Register(services)`，应用层通过 `ILogService` 新增或查询日志记录。

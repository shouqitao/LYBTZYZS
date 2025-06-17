# LYBT.Module.Logs

系统日志模块，用于统一记录并查询应用运行过程中的重要操作。提供写入日志、按条件查询以及审计追踪等功能，供其它业务模块共享调用。

## 模块职责

| 功能点 | 说明 |
| --- | --- |
| 记录操作日志 | 包括新增、修改、删除、登录、权限变更等操作均需留痕 |
| 查询日志记录 | 支持按用户、时间、模块、操作类型等条件筛选 |
| 审计支持 | 病历、患者、医生等重要资料变更均需记录详细历史 |
| 不同模块共享使用 | 各业务模块可调用统一日志服务 `ILogService` 进行写入 |
| 管理员查看所有日志 | 普通医生仅能查看自己的操作记录 |

## 数据模型

`OperationLogModel` 建议的字段如下：

```csharp
public class OperationLogModel
{
    public string Id { get; set; }
    public string UserId { get; set; }               // 执行者
    public string UserName { get; set; }
    public string Module { get; set; }               // 模块名称，如 "Patients"
    public string Operation { get; set; }            // 操作类型，如 "Add", "Edit", "Delete"
    public string? TargetId { get; set; }            // 被操作对象主键
    public string? Description { get; set; }         // 变更说明
    public DateTime Timestamp { get; set; }
    public string IP { get; set; }                   // 来源 IP（可选）
}
```

项目中实际使用 `LogModel` 与 `LogDto` 等进行映射，字段与上表含义相同。

## 接口概览（`LogsController`）

| 接口名称 | 方法 | 权限 | 说明 |
| --- | --- | --- | --- |
| `GetLogPaged` | `GET` | Admin | 分页获取日志，可按模块、用户、时间等筛选 |
| `GetLogById` | `GET` | Admin | 获取单条日志详情 |
| `SearchLogs` | `GET` | Admin | 关键词模糊搜索（用户名、模块名等） |
| `GetMyLogs` | `GET` | 医生 | 当前登录用户查看自己的日志 |
| `WriteLog` | `POST` | 内部调用 | 记录一条操作日志 |

## 服务接口（`ILogService`）

业务模块在关键操作处调用 `ILogService.WriteAsync` 统一写日志，例如：

```csharp
await _logService.WriteAsync(userId, "张三", "Patients", "Edit", patient.Id, "修改电话从xxx改为yyy");
```

## 典型使用场景

| 模块 | 操作类型 | 记录内容示例 |
| --- | --- | --- |
| Users | 启用/禁用 | 管理员禁用医生账号 |
| Patients | 修改 | 修改地址、电话等 |
| Records | 设置共享 | 病历被共享给某医生 |
| Billing | 退款 | 某账单发起退款并成功 |

在应用启动时调用 `LogsModule.Register(services)` 完成依赖注入，即可通过 `ILogService` 在各个模块中进行日志写入与查询。

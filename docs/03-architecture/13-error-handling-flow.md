# 错误处理流程

> ⚠️⚠️ **本文档已过期，待重写** ⚠️⚠️
>
> 本文描述的异常体系（`LYBTException` 基类、`ForbiddenException(403)`、`ValidationException(422)`）**与代码不符**。实际异常体系以 [`06-error-handling.md`](06-error-handling.md) 为准：
>
> | 项 | 本文（错误） | 实际（见 06-error-handling.md） |
> |---|---|---|
> | 业务异常基类 | `LYBTException` | `AppException`（命名空间 `LYBT.Shared.ExceptionHandling.Exceptions`） |
> | `ValidationException` HTTP 码 | 422 | **400** |
> | `ForbiddenException` | 存在 (403) | **不存在**（无此类型；权限不足走 `UnauthorizedAccessException` → 403） |
> | 层级 | 平铺 5 子类 | `AppException → BusinessException → ValidationException/NotFoundException/ConflictException` 三层 |
>
> 下方原内容保留作为重写参考，**不要据此判断代码行为**。

## 异常层级

```
System.Exception
  └─ LYBTException (业务异常基类)
       ├─ ValidationException (422) — 输入校验失败
       ├─ NotFoundException (404) — 资源不存在
       ├─ ConflictException (409) — 状态冲突（并发、重复）
       ├─ UnauthorizedException (401) — 未认证
       └─ ForbiddenException (403) — 无权限
```

## Desktop 端处理链

```
DispatcherUnhandledException (UI 线程)
  ├─ 识别异常类型
  ├─ LYBTException → Toast 中文消息
  ├─ HttpException → 网络错误提示 / 切本地模式
  ├─ 其他 → "系统异常，请联系管理员" + TraceId
  └─ 记录 Serilog 完整堆栈

AppDomain.UnhandledException (非 UI 线程)
  └─ 记录 Serilog + 重启
```

## Server 端处理链

```
请求进入
  ├─ 401 Unauthorized → JWT 无效/过期
  ├─ 403 Forbidden → 角色策略拒绝
  ├─ 404 NotFound → 资源不存在
  ├─ 409 Conflict → 并发冲突/重复操作
  ├─ 422 ValidationException → 字段级错误列表
  ├─ 500 其他异常 → TraceId + 中文摘要（生产屏蔽堆栈）
  └─ 全局 ExceptionHandlerMiddleware 统一处理
```

## 用户感知映射

| 异常类型 | Desktop 呈现 | 示例 |
|---------|-------------|------|
| ValidationException | Snackbar 黄色 + 字段提示 | "密码长度不足 8 位" |
| NotFoundException | Snackbar 红色 | "患者不存在" |
| ConflictException | Snackbar 红色 | "该患者已有进行中医案" |
| UnauthorizedException | 跳转登录页 | "登录已过期，请重新登录" |
| ForbiddenException | Snackbar 红色 | "无权执行此操作" |
| HttpException (网络) | 断网横幅 + 自动切本地 | "网络连接中断" |
| 未知异常 | DialogHost 模态 | "系统异常（TraceId: xxx）" |

## 错误码规范

| 前缀 | 模块 | 示例 |
|------|------|------|
| ERR-1xxxx | Auth | ERR-10101 密码错误 |
| ERR-2xxxx | Users | ERR-20101 用户不存在 |
| ERR-3xxxx | MedicalCases | ERR-30101 医案不存在 |
| ERR-4xxxx | Herbs | ERR-40101 药材不存在 |
| ERR-7xxxx | System | ERR-70506 切换模式失败 |

# 部署 API

> **用户速览**：运维部署接口。上传更新包、重启服务，仅 sysadmin/管理员可用。

## 基本信息

| 属性 | 值 |
|------|-----|
| Controller | `DeployController` |
| 路由前缀 | `/api/v1/deploy` |
| 默认权限 | `[Authorize(Policy = AdminOrSuperAdmin)]` |

## POST /deploy/upload — 上传更新包

上传 ZIP 格式的更新包到服务器 `uploads/` 目录。

**请求**: `multipart/form-data`，字段名 `file`（仅支持 `.zip`）

**成功响应** (200):
```json
{
  "success": true,
  "message": "更新包上传成功",
  "data": { "fileName": "update_20260803_120000.zip", "size": 102400 }
}
```

**失败响应**:
| 场景 | 状态码 | 说明 |
|------|--------|------|
| 文件为空 | 422 | 未选择文件或文件为空 |
| 非 ZIP 格式 | 422 | 仅支持 ZIP 格式的更新包 |

## POST /deploy/restart — 重启服务

发送服务重启指令（2 秒后由 `IHostApplicationLifetime.StopApplication()` 执行优雅停机）。

**成功响应** (200):
```json
{
  "success": true,
  "message": "服务重启指令已发送",
  "data": null
}
```

> ⚠️ **注意**: 重启指令为**异步执行**，调用后服务会中断数秒。生产环境谨慎使用。

## HTTP 状态码

| 码 | 含义 |
|----|------|
| 200 | 成功 |
| 401/403 | 未认证/权限不足（需 AdminOrSuperAdmin） |

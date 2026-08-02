# API 参考

> **用户速览**：所有 HTTP 接口的文档。前端开发者和测试人员主要看这里。

## 基本信息

| 属性 | 值 |
|------|-----|
| Base URL | `https://{host}/api/v1` |
| 认证 | Bearer Token (JWT) |
| 格式 | `application/json` |
| Token 有效期 | 60 分钟 |

## 获取 Token

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.data.token')
```

## 响应格式

```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "requestId": "0HN8V..."
}
```

## 端点索引

| 模块 | 文件 | 端点数 | 策略 |
|------|------|:------:|------|
| [认证](01-auth.md) | 登录/登出/刷新/验证 | 5 | AllowAnonymous |
| [用户](02-users.md) | 增删改查+批量操作 | 14 | AdminOrSuperAdmin |
| [患者](03-patients.md) | 增删改查+引用检查 | 10 | DoctorOrAdminOrReceptionist |
| [药材](04-herbs.md) | 增删改查+批量导入 | 8 | DoctorOrReceptionist |
| [验方](05-formulas.md) | 增删改查+验证+导入 | 10 | DoctorOrReceptionist |
| [医案](06-medical-cases.md) | 核心业务+状态流转 | 18 | DoctorOrAdmin |
| [挂号](07-registrations.md) | 挂号+接诊+退号 | 7 | DoctorOrAdminOrReceptionist |
| [打印](08-printing.md) | 打印记录回写 | — | 挂在医案下 |
| [同步](09-sync.md) | v2.0 规划 | 6 | 🔴 未实现 |
| [配置](10-configuration.md) | 系统配置读写 | 3 | AdminOrSuperAdmin |
| [健康检查](11-health.md) | 探活+详细检查 | 4 | 匿名/已认证 |
| [诊断](12-diagnostics.md) | 日志级别调整 | 4 | AdminOrSuperAdmin |
| [报表](13-reports.md) | 收入/问诊/药材统计 | 3 | DoctorOrAdmin |

## 通用 HTTP 状态码

| 码 | 含义 |
|----|------|
| 200 | 成功 |
| 201 | 已创建 |
| 400 | 参数错误 |
| 401 | 未授权 |
| 403 | 权限不足 |
| 404 | 不存在 |
| 422 | 业务规则失败 |
| 429 | 限流 |
| 500 | 服务器错误 |

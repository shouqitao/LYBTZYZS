# API 参考

> **用户速览**：所有 HTTP 接口的文档。前端开发者和测试人员主要看这里。
>
> **权威文档在哪**：API 端点契约即本目录（04-api-reference/）；权限标注以 [01-product/04-permissions.md](../01-product/04-permissions.md) 为准。完整查询指南见 [docs/README.md](../README.md#ai-查询指南)。

## 基本信息

| 属性 | 值 |
| ------ | ----- |
| Base URL | `https://{host}/api/v1` |
| 认证 | Bearer Token (JWT) |
| 格式 | `application/json` |
| Token 有效期 | 配置驱动：base 480 / Dev·Test 60 / Prod 30 分钟（见 [01-auth.md](01-auth.md)） |

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
| ------ | ------ | :------: | ------ |
| [认证](01-auth.md) | 登录/登出/刷新/验证 | 5 | AllowAnonymous（validate 需认证） |
| [用户](02-users.md) | 增删改查+批量操作 | 14 | AdminOrSuperAdmin |
| [患者](03-patients.md) | 增删改查+引用检查+身份证查询 | 11 | GET/POST/PUT: Doctor/Receptionist；DELETE/启停: Admin+；恢复: Admin |
| [药材](04-herbs.md) | 增删改查+批量导入+引用检查+恢复+批量启用/禁用 | 13 | GET: Doctor/Admin（前台不可查）；写: Admin+；恢复: Admin |
| [验方](05-formulas.md) | 增删改查+验证+导入+恢复+批量启用/禁用 | 13 | GET: Doctor/Admin（前台不可查）；写: Admin/Doctor；批量: Admin+；恢复: Admin |
| [医案](06-medical-cases.md) | 核心业务+状态流转+搜索+查询+审计 | 21 | 创建: DoctorOnly；查看/查询: Doctor(自己)+Admin；审计: Doctor/Admin |
| [挂号](07-registrations.md) | 挂号+接诊+退号+队列 | 7 | DoctorOrAdminOrReceptionist |
| [打印](08-printing.md) | 打印记录回写 | — | 挂在医案下 |
| [同步](09-sync.md) | v2.0 规划 | 6 | 🔴 未实现 |
| [配置](10-configuration.md) | 系统配置读写 | 3 | SysAdminOnly |
| [健康检查](11-health.md) | 探活+详细检查 | 3 | 匿名/已认证 |
| [诊断](12-diagnostics.md) | 日志级别调整 | 4 | AdminOrSuperAdmin |
| [报表](13-reports.md) | 收入/就诊/药材统计 | 3 | DoctorOrAdmin |
| [部署](14-deploy.md) | 更新包上传+服务重启 | 2 | SysAdminOnly |

> **策略说明**：策略常量定义见 `PolicyConstants.cs`（7 项：`AdminBusinessOnly`、`DoctorOnly`、`DoctorOrAdmin`、`AdminOrSuperAdmin`、`SysAdminOnly`、`DoctorOrReceptionist`、`DoctorOrAdminOrReceptionist`）。K1 待修复：`DoctorOrReceptionist` 代码注册仅含 Doctor/Receptionist，缺 SuperAdmin/Admin。

## 通用 HTTP 状态码

| 码 | 含义 |
| ---- | ------ |
| 200 | 成功 |
| 201 | 已创建 |
| 400 | 参数错误 |
| 401 | 未授权 |
| 403 | 权限不足 |
| 404 | 不存在 |
| 422 | 业务规则失败 |
| 429 | 限流 |
| 500 | 服务器错误 |

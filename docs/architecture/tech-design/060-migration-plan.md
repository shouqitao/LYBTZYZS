# 060-迁移计划（最小改动）

## M1：最小闭环（本轮）
- Server：默认端口改 5001；/api/health；/api/auth/login；sysadmin 播种（Dev 打印一次）。
- Desktop：默认 BaseUrl=5001；最小登录对接（保存 token）。
- Shared：LoginRequest/Response DTO 就位。

## M2：用户模块梳理
- Users 服务与模型一致化；最小单测恢复。

## 自检结果表（✅ 全部已存在）

| 组件 | 状态 | 证据文件 | 需要修改 |
|------|------|----------|----------|
| **登录接口** | ✅ 已存在 | `AuthController.cs:39-76` | 无 |
| **健康检查** | ✅ 已存在 | `HealthController.cs:37-59` | 无 |
| **端口5001** | ✅ 已配置 | `Program.cs:49` | 无 |
| **Desktop BaseUrl** | ✅ 已设置 | `Shell/appsettings.json:3` | 无 |
| **Shared DTO** | ✅ 完整 | `LoginRequest.cs`, `LoginResponse.cs` | 无 |
| **sysadmin播种** | ✅ 已实现 | `DatabaseInitializationService.cs:62` | 无 |

## 结论：M1阶段无需任何代码修改
**所有最小闭环组件均已实现并正常工作** 🎉


# 部署与回滚指南

本文档定义 LYBT 系统的部署流程和回滚策略。服务端部署方式取决于目标环境（Linux/nohup、Windows Service、IIS），客户端使用 Velopack 自动更新分发。

> 备份恢复见 [06-backup-recovery.md](./06-backup-recovery.md)；监控告警见 [07-monitoring-alerting.md](./07-monitoring-alerting.md)；部署架构见 [01-deployment.md](./01-deployment.md)。

---

## 版本管理

### 版本号规则

遵循语义化版本 `MAJOR.MINOR.PATCH`（如 `1.2.3`），记录于 `VERSION` 文件。

### 发布包结构

```
C:\Services\LYBT-releases\
├── v1.2.3\                    # 当前版本
│   ├── LYBT.WebAPI\           # 服务端发布包
│   └── LYBT.Desktop\          # 客户端发布包
├── v1.2.2\                    # 前一版本（回滚保留）
└── rollback.json              # 回滚元数据
```

---

## 服务端部署流程

> 部署步骤（编译/上传/配置/重启）详见 [01-deployment.md §Linux 公网服务器部署](./01-deployment.md#linux-公网服务器部署)。本节仅覆盖回滚场景的前置检查与回滚操作。

### 前置检查

```bash
# 1. 确认当前版本
ssh -p 5555 player@60.190.215.86 "cat /home/player/lybt-api/VERSION"

# 2. 健康检查
curl -s http://60.190.215.86:5000/health

# 3. 数据库备份（必需）
# 远程数据库 192.168.190.243/LYBTDB_Dev，备份方式见 01-deployment.md
```

---

## 回滚流程

> 回滚操作在生产服务器 `60.190.215.86` 上执行。回滚前先查 [01-deployment.md](./01-deployment.md) 确认当前部署状态。

### 场景 1：服务启动失败

```bash
# 1. 查看日志
ssh -p 5555 player@60.190.215.86 "tail -50 /home/player/lybt-api/logs/*.log"

# 2. 回滚到前一版本
ssh -p 5555 player@60.190.215.86 "
  cd /home/player/lybt-api &&
  pkill -f LYBT.WebAPI || true &&
  cp -r backups/v前版本号/* . &&
  bash start.sh
"

# 3. 验证
curl -s http://60.190.215.86:5000/health
```

### 场景 2：数据库迁移需回退

```
⚠️ EF Core 不支持自动迁移回退。处理方式：
```

**手动迁移回退步骤**：

```bash
# 1. 停止服务
ssh -p 5555 player@60.190.215.86 "pkill -f LYBT.WebAPI || true"

# 2. 恢复数据库到部署前备份
# 数据库 192.168.190.243/LYBTDB_Dev，RESTORE DATABASE 操作见 01-deployment.md

# 3. 回滚应用版本（同场景 1）

# 4. 启动并验证
ssh -p 5555 player@60.190.215.86 "cd /home/player/lybt-api && bash start.sh"
curl -s http://60.190.215.86:5000/health
```

**数据库迁移回退策略**：

| 策略 | 适用场景 | 操作 |
|------|----------|------|
| 恢复备份 | 迁移导致数据丢失或结构损坏 | `RESTORE DATABASE ... WITH REPLACE` |
| 手动回退脚本 | 需要保留新数据但撤销 schema 变更 | 手动编写 `ALTER TABLE` / `DROP` 语句 |
| 仅回滚应用 | 迁移本身成功但应用代码有 bug | 恢复前一版本应用，不动数据库 |
| 跳过迁移 | 迁移无破坏性，可安全留在数据库中 | 应用回滚到前一版本，忽略新迁移 |

> **最佳实践**：迁移前始终创建数据库备份。生产环境禁止使用 `dotnet ef database update` 自动迁移，应通过 SQL 脚本手动执行。

### 场景 3：功能异常但服务正常

```
1. 在 Diagnostics API 启用 Debug 日志
   → POST /api/v1/diagnostics/logging/debug/enable { "level": "Debug", "durationMinutes": 30 }

2. 复现问题，收集日志
   → GET /api/v1/diagnostics/recent-logs
   → 复制 logs/ 目录下相关日志文件

3. 评估影响
   → 非关键功能异常 → 记录问题，安排修复版本
   → 关键功能不可用 → 执行回滚（场景 1）

4. 修复后重新部署（回到"部署流程"）
```

---

## 客户端部署

### 发布流程

```powershell
# 1. 构建 Desktop 发布包
dotnet publish src/Client/Desktop/Shell/App.csproj -c Release -r win-x64

# 2. 复制到发布目录
Copy-Item "bin\Release\win-x64\publish\*" "C:\Services\LYBT-releases\v1.2.3\LYBT.Desktop\" -Recurse

# 3. 各客户端从共享目录安装
# \\SERVER\LYBT-releases\v1.2.3\LYBT.Desktop\setup.exe
```

### 客户端回滚

客户端版本独立于服务端。回滚方式：

1. 关闭 Desktop 客户端
2. 从 `C:\Services\LYBT-releases\v前版本号\LYBT.Desktop\` 重新复制
3. 客户端配置在 `%APPDATA%\LYBT\` — 回滚不影响配置和数据

---

## 回滚决策矩阵

| 场景 | 影响 | 回滚时间 | 操作 |
|------|------|----------|------|
| 服务无法启动 | 全部用户 | < 5 分钟 | 场景 1：版本回滚 |
| 数据库迁移失败 | 全部用户 | < 30 分钟 | 场景 2：数据库恢复 + 版本回滚 |
| 单一功能异常 | 部分用户 | 评估后决定 | 场景 3：Debug → 评估 → 修复或回滚 |
| 客户端不兼容 | 单用户 | < 10 分钟 | 客户端版本回滚（不影响服务端） |

---

## 回滚后验证清单

每次回滚完成后，必须逐项验证以下项目：

```bash
# ===== 回滚验证清单 =====
API="http://60.190.215.86:5000"

# 1. 版本确认
ssh -p 5555 player@60.190.215.86 "cat /home/player/lybt-api/VERSION"

# 2. 健康检查
curl -sf $API/health || echo "FAIL: Health endpoint unreachable"

# 3. 数据库连接
curl -sf $API/health/details -H "Authorization: Bearer $TOKEN" || echo "FAIL: Database health check failed"

# 4. 登录功能
LOGIN=$(curl -sf -X POST $API/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123456"}')
echo "$LOGIN" | grep -q '"success":true' || echo "FAIL: Login failed"

# 5. 核心 API 抽查
TOKEN=$(echo "$LOGIN" | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")
curl -sf "$API/api/v1/patients?page=1&pageSize=5" -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json; d=json.load(sys.stdin); print(f'Patients: {len(d[\"data\"])} records')"
curl -sf "$API/api/v1/herbs?page=1&pageSize=5" -H "Authorization: Bearer $TOKEN" | python3 -c "import sys,json; d=json.load(sys.stdin); print(f'Herbs: {len(d[\"data\"])} records')"

# 6. 日志无新错误
ssh -p 5555 player@60.190.215.86 "tail -20 /home/player/lybt-api/logs/*.log | grep -i error" || echo "No recent errors"

# 7. Desktop 客户端连接测试
echo "Manual: Verify Desktop client can connect and login"
```

### 验证通过标准

| 项目 | 通过条件 |
|------|----------|
| 版本号 | 与回滚目标版本一致 |
| 健康检查 | status = Healthy |
| 数据库连接 | status = Healthy |
| 登录功能 | success = true |
| 核心 API | patients / herbs 正常返回 |
| 日志 | 无新增 Error 条目 |
| 客户端连接 | Desktop 可正常登录使用 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
| 2026-06-25 | v1.1 | 新增数据库迁移回退策略表、回滚后验证清单 |

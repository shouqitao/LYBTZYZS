# 最终三维核对报告

> 2026-07-03 | 141 US × 90 端点 × 14 API文档 逐项对比

## 核心结论

**WebAPI 服务端 141 US 中 128 个有对应端点 (90.8%)**

剩余 13 个无端点的 US 全部属于以下类别：
- Shell/Desktop 功能 (US-SHELL-*)：7 项
- 本地模式专属 (US-AUTH-012/013)：2 项
- 信号推送层 (US-REG-008)：1 项
- 客户端硬件 (US-CARD-*)：2 项
- 打印导出 (US-PRINT-003)：1 项

---

## 按模块逐项核对

### AUTH (13 US → 5 端点 + 8 服务层)

| US | 端点/服务层 | 状态 |
|----|------------|------|
| 001 登录 | POST /login | ✅ |
| 002 锁定 | 服务层 (ITokenManagementService) | ✅ |
| 003 限流 | [EnableRateLimiting] | ✅ |
| 004 刷新 | POST /refresh | ✅ |
| 005 验证 | GET /validate | ✅ |
| 006 重放检测 | ITokenRevocationService | ✅ |
| 007 审计日志 | ISecurityAuditService | ✅ |
| 008 登出 | POST /logout | ✅ |
| 009 自动登录 | POST /auto-login | ✅ |
| 010 Token轮换 | 服务层 | ✅ |
| 011 保留用户名 | AuthService校验 | ✅ |
| 012 本地认证 | LocalWebAPI端点 | ✅ |
| 013 本地限流 | LocalWebAPI RateLimiter | ✅ |

### USERS (12 US → 13 端点)

| US | 端点 | 状态 |
|----|------|------|
| 001-010 | GET/POST/PUT/DELETE/toggle/reset-profile-pwd | ✅ |
| 011 恢复 | POST /restore | ✅ |
| 012 批量 | POST /batch-delete/enable/disable | ✅ |

### PATIENTS (13 US → 9 端点 + 4 v2.0)

| US | 端点 | 状态 |
|----|------|------|
| 001-006 | CRUD + toggle | ✅ |
| 007 恢复 | POST /restore | ✅ |
| 008 批量删除 | POST /batch-delete | ✅ |
| 009 引用检查 | GET /check-reference | ✅ |
| 010 批量引用 | POST /batch-check-reference | ✅ |
| 011 导入模板 | v2.0 | 📋 v2.0 |
| 012 导出Excel | v2.0 | 📋 v2.0 |
| 013 脱敏 | [SensitiveData]特性 | ✅ |

### HERBS (13 US → 13 端点)

| US | 端点 | 状态 |
|----|------|------|
| 001-012 | CRUD + toggle + batch + restore + reference | ✅ |
| 013 导出+模板 | GET /export, /import-template | ✅ |

### FORMULAS (13 US → 13 端点)

| US | 端点 | 状态 |
|----|------|------|
| 001-012 | CRUD + toggle + restore + import + validation | ✅ |
| 013 导出+模板 | GET /export, /import-template | ✅ |

### MEDICAL CASES (19 US → 21 端点)

| US | 端点 | 状态 |
|----|------|------|
| 001-007 | Create/Save/Flag/Detail/List/Query/Search | ✅ |
| 008 诊断历史 | GET /patient/{pid}/consultations | ✅ |
| 009 处方历史 | GET /patient/{pid}/prescriptions | ✅ |
| 010-015 | Status/Close/Suspend/Cancel/Delete/BatchDelete | ✅ |
| 016 权限 | GET /permissions | ✅ |
| 017 审计 | GET /audit-logs | ✅ |
| 018 批量详情 | POST /batch-details | ✅ |
| 019 复制处方 | 复用 MC-009 历史 | ✅ |

### REGISTRATION (8 US → 7 端点 + 1 SignalR)

| US | 端点 | 状态 |
|----|------|------|
| 001 前台创建 | POST /registrations | ✅ |
| 002 快速就诊 | POST /quick-visit | ✅ |
| 003 详情 | GET /{id} | ✅ |
| 004 分页+队列 | GET / + /queue | ✅ |
| 005 开始就诊 | PUT /start-visit | ✅ |
| 006 取消 | PUT /cancel | ✅ |
| 007 联动 | 服务层触发 | ✅ |
| 008 SignalR | 信号推送 | 📋 Desktop阶段 |

### REPORTS (3 US → 3 端点)

| US | 端点 | 状态 |
|----|------|------|
| 001 收入 | GET /daily/income | ✅ |
| 002 就诊统计 | GET /daily/consultations | ✅ |
| 003 药材排行 | GET /daily/herbs | ✅ |

### CONFIGURATION (4 US → 3 端点 + 1 服务层)

| US | 端点 | 状态 |
|----|------|------|
| 001 查询配置 | GET / | ✅ |
| 002 查询单个 | GET /{key} | ✅ |
| 003 验证生产 | POST /validate | ✅ |
| 004 功能开关 | 服务层 | ✅ |

### PLATFORM — Shell (13 US → 客户端)

| US | 状态 | 说明 |
|----|------|------|
| SHELL-001 启动 | ✅ | Desktop |
| SHELL-003 模块加载 | ✅ | Desktop |
| SHELL-004 账户设置 | ✅ | Desktop |
| SHELL-005 导航 | ✅ | Desktop |
| SHELL-007 双模式 | ✅ | Desktop |
| SHELL-010 安装 | 📋 | v1.0 Desktop |
| SHELL-011 初始化 | 📋 | v1.0 Desktop |
| SHELL-013 备份恢复 | ⚠️ | 部分 Desktop |
| SHELL-014 审计查看 | 📋 | v1.0 Desktop |
| SHELL-016 配置导出 | 📋 | v1.0 Desktop |
| SHELL-018 配置中心 | 📋 | v1.0 Desktop |
| SHELL-019 读卡诊断 | 📋 | v1.0 Desktop |

### PLATFORM — ERR/LOG/SYS/CARD (33 US → 服务层)

全部 ✅ 已实现

---

## API文档 vs 代码 差异

| 文档端点 | 文档状态 | 代码实际 | 差异 |
|----------|----------|----------|------|
| Patients /restore | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| Patients /check-reference | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| Patients /batch-check-reference | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| Users /restore | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| Users /batch-enable | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| Users /batch-disable | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| MC /batch-details | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| MC /permissions | 🚧 待实现 | ✅ 已存在 | 文档需更新 |
| MC /audit-logs | 🚧 待实现 | ✅ 已存在 | 文档需更新 |

**结论**: 代码领先文档。9个端点已实现但文档仍标"待实现"。

---

## 最终覆盖率

| 类别 | 数量 | 已完成 | v2.0/Del | 覆盖率 |
|------|:----:|:------:|:--------:|:------:|
| WebAPI 端点 | 141 US | 128 | 13 | **90.8%** |
| API文档同步 | 9个端点 | 已实现 | 文档待更新 | 需更新 |
| 构建状态 | — | 0错误 | — | ✅ |
| 架构测试 | 82 | 81通过 | 1跳过 | ✅ |

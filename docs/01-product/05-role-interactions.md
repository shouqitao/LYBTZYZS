# 角色交互与协同流程 (Role Interactions)

> 版本: v4.0 | 日期: 2026-08-02 | 状态: 文档定义（设计态）

本文件定义四角色之间的交接闭环、端到端协同流程、异常场景处理矩阵。

**图例**：✅ 已实现 | 📋 已设计未实现 | 🔴 未实现/缺失

> 角色定位与工作流程详见 [02-personas.md](02-personas.md)。
> 权限矩阵与修复项详见 [04-permissions.md](04-permissions.md)。

---

## 一、端到端协同泳道图

### 1.1 首诊完整旅程（跨 4 角色）

```mermaid
flowchart TD
    subgraph Sysadmin["🔧 Sysadmin"]
        SA1[部署系统] --> SA2[首次登录 → 改密]
        SA2 --> SA3[填诊所信息]
        SA3 --> SA4[创建 Admin 账号]
        SA4 --> SA5[交权给 Admin]
    end

    subgraph Admin["👔 Admin"]
        AD1[创建 Doctor 账号] --> AD2[创建 Receptionist 账号]
        AD2 --> AD3[导入药材库 📋]
        AD3 --> AD4[导入验方库 📋]
        AD4 --> AD5[诊所就绪]
    end

    subgraph Receptionist["📋 Receptionist"]
        RE1[患者到达 → 读卡登记] --> RE2[创建挂号 → Waiting]
        RE2 --> RE3[患者进入候诊队列]
    end

    subgraph Doctor["🩺 Doctor"]
        DO1[查看待诊队列] --> DO2[选择患者 → StartVisit]
        DO2 --> DO3[填写诊断 + 开方]
        DO3 --> DO4[打印处方]
        DO4 --> DO5[完成医案]
    end

    SA5 -.->|"创建账号"| AD1
    AD5 -.->|"系统就绪"| RE1
    RE3 -.->|"队列更新"| DO1
    DO5 -.->|"队列移除"| RE3
```

### 1.2 复诊旅程

```mermaid
flowchart TD
    subgraph Receptionist["📋 Receptionist"]
        R1[患者到达] --> R2[查找患者]
        R2 --> R3[创建挂号]
        R3 --> R4[队列更新]
    end

    subgraph Doctor["🩺 Doctor"]
        D1[选患者] --> D2[查看既往医案 📋 MC-008/009 🔴]
        D2 --> D3[导入历史处方 📋]
        D3 --> D4[调整后开方]
        D4 --> D5[打印 + 完成]
    end

    R4 -.-> D1
```

### 1.3 医案纠偏旅程（Admin 介入）

```mermaid
flowchart TD
    subgraph Admin["👔 Admin"]
        A1[发现医案错误] --> A2[选择目标医案]
        A2 --> A3[填写纠偏原因 📋 必填]
        A3 --> A4[执行修改]
        A4 --> A5[保存 → 写入审计日志 📋]
    end

    subgraph Doctor["🩺 Doctor"]
        D1[下次查看该医案] --> D2[看到纠偏记录 📋]
    end

    A5 -.-> D2
```

---

## 二、角色交接闭环

### 2.1 交接点清单

| # | 交接 | 上游 → 下游 | 传递物 | 状态 | 关键缺口 |
|:---:|------|------------|--------|:----:|----------|
| 1 | 系统初始化 | Sysadmin → Admin | Admin 账号 + 密码 | 📋 | 向导未实现（当前仅连接配置） |
| 2 | 用户管理 | Sysadmin → Admin | 管理权（创建/编辑/禁用/删除/重置密码） | 📋 | Sysadmin 管 Admin 的 UI 待开发 |
| 3 | 用户管理 | Admin → Doctor/Receptionist | 管理权（创建/编辑/禁用/删除/重置密码） | ⚠️ | 部分功能可用，禁用/删除待完善 |
| 4 | 基础数据准备 | Admin → Doctor/Receptionist | 药材库 + 验方库 | 📋 | Excel 导入缺失 |
| 5 | 挂号登记 | Receptionist → Doctor | 挂号单（Registration，状态=Waiting） | ✅ | — |
| 6 | 开始就诊 | Doctor → 医案系统 | 医案（MedicalCase，状态=Suspended） | ✅ | StartVisit 不创建医案（BR-000 设计正确） |
| 7 | 完成就诊 | Doctor → 系统 | 完成医案 + 打印记录 | ⚠️ | 打印回写缺失、审计日志缺失 |
| 8 | 队列更新 | Doctor → Receptionist | 候诊队列状态变更（SignalR） | 📋 | SignalR 接线待验证 |
| 9 | 纠偏修改 | Admin → 医案系统 | 修改记录 + 审计日志 | 📋 | 纠偏 UI + 审计日志未实现 |
| 10 | 密码重置 | Sysadmin → Admin（离线工具） | 重置后的密码 hash | 📋 | 离线密码重置工具待开发 |

### 2.2 交接物定义

| 交接物 | 数据结构 | 说明 |
|--------|---------|------|
| Admin 账号 | `ApplicationUser` + `UserRole=Admin` | Sysadmin 创建，包含用户名+初始密码 |
| 药材库 | `Herb` 集合 | Admin 通过 Excel 导入或逐条创建 |
| 验方库 | `Formula` + `FormulaItem` 集合 | Admin 或 Doctor 创建，关联药材 |
| 挂号单 | `Registration` | 包含 PatientId, DoctorId, Status=Waiting |
| 医案 | `MedicalCase` | 包含 PatientId, DoctorId, Diagnosis, Status |
| 处方 | `Prescription` + `PrescriptionItem` | 医案关联，包含药材+剂量+煎法 |
| 审计日志 | `SecurityAuditLog` | 记录操作人、操作类型、时间、变更内容 |

---

## 三、异常场景处理矩阵

### 3.1 挂号环节

| 异常场景 | 处理策略 | 负责角色 | 实现状态 |
|---------|---------|---------|:--------:|
| 同一医生同时段重复挂号 | 提示冲突，引导选择其他时段/医生 | Receptionist | 📋 |
| 读身份证失败（磁条损坏） | 回退到手动输入 | Receptionist | 📋 |
| 患者信息不全 | 创建不完整档案，标记「待补充」 | Receptionist | 📋 |
| 患者要求退号 | 确认后取消挂号，状态→Cancelled | Receptionist | ✅ |
| 批量患者同时到达 | 支持连续读卡批量登记 | Receptionist | 📋 |

### 3.2 诊疗环节

| 异常场景 | 处理策略 | 负责角色 | 实现状态 |
|---------|---------|---------|:--------:|
| 打印机故障/缺纸 | 保存草稿，暂不打印 | Doctor | 📋 |
| 看诊中途离开 | 自动保存草稿，下次恢复 | Doctor | 📋 |
| 患者拒绝处方 | 标记「未开方」，完成医案 | Doctor | 📋 |
| 处方开错药（未打印） | 直接修改处方 | Doctor | ✅ |
| 处方开错药（已打印） | 作废原处方，重新开方 | Doctor | 📋 |
| 复诊查看历史 | 展示既往医案列表，支持导入处方 | Doctor | 🔴 MC-008/009 缺失 |
| 并发：同时编辑同一医案 | 乐观锁（RowVersion）拒绝后重试 | 系统 | ✅ |
| 时区问题（MC-LOCK） | UtcNow → 本地时间 | 系统 | 🔴 需修复 |

### 3.3 管理环节

| 异常场景 | 处理策略 | 负责角色 | 实现状态 |
|---------|---------|---------|:--------:|
| 药材被处方引用时删除 | 引用检查 → 拒绝删除 + 提示关联处方数 | Admin | 🔴 BR-DEL-001 缺失 |
| 患者被医案引用时删除 | 引用检查 → 拒绝删除 + 提示关联医案数 | Admin | 🔴 BR-DEL-001 缺失 |
| 禁用正在看诊的医生 | 等当前就诊完成后生效 | Admin | 📋 |
| Admin 误改医案 | 纠偏流程 + 审计日志追溯 | Admin | 📋 |
| 数据库备份失败 | 重试 + 日志记录 + 通知 sysadmin | Sysadmin | 📋 |

### 3.4 系统层面

| 异常场景 | 处理策略 | 负责角色 | 实现状态 |
|---------|---------|---------|:--------:|
| 远程服务器不可用 | 自动切换本地模式（SwitchingApiClient） | 系统 | ✅ |
| 模式切换中数据不一致 | v1.0 不处理（v2.0 同步） | — | ✅ 设计确认 |
| 版本升级数据迁移 | 非破坏性→MigrateAsync()；破坏性→sysadmin 手动 | Sysadmin | ✅ |
| 并发操作保护 | 乐观锁（RowVersion） | 系统 | ✅ |

---

## 四、角色关系图

```mermaid
flowchart LR
    SA["Sysadmin 🔧"] -->|"管理 Admin"| AD["Admin 👔"]
    AD -->|"管理 Doctor/Receptionist"| DR["Doctor 🩺"]
    AD -->|"管理 Doctor/Receptionist"| RE["Receptionist 📋"]
    AD -->|"导入药材/验方"| DR
    RE -->|"挂号单"| DR
    DR -->|"完成医案"| SYS["系统"]
    AD -->|"纠偏修改"| SYS
    SA -.->|"About 页公开联系方式"| AD
```

---

## 五、无人区场景处理

| 场景 | 决策 | 说明 |
|------|------|------|
| 版本升级数据迁移 | 自动 + 手动 | 非破坏性→`MigrateAsync()`；破坏性→sysadmin 手动+文档 |
| 医疗纠纷追溯 | v1.0 审计功能全部补回 | SecurityAuditLog 恢复 + 关键操作审计 + Sysadmin 查看 UI |
| 处方打印审计 | v1.0 打印回写补回 | 恢复 PrintLog 字段/实体 |
| 患者知情同意 | 方案 A（勾选） | 医案完成时确认勾选 + 打印处方单为知情同意载体 |
| 多医生协作 | BR-001 已覆盖 | 无需额外功能 |
| 本地数据生命周期 | v1.0 不处理 | v2.0 同步处理；本地数据随 Desktop 存在 |

---

## 六、变更日志

| 日期 | 变更 |
|------|------|
| 2026-08-02 | v4.0 新建：从 02-personas.md 拆分；新增泳道图（首诊/复诊/纠偏）；新增异常场景处理矩阵（4 类 20+ 场景）；新增交接物定义；新增角色关系图 |
| 2026-06-28 | 初始交接闭环验证（含在 personas 中） |

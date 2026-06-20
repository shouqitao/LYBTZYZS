# 术语表 (Glossary)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

本文档定义凌隐宝堂中医诊所管理系统中使用的核心术语。所有开发文档、代码注释、用户界面均应遵循本表规范。

---

## 铁律 (Iron Rules)

以下三条术语规则为**强制性约束**，任何文档、代码、UI 文案违反即为缺陷：

| 规则 | 正确 | 错误 | 说明 |
|------|------|------|------|
| **Consultation = 中医诊断** | "中医诊断" / "诊断" | ❌ "问诊" ❌ "就诊" | 仅指中医诊断部分（望闻问切、辨证），是 MedicalCase 的内部实体 (1:1) |
| **MedicalCase = 医案** | "医案" | ❌ "病历" | 完整的诊疗记录，是系统唯一聚合根 |
| **Formula = 验方** | "验方" / "经验方" | ❌ "公式" | 可复用的处方模板，定义药材组成和剂量 |
| **Prescription = 处方** | "处方" | ❌ 等同于 Formula | 具体的药材配伍和剂量，是 MedicalCase 的可选内部实体 (1:0..1) |

---

## 业务术语

| 英文 | 中文 | 说明 |
|------|------|------|
| Consultation | 中医诊断 | 现病史、舌诊、脉诊、辨证论治。MedicalCase 的内部实体 (1:1)。**铁律：不是"问诊"或"就诊"** |
| MedicalCase | 医案 | 核心聚合根。一次完整的诊疗记录，包含诊断 (Consultation) 和处方 (Prescription)。**铁律：不是"病历"** |
| Formula | 验方 / 经验方 | 可复用的处方模板，定义药材组成和剂量，不含价格计算。**铁律：不是"公式"** |
| Prescription | 处方 | 药材配伍和剂量，MedicalCase 的可选内部实体 (1:0..1) |
| PrescriptionItem | 处方项 | 处方中的单味药材：药名、剂量、单价、煎法 |
| FormulaHerbItem | 验方药材项 | 验方中的单味药材及用量，支持延迟绑定（HerbId 可空） |
| Herb | 药材 | 中药材，包含名称、分类、产地、价格等信息。记录式管理（无库存） |
| Patient | 患者 | 患者基本信息，含个人信息和就诊历史统计 |
| Registration | 挂号 | 患者就诊登记记录，含候诊队列管理和就诊状态跟踪 |
| User | 用户 | 系统用户，角色分为前台接待、医生、管理员、超级管理员 |
| MedicalCaseAuditLog | 医案审计日志 | 记录医案的所有修改历史，含操作人、变更字段（20 字段差异追踪）、修改原因 |
| MedicalCasePrintLog | 打印日志 | 记录医案打印历史（含 PrintType 区分打印类型） |
| SecurityAuditLog | 安全审计日志 | 记录认证相关的安全事件（登录、登出、令牌撤销等） |
| AuthSession | 认证会话 | JWT 登录会话记录 |
| RefreshToken | 刷新令牌 | JWT 刷新令牌，支持令牌轮换和重放攻击检测 |
| BlacklistedToken | 黑名单令牌 | 被撤销的 JWT 令牌 |
| PinYinCode | 拼音码 | 中文姓名/药材名的拼音首字母，用于快速检索 |
| DecocteMethod | 煎法 | 药材的煎煮方式：默认、先煎 (PreDecoct)、后下 (PostDecoct) |

---

## 技术术语

| 英文 | 中文 | 说明 |
|------|------|------|
| Aggregate Root | 聚合根 | DDD 概念。本系统中 MedicalCase 是**唯一**聚合根 |
| Dual-Mode | 双模式 | 远程 (SQL Server) + 本地 (LocalDB) 双运行模式，URL 切换 |
| CQRS | 命令查询职责分离 | MedicalCase 模块采用 CommandHandler 模式，非传统三层 |
| SoftDelete | 软删除 | 通过 `IsDeleted` 字段标记删除，配合全局查询过滤器 |
| BaseEntity | 基础实体 | 所有业务实体的基类，含 Id、CreatedAt、UpdatedAt、IsDeleted 等通用字段 |
| Controller | 控制器 | ASP.NET Core Web API 层，处理 HTTP 请求 |
| Service | 服务 | Server 端业务逻辑层 |
| Repository | 仓储 | Server 端数据访问层，封装 EF Core 查询 |
| DTO | 数据传输对象 | API 请求/响应载体，分 ListDto、DetailDto、InputDto |
| EF Core | Entity Framework Core | .NET ORM 框架 |
| JWT | JSON Web Token | 认证令牌格式 |
| Mapperly | 映射器框架 | 编译时源生成器，替代 AutoMapper |
| MVVM | Model-View-ViewModel | WPF 桌面端架构模式 |
| Prism | Prism 框架 | WPF MVVM 框架，负责模块注册、导航、依赖注入 |
| QueryFilter | 全局查询过滤器 | EF Core 功能，自动过滤 `IsDeleted=true` 的记录 |

---

## 关键枚举值

### UserRole (用户角色)

| 值 | 英文 | 中文 | 授权策略 |
|----|------|------|---------|
| 0 | Receptionist | 前台接待 | `DoctorOrReceptionist` |
| 1 | Doctor | 医生 | `DoctorOrReceptionist` |
| 10 | Admin | 管理员 | `DoctorOrReceptionist` + `AdminOrSuperAdmin` |
| 100 | SuperAdmin | 超级管理员 | `DoctorOrReceptionist` + `AdminOrSuperAdmin` |

### MedicalCaseStatus (医案状态)

| 值 | 英文 | 中文 | 说明 |
|----|------|------|------|
| 0 | Suspended | 已挂起 | 医生暂时离开，稍后继续 |
| 1 | Active | 进行中 | 正在诊疗 |
| 2 | Completed | 已完成 | 诊疗流程全部完成，锁定编辑 |

> 取消医案统一通过 `IsDeleted=true` 软删除实现（审计类型为 `SoftDelete`），不再使用独立的 Cancelled 状态。

### RegistrationStatus (挂号状态)

| 值 | 英文 | 中文 |
|----|------|------|
| 0 | Waiting | 候诊 |
| 1 | InProgress | 就诊中 |
| 2 | Completed | 已完成 |
| 3 | Cancelled | 已取消 |

### FormulaStatus (验方状态)

| 值 | 英文 | 中文 | 说明 |
|----|------|------|------|
| 0 | Draft | 草稿 | 新建初始状态；药材变更触发降级 |
| 1 | Validated | 已验证 | 全部药材绑定系统药材后自动晋升 |

### AuditOperationType (审计操作类型)

| 值 | 英文 | 中文 |
|----|------|------|
| 1 | Create | 创建 |
| 2 | Update | 更新 |
| 3 | StatusChange | 状态变更 |
| 4 | SoftDelete | 软删除（含取消操作） |

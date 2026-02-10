# 术语表

## 概述

本文档定义凌隐宝堂中医诊所管理系统中使用的核心术语，提供中英文对照和准确释义。所有开发文档、代码注释、用户界面均应遵循本表中的术语规范。

---

## 术语使用铁律

| 规则 | 说明 |
|------|------|
| Consultation = 诊断 | 仅指中医诊断部分 (望闻问切、辨证)，**不是**"问诊"或"就诊" |
| MedicalCase = 医案 | 完整的诊疗记录，**不是**"病历" |
| Formula = 验方/经验方 | 可复用的处方模板 |
| Prescription = 处方 | 具体的药材配伍和剂量，不等于 Formula |

---

## 业务术语

| 英文 | 中文 | 说明 |
|------|------|------|
| Aggregate Root | 聚合根 | DDD 概念。本系统中 MedicalCase 是唯一聚合根 |
| AuthSession | 认证会话 | JWT 登录会话记录 |
| BlacklistedToken | 黑名单令牌 | 被撤销的 JWT 令牌 |
| Consultation | 诊断 | 中医诊断部分: 现病史、舌诊、脉诊、辨证论治。MedicalCase 的内部实体 (1:1) |
| DataSource | 数据源 | Desktop 本地模式的数据访问层 |
| DecocteMethod | 煎法 | 药材的煎煮方式: 默认 (Default)、先煎 (PreDecoct)、后下 (PostDecoct) |
| Formula | 验方/经验方 | 可复用的处方模板，定义药材组成和剂量，不含价格计算 |
| FormulaHerbItem | 验方药材项 | 验方中的单味药材及用量，支持延迟绑定 (HerbId 可空) |
| Herb | 药材 | 中药材，包含名称、分类、产地、价格等信息 |
| MedicalCase | 医案 | 核心聚合根。一次完整的诊疗记录，包含诊断 (Consultation) 和处方 (Prescription) |
| MedicalCaseAuditLog | 医案审计日志 | 记录医案的所有修改历史，含操作人、变更字段、修改原因 |
| Patient | 患者 | 患者基本信息，含个人信息和就诊历史统计 |
| PinYinCode | 拼音码 | 中文姓名的拼音首字母，用于快速检索 |
| Prescription | 处方 | 药材配伍和剂量，MedicalCase 的可选内部实体 (1:0..1) |
| PrescriptionItem | 处方项 | 处方中的单味药材: 药名、剂量、单价、煎法 |
| PrescriptionPrintLog | 处方打印日志 | 记录处方的打印历史 |
| RefreshToken | 刷新令牌 | JWT 刷新令牌，支持令牌轮换和重放攻击检测 |
| SecurityAuditLog | 安全审计日志 | 记录认证相关的安全事件 (登录、登出、令牌撤销等) |
| User | 用户 | 系统用户，角色分为前台接待、医生、管理员、超级管理员 |

---

## 技术术语

| 英文 | 中文 | 说明 |
|------|------|------|
| BaseEntity | 基础实体 | 所有业务实体的基类，含 Id、CreatedAt、UpdatedAt、IsDeleted 等通用字段 |
| Controller | 控制器 | ASP.NET Core Web API 层，处理 HTTP 请求 |
| DDD | 领域驱动设计 | 架构方法论，本系统采用聚合根模式 |
| DTO | 数据传输对象 | API 请求/响应载体，分 ListDto、DetailDto、InputDto |
| EF Core | Entity Framework Core | .NET ORM 框架 |
| JWT | JSON Web Token | 认证令牌格式 |
| Mapperly | 映射器框架 | 编译时源生成器，替代 AutoMapper |
| MVVM | Model-View-ViewModel | WPF 桌面端架构模式 |
| Prism | Prism 框架 | WPF MVVM 框架，负责模块注册、导航、依赖注入 |
| Repository | 仓储 | Server 端数据访问层，封装 EF Core 查询 |
| Service | 服务 | Server 端业务逻辑层 |

---

## 枚举值

### UserRole (用户角色)

| 值 | 英文 | 中文 | 说明 |
|----|------|------|------|
| 0 | Receptionist | 前台接待 | 患者登记、预约管理 |
| 1 | Doctor | 医生 | 日常诊疗、开方、患者管理 |
| 10 | Admin | 管理员 | 系统管理、用户管理、全局数据查看 |
| 100 | SuperAdmin | 超级管理员 | 最高权限，系统初始化专用 |

### CommonStatus (通用状态)

| 值 | 英文 | 中文 |
|----|------|------|
| 0 | Disabled | 禁用 |
| 1 | Enabled | 启用 |

### MedicalCaseStatus (医案状态)

| 值 | 英文 | 中文 | 说明 |
|----|------|------|------|
| 0 | Draft | 暂存 | 用户暂时保存 |
| 1 | Active | 进行中 | 正在诊疗 |
| 2 | Completed | 已完成 | 诊疗流程全部完成，锁定编辑 |
| 3 | Cancelled | 已取消 | 数据保留但标记取消 |

### FormulaType (方剂类型)

| 值 | 英文 | 中文 |
|----|------|------|
| 1 | Classic | 经典方 |
| 2 | Experience | 经验方 |

### DecocteMethod (煎法)

| 值 | 英文 | 中文 |
|----|------|------|
| 0 | Default | 默认煎法 |
| 1 | PreDecoct | 先煎 |
| 2 | PostDecoct | 后下 |

### AuditOperationType (审计操作类型)

| 值 | 英文 | 中文 |
|----|------|------|
| 1 | Create | 创建 |
| 2 | Update | 更新 |
| 3 | StatusChange | 状态变更 |
| 4 | SoftDelete | 软删除 |
| 5 | Cancel | 取消 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 openspec/project.md 和实体代码提取 |

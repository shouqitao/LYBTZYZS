# 文档体系重构 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将 docs/ 从 608 个文件/17 个目录重构为约 35 个高质量文档/6 个目录，以 PRD 为核心。

**Architecture:** 6 层文档体系 (product → requirements → architecture → api-reference → development → operations)，数字前缀排序。每个需求功能包含远程/本地双模式对比。信息源从 openspec/specs/ (48个规范)、openspec/project.md、现有代码逆向工程获取。

**Tech Stack:** Markdown, Mermaid diagrams

**Design Doc:** `docs/plans/2026-02-10-documentation-system-design.md`

---

## 信息源索引

编写文档时，从以下位置提取信息:

| 信息类型 | 来源路径 |
|----------|----------|
| 项目概述/技术栈 | `openspec/project.md` |
| 业务规则 (认证) | `openspec/specs/authentication/spec.md`, `openspec/specs/login-state-machine/spec.md`, `openspec/specs/login-ui/spec.md`, `openspec/specs/login-credential-handling/spec.md` |
| 业务规则 (用户) | `openspec/specs/user-management/spec.md`, `openspec/specs/api-authorization/spec.md` |
| 业务规则 (医案) | `openspec/specs/medicalcase-lifecycle/spec.md`, `openspec/specs/medicalcase-edit-modes/spec.md`, `openspec/specs/medicalcase-ui-layout/spec.md` |
| 业务规则 (验方) | `openspec/specs/formula-copy-flow/spec.md` |
| 业务规则 (药材) | `openspec/specs/herb-card-control/spec.md` |
| 业务规则 (审计) | `openspec/specs/global-audit/spec.md` |
| 业务规则 (打印) | `openspec/specs/printing-infrastructure/spec.md` |
| 架构规范 | `openspec/specs/project-architecture/spec.md`, `openspec/specs/server-layer-architecture/spec.md`, `openspec/specs/client-layer-architecture/spec.md`, `openspec/specs/shared-layer-architecture/spec.md`, `openspec/specs/desktop-architecture/spec.md` |
| 模式规范 | `openspec/specs/repository-patterns/spec.md`, `openspec/specs/service-conventions/spec.md`, `openspec/specs/viewmodel-conventions/spec.md`, `openspec/specs/dto-architecture/spec.md`, `openspec/specs/error-handling/spec.md` |
| 实体模型 | `src/Server/Core/LYBT.Entities/` (所有实体类) |
| API 端点 | `src/Server/Services/LYBT.WebAPI/Controllers/` (所有 Controller) |
| DTO 定义 | `src/Shared/LYBT.Shared.Models/` |
| Desktop 模块 | `src/Client/Desktop/Modules/` |
| 编码规范 | `.claude/rules/code-standards.md`, `.editorconfig` |
| 测试策略 | `docs/reference/how-to/quality/test-layer-strategy.md`, `docs/plans/2026-02-08-test-restructure-plan.md` |

---

## Task 1: 创建目录骨架

**Files:**
- Create: `docs/01-product/` (目录)
- Create: `docs/02-requirements/` (目录)
- Create: `docs/03-architecture/` (目录)
- Create: `docs/03-architecture/decisions/` (目录)
- Create: `docs/04-api-reference/` (目录)
- Create: `docs/05-development/` (目录)
- Create: `docs/06-operations/` (目录)
- Create: `docs/assets/` (目录)

**Step 1: 创建全部目录**

```bash
mkdir -p docs/01-product docs/02-requirements docs/03-architecture/decisions docs/04-api-reference docs/05-development docs/06-operations docs/assets
```

**Step 2: 验证目录存在**

```bash
ls -d docs/01-product docs/02-requirements docs/03-architecture docs/04-api-reference docs/05-development docs/06-operations docs/assets
```

Expected: 全部目录存在。

---

## Task 2: 编写 docs/01-product/README.md (产品概述)

**Files:**
- Create: `docs/01-product/README.md`

**Source:** `openspec/project.md` (项目介绍段落), 项目根 `README.md`

**Content requirements:**
- 产品名称: 凌隐宝堂中医诊所管理系统
- 一段话产品定位
- 核心功能列表 (8个模块一句话概述)
- 双模式特性说明 (远程+本地)
- 目标用户: 中医诊所
- 技术栈摘要 (.NET 8 + WPF + ASP.NET Core + EF Core + SQL Server/SQLite)
- 本目录文档索引表 (链接到 vision.md, glossary.md, user-roles.md)
- 变更记录表

**Step 1:** 读取 `openspec/project.md` 提取项目介绍
**Step 2:** 编写完整文档
**Step 3:** 验证文档符合模板 (有概述、正文、变更记录)

---

## Task 3: 编写 docs/01-product/01-vision.md (产品愿景)

**Files:**
- Create: `docs/01-product/01-vision.md`

**Source:** `openspec/project.md`, 项目 README.md

**Content requirements:**
- 产品愿景: 为中医诊所提供完整的数字化诊疗管理
- 业务目标 (3-5个):
  1. 患者档案电子化管理
  2. 诊疗流程标准化 (望闻问切 → 辨证 → 开方)
  3. 处方和验方的规范化管理
  4. 药材库和价格的统一管理
  5. 支持离线诊疗 (本地模式)
- 核心业务流程图 (Mermaid): 患者登记 → 创建医案 → 诊断 → 处方 → 完成
- 系统边界: 做什么、不做什么
- 变更记录表

**Step 1:** 编写完整文档
**Step 2:** 验证 Mermaid 图表语法正确

---

## Task 4: 编写 docs/01-product/07-glossary.md (术语表)

**Files:**
- Create: `docs/01-product/07-glossary.md`

**Source:** `openspec/project.md` (术语规范段落), 实体模型定义

**Content requirements:**
- 按字母排序的中英文术语对照表
- 必须包含的核心术语:
  | 英文 | 中文 | 说明 |
  |------|------|------|
  | MedicalCase | 医案 | 核心聚合根，完整诊疗记录 |
  | Consultation | 诊断 | 仅指中医诊断部分 (望闻问切、辨证)，不是"问诊" |
  | Prescription | 处方 | 药材配伍和剂量 |
  | PrescriptionItem | 处方项 | 单味药材及用量 |
  | Formula | 验方/经验方 | 可复用的处方模板 |
  | FormulaHerbItem | 验方药材项 | 验方中的单味药材 |
  | Herb | 药材 | 中药材 |
  | Patient | 患者 | 患者基本信息 |
  | User | 用户 | 系统用户 (医生/管理员) |
  | AuthSession | 认证会话 | JWT 登录会话 |
  | DataSource | 数据源 | Desktop 本地数据访问层 |
  | Repository | 仓储 | Server 端数据访问层 |
  | DTO | 数据传输对象 | API 请求/响应载体 |
  | DDD | 领域驱动设计 | 架构方法论 |
  | Aggregate Root | 聚合根 | DDD 概念，本项目中 MedicalCase 是唯一聚合根 |
- 术语使用铁律 (从 project.md 提取):
  - Consultation = 仅指诊断，不是"问诊"
  - MedicalCase = 医案，不是"病历"
  - Formula = 验方/经验方
- 变更记录表

**Step 1:** 编写完整文档
**Step 2:** 验证所有实体名称与代码一致

---

## Task 5: 编写 docs/01-product/04-user-roles.md (用户角色)

**Files:**
- Create: `docs/01-product/04-user-roles.md`

**Source:** `openspec/specs/api-authorization/spec.md`, `openspec/specs/user-management/spec.md`, `src/Server/Core/LYBT.Entities/Users/UserModel.cs` (UserRole 枚举)

**Content requirements:**
- 角色定义表:
  | 角色 | 英文 | 说明 |
  |------|------|------|
  | 超级管理员 | SuperAdmin | 系统初始化专用 |
  | 管理员 | Admin | 系统管理、用户管理、全局数据查看 |
  | 医生 | Doctor | 日常诊疗、开方、患者管理 |
- 每个角色的模块权限矩阵:
  | 模块 | SuperAdmin | Admin | Doctor |
  |------|-----------|-------|--------|
  | 认证 | - | 登录 | 登录 |
  | 用户管理 | CRUD | CRUD | 查看自己 |
  | 患者管理 | - | CRUD全部 | CRUD自己创建 |
  | ... | ... | ... | ... |
- 医案特殊权限规则 (从 medicalcase-edit-modes 提取):
  - Doctor 只能编辑自己的未完成医案
  - Admin 可查看/编辑所有医案
  - 跨日期修改需提供修改原因
- 变更记录表

**Step 1:** 读取权限相关 spec 文件
**Step 2:** 读取 Controller 中的 `[Authorize]` 属性确认实际权限
**Step 3:** 编写完整文档
**Step 4:** 交叉验证文档与代码一致

---

## Task 6: 编写 docs/02-requirements/README.md (需求总览)

**Files:**
- Create: `docs/02-requirements/README.md`

**Content requirements:**
- 概述: 本目录包含系统所有业务模块的功能需求规格
- 模块索引表:
  | 模块 | 文件 | FR 编号范围 | 功能数 |
  |------|------|------------|--------|
  | 认证 | auth.md | FR-AUTH-xxx | TBD |
  | 用户管理 | users.md | FR-USER-xxx | TBD |
  | 患者管理 | patients.md | FR-PAT-xxx | TBD |
  | 药材管理 | herbs.md | FR-HERB-xxx | TBD |
  | 验方管理 | formulas.md | FR-FORM-xxx | TBD |
  | 医案管理 | medical-cases.md | FR-MC-xxx | TBD |
  | 数据同步 | sync.md | FR-SYNC-xxx | TBD |
  | 打印 | printing.md | FR-PRINT-xxx | TBD |
- FR 编号规则说明
- 双模式标注说明 (远程/本地/待讨论)
- 变更记录表

**Step 1:** 编写完整文档 (功能数在各模块文档完成后回填)

---

## Task 7: 编写 docs/02-requirements/02-auth.md (认证需求)

**Files:**
- Create: `docs/02-requirements/02-auth.md`

**Source:** `openspec/specs/authentication/spec.md`, `openspec/specs/login-state-machine/spec.md`, `openspec/specs/login-ui/spec.md`, `openspec/specs/login-credential-handling/spec.md`, `openspec/specs/token-management/spec.md`, `openspec/specs/credential-vault/spec.md`, `openspec/specs/auth-events/spec.md`, `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`

**Content requirements (FR-AUTH-xxx):**
- FR-AUTH-001: 用户登录 (用户名+密码, JWT Token)
- FR-AUTH-002: 自动登录 (AutoLoginToken)
- FR-AUTH-003: Token 刷新 (滑动过期)
- FR-AUTH-004: 用户登出 (本地清理+服务端撤销)
- FR-AUTH-005: 会话超时 (15分钟不活跃, 2分钟警告)
- FR-AUTH-006: Token 验证
- FR-AUTH-007: 凭证保存 (DPAPI 加密)
- FR-AUTH-008: 登录状态机 (Idle→Validating→Active→Refreshing→Expired)
- 每个 FR 都要有远程/本地模式对比
- 数据模型: AuthSession 实体
- 配置参数表 (InactivityTimeout, WarningBefore 等)

**Step 1:** 读取全部认证相关 spec 文件
**Step 2:** 读取 AuthController 确认端点
**Step 3:** 编写完整需求文档
**Step 4:** 验证 FR 编号连续、无遗漏

---

## Task 8: 编写 docs/02-requirements/03-users.md (用户管理需求)

**Files:**
- Create: `docs/02-requirements/03-users.md`

**Source:** `openspec/specs/user-management/spec.md`, `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`, `src/Server/Core/LYBT.Entities/Users/UserModel.cs`

**Content requirements (FR-USER-xxx):**
- FR-USER-001: 创建用户
- FR-USER-002: 查看用户列表 (分页、筛选)
- FR-USER-003: 查看用户详情
- FR-USER-004: 更新用户信息
- FR-USER-005: 删除用户 (软删除)
- FR-USER-006: 恢复已删除用户
- FR-USER-007: 批量删除
- FR-USER-008: 重置密码
- FR-USER-009: 修改密码
- FR-USER-010: 修改个人信息
- FR-USER-011: 启用/禁用用户
- FR-USER-012: 导入用户 (Excel)
- 数据模型: User 实体全部字段
- 权限矩阵: Admin vs Doctor

**Step 1-4:** 同 Task 7 模式

---

## Task 9: 编写 docs/02-requirements/04-patients.md (患者管理需求)

**Files:**
- Create: `docs/02-requirements/04-patients.md`

**Source:** `src/Server/Services/LYBT.WebAPI/Controllers/PatientsController.cs`, `src/Server/Core/LYBT.Entities/Patients/PatientModel.cs`

**Content requirements (FR-PAT-xxx):**
- FR-PAT-001 ~ 012: CRUD、搜索(拼音码)、软删除、恢复、批量删除、导入/导出(Excel)、模板下载、年龄自动计算、身份证读卡
- 数据模型: Patient 实体 (含敏感数据标记)
- 所有权规则: Doctor 编辑自己创建的，Admin 全部

---

## Task 10: 编写 docs/02-requirements/05-herbs.md (药材管理需求)

**Files:**
- Create: `docs/02-requirements/05-herbs.md`

**Source:** `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`, `src/Server/Core/LYBT.Entities/Herbs/HerbModel.cs`, `openspec/specs/herb-card-control/spec.md`

**Content requirements (FR-HERB-xxx):**
- CRUD、分类筛选、启用/禁用、导入/导出、批量引用检查、价格管理
- 数据模型: Herb 实体

---

## Task 11: 编写 docs/02-requirements/06-formulas.md (验方管理需求)

**Files:**
- Create: `docs/02-requirements/06-formulas.md`

**Source:** `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`, `src/Server/Core/LYBT.Entities/Formulas/`, `openspec/specs/formula-copy-flow/spec.md`

**Content requirements (FR-FORM-xxx):**
- CRUD、药材组成管理、启用/禁用、复制验方、延迟绑定(HerbId 可空)、共享验方、导入/导出
- 数据模型: Formula + FormulaHerbItem

---

## Task 12: 编写 docs/02-requirements/07-medical-cases.md (医案管理需求 -- 核心)

**Files:**
- Create: `docs/02-requirements/07-medical-cases.md`

**Source:** `openspec/specs/medicalcase-lifecycle/spec.md`, `openspec/specs/medicalcase-edit-modes/spec.md`, `openspec/specs/medicalcase-ui-layout/spec.md`, `openspec/specs/global-audit/spec.md`, `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`, `src/Server/Core/LYBT.Entities/MedicalCases/`, `src/Server/Core/LYBT.Entities/Consultations/`, `src/Server/Core/LYBT.Entities/Prescriptions/`

**Content requirements (FR-MC-xxx):** 这是最大最复杂的需求文档。
- FR-MC-001: 创建医案 (聚合根，含 Patient 关联)
- FR-MC-002: 填写诊断 (Consultation: 现病史、舌诊、脉诊、中医辨证)
- FR-MC-003: 标记处方需求 (NeedsPrescription flag)
- FR-MC-004: 开具处方 (Prescription + Items)
- FR-MC-005: 保存医案 (聚合根整体保存)
- FR-MC-006: 暂存草稿 (Draft)
- FR-MC-007: 完成医案 (Completed, 锁定)
- FR-MC-008: 取消医案 (Cancelled, 数据保留)
- FR-MC-009: 医案列表查询 (分页、按患者、待诊、未完成、最近)
- FR-MC-010: 跨医案搜索 (患者名/诊断关键词)
- FR-MC-011: 编辑模式 (Clinical/Management 工作区, Editing/ReadOnly 状态)
- FR-MC-012: 审计日志 (操作人/时间/内容/原因)
- FR-MC-013: 权限控制 (Doctor只编辑自己，Admin全部)
- FR-MC-014: 锁定规则 (完成且非当天 = 不可编辑)
- FR-MC-015: 处方打印 (PrintVersion管理)
- FR-MC-016: 验方导入到处方
- FR-MC-017: 待诊队列
- 状态机图 (Mermaid): Draft ↔ Active → Completed / Cancelled
- 数据模型: MedicalCase + Consultation + Prescription + PrescriptionItem
- 聚合根边界说明

---

## Task 13: 编写 docs/02-requirements/10-sync.md (数据同步需求)

**Files:**
- Create: `docs/02-requirements/10-sync.md`

**Source:** `src/Server/Services/LYBT.WebAPI/Controllers/SyncController.cs`, `src/Client/Desktop/Modules/LYBT.Desktop.Sync/`, `src/Client/Desktop/Core/LYBT.Desktop.LocalData/`

**Content requirements (FR-SYNC-xxx):**
- FR-SYNC-001: 获取可同步实体类型
- FR-SYNC-002: 获取同步元数据
- FR-SYNC-003: 数据比对 (Compare)
- FR-SYNC-004: 上传本地变更 (Upload)
- FR-SYNC-005: 下载服务端变更 (Download)
- FR-SYNC-006: 删除同步 (Delete)
- FR-SYNC-007: 完整同步工作流
- FR-SYNC-008: 模式切换 (手动触发)
- 待讨论项: 冲突解决策略、本地模式功能受限范围

---

## Task 14: 编写 docs/02-requirements/09-printing.md (打印需求)

**Files:**
- Create: `docs/02-requirements/09-printing.md`

**Source:** `openspec/specs/printing-infrastructure/spec.md`, `src/Client/Desktop/Core/LYBT.Desktop.Printing/`

**Content requirements (FR-PRINT-xxx):**
- FR-PRINT-001: 处方打印 (A5 模板)
- FR-PRINT-002: 打印版本管理
- FR-PRINT-003: 打印日志
- FR-PRINT-004: 打印预览
- 数据模型: PrescriptionPrintLog

---

## Task 15: 回填 02-requirements/README.md 功能数

**Files:**
- Modify: `docs/02-requirements/README.md`

**Step 1:** 统计各模块 FR 编号数量
**Step 2:** 回填 README.md 索引表中的"功能数"列

---

## Task 16: 编写 docs/03-architecture/README.md (架构总览)

**Files:**
- Create: `docs/03-architecture/README.md`

**Content requirements:**
- 架构概述 (三层: Server/Shared/Client)
- 本目录文档索引
- 技术栈版本表
- 变更记录

---

## Task 17: 编写 docs/03-architecture/01-system-overview.md (系统架构图)

**Files:**
- Create: `docs/03-architecture/01-system-overview.md`

**Source:** `openspec/project.md`, `openspec/specs/project-architecture/spec.md`

**Content requirements:**
- 系统整体架构图 (Mermaid C4 风格)
- 解决方案结构图 (src/ 完整树)
- 依赖方向图
- 模块通信图

---

## Task 18: 编写 docs/03-architecture/03-server.md (服务端架构)

**Files:**
- Create: `docs/03-architecture/03-server.md`

**Source:** `openspec/specs/server-layer-architecture/spec.md`, `openspec/specs/repository-patterns/spec.md`, `openspec/specs/service-conventions/spec.md`, `openspec/specs/error-handling/spec.md`

**Content requirements:**
- 三层架构: Controller → Service → Repository → DbContext
- 模块列表和职责
- 依赖注入规范
- 异常处理链 (IExceptionHandler)
- 错误码体系

---

## Task 19: 编写 docs/03-architecture/02-desktop.md (桌面端架构)

**Files:**
- Create: `docs/03-architecture/02-desktop.md`

**Source:** `openspec/specs/client-layer-architecture/spec.md`, `openspec/specs/desktop-architecture/spec.md`, `openspec/specs/viewmodel-conventions/spec.md`

**Content requirements:**
- MVVM + Prism 架构
- 模块注册和导航
- ViewModel 基类层次
- Components 分层模式
- 角色工作台 (Admin/Clinical)

---

## Task 20: 编写 docs/03-architecture/08-shared.md (共享层架构)

**Files:**
- Create: `docs/03-architecture/08-shared.md`

**Source:** `openspec/specs/shared-layer-architecture/spec.md`, `openspec/specs/dto-architecture/spec.md`

**Content requirements:**
- Shared 层职责
- DTO 继承层次
- Models/Utilities/Components 分工

---

## Task 21: 编写 docs/03-architecture/05-dual-mode.md (双模式架构)

**Files:**
- Create: `docs/03-architecture/05-dual-mode.md`

**Source:** `src/Client/Desktop/Core/LYBT.Desktop.LocalData/`, `src/Client/Desktop/Modules/LYBT.Desktop.Sync/`

**Content requirements:**
- 远程模式 vs 本地模式架构图 (Mermaid)
- 数据链路对比
- 认证差异
- 模式切换机制 (手动触发)
- 同步架构
- 待讨论: 功能受限范围、冲突解决

---

## Task 22: 编写 docs/03-architecture/04-data-model.md (数据模型)

**Files:**
- Create: `docs/03-architecture/04-data-model.md`

**Source:** `src/Server/Core/LYBT.Entities/` (所有实体类)

**Content requirements:**
- ER 图 (Mermaid)
- 每个实体的完整字段表
- 聚合根边界标注
- 通用基类 (BaseEntity)
- 枚举定义
- 数据库约定 (命名、软删除、审计)

---

## Task 23: 提取 ADR 到 docs/03-architecture/decisions/

**Files:**
- Create: `docs/03-architecture/decisions/0001-integration-first-testing.md`
- Create: `docs/03-architecture/decisions/0002-dual-mode-architecture.md`
- Create: `docs/03-architecture/decisions/0003-medicalcase-aggregate-root.md`
- Create: 其他 ADR (从 `docs/state/architecture/decisions/` 提取)

**Source:** `docs/state/architecture/decisions/`, brainstorm 中的决策记录

**Step 1:** 读取 `docs/state/architecture/decisions/` 全部文件
**Step 2:** 筛选仍然有效的 ADR
**Step 3:** 按统一模板重写

---

## Task 24: 编写 docs/04-api-reference/README.md (API 总览)

**Files:**
- Create: `docs/04-api-reference/README.md`

**Content requirements:**
- API 基本信息 (Base URL, 版本, 认证方式)
- 通用响应格式 (成功/失败 JSON)
- 分页参数规范
- 模块端点索引表
- 错误码速查表

---

## Task 25-30: 编写各模块 API 参考 (6个文件)

**Files:**
- Create: `docs/04-api-reference/01-auth.md`
- Create: `docs/04-api-reference/02-users.md`
- Create: `docs/04-api-reference/03-patients.md`
- Create: `docs/04-api-reference/04-herbs.md`
- Create: `docs/04-api-reference/05-formulas.md`
- Create: `docs/04-api-reference/06-medical-cases.md`
- Create: `docs/04-api-reference/09-sync.md`

**Source:** 对应 Controller 文件 + DTO 定义

**每个文件包含:**
- 每个端点: HTTP 方法、路径、权限、请求体(JSON)、响应体(JSON)、错误码
- 从 Controller 代码逆向工程，确保 100% 准确

**可并行执行:** 6 个文件互相独立

---

## Task 31: 编写 docs/05-development/README.md (快速开始)

**Files:**
- Create: `docs/05-development/README.md`

**Content:** 新开发者上手指南 (克隆→安装→运行→验证)

---

## Task 32: 编写 docs/05-development/01-setup.md (环境搭建)

**Files:**
- Create: `docs/05-development/01-setup.md`

**Source:** `global.json`, `Directory.Packages.props`, `nuget.config`

**Content:** .NET SDK 版本、SQL Server 配置、IDE 设置

---

## Task 33: 编写 docs/05-development/03-code-standards.md (编码规范)

**Files:**
- Create: `docs/05-development/03-code-standards.md`

**Source:** `.claude/rules/code-standards.md`, `.editorconfig`, `openspec/project.md` (命名规范段)

---

## Task 34: 编写 docs/05-development/04-patterns.md (设计模式速查)

**Files:**
- Create: `docs/05-development/04-patterns.md`

**Source:** `openspec/specs/repository-patterns/spec.md`, `openspec/specs/service-conventions/spec.md`, `openspec/specs/viewmodel-conventions/spec.md`, `openspec/specs/dialog-patterns/spec.md`

---

## Task 35: 编写 docs/05-development/05-testing.md (测试指南)

**Files:**
- Create: `docs/05-development/05-testing.md`

**Source:** `docs/plans/2026-02-08-test-restructure-plan.md`, `docs/reference/how-to/quality/test-layer-strategy.md`

**Content:** 5 个测试项目、集成优先策略、如何写/跑测试

---

## Task 36: 编写 docs/06-operations/ (运维文档, 3个文件)

**Files:**
- Create: `docs/06-operations/README.md`
- Create: `docs/06-operations/01-deployment.md`
- Create: `docs/06-operations/02-configuration.md`

**Source:** `src/Server/Services/LYBT.WebAPI/appsettings.json`, `src/Server/Services/LYBT.WebAPI/Program.cs`

---

## Task 37: 编写 docs/README.md (文档导航入口)

**Files:**
- Create: `docs/README.md` (替换旧版)

**Content:** 简洁的文档体系导航，链接到 6 个目录的 README

---

## Task 38: 清理旧文档目录

**Files:**
- Delete: `docs/process/` (216 files)
- Delete: `docs/state/` (131 files)
- Delete: `docs/reference/` (142 files)
- Delete: `docs/guides/` (4 files)
- Delete: `docs/how-to-guides/` (11 files)
- Delete: `docs/tutorials/` (9 files)
- Delete: `docs/explanation/` (19 files)
- Delete: `docs/reports/` (16 files)
- Delete: `docs/audits/` (1 file)
- Delete: `docs/reflections/` (1 file)
- Delete: `docs/meta/` (5 files)
- Delete: `docs/support/` (非运行时文件)
- Delete: `docs/tasks/` (2 files)
- Delete: `docs/testing/` (2 files)
- Delete: `docs/development/` (3 files)
- Delete: `docs/architecture/` (6 files, 已迁移到 03-architecture/)
- Delete: `docs/mapperly-warning-fix-plan.md`

**Step 1:** 确认新文档全部就位
**Step 2:** 执行删除
**Step 3:** 验证只剩新体系 6 个目录 + assets/ + plans/ + README.md

**注意:** `docs/plans/` 保留 (包含设计文档和本计划)

---

## Task 39: 精简项目根 README.md

**Files:**
- Modify: `README.md` (项目根)

**Content:** 精简为项目简介 (3-5段) + "详细文档见 docs/" 链接

---

## Task 40: 更新 CLAUDE.md 引用路径

**Files:**
- Modify: `CLAUDE.md` (项目根)

**Content:** 将 openspec 相关引用更新为 docs/ 新路径

---

## Task 依赖关系

```
Task 1 (目录骨架)
  ├→ Task 2-5 (01-product/, 可并行)
  │    └→ Task 6-14 (02-requirements/, 可并行)
  │         ├→ Task 15 (回填 README 功能数)
  │         └→ Task 16-23 (03-architecture/, 可并行)
  │              └→ Task 24-30 (04-api-reference/, 可并行)
  │                   └→ Task 31-36 (05-development/ + 06-operations/, 可并行)
  │                        └→ Task 37 (docs/README.md)
  │                             └→ Task 38 (清理旧文档)
  │                                  └→ Task 39-40 (更新根目录文件)
```

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始计划，40 个 Task |

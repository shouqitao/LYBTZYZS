# User Story Map

## Purpose
用户故事地图是产品功能的全局导航图，将 21 个模块的 User Stories 按业务流程和优先级组织。本文档不是独立 PRD，而是各模块 PRD 中 User Stories 的汇总视图。

## 目标用户
- 产品负责人: 验证功能覆盖完整性
- 开发团队: 理解功能间依赖和优先级
- 测试团队: 规划测试策略和覆盖率

## 成功标准
1) 所有 138 个 User Stories 均已映射到模块和优先级
2) 每个核心诊疗流程(挂号→看诊→开方→打印)至少有一条 Must Have 故事
3) 无孤立故事 (每条故事都关联到模块 PRD)

## Out of Scope
- 详细验收标准: 见各模块 PRD 的 User Story 章节
- 技术实现细节: 见 03-architecture/
- API 契约: 见 04-api-reference/

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **框架**: Jeff Patton User Story Mapping
> **数据来源**: clinical-workflow.md + JTBD (10 个) + 138 US (15 个模块)

---

## 阅读指南

**横轴 (Backbone)**: Activities 代表用户完成目标的主要活动，从左到右按时间顺序排列。
**纵轴 (Priority)**: 每个 Activity 下的 Tasks 按 MoSCoW 优先级从上到下排列 (Must > Should > Could)。
**Release Slices**: 横向切线划分发布版本，Must Have 构成当前版本核心功能。

---

## Narrative 1: 首诊完整流程

**Persona**: 李医生 (Doctor) + 小张 (Receptionist)
**Goal**: 完成一位新患者的首次就诊，从登记到处方打印
**JTBD**: JTBD-D01 (复诊患者识别), JTBD-R01 (快速登记), JTBD-D02 (高效开方)
**对应流程**: clinical-workflow.md 阶段 1a/1b + 阶段 2 + 阶段 3 + 阶段 4

### Activities (Backbone)

```
[患者登记] -> [创建医案] -> [四诊合参] -> [处方开具] -> [保存与打印]
```

### Activity 1: 患者登记

**Steps:**
1. 搜索已有患者 (US-PAT-002)
2. 刷身份证读卡 (US-CARD-001)
3. 创建新患者 (US-PAT-001)
4. 患者自动匹配 (US-CARD-002)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 创建患者基本信息 (US-PAT-001) | 身份证读卡自动填充 (US-CARD-001) | 批量导入患者 (US-PAT-008) |
| 姓名/拼音码搜索 (US-PAT-002) | 已有患者自动匹配 (US-CARD-002) | 下载导入模板 (US-PAT-009) |
| 查看患者详情 (US-PAT-003) | 患者状态管理 (US-PAT-013) | 导出患者数据 (US-PAT-010) |
| 更新患者信息 (US-PAT-004) | | 检查患者引用 (US-PAT-011) |
| | | 批量检查引用 (US-PAT-012) |

### Activity 1.5: 挂号分流

**Steps:**
1. 前台创建挂号 (US-REG-001)
2. 医生从队列选择 (US-REG-003)
3. 或医生直接看诊 (US-REG-002)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 前台创建挂号 (US-REG-001) | 挂号历史查询 (US-REG-007) | |
| 医生快速看诊 (US-REG-002) | | |
| 查看挂号队列 (US-REG-003) | | |
| 前台取消挂号 (US-REG-004) | | |
| 状态自动跟随医案完成 (US-REG-005) | | |
| 医案取消联动 (US-REG-006) | | |

### Activity 2: 创建医案

**Steps:**
1. 选择患者 (US-PAT-002/003)
2. 创建医案 (US-MC-001)
3. BR-001 碰撞检查 (单活跃医案约束)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 创建医案 (US-MC-001) | 待诊队列 (US-MC-017) | |
| 权限控制 (US-MC-013) | | |

### Activity 3: 四诊合参

**Steps:**
1. 填写中医辨证 (US-MC-002)
2. 标记处方需求 (US-MC-003)
3. 可选: 挂起医案 (US-MC-006)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 填写诊断 (US-MC-002) | 编辑模式切换 (US-MC-011) | |
| 标记处方需求 (US-MC-003) | | |
| 挂起医案 (US-MC-006) | | |

### Activity 4: 处方开具

**Steps:**
1. 开具处方 -- 手工添加药材 (US-MC-004)
2. 可选: 导入验方到处方 (US-MC-016)
3. 费用自动计算

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 开具处方/添加药材 (US-MC-004) | 验方导入到处方 (US-MC-016) | |
| 查看药材列表 (US-HERB-002) | | |

### Activity 5: 保存与打印

**Steps:**
1. 聚合保存 (US-MC-005)
2. 打印预览 (US-PRINT-002)
3. 确认打印 (US-PRINT-001)
4. 完成医案 (US-MC-007)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 聚合保存 (US-MC-005) | 打印预览 (US-PRINT-002) | 打印日志 (US-PRINT-004) |
| 完成医案 (US-MC-007) | 处方打印 (US-PRINT-001) | |
| | 打印版本管理 (US-PRINT-003) | |

---

## Narrative 2: 复诊流程

**Persona**: 李医生 (Doctor)
**Goal**: 高效完成复诊，复用历史处方并微调
**JTBD**: JTBD-D01 (复诊患者识别), JTBD-D03 (复诊处方延续)
**对应流程**: clinical-workflow.md 阶段 1b + 阶段 2 + 阶段 3 (历史处方复制) + 阶段 4

### Activities (Backbone)

```
[患者识别] -> [历史回顾] -> [本次诊断] -> [处方延续] -> [保存与打印]
```

### Activity 1: 患者识别

**Steps:**
1. 搜索已有患者 (US-PAT-002)
2. 查看患者详情确认身份 (US-PAT-003)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 姓名/拼音码搜索 (US-PAT-002) | 身份证读卡快速识别 (US-CARD-001) | |
| 查看患者详情 (US-PAT-003) | 已有患者自动匹配 (US-CARD-002) | |

### Activity 2: 历史回顾

**Steps:**
1. 查看患者历史医案列表 (US-MC-009)
2. 跨医案搜索 (US-MC-010)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 医案列表查询 (US-MC-009) | 跨医案搜索 (US-MC-010) | |

### Activity 3: 本次诊断

**Steps:**
1. 创建新医案 (US-MC-001)
2. 填写诊断 (US-MC-002)
3. 标记处方需求 (US-MC-003)

**Tasks (纵向优先级):**

与 Narrative 1 Activity 2+3 相同，复用 US-MC-001/002/003/006。

### Activity 4: 处方延续

**Steps:**
1. 复制历史处方 (US-MC-018)
2. 微调药材和剂量 (US-MC-004)
3. 可选: 补充导入验方 (US-MC-016)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 开具处方/编辑药材 (US-MC-004) | 复制历史处方 (US-MC-018) | |
| | 验方导入到处方 (US-MC-016) | |

### Activity 5: 保存与打印

与 Narrative 1 Activity 5 相同，复用 US-MC-005/007, US-PRINT-001~004。

---

## Narrative 3: 药材与验方管理

**Persona**: 王主任 (Admin) + 李医生 (Doctor)
**Goal**: 维护药材库和积累经验方
**JTBD**: JTBD-A01 (药材库初始化), JTBD-A02 (药材价格更新), JTBD-D05 (经验方积累)
**对应流程**: clinical-workflow.md Section 五 Admin 日常操作

### Activities (Backbone)

```
[药材维护] -> [验方创建] -> [验方验证] -> [验方使用]
```

### Activity 1: 药材维护

**Steps:**
1. 查看药材列表 (US-HERB-002)
2. 创建/编辑药材 (US-HERB-001, US-HERB-004)
3. 批量导入药材 (US-HERB-009, US-HERB-010)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 创建药材 (US-HERB-001) | 启用/禁用药材 (US-HERB-006) | 恢复已删除药材 (US-HERB-007) |
| 查看药材列表 (US-HERB-002) | Excel 导入 (US-HERB-009) | JSON 批量导入 (US-HERB-010) |
| 查看药材详情 (US-HERB-003) | 导出药材数据 (US-HERB-011) | 下载导入模板 (US-HERB-012) |
| 更新药材信息 (US-HERB-004) | 批量删除 (US-HERB-008) | 检查药材引用 (US-HERB-013) |
| 删除药材 (US-HERB-005) | | |

### Activity 2: 验方创建

**Steps:**
1. 创建验方 (US-FORM-001)
2. 添加药材组成和剂量
3. 设置分类和适应证

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 创建验方 (US-FORM-001) | 共享验方 (US-FORM-008) | 批量导入验方 (US-FORM-011) |
| 查看验方列表 (US-FORM-002) | 导出验方 (US-FORM-012) | 下载导入模板 (US-FORM-013) |
| 查看验方详情 (US-FORM-003) | | |
| 更新验方 (US-FORM-004) | | |
| 删除验方 (US-FORM-005) | | |

### Activity 3: 验方验证

**Steps:**
1. 延迟绑定 -- 药材名匹配 (US-FORM-009)
2. 获取待验证验方列表 (US-FORM-010)
3. 启用/禁用验方 (US-FORM-006)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 启用/禁用验方 (US-FORM-006) | 延迟绑定 (US-FORM-009) | 恢复已删除验方 (US-FORM-007) |
| | 获取待验证验方 (US-FORM-010) | |

### Activity 4: 验方使用

验方在诊疗流程中的使用已在 Narrative 1 Activity 4 和 Narrative 2 Activity 4 中覆盖 (US-MC-016 验方导入到处方)。

---

## Narrative 4: 系统管理与运维

**Persona**: 王主任 (Admin)
**Goal**: 管理用户账号、数据同步和系统配置
**JTBD**: JTBD-A03 (用户生命周期管理), JTBD-D04 (离线诊疗/数据同步)

### Activities (Backbone)

```
[用户管理] -> [数据同步] -> [系统配置]
```

### Activity 1: 用户管理

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 创建用户 (US-USER-001) | 启用/禁用用户 (US-USER-011) | 恢复已删除用户 (US-USER-006) |
| 查看用户列表 (US-USER-002) | 管理员重置密码 (US-USER-008) | 批量删除 (US-USER-007) |
| 查看用户详情 (US-USER-003) | 用户修改密码 (US-USER-009) | |
| 更新用户信息 (US-USER-004) | 修改个人资料 (US-USER-010) | |
| 删除用户 (US-USER-005) | 获取当前用户 (US-USER-012) | |

### Activity 2: 数据同步

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 模式切换 (US-SYNC-008) | 获取可同步实体 (US-SYNC-001) | |
| | 获取同步元数据 (US-SYNC-002) | |
| | 数据比对 (US-SYNC-003) | |
| | 上传本地变更 (US-SYNC-004) | |
| | 下载服务端变更 (US-SYNC-005) | |
| | 同步删除 (US-SYNC-006) | |
| | 完整同步工作流 (US-SYNC-007) | |

### Activity 3: 系统配置

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 服务端配置参数 (US-CFG-001) | 环境配置管理 (US-CFG-003) | |
| 客户端配置参数 (US-CFG-002) | 生产环境启动验证 (US-CFG-004) | |

---

## Release Slices

### Release 1: 当前版本 (Must Have) -- v1.0 核心

系统无此功能则不可用的最小功能集。

**患者管理**: US-PAT-001, 002, 003, 004 (4)
**医案管理**: US-MC-001, 002, 003, 004, 005, 006, 007, 009, 013 (9)
**药材管理**: US-HERB-001, 002, 003, 004, 005 (5)
**验方管理**: US-FORM-001, 002, 003, 004, 005, 006 (6)
**用户管理**: US-USER-001, 002, 003, 004, 005 (5)
**认证**: US-AUTH-001, 002, 003, 005, 008, 009, 010, 012 (8)
**Desktop Shell**: US-SHELL-001, 002, 003, 004, 005 (5)
**配置**: US-CFG-001, 002 (2)
**挂号管理**: US-REG-001, 002, 003, 004, 005, 006 (6)
**数据同步**: US-SYNC-008 (1)

**Must Have 合计: 51 US**

### Release 2: 效率增强 (Should Have) -- v1.0 完整版

显著提升诊疗效率但非阻断。

**患者管理**: US-PAT-005, 013 (2)
**医案管理**: US-MC-008, 010, 011, 014, 015, 016, 017, 018 (8)
**打印**: US-PRINT-001, 002, 003 (3)
**药材管理**: US-HERB-006, 008, 009, 011 (4)
**验方管理**: US-FORM-008, 009, 010, 012 (4)
**身份证读卡**: US-CARD-001, 002 (2)
**用户管理**: US-USER-008, 009, 010, 011, 012 (5)
**挂号管理**: US-REG-007 (1)
**认证**: US-AUTH-004, 006, 011, 013 (4) _(US-AUTH-007 Removed: 设计决策已移除登出前警告)_
**数据同步**: US-SYNC-001, 002, 003, 004, 005, 006, 007 (7)
**异常处理**: US-ERR-001, 002, 003, 004, 005, 006 (6)
**日志**: US-LOG-001, 002, 003, 007 (4)
**Desktop Shell**: US-SHELL-006 (1)
**配置**: US-CFG-003, 004 (2)

**Should Have 合计: 54 US**

### Release 3: 锦上添花 (Could Have) -- 后续版本

**患者管理**: US-PAT-006, 007, 008, 009, 010, 011, 012 (7)
**医案管理**: US-MC-012 (1)
**打印**: US-PRINT-004 (1)
**药材管理**: US-HERB-007, 010, 012, 013 (4)
**验方管理**: US-FORM-007, 011, 013 (3)
**用户管理**: US-USER-006, 007 (2)
**认证**: US-AUTH-003 (Token刷新已在Must中覆盖基础能力) (0)
**异常处理**: US-ERR-007, 008 (2)
**日志**: US-LOG-004, 005, 006 (3)
**健康诊断**: US-SYS-001, 002, 003, 004, 005, 006, 007, 008, 009 (9)
**Desktop Shell**: US-SHELL-007 (1)

**Could Have 合计: 33 US**

### 分布统计

| 优先级 | US 数量 | 占比 |
|--------|---------|------|
| Must Have | 51 | 37.0% |
| Should Have | 54 | 39.1% |
| Could Have | 33 | 23.9% |
| **合计** | **138** | **100%** |

---

## Gap 分析

### 未映射到故事地图的 US

以下 US 未出现在 Narrative 1-4 的 Activity 分解中，但已在 Release Slices 中分配优先级:

| US 编号 | 模块 | 说明 | 原因 |
|---------|------|------|------|
| US-AUTH-001~013 | 认证 | JWT 登录/登出/Token 管理 | **基础设施**: 认证是所有操作的前提，非特定 Narrative |
| US-ERR-001~008 | 异常处理 | 全局异常处理/ProblemDetails | **基础设施**: 跨模块异常处理机制 |
| US-LOG-001~007 | 日志 | 结构化日志/审计/脱敏 | **基础设施**: 跨模块日志记录 |
| US-SYS-001~009 | 健康诊断 | 健康检查/调试模式 | **运维工具**: 非业务流程 |
| US-SHELL-001~007 | Desktop Shell | 启动/导航/菜单 | **应用框架**: 承载业务模块的外壳 |
| US-CFG-001~004 | 配置 | 配置参数管理 | **部署基础**: 系统配置 |
| US-MC-012 | 医案 | 审计日志 | **合规需求**: 隐含在保存流程中 |
| US-PAT-005~012 | 患者 | 删除/恢复/批量/导入导出 | **数据管理**: 非核心诊疗流程 |

### 故事地图中无 US 支撑的 Task

未发现需求盲区。所有故事地图中的 Task 均有对应 US 支撑。

### JTBD 覆盖度交叉验证

| JTBD | 故事地图覆盖 | 对应 Narrative |
|------|-------------|----------------|
| JTBD-D01 复诊患者识别 | N1-A1 + N2-A1/A2 | Narrative 1 + 2 |
| JTBD-D02 高效开方 | N1-A4 | Narrative 1 |
| JTBD-D03 复诊处方延续 | N2-A4 | Narrative 2 |
| JTBD-D04 离线诊疗 | N4-A2 | Narrative 4 |
| JTBD-D05 经验方积累 | N3-A2/A3 | Narrative 3 |
| JTBD-A01 药材库初始化 | N3-A1 | Narrative 3 |
| JTBD-A02 药材价格更新 | N3-A1 | Narrative 3 |
| JTBD-A03 用户生命周期管理 | N4-A1 | Narrative 4 |
| JTBD-R01 快速登记 | N1-A1 | Narrative 1 |
| JTBD-R02 老患者识别 | N2-A1 | Narrative 2 |

10/10 JTBD 在故事地图中均有对应 Narrative 和 Activity 覆盖。

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始版本: 4 Narrative + Release Slices + Gap 分析 |
| 2026-03-06 | v1.1 | Registration 集成: Narrative 1 新增 Activity 1.5 (挂号分流, 7 US); Release Slices 新增 REG (Must 6 + Should 1); US-AUTH-007 标记 Removed; 分布统计 131->138 (51/54/33) |

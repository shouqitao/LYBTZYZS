# 文档-代码对齐补全 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 补全代码已有但文档缺失的模块 (CardReader/Health/Diagnostics)，完善 desktop.md 组件层文档，拆分运维文档并清理残留文件。使文档体系 100% 覆盖当前已实现的所有功能。

**Architecture:** 纯文档任务，无代码变更。新增 5 个文档文件，修改 4 个现有文档，删除 1 个残留文件。所有内容基于代码逆向分析，遵循项目现有文档模板格式。

**Tech Stack:** Markdown, Mermaid diagrams

**Design Doc:** `docs/plans/2026-02-10-doc-code-alignment-design.md`

**Scope Adjustment:** EntityAudit 审计模块为后续独立开发功能，本次不补文档。

---

## Task 概览

| Task | 文件操作 | 层 | 可并行 |
|------|---------|-----|--------|
| 1 | 创建 card-reader.md 需求 | 02-requirements | 与 Task 2-3 并行 |
| 2 | 创建 health.md API参考 | 04-api-reference | 与 Task 1,3 并行 |
| 3 | 创建 diagnostics.md API参考 | 04-api-reference | 与 Task 1,2 并行 |
| 4 | desktop.md 新增 Controls/Dialogs/CardReader | 03-architecture | 依赖 Task 1 |
| 5 | 拆分 06-operations/ 三文件 | 06-operations | 独立 |
| 6 | 更新 README 索引 (2个文件) | 02/04 | 依赖 Task 1-3 |
| 7 | 删除残留 + 全量验证 | 清理 | 依赖全部 |

---

## Task 1: 创建 docs/02-requirements/16-card-reader.md

**Files:**
- Create: `docs/02-requirements/16-card-reader.md`

**Source:** `src/Client/Desktop/Core/LYBT.Desktop.CardReader/` (ICardReader, IPatientCardReaderIntegration)

**Step 1: 创建需求文档**

遵循项目需求文档模板 (参考 auth.md 格式)，内容基于代码逆向分析:

```markdown
# 身份证读卡器集成 需求规格

## 概述

身份证读卡器模块提供硬件设备集成能力，支持通过二代身份证读卡器快速读取患者身份信息并自动填充到患者表单，提升挂号登记效率。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| Admin | 使用读卡器读取患者信息 |
| Doctor | 使用读卡器读取患者信息 |

> 读卡器功能仅在 Desktop 端可用，无服务端 API。

---

## 功能清单

### FR-CARD-001: 身份证读卡器连接与读取

- **描述**: 连接身份证读卡器设备，读取二代身份证芯片信息
- **业务规则**:
  1. 支持多厂商读卡器 (策略模式，ICardReader 接口)
  2. 自动检测读卡器连接状态 (IsConnected)
  3. 支持主动探测卡片 (DetectCardAsync)
  4. 读取信息包含: 姓名、性别、民族、出生日期、身份证号、住址
  5. 可选保存证件照片 (savePhoto 参数)
  6. 连接状态变更通过 ConnectionStateChanged 事件通知
  7. 卡片插入通过 CardDetected 事件通知
- **远程模式**: 不适用 (纯客户端硬件交互)
- **本地模式**: 不适用 (纯客户端硬件交互)
- **验收标准**:
  - [ ] 读卡器连接成功后 IsConnected 为 true
  - [ ] 读取身份证返回完整 CardReadResult
  - [ ] 读卡器断开时 ConnectionStateChanged 触发

### FR-CARD-002: 读卡数据填充到患者表单

- **描述**: 将读卡器读取的身份信息自动填充到患者管理表单，支持已有患者匹配
- **业务规则**:
  1. 根据身份证号查询已有患者 (FindPatientByIdNumberAsync)
  2. 如患者已存在: 自动加载患者信息，显示就诊历史 (LastVisitTime, VisitCount)
  3. 如患者不存在: 提供快速创建入口 (QuickCreatePatientAsync)
  4. 支持一键匹配或创建 (FindOrCreatePatientAsync)
  5. 读卡数据自动映射: 姓名→RealName, 身份证号→IdNumber, 出生日期→BirthDate, 性别→Gender
  6. 在患者列表页通过 ReadCardCommand 触发
- **远程模式**: 读卡后通过 API 查询/创建患者
- **本地模式**: 读卡后通过 LocalPatientDataSource 查询/创建患者
- **验收标准**:
  - [ ] 已有患者通过身份证号正确匹配
  - [ ] 新患者快速创建包含完整读卡信息
  - [ ] IsNewlyCreated 标志正确反映创建状态

---

## 数据模型

### CardReadResult

| 字段 | 类型 | 说明 |
|------|------|------|
| Name | string | 姓名 |
| Gender | string | 性别 |
| Nation | string | 民族 |
| BirthDate | DateTime | 出生日期 |
| IdNumber | string | 身份证号 |
| Address | string | 住址 |
| PhotoPath | string? | 证件照路径 (可选) |

### PatientFromCardResult

| 字段 | 类型 | 说明 |
|------|------|------|
| PatientId | Guid | 患者 ID |
| Name | string | 姓名 |
| IdNumber | string | 身份证号 |
| IsNewlyCreated | bool | 是否新创建 |
| LastVisitTime | DateTime? | 最近就诊时间 |
| VisitCount | int | 就诊次数 |

---

## 接口定义

### ICardReader

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| ConnectAsync | Task<bool> | 连接读卡器 |
| DisconnectAsync | Task | 断开连接 |
| ReadCardAsync | Task<CardReadResult> | 读取身份证 |
| DetectCardAsync | Task<bool> | 探测卡片 |

### IPatientCardReaderIntegration

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| FindPatientByIdNumberAsync | Task<PatientFromCardResult?> | 按身份证号查患者 |
| QuickCreatePatientAsync | Task<Guid> | 快速创建患者 |
| FindOrCreatePatientAsync | Task<PatientFromCardResult> | 查找或创建 |

---

## 决策记录

| # | 决策 | 结论 | 依据 |
|---|------|------|------|
| 1 | 读卡器支持范围 | 策略模式多厂商，通过 ICardReader 接口抽象 | ICardReader 接口设计 |
| 2 | 读卡器功能可用模式 | 仅 Desktop 端，不区分远程/本地模式 | 纯客户端硬件交互 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
```

**Step 2: 验证**

Run: `grep -c "FR-CARD" docs/02-requirements/16-card-reader.md`
Expected: >= 2

---

## Task 2: 创建 docs/04-api-reference/11-health.md

**Files:**
- Create: `docs/04-api-reference/11-health.md`

**Source:** `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`

**Step 1: 创建API参考文档**

遵循项目API参考模板 (参考 auth.md 格式):

```markdown
# 健康检查 API

> Controller: `HealthController` | 路由前缀: `/api/v1/health` | 默认权限: `[Authorize]`

## 概述

提供服务端健康状态检查功能，用于负载均衡器探活、监控系统集成和运维排查。基础检查匿名访问，详细检查需认证。

---

## GET /health

基础健康检查，快速探活端点。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200):

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-10T12:00:00Z"
}
```

---

## GET /health/ping

Ping/Pong 端点，最轻量的探活检查。

- **权限**: 匿名 (`[AllowAnonymous]`)

**成功响应** (200):

```json
{
  "message": "pong",
  "timestamp": "2026-02-10T12:00:00Z"
}
```

---

## GET /health/details

详细健康检查，包含数据库连接状态。

- **权限**: 已认证 (`[Authorize]`)

**成功响应** (200):

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-10T12:00:00Z",
  "database": {
    "status": "Healthy",
    "duration": 45
  }
}
```

**降级响应** (503):

```json
{
  "status": "Degraded",
  "timestamp": "2026-02-10T12:00:00Z",
  "database": {
    "status": "Degraded",
    "duration": 120
  }
}
```

**状态值说明**:

| 状态 | HTTP | 说明 |
|------|------|------|
| Healthy | 200 | 所有组件正常 |
| Degraded | 503 | 数据库连接异常或超时 |
| Unhealthy | 503 | 严重错误 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
```

---

## Task 3: 创建 docs/04-api-reference/12-diagnostics.md

**Files:**
- Create: `docs/04-api-reference/12-diagnostics.md`

**Source:** `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs`, `src/Shared/LYBT.Shared.Logging/Management/LoggingLevelManager.cs`

**Step 1: 创建API参考文档**

```markdown
# 诊断工具 API

> Controller: `DiagnosticsController` | 路由前缀: `/api/v1/diagnostics` | 默认权限: `[Authorize(Roles = "SuperAdmin")]`

## 概述

提供运行时日志级别动态调整功能，用于生产环境问题排查。仅 SuperAdmin 角色可访问。调试模式有最大时长限制 (120 分钟)，到期自动恢复默认级别。

---

## GET /diagnostics/logging/status

获取当前日志级别状态。

- **权限**: SuperAdmin

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "currentLevel": "Debug",
    "defaultLevel": "Information",
    "isDebugModeActive": true,
    "debugModeStartedAt": "2026-02-10T12:00:00Z",
    "debugModeExpiresAt": "2026-02-10T12:30:00Z",
    "remainingMinutes": 25
  }
}
```

---

## POST /diagnostics/logging/debug/enable

启用临时调试模式。到期自动恢复默认日志级别。

- **权限**: SuperAdmin

**请求体**:

```json
{
  "level": "Debug",          // 可选，目标级别 (Verbose/Debug/Information)，默认 Debug
  "durationMinutes": 30      // 可选，持续时间 (1-120分钟)，默认 30
}
```

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "message": "调试模式已启用",
    "previousLevel": "Information",
    "currentLevel": "Debug",
    "startedAt": "2026-02-10T12:00:00Z",
    "expiresAt": "2026-02-10T12:30:00Z",
    "durationMinutes": 30
  }
}
```

---

## POST /diagnostics/logging/debug/disable

手动禁用调试模式，恢复默认日志级别。

- **权限**: SuperAdmin

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "message": "调试模式已禁用，已恢复默认日志级别",
    "previousLevel": "Debug",
    "currentLevel": "Information"
  }
}
```

---

## POST /diagnostics/logging/level

直接设置日志级别 (持久生效，直到重启或再次设置)。

- **权限**: SuperAdmin

**请求体**:

```json
{
  "level": "Debug"    // 必填，目标级别 (Verbose/Debug/Information/Warning/Error/Fatal)
}
```

**成功响应** (200):

```json
{
  "success": true,
  "data": {
    "message": "日志级别已更新",
    "previousLevel": "Information",
    "currentLevel": "Debug"
  }
}
```

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
```

---

## Task 4: desktop.md 新增 Controls/Dialogs/CardReader 章节

**Files:**
- Modify: `docs/03-architecture/02-desktop.md`

**Source:** HerbListControlViewModel, HerbItemControlViewModel, FormulaImportDialogViewModel, HistoryCopyDialogViewModel, SyncConflictDialogViewModel, UnsavedChangesDialogViewModel, UnfinishedCaseDialogViewModel, ICardReader, IPatientCardReaderIntegration

**Step 1: 读取 desktop.md 确定插入位置**

在"命令模式"章节之前 (变更记录之前)，新增三个章节。

**Step 2: 插入"可复用业务控件"章节**

在 desktop.md 的"变更记录"表格之前插入:

```markdown
## 可复用业务控件

Modules 层中提取的可复用 UI 组件，采用独立 ViewModel + 事件驱动模式。

### HerbListControl (药材列表编辑器)

**位置**: `Modules/LYBT.Desktop.Herbs/Controls/HerbList/`
**使用场景**: 处方药材编辑 (MedicalCase)、验方药材编辑 (Formula)

| 职责 | 说明 |
|------|------|
| 药材项管理 | 增删移动药材项 (ObservableCollection) |
| 重复检测 | 检测相同药材重复添加，支持合并策略 (DuplicateDosageStrategy) |
| 数据转换 | LoadFromDto / ToDto 与 PrescriptionItemDto 互转 |
| 空槽管理 | 自动创建空槽 (RequestNewSlot, GetNextEmptySlotIndex) |

**关键接口**:
- `ListChanged` 事件: 药材列表变更通知
- `Validate()`: 全部项验证
- `CanAddHerb(herbId)`: 重复检查

### HerbItemControl (单味药材编辑器)

**位置**: `Modules/LYBT.Desktop.Herbs/Controls/HerbItem/`
**使用场景**: HerbListControl 内部子组件

| 职责 | 说明 |
|------|------|
| 药材选择 | 下拉自动补全 (AllHerbs → FilteredHerbs → SelectedHerb) |
| 剂量验证 | 实时验证剂量有效性 (IsDosageValid, ValidationMessage) |
| 煎法选择 | DecocteMethod 枚举选择 |
| 状态判断 | IsEmpty (未选择药材), IsValid (已选择且剂量有效) |

**关键接口**:
- `ItemChanged` 事件: 单项变更通知
- `LoadFromDto(PrescriptionItemDto)` / `ToDto()`: 数据转换

---

## 业务弹窗

所有业务弹窗继承 `DialogViewModelBase`，通过 `IDialogService.ShowDialog` 调用。

| 弹窗 | 位置 | 用途 | 基类 |
|------|------|------|------|
| FormulaImportDialog | Modules/MedicalCase/Dialogs | 从验方库导入药材到处方 | DialogViewModelBase |
| HistoryCopyDialog | Modules/MedicalCase/Dialogs | 从历史医案复制处方 | DialogViewModelBase |
| UnsavedChangesDialog | Modules/MedicalCase/Dialogs | 未保存修改确认 (保存/放弃/取消) | ObservableObject |
| SyncConflictDialog | Modules/Sync/ViewModels | 同步冲突逐条处理 | DialogViewModelBase |
| UnfinishedCaseDialog | Core/Infrastructure/ViewModels | 未完成医案处理 (继续/新建/关闭) | ObservableObject |

### FormulaImportDialog

从验方库中搜索和选择验方，将验方中的药材导入到当前处方。

**交互流程**: 分类筛选 → 关键词搜索 → 选择验方 → 预览药材列表 → 确认导入

**关键属性**: SearchText, SelectedCategory, FilteredFormulas, SelectedFormula, SelectedFormulaHerbs

### HistoryCopyDialog

从患者历史医案中选择处方进行复制。

**交互流程**: 选择患者 → 时间范围筛选 → 选择医案 → 预览处方药材 → 确认复制

**关键属性**: PatientName, FilteredCases, SelectedCase, SelectedPrescriptionItems, IsShowingAllPatients

### SyncConflictDialog

逐条处理本地-服务端数据冲突。

**交互流程**: 查看冲突详情 → 选择策略 (保留本地/使用服务端/跳过) → 下一条 → 全部处理完成

**关键命令**: UseLocalCommand, UseServerCommand, SkipCommand, UseAllLocalCommand, UseAllServerCommand

---

## CardReader 集成

**位置**: `Core/LYBT.Desktop.CardReader/`

身份证读卡器硬件集成模块，通过策略模式支持多厂商设备。

### 架构

```
PatientMasterDetailViewModel
    └── ReadCardCommand
        └── IPatientCardReaderIntegration
            ├── FindPatientByIdNumberAsync (按身份证号查患者)
            ├── QuickCreatePatientAsync (快速创建)
            └── FindOrCreatePatientAsync (查找或创建)
                └── ICardReader
                    ├── ConnectAsync / DisconnectAsync
                    ├── ReadCardAsync → CardReadResult
                    └── DetectCardAsync
```

### 接口

| 接口 | 职责 |
|------|------|
| ICardReader | 硬件层: 设备连接、读卡、探测 |
| IPatientCardReaderIntegration | 业务层: 患者匹配、创建、数据映射 |

### 事件

| 事件 | 触发时机 |
|------|----------|
| ConnectionStateChanged | 读卡器连接/断开 |
| CardDetected | 检测到卡片插入 |

需求详见 [card-reader.md](../../02-requirements/16-card-reader.md)。
```

**Step 3: 更新变更记录表**

在 desktop.md 的变更记录表中追加一行:
```
| 2026-02-10 | v1.1 | 新增可复用业务控件、业务弹窗、CardReader 集成章节 |
```

---

## Task 5: 拆分 docs/06-operations/ 三文件

**Files:**
- Modify: `docs/06-operations/README.md` (精简为概述+索引)
- Create: `docs/06-operations/01-deployment.md` (提取部署内容)
- Create: `docs/06-operations/02-configuration.md` (提取配置内容)

**Step 1: 创建 deployment.md**

从 README.md 提取"服务端部署"和"客户端部署"部分，加上"数据库运维"部分:

内容来源: README.md 第 17-46 行 (服务端部署) + 第 186-229 行 (数据库运维 + 客户端部署)

**Step 2: 创建 configuration.md**

从 README.md 提取"配置说明"完整部分，扩展 appsettings.json 全量配置参考:

内容来源: README.md 第 50-118 行 (配置说明) + appsettings.json 逆向分析的完整配置树

configuration.md 应包含:
- 配置文件层次 (appsettings.json → appsettings.{Environment}.json)
- 11 个主要配置节的完整键值表 (AllowedHosts, Kestrel, ConnectionStrings, Jwt, PasswordPolicy, Session, DefaultPasswords, Security, Database, MemoryCache, Serilog 等)
- 每个配置项: 键名、类型、默认值、说明
- 环境变量覆盖说明

**Step 3: 精简 README.md**

保留:
- 部署架构图 (第 1-13 行)
- 概述段 + 索引表 (链接到 deployment.md, configuration.md)
- 日志系统概述 (第 122-157 行)
- 健康检查概述 (第 160-183 行) + 链接到 API 参考

删除:
- 服务端部署详细内容 (已迁移到 deployment.md)
- 配置说明详细内容 (已迁移到 configuration.md)
- 数据库运维 (已迁移到 deployment.md)
- 客户端部署 (已迁移到 deployment.md)

---

## Task 6: 更新 README 索引 (2个文件)

**Files:**
- Modify: `docs/02-requirements/README.md`
- Modify: `docs/04-api-reference/README.md`

**Step 1: 更新 02-requirements/README.md**

在模块索引表中追加:

```
| 身份证读卡器 | [card-reader.md](../../02-requirements/16-card-reader.md) | FR-CARD-001 ~ 002 | 2 |
```

更新总计: `92 → 94`

**Step 2: 更新 04-api-reference/README.md**

在"系统模块 (非业务)"部分，将 Health 和 Diagnostics 的行更新为链接到独立文档:

将原来的内嵌表格行改为:
```markdown
### 健康检查 ([health.md](../../04-api-reference/11-health.md))

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/health` | 匿名 | 基础健康检查 |
| GET | `/health/ping` | 匿名 | Ping |
| GET | `/health/details` | 已认证 | 详细健康检查 (含数据库) |

### 诊断工具 ([diagnostics.md](../../04-api-reference/12-diagnostics.md)) -- SuperAdmin

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/diagnostics/logging/status` | 日志级别状态 |
| POST | `/diagnostics/logging/debug/enable` | 启用调试模式 |
| POST | `/diagnostics/logging/debug/disable` | 禁用调试模式 |
| POST | `/diagnostics/logging/level` | 设置日志级别 |

### 实体审计 (无独立文档)

| 方法 | 路径 | 权限 | 说明 |
|------|------|------|------|
| GET | `/entityaudit/{entityType}/{entityId}` | 已认证 | 通用实体审计日志 |
```

**Step 3: 验证**

Run: `grep -c "card-reader.md" docs/02-requirements/README.md`
Expected: 1

Run: `grep -c "health.md\|diagnostics.md" docs/04-api-reference/README.md`
Expected: >= 2

---

## Task 7: 删除残留 + 全量验证

**Files:**
- Delete: `docs/mapperly-warning-fix-plan.md`

**Step 1: 删除残留文件**

Run: `rm docs/mapperly-warning-fix-plan.md`

**Step 2: 全量验证 - 文件结构**

Run: `find docs -name "*.md" -not -path "*/plans/*" | sort`

Expected: 应该有以下文件 (共 44 个):
- docs/README.md
- docs/01-product/ (4个)
- docs/02-requirements/ (9+1=10个，含新增 card-reader.md)
- docs/03-architecture/ (6个 + decisions/6个)
- docs/04-api-reference/ (7+2=9个，含新增 health.md, diagnostics.md)
- docs/05-development/ (5个)
- docs/06-operations/ (1+2=3个，含新增 deployment.md, configuration.md)

**Step 3: 全量验证 - 无残留**

Run: `ls docs/mapperly-warning-fix-plan.md 2>&1`
Expected: 文件不存在

**Step 4: 更新 planning-with-files 三文件**

更新 `task_plan.md`: 标记所有 Phase 为 complete
更新 `findings.md`: 添加补全后统计
更新 `progress.md`: 添加执行日志和 Final Summary

---

## Task 依赖关系

```
Task 1 (card-reader需求) ──┐
Task 2 (health API) ───────┤── 全部可并行
Task 3 (diagnostics API) ──┘
         │
         ▼
Task 4 (desktop.md补充) ── 依赖 Task 1 (CardReader内容)
Task 5 (运维文档拆分) ──── 独立
         │
         ▼
Task 6 (README索引更新) ── 依赖 Task 1-3
         │
         ▼
Task 7 (清理+验证) ─────── 依赖全部
```

**最大并行度**: Task 1 + Task 2 + Task 3 + Task 5 可同时执行 (4 路并行)

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始计划，7 Tasks |

---

**Created**: 2026-02-10
**Design Reference**: `docs/plans/2026-02-10-doc-code-alignment-design.md`
**Total Tasks**: 7
**Total Files**: 5 新增 + 4 修改 + 1 删除 = 10 个文件操作
**Estimated Parallel Batches**: 3 (Batch 1: Task 1-3+5 并行 | Batch 2: Task 4+6 | Batch 3: Task 7 验证)

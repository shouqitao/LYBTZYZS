# 数据同步 需求规格

## 概述

数据同步模块实现本地模式 (SQLite) 与远程服务端 (SQL Server) 之间的双向数据同步。基于 SHA256 Checksum 比对差异，支持冲突手动解决。同步范围涵盖药材、患者、验方三种实体类型。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 全部同步操作 |
| Admin | 全部同步操作 |
| Doctor | 全部同步操作 |
| Receptionist | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。

---

## 功能清单

> **[延期 2026-02-21]** MedicalCase 同步完全未实现 (FR-SYNC-001 实体类型列表含 MedicalCase，FR-SYNC-005 下载含 MedicalCase)
> 原因: 独立 Epic 规划复杂度极高，需聚合级原子同步+患者去重+编号重分配等  |  计划: 同步体系完善 Epic  |  参考: SYNC-01

### FR-SYNC-001: 获取可同步实体类型

- **描述**: 查询系统支持同步的实体类型列表
- **业务规则**:
  1. 当前支持: Herb (药材)、Patient (患者)、Formula (验方)、MedicalCase (医案, 含 Consultation + Prescription + Items)
  2. 返回实体类型名称列表
- **远程模式**: GET `/api/v1/sync/entity-types`
- **本地模式**: 不适用 (本功能需要网络)
- **验收标准**:
  - [ ] GET 请求 -> 返回 ["Herb", "Patient", "Formula", "MedicalCase"]

### FR-SYNC-002: 获取同步元数据

- **描述**: 获取指定实体类型在服务端的元数据 (ID + Checksum + 修改时间)
- **业务规则**:
  1. 返回每条记录的 EntityId、Checksum (SHA256)、LastModifiedAt、IsDeleted
  2. 用于客户端进行本地比对
- **远程模式**: GET `/api/v1/sync/metadata?entityType=`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 实体字段变更 -> SHA256 Checksum 值变化
  - [ ] 元数据列表 -> 包含 IsDeleted=true 的记录

> **[延期 2026-02-21]** ChangedFields 始终为 null，未实现变更字段检测
> 原因: 变更检测复杂度高，当前 Checksum 比对可识别差异存在  |  计划: 同步体系完善 Epic  |  参考: SYNC-08

### FR-SYNC-003: 数据比对

- **描述**: 比较本地与服务端数据的差异
- **业务规则**:
  1. 客户端发送本地元数据列表
  2. 服务端比对后返回差异列表
  3. 差异类型: LocalOnly (仅本地有) / ServerOnly (仅服务端有) / Modified (双方不同) / Identical (相同)
  4. 差异项包含: 实体ID、名称、Checksum、修改时间、变更字段列表
- **远程模式**: POST `/api/v1/sync/compare`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 比对结果 -> 包含 LocalOnly/ServerOnly/Modified 分类
  - [ ] Checksum 一致 -> 差异类型=Identical

### FR-SYNC-004: 上传本地变更

- **描述**: 将本地数据上传到服务端
- **业务规则**:
  1. 序列化为 JSON 格式传输
  2. 支持覆盖冲突选项 (OverwriteConflicts)
  3. 返回每条记录的上传结果 (成功/冲突/错误)
  4. 部分失败不影响其他记录
- **远程模式**: POST `/api/v1/sync/upload`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 上传 LocalOnly 数据 -> 服务端新增该记录
  - [ ] 数据已存在且 OverwriteConflicts=false -> IsConflict=true

### FR-SYNC-005: 下载服务端变更

- **描述**: 从服务端下载数据到本地
- **业务规则**:
  1. 按实体 ID 列表下载
  2. 返回 JSON 格式的实体数据
  3. 客户端负责保存到本地 SQLite
- **远程模式**: POST `/api/v1/sync/download`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 下载 ServerOnly 数据 -> 本地 SQLite 新增该记录

### FR-SYNC-006: 同步删除

- **描述**: 同步删除操作 (软删除)
- **业务规则**:
  1. 执行前进行引用检查
  2. 有引用的实体被拒绝删除，返回拒绝原因
  3. 返回成功 ID 列表和被拒绝项列表
- **远程模式**: POST `/api/v1/sync/delete`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 药材被处方引用 -> 返回拒绝，原因"药材被N个处方引用"
  - [ ] 删除被拒绝 -> 返回具体拒绝原因字符串

> **[延期 2026-02-21]** SyncConflictDetailDto 未实现 (冲突对话框仅展示 Checksum 信息)
> 原因: 依赖同步基础架构完善，当前 Checksum 展示可满足基本冲突识别  |  计划: 同步体系完善 Epic  |  参考: SYNC-02

> **[延期 2026-02-21]** 冲突对话框仅展示 Checksum，缺少字段级左右对比 UI
> 原因: 依赖 SyncConflictDetailDto 实现，与同步基础架构同步推进  |  计划: 同步体系完善 Epic  |  参考: SYNC-03

> **[已修订 2026-02-21]** 简单进度条 MVP 够用，PRD 简化进度 UI 描述 (移除 4 步骤指示器的详细格式要求)
> 原因: 当前简单进度条满足 MVP 需求，详细步骤指示器为锦上添花  |  参考: SYNC-11
> [实现状态] 代码实现已接受 (Sprint3)

### FR-SYNC-007: 完整同步工作流

- **描述**: 执行端到端的完整同步流程
- **业务规则**:
  1. 流程: 加载实体类型 -> 选择类型 -> 检查差异 -> 展示差异 -> 解决冲突 -> 执行同步 -> 结果汇总
  2. 冲突解决方式: 使用本地版本 (上传) / 使用服务端版本 (下载) / 跳过
  3. 差异展示: 分类显示 LocalOnly、ServerOnly、Conflicts
  4. 冲突解决对话框: 显示双方数据对比，用户选择保留哪一方
- **远程模式**: 客户端 UI 协调多个 API 调用
- **本地模式**: 不适用
- **同步前检查**:
  1. 网络连通性检查 (尝试 Ping 服务端)
  2. 认证有效性检查 (AccessToken 是否过期)
  3. 检查失败 -> 提示用户 (网络不可用 / Token 已过期，请重新登录)
- **进度 UI (步骤指示器)**:
  ```
  [1. 检查差异] -> [2. 解决冲突] -> [3. 执行同步] -> [4. 完成]
         ↓                ↓                ↓              ↓
   显示当前实体类型    冲突数量显示     进度条+当前项    结果汇总
  ```
  - 步骤 1: 显示 "正在检查 {EntityType} 差异..." + 进度条
  - 步骤 2: 显示冲突数量，用户逐条解决
  - 步骤 3: 显示 "正在同步 {EntityType}... ({N}/{Total})" + 进度条
  - 步骤 4: 显示结果汇总
- **结果汇总 UI**:
  按实体类型分组显示:
  ```
  同步完成
  ┌─────────────────────────────────────┐
  │ 药材:  上传 3 | 下载 5 | 跳过 0    │
  │ 患者:  上传 1 | 下载 8 | 跳过 2    │
  │ 验方:  上传 0 | 下载 3 | 跳过 0    │
  │ 失败:  0 条                         │
  └─────────────────────────────────────┘
  ```
  失败项可展开查看失败原因
- **失败恢复策略**: 重新开始
  - 失败时显示错误摘要 + 已成功同步的部分
  - 用户点击"重新同步"从头开始 (Checksum 比对保证已同步数据不重复)
  - 已同步的数据通过 Checksum 一致性自动跳过
- **验收标准**:
  - [ ] 选择类型->检查差异->解决冲突->执行同步 -> 全流程完成
  - [ ] 选择"使用本地版本" -> 上传覆盖服务端数据
  - [ ] 同步过程中网络中断 -> 显示错误提示 + "重新同步"按钮
  - [ ] 重新同步 -> 已同步项自动跳过 (Checksum 一致)
  - [ ] 完成后 -> 显示按实体类型分组的详细汇总

> **[延期 2026-02-21]** 运行时模式切换未实现，当前仅支持登录时选择模式
> 原因: MVP 阶段登录时选择够用，运行时切换增加复杂度  |  计划: 模式切换 Sprint  |  参考: SYNC-04

> **[延期 2026-02-21]** 切换前未同步变更检查未实现
> 原因: 依赖运行时模式切换功能  |  计划: 模式切换 Sprint  |  参考: SYNC-05

> **[延期 2026-02-21]** 切换失败回退策略未实现
> 原因: 依赖运行时模式切换功能  |  计划: 模式切换 Sprint  |  参考: SYNC-13

### FR-SYNC-008: 模式切换

- **描述**: 手动切换远程模式和本地模式
- **业务规则**:
  1. 切换为手动触发 (设置菜单或系统设置面板)
  2. 切换到本地模式时使用 SQLite 数据源
  3. 切换到远程模式时使用 HTTP API + SQL Server
  4. 切换前检查未同步变更
- **切换前检查**:
  1. 检查本地是否有未同步变更 (比对本地 Checksum 与上次同步时的 Checksum)
  2. 有未同步变更 -> 弹出提示: "本地有 N 条未同步数据，建议先同步再切换。是否继续?"
  3. 用户可选择: "先同步" (跳转同步页面) / "继续切换" (忽略未同步数据) / "取消"
- **远程 -> 本地 切换**:
  1. 检查 SQLite 数据库文件是否存在且完整
  2. 不存在 -> 提示: "本地数据库不存在，需先执行一次同步"
  3. 存在 -> 切换 DataSource 为 SQLite
  4. 切换成功 -> 状态栏显示"本地模式"标识
- **本地 -> 远程 切换**:
  1. **检查本地未完成医案 (SYNC-D01 前置约束)**:
     - 查询本地 SQLite 中 CaseStatus = Active 或 Suspended 的医案数量
     - 有未完成医案 -> 阻断切换，提示: "本地有 {N} 个未完成的医案，请先完成或取消后再切换模式" (ERR-70506)
     - 无未完成医案 -> 继续
  2. 检查网络连通性 (Ping 服务端)
  3. 网络不可用 -> 提示: "无法连接服务器，请检查网络"
  4. 检查认证状态 (Token 是否有效)
  5. Token 过期 -> 跳转登录页重新认证
  6. 认证有效 -> 切换 DataSource 为 HTTP API
  7. 切换成功 -> 状态栏显示"远程模式"标识
- **切换失败回退**:
  - 切换过程中出现错误 -> 自动回退到切换前的模式
  - 显示错误提示: "切换失败: {原因}，已恢复到{当前模式}"
- **远程模式**: 不适用 (切换操作本身)
- **本地模式**: 不适用 (切换操作本身)
- **验收标准**:
  - [ ] 切换到本地模式 -> DataSource 使用 SQLite
  - [ ] 切换到远程模式 -> DataSource 使用 HTTP API
  - [ ] 有未同步数据时切换 -> 弹出提示对话框
  - [ ] 网络不可用时切换到远程模式 -> 显示网络错误提示
  - [ ] 切换失败 -> 自动回退到切换前模式
  - [ ] 本地有未完成医案时切换到远程模式 -> 阻断并提示完成或取消 (SYNC-D01)

---

## 同步 DTO 定义

> **[已修订 2026-02-21]** DTO 命名与代码不一致，PRD 对齐代码命名 (SyncMetadataDto/SyncDiffResultDto/SyncDiffItemDto 等字段名以代码实现为准)
> 原因: 确保 PRD DTO 定义与代码实现一致，减少开发歧义  |  参考: SYNC-15
> [实现状态] 代码实现已接受 (Sprint3)

> **[已修订 2026-02-21]** 字段名差异，PRD 对齐代码字段名 (如 LastModifiedAt vs ModifiedAt 等具体字段以代码为准)
> 原因: PRD 与代码字段名不一致导致理解偏差  |  参考: SYNC-16
> [实现状态] 代码实现已接受 (Sprint3)

> **[已修订 2026-02-21]** 其他命名规范差异，PRD 对齐代码 (枚举值、属性名等以代码实现为准)
> 原因: 统一 PRD 与代码的命名规范  |  参考: SYNC-19
> [实现状态] 代码实现已接受 (Sprint3)

### SyncMetadataDto (元数据)

```
SyncMetadataDto {
    EntityId: Guid          // 实体 ID
    EntityType: string      // "Herb" | "Patient" | "Formula"
    Checksum: string        // SHA256 哈希值
    LastModifiedAt: DateTime // 最后修改时间 (UTC)
    IsDeleted: bool         // 是否已删除
    DisplayName: string     // 显示名称 (用于 UI 展示)
}
```

### SyncDiffResultDto (比对结果)

```
SyncDiffResultDto {
    EntityType: string                    // 实体类型
    LocalOnlyCount: int                   // 仅本地数量
    ServerOnlyCount: int                  // 仅服务端数量
    ModifiedCount: int                    // 冲突数量
    IdenticalCount: int                   // 一致数量
    Items: SyncDiffItemDto[]              // 差异项列表
}

SyncDiffItemDto {
    EntityId: Guid
    DisplayName: string                   // 实体显示名称
    DiffType: string                      // "LocalOnly" | "ServerOnly" | "Modified" | "Identical"
    LocalChecksum: string?                // 本地 Checksum (LocalOnly 时有值)
    ServerChecksum: string?               // 服务端 Checksum (ServerOnly 时有值)
    LocalModifiedAt: DateTime?            // 本地修改时间
    ServerModifiedAt: DateTime?           // 服务端修改时间
    ChangedFields: string[]?              // 变更字段列表 (Modified 时有值)
}
```

### SyncConflictDetailDto (冲突详情，用于左右对比 UI)

```
SyncConflictDetailDto {
    EntityId: Guid
    EntityType: string
    DisplayName: string
    LocalVersion: Dictionary<string, string>    // 本地字段值 { "Name": "甘草", "Price": "15.00", ... }
    ServerVersion: Dictionary<string, string>   // 服务端字段值
    ChangedFields: string[]                     // 差异字段列表 (高亮显示)
    LocalModifiedAt: DateTime
    ServerModifiedAt: DateTime
}
```

### SyncResultDto (同步结果)

```
SyncResultDto {
    EntityType: string
    UploadedCount: int       // 上传成功数
    DownloadedCount: int     // 下载成功数
    SkippedCount: int        // 跳过数
    FailedCount: int         // 失败数
    FailedItems: SyncFailedItemDto[]  // 失败详情
}

SyncFailedItemDto {
    EntityId: Guid
    DisplayName: string
    ErrorMessage: string     // 失败原因
}
```

---

## 冲突解决 UI

### 布局: 左右对比

```
┌─────────────────────────────────────────────────────────────┐
│  冲突解决: 药材 "甘草" (1/3)                                  │
├──────────────────────────┬──────────────────────────────────┤
│  本地版本                 │  服务端版本                       │
│  修改时间: 2026-02-15     │  修改时间: 2026-02-16            │
├──────────────────────────┼──────────────────────────────────┤
│  名称: 甘草               │  名称: 甘草                      │
│  价格: **15.00**          │  价格: **18.00**                 │  ← 差异字段高亮
│  分类: 补益药             │  分类: 补益药                    │
│  单位: 克                 │  单位: 克                        │
├──────────────────────────┴──────────────────────────────────┤
│  [保留本地版本]    [使用服务端版本]    [跳过]                   │
└─────────────────────────────────────────────────────────────┘
```

### 交互规则

| 操作 | 行为 |
|------|------|
| 保留本地版本 | 上传本地数据覆盖服务端 (OverwriteConflicts=true) |
| 使用服务端版本 | 下载服务端数据覆盖本地 |
| 跳过 | 该条记录本次不同步，保持双方各自版本 |
| 差异字段 | 仅变更字段高亮显示 (黄色背景)，未变更字段正常显示 |
| 多条冲突 | 逐条解决，显示当前进度 (如 "1/3") |

---

## Checksum 机制

每种实体类型有独立的 Checksum 计算逻辑:

| 实体类型 | 参与计算的字段 |
|----------|---------------|
| Herb | Name, Unit, Price, Category, Origin, Spec, Effect, Usage |
| Patient | Name, Gender, BirthDate, IdNumber, PhoneNumber, Address |
| Formula | Name, Effect, Indication, Usage, Herbs 组成 |
| MedicalCase | 聚合级计算，详见下方 MedicalCase 同步设计章节 |

算法: SHA256 哈希

---

## MedicalCase 同步设计

### 核心场景: 外出看诊离线工作流

```
出诊前 (在线)                 外出看诊 (离线)                返回诊所 (在线)
─────────────────────    ─────────────────────────    ─────────────────────
1. 药材全量同步 (基础数据)    4. 查看历史医案 (参考)         7. 上传新建医案
2. 患者同步 (可能忘记)       5. 本地新建医案               8. 患者去重 / 新建
3. 验方同步 (可选, 提高效率)  6. 开具处方 (用本地药材)       9. 数据一致性校验
   下载目标患者历史医案
```

- 药材作为基础数据，必须同步
- 患者可以同步，忘记同步时可在本地新建，返回后通过 IdCardNumber 去重
- 验方可以同步，忘记同步仅牺牲快捷导入便利性
- 医案是同步的**主要焦点**: 离线创建 → 联网后上传到 Server

### 同步粒度: 聚合级原子同步

MedicalCase 以 DDD 聚合为同步单位，包含:

```
MedicalCase (聚合根)
├── Consultation (1:1, 共享主键) -- 诊断信息
├── Prescription (1:0..1) -- 处方信息
│   └── PrescriptionItem[] (1:N) -- 药材明细
└── 跨聚合引用: PatientId, UserId, HerbId (Items)
```

上传/下载时，整个聚合作为一个 JSON 对象传输。Server 端使用单数据库事务写入全部实体，任何一部分失败则整体回滚。

### 同步状态约束 (SYNC-D01)

| 医案状态 | 上传 | 下载 | 说明 |
|---------|------|------|------|
| Active | 不可 | 不可 | 正在诊疗中，应先完成再同步 |
| Suspended | 不可 | 不可 | 已挂起，应先完成或取消再同步 |
| Completed | 可以 | 可以 | 通过 BR-003 校验，数据完整，已存在的 Completed 不可被覆盖 |

> 注: Draft 状态已替换为 Suspended (MC-D20)，医案状态: Active / Suspended / Completed。Suspended 亦不可同步。

仅同步 Completed 医案。理由:
1. **数据完整性** -- Completed 通过 BR-003 校验 (辨证+处方+帖数)，Server 端无需处理半成品数据
2. **冲突处理极简** -- 完成的医案除 Admin 编辑外不会再变，上传后几乎不存在双向冲突
3. **同步逻辑单向化** -- 本地 -> Server 单向推送，不需要回拉状态更新
4. **符合业务语义** -- 本地模式定位为短期无网络应急，Active 的生命周期应在单一模式内闭合

> 模式切换前，系统强制要求本地无 Active/Suspended 医案 (见 FR-SYNC-008 切换前检查)。

### MedicalCase 同步 DTO

```
MedicalCaseSyncDto {
    // MedicalCase 聚合根
    Id: Guid
    PatientId: Guid
    UserId: Guid
    PatientName: string
    DoctorName: string
    CaseNumber: string?           // 本地编号，Server 会重新分配
    CaseStatus: MedicalCaseStatus // Active / Suspended / Completed (仅 Completed 参与同步)
    NeedsPrescription: bool?
    CompletedAt: DateTime?
    Remark: string?
    IsDeleted: bool               // 软删除标记，用于同步删除操作
    CreatedAt: DateTime           // 保留本地创建时间
    UpdatedAt: DateTime           // 保留本地更新时间

    // Consultation (内嵌, Id 与 MedicalCase 共享主键，无需单独传递)
    Consultation: ConsultationSyncDto? {
        PresentIllness: string?
        TongueDiagnosis: string?
        PulseDiagnosis: string?
        TcmDiagnosis: string?
    }

    // Prescription (内嵌, 可选)
    Prescription: PrescriptionSyncDto? {
        PrescriptionNumber: string?  // 本地编号，Server 会重新分配
        DosageCount: int
        Discount: decimal
        Usage: string?
        Advice: string?
        ReferencedFormulas: string?
        Remark: string?
        Items: PrescriptionItemSyncDto[] {
            Id: Guid
            HerbId: Guid
            HerbName: string
            Dosage: int
            Unit: string
            DecocteMethod: DecocteMethod
            UnitPrice: decimal
            Usage: string?
            Remark: string?
        }
    }
}
```

> 打印相关字段 (MedicalCase.IsPrinted, MedicalCase.PrintVersion, MedicalCase.PrintCount, MedicalCase.LastPrintedAt, MedicalCasePrintLog) 不参与同步。打印是本地行为，每台设备独立记录。

### MedicalCase Checksum 计算

聚合级哈希，合并 4 层实体的业务字段:

| 层级 | 参与计算字段 | 排除字段 |
|------|------------|---------|
| MedicalCase | PatientId, UserId, CaseStatus, NeedsPrescription, CompletedAt, Remark | CaseNumber (可变), PatientName/DoctorName (冗余), 审计字段 |
| Consultation | PresentIllness, TongueDiagnosis, PulseDiagnosis, TcmDiagnosis | 审计字段 |
| Prescription | DosageCount, Discount, Usage, Advice, ReferencedFormulas, Remark | PrescriptionNumber (可变), 打印字段, 审计字段 |
| PrescriptionItem | HerbId, Dosage, Unit, DecocteMethod, UnitPrice, Usage, Remark | Id, PrescriptionId (结构字段) |

- PrescriptionItem 按 HerbId 排序后参与计算，保证哈希确定性
- 无 Prescription 时，该层哈希贡献为空
- 算法同其他实体: SHA256

### 依赖顺序: 自动强制

```
用户勾选 MedicalCase 同步
       ↓
系统自动编排执行顺序:
  Step 1: Herb 同步 (基础药材数据)
  Step 2: Patient 同步 (含患者去重)
  Step 3: MedicalCase 同步
```

- 用户无需关心依赖顺序，系统自动处理
- 同步进度 UI 依次显示每个步骤的进展
- 如果 Herb/Patient 同步失败，MedicalCase 同步不执行，提示失败原因

### 患者去重流程 (忘记同步患者时)

```
本地新建患者 (GUID-A) + 本地新建医案 (PatientId=GUID-A)
                    ↓ 上传患者
Server 按 IdCardNumber 检查:
    ├─ 不存在 → 新建患者 (保留 GUID-A) → 医案正常上传
    └─ 已存在 (GUID-B) → 返回 Server 端 PatientId (GUID-B)
                          → 客户端重映射:
                            MedicalCase.PatientId: GUID-A → GUID-B
                            MedicalCase.PatientName: 以 Server 端为准
                          → 上传医案 (PatientId=GUID-B)
                          → 本地患者 GUID-A 标记为"已合并"
```

- 去重匹配键: **IdCardNumber** (Required + Unique，PRD PAT-D03)
- Server 端在 Patient 上传接口中增加去重检查逻辑
- 客户端收到重映射信号后，自动替换所有关联 MedicalCase 的 PatientId

### BR-001 约束 (同一患者单活跃医案)

> 由于 SYNC-D01 约束仅同步 Completed 医案，BR-001 (同一患者不可有多个活跃医案) 在同步场景下不会触发冲突。上传的 Completed 医案不受此规则限制。

### 编号重分配

| 编号字段 | 本地生成 | 上传后处理 |
|---------|---------|-----------|
| MedicalCase.CaseNumber | 本地按日期+序号生成 (如 MC20260218001) | Server 重新分配，保持全局唯一序列 |
| Prescription.PrescriptionNumber | 本地按日期+序号生成 (如 RX-20260218-0001) | Server 重新分配 |
| 各实体 Id (GUID) | 本地生成 GUID | **保留不变** (GUID 全局唯一，无冲突) |

> Server 分配的新编号通过上传响应返回客户端，客户端更新本地记录。

### 引用完整性校验

上传 MedicalCase 前，Server 端自动校验:

| 引用字段 | 校验方式 | 失败处理 |
|---------|---------|---------|
| PatientId | 查询 Patient 表是否存在 | 拒绝上传，提示 "患者不存在，请先同步患者" |
| PrescriptionItem.HerbId | 批量查询 Herb 表 | 拒绝上传，提示 "药材 {HerbName} 不存在，请先同步药材" |
| UserId | 查询 User 表是否存在 | 应始终存在 (登录用户)；不存在则拒绝 |

> 由于采用自动强制依赖顺序，正常流程中不会触发引用校验失败。此校验为防御性措施。

### MedicalCase 冲突解决 UI

复用现有冲突解决 UI (SyncConflictDialog)，左右对比布局:

```
┌──────────────────────────────────────────────────────────────────────┐
│  冲突解决: 医案 "张三 - MC20260218001" (1/2)                           │
├────────────────────────────────┬─────────────────────────────────────┤
│  本地版本                       │  服务端版本                          │
│  修改时间: 2026-02-18 10:30     │  修改时间: 2026-02-18 14:20         │
│  状态: Completed                │  状态: Active                       │
├────────────────────────────────┼─────────────────────────────────────┤
│  中医辨证: **肝气郁结**          │  中医辨证: **肝郁脾虚**              │
│  现病史: 胁肋胀痛...             │  现病史: 胁肋胀痛...                │
│  处方:                          │  处方:                              │
│    甘草 10g                     │    甘草 10g                         │
│    **柴胡 12g**                 │    **柴胡 15g**                     │
│    白芍 15g                     │    白芍 15g                         │
├────────────────────────────────┴─────────────────────────────────────┤
│  [保留本地版本]    [使用服务端版本]    [跳过]                             │
└──────────────────────────────────────────────────────────────────────┘
```

变更字段检测跨整个聚合 (诊断 + 处方 + 药材明细)，差异字段高亮显示。

### MedicalCase 同步错误

#### 服务端新增错误

| 场景 | 错误消息 | 触发条件 |
|------|----------|----------|
| MedicalCase 上传失败 | (异常原始消息) | 医案上传过程异常 |
| 患者不存在 | 患者 {PatientId} 不存在，请先同步患者 | PatientId 引用的患者在 Server 端不存在 |
| 药材不存在 | 药材 {HerbName} ({HerbId}) 不存在，请先同步药材 | PrescriptionItem.HerbId 引用的药材不存在 |
| BR-001 冲突 | 患者 {PatientName} 已有活跃医案 | SYNC-D01: 仅同步 Completed，此场景不再触发 |
| MedicalCase 有引用 | 医案已完成且已锁定，无法通过同步覆盖 | 尝试覆盖已锁定的 Completed 医案 |

#### 客户端新增错误

| 场景 | 错误消息 | 触发条件 |
|------|----------|----------|
| 依赖未同步 | 请先同步药材和患者数据 | MedicalCase 同步前依赖检查失败 |
| 患者重映射失败 | 无法匹配患者 {PatientName}，请手动处理 | IdCardNumber 匹配失败 (本地患者无身份证号) |

---

## 差异类型

| 类型 | 英文 | 说明 | 处理方式 |
|------|------|------|----------|
| 仅本地 | LocalOnly | 本地有但服务端无 | 上传 |
| 仅服务端 | ServerOnly | 服务端有但本地无 | 下载 |
| 已修改 | Modified | 双方都有但 Checksum 不同 | 需要手动解决冲突 |
| 相同 | Identical | 双方 Checksum 一致 | 无需处理 |

---

## 错误码

> 同步模块当前采用 ServiceResult 模式处理错误，计划在 S5 迁移到 Result\<T\> 统一返回类型 (D2-2 设计)。错误码分区: 7xxxx，编号体系: MCCEE (M=模块7, CC=子类别, EE=序号)。服务端和客户端分层处理。

### 服务端通用错误 (701xx)

| 错误码 | 枚举名 | HTTP | 错误消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-70101 | UnsupportedEntityType | 400 | 不支持的实体类型: {EntityType}，支持的类型: Herb, Patient, Formula, MedicalCase | 传入非支持的实体类型 |
| ERR-70102 | JsonDeserializeFailed | 400 | JSON 反序列化失败 | 上传数据格式错误 |
| ERR-70103 | SyncDataConflict | 409 | 服务器已存在该数据 | 上传时数据冲突且 OverwriteConflicts=false |

### 服务端上传错误 (702xx)

| 错误码 | 枚举名 | HTTP | 错误消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-70201 | HerbUploadFailed | 500 | (异常原始消息) | 药材上传过程异常 |
| ERR-70202 | PatientUploadFailed | 500 | (异常原始消息) | 患者上传过程异常 |
| ERR-70203 | FormulaUploadFailed | 500 | (异常原始消息) | 验方上传过程异常 |
| ERR-70204 | MedicalCaseUploadFailed | 500 | (异常原始消息) | 医案上传过程异常 |

### 服务端 MedicalCase 特有错误 (703xx)

| 错误码 | 枚举名 | HTTP | 错误消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-70301 | SyncPatientNotFound | 422 | 患者 {PatientId} 不存在，请先同步患者 | MedicalCase 上传时 PatientId 引用的患者不存在 |
| ERR-70302 | SyncHerbNotFound | 422 | 药材 {HerbName} ({HerbId}) 不存在，请先同步药材 | MedicalCase 上传时 PrescriptionItem.HerbId 不存在 |
| ~~ERR-70303~~ | ~~SyncActiveCaseConflict~~ | - | ~~已移除~~ | SYNC-D01: 仅同步 Completed，不再上传 Active/Suspended |
| ERR-70304 | SyncCaseLocked | 422 | 医案已完成且已锁定，无法通过同步覆盖 | 尝试覆盖已锁定的 Completed 医案 |

### 服务端删除错误 (704xx)

| 错误码 | 枚举名 | HTTP | 错误消息 | 触发条件 |
|--------|--------|------|----------|----------|
| ERR-70401 | SyncReferenceCheckFailed | 500 | 无法检查引用关系 | 删除前引用检查异常 |
| ERR-70402 | SyncHerbHasReference | 422 | 药材被 {ReferenceCount} 个处方引用，请先禁用 | 删除药材时被处方引用 |
| ERR-70403 | SyncPatientHasReference | 422 | 患者有 {ReferenceCount} 条医案记录，请先禁用 | 删除患者时有关联医案 |
| ERR-70404 | SyncEntityNotFound | 404 | 实体不存在或已删除 | 软删除时实体已不存在 |

### 客户端错误 (705xx)

| 错误码 | 枚举名 | 错误消息 | 触发条件 |
|--------|--------|----------|----------|
| ERR-70501 | SyncNoEntityTypeSelected | 请选择要同步的数据类型 | UI 中未选择 EntityType |
| ERR-70502 | SyncFailed | 同步失败: {错误列表} | 服务返回失败结果 |
| ERR-70503 | SyncChecksumTypeError | 不支持的实体类型: {entityType} | 计算 Checksum 时类型无效 |
| ERR-70504 | SyncDependencyNotSynced | 请先同步药材和患者数据 | MedicalCase 同步前依赖检查失败 |
| ERR-70505 | SyncPatientRemapFailed | 无法匹配患者 {PatientName}，请手动处理 | IdCardNumber 匹配失败 (本地患者无身份证号) |
| ERR-70506 | SyncLocalActiveCasesExist | 本地有 {Count} 个未完成的医案，请先完成或取消后再切换模式 | 模式切换前检测到本地存在 Active/Suspended 医案 |

### 上传结果结构

```
SyncUploadResultDto: { SuccessCount, ConflictCount, ErrorCount, Results[] }
SyncUploadItemResult: { Success, ErrorMessage, IsConflict }
```

---

## 决策记录

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| 1 | 冲突解决策略 | FR-SYNC-003, 007 | 已确定: 手动逐条选择 (保留本地 / 使用服务端 / 跳过)。医疗数据需人工确认，不适合自动覆盖 |
| 2 | 本地模式功能受限范围 | 全部 FR-SYNC | 已确定: 同步需网络连接。不可用项: 自动登录 / Token刷新 / 审计日志查询 / User同步。MedicalCase同步已支持 (决策3)。详见 dual-mode.md |
| 3 | MedicalCase 同步 | FR-SYNC-001 | 已确定: 详细设计已完成。聚合级原子同步 (MC+Consultation+Prescription+Items); **仅 Completed 状态同步** (SYNC-D01); 自动强制依赖顺序 (Herb->Patient->MC); 患者 IdCardNumber 去重+PatientId 重映射; CaseNumber/PrescriptionNumber Server 重分配; 打印字段不参与同步; 模式切换前强制无 Active/Suspended 医案。详见 "MedicalCase 同步设计" 章节 |
| 4 | 自动同步提示 | FR-SYNC-007 | 已确定: v1.0 不实现。用户手动进入同步模块触发。v2.0 考虑 NetworkStatusService + 状态栏指示器 |
| 5 | 同步进度 UI | FR-SYNC-007 | 已确定: 步骤指示器 (4步) + 当前实体类型 + 进度条。结果汇总按实体类型分组 |
| 6 | 同步失败恢复策略 | FR-SYNC-007 | 已确定: 重新开始。已同步数据通过 Checksum 比对自动跳过不重复 |
| 7 | 冲突解决 UI | FR-SYNC-007 | 已确定: 左右对比布局，差异字段高亮，逐条解决 |
| 8 | 模式切换前检查 | FR-SYNC-008 | 已确定: 检查未同步变更并提示用户，用户可选择先同步/继续切换/取消 |
| SYNC-D01 | 医案同步范围 | FR-SYNC-004, 005, 008 | 已确定: 仅同步 CaseStatus==Completed 的医案。Active 生命周期在单一模式内闭合。模式切换前置校验: 本地无 Active/Suspended 医案才允许切换到远程模式。理由: 数据完整性保证 + 冲突处理极简 + 符合业务语义 (本地模式=短期应急)。注: Draft 已替换为 Suspended (MC-D20) |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本，从 SyncController + Desktop.Sync + Desktop.LocalData 提取 |
| 2026-02-11 | v1.1 | 新增错误码章节，含服务端 10 个 + 客户端 3 个错误场景 |
| 2026-02-11 | v1.2 | 验收标准格式统一为 [场景] -> [预期结果] 格式 |
| 2026-02-17 | v2.0 | Round 4 深化: 新增 DTO 定义 (4个)、冲突解决 UI (左右对比)、同步进度 UI (步骤指示器)、模式切换前检查和回退策略、失败恢复策略 |
| 2026-02-17 | v2.1 | PRD审查修复: E1-MedicalCase同步必须支持(决策3更新+FR-SYNC-001实体类型扩展), A7-FR-SYNC-006删除策略已对齐BR-DEL-001 |
| 2026-02-18 | v3.0 | MedicalCase同步详细设计: 外出看诊离线工作流、聚合级原子同步、MedicalCaseSyncDto定义、聚合Checksum计算、自动依赖顺序(Herb->Patient->MC)、患者IdCardNumber去重+PatientId重映射、BR-001冲突处理、编号重分配、引用完整性校验、冲突解决UI、新增错误码(服务端6个+客户端2个) |
| 2026-02-18 | v3.1 | 错误码全量分配: 新增7xxxx范围，5个子类别(701xx~705xx)共20个错误码，服务端错误补充HTTP状态码，统一ERR-MCCEE格式+枚举名 |
| 2026-02-21 | v3.2 | PRD vs Code 偏差分析修订: 4 项修订, 7 项延期标注 |
| 2026-02-21 | v3.3 | SYNC-D01: 医案同步仅限 Completed 状态。移除 BR-001 冲突处理和 ERR-70303; 新增 ERR-70506 (本地未完成医案阻断切换); FR-SYNC-008 新增 Active/Suspended 前置检查; Draft→Suspended 术语更新 (MC-D20) |
| 2026-02-23 | v3.4 | 一致性审计: 错误码章节标注 ServiceResult → Result\<T\> 迁移计划 (D2-2, S5) |

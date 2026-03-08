# 端到端临床工作流

> **版本**: v2.0
> **创建日期**: 2026-02-21
> **目的**: 串联从患者到达到离开的完整临床流程，统一展示跨模块交互
> **覆盖缺口**: GAP-FLOW-1 (docs/plans/2026-02-21-prd-design-gap-analysis.md)
> **v2.0 深化**: 补全异常路径、分支流程、子流程时序图、跨模块联动、编辑模式切换

---

## 一、流程全景

### 1.1 端到端时序图

> **v2.0 更新**: 增加两种入口模式 (前台挂号 / 医生直接) 和 Registration 挂号记录

```mermaid
sequenceDiagram
    participant R as 前台 (Receptionist)
    participant S as 系统
    participant D as 医生 (Doctor)
    participant P as 打印

    rect rgb(240, 248, 255)
        Note over R, S: 阶段 1a: 前台挂号 (模式 1)
        R->>S: 查询患者 (模糊搜索 / 身份证匹配 / 读卡器)
        alt 患者已存在
            S-->>R: 加载患者信息
        else 患者不存在
            R->>S: 快速创建患者 (FR-PAT-001)
            S-->>R: 新患者信息
        end
        R->>S: 挂号: 指派医生
        Note right of S: 创建 Registration<br/>(PatientId, DoctorId 必填)<br/>Status=Waiting
        S-->>R: 挂号成功
    end

    rect rgb(240, 255, 240)
        Note over D, S: 阶段 1b: 医生直接创建 (模式 2, 前台不在时)
        D->>S: 查询患者 (模糊搜索 / 身份证匹配)
        alt 患者已存在
            S-->>D: 加载患者信息
        else 患者不存在
            D->>S: 创建新患者 (FR-PAT-001)
            S-->>D: 新患者信息
        end
    end

    rect rgb(245, 255, 245)
        Note over D, S: 阶段 2: 诊疗
        alt 模式 1
            D->>S: 从挂号队列选中患者 (Registration.Status=Waiting)
        else 模式 2
            D->>S: 直接选择患者发起创建
        end
        D->>S: 创建医案 (FR-MC-001)
        Note right of S: BR-001 碰撞检查 (见 Section 六)<br/>Registration.Status → InProgress
        S-->>D: MedicalCase (Active) + Consultation

        D->>S: 填写诊断 (FR-MC-002)
        Note right of S: 中医辨证 (完成时必填)<br/>现病史/舌诊/脉诊 (可选)
        S-->>D: MedicalCase (Active)

        D->>S: 标记处方需求 (FR-MC-003)
        Note right of S: NeedsPrescription: true/false/null
    end

    rect rgb(255, 248, 240)
        Note over D, S: 阶段 3: 处方 (NeedsPrescription=true 时)
        alt 验方导入 (FR-MC-016)
            D->>S: 获取已验证验方列表
            S-->>D: Formula[] (Validated + Enabled)
            D->>S: 导入药材到处方 (见 Section 七)
            Note right of S: 禁用药材跳过 (MC-D09)<br/>重复药材合并 (MC-D17)<br/>数据复制 (MC-D12)
        else 历史处方复制 (FR-MC-018)
            D->>S: 获取患者历史 Completed 医案
            D->>S: 复制处方 (见 Section 七)
            Note right of S: 价格实时获取 (MC-D13)<br/>禁用药材跳过 (MC-D09)
        else 手工输入
            D->>S: 直接编辑处方药材
        end

        D->>S: 聚合保存 (FR-MC-005)
        Note right of S: 乐观锁 RowVersion (MC-D10)<br/>打印保护检查 (MC-D15)<br/>审计日志 (FR-MC-012)
    end

    rect rgb(255, 245, 255)
        Note over D, P: 阶段 4: 打印与完成
        D->>P: 打印预览 (FR-PRINT-002)
        P-->>D: FixedDocument 预览 (A5/A4)
        D->>P: 确认打印 (FR-PRINT-001)
        Note right of P: MedicalCase.IsPrinted=true<br/>PrintCount++<br/>MedicalCasePrintLog

        D->>S: 完成医案 (FR-MC-007)
        Note right of S: 校验 BR-003: 诊断+处方标记+药材<br/>Registration.Status → Completed
        S-->>D: MedicalCase (Completed)

        Note over S: 隔天自动锁定 (FR-MC-014)<br/>IsLocked = CompletedAt.Date < Today
    end
```

### 1.2 简化流程图

> **v2.0 更新**: 增加两种入口模式收敛点

```mermaid
flowchart TD
    A[患者到达] --> QueryPatient["查询患者\n(模糊搜索/身份证/读卡器)"]
    QueryPatient --> Found{找到患者?}
    Found -->|已存在| C[加载患者信息]
    Found -->|不存在| D[创建新患者]
    D --> C

    C --> RoleCheck{当前角色?}
    RoleCheck -->|Receptionist| Reg["挂号: 指派医生\n(Registration.Status=Waiting)"]
    RoleCheck -->|Doctor| DirectCreate[直接选择患者]

    Reg -.->|医生侧| DoctorQueue["从挂号队列选中"]
    DoctorQueue --> E
    DirectCreate --> E

    E["创建医案"] -->|BR-001 碰撞检查| F[填写诊断]
    F --> G{需要处方?}
    G -->|true| H[开具处方]
    G -->|false| K[完成医案]
    G -->|null 未决策| F

    H --> I[聚合保存]
    I --> K[完成医案]

    K -->|BR-003 校验| M[医案 Completed]
    M -->|当天| N[可编辑 需EditReason]
    M -->|隔天| O[自动锁定 IsLocked=true]

    I -.->|可选 独立于完成| L[打印<br/>MedicalCase 聚合根能力]

    style A fill:#e1f5fe
    style Reg fill:#e3f2fd
    style E fill:#e8f5e9
    style H fill:#fff3e0
    style L fill:#fce4ec
    style M fill:#f3e5f5
```

---

## 二、阶段详解

### 2.1 前台挂号 (Receptionist)

| 步骤 | 操作 | PRD | 关键约束 |
|------|------|-----|---------|
| 1 | 连接身份证读卡器 | FR-CARD-001 | CardReadResult.IsSuccess 校验 |
| 2 | 读取身份证信息 | FR-CARD-001 | 姓名/性别/民族/出生日期/身份证号/住址 |
| 3 | 按身份证号查询患者 | FR-CARD-002 | IdNumber 唯一性 (PAT-D03) |
| 4a | 已存在: 加载患者，显示 LastVisitTime + VisitCount | FR-CARD-002 | Patient.Status 检查 |
| 4b | 不存在: 快速创建 (自动映射身份证字段) | FR-PAT-001 | 手机号必填 + 唯一 (ERR-20003) |

**Receptionist 权限边界**: 患者 CRU (无删除)、读卡器使用、查看未完成医案简要提示 (仅时间+医生，无诊断/处方详情)。

### 2.2 创建医案 (Doctor)

| 步骤 | 操作 | PRD | 关键约束 |
|------|------|-----|---------|
| 1 | 选择患者，创建医案 | FR-MC-001 | 仅 Doctor 角色可创建 |
| 2 | 系统校验: 患者状态 | FR-MC-001 | Patient.Status=Enabled (ERR-30105) |
| 3 | 系统校验: 单活跃医案约束 | BR-001 | Suspended -> ERR-30104, Active -> ERR-30103 |
| 4 | 系统创建 MedicalCase (Active) + Consultation (1:1 共享主键) | FR-MC-001 | CaseNumber 自动生成: MC+yyyyMMdd+序号 |
| 5 | 冗余存储 PatientName, DoctorName | - | 读优化，患者禁用时按角色脱敏 (MC-D16) |

### 2.3 填写诊断 (Doctor)

| 步骤 | 操作 | PRD | 关键约束 |
|------|------|-----|---------|
| 1 | 填写中医辨证 (TcmDiagnosis) | FR-MC-002 | **完成时必填** |
| 2 | 填写现病史/舌诊/脉诊 | FR-MC-002 | 可选字段 |
| 3 | 保存诊断数据 | FR-MC-002 | 状态保持 Active |
| 4 | 或挂起医案 (状态转为 Suspended) | FR-MC-006 | TcmDiagnosis 可空，稍后继续 |

### 2.4 处方决策与开具 (Doctor)

**处方需求标记 (FR-MC-003)**:
- `true`: 需要处方 -> 允许创建/编辑 Prescription
- `false`: 不需要处方 -> 删除已有 Prescription
- `null`: 未决策 -> 完成时报错 ERR-30302

**三种处方来源**:

| 来源 | PRD | 关键规则 |
|------|-----|---------|
| 验方导入 | FR-MC-016, FR-FORM-* | 仅 Validated + Enabled 验方可用 (MC-D08); 禁用药材自动跳过+提示 (MC-D09); 数据复制 (MC-D12) |
| 历史处方复制 | FR-MC-018 | 仅 Completed 医案; 价格从药材库实时获取 (MC-D13); 禁用药材跳过 (MC-D09) |
| 手工输入 | FR-MC-004 | 直接编辑药材列表 |

**费用计算 (MC-D14)**:

```
Item.Amount = UnitPrice x Dosage
SingleDosePrice = SUM(Items.Amount)
TotalPrice = SingleDosePrice x DosageCount x Discount
```

### 2.5 聚合保存 (FR-MC-005)

医案采用聚合根模式，MedicalCase + Consultation + Prescription + PrescriptionItems 原子保存。

**EditReason 判断矩阵**:

| 场景 | 需要 EditReason |
|------|:---:|
| 当天本人修改 Active/Suspended | 否 |
| 修改 Completed 医案 | 是 |
| 隔天修改 | 是 |
| 非本人修改 | 是 |
| IsPrinted=true 时修改诊断/处方 | 是 (ERR-30403) |

**打印保护 (MC-D15)**:
- IsPrinted=true 且诊断/处方有变更 -> 需 EditReason
- 保存成功后: IsPrinted 重置为 false, PrintVersion++
- 需要重新打印

**并发控制**: 乐观锁 RowVersion, 3 次重试后返回 ERR-30502

### 2.6 打印 (FR-PRINT-001~004)

> 打印是 MedicalCase 聚合根的能力，v1.0 支持处方打印 (PrintType=Prescription)。IsPrinted 和 PrintVersion 均在 MedicalCase 上，打印日志通过 MedicalCasePrintLog 统一管理。

| 步骤 | 操作 | 说明 |
|------|------|------|
| 1 | 打印预览 | 左: 打印机/份数/纸张; 右: FixedDocument 预览 |
| 2 | 确认打印 | MedicalCase.IsPrinted=true, MedicalCase.PrintCount++, MedicalCase.LastPrintedAt=now |
| 3 | 生成 MedicalCasePrintLog | 记录 PrintType + 打印版本/操作人/打印机/结果 |

**排版规格**: A5 (148mm x 210mm) 默认, A4 可选; 药材 <=12 味单页, >12 味自动分页。

### 2.7 完成医案 (FR-MC-007)

**完成校验 (BR-003)**:

| 校验项 | 条件 | 错误码 |
|--------|------|--------|
| 中医辨证 | TcmDiagnosis 非空 | ERR-30301 |
| 处方需求标记 | NeedsPrescription 非 null | ERR-30302 |
| 处方存在性 | NeedsPrescription=true 时 Prescription 非 null | ERR-30303 |
| 处方药材数量 | Items.Count > 0 | ERR-30304 |
| 处方帖数 | DosageCount > 0 | ERR-30305 |

### 2.8 医案锁定 (FR-MC-014)

```
IsLocked = IsCompleted AND CompletedAt.Date < Today
```

- **计算属性**, 无后台任务，每次查询实时计算
- Doctor: 不可编辑锁定医案 (ERR-30201)
- Admin: 可编辑，需提供 EditReason

---

## 三、MedicalCase 状态机

```mermaid
stateDiagram-v2
    [*] --> Active: 创建医案 (FR-MC-001)
    Active --> Suspended: 挂起 (FR-MC-006)
    Suspended --> Active: 恢复诊疗
    Active --> Completed: 完成看诊 (FR-MC-007)
    Suspended --> Completed: 完成看诊 (FR-MC-007)
    Active --> [*]: 取消/软删除 (FR-MC-008)
    Suspended --> [*]: 取消/软删除 (FR-MC-008)
    Completed --> [*]

    state Completed {
        [*] --> Editable: 当天
        Editable --> Locked: CompletedAt.Date < Today
        Locked --> [*]

        note right of Editable: Doctor可编辑(需EditReason)
        note right of Locked: Doctor无权<br/>Admin需EditReason
    }
```

> **取消操作**: 不再有独立的 Cancelled 状态。取消医案通过 `IsDeleted=true` 软删除实现，聚合根域方法 `MedicalCase.SoftDelete()` 统一处理。已完成的医案不可取消。

---

## 四、跨模块交互矩阵

```mermaid
graph LR
    subgraph 前台
        CR[读卡器<br/>card-reader]
        PAT[患者管理<br/>patients]
        REG[挂号管理<br/>registration]
    end

    subgraph 诊疗
        MC[医案管理<br/>medical-cases]
        FORM[验方管理<br/>formulas]
        HERB[药材管理<br/>herbs]
    end

    subgraph 输出
        PRINT[打印模块<br/>printing]
    end

    CR -->|身份证信息| PAT
    PAT -->|PatientId| REG
    REG -->|PatientId + DoctorId<br/>挂号队列| MC
    PAT -->|PatientId + 状态检查| MC
    FORM -->|验方导入 MC-D08| MC
    HERB -->|药材价格 + 状态| MC
    MC -->|医案打印能力 IsPrinted+PrintVersion| PRINT
    MC -->|历史处方复制 MC-D13| MC

    style CR fill:#e3f2fd
    style PAT fill:#e3f2fd
    style REG fill:#e3f2fd
    style MC fill:#e8f5e9
    style FORM fill:#fff8e1
    style HERB fill:#fff8e1
    style PRINT fill:#fce4ec
```

### 交互约束汇总

| 来源模块 | 目标模块 | 交互点 | 约束 | PRD |
|---------|---------|--------|------|-----|
| card-reader | patients | 身份证信息映射 | IdNumber 唯一性 | FR-CARD-002, PAT-D03 |
| patients | registration | 挂号 | PatientId + DoctorId 必填 | FR-REG-001 |
| registration | medical-cases | 从挂号队列创建医案 | Status=Waiting → InProgress | FR-REG-002 |
| patients | medical-cases | 创建医案 (医生直接) | Status=Enabled, 单活跃约束 | FR-MC-001, BR-001 |
| patients | medical-cases | 删除保护 | 有医案禁删 | MC-D04, ERR-20004 |
| patients | medical-cases | 禁用联动 | 禁止创建新医案 | MC-D16, ERR-30105 |
| formulas | medical-cases | 验方导入 | Validated + Enabled, 重复药材合并 | MC-D08, MC-D17, FR-MC-016 |
| herbs | medical-cases | 药材状态检查 | 禁用药材跳过+提示 | MC-D09 |
| herbs | medical-cases | 价格获取 | 实时价格 (非快照) | MC-D13, MC-D14 |
| medical-cases | registration | 医案完成联动 | Registration.Status → Completed | FR-REG-003 |
| medical-cases | printing | 医案打印 (v1.0: 处方打印) | MedicalCase.IsPrinted/PrintVersion + MedicalCasePrintLog | MC-D15, FR-PRINT-001 |

---

## 五、角色视角工作流

### 5.1 Receptionist 日常操作

```
登录 -> 患者管理 + 挂号管理
  ├─ 查询患者 (模糊搜索/身份证匹配/读卡器)
  │   ├─ 已存在 -> 加载患者信息
  │   └─ 不存在 -> 创建新患者 (FR-PAT-001)
  ├─ 挂号: 选择患者 + 指派医生 -> 创建 Registration (Status=Waiting)
  ├─ 患者信息编辑 (CRU, 无删除)
  └─ 查看今日挂号队列
```

### 5.2 Doctor 日常操作

```
登录 -> 首页 (挂号队列 + 进行中医案)
  ├─ 模式 1: 从挂号队列选中患者 -> 创建医案 -> 诊断 -> 处方 -> 保存 -> 打印 -> 完成
  ├─ 模式 2: 直接查询患者 -> 创建医案 (前台不在时)
  ├─ 恢复挂起的 Suspended 医案
  ├─ 查看/复制历史医案处方
  └─ 管理个人验方
```

### 5.3 Admin 日常操作

```
登录 -> 管理模块
  ├─ 药材管理 (CRUD + 导入导出)
  ├─ 验方管理 (CRUD + 共享管理)
  ├─ 医案审核 (编辑任意医案, 含锁定医案)
  ├─ 用户管理 (CRUD, 不含 SuperAdmin)
  └─ 数据同步
```

---

## 六、异常路径与分支流程

> **v2.0 新增**: 补全正常路径之外的异常分支、决策流程。

### 6.1 BR-001 碰撞处理流程

创建医案时，如果发现该患者已有 Active 或 Suspended 状态的医案，需要三选一处理。

```mermaid
flowchart TD
    Start["Doctor: 创建医案"] --> Select["患者选择列表\n(仅 Status=Enabled 可见)"]
    Select --> PatientSelected["选中患者"]

    PatientSelected --> BR001{"BR-001 检查\n该患者是否有\nActive/Suspended 医案?"}

    BR001 -->|"无"| Create["创建新医案 (Active)\n+ Consultation 自动创建\n+ CaseNumber 生成\n+ 冗余存储 PatientName/DoctorName"]

    BR001 -->|"有 Suspended (ERR-30104)"| ShowSuspended["弹窗:\n'该患者已有挂起的医案\n请先处理现有医案'"]
    BR001 -->|"有 Active (ERR-30103)"| ShowActive["弹窗:\n'该患者已有进行中的医案\n请先完成现有医案'"]

    ShowSuspended --> Choice{"用户选择"}
    ShowActive --> Choice

    Choice -->|"重开现有医案"| Navigate["导航到已有医案\n继续编辑"]
    Choice -->|"关闭旧的后新建"| SoftDel["旧医案 SoftDelete\n(IsDeleted=true)\n审计: SoftDelete"] --> Create
    Choice -->|"取消操作"| Cancel["取消\n返回来源页面"]

    Create --> Edit["进入医案编辑\n(Clinical 模式)"]

    Note["API 兜底防护:\nPatient.Status != Enabled\n→ ERR-30105\n(正常流程不触发:\n禁用患者在选择列表中不可见)"]

    style Create fill:#c8e6c9
    style Cancel fill:#f5f5f5
    style Note fill:#fff9c4,stroke-dasharray: 5 5
```

**设计要点:**
- 禁用患者在患者选择列表中已被 UI 过滤，用户选不到禁用患者
- ERR-30105 是 API 层兜底防护，正常流程不触发
- "关闭旧的后新建"触发 SoftDelete 审计记录

### 6.2 BR-002 离开界面决策流程

医生离开医案编辑界面时，根据编辑模式 (Clinical/Management) 展示不同的处置选项。

```mermaid
flowchart TD
    Leave["医生触发离开\n(导航/关闭/返回)"] --> DirtyCheck{"有未保存变更?\n(HasUnsavedChanges)"}

    DirtyCheck -->|"无变更"| DirectLeave["直接离开\n无弹窗"]

    DirtyCheck -->|"有变更"| ModeCheck{"编辑模式?"}

    ModeCheck -->|"Clinical 模式"| ClinicalDialog["弹窗:\n[挂起] [关闭(取消)] [完成看诊]"]
    ModeCheck -->|"Management 模式"| MgmtDialog["弹窗:\n[保存] [放弃] [取消离开]"]

    ClinicalDialog -->|"挂起"| Suspend["Suspend (FR-MC-006)\nStatus=Suspended\n保存当前数据\nTcmDiagnosis 可空"]
    ClinicalDialog -->|"关闭"| ConfirmClose{"确认关闭?\n'数据将被保留\n但医案将被取消'"}
    ClinicalDialog -->|"完成看诊"| Complete["CompleteAsync (FR-MC-007)\n校验 BR-003"]

    ConfirmClose -->|"确认"| SoftDelete["SoftDelete\n(IsDeleted=true)\n审计: SoftDelete"]
    ConfirmClose -->|"取消"| StayEdit["返回编辑"]

    Complete --> CompleteCheck{"BR-003 校验通过?"}
    CompleteCheck -->|"通过"| Completed["医案完成\nRegistration.Status=Completed"]
    CompleteCheck -->|"失败"| ShowErrors["显示校验错误\n返回编辑"]

    MgmtDialog -->|"保存"| Save["聚合保存 (FR-MC-005)\n含权限/打印保护检查"]
    MgmtDialog -->|"放弃"| Discard["放弃变更\n直接离开"]
    MgmtDialog -->|"取消离开"| StayEdit

    Suspend --> LeaveSuccess["离开: 返回患者选择/待诊队列"]
    SoftDelete --> LeaveSuccess
    Completed --> LeaveSuccess
    Save --> SaveCheck{"保存成功?"}
    SaveCheck -->|"成功"| LeaveMgmt["离开: 返回医案列表"]
    SaveCheck -->|"失败"| ShowSaveError["显示错误\n返回编辑"]
    Discard --> LeaveMgmt
    DirectLeave --> AutoLeave["返回来源页面"]

    style ClinicalDialog fill:#e3f2fd
    style MgmtDialog fill:#e8f5e9
    style SoftDelete fill:#ffcdd2
    style Completed fill:#c8e6c9
```

**Clinical 模式 vs Management 模式:**

| 维度 | Clinical 模式 | Management 模式 |
|------|--------------|----------------|
| 入口 | 从患者选择/待诊队列进入 | 从医案列表进入 |
| 默认状态 | Editing | ReadOnly |
| 离开选项 | 挂起 / 关闭 / 完成看诊 | 保存 / 放弃 / 取消离开 |
| 返回目标 | 患者选择/待诊队列 | 医案列表 |
| 崩溃处理 | 未保存变更丢失 | 未保存变更丢失 |

**崩溃场景**: 崩溃/断网/强制关闭统一做变更丢失处理 (无自动保存机制)。医案保持最后一次成功保存的状态。

### 6.3 并发冲突处理

两个用户同时编辑同一医案时，通过乐观锁 (RowVersion) + 3 次重试机制处理。

```mermaid
sequenceDiagram
    participant D1 as 医生A
    participant D2 as 管理员B (同时编辑)
    participant API as Server
    participant DB as Database

    D1->>API: GET /medicalcases/{id}
    API-->>D1: MedicalCase (RowVersion=V1)

    D2->>API: GET /medicalcases/{id}
    API-->>D2: MedicalCase (RowVersion=V1)

    Note over D1, D2: 两人同时加载了同一版本

    D1->>API: PUT /medicalcases/{id} (RowVersion=V1)
    API->>DB: UPDATE ... WHERE RowVersion=V1
    DB-->>API: 成功 (RowVersion → V2)
    API-->>D1: 200 OK (新 RowVersion=V2)

    D2->>API: PUT /medicalcases/{id} (RowVersion=V1)
    API->>DB: UPDATE ... WHERE RowVersion=V1
    DB-->>API: DbUpdateConcurrencyException (RowVersion 已变为 V2)

    loop 重试 (最多3次)
        API->>DB: 重新读取最新数据
        DB-->>API: MedicalCase (RowVersion=V2)
        API->>DB: UPDATE ... WHERE RowVersion=V2
        alt 成功
            DB-->>API: OK
            API-->>D2: 200 OK
        else 仍然冲突
            DB-->>API: ConcurrencyException
        end
    end

    alt 3次重试全部失败
        API-->>D2: ERR-30502 "保存失败，请稍后重试"
        D2->>D2: 刷新页面，重新加载最新数据
    end
```

**关键规则:**
- 乐观锁通过 RowVersion (timestamp) 实现
- 3 次重试机制: 每次重试自动读取最新版本再尝试
- NFR 约束: 系统 1-3 并发用户，冲突概率极低

---

## 七、子流程详解

### 7.1 验方/历史处方导入

验方导入 (FR-MC-016) 和历史处方复制 (FR-MC-018) 共享统一的药材导入逻辑，差异仅在来源。

```mermaid
sequenceDiagram
    participant D as 医生
    participant VM as 处方药材编辑区
    participant Src as 来源 (验方/历史处方)
    participant HS as HerbService

    Note over D, HS: 统一导入流程 (验方/历史处方共用)

    D->>Src: 选择来源 (验方 或 历史处方)
    Src-->>D: 预览药材列表

    D->>VM: 确认导入

    loop 逐个药材处理
        VM->>HS: 检查药材状态 + 获取当前价格

        alt 药材已禁用 (MC-D09)
            HS-->>VM: Status=Disabled
            VM->>VM: 跳过该药材，记录到跳过列表
        else 药材启用
            HS-->>VM: 当前价格
            VM->>VM: 检查重复 (编辑区是否已有相同 HerbId)
            alt 已有相同 HerbId
                VM->>VM: 按 DuplicateHerbStrategy 合并 (MC-D17)
                VM->>VM: 记录到合并列表
            else 不重复
                VM->>VM: 添加到编辑区
            end
        end
    end

    alt 有跳过的药材
        VM-->>D: 提示: "以下药材已停用，已跳过: xxx"
    end
    alt 有合并的药材
        VM-->>D: 提示: "以下药材已存在，已合并: yyy (策略: Accumulate)"
    end

    VM->>VM: 重新计算 SingleDosePrice / TotalPrice
    VM-->>D: 显示更新后的药材列表

    Note over D: 可多次导入 (验方A + 验方B + 历史处方)，每次导入都做重复校验
```

**重复药材剂量合并策略 (MC-D17):**

通过 `appsettings.json` 配置 `PrescriptionImport.DuplicateHerbStrategy`:

| 策略 | 行为 | 示例 (已有=5g, 导入=3g) |
|------|------|------|
| `Max` | 取最大剂量 | 结果=5g |
| `Min` | 取最小剂量 | 结果=3g |
| `Accumulate` | 累加 (默认) | 结果=8g |
| `Skip` | 跳过，保持原值 | 结果=5g (不变) |
| `Replace` | 替换为导入值 | 结果=3g |

合并时仅更新 Dosage，DecocteMethod 和 Unit 保持原有值不变。

**验方导入 vs 历史处方复制差异:**

| 维度 | 验方导入 (FR-MC-016) | 历史处方复制 (FR-MC-018) |
|------|---------------------|------------------------|
| 来源 | 验方库 (Validated + Enabled) | 同一患者 Completed 医案 |
| 价格 | 药材库当前价格 | 药材库当前价格 (非历史) |
| 额外字段 | 无 | DosageCount/Discount/Usage/Advice 从历史复制 |
| ReferencedFormulas | `{"type":"formula",...}` | `{"type":"history",...}` |

### 7.2 打印保护完整流程

覆盖从首次打印到修改后重新打印的完整生命周期。

```mermaid
flowchart TD
    subgraph PrintFlow["打印流程"]
        P1["医生: 打印预览 (FR-PRINT-002)"]
        P1 --> P2["确认打印 (FR-PRINT-001)"]
        P2 --> P3["系统更新:\nMedicalCase.IsPrinted=true\nMedicalCase.PrintCount++\nMedicalCase.LastPrintedAt=now"]
        P3 --> P4["生成 MedicalCasePrintLog\nPrintType=Prescription\nPrintVersion=当前版本"]
    end

    P4 --> EditAttempt{"后续修改尝试?"}
    EditAttempt -->|"无修改"| Done["流程结束"]
    EditAttempt -->|"修改诊断或处方"| PrintProtection{"打印保护检查 (MC-D15)\nIsPrinted == true?"}

    PrintProtection -->|"IsPrinted=false"| NormalSave["正常保存 (无额外要求)"]
    PrintProtection -->|"IsPrinted=true"| NeedReason{"EditReason 是否提供?"}

    NeedReason -->|"未提供"| RejectSave["ERR-30403\n'医案已打印，修改需要提供修改原因'"]
    RejectSave --> PromptReason["弹窗: 输入修改原因\n(预置选项 + 自由输入)"]
    PromptReason --> NeedReason

    NeedReason -->|"已提供"| SaveWithReason["聚合保存 (含 EditReason)"]
    SaveWithReason --> ResetPrint["系统重置:\nMedicalCase.IsPrinted=false\nMedicalCase.PrintVersion++"]
    ResetPrint --> NeedReprint["提示: '内容已修改，需要重新打印'\n(PrintVersion 已递增)"]

    NeedReprint --> ReprintChoice{"医生选择"}
    ReprintChoice -->|"立即重新打印"| P1
    ReprintChoice -->|"稍后打印"| Done2["流程结束\nIsPrinted=false\n下次查看时可见 '未打印' 标记"]

    style PrintFlow fill:#fce4ec,stroke:#c62828
    style RejectSave fill:#ffcdd2
    style ResetPrint fill:#fff9c4
    style NeedReprint fill:#fff3e0
```

**打印版本追踪:**

```
初始:     PrintVersion=1, IsPrinted=false
首次打印: PrintVersion=1, IsPrinted=true,  PrintCount=1, PrintLog(V=1)
修改内容: PrintVersion=2, IsPrinted=false
再次打印: PrintVersion=2, IsPrinted=true,  PrintCount=2, PrintLog(V=2)
再次修改: PrintVersion=3, IsPrinted=false
```

### 7.3 编辑模式切换

Medical Case 编辑界面支持 Clinical 模式和 Management 模式两种工作方式。

```mermaid
stateDiagram-v2
    state "Clinical 模式" as Clinical {
        [*] --> C_Editing: 从患者选择/待诊进入
        note right of C_Editing
            默认 Editing
            底部: [挂起] [打印] [完成看诊]
        end note
        C_Editing --> C_Saved: 聚合保存成功
        C_Saved --> C_Editing: 继续编辑
        C_Editing --> [*]: 离开 (BR-002)
    }

    state "Management 模式" as Management {
        [*] --> M_ReadOnly: 从医案列表进入
        note right of M_ReadOnly
            默认 ReadOnly
            底部: [编辑医案] [打印]
        end note
        M_ReadOnly --> M_Editing: 点击"编辑医案"
        note right of M_Editing
            底部: [保存医案] [取消编辑] [打印]
        end note
        M_Editing --> M_ReadOnly: 保存成功 / 取消编辑
        M_Editing --> [*]: 离开 (BR-002 弹窗)
        M_ReadOnly --> [*]: 直接离开 (无弹窗)
    }
```

| 模式 | 状态 | 底部按钮 |
|------|------|----------|
| Clinical | Editing | [挂起医案] [打印处方笺] [完成看诊] |
| Management | ReadOnly | [编辑医案] [打印处方笺] |
| Management | Editing | [保存医案] [取消编辑] [打印处方笺] |

---

## 八、跨模块联动影响

### 8.1 药材禁用联动

```mermaid
flowchart TD
    HD1["Admin: 禁用药材 (Status=Disabled)"]
    HD1 --> HD2["影响范围"]
    HD2 --> HD3["新建处方: 药材选择列表\n自动过滤禁用药材"]
    HD2 --> HD4["验方导入: 禁用药材\n自动跳过 + 提示 (MC-D09)"]
    HD2 --> HD5["历史处方复制: 禁用药材\n自动跳过 + 提示 (MC-D09)"]
    HD2 --> HD6["历史处方查看: 名称标注\n'(已停用)' (MC-D07)"]
    HD2 --> HD7["已有处方中含该药材:\n不影响已保存数据\n编辑时该药材不可修改剂量"]

    style HD1 fill:#fff3e0,stroke:#e65100
```

### 8.2 Registration 医案取消联动

医案取消时，Registration 根据 Source 执行不同的状态回退策略:

| 触发事件 | Source=Receptionist | Source=Doctor |
|---------|-------------------|---------------|
| 医案创建 | Status -> InProgress | (创建即 InProgress) |
| 医案 Completed | Status -> Completed | Status -> Completed |
| 医案 Cancelled | Status -> Waiting (等前台取消); MedicalCaseId 清空 | Status -> Cancelled (自动) |

**取消挂号前置校验 (REG-BR-001)**: 无关联医案 OR 关联医案状态为 Cancelled。有 Active/Suspended/Completed 医案时拒绝取消。

**设计理由**: 职责对等 -- 前台发起的流程由前台闭环 (回退 Waiting 后手动取消)；医生自动创建的由系统自动闭环 (直接 Cancelled)。

### 8.3 患者禁用联动

```mermaid
flowchart TD
    PD1["Admin: 禁用患者 (Status=Disabled)"]
    PD1 --> PDCheck{"有 Active/Suspended 医案?"}
    PDCheck -->|"有"| PDReject["拒绝禁用 (FR-PAT-013)\n需先完成或取消活跃医案"]
    PDCheck -->|"无"| PD2["禁用成功"]
    PD2 --> PD3["影响范围"]
    PD3 --> PD4["患者选择列表: 不可见 (UI 过滤)"]
    PD3 --> PD5["创建医案: ERR-30105 (API 兜底)"]
    PD3 --> PD6["历史医案查阅: 允许\nPatientName 按角色脱敏"]
    PD3 --> PD7["Receptionist 查询: 禁用患者不可见"]

    PD6 --> PD6A["Admin/SuperAdmin: 完整姓名"]
    PD6 --> PD6B["Doctor: 掩码 '张*'"]

    style PD1 fill:#e3f2fd,stroke:#1565c0
    style PDReject fill:#ffcdd2
```

---

## 九、相关文档索引

| 文档 | 路径 | 关系 |
|------|------|------|
| 患者管理 PRD | [patients.md](../02-requirements/patients.md) | 前台挂号流程 |
| 医案管理 PRD | [medical-cases.md](../02-requirements/medical-cases.md) | 诊疗核心流程 |
| 验方管理 PRD | [formulas.md](../02-requirements/formulas.md) | 处方导入来源 |
| 药材管理 PRD | [herbs.md](../02-requirements/herbs.md) | 药材价格和状态 |
| 打印管理 PRD | [printing.md](../02-requirements/printing.md) | 打印处方笺 |
| 读卡器 PRD | [card-reader.md](../02-requirements/card-reader.md) | 身份证读卡 |
| 桌面端架构 | [desktop.md](../03-architecture/desktop.md) | UI 架构和导航 |
| 用户角色 | [user-roles.md](user-roles.md) | 角色权限定义 |
| 数据模型 | [data-model.md](../03-architecture/data-model.md) | 实体关系 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-21 | v1.0 | 初始版本，覆盖 GAP-FLOW-1 端到端临床工作流 |
| 2026-02-21 | v1.1 | 打印层级提升: Section 2.6 补充医案级打印能力说明; PrescriptionPrintLog->MedicalCasePrintLog; 交互矩阵 MC->PRINT 描述更新 |
| 2026-02-21 | v1.2 | 深度重构同步: 状态机移除 Cancelled (取消=软删除); 状态机补充 Draft->Completed 转换; 补充聚合根域方法说明 |
| 2026-02-21 | v2.0 | **设计深化**: (1) 新增两种入口模式 (前台挂号/医生直接); (2) 新增 Registration 独立挂号模块 (DoctorId 必填); (3) Section 六: 异常路径 (BR-001 碰撞处理/BR-002 离开界面/并发冲突); (4) Section 七: 子流程 (验方导入含重复药材合并策略 MC-D17/打印保护完整流程/编辑模式切换); (5) Section 八: 跨模块联动影响 (药材禁用/患者禁用); (6) 交互矩阵新增 Registration 模块 |
| 2026-02-22 | v2.1 | **Draft→Suspended (MC-D20)**: 全文 Draft 替换为 Suspended; 状态机 `[*]→Active↔Suspended→Completed`; BR-001 碰撞处理 Draft→Suspended; BR-002 离开界面 SaveDraft→Suspend; 患者禁用联动 Draft/Active→Active/Suspended; 创建医案初始状态 Active |
| 2026-03-06 | v2.2 | Section 八新增 8.2 Registration 医案取消联动 (Source 分流策略 + REG-BR-001 校验); 原 8.2 患者禁用联动重编号为 8.3 |

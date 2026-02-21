# 端到端临床工作流

> **版本**: v1.0
> **创建日期**: 2026-02-21
> **目的**: 串联从患者到达到离开的完整临床流程，统一展示跨模块交互
> **覆盖缺口**: GAP-FLOW-1 (docs/plans/2026-02-21-prd-design-gap-analysis.md)

---

## 一、流程全景

### 1.1 端到端时序图

```mermaid
sequenceDiagram
    participant R as 前台 (Receptionist)
    participant S as 系统
    participant D as 医生 (Doctor)
    participant P as 打印

    rect rgb(240, 248, 255)
        Note over R, S: 阶段 1: 前台挂号
        R->>S: 读取身份证 (FR-CARD-001)
        S-->>R: CardReadResult (姓名/性别/身份证号...)
        R->>S: 查询患者 (FR-CARD-002)
        alt 患者已存在
            S-->>R: PatientFromCardResult (IsNewlyCreated=false)
        else 患者不存在
            R->>S: 快速创建患者 (FR-PAT-001)
            S-->>R: PatientFromCardResult (IsNewlyCreated=true)
        end
    end

    rect rgb(245, 255, 245)
        Note over D, S: 阶段 2: 诊疗
        D->>S: 创建医案 (FR-MC-001)
        Note right of S: 校验: Patient.Status=Enabled<br/>校验: 无活跃医案 (BR-001)
        S-->>D: MedicalCase (Draft) + Consultation

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
            D->>S: 导入药材到处方
            Note right of S: 禁用药材自动跳过 (MC-D09)<br/>数据复制，不影响原验方 (MC-D12)
        else 历史处方复制 (FR-MC-018)
            D->>S: 获取患者历史 Completed 医案
            D->>S: 复制处方 (价格实时获取 MC-D13)
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
        Note right of S: 校验 BR-003: 诊断+处方标记+药材
        S-->>D: MedicalCase (Completed)

        Note over S: 隔天自动锁定 (FR-MC-014)<br/>IsLocked = CompletedAt.Date < Today
    end
```

### 1.2 简化流程图

```mermaid
flowchart TD
    A[患者到达] --> B{身份证读卡}
    B -->|已存在| C[加载患者信息]
    B -->|不存在| D[快速创建患者]
    C --> E[医生: 创建医案]
    D --> E

    E -->|BR-001 校验| F[填写诊断]
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
| 3 | 系统校验: 单活跃医案约束 | BR-001 | Draft -> ERR-30104, Active -> ERR-30103 |
| 4 | 系统创建 MedicalCase (Draft) + Consultation (1:1 共享主键) | FR-MC-001 | CaseNumber 自动生成: MC+yyyyMMdd+序号 |
| 5 | 冗余存储 PatientName, DoctorName | - | 读优化，患者禁用时按角色脱敏 (MC-D16) |

### 2.3 填写诊断 (Doctor)

| 步骤 | 操作 | PRD | 关键约束 |
|------|------|-----|---------|
| 1 | 填写中医辨证 (TcmDiagnosis) | FR-MC-002 | **完成时必填** |
| 2 | 填写现病史/舌诊/脉诊 | FR-MC-002 | 可选字段 |
| 3 | 保存 -> 状态转为 Active | FR-MC-002 | 首次保存诊断触发 Draft -> Active |
| 4 | 或暂存草稿 (状态保持 Draft) | FR-MC-006 | TcmDiagnosis 可空 |

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
| 当天本人修改 Draft/Active | 否 |
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
| 2 | 确认打印 | MedicalCase.IsPrinted=true, Prescription.PrintCount++, LastPrintedAt=now |
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
    [*] --> Draft: 创建医案 (FR-MC-001)
    Draft --> Active: 保存诊断 (FR-MC-002)
    Draft --> Completed: 完成看诊 (FR-MC-007)
    Draft --> [*]: 取消/软删除 (FR-MC-008)
    Active --> Completed: 完成看诊 (FR-MC-007)
    Active --> [*]: 取消/软删除 (FR-MC-008)
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
    PAT -->|PatientId + 状态检查| MC
    FORM -->|验方导入 MC-D08| MC
    HERB -->|药材价格 + 状态| MC
    MC -->|医案打印能力 IsPrinted+PrintVersion| PRINT
    MC -->|历史处方复制 MC-D13| MC

    style CR fill:#e3f2fd
    style PAT fill:#e3f2fd
    style MC fill:#e8f5e9
    style FORM fill:#fff8e1
    style HERB fill:#fff8e1
    style PRINT fill:#fce4ec
```

### 交互约束汇总

| 来源模块 | 目标模块 | 交互点 | 约束 | PRD |
|---------|---------|--------|------|-----|
| card-reader | patients | 身份证信息映射 | IdNumber 唯一性 | FR-CARD-002, PAT-D03 |
| patients | medical-cases | 创建医案 | Status=Enabled, 单活跃约束 | FR-MC-001, BR-001 |
| patients | medical-cases | 删除保护 | 有医案禁删 | MC-D04, ERR-20004 |
| patients | medical-cases | 禁用联动 | 禁止创建新医案 | MC-D16, ERR-30105 |
| formulas | medical-cases | 验方导入 | Validated + Enabled | MC-D08, FR-MC-016 |
| herbs | medical-cases | 药材状态检查 | 禁用药材跳过+提示 | MC-D09 |
| herbs | medical-cases | 价格获取 | 实时价格 (非快照) | MC-D13, MC-D14 |
| medical-cases | printing | 医案打印 (v1.0: 处方打印) | MedicalCase.IsPrinted/PrintVersion + MedicalCasePrintLog | MC-D15, FR-PRINT-001 |

---

## 五、角色视角工作流

### 5.1 Receptionist 日常操作

```
登录 -> 患者管理模块
  ├─ 身份证读卡 -> 查询/创建患者
  ├─ 患者信息编辑 (CRU, 无删除)
  └─ 查看今日未完成医案简要 (时间+医生)
```

### 5.2 Doctor 日常操作

```
登录 -> 首页 (今日待诊列表)
  ├─ 选择患者 -> 创建医案 -> 诊断 -> 处方 -> 保存 -> 打印 -> 完成
  ├─ 继续编辑暂存的 Draft 医案
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

## 六、相关文档索引

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

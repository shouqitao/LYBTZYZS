# 看诊流程核心实体关系架构文档

> **文档版本**: v1.1
> **创建日期**: 2025-10-18
> **最后更新**: 2025-10-26
> **状态**: ⭐⭐⭐ **权威文档** ✅ 已确认（基于用户深度澄清）

## 📌 权威文档声明

**本文档是MedicalCase/Consultation/Prescription实体关系的唯一权威定义**。

**其他文档引用规则**：
- **Client架构文档**（`docs/explanation/architecture/client/README.md`）：从MVVM聚合根模式视角引用本文档
- **Server架构文档**（`docs/explanation/architecture/server/README.md`）：从Repository/Service视角引用本文档
- **禁止重复**：其他文档应通过链接引用本文档，避免重复描述实体关系

**文档分工**：
- **本文档**：业务流程视角的实体关系（What & Why）
- **Client README**：MVVM实现视角（How in Desktop）
- **Server README**：API实现视角（How in WebAPI）

---

## 📋 目录

- [1. 文档目的](#1-文档目的)
- [2. 核心澄清问题总结](#2-核心澄清问题总结)
- [3. 核心实体定义](#3-核心实体定义)
- [4. 实体关系图](#4-实体关系图)
- [5. 数据模型设计](#5-数据模型设计)
- [6. 状态机设计](#6-状态机设计)
- [7. 完整工作流](#7-完整工作流)
- [8. 架构决策记录](#8-架构决策记录)
- [9. 实施路线图](#9-实施路线图)

---

## 1. 文档目的

本文档旨在**彻底搞清楚**看诊流程中各个实体（Patient、Registration、MedicalCase、Consultation、Prescription）的关系，避免后续开发中的混淆和误解。

**目标受众**：
- 全栈开发人员
- 架构师
- 产品经理
- 测试工程师

**适用范围**：
- Server端：API设计、数据库建模
- Client端：ViewModel设计、导航流程
- Shared层：DTO定义、业务规则

---

## 2. 核心澄清问题总结

通过与业务专家（用户）的深度讨论，我们明确了以下5个关键问题：

### ✅ 问题1：医案（MedicalCase）的本质

**答案**：医案 = **一次完整的就诊记录**（从患者到诊所 → 完成看诊离开）

**关键理解**：
- ❌ 医案 ≠ 疾病治疗周期（不跨越多次就诊）
- ❌ 医案 ≠ 挂号单位（挂号和医案是分离的）
- ✅ 医案 = 就诊会话/就诊实例

**对比**：
- **Consultation（诊断）** = 就诊过程中的**辩证环节**（四诊、主诉、诊断结果）
- **Prescription（处方）** = 就诊过程中的**治疗方案**（药材、剂量、用法）

---

### ✅ 问题2：实体数量关系（核心结论）

**答案**：**全部是1:1关系**

```
1个医案（MedicalCase） : 1个诊断（Consultation） : 1个处方（Prescription） : 1个挂号（Registration，未来）
```

**这意味着**：
- ✅ 一次就诊 = 一个诊断记录（不支持多次修改诊断，如需修改则更新现有记录）
- ✅ 一次就诊 = 一个处方（不支持同一就诊开多个处方）
- ✅ 一次挂号 = 一次就诊（一号一诊）

**实际实现方式**（v2.0更新）：
```csharp
// Issue #1562 后的简化设计：
// - MedicalCase与Consultation使用共享主键（Consultation.Id == MedicalCase.Id）
// - MedicalCase与Prescription也使用共享主键（Prescription.Id == MedicalCase.Id）
// - 通过EF Core导航属性关联，而非显式外键字段

// Entity层实际字段：
public class MedicalCase
{
    public Guid Id { get; set; }
    public Consultation? Consultation { get; set; }  // 导航属性，1:1
    public Prescription? Prescription { get; set; }  // 导航属性，1:1
}

public class Consultation
{
    public Guid Id { get; set; }  // 与MedicalCase.Id相同（共享主键）
    public MedicalCase MedicalCase { get; set; }  // 导航属性
}
```

---

### ✅ 问题3：医案的创建时机和生命周期

**答案**：**医案作为容器先于内容创建**（采用流程A）

**完整流程**（v2.0更新 - Issue #1562）：
```
1. 患者选择 → 立即创建MedicalCase（患者ID、医生ID、就诊日期）
             ↓ Status = Active

2. 填写诊断表单 → 创建Consultation（使用共享主键：Consultation.Id = MedicalCase.Id）
             ↓ 通过导航属性关联：MedicalCase.Consultation = newConsultation

3. 开处方（可选）→ 创建Prescription（使用共享主键：Prescription.Id = MedicalCase.Id）
             ↓ 通过导航属性关联：MedicalCase.Prescription = newPrescription

4. 完成就诊 → 更新MedicalCase（Status = Closed）
```

**关键变更说明**（v2.0 - Issue #1562肃清计划）：
- ✅ 删除外键字段（ConsultationId/PrescriptionId），改用共享主键
- ✅ 简化时间跟踪（仅使用CreatedAt/UpdatedAt，删除StartTime/EndTime）
- ✅ 简化状态管理（统一使用CommonStatus，删除ConsultationStatus）

**关键理解**：
- ✅ 医案 = 容器（Container），先于内容存在
- ✅ 诊断 = 必填内容（Required Content）
- ✅ 处方 = 可选内容（Optional Content，"如果有处方"）
- ✅ 从属关系：`Consultation ∈ MedicalCase`, `Prescription ∈ MedicalCase`

---

### ✅ 问题4：医案的关闭时机和状态管理

**答案**：

#### **关闭时机**
- ✅ 医生手动点击"**完成就诊**"按钮
- ❌ 不自动关闭（即使有诊断+处方）
- ✅ 支持无处方场景（只有诊断，也可以完成就诊）

#### **按钮命名规范**
- ✅ "完成就诊" = 结束整个MedicalCase的按钮
- ✅ "诊断" = 仅用于描述Consultation相关内容
- ❌ 不使用"完成诊断"这种混淆的说法

#### **开处方流程**
- ✅ 医生自主决定是否开处方
- ✅ 直接点击"开处方"按钮进入
- ❌ 不需要系统弹窗询问"是否需要开处方？"

#### **暂停-恢复支持**
- ✅ **支持暂停-恢复**
- ✅ 急诊插队场景：暂时离开当前病案（保持Active状态）
- ✅ 急诊完成后：重新打开之前的病案继续
- ✅ 可以同时存在多个Active状态的MedicalCase（一个医生同时处理多个患者）

---

### ✅ 问题5：挂号（Registration）与医案的关系

**答案**：**采用方案B（挂号和医案分离）**

**核心原则**：
```
Registration（挂号）职责：
- 排队管理（队列号、叫号状态）
- 爽约处理（患者未到）
- 挂号费用（如果有）
- 预约时间管理

MedicalCase（医案）职责：
- 真实就诊记录
- 诊断和处方内容
- 就诊时长统计
- 医疗质量追溯
```

**关系设计**：
- ✅ 挂号时创建`Registration`（不创建医案）
- ✅ 医生叫号、患者进诊室、选择患者时，创建`MedicalCase`（关联`RegistrationId`）
- ✅ `MedicalCase.RegistrationId`是可选的（nullable），支持无挂号场景（在线问诊等）
- ✅ `Registration.MedicalCaseId`是可选的（nullable），支持爽约场景（挂号了但未就诊）

**优势**（详见第8节架构决策记录）：
- ✅ 职责分离，架构更清晰
- ✅ 支持预约、在线问诊、复诊等多种场景
- ✅ 医案仅记录真实就诊，统计数据不被爽约污染
- ✅ MVP友好，当前阶段无需改动，未来平滑扩展

---

## 3. 核心实体定义

### 3.1 Patient（患者）

**本质**：系统中的基础主数据，长期存在的实体。

**职责**：
- 存储患者基本信息（姓名、性别、年龄、联系方式等）
- 作为医案、挂号、处方的主体

**生命周期**：
- 创建：患者首次登记
- 更新：患者信息变更
- 删除：（通常不删除，软删除）

---

### 3.2 Registration（挂号）- 未来功能

**本质**：挂号单据，患者到诊所就诊的预约/排队凭证。

**职责**：
- 排队管理（队列号、叫号状态）
- 爽约处理（患者未到）
- 挂号费用管理
- 预约时间管理

**状态**：
- `Waiting`（待叫号）
- `Called`（已叫号）
- `Completed`（已就诊，关联MedicalCaseId）
- `Cancelled`（已取消/爽约）

**关键字段**：
```csharp
public Guid Id { get; set; }
public Guid PatientId { get; set; }
public Guid DoctorId { get; set; }
public DateTime RegistrationDate { get; set; }
public string QueueNumber { get; set; }  // 例如：A001
public RegistrationStatus Status { get; set; }
public Guid? MedicalCaseId { get; set; }  // 就诊后填充
```

---

### 3.3 MedicalCase（医案）

**本质**：一次完整就诊的容器/会话实例。

**职责**：
- 记录就诊基本信息（患者、医生、时间）
- 关联诊断和处方
- 管理就诊状态（Active/Closed）
- 提供业务方法（CanStartConsultation、CanComplete等）

**状态**（简化设计，Record-Only模式）：
- `Active`（活跃状态，可以添加诊断、处方）
- `Closed`（已关闭，已完成/已归档）

**关键字段**（v2.0更新 - Issue #1562）：
```csharp
public Guid Id { get; set; }
public Guid PatientId { get; set; }
public Guid DoctorId { get; set; }
public DateTime ConsultationDate { get; set; }

// ❌ 已删除外键字段（v1.0）：
// public Guid? ConsultationId { get; set; }
// public Guid? PrescriptionId { get; set; }

// ✅ v2.0使用导航属性（共享主键关联）：
public Consultation? Consultation { get; set; }  // 1:1，共享主键
public Prescription? Prescription { get; set; }  // 1:1，共享主键

// 未来扩展：关联挂号（可选）
public Guid? RegistrationId { get; set; }

// 状态管理（使用CommonStatus）
public CommonStatus Status { get; set; }
```

---

### 3.4 Consultation（诊断/辩证）

**本质**：诊疗活动的详细记录，中医的"辩证"环节。

**职责**（v2.0回归核心）：
- 记录四诊（望、闻、问、切）
- 记录主诉、现病史
- 记录中医诊断结果、治疗原则
- ❌ 不负责时间跟踪（使用Entity基类的CreatedAt/UpdatedAt）
- ❌ 不负责工作流状态（使用CommonStatus统一管理）

**关键字段**（v2.0更新 - Issue #1562）：
```csharp
public Guid Id { get; set; }  // 与MedicalCase.Id相同（共享主键）

// ❌ 已删除字段（v1.0过度设计）：
// public Guid MedicalCaseId { get; set; }  // 共享主键后不需要
// public Guid PatientId { get; set; }      // 通过MedicalCase获取
// public Guid UserId { get; set; }         // 通过MedicalCase获取
// public DateTime StartTime { get; set; }  // 删除，使用CreatedAt
// public DateTime? EndTime { get; set; }   // 删除，使用UpdatedAt
// public ConsultationStatus ConsultationStatus { get; set; }  // 删除，使用CommonStatus

// ✅ v2.0保留的核心字段：
// 诊断内容（四诊）
public string? ChiefComplaint { get; set; }        // 主诉
public string? PresentIllness { get; set; }        // 现病史
public string? Inspection { get; set; }            // 望诊
public string? AuscultationOlfaction { get; set; } // 闻诊
public string? Inquiry { get; set; }               // 问诊
public string? Palpation { get; set; }             // 切诊
public string? TCMDiagnosis { get; set; }          // 中医诊断
public string? TreatmentPrinciple { get; set; }    // 治疗原则
public string? MedicalAdvice { get; set; }         // 医嘱（v2.0新增）

// 导航属性
public MedicalCase MedicalCase { get; set; }  // 所属医案

// 状态管理（统一使用CommonStatus）
public CommonStatus Status { get; set; }
```

---

### 3.5 Prescription（处方）

**本质**：治疗方案的具体药方文档。

**职责**：
- 记录药材清单（药材、剂量、单价）
- 计算总价格、总重量
- 记录用法、医嘱
- 记录引用的验方（如果从验方导入）

**关键字段**：
```csharp
public Guid Id { get; set; }
public Guid MedicalCaseId { get; set; }  // 属于医案
public Guid PatientId { get; set; }
public Guid UserId { get; set; }  // 医生ID

// 处方内容
public string? Indication { get; set; }           // 主治
public int DosageCount { get; set; } = 7;         // 剂数
public string? Usage { get; set; }                // 用法
public decimal Discount { get; set; } = 1.0m;     // 折扣
public string? Advice { get; set; }               // 医嘱
public string? ReferencedFormulas { get; set; }   // 引用验方名称（逗号分隔）

// 药材清单
public List<PrescriptionItemDto> Items { get; set; }

// 计算属性
public decimal SingleDosePrice { get; }  // 单帖价格
public decimal TotalPrice { get; }       // 总价格
public decimal TotalWeight { get; }      // 总重量
```

---

## 4. 实体关系图

### 4.1 完整关系图（ER图）

```
┌─────────────┐
│   Patient   │ (1) ────────────────────┐
│  (患者)     │                         │
└─────────────┘                         │
                                        │ 1:N
                                        │
                              ┌─────────▼──────────┐
                              │   Registration     │ (未来功能)
                              │    (挂号)          │
                              └─────────┬──────────┘
                                        │ 1:1 (可选)
                                        │
                              ┌─────────▼──────────┐
┌─────────────┐               │   MedicalCase      │
│   Doctor    │ (1) ──────────│    (医案)          │
│  (医生)     │               └─────────┬──────────┘
└─────────────┘                         │
                                        │
                        ┌───────────────┼───────────────┐
                        │ 1:1           │ 1:1           │
                        │               │               │
            ┌───────────▼─────────┐     │     ┌─────────▼──────────┐
            │   Consultation      │     │     │   Prescription      │
            │    (诊断/辩证)       │     │     │    (处方)           │
            └─────────────────────┘     │     └────────────────────┘
                                        │
                                        │ (可选)
                                        │
                                        ▼
                                    [无处方场景]
```

### 4.2 关系说明表

| 起点实体 | 关系类型 | 终点实体 | 基数 | 可选性 | 说明 |
|---------|---------|---------|------|-------|------|
| Patient | has many | Registration | 1:N | - | 一个患者可以挂多次号 |
| Patient | has many | MedicalCase | 1:N | - | 一个患者可以有多次就诊 |
| Doctor | has many | MedicalCase | 1:N | - | 一个医生可以接诊多个患者 |
| Registration | has one | MedicalCase | 1:1 | 可选 | 一个挂号可以对应一次就诊（爽约则为null） |
| MedicalCase | has one | Registration | 1:1 | 可选 | 一个医案可以关联一个挂号（无挂号场景则为null） |
| MedicalCase | has one | Consultation | 1:1 | 必须 | 一个医案必须有一个诊断 |
| MedicalCase | has one | Prescription | 1:1 | 可选 | 一个医案可以有一个处方（可能无处方） |
| Consultation | belongs to | MedicalCase | N:1 | 必须 | 诊断必须属于一个医案 |
| Prescription | belongs to | MedicalCase | N:1 | 必须 | 处方必须属于一个医案 |

---

## 5. 数据模型设计

### 5.1 MedicalCase（医案）表结构

**设计说明**：
- ✅ Consultation和Prescription通过**共享主键**关联（`Consultation.Id == MedicalCase.Id`）
- ✅ 使用EF Core导航属性（`virtual Consultation? Consultation`），无需外键字段
- ✅ 1:1关系通过Fluent API配置（参见`ConsultationConfiguration.cs`）

```sql
CREATE TABLE MedicalCases (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- 关联患者和医生
    PatientId UNIQUEIDENTIFIER NOT NULL,
    PatientName NVARCHAR(100) NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    DoctorName NVARCHAR(100) NOT NULL,

    -- 时间管理
    ConsultationDate DATETIME2 NOT NULL DEFAULT GETDATE(),

    -- 状态管理
    Status INT NOT NULL DEFAULT 10,             -- 10=Active, 20=Closed (MedicalCaseStatus枚举)

    -- 其他字段
    Remark NVARCHAR(500),

    -- 审计字段（继承自BaseEntity）
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedBy UNIQUEIDENTIFIER,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION,

    -- 外键约束
    CONSTRAINT FK_MedicalCase_Patient FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_MedicalCase_Doctor FOREIGN KEY (DoctorId) REFERENCES Users(Id)
);

-- 索引
CREATE INDEX IX_MedicalCase_PatientId ON MedicalCases(PatientId);
CREATE INDEX IX_MedicalCase_DoctorId ON MedicalCases(DoctorId);
CREATE INDEX IX_MedicalCase_ConsultationDate ON MedicalCases(ConsultationDate);
CREATE INDEX IX_MedicalCase_Status ON MedicalCases(Status);
```

**导航属性** (代码层)：
```csharp
public class MedicalCase : BaseEntity
{
    // ✅ 1:1关系，通过共享主键关联
    public virtual Consultation? Consultation { get; set; }
    public virtual Prescription? Prescription { get; set; }
    // 未来功能
    public virtual Registration? Registration { get; set; }
}
```

### 5.2 Consultation（诊断）表结构

**设计说明**：
- ✅ **共享主键**：`Consultation.Id == MedicalCase.Id`（通过EF Core Fluent API配置）
- ✅ **无冗余字段**：PatientId/UserId通过`MedicalCase`导航属性获取
- ✅ **1:1关系**：一个医案对应唯一一个诊断

```sql
CREATE TABLE Consultations (
    -- ✅ 共享主键：Id必须等于关联的MedicalCase.Id
    Id UNIQUEIDENTIFIER PRIMARY KEY,

    -- 诊断内容（四诊合参）
    ChiefComplaint NVARCHAR(500),               -- 主诉
    PresentIllness NVARCHAR(1000),              -- 现病史
    Inspection NVARCHAR(500),                   -- 望诊
    AuscultationOlfaction NVARCHAR(500),        -- 闻诊
    Inquiry NVARCHAR(500),                      -- 问诊
    Palpation NVARCHAR(500),                    -- 切诊

    -- 中医诊断结果
    TCMDiagnosis NVARCHAR(500),                 -- 中医辨证
    TreatmentPrinciple NVARCHAR(500),           -- 治疗原则
    MedicalAdvice NVARCHAR(1000),               -- 医嘱

    -- 状态
    Status INT NOT NULL DEFAULT 1,              -- 1=Enabled (CommonStatus枚举)

    -- 其他字段
    Remark NVARCHAR(500),

    -- 审计字段（继承自BaseEntity）
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedBy UNIQUEIDENTIFIER,
    IsDeleted BIT NOT NULL DEFAULT 0,
    RowVersion ROWVERSION,

    -- 共享主键约束（EF Core配置）
    CONSTRAINT FK_Consultation_MedicalCase FOREIGN KEY (Id) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);
```

**EF Core配置** (ConsultationConfiguration.cs):
```csharp
entity.HasOne(c => c.MedicalCase)
      .WithOne(m => m.Consultation)
      .HasForeignKey<Consultation>(c => c.Id)  // 共享主键
      .IsRequired()
      .OnDelete(DeleteBehavior.Cascade);
```

### 5.3 Prescription（处方）表结构

**设计说明**：
- ✅ Prescription使用**传统FK模式**（MedicalCaseId字段），与Consultation的共享主键设计不同
- ⚠️ PatientId和UserId为**冗余字段**（可空），实际应通过MedicalCase导航属性获取（Phase 2待简化）
- ✅ 1:1关系通过唯一索引约束（UX_Prescriptions_MedicalCaseId）
- ✅ Fluent API配置参见PrescriptionConfiguration.cs

```sql
CREATE TABLE Prescriptions (
    -- ✅ 独立主键（非共享主键）
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- ✅ 关联医案（必需，有FK约束和唯一索引）
    MedicalCaseId UNIQUEIDENTIFIER NOT NULL,

    -- ⚠️ 冗余字段（可空，Phase 2待删除，应通过MedicalCase.PatientId/UserId获取）
    PatientId UNIQUEIDENTIFIER NULL,            -- 冗余字段，通过MedicalCase获取
    UserId UNIQUEIDENTIFIER NULL,               -- 冗余字段，通过MedicalCase获取

    -- 处方内容
    PrescriptionNumber NVARCHAR(20) NULL,       -- 处方编号（格式：RX-YYYYMMDD-NNNN，Issue #1551）
    Indication NVARCHAR(500),                   -- 主治（适应症）
    DosageCount INT NOT NULL DEFAULT 7,         -- 剂数
    Discount DECIMAL(3, 2) NOT NULL DEFAULT 1.0, -- 折扣（0-1之间，0.80表示8折）
    Advice NVARCHAR(500),                       -- 医嘱
    FormulaSource NVARCHAR(200),                -- 验方来源（自动填写）
    ReferencedFormulas NVARCHAR(500),           -- 引用验方名称列表（逗号分隔）

    -- 打印管理字段
    PrintVersion INT NOT NULL DEFAULT 1,        -- 当前打印版本号
    LastPrintedAt DATETIME2 NULL,               -- 最后打印时间
    PrintCount INT NOT NULL DEFAULT 0,          -- 打印次数
    IsPrinted BIT NOT NULL DEFAULT 0,           -- 是否已打印

    -- 其他字段
    Remark NVARCHAR(500),

    -- 审计字段
    Status INT NOT NULL DEFAULT 1,              -- 处方状态（PrescriptionStatus枚举）
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,        -- 必需（医生用户ID）
    UpdatedBy UNIQUEIDENTIFIER,
    RowVersion ROWVERSION,                      -- 并发控制

    -- 外键约束（仅关联MedicalCase，无PatientId/UserId的FK）
    CONSTRAINT FK_Prescription_MedicalCase FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id) ON DELETE CASCADE
);

-- 唯一索引（保证一病案至多一处方）
CREATE UNIQUE INDEX UX_Prescriptions_MedicalCaseId ON Prescriptions(MedicalCaseId);
```

### 5.4 Registration（挂号）表结构（未来功能）

```sql
CREATE TABLE Registrations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

    -- 关联患者和医生
    PatientId UNIQUEIDENTIFIER NOT NULL,
    DoctorId UNIQUEIDENTIFIER NOT NULL,

    -- 挂号信息
    RegistrationDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    QueueNumber NVARCHAR(20) NOT NULL,          -- 排队号（例如：A001）

    -- 可选：预约时间
    AppointmentTime DATETIME2 NULL,

    -- 可选：挂号费用
    RegistrationFee DECIMAL(18, 2) NULL,

    -- 关联医案（就诊后填充）
    MedicalCaseId UNIQUEIDENTIFIER NULL,

    -- 状态管理
    RegistrationStatus INT NOT NULL DEFAULT 0,  -- 0=Waiting, 1=Called, 2=Completed, 3=Cancelled

    -- 审计字段
    Status INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy UNIQUEIDENTIFIER,
    UpdatedBy UNIQUEIDENTIFIER,

    -- 外键约束
    CONSTRAINT FK_Registration_Patient FOREIGN KEY (PatientId) REFERENCES Patients(Id),
    CONSTRAINT FK_Registration_Doctor FOREIGN KEY (DoctorId) REFERENCES Users(Id),
    CONSTRAINT FK_Registration_MedicalCase FOREIGN KEY (MedicalCaseId) REFERENCES MedicalCases(Id)
);

-- 索引
CREATE INDEX IX_Registration_PatientId ON Registrations(PatientId);
CREATE INDEX IX_Registration_DoctorId ON Registrations(DoctorId);
CREATE INDEX IX_Registration_RegistrationDate ON Registrations(RegistrationDate);
CREATE INDEX IX_Registration_Status ON Registrations(RegistrationStatus);
CREATE UNIQUE INDEX UQ_Registration_QueueNumber ON Registrations(QueueNumber, RegistrationDate);
```

---

## 6. 状态机设计

### 6.1 MedicalCase（医案）状态机

```
┌─────────────────┐
│  [患者选择]     │
└────────┬────────┘
         │
         ▼
    ┌────────┐
    │ Active │ (活跃状态)
    └────┬───┘
         │
         │ [医生点击"完成就诊"]
         │
         ▼
    ┌────────┐
    │ Closed │ (已关闭)
    └────────┘
```

**状态定义**：
- `Active (10)`：活跃状态，可以添加/修改诊断、处方
- `Closed (20)`：已关闭，不可修改

**状态转换规则**：
```csharp
// 创建医案
MedicalCase.Status = Active

// 完成就诊
if (医生点击"完成就诊" && MedicalCase.ConsultationId != null)
{
    MedicalCase.Status = Closed
    MedicalCase.EndTime = DateTime.Now
}

// 业务规则验证
public bool CanComplete() => CaseStatus == Active && ConsultationId != null
```

**支持暂停-恢复**：
- ✅ 可以同时存在多个Active状态的MedicalCase
- ✅ 医生可以暂时离开当前医案（保持Active），处理其他患者
- ✅ 稍后回到暂停的医案继续（通过"今日患者列表"或"未完成医案列表"）

---

### 6.2 Consultation（诊断）状态机

```
┌─────────────────┐
│ [创建诊断记录]  │
└────────┬────────┘
         │
         ▼
  ┌──────────────┐
  │  InProgress  │ (诊断中)
  └──────┬───────┘
         │
         │ [保存诊断表单]
         │
         ▼
  ┌──────────────┐
  │  Completed   │ (已完成)
  └──────────────┘
```

**状态定义**：
- `InProgress (1)`：诊断中，可以修改
- `Completed (2)`：已完成

**注意**：根据1:1关系，一个MedicalCase只有一个Consultation，因此Consultation的状态管理相对简单。

---

### 6.3 Registration（挂号）状态机（未来功能）

```
┌─────────────────┐
│  [前台挂号]     │
└────────┬────────┘
         │
         ▼
   ┌──────────┐
   │ Waiting  │ (待叫号)
   └─────┬────┘
         │
         │ [医生叫号]
         │
         ▼
   ┌──────────┐
   │ Called   │ (已叫号)
   └─────┬────┘
         │
         │ [患者进诊室，创建MedicalCase]
         │
         ▼
   ┌───────────┐
   │ Completed │ (已就诊)
   └───────────┘

   [患者未到/爽约]
         │
         ▼
   ┌───────────┐
   │ Cancelled │ (已取消)
   └───────────┘
```

**状态定义**：
- `Waiting (0)`：待叫号
- `Called (1)`：已叫号
- `Completed (2)`：已就诊（关联MedicalCaseId）
- `Cancelled (3)`：已取消/爽约

---

## 7. 完整工作流

### 7.1 当前MVP阶段（无挂号功能）

```
┌──────────────────┐
│ 1. 医生登录系统  │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ 2. HomeView      │
│    点击"开始看诊"│
└────────┬─────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 3. PatientSelectionDialog           │
│    - 搜索患者（姓名/拼音/手机号）   │
│    - 双击选择患者                   │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 4. 创建MedicalCase                  │
│    - PatientId = 选中的患者ID       │
│    - DoctorId = 当前登录医生ID      │
│    - ConsultationDate = DateTime.Now│
│    - Status = Active                │
│    - ConsultationId = null          │
│    - PrescriptionId = null          │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 5. 导航到 ClinicalWorkstation       │
│    Region: ClinicalContentRegion    │
│    View: MedicalCaseEntryView       │
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 6. MedicalCaseEntryView             │
│    (诊断表单)                        │
│    - 填写主诉、现病史                │
│    - 填写四诊（望闻问切）            │
│    - 填写中医诊断、治疗原则          │
└────────┬────────────────────────────┘
         │
         │ [点击"保存诊断"]
         ▼
┌─────────────────────────────────────┐
│ 7. 创建Consultation                 │
│    - MedicalCaseId = 当前医案ID     │
│    - 保存诊断内容                    │
│    - 更新 MedicalCase.ConsultationId│
└────────┬────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────┐
│ 8. 医生决定是否开处方？             │
│    - 选项A：点击"开处方"按钮        │
│    - 选项B：点击"完成就诊"按钮      │
└────────┬────────────────────────────┘
         │
         ├─────────────────────────────┐
         │ [选项A：开处方]             │ [选项B：完成就诊]
         ▼                             ▼
┌──────────────────────┐        ┌──────────────────────┐
│ 9A. PrescriptionView │        │ 9B. 完成就诊          │
│     (处方录入)        │        │     - MedicalCase     │
│     - 添加药材        │        │       Status = Closed │
│     - 设置剂数、用法  │        │     - EndTime = Now   │
└──────────┬───────────┘        └───────┬──────────────┘
           │                            │
           │ [点击"保存处方"]            │
           ▼                            │
┌──────────────────────┐               │
│ 10. 创建Prescription │               │
│     - 保存处方内容    │               │
│     - 更新 MedicalCase│               │
│       PrescriptionId  │               │
└──────────┬───────────┘               │
           │                            │
           │ [点击"完成就诊"]            │
           ▼                            │
┌──────────────────────┐               │
│ 11. 完成就诊          │ ◄─────────────┘
│     - MedicalCase     │
│       Status = Closed │
│     - EndTime = Now   │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ 12. 返回HomeView     │
│     或继续选择患者    │
└──────────────────────┘
```

---

### 7.2 未来挂号功能完整流程

```
┌──────────────────────┐
│ 1. 前台接待患者      │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 2. RegistrationView                  │
│    - 选择患者                         │
│    - 选择医生                         │
│    - 生成排队号（例如：A001）         │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 3. 创建Registration                  │
│    - PatientId = 患者ID              │
│    - DoctorId = 医生ID               │
│    - QueueNumber = A001              │
│    - Status = Waiting (待叫号)       │
│    - MedicalCaseId = null            │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 4. 患者在候诊区等待                  │
│    (排队叫号系统显示进度)             │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 5. 医生叫号                          │
│    - 更新 Registration.Status = Called│
│    - 通知患者进诊室                   │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 6. 患者进诊室                        │
│    医生在ClinicalWorkstation中        │
│    选择该患者（关联Registration）     │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 7. 创建MedicalCase                   │
│    - PatientId = 患者ID              │
│    - DoctorId = 医生ID               │
│    - RegistrationId = Registration.Id│
│    - Status = Active                 │
│    - ConsultationId = null           │
│    - PrescriptionId = null           │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 8. 更新Registration                  │
│    - MedicalCaseId = 新创建的医案ID  │
│    - Status = Completed (已就诊)     │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 9-12. 诊断 → 处方 → 完成就诊         │
│       (同MVP流程步骤5-12)             │
└───────────────────────────────────────┘
```

**爽约场景**：
```
┌──────────────────────┐
│ 患者挂号后未到诊    │
└────────┬─────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ 前台/医生标记为"爽约"                │
│ - Registration.Status = Cancelled    │
│ - MedicalCaseId = null (无医案创建)  │
└───────────────────────────────────────┘
```

---

## 8. 架构决策记录

### 8.1 ADR-001: 挂号与医案分离设计

**决策日期**: 2025-10-18
**决策者**: 架构设计团队 + 业务专家
**状态**: ✅ 已采纳

#### **背景**

在设计挂号（Registration）功能时，需要决定挂号与医案（MedicalCase）的创建时机关系：
- **方案A**：挂号时立即创建医案（预创建）
- **方案B**：挂号和医案分离创建（按需创建）

#### **决策**

采用**方案B：挂号和医案分离创建**。

#### **理由**

##### **1. 职责分离（Separation of Concerns）**

| 职责 | Registration（挂号） | MedicalCase（医案） |
|------|---------------------|-------------------|
| 排队管理 | ✅ | ❌ |
| 爽约处理 | ✅ | ❌ |
| 挂号费用 | ✅ | ❌ |
| 预约时间 | ✅ | ❌ |
| 真实就诊记录 | ❌ | ✅ |
| 诊断和处方 | ❌ | ✅ |
| 就诊时长统计 | ❌ | ✅ |
| 医疗质量追溯 | ❌ | ✅ |

##### **2. 数据准确性**

**方案A的问题**：
- ❌ 爽约患者会产生大量未完成的医案记录
- ❌ 就诊人数统计 = 挂号人数 - 爽约人数（计算复杂）
- ❌ 医案状态需要区分"已挂号未就诊"和"已就诊"

**方案B的优势**：
- ✅ 医案仅记录真实就诊，数据更纯净
- ✅ 就诊人数统计 = COUNT(MedicalCases)（简单直接）
- ✅ 爽约率统计 = COUNT(Registrations WHERE Status='Cancelled') / COUNT(Registrations)

##### **3. 未来扩展性**

**方案B支持的扩展场景**：

| 场景 | 方案A（预创建） | 方案B（分离） |
|------|----------------|--------------|
| 预约挂号 | ⚠️ 需要区分"已预约未到诊"状态 | ✅ Registration.AppointmentTime |
| 在线问诊 | ❌ 无法支持（必须先挂号） | ✅ MedicalCase.RegistrationId=null |
| 一号多诊（复诊） | ❌ 1:1关系，无法支持 | ✅ 多个MedicalCase关联同一Registration |
| 爽约统计 | ⚠️ 需要查询未完成的医案 | ✅ Registration.Status='Cancelled' |

##### **4. MVP友好性**

- ✅ 当前MVP阶段无需实现Registration（可以直接创建MedicalCase）
- ✅ 未来增加挂号功能时，只需在MedicalCase中添加`RegistrationId`字段（Migration）
- ✅ 平滑扩展，不影响现有代码

##### **5. 查询性能对比**

**方案A（预创建）**：
```sql
-- 查询今日就诊患者
SELECT * FROM MedicalCases
WHERE ConsultationDate = TODAY
  AND Status = 'Closed'  -- 简单
```

**方案B（分离）**：
```sql
-- 查询今日就诊患者
SELECT mc.*, r.QueueNumber
FROM MedicalCases mc
LEFT JOIN Registrations r ON mc.RegistrationId = r.Id
WHERE mc.ConsultationDate = TODAY
  AND mc.Status = 'Closed'  -- 稍复杂，但可以通过索引优化
```

**结论**：查询复杂度略有增加，但通过合理的索引设计可以保证性能。数据准确性和扩展性的优势远大于查询复杂度的劣势。

#### **后果**

##### **积极后果**：
- ✅ 架构更清晰，职责分离
- ✅ 数据更准确，统计更简单
- ✅ 支持更多业务场景（预约、在线问诊、复诊）
- ✅ MVP友好，平滑扩展

##### **消极后果**：
- ⚠️ 查询时需要JOIN两张表（但可以通过索引优化）
- ⚠️ 需要新增Registration实体和API（但这是未来功能，当前无需实现）

#### **实施计划**

- **Phase 0（当前MVP）**：
  - 不实现Registration功能
  - MedicalCase设计中预留RegistrationId字段（在文档中说明，代码中不实现）

- **Phase 1（挂号功能）**：
  - 新增Registration实体、DTO、API
  - MedicalCase增加RegistrationId字段（Migration）
  - 实现挂号管理界面

- **Phase 2（高级功能）**：
  - 预约挂号
  - 在线叫号
  - 爽约管理
  - 统计报表

---

## 9. 实施路线图

### 9.1 当前MVP阶段（Epic #1343）

**目标**：实现基本看诊流程（无挂号功能）

**核心实体**：
- ✅ Patient（已实现）
- ✅ MedicalCase（已实现）
- ✅ Consultation（已实现）
- ✅ Prescription（已实现）

**工作流**：
```
患者选择 → 创建MedicalCase → 诊断录入 → 处方录入（可选）→ 完成就诊
```

**重点任务**：
1. 重新设计HomeView UI/UX（突出"开始看诊"）
2. 实现完整的患者选择 → 医案创建 → 导航流程
3. 优化MedicalCaseEntryView（诊断表单）
4. 优化PrescriptionView（处方录入）
5. 实现"完成就诊"逻辑
6. 支持暂停-恢复（未完成医案列表）

---

### 9.2 Phase 1（挂号功能，未来）

**目标**：增加挂号管理和叫号功能

**新增实体**：
- ➕ Registration（挂号）

**数据模型变更**：
- ➕ MedicalCase增加`RegistrationId`字段（Migration）
- ➕ Registration表创建

**新增API**：
- `POST /api/v1/registrations` - 创建挂号
- `GET /api/v1/registrations` - 获取挂号列表
- `PUT /api/v1/registrations/{id}/call` - 叫号
- `PUT /api/v1/registrations/{id}/cancel` - 取消挂号

**新增界面**：
- RegistrationView（前台挂号界面）
- QueueManagementView（叫号管理界面）
- 修改ClinicalWorkstation，集成挂号列表

---

### 9.3 Phase 2（高级功能，未来）

**目标**：扩展预约、在线问诊等高级场景

**扩展功能**：
- 预约挂号（AppointmentTime字段）
- 在线问诊（无挂号场景）
- 一号多诊（复诊调整处方）
- 爽约管理和统计
- 挂号费用管理
- 统计报表（就诊人数、爽约率、医生工作量）

---

## 10. 总结

通过本次深度澄清，我们彻底搞清楚了看诊流程中各个实体的关系：

### ✅ 核心结论

1. **医案（MedicalCase）** = 一次完整就诊的容器
2. **全部是1:1关系**：1医案 : 1诊断 : 1处方 : 1挂号
3. **医案先创建**：患者选择后立即创建MedicalCase（作为容器）
4. **医生主导完成**：点击"完成就诊"按钮关闭医案
5. **支持暂停-恢复**：可以同时存在多个Active状态的医案
6. **挂号和医案分离**：职责清晰，支持未来扩展

### ✅ 架构优势

- ✅ **清晰的职责分离**：每个实体各司其职
- ✅ **准确的数据模型**：1:1关系，简单直接
- ✅ **灵活的扩展性**：支持预约、在线问诊、复诊等多种场景
- ✅ **MVP友好设计**：当前阶段无需改动，未来平滑扩展

### ✅ 下一步行动

1. 基于本文档编写Spec的`requirements.md`和`design.md`
2. 重新设计看诊流程UI/UX
3. 实现完整的患者选择 → 医案创建 → 诊断 → 处方 → 完成就诊流程
4. 为未来的挂号功能预留扩展点

---

**文档维护**：
- 本文档是看诊流程的权威参考
- 任何对实体关系的修改都必须先更新本文档
- 定期review，确保文档与代码保持同步

---

**参考资料**：
- Epic #1343: MVP "能看诊"
- `.spec-workflow/steering/constitution.md` - 项目宪法
- `docs/explanation/architecture/server/README.md` - Server端三层架构
- `docs/explanation/architecture/client/README.md` - Client端MVVM架构

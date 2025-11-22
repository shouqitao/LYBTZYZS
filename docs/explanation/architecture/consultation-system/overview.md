# 中医诊断系统架构设计

**TCM Consultation System Architecture**

本文档详细说明LYBTZYZS系统中中医诊断模块的架构设计、技术决策、业务规则和数据流。

---

## 目录

1. [模块概述](#模块概述)
2. [三层架构设计](#三层架构设计)
3. [核心领域模型](#核心领域模型)
4. [业务规则体系](#业务规则体系)
5. [数据流与交互](#数据流与交互)
6. [技术决策](#技术决策)
7. [模块依赖关系](#模块依赖关系)
8. [扩展性设计](#扩展性设计)

---

## 模块概述

### 业务定位

中医诊断模块是LYBTZYZS系统的核心临床业务模块,负责中医四诊信息采集、辨证分析、诊断记录管理。它为病案模块、处方模块提供完整的中医诊断数据支持。

**核心职责**:
1. 四诊信息采集(望、闻、问、切)
2. 舌诊数据记录与分析
3. 脉诊数据采集与判读
4. 中医辨证分析(八纲辨证、脏腑辨证)
5. 诊断结果记录与管理

**设计原则**:
- **MVP优先**: 聚焦核心四诊功能,避免过度设计
- **三层对齐**: 严格遵循三层架构规范
- **中医专业性**: 遵循中医诊断规范和术语标准
- **临床实用性**: 优化诊断流程,提升医生效率

---

## 三层架构设计

### 架构层次

```
┌──────────────────────────────────────────┐
│           Client Layer (WPF)             │
│  ┌────────────────────────────────────┐  │
│  │ ConsultationView (诊断视图)         │  │
│  │ FourExamsPanel (四诊面板)           │  │
│  │ TongueAnalysisView (舌诊分析)       │  │
│  │ PulseAnalysisView (脉诊分析)        │  │
│  │ SyndromeAnalysisView (辨证分析)     │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ ConsultationViewModel              │  │
│  │ TongueAnalysisViewModel            │  │
│  │ PulseAnalysisViewModel             │  │
│  │ SyndromeAnalysisViewModel          │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ ConsultationService                │  │
│  │ - IConsultationApi (Refit)         │  │
│  │ - ITongueAnalysisApi (Refit)       │  │
│  │ - IPulseAnalysisApi (Refit)        │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    ↓ HTTP/API
┌──────────────────────────────────────────┐
│          Server Layer (ASP.NET)          │
│  ┌────────────────────────────────────┐  │
│  │ ConsultationController (WebAPI)    │  │
│  │ - POST /api/consultation/diagnostic│  │
│  │ - POST /api/consultation/tongue    │  │
│  │ - POST /api/consultation/pulse     │  │
│  │ - POST /api/consultation/syndrome  │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ ConsultationService (业务逻辑)      │  │
│  │ - Four Exams Processing            │  │
│  │ - Tongue Analysis                  │  │
│  │ - Pulse Analysis                   │  │
│  │ - Syndrome Differentiation         │  │
│  └────────────────────────────────────┘  │
│  ┌────────────────────────────────────┐  │
│  │ ConsultationRepository (数据访问)   │  │
│  │ - IRepository<Consultation>        │  │
│  └────────────────────────────────────┘  │
└──────────────────────────────────────────┘
                    ↓ EF Core
┌──────────────────────────────────────────┐
│         Database (SQL Server)            │
│  Consultations Table (诊断表)           │
│  - Id, MedicalCaseId, Inspection...     │
│  TongueAnalysis Table (舌诊表)          │
│  PulseAnalysis Table (脉诊表)           │
│  SyndromeAnalysis Table (辨证表)        │
└──────────────────────────────────────────┘
```

### 层次职责

**Client Layer (WPF)**:
- **View**: 四诊信息录入界面(XAML)
- **ViewModel**: 诊断流程控制和数据绑定(MVVM模式)
- **Service**: API调用和诊断数据管理(Refit)

**Server Layer (ASP.NET Core)**:
- **Controller**: RESTful API端点(四诊、舌诊、脉诊、辨证)
- **Service**: 中医诊断业务逻辑(验证、分析、协调)
- **Repository**: 诊断数据访问抽象(EF Core查询)

**Data Layer (SQL Server)**:
- **Database**: 诊断数据持久化存储
- **Migrations**: 数据库schema版本管理

---

## 核心领域模型

### 实体定义

**Consultation实体** (`LYBT.Entities/Consultation/Consultation.cs`)

```csharp
public class Consultation : BaseEntity
{
    // 关联关系
    public Guid MedicalCaseId { get; set; }        // 关联病案*
    public virtual MedicalCase MedicalCase { get; set; }

    // 四诊信息 (望、闻、问、切)
    public string? Inspection { get; set; }        // 望诊 (面色、神态、形体、舌象)
    public string? AuscultationOlfaction { get; set; }  // 闻诊 (听声音、嗅气味)
    public string? Inquiry { get; set; }           // 问诊 (主诉、现病史、既往史)
    public string? Palpation { get; set; }         // 切诊 (脉象、腹诊)

    // 诊断结果
    public string? TCMDiagnosis { get; set; }      // 中医诊断*
    public string? TreatmentPrinciple { get; set; } // 治疗原则
    public string? Remark { get; set; }            // 备注

    // 关联数据
    public virtual TongueAnalysis? TongueAnalysis { get; set; }  // 舌诊分析
    public virtual PulseAnalysis? PulseAnalysis { get; set; }    // 脉诊分析
    public virtual SyndromeAnalysis? SyndromeAnalysis { get; set; } // 辨证分析
}
```

**TongueAnalysis实体** (舌诊分析)

```csharp
public class TongueAnalysis : BaseEntity
{
    public Guid ConsultationId { get; set; }       // 关联诊断*
    public virtual Consultation Consultation { get; set; }

    // 舌质分析
    public string? TongueColor { get; set; }       // 舌色 (淡红、淡白、红、绛、紫)
    public string? TongueShape { get; set; }       // 舌形 (胖大、瘦薄、点刺、裂纹)
    public string? TongueMoisture { get; set; }    // 润燥 (润、燥)

    // 舌苔分析
    public string? CoatingColor { get; set; }      // 苔色 (白、黄、灰、黑)
    public string? CoatingThickness { get; set; }  // 厚薄 (薄、厚、无苔)
    public string? CoatingTexture { get; set; }    // 腻润 (润、腻、燥)

    // 舌诊影像
    public string? TongueImagePath { get; set; }   // 舌象图片路径

    // 综合分析
    public string? Analysis { get; set; }          // 舌诊分析结论
}
```

**PulseAnalysis实体** (脉诊分析)

```csharp
public class PulseAnalysis : BaseEntity
{
    public Guid ConsultationId { get; set; }       // 关联诊断*
    public virtual Consultation Consultation { get; set; }

    // 脉象特征
    public string? PulsePosition { get; set; }     // 部位 (浮、中、沉)
    public string? PulseRate { get; set; }         // 至数 (迟、缓、平、数、疾)
    public string? PulseStrength { get; set; }     // 力度 (虚、实)
    public string? PulseShape { get; set; }        // 形态 (长、短、洪、细、弦、滑)

    // 28脉分类
    public string? PulseType { get; set; }         // 脉象类型 (浮、沉、迟、数等28脉)

    // 左右手脉象
    public string? LeftWristPulse { get; set; }    // 左手脉象 (寸关尺)
    public string? RightWristPulse { get; set; }   // 右手脉象 (寸关尺)

    // 脉诊数据 (可选:智能脉诊仪数据)
    public string? PulseDataJson { get; set; }     // JSON格式脉象数据

    // 综合分析
    public string? Analysis { get; set; }          // 脉诊分析结论
}
```

**SyndromeAnalysis实体** (辨证分析)

```csharp
public class SyndromeAnalysis : BaseEntity
{
    public Guid ConsultationId { get; set; }       // 关联诊断*
    public virtual Consultation Consultation { get; set; }

    // 八纲辨证
    public string? EightPrinciples { get; set; }   // 表里、寒热、虚实、阴阳

    // 脏腑辨证
    public string? OrganSyndrome { get; set; }     // 脏腑病证 (心、肝、脾、肺、肾)

    // 病因辨证
    public string? EtiologySyndrome { get; set; }  // 六淫、七情、饮食劳倦

    // 气血津液辨证
    public string? QiBloodSyndrome { get; set; }   // 气血津液病证

    // 经络辨证
    public string? MeridianSyndrome { get; set; }  // 十二经络病证

    // 综合分析
    public string? SyndromeConclusion { get; set; } // 辨证结论*
    public string? PathogenesisSummary { get; set; } // 病机总结
}
```

### 字段说明

**Consultation核心字段**:

| 字段 | 类型 | 必填 | 说明 | 业务规则 |
|------|------|------|------|---------|
| MedicalCaseId | Guid | ✅ | 病案ID | BR-001: 必须关联有效病案 |
| Inspection | string | ❌ | 望诊信息 | BR-002: 最大5000字符 |
| AuscultationOlfaction | string | ❌ | 闻诊信息 | BR-002: 最大5000字符 |
| Inquiry | string | ❌ | 问诊信息 | BR-002: 最大5000字符 |
| Palpation | string | ❌ | 切诊信息 | BR-002: 最大5000字符 |
| TCMDiagnosis | string | ✅ | 中医诊断 | BR-003: 必填,最大500字符 |
| TreatmentPrinciple | string | ❌ | 治疗原则 | BR-004: 最大1000字符 |

### DTO设计

**ConsultationDto** (`LYBT.Shared.Models/Contracts/Consultation/ConsultationDto.cs`):
- 前后端数据传输
- 包含四诊信息和诊断结果
- 关联TongueAnalysisDto、PulseAnalysisDto、SyndromeAnalysisDto

**DiagnosticDataDto** (诊断数据输入):
- 创建和更新诊断的统一输入模型
- FluentValidation验证
- 支持部分字段更新

**FourExamsDto** (四诊数据):
- 结构化四诊信息
- 支持前端分步录入
- 数据完整性验证

---

## 业务规则体系

### 数据验证规则

**BR-001: 病案关联**
```csharp
// 验证器: ConsultationDtoValidator.cs
RuleFor(x => x.MedicalCaseId)
    .NotEmpty().WithMessage("病案ID不能为空")
    .Must(BeValidMedicalCase).WithMessage("病案不存在或已关闭");
```

**BR-002: 四诊信息长度**
```csharp
RuleFor(x => x.Inspection)
    .MaximumLength(5000).WithMessage("望诊信息不能超过5000字符");

RuleFor(x => x.AuscultationOlfaction)
    .MaximumLength(5000).WithMessage("闻诊信息不能超过5000字符");

RuleFor(x => x.Inquiry)
    .MaximumLength(5000).WithMessage("问诊信息不能超过5000字符");

RuleFor(x => x.Palpation)
    .MaximumLength(5000).WithMessage("切诊信息不能超过5000字符");
```

**BR-003: 诊断结果必填**
```csharp
RuleFor(x => x.TCMDiagnosis)
    .NotEmpty().WithMessage("中医诊断不能为空")
    .MaximumLength(500).WithMessage("中医诊断不能超过500字符");
```

**BR-004: 治疗原则**
```csharp
RuleFor(x => x.TreatmentPrinciple)
    .MaximumLength(1000).WithMessage("治疗原则不能超过1000字符");
```

### 业务逻辑规则

**BR-005: 唯一性约束**
```csharp
// 一个病案只能有一条诊断记录
var exists = await _repository.ExistsByMedicalCaseIdAsync(dto.MedicalCaseId);
if (exists && isNewRecord)
{
    return Result.Failure("该病案已存在诊断记录,请编辑现有记录");
}
```

**BR-006: 四诊完整性检查**
```csharp
// 至少需要录入一项四诊信息
public bool HasFourExamsData =>
    !string.IsNullOrWhiteSpace(Inspection) ||
    !string.IsNullOrWhiteSpace(AuscultationOlfaction) ||
    !string.IsNullOrWhiteSpace(Inquiry) ||
    !string.IsNullOrWhiteSpace(Palpation);
```

**BR-007: 舌诊图片格式**
```csharp
// 支持格式: jpg, jpeg, png
// 最大大小: 5MB
RuleFor(x => x.TongueImagePath)
    .Must(BeValidImageFormat).WithMessage("仅支持jpg/png格式")
    .Must(BeWithinSizeLimit).WithMessage("图片大小不能超过5MB");
```

**BR-008: 脉诊数据有效性**
```csharp
// 脉诊数据JSON格式验证
RuleFor(x => x.PulseDataJson)
    .Must(BeValidJson).WithMessage("脉诊数据格式无效")
    .When(x => !string.IsNullOrWhiteSpace(x.PulseDataJson));
```

### 四诊合参规则

**BR-009: 四诊合参原则**
```
望诊 → 初步判断（神色形态）
闻诊 → 补充信息（声音气味）
问诊 → 详细病史（主诉、病程）
切诊 → 确诊依据（脉象、腹诊）

综合分析 → 辨证论治
```

**BR-010: 辨证分析完整性**
```csharp
// 辨证分析至少包含八纲辨证或脏腑辨证
public bool HasSyndromeAnalysis =>
    !string.IsNullOrWhiteSpace(EightPrinciples) ||
    !string.IsNullOrWhiteSpace(OrganSyndrome);
```

---

## 数据流与交互

### 诊断创建流程

```
┌─────────────┐
│  Client     │
│  (WPF)      │
└──────┬──────┘
       │ 1. 医生录入四诊信息
       ↓
┌──────────────────┐
│  ViewModel       │
│  - 四诊数据绑定  │
│  - 诊断命令处理  │
└──────┬───────────┘
       │ 2. 调用ConsultationService
       ↓
┌──────────────────┐
│ ConsultationService│
│  (Refit API)     │
└──────┬───────────┘
       │ 3. POST /api/consultation/diagnostic
       ↓
┌──────────────────┐
│  Controller      │
│  - 参数验证      │
│  - 业务路由      │
└──────┬───────────┘
       │ 4. 调用Service
       ↓
┌──────────────────┐
│ ConsultationService│
│  (Server)        │
│  - FluentValidation│
│  - 四诊合参分析  │
│  - 辨证逻辑      │
└──────┬───────────┘
       │ 5. 保存诊断数据
       ↓
┌──────────────────┐
│  Repository      │
│  - EF Core保存   │
│  - 关联数据处理  │
└──────┬───────────┘
       │ 6. 数据库INSERT
       ↓
┌──────────────────┐
│  Database        │
│  Consultations   │
│  TongueAnalysis  │
│  PulseAnalysis   │
│  SyndromeAnalysis│
└──────────────────┘
```

### 舌诊分析流程

```
舌象图片 → 客户端预览 → 上传Server → 存储文件 → 保存路径

详细步骤：
1. 医生拍摄舌象照片
2. 客户端显示图片预览
3. 上传 POST /api/consultation/tongue-image
4. Server保存到指定目录 (wwwroot/uploads/tongue/{date}/)
5. 返回图片相对路径
6. TongueAnalysis.TongueImagePath = 相对路径
7. 医生录入舌质、舌苔分析
8. 保存完整舌诊记录
```

### 脉诊数据采集流程

```
手动录入模式:
医生手诊 → 录入28脉分类 → 记录左右手脉象 → 分析结论

智能设备模式 (未来扩展):
脉诊仪采集 → JSON数据传输 → 自动分析 → 生成报告 → 医生确认
```

### 辨证分析流程

```
四诊数据 → 八纲辨证 → 脏腑辨证 → 病因辨证 → 综合分析

详细步骤：
1. 读取四诊信息
2. 分析表里寒热虚实阴阳 (八纲)
3. 判断五脏六腑病证
4. 识别六淫七情致病因素
5. 综合形成辨证结论
6. 指导治疗原则和处方用药
```

---

## 技术决策

### TD-001: 四诊信息字段设计

**决策**: 四诊信息使用4个大文本字段存储,不做进一步细分

**理由**:
1. 中医诊断灵活性高,不宜过度结构化
2. 医生录入习惯各异,文本字段更自由
3. 减少数据库表复杂度
4. 便于快速录入和查看

**实现**:
```csharp
public string? Inspection { get; set; }        // 5000字符
public string? AuscultationOlfaction { get; set; }  // 5000字符
public string? Inquiry { get; set; }           // 5000字符
public string? Palpation { get; set; }         // 5000字符
```

**代码位置**: `LYBT.Entities/Consultation/Consultation.cs:15-24`

---

### TD-002: 舌诊脉诊独立实体

**决策**: TongueAnalysis、PulseAnalysis作为独立实体,而非嵌入Consultation

**理由**:
1. 舌诊脉诊数据复杂,独立实体便于扩展
2. 支持舌象图片存储和分析
3. 未来可扩展智能分析功能
4. 查询性能优化(按需加载)

**实现**:
```csharp
// 一对一关系
public virtual TongueAnalysis? TongueAnalysis { get; set; }
public virtual PulseAnalysis? PulseAnalysis { get; set; }

// 导航属性配置
builder.HasOne(c => c.TongueAnalysis)
       .WithOne(t => t.Consultation)
       .HasForeignKey<TongueAnalysis>(t => t.ConsultationId);
```

**代码位置**: `LYBT.Entities/Consultation/Consultation.cs:35-36`

---

### TD-003: 辨证分析多维度设计

**决策**: SyndromeAnalysis包含5种辨证方法(八纲、脏腑、病因、气血、经络)

**理由**:
1. 符合中医诊断思维多维度特点
2. 支持不同医生的辨证习惯
3. 便于临床教学和病例分析
4. 为AI辅助辨证预留接口

**实现**:
```csharp
public string? EightPrinciples { get; set; }   // 八纲辨证
public string? OrganSyndrome { get; set; }     // 脏腑辨证
public string? EtiologySyndrome { get; set; }  // 病因辨证
public string? QiBloodSyndrome { get; set; }   // 气血津液辨证
public string? MeridianSyndrome { get; set; }  // 经络辨证
```

**代码位置**: `LYBT.Entities/Consultation/SyndromeAnalysis.cs:12-22`

---

### TD-004: 唯一性约束-一案一诊

**决策**: 一个MedicalCase只能有一条Consultation记录

**理由**:
1. 简化数据模型,符合临床实际
2. 避免重复诊断记录
3. 诊断修改通过Update而非Create
4. 历史记录通过审计字段追踪

**实现**:
```csharp
// 数据库唯一索引
CREATE UNIQUE NONCLUSTERED INDEX IX_Consultations_MedicalCaseId
ON Consultations(MedicalCaseId) WHERE IsDeleted = 0;

// Service层验证
var exists = await _repository.ExistsByMedicalCaseIdAsync(dto.MedicalCaseId);
if (exists)
{
    return Result.Failure("该病案已存在诊断记录");
}
```

**代码位置**: `ConsultationService.cs:CreateAsync`

---

### TD-005: 舌象图片存储策略

**决策**: 舌象图片存储在服务器文件系统,数据库仅保存相对路径

**理由**:
1. 减少数据库存储压力
2. 提升查询性能
3. 便于备份和迁移
4. 支持CDN加速(未来)

**实现**:
```csharp
// 存储路径: wwwroot/uploads/tongue/{yyyy-MM-dd}/{guid}.jpg
var relativePath = $"/uploads/tongue/{DateTime.Now:yyyy-MM-dd}/{Guid.NewGuid()}.jpg";

// 数据库字段
public string? TongueImagePath { get; set; }  // 存储相对路径

// 客户端访问
var imageUrl = $"{ApiBaseUrl}{tongueAnalysis.TongueImagePath}";
```

**代码位置**: `ConsultationService.cs:UploadTongueImageAsync`

---

### TD-006: 软删除保护诊断历史

**决策**: 所有删除操作均为软删除(IsDeleted=true)

**理由**:
1. 诊断记录为医疗档案,不可物理删除
2. 支持病案完整性审计
3. 误删除可恢复
4. 符合医疗数据合规要求

**实现**:
```csharp
// 继承BaseEntity自动获得软删除支持
public class Consultation : BaseEntity
{
    // IsDeleted, DeletedAt, DeletedBy 继承自BaseEntity
}

// Repository自动过滤已删除记录
protected virtual IQueryable<TEntity> GetQueryable()
{
    return _dbSet.Where(e => !e.IsDeleted);
}
```

**代码位置**: `BaseRepository.cs:GetQueryable`

---

## 模块依赖关系

### 依赖图

```
┌─────────────────┐
│  Prescription   │ ← 处方依赖诊断结果
│     Module      │   (治疗原则、用药指导)
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│  Consultation   │ ← 核心诊断模块
│     Module      │   (四诊、辨证)
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│  MedicalCase    │ ← 依赖病案信息
│     Module      │   (患者、医生、就诊日期)
└─────────────────┘
```

### 模块职责边界

**Consultation模块职责**:
- ✅ 四诊信息采集和管理
- ✅ 舌诊脉诊分析
- ✅ 中医辨证分析
- ✅ 诊断结果记录
- ❌ 不负责处方开具(由Prescription模块)
- ❌ 不负责患者档案管理(由Patient模块)
- ❌ 不负责病案流程管理(由MedicalCase模块)

**MedicalCase模块依赖**:
- 提供病案基础信息(患者、医生、就诊日期)
- 关联Consultation记录(一对一)
- 提供病案状态管理

**Prescription模块依赖**:
- 读取诊断结果(TCMDiagnosis、TreatmentPrinciple)
- 读取辨证分析(SyndromeConclusion)
- 根据治疗原则开具处方

**数据流向**:
```
Patient → MedicalCase → Consultation → Prescription
         (就诊)        (诊断)         (用药)
```

---

## 扩展性设计

### 未来功能规划

**Phase 1 (已完成)**:
- ✅ 基础四诊信息录入
- ✅ 舌诊脉诊记录
- ✅ 辨证分析功能
- ✅ 诊断结果管理

**Phase 2 (进行中)**:
- ⏳ 舌象图片上传和预览
- ⏳ 脉诊数据可视化
- ⏳ 辨证分析模板
- ⏳ 诊断记录模板

**Phase 3 (未来扩展)**:
- ⏳ AI辅助舌诊分析
- ⏳ 智能脉诊仪集成
- ⏳ 辨证决策树引导
- ⏳ 诊断知识库检索

**Phase 4 (长期规划)**:
- ⏳ 名医诊断案例库
- ⏳ 中医证候数据分析
- ⏳ 多模态诊断数据融合
- ⏳ 远程诊断支持

### 架构扩展点

**扩展点1: 舌诊智能分析**
```csharp
// 当前: 手动录入舌质舌苔
public string? TongueColor { get; set; }
public string? CoatingColor { get; set; }

// 未来扩展: AI图像识别
public class TongueAIAnalysis
{
    public string TongueImagePath { get; set; }    // 原图
    public TongueSegmentationResult Segmentation { get; set; }  // 舌体分割
    public ColorAnalysisResult ColorAnalysis { get; set; }      // 颜色分析
    public CoatingAnalysisResult CoatingAnalysis { get; set; }  // 舌苔分析
    public float ConfidenceScore { get; set; }                  // 置信度
    public string AIConclusion { get; set; }                    // AI结论
}
```

**扩展点2: 脉诊仪数据集成**
```csharp
// 当前: 手动录入脉象
public string? PulseType { get; set; }

// 未来扩展: 智能脉诊仪
public class PulseDeviceData
{
    public string DeviceModel { get; set; }        // 设备型号
    public DateTime CollectionTime { get; set; }   // 采集时间
    public PulseWaveform LeftWrist { get; set; }   // 左手波形数据
    public PulseWaveform RightWrist { get; set; }  // 右手波形数据
    public PulseCharacteristics Features { get; set; }  // 特征提取
    public string AutoDiagnosis { get; set; }      // 自动诊断
}
```

**扩展点3: 辨证决策树**
```csharp
// 当前: 自由文本辨证
public string? EightPrinciples { get; set; }

// 未来扩展: 结构化决策树
public class SyndromeDecisionTree
{
    public List<DecisionNode> DecisionPath { get; set; }  // 决策路径
    public Dictionary<string, float> SyndromeScores { get; set; }  // 证候评分
    public string PrimarySyndrome { get; set; }            // 主证
    public List<string> SecondarySyndromes { get; set; }   // 兼证
    public string PathogenesisMechanism { get; set; }      // 病机分析
    public List<string> RecommendedTreatments { get; set; } // 推荐治法
}
```

**扩展点4: 诊断模板系统**
```csharp
// 未来扩展: 诊断模板
public class ConsultationTemplate
{
    public string TemplateName { get; set; }       // 模板名称
    public string Category { get; set; }           // 分类 (感冒、胃病等)
    public string InspectionTemplate { get; set; } // 望诊模板
    public string InquiryTemplate { get; set; }    // 问诊模板
    public string PalpationTemplate { get; set; }  // 切诊模板
    public List<string> CommonSyndromes { get; set; }  // 常见证型
    public string TreatmentGuideline { get; set; } // 治疗指引
}
```

---

## 性能优化

### 数据库索引

```sql
-- 主键索引（自动）
CREATE UNIQUE CLUSTERED INDEX PK_Consultations ON Consultations(Id);

-- 病案关联索引（唯一）
CREATE UNIQUE NONCLUSTERED INDEX IX_Consultations_MedicalCaseId
ON Consultations(MedicalCaseId) WHERE IsDeleted = 0;

-- 诊断日期索引（查询优化）
CREATE NONCLUSTERED INDEX IX_Consultations_CreatedAt
ON Consultations(CreatedAt DESC)
INCLUDE (MedicalCaseId, TCMDiagnosis)
WHERE IsDeleted = 0;

-- 舌诊关联索引
CREATE UNIQUE NONCLUSTERED INDEX IX_TongueAnalysis_ConsultationId
ON TongueAnalysis(ConsultationId) WHERE IsDeleted = 0;

-- 脉诊关联索引
CREATE UNIQUE NONCLUSTERED INDEX IX_PulseAnalysis_ConsultationId
ON PulseAnalysis(ConsultationId) WHERE IsDeleted = 0;

-- 辨证关联索引
CREATE UNIQUE NONCLUSTERED INDEX IX_SyndromeAnalysis_ConsultationId
ON SyndromeAnalysis(ConsultationId) WHERE IsDeleted = 0;
```

### 查询优化

**关联数据预加载**:
```csharp
// ConsultationRepository.cs: GetByIdWithDetailsAsync
public async Task<Consultation?> GetByIdWithDetailsAsync(Guid id)
{
    return await _dbSet
        .Include(c => c.MedicalCase)
        .Include(c => c.TongueAnalysis)
        .Include(c => c.PulseAnalysis)
        .Include(c => c.SyndromeAnalysis)
        .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
}
```

**分页查询优化**:
```csharp
// 按医生查询诊断记录
public async Task<PagedResult<ConsultationDto>> GetPagedByDoctorAsync(
    Guid doctorId, int page, int pageSize)
{
    var query = _dbSet
        .Include(c => c.MedicalCase)
        .Where(c => c.MedicalCase.DoctorId == doctorId && !c.IsDeleted)
        .OrderByDescending(c => c.CreatedAt);

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return new PagedResult<ConsultationDto>
    {
        Items = _mapper.Map<List<ConsultationDto>>(items),
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
```

### 性能基准

| 操作 | 目标性能 | 说明 |
|------|---------|------|
| 创建诊断记录 | < 200ms | 包含四诊+舌诊+脉诊+辨证 |
| 查询单条记录(含关联) | < 100ms | Include 4个关联表 |
| 按病案查询 | < 50ms | 索引优化 |
| 舌象图片上传 | < 2s | 5MB图片 |
| 分页查询(20条) | < 150ms | 包含MedicalCase关联 |

---

## 相关文档

**Tutorial**:
- [中医诊断快速入门](../../../tutorials/modules/consultation/tcm-diagnosis-tutorial.md)

**How-to**:
- [中医诊断问题解决指南](../../../how-to-guides/modules/consultation/consultation-issues.md)

**Reference**:
- [Consultation API参考](../../../reference/api/consultation.md)

**Business Domain**:
- [中医诊断理论体系](../../business-domain/tcm-diagnostic-theory.md)
- [中医辨证方法](../../business-domain/tcm-syndrome-differentiation.md)
- [中医术语词汇表](../../business-domain/tcm-terminology-glossary.md)

---

**文档版本**: v1.0
**更新日期**: 2025-01-22
**维护团队**: LYBTZYZS开发组

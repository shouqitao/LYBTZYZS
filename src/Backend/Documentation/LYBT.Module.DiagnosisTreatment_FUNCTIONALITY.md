# LYBT.Module.DiagnosisTreatment 功能说明文档

## 模块概述

诊疗模块是中医诊疗系统的核心业务模块，负责完整的诊疗流程管理，包括患者主诉收集、现病史记录、诊断确定、治疗方案制定以及药方配置等关键环节。本模块实现了结构化的诊疗数据管理，支持多种治疗项目组合和个性化药方配制，为中医诊疗提供全面的数字化支持。

## 业务价值

- **完整诊疗流程**: 从主诉收集到治疗方案制定的全流程数字化管理
- **结构化数据**: 支持现病史、诊断、治疗项目的结构化存储和检索
- **个性化药方**: 支持为每次诊疗配制专门的治疗药方
- **多样治疗项目**: 支持针灸、正骨等多种中医治疗方式的组合应用
- **费用透明**: 自动计算治疗项目和药方费用，提供透明的价格信息
- **数据追溯**: 完整的诊疗记录便于历史查询和疗效分析

## 数据模型

### DiagnosisTreatmentModel (诊疗主实体)

**文件位置**: `LYBT.Module.DiagnosisTreatment/Models/DiagnosisTreatmentModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Id | Guid | 诊疗ID（主键） | 自动生成 | 诊疗记录唯一标识 |
| PatientId | Guid | 病人ID（外键） | 必填，关联患者表 | 建立诊疗与患者关联 |
| ChiefComplaint | string? | 主诉 | 可选，最大长度建议1000字符 | 记录患者主要症状描述 |
| PresentIllness | string? | 现病史（结构化文本） | 可选，支持结构化存储 | 详细记录病史发展过程 |
| DiagnosisCatalogId | Guid | 诊断类型ID | 必填，引用诊断目录 | 标准化诊断分类管理 |
| Diagnosis | string | 诊断内容 | 必填，具体诊断描述 | 记录医生的诊断结论 |
| Treatments | List&lt;TreatmentItemModel&gt; | 治疗项目列表 | 集合类型，可包含多个治疗项目 | 支持多种治疗方式组合 |
| Formula | FormulaModel? | 治疗药方 | 可选，独立药方配置 | 个性化药方管理 |
| CreateTime | DateTime | 诊疗创建时间 | 自动设置当前时间 | 诊疗时间记录和排序 |

### TreatmentItemModel (治疗项目实体)

**文件位置**: `LYBT.Module.DiagnosisTreatment/Models/TreatmentItemModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Name | string | 治疗项目名称 | 必填，如"针灸"、"正骨" | 治疗项目标识 |
| Count | int | 治疗次数 | 正整数，默认1 | 治疗频次控制 |
| Price | decimal | 单价 | 正数，两位小数 | 单次治疗费用 |
| Subtotal | decimal | 小计（计算属性） | Price × Count | 自动计算项目总费用 |

### FormulaModel (药方实体)

**文件位置**: `LYBT.Module.DiagnosisTreatment/Models/FormulaModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| Name | string | 药方名称 | 必填，建议最大100字符 | 药方标识和管理 |
| Herbs | List&lt;HerbItemModel&gt; | 药材明细列表 | 至少包含一味药材 | 药方组成管理 |
| TotalPrice | decimal | 药方总价（计算属性） | 所有药材费用之和 | 自动计算药方总费用 |

### HerbItemModel (药材明细实体)

**文件位置**: `LYBT.Module.DiagnosisTreatment/Models/HerbItemModel.cs`

| 字段名 | 类型 | 说明 | 验证规则 | 业务用途 |
|--------|------|------|----------|----------|
| HerbId | Guid | 药材ID | 必填，关联药材主数据 | 建立与药材库的关联 |
| Name | string | 药材名称 | 必填，冗余存储便于显示 | 药材标识和显示 |
| Amount | decimal | 剂量 | 正数，支持小数 | 药材用量控制 |
| UnitPrice | decimal | 单价 | 正数，两位小数 | 药材单位价格 |
| TotalPrice | decimal | 小计（计算属性） | UnitPrice × Amount | 自动计算药材费用 |

## DTO 数据传输对象

### DiagnosisTreatmentCreateDto (新增诊疗)

**使用场景**: 创建新的诊疗记录
**特点**: 包含完整的诊疗信息，支持治疗项目和药方的同时配置

```csharp
- PatientId: 病人ID（必填，Guid类型）
- ChiefComplaint: 主诉（可选，string类型）
- PresentIllness: 现病史（可选，结构化文本）
- DiagnosisCatalogId: 诊断类型ID（必填，关联诊断目录）
- Diagnosis: 诊断内容（必填，具体诊断描述）
- Treatments: 治疗项目列表（List<TreatmentItemDto>）
- Formula: 治疗药方（可选，FormulaDto类型）
```

### DiagnosisTreatmentDetailDto (诊疗详情)

**使用场景**: 查看完整的诊疗记录详情
**特点**: 包含关联信息（如患者姓名）和完整的诊疗数据

```csharp
- Id: 诊疗ID
- PatientId: 病人ID
- PatientName: 病人姓名（关联查询）
- ChiefComplaint: 主诉
- PresentIllness: 现病史
- DiagnosisCatalogId: 诊断类型ID
- Diagnosis: 诊断内容
- Treatments: 治疗项目列表
- Formula: 治疗药方
- CreateTime: 诊疗时间
```

### DiagnosisTreatmentDto (诊疗列表)

**使用场景**: 诊疗记录列表展示
**特点**: 精简信息，适合列表显示和检索

### DiagnosisTreatmentEditDto (编辑诊疗)

**使用场景**: 修改现有诊疗记录
**特点**: 包含ID标识和可修改的诊疗信息

### TreatmentItemDto (治疗项目传输)

**使用场景**: 治疗项目的前后端数据传输
**特点**: 简化的治疗项目信息，用于表单填写和显示

### FormulaDto (药方传输)

**使用场景**: 药方信息的前后端传输
**特点**: 完整的药方信息，包含药材列表和计算字段

## 服务层 (IDiagnosisTreatmentService & DiagnosisTreatmentService)

### 诊疗管理方法

#### GetByIdAsync

```csharp
Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 获取指定诊疗记录的详细信息
**业务逻辑**: 
- 根据ID查询诊疗记录
- 包含关联的治疗项目和药方信息
- 使用AutoMapper进行实体到DTO的转换
- 处理数据不存在的情况

**使用场景**: 诊疗详情页面、编辑前数据加载

#### GetListAsync

```csharp
Task<List<DiagnosisTreatmentDto>> GetListAsync()
```

**功能**: 获取诊疗记录列表
**业务逻辑**: 
- 查询所有诊疗记录
- 按创建时间排序
- 返回精简的列表信息

**使用场景**: 诊疗记录列表页面、检索功能

#### AddAsync

```csharp
Task<bool> AddAsync(DiagnosisTreatmentCreateDto dto)
```

**功能**: 创建新的诊疗记录
**业务逻辑**: 
- 验证输入数据的完整性
- 生成新的诊疗ID
- 设置创建时间
- 处理治疗项目和药方的级联创建
- 使用AutoMapper进行DTO到实体的转换

**特殊处理**:
- 自动生成GUID主键
- 验证患者ID的有效性
- 确保诊断目录ID的存在性
- 计算治疗项目和药方的费用

**使用场景**: 新诊疗记录创建、诊疗流程结束时保存

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto)
```

**功能**: 更新现有诊疗记录
**业务逻辑**: 
- 验证诊疗记录的存在性
- 更新可修改的字段
- 处理治疗项目和药方的变更
- 保持创建时间不变

**特殊处理**:
- 增量更新策略
- 关联数据的同步更新
- 数据变更历史记录

**使用场景**: 诊疗记录修正、补充诊疗信息

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除指定的诊疗记录
**业务逻辑**: 
- 验证诊疗记录的存在性
- 级联删除关联的治疗项目和药方
- 考虑软删除策略

**安全考虑**:
- 验证删除权限
- 检查是否存在关联的处方或账单
- 记录删除操作日志

**使用场景**: 错误记录清理、数据维护

## 仓储层 (IDiagnosisTreatmentRepository & DiagnosisTreatmentRepository)

### 基础数据操作

#### GetByIdAsync

```csharp
Task<DiagnosisTreatmentModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取诊疗实体
**实现细节**: 
- 使用EF Core的FindAsync方法
- 支持Include加载关联数据
- 处理数据不存在的情况

#### GetListAsync

```csharp
Task<List<DiagnosisTreatmentModel>> GetListAsync()
```

**功能**: 获取所有诊疗记录
**实现细节**: 
- 返回完整的诊疗记录列表
- 包含关联的治疗项目和药方数据
- 可扩展为支持分页和筛选

#### AddAsync

```csharp
Task<bool> AddAsync(DiagnosisTreatmentModel model)
```

**功能**: 新增诊疗记录到数据库
**实现细节**: 
- 使用EF Core的Add方法
- 级联保存关联数据
- 事务性操作确保数据一致性

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(DiagnosisTreatmentModel model)
```

**功能**: 更新诊疗记录
**实现细节**: 
- 使用EF Core的Update方法
- 处理实体状态跟踪
- 更新关联的子实体

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除诊疗记录
**实现细节**: 
- 先查询再删除的安全模式
- 级联删除关联数据
- 返回操作结果状态

## 权限控制策略

### 操作权限

- **查看权限**: 医生可查看自己的诊疗记录，管理员可查看所有记录
- **创建权限**: 只有执业医生可以创建诊疗记录
- **修改权限**: 医生可修改自己创建的诊疗记录，限定时间内可修改
- **删除权限**: 需要管理员权限，且只能删除未关联处方的记录

### 数据安全

- **患者隐私**: 诊疗信息按患者隐私保护要求进行访问控制
- **数据完整性**: 关键字段（如诊断内容）不允许为空
- **审计跟踪**: 所有诊疗操作都需要记录操作者和操作时间

## 日志审计机制

### 操作日志

所有诊疗相关操作都会记录详细日志：

- **诊疗创建**: 记录医生、患者、诊疗内容摘要
- **诊疗修改**: 记录修改前后的关键字段变更
- **诊疗删除**: 记录删除原因和完整的诊疗信息
- **查询操作**: 记录敏感信息的访问日志

### 业务日志

- **费用计算**: 记录治疗项目和药方费用的计算过程
- **关联操作**: 记录与患者、处方等关联数据的操作
- **异常处理**: 记录业务异常和错误处理过程

### 审计内容

- 操作时间和操作者信息
- 操作类型和操作对象
- 关键数据的变更前后状态
- 客户端信息和会话标识

## 集成依赖

### 外部模块依赖

- **LYBT.Module.Patients**: 患者基础信息查询和验证
- **LYBT.Module.Herbs**: 药材基础数据和价格信息
- **LYBT.Module.Prescriptions**: 处方生成和管理
- **LYBT.Infrastructure.Configuration**: 诊断目录配置管理

### 基础服务依赖

- **IUnifiedLogService**: 统一日志服务
- **IMapper**: AutoMapper对象映射服务
- **DiagnosisTreatmentDbContext**: 专用数据库上下文

## 使用示例

### 创建完整诊疗记录

```csharp
var createDto = new DiagnosisTreatmentCreateDto
{
    PatientId = patientId,
    ChiefComplaint = "头痛、失眠，持续3天",
    PresentIllness = "患者3天前开始出现头痛症状，伴有失眠，夜间难以入睡",
    DiagnosisCatalogId = diagnosisCatalogId,
    Diagnosis = "肝阳上亢，心肾不交",
    Treatments = new List<TreatmentItemDto>
    {
        new TreatmentItemDto 
        { 
            Name = "针灸", 
            Count = 1, 
            Price = 80.00m 
        },
        new TreatmentItemDto 
        { 
            Name = "拔罐", 
            Count = 1, 
            Price = 30.00m 
        }
    },
    Formula = new FormulaDto
    {
        Name = "安神定志汤",
        Herbs = new List<HerbItemDto>
        {
            new HerbItemDto 
            { 
                HerbId = herb1Id, 
                Name = "酸枣仁", 
                Amount = 15, 
                UnitPrice = 12.00m 
            },
            new HerbItemDto 
            { 
                HerbId = herb2Id, 
                Name = "龙骨", 
                Amount = 20, 
                UnitPrice = 8.00m 
            }
        }
    }
};

var success = await diagnosisService.AddAsync(createDto);
if (success)
{
    logger.LogInformation("诊疗记录创建成功，患者ID: {PatientId}", createDto.PatientId);
}
```

### 查询诊疗详情

```csharp
var diagnosisId = Guid.Parse("diagnosis-guid-here");
var diagnosisDetail = await diagnosisService.GetByIdAsync(diagnosisId);

if (diagnosisDetail != null)
{
    Console.WriteLine($"患者: {diagnosisDetail.PatientName}");
    Console.WriteLine($"诊断: {diagnosisDetail.Diagnosis}");
    Console.WriteLine($"治疗项目数: {diagnosisDetail.Treatments.Count}");
    
    // 计算总费用
    var treatmentCost = diagnosisDetail.Treatments.Sum(t => t.Subtotal);
    var formulaCost = diagnosisDetail.Formula?.TotalPrice ?? 0;
    var totalCost = treatmentCost + formulaCost;
    
    Console.WriteLine($"总费用: {totalCost:C}");
}
```

### 更新诊疗记录

```csharp
var editDto = new DiagnosisTreatmentEditDto
{
    Id = existingDiagnosisId,
    ChiefComplaint = "更新后的主诉内容",
    PresentIllness = "补充的病史信息",
    Diagnosis = "调整后的诊断结论",
    // 保持其他字段不变或按需更新
};

var updateSuccess = await diagnosisService.UpdateAsync(editDto);
if (updateSuccess)
{
    logger.LogInformation("诊疗记录更新成功，ID: {DiagnosisId}", editDto.Id);
}
```

### 费用计算示例

```csharp
// 计算治疗项目总费用
public decimal CalculateTreatmentCost(List<TreatmentItemDto> treatments)
{
    return treatments.Sum(t => t.Price * t.Count);
}

// 计算药方总费用
public decimal CalculateFormulaCost(FormulaDto formula)
{
    return formula.Herbs.Sum(h => h.UnitPrice * h.Amount);
}

// 计算诊疗总费用
public decimal CalculateTotalCost(DiagnosisTreatmentDetailDto diagnosis)
{
    var treatmentCost = CalculateTreatmentCost(diagnosis.Treatments);
    var formulaCost = diagnosis.Formula != null ? 
        CalculateFormulaCost(diagnosis.Formula) : 0;
    
    return treatmentCost + formulaCost;
}
```

## 业务扩展建议

### 功能增强

- **诊疗模板**: 支持常见诊疗的模板化管理
- **疗效跟踪**: 增加治疗效果评估和跟踪功能
- **智能推荐**: 基于历史诊疗数据的治疗方案推荐
- **费用预估**: 治疗前的费用预估和患者告知

### 数据分析

- **诊疗统计**: 按病种、医生、时间段的诊疗统计分析
- **费用分析**: 治疗费用的构成分析和趋势预测
- **疗效评估**: 基于回访数据的疗效评估体系

### 质量管理

- **诊疗规范**: 建立诊疗质量控制和规范检查机制
- **知识库**: 集成中医诊疗知识库和临床决策支持
- **培训管理**: 基于诊疗数据的医生培训和考核体系
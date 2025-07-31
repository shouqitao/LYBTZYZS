# LYBT.Module.Prescriptions 功能说明文档

## 模块概述

处方管理模块负责中医处方的完整生命周期管理，包括处方开具、药品配伍、状态跟踪、配药管理等功能。本模块与患者、医生、药材模块深度集成，支持处方状态流转、明细管理和历史追踪，为中医诊疗提供核心业务支持。

## 数据模型

### PrescriptionModel (处方主表实体)

**文件位置**: `Models/PrescriptionModel.cs`

| 字段名        | 类型                  | 说明         | 验证规则             |
| ---------- | ------------------- | ---------- | ---------------- |
| Id         | Guid                | 处方唯一标识（主键） | 必填               |
| PatientId  | Guid                | 患者ID       | 必填，外键关联患者表       |
| DoctorId   | Guid                | 医生ID       | 必填，外键关联医生表       |
| CreateTime | DateTime            | 开具时间       | 必填，系统自动设置        |
| Diagnosis  | string?             | 诊断信息       | 最长256字符，可选       |
| Remark     | string?             | 备注说明       | 最长256字符，可选       |
| Status     | PrescriptionStatus  | 处方状态       | 枚举值，默认Draft      |
| Items      | List&lt;PrescriptionItemModel&gt; | 处方明细列表     | 导航属性，一对多关系       |

### PrescriptionItemModel (处方明细实体)

**文件位置**: `Models/PrescriptionItemModel.cs`

| 字段名            | 类型         | 说明         | 验证规则          |
| -------------- | ---------- | ---------- | ------------- |
| Id             | Guid       | 明细唯一标识（主键） | 必填            |
| PrescriptionId | Guid       | 处方ID       | 必填，外键关联处方表    |
| HerbId         | Guid       | 药材ID       | 必填，外键关联药材表    |
| HerbName       | string     | 药材名称       | 最长64字符，必填     |
| Quantity       | decimal    | 用量         | decimal(18,2) |
| Unit           | string?    | 单位         | 最长16字符，可选     |
| Usage          | string?    | 用法用量       | 最长64字符，可选     |

### 枚举类型

#### PrescriptionStatus (处方状态)
- `Draft (0)`: 草稿 - 医生正在编辑，未正式开具
- `Issued (1)`: 已开具 - 医生确认开具，等待配药
- `Dispensed (2)`: 已配药 - 药房已配药，等待患者取药
- `Completed (3)`: 已完成 - 患者已取药，处方完成
- `Cancelled (-1)`: 已取消 - 处方被取消，不再有效

#### FormulaType (配方类型)
- `Decoction (1)`: 汤剂
- `Pill (2)`: 丸剂  
- `Powder (3)`: 散剂
- `Paste (4)`: 膏剂
- `External (5)`: 外用

## DTO 数据传输对象

### PrescriptionDto (处方列表展示)

**使用场景**: 处方列表展示、简单处方信息返回
**特点**: 包含处方基本信息，不包含明细

```csharp
- Id: 处方ID
- PatientId: 患者ID
- DoctorId: 医生ID
- CreateTime: 开具时间
- Status: 处方状态
```

### PrescriptionDetailDto (处方详情)

**使用场景**: 处方详情展示、完整处方信息查看
**特点**: 包含处方完整信息和明细列表

```csharp
- Id: 处方ID
- PatientId: 患者ID
- DoctorId: 医生ID
- CreateTime: 开具时间
- Diagnosis: 诊断信息
- Remark: 备注说明
- Status: 处方状态
- Items: 处方明细列表（PrescriptionItemDto集合）
```

### PrescriptionItemDto (处方明细展示)

**使用场景**: 处方明细展示，与处方详情配合使用

```csharp
- Id: 明细ID
- HerbId: 药材ID
- HerbName: 药材名称
- Quantity: 用量
- Unit: 单位
- Usage: 用法用量
```

### PrescriptionCreateDto (处方创建)

**使用场景**: 医生开具新处方
**特点**: 包含数据验证规则

```csharp
- PatientId: 患者ID（必填）
- DoctorId: 医生ID（必填）
- Diagnosis: 诊断信息（可选）
- Remark: 备注说明（可选）
- Status: 处方状态（默认Draft）
- Items: 处方明细列表（PrescriptionItemCreateDto集合）
```

### PrescriptionItemCreateDto (处方明细创建)

**使用场景**: 创建处方明细，与处方创建配合使用

```csharp
- HerbId: 药材ID（必填）
- HerbName: 药材名称（必填）
- Quantity: 用量
- Unit: 单位（可选）
- Usage: 用法用量（可选）
```

### PrescriptionEditDto (处方编辑)

**使用场景**: 编辑处方信息
**特点**: 继承自PrescriptionCreateDto，包含ID字段

```csharp
- Id: 处方ID（必填，标识更新目标）
- 其他字段同PrescriptionCreateDto
```

## 服务层 (IPrescriptionService & PrescriptionService)

### 基础CRUD方法

#### GetAllAsync

```csharp
Task<List<PrescriptionDto>> GetAllAsync()
```

**功能**: 获取所有处方列表
**特点**: 返回简单处方信息，不包含明细
**使用场景**: 处方管理页面的列表展示

#### GetByIdAsync

```csharp
Task<PrescriptionDetailDto?> GetByIdAsync(string id)
```

**功能**: 根据ID获取处方详情
**特点**: 包含完整处方信息和明细列表
**使用场景**: 处方详情页面、配药确认

#### CreateAsync

```csharp
Task<bool> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)
```

**功能**: 创建新处方
**业务逻辑**: 
- 患者ID和医生ID必填验证
- 处方明细验证（至少一个药材）
- 药材ID和名称验证
- 用量有效性验证
- 操作人信息记录

**使用场景**: 医生开具处方

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)
```

**功能**: 更新处方信息
**业务逻辑**: 
- 处方ID验证
- 状态检查（只有草稿状态可编辑）
- 明细更新（删除原明细，重新创建）
- 操作日志记录

**使用场景**: 医生修改草稿处方

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName)
```

**功能**: 删除处方
**业务逻辑**: 
- 状态检查（只有草稿状态可删除）
- 级联删除处方明细
- 操作日志记录

**使用场景**: 删除草稿处方

#### CancelAsync

```csharp
Task<bool> CancelAsync(string id, Guid operatorId, string operatorName)
```

**功能**: 取消处方
**业务逻辑**: 
- 状态检查（已开具但未配药的处方可取消）
- 更改状态为Cancelled
- 操作日志记录

**使用场景**: 医生或管理员取消已开具处方

### 扩展业务方法

#### GetByPatientIdAsync (建议扩展)

```csharp
Task<List<PrescriptionDto>> GetByPatientIdAsync(Guid patientId, int days = 30)
```

**功能**: 获取患者指定时间内的处方列表
**参数**: days - 查询天数（默认30天）
**使用场景**: 患者历史处方查询

#### GetByDoctorIdAsync (建议扩展)

```csharp
Task<List<PrescriptionDto>> GetByDoctorIdAsync(Guid doctorId, int days = 30)
```

**功能**: 获取医生指定时间内开具的处方列表
**使用场景**: 医生工作量统计

#### GetByStatusAsync (建议扩展)

```csharp
Task<List<PrescriptionDto>> GetByStatusAsync(PrescriptionStatus status)
```

**功能**: 根据状态获取处方列表
**使用场景**: 药房配药队列、待处理处方查询

#### IssueAsync (建议扩展)

```csharp
Task<bool> IssueAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 正式开具处方（从草稿变为已开具）
**业务逻辑**: 
- 状态检查（只有草稿状态可开具）
- 药材库存检查
- 状态更新为Issued
- 操作日志记录

**使用场景**: 医生确认开具处方

#### DispenseAsync (建议扩展)

```csharp
Task<bool> DispenseAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 配药完成（从已开具变为已配药）
**业务逻辑**: 
- 状态检查（只有已开具状态可配药）
- 库存扣减
- 状态更新为Dispensed
- 操作日志记录

**使用场景**: 药房配药完成

#### CompleteAsync (建议扩展)

```csharp
Task<bool> CompleteAsync(Guid id, Guid operatorId, string operatorName)
```

**功能**: 完成处方（患者取药）
**业务逻辑**: 
- 状态检查（只有已配药状态可完成）
- 状态更新为Completed
- 操作日志记录

**使用场景**: 患者取药完成

### 统计分析方法 (建议扩展)

#### GetStatisticsAsync

```csharp
Task<PrescriptionStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate)
```

**功能**: 获取处方统计信息
**统计内容**: 
- 各状态处方数量
- 医生开具处方数量排名
- 常用药材统计
- 平均处方金额

**使用场景**: 数据分析、报表展示

## 仓储层 (IPrescriptionRepository & PrescriptionRepository)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<PrescriptionModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取处方实体
**特点**: 包含处方明细的导航属性
**使用场景**: 服务层调用的底层数据操作

#### GetListAsync

```csharp
Task<List<PrescriptionModel>> GetListAsync()
```

**功能**: 获取所有处方实体列表
**使用场景**: 批量操作、全量查询

#### AddAsync

```csharp
Task<bool> AddAsync(PrescriptionModel model)
```

**功能**: 新增处方
**特点**: 同时保存处方明细
**使用场景**: 创建新处方

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(PrescriptionModel model)
```

**功能**: 更新处方
**特点**: 处理明细的增删改
**使用场景**: 处方信息修改

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除处方
**特点**: 级联删除处方明细
**使用场景**: 物理删除处方

#### CancelAsync

```csharp
Task<bool> CancelAsync(Guid id)
```

**功能**: 取消处方
**特点**: 仅更新状态，不删除数据
**使用场景**: 处方取消操作

### 扩展查询方法 (建议扩展)

#### GetByPatientIdAsync

```csharp
Task<List<PrescriptionModel>> GetByPatientIdAsync(Guid patientId, DateTime? startDate = null, DateTime? endDate = null)
```

**功能**: 根据患者ID查询处方
**使用场景**: 患者历史处方查询

#### GetByDoctorIdAsync

```csharp
Task<List<PrescriptionModel>> GetByDoctorIdAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
```

**功能**: 根据医生ID查询处方
**使用场景**: 医生处方查询

#### GetByStatusAsync

```csharp
Task<List<PrescriptionModel>> GetByStatusAsync(PrescriptionStatus status)
```

**功能**: 根据状态查询处方
**使用场景**: 状态筛选查询

#### GetPagedAsync

```csharp
Task<(List<PrescriptionModel> list, int total)> GetPagedAsync(PrescriptionQueryDto query)
```

**功能**: 分页查询处方
**查询条件**: 患者、医生、状态、时间范围等
**使用场景**: 分页列表展示

## 权限控制策略

### 角色级别权限

- **医生**: 可开具、修改自己的草稿处方，查看自己开具的所有处方
- **药房人员**: 可查看已开具处方，进行配药操作，不能修改处方内容
- **管理员**: 可查看和操作所有处方，可取消任何状态的处方
- **普通用户**: 只能查看，不能进行任何操作

### 操作权限

- **创建处方**: 仅医生可操作
- **编辑处方**: 仅医生可编辑自己的草稿处方
- **开具处方**: 仅医生可将自己的草稿处方开具
- **配药操作**: 仅药房人员和管理员可操作
- **取消处方**: 医生可取消自己的处方，管理员可取消任何处方

### 数据访问控制

- 医生只能访问自己开具的处方
- 药房人员可访问所有已开具状态的处方
- 患者相关人员可查看该患者的处方历史
- 敏感信息需要权限验证

## 业务规则

### 处方状态流转

```
Draft → Issued → Dispensed → Completed
  ↓         ↓
Cancelled  Cancelled
```

**状态流转规则**:
- 草稿可以编辑、删除、开具、取消
- 已开具可以配药、取消，不能编辑
- 已配药可以完成，不能取消或编辑
- 已完成和已取消为终态，不能再变更

### 数据完整性

- **处方明细**: 每个处方至少包含一个药材
- **药材验证**: 药材必须是可用状态
- **用量验证**: 用量必须大于0
- **关联验证**: 患者和医生必须存在且有效

### 业务约束

- **编辑限制**: 只有草稿状态可编辑
- **删除限制**: 只有草稿状态可删除
- **库存检查**: 配药时需检查库存充足
- **权限验证**: 医生只能操作自己的处方

## 集成依赖

### 模块依赖

- **LYBT.Module.Patients**: 患者模块（患者信息验证）
- **LYBT.Module.Doctors**: 医生模块（医生信息验证）
- **LYBT.Module.Herbs**: 药材模块（药材信息和库存）
- **LYBT.Infrastructure**: 基础设施（日志、缓存、配置）

### 外部集成

- **库存系统**: 药材库存查询和扣减
- **打印系统**: 处方打印功能
- **收费系统**: 处方费用计算
- **病历系统**: 处方与病历关联

## 使用示例

### 医生开具处方

```csharp
var createDto = new PrescriptionCreateDto {
    PatientId = patientId,
    DoctorId = doctorId,
    Diagnosis = "风寒感冒",
    Remark = "忌辛辣食物",
    Status = PrescriptionStatus.Draft,
    Items = new List<PrescriptionItemCreateDto> {
        new() {
            HerbId = herbId1,
            HerbName = "麻黄",
            Quantity = 10m,
            Unit = "克",
            Usage = "先煎10分钟"
        },
        new() {
            HerbId = herbId2,
            HerbName = "桂枝",
            Quantity = 15m,
            Unit = "克",
            Usage = "后下"
        }
    }
};

var result = await prescriptionService.CreateAsync(createDto, doctorId, "张医生");
```

### 正式开具处方

```csharp
// 医生确认开具处方
var issueResult = await prescriptionService.IssueAsync(prescriptionId, doctorId, "张医生");
```

### 药房配药

```csharp
// 药房人员配药
var dispenseResult = await prescriptionService.DispenseAsync(prescriptionId, pharmacistId, "李药师");
```

### 查询患者处方历史

```csharp
var patientPrescriptions = await prescriptionService.GetByPatientIdAsync(patientId, 90); // 90天内
```

### 处方状态查询

```csharp
// 查询待配药处方
var pendingPrescriptions = await prescriptionService.GetByStatusAsync(PrescriptionStatus.Issued);

// 查询医生今日处方
var doctorTodayPrescriptions = await prescriptionService.GetByDoctorIdAsync(doctorId, 1);
```

### 处方统计

```csharp
var statistics = await prescriptionService.GetStatisticsAsync(
    DateTime.Today.AddDays(-30), 
    DateTime.Today
);

Console.WriteLine($"本月处方总数: {statistics.TotalCount}");
Console.WriteLine($"已完成处方: {statistics.CompletedCount}");
Console.WriteLine($"平均处方金额: {statistics.AverageAmount:C}");
```

## 扩展建议

### 功能扩展

- **处方模板**: 常用处方模板管理
- **药物相互作用**: 药材配伍禁忌检查
- **剂量计算**: 根据患者体重、年龄自动计算剂量
- **费用计算**: 处方总费用自动计算
- **电子签名**: 医生电子签名验证

### 技术优化

- **并发控制**: 处方编辑时的并发锁定
- **缓存策略**: 常用药材信息缓存
- **消息队列**: 处方状态变更事件通知
- **审计日志**: 详细的操作审计记录
- **数据导出**: 处方数据导出和报表生成

### 集成增强

- **移动端**: 移动端处方查看和操作
- **打印优化**: 处方格式化打印
- **条码支持**: 处方条码生成和识别
- **语音输入**: 医生语音录入处方
- **AI辅助**: 智能处方推荐和审核
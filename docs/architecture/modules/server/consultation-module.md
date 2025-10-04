# Consultation模块设计 - Server端

## 📋 模块概述
**职责**：中医诊疗记录管理、四诊信息记录、辨证论治、医嘱管理
**命名空间**：`LYBT.Module.Consultation`
**API路径**：`/api/v1/consultation/*`（通过统一控制器）

## 🏗️ 架构设计

### 分层结构
```
├── Services/             # 业务服务
│   ├── ConsultationService.cs          # 诊疗业务服务
│   ├── ConsultationQueryService.cs     # 诊疗查询服务
│   └── DiagnosisService.cs             # 诊断相关服务
├── Interfaces/           # 服务接口
│   ├── IConsultationService.cs
│   ├── IConsultationQueryService.cs
│   ├── IConsultationRepository.cs
│   ├── IConsultationRecordRepository.cs
│   └── IDiagnosisService.cs
├── Repositories/         # 数据访问
│   ├── ConsultationRepository.cs
│   └── ConsultationRecordRepository.cs
├── Validators/           # 验证器
│   ├── ConsultationCreateDtoValidator.cs
│   └── DiagnosisDtoValidator.cs
├── Mapping/             # 对象映射
│   └── ConsultationMappingProfile.cs
├── Health/              # 健康检查
├── Options/             # 配置选项
└── ConsultationModule.cs # 模块注册
```

## 🔌 API接口设计

### 核心CRUD操作
由于模块采用统一控制器模式，API通过泛型控制器暴露：

#### GET /api/v1/consultation
**功能**：分页获取诊疗记录列表
```csharp
// Query Parameters
{
  "page": 1,
  "pageSize": 20,
  "keyword": "可选搜索关键词"
}

// Response 200
{
  "items": [...],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 20
}
```

#### GET /api/v1/consultation/{id}
**功能**：获取诊疗记录详情
```csharp
// Response 200 - ConsultationDto
{
  "id": "guid",
  "medicalCaseId": "guid",
  "patientId": "guid",
  "userId": "guid",
  "patientName": "患者姓名",
  "doctorName": "医生姓名",
  "chiefComplaint": "主诉内容",
  "presentIllness": "现病史",
  "inspection": "望诊结果",
  "auscultationOlfaction": "闻诊结果",
  "inquiry": "问诊结果",
  "palpation": "切诊结果",
  "tcmDiagnosis": "中医辨证",
  "treatmentPrinciple": "治疗原则",
  "startTime": "2024-12-31T10:00:00Z",
  "endTime": "2024-12-31T11:00:00Z",
  "consultationStatus": "Completed",
  "remark": "备注信息"
}
```

#### POST /api/v1/consultation
**功能**：创建新诊疗记录
```csharp
// Request - ConsultationCreateDto
{
  "medicalCaseId": "guid",
  "patientId": "guid", 
  "userId": "guid",
  "chiefComplaint": "主诉",
  "presentIllness": "现病史",
  "inspection": "望诊",
  "auscultationOlfaction": "闻诊", 
  "inquiry": "问诊",
  "palpation": "切诊",
  "tcmDiagnosis": "中医辨证",
  "treatmentPrinciple": "治疗原则",
  "medicalAdvice": "医嘱",
  "startTime": "2024-12-31T10:00:00Z",
  "remark": "备注"
}

// Response 201 - ConsultationDto
```

#### PUT /api/v1/consultation/{id}
**功能**：更新诊疗记录
```csharp
// Request - ConsultationUpdateDto
{
  "id": "guid",
  "chiefComplaint": "更新的主诉",
  "tcmDiagnosis": "更新的中医辨证",
  "consultationStatus": "Completed",
  "endTime": "2024-12-31T11:00:00Z",
  "remark": "更新备注"
}
```

#### DELETE /api/v1/consultation/{id}
**功能**：删除诊疗记录（软删除）

## 🔧 核心服务

### ConsultationService
**职责**：诊疗记录业务逻辑处理
```csharp
public interface IConsultationService
{
    Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto);
    Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto);
    Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword);
    Task<ServiceResult> DeleteAsync(Guid id);
}
```

**关键特性**：
- ✅ N+1查询优化：预加载Patient和User信息
- ✅ 自动审计字段处理
- ✅ 软删除支持
- ✅ 异常处理和日志记录

### ConsultationQueryService
**职责**：诊疗记录专门查询服务
```csharp
public interface IConsultationQueryService
{
    Task<PagedResult<ConsultationDto>> GetPagedConsultationsAsync(ConsultationSearchDto searchDto);
    Task<ConsultationDto?> GetConsultationByIdAsync(Guid consultationId);
}
```

### DiagnosisService
**职责**：诊断相关业务逻辑（待实现）

## 📊 数据模型与实体

### 核心实体：Consultation
**表名**：`Consultations`
**继承**：`BaseEntity`（提供审计字段）
**关键特性**：与MedicalCase共享主键，建立1:1关系

```csharp
[Table("Consultations")]
public class Consultation : BaseEntity
{
    // 重要：Id字段与MedicalCase共享主键
    // PatientId和UserId通过MedicalCase获取，不需要重复存储
    
    // 病史信息
    [StringLength(500)]
    public string? ChiefComplaint { get; set; }         // 主诉
    
    [StringLength(1000)]
    public string? PresentIllness { get; set; }         // 现病史
    
    // 中医四诊
    [StringLength(500)]
    public string? Inspection { get; set; }             // 望诊
    
    [StringLength(500)]
    public string? AuscultationOlfaction { get; set; }  // 闻诊
    
    [StringLength(500)]
    public string? Inquiry { get; set; }                // 问诊
    
    [StringLength(500)]
    public string? Palpation { get; set; }              // 切诊
    
    // 诊断结果
    [StringLength(500)]
    public string? TCMDiagnosis { get; set; }           // 中医辨证
    
    [StringLength(500)]
    public string? TreatmentPrinciple { get; set; }     // 治疗原则
    
    [StringLength(1000)]
    public string? MedicalAdvice { get; set; }          // 医嘱
    
    // 状态和备注
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
    
    [StringLength(500)]
    public string? Remark { get; set; }                 // 备注
    
    // 导航属性（必需的，通过共享主键关联）
    [Required]
    public virtual MedicalCase.MedicalCase MedicalCase { get; set; } = null!;
}
```

### DTO模型层次

#### 基础模型
- **ConsultationDto**：主要DTO模型，包含MedicalCaseId、PatientId、UserId等完整字段
- **ConsultationDetailDto**：详情展示用DTO（如需要可扩展）

#### 输入模型  
- **ConsultationInputBaseDto**：输入基类，包含四诊和诊断字段
- **ConsultationCreateDto**：继承输入基类，用于创建诊疗记录
- **ConsultationUpdateDto**：继承输入基类，用于更新诊疗记录

#### 操作模型
- **ConsultationStartDto**：开始诊疗用DTO
- **ConsultationCompleteDto**：完成诊疗用DTO
- **UpdateStatusDto**：状态更新用DTO
- **CancelConsultationDto**：取消诊疗用DTO

#### 查询模型
- **ConsultationQueryDto**：基础查询条件
- **ConsultationSearchDto**：高级搜索条件
- **ConsultationHistoryQueryDto**：患者诊疗历史查询

#### 统计模型
- **ConsultationStatisticsDto**：诊疗统计信息
- **ConsultationScheduleDto**：诊疗日程安排

## 🏛️ 仓储层设计

### ConsultationRepository
**继承**：`BaseRepository<Consultation>`
**接口**：`IConsultationRepository`

**优化策略**：
```csharp
// N+1查询优化示例
public async Task<PagedResult<ConsultationEntity>> GetPagedWithDetailsAsync(
    int pageNumber, int pageSize, string keyword = null)
{
    var query = _dbSet
        .Include(c => c.Patient)  // 预加载患者信息
        .Include(c => c.User)     // 预加载医生信息
        .Where(c => !c.IsDeleted);
    
    // 关键字搜索逻辑
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(c =>
            c.ChiefComplaint.Contains(keyword) ||
            c.TCMDiagnosis.Contains(keyword) ||
            c.Patient.Name.Contains(keyword) ||
            c.User.RealName.Contains(keyword));
    }
    
    // 分页处理...
}
```

**核心方法**：
- `GetPagedWithDetailsAsync`：分页查询含关联数据
- `GetByIdWithDetailsAsync`：单记录查询含所有关联
- `GetByPatientIdAsync`：按患者ID查询
- `GetByMedicalCaseIdAsync`：按医疗案例ID查询

## 📋 数据验证规则

### ConsultationCreateDtoValidator
```csharp
RuleFor(x => x.PatientId)
    .NotEmpty().WithMessage("患者ID不能为空");

RuleFor(x => x.ChiefComplaint)
    .NotEmpty().WithMessage("主诉不能为空")
    .MaximumLength(500).WithMessage("主诉长度不能超过500个字符");

RuleFor(x => x.Diagnosis)
    .NotEmpty().WithMessage("诊断不能为空")
    .MaximumLength(1000).WithMessage("诊断长度不能超过1000个字符");
```

### 字段长度限制
- **主诉**：500字符
- **现病史**：1000字符
- **四诊各项**：500字符
- **中医辨证**：500字符
- **治疗原则**：500字符
- **医嘱**：1000字符
- **备注**：500字符

## 🛡️ 权限与安全

### 数据访问控制
- ✅ 软删除：使用`IsDeleted`标记
- ✅ 审计追踪：继承`BaseEntity`自动记录创建/更新信息
- ✅ 状态管理：通过`CommonStatus`枚举控制记录状态

### 业务规则
- **创建权限**：需要有效的医生ID和患者ID
- **修改权限**：只能修改自己创建的诊疗记录
- **删除权限**：只能软删除，不能物理删除
- **查看权限**：医生可查看自己的诊疗记录

## 🔄 对象映射设计

### AutoMapper配置特点
```csharp
// 状态映射：ConsultationStatus <-> CommonStatus
.ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
    src.ConsultationStatus == ConsultationStatus.Completed
        ? CommonStatus.Disabled
        : CommonStatus.Enabled))

// 时间映射：创建时间映射到诊疗开始时间
.ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.CreatedAt))

// 导航属性忽略策略
.ForMember(dest => dest.Patient, opt => opt.Ignore())
.ForMember(dest => dest.User, opt => opt.Ignore())

// 条件映射：只映射非null值
.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
```

## 📈 诊疗状态流转

### ConsultationStatus枚举
```csharp
public enum ConsultationStatus
{
    Pending = 0,      // 等待开始
    InProgress = 1,   // 诊疗中
    Completed = 2,    // 已完成
    Cancelled = 3     // 已取消
}
```

### 状态流转规则
```
Pending → InProgress → Completed
   ↓           ↓
Cancelled ← Cancelled
```

## 📋 实现状态

### ✅ 已实现
- 完整的CRUD服务实现
- 与MedicalCase的1:1关系（共享主键）
- 全面的DTO模型设计
- 四诊信息记录功能
- 中医辨证论治支持
- 软删除支持
- 审计字段自动记录

### ⚠️ 部分实现
- **DiagnosisService**：接口已定义，具体实现待完善
- **控制器层**：使用统一控制器，无专门控制器
- **健康检查**：已注册但具体实现待补充

### 🔄 待优化
- **诊疗工作流**：开始→进行→完成的完整流程
- **复诊管理**：复诊建议和预约功能
- **诊疗模板**：常用诊疗内容模板化
- **统计报表**：医生工作量、诊疗效果统计
- **审计日志**：诊疗记录变更历史
- **附件管理**：诊疗相关图片、文档附件
- **集成测试**：端到端测试覆盖

## 🧪 测试覆盖

### 单元测试需求
- ConsultationService业务逻辑测试
- ConsultationRepository数据访问测试
- AutoMapper映射测试
- 验证器规则测试

### 集成测试需求
- 诊疗记录CRUD API测试
- 四诊信息完整性测试
- 诊疗状态流转测试
- N+1查询优化验证

## ⚠️ 重要实现说明

### 共享主键设计
Consultation实体与MedicalCase通过**共享主键**建立1:1关系：
- Consultation的Id字段与MedicalCase的Id相同
- 不需要单独存储PatientId和UserId（通过MedicalCase获取）
- 通过EF Core配置建立一对一关系
- 确保了数据一致性和引用完整性

### 实际与文档差异
- **实体关系**：实际使用共享主键，而非独立主键+外键
- **服务接口**：实际接口较简单，无StartAsync等方法
- **查询服务**：部分模块有QueryService分离，但不是全部

## 🔗 依赖关系

### 依赖模块
- **Patients模块**：患者信息管理
- **Users模块**：医生信息管理
- **MedicalCase模块**：医疗案例管理
- **Infrastructure**：数据库上下文和基础服务
- **Shared.Models**：DTO和枚举定义

### 被依赖模块
- **Prescriptions模块**：处方开具依赖诊疗信息
- **统计报表模块**：诊疗数据统计分析
- **WebAPI**：统一控制器暴露API

## 🏥 中医特色功能

### 四诊合参
- **望诊**：观察患者神色、形体、舌象等
- **闻诊**：听声音、嗅气味
- **问诊**：询问症状、病史
- **切诊**：脉诊、按诊

### 辨证论治
- **辨证**：根据四诊信息进行证候分析
- **立法**：确定治疗原则和方法
- **处方**：开具中药处方（关联处方模块）
- **调护**：生活调理建议

### 诊疗记录特点
- 结构化四诊信息录入
- 中医术语标准化
- 证候要素关联
- 诊疗经验积累

## 📝 配置管理

### 模块配置选项
```json
{
  "Modules": {
    "Consultation": {
      "DefaultConsultationDuration": 30,
      "AutoSaveInterval": 5,
      "EnableDiagnosisTemplate": true,
      "MaxHistoryRecords": 100
    }
  }
}
```

## 🔮 扩展规划

### 短期改进
- 诊疗模板管理
- 症状标准化录入
- 诊疗质量评估

### 长期规划
- AI辅助诊断建议
- 诊疗决策支持系统
- 远程诊疗支持
- 多媒体病历集成
# Prescriptions模块设计 - Server端

## 📋 模块概述
**职责**：中医处方管理、处方开具、用药管理、处方审核、价格计算
**命名空间**：`LYBT.Module.Prescriptions`
**API路径**：`/api/v1/prescriptions/*`

## 🏗️ 架构设计

### 分层结构
```
├── Services/                       # 业务服务
│   └── PrescriptionService.cs      # 处方业务逻辑实现
├── Repositories/                   # 数据仓储
│   └── PrescriptionRepository.cs   # 处方数据访问
├── Interfaces/                     # 仓储接口（服务接口在Shared中）
│   └── IPrescriptionRepository.cs  # 仓储接口定义
├── Mapping/                        # AutoMapper配置
│   └── PrescriptionMappingProfile.cs
└── PrescriptionsModule.cs          # 模块依赖注册
```

## 🔌 API接口设计

### GET /api/v1/prescriptions
**功能**：获取处方列表（支持分页和查询）
```csharp
// Query Parameters
{
  "page": 1,
  "pageSize": 20,
  "keyword": "可选关键词"
}

// Response 200
{
  "success": true,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "guid",
        "medicalCaseId": "guid",
        "patientId": "guid",
        "userId": "guid",
        "indication": "主治症状",
        "dosageCount": 7,
        "discount": 1.0,
        "advice": "医嘱",
        "formulaSource": "验方来源",
        "status": "Draft",
        "remark": "备注",
        "items": [...],
        "singleDosePrice": 35.50,
        "totalPrice": 248.50,
        "totalWeight": 140.0,
        "createTime": "2024-12-31T10:00:00Z",
        "updateTime": "2024-12-31T10:30:00Z"
      }
    ],
    "totalCount": 150,
    "currentPage": 1,
    "pageSize": 20
  }
}
```

### GET /api/v1/prescriptions/{id}
**功能**：获取处方详情
```csharp
// Response 200
{
  "success": true,
  "message": "查询成功",
  "data": {
    "id": "guid",
    "medicalCaseId": "guid",
    "patientId": "guid",
    "userId": "guid",
    "indication": "主治症状",
    "dosageCount": 7,
    "discount": 1.0,
    "advice": "医嘱",
    "formulaSource": "验方来源",
    "status": "Draft",
    "remark": "备注",
    "items": [
      {
        "id": "guid",
        "prescriptionId": "guid",
        "herbId": "guid",
        "herbName": "当归",
        "quantity": 10,
        "unit": "g",
        "unitPrice": 2.50,
        "amount": 25.00,
        "usage": "后下",
        "remark": "备注"
      }
    ],
    "singleDosePrice": 35.50,
    "totalPrice": 248.50,
    "totalWeight": 140.0
  }
}

// Response 404
{
  "success": false,
  "message": "处方不存在",
  "code": "PRESCRIPTIONNOTFOUND"
}
```

### POST /api/v1/prescriptions
**功能**：创建新处方
```csharp
// Request
{
  "patientId": "guid",
  "doctorId": "guid",
  "consultationId": "guid",
  "diagnosis": "脾胃虚寒，寒湿内盛",
  "dosageCount": 7,
  "advice": "温服，每日三次",
  "items": [
    {
      "herbId": "guid",
      "herbName": "当归",
      "quantity": 10,
      "unit": "g",
      "unitPrice": 2.50,
      "subtotal": 25.00,
      "usage": "后下",
      "note": "备注"
    }
  ],
  "dosageForm": "汤剂",
  "usage": "温服",
  "totalAmount": 248.50,
  "formulaSource": "补中益气汤加减",
  "remark": "备注"
}

// Response 200
{
  "success": true,
  "message": "处方创建成功",
  "data": { /* PrescriptionDto */ }
}
```

### PUT /api/v1/prescriptions/{id}
**功能**：更新处方信息
```csharp
// Request
{
  "diagnosis": "更新后的诊断",
  "remarks": "更新后的备注"
}

// Response 200
{
  "success": true,
  "message": "处方更新成功",
  "data": { /* PrescriptionDto */ }
}
```

### DELETE /api/v1/prescriptions/{id}
**功能**：删除处方（软删除）
```csharp
// Response 200
{
  "success": true,
  "message": "删除成功"
}
```

## 🔧 核心服务

### IPrescriptionService
**职责**：处方业务逻辑处理
```csharp
public interface IPrescriptionService
{
    // 基础CRUD操作
    Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto);
    Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    
    // 业务查询
    Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
}
```

### IPrescriptionRepository
**职责**：处方数据访问与查询优化
```csharp
public interface IPrescriptionRepository : IRepository<Prescription>
{
    // 优化查询方法（避免N+1问题）
    Task<Prescription> GetByIdWithItemsAsync(Guid id);
    Task<PagedResult<Prescription>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string keyword);
    Task<List<Prescription>> GetByPatientIdAsync(Guid patientId);
    Task<List<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
}
```

## 📊 数据模型与实体

### 核心实体

#### Prescription（处方主表）
```csharp
[Table("Prescriptions")]
public class Prescription : BaseEntity
{
    // 关联信息
    [Required]
    public Guid MedicalCaseId { get; set; }      // 医疗案例ID（外键，必需）
    public Guid? PatientId { get; set; }         // 患者ID（冗余，可通过MedicalCase获取）
    public Guid? UserId { get; set; }            // 医生ID（冗余，可通过MedicalCase获取）
    
    // 处方基本信息
    [StringLength(500)]
    public string? Indication { get; set; }      // 主治（适应症）
    public int DosageCount { get; set; } = 7;    // 处方帖数
    
    [Column(TypeName = "decimal(5,4)")]
    public decimal Discount { get; set; } = 1.0m; // 折扣（0-1）
    
    [StringLength(500)]
    public string? Advice { get; set; }          // 医嘱
    
    [StringLength(200)]
    public string? FormulaSource { get; set; }   // 验方来源
    
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft; // 处方状态
    
    [StringLength(500)]
    public string? Remark { get; set; }          // 备注
    
    // 打印管理字段（新增特性）
    public int PrintVersion { get; set; } = 1;   // 当前打印版本号
    public DateTime? LastPrintedAt { get; set; } // 最后打印时间
    public int PrintCount { get; set; } = 0;     // 打印次数
    public bool IsPrinted { get; set; } = false; // 是否已打印
    
    // 导航属性
    public List<PrescriptionItem> Items { get; set; } = new(); // 处方项目
    public virtual MedicalCase.MedicalCase? MedicalCase { get; set; } // 关联病历
    public List<PrescriptionPrintLog> PrintLogs { get; set; } = new(); // 打印日志
}
```

#### PrescriptionItem（处方药材项）
```csharp
public class PrescriptionItem
{
    public Guid Id { get; set; }                 // 主键
    public Guid PrescriptionId { get; set; }     // 关联处方ID
    public Guid HerbId { get; set; }             // 药材ID
    public string HerbName { get; set; }         // 药材名称
    public int Quantity { get; set; }            // 实际用量（整数）
    public string Unit { get; set; } = "g";      // 单位
    public decimal UnitPrice { get; set; }       // 药材单价
    public decimal Amount => UnitPrice * Quantity; // 小计金额（计算属性）
    public string? Usage { get; set; }           // 用法说明
    public string? Remark { get; set; }          // 备注信息
}
```

### 枚举定义

#### PrescriptionStatus（处方状态）
```csharp
[Description("处方状态")]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrescriptionStatus
{
    /// <summary>草稿 - 处方正在编辑中</summary>
    [Description("草稿")]
    Draft = 0,
    
    /// <summary>已完成 - 处方已完成</summary>
    [Description("已完成")]
    Completed = 1
}
```

### DTO模型体系

#### 主要DTO
- **PrescriptionDto** - 处方基础信息展示（含计算属性）
- **PrescriptionDetailDto** - 处方详细信息
- **PrescriptionCreateDto** - 创建处方请求
- **PrescriptionEditDto** - 编辑处方请求
- **PrescriptionUpdateDto** - 更新处方请求（简化版）
- **PrescriptionItemDto** - 处方项目信息
- **PrescriptionItemCreateDto** - 创建处方项目

#### 计算属性（DTO层实现）
```csharp
// 在PrescriptionDto中实现价格计算
public decimal SingleDosePrice => Items?.Sum(item => item.UnitPrice * item.Quantity) * Discount ?? 0m;
public decimal TotalPrice => SingleDosePrice * DosageCount;
public decimal TotalWeight => Items?.Sum(item => item.Quantity) * DosageCount ?? 0m;
```

#### 查询与统计DTO
- **PrescriptionQueryDto** - 分页查询参数
- **PrescriptionSearchDto** - 高级搜索参数
- **PrescriptionStatisticsDto** - 处方统计信息
- **PrescriptionValidationResult** - 处方验证结果
- **PrescriptionCalculationDto** - 处方计算结果

## 🛡️ 数据验证规则

### 实体验证
```csharp
// Prescription验证
[Required] public Guid MedicalCaseId      // 医疗案例ID必填
[Required] public Guid PatientId          // 患者ID必填
[Required] public Guid UserId             // 医生ID必填
[StringLength(500)] public string? Indication  // 主治最大500字符
[Column(TypeName = "decimal(5,4)")] public decimal Discount  // 折扣精度限制

// PrescriptionItem验证
[Required] public Guid HerbId             // 药材ID必填
[Required] [StringLength(100)] public string HerbName  // 药材名称必填，最大100字符
[StringLength(16)] public string Unit     // 单位最大16字符
[Column(TypeName = "decimal(18,2)")] public decimal UnitPrice  // 单价精度限制
```

### DTO验证
```csharp
// PrescriptionCreateDto验证
[Required(ErrorMessage = "诊断不能为空")]
[StringLength(500, ErrorMessage = "诊断长度不能超过500个字符")]
public string Diagnosis { get; set; }

[Range(1, 30, ErrorMessage = "剂数必须在1-30之间")]
public int DosageCount { get; set; }

[Required(ErrorMessage = "必须包含至少一味中药材")]
public List<PrescriptionItemCreateDto> Items { get; set; }

// PrescriptionItemCreateDto验证
[Range(0.1, 1000)] public decimal Quantity
[Range(0, 10000)] public decimal UnitPrice
[StringLength(200)] public string? Usage
```

## 🔒 权限与安全

### 访问控制
- **授权要求**：所有API端点都需要用户认证（`[Authorize]`）
- **角色权限**：
  - **医生**：可以创建、查看、编辑自己开具的处方
  - **管理员**：可以查看、编辑所有处方
  - **药房人员**：可以查看处方，用于配药

### 数据安全
- **软删除**：使用`IsDeleted`标记，不物理删除处方数据
- **审计日志**：继承`BaseEntity`，自动记录创建/更新时间和操作人
- **版本控制**：使用`RowVersion`字段支持并发控制

## 📈 性能优化

### 查询优化
- **预加载策略**：使用`Include(p => p.Items)`避免N+1查询问题
- **分页查询**：支持页码和页大小参数，最大页大小限制为100
- **索引建议**：
  ```sql
  -- 患者处方查询索引
  CREATE INDEX IX_Prescriptions_PatientId_CreatedAt ON Prescriptions(PatientId, CreatedAt DESC);
  
  -- 医疗案例处方查询索引  
  CREATE INDEX IX_Prescriptions_MedicalCaseId ON Prescriptions(MedicalCaseId);
  
  -- 软删除过滤索引
  CREATE INDEX IX_Prescriptions_IsDeleted ON Prescriptions(IsDeleted);
  ```

### 缓存策略
- **内存缓存**：常用药材信息缓存
- **查询缓存**：处方统计数据缓存
- **计算缓存**：复杂价格计算结果缓存

## 📝 实现状态

### ✅ 已实现
- **核心实体**：Prescription、PrescriptionItem完整定义
- **仓储层**：PrescriptionRepository包含优化查询方法
- **服务层**：PrescriptionService基础CRUD功能
- **控制器**：PrescriptionsController完整API端点
- **DTO体系**：完整的数据传输对象定义
- **映射配置**：AutoMapper配置文件
- **验证规则**：基础数据验证注解

### 🔄 待优化
- **业务验证**：处方药材配伍禁忌检查
- **库存检查**：开方时检查药材库存
- **价格更新**：药材价格变动时的处方价格更新策略
- **审核流程**：处方审核工作流实现
- **统计报表**：处方统计分析功能
- **批量操作**：批量创建、更新处方功能

### ❌ 未实现
- **处方模板**：常用处方模板管理
- **电子签名**：医生电子签名功能
- **处方打印**：处方单据打印格式
- **配伍分析**：智能药材配伍分析
- **用药提醒**：患者用药提醒功能

## 🧪 测试覆盖

### 单元测试
- **服务层测试**：PrescriptionService业务逻辑测试
- **仓储层测试**：PrescriptionRepository数据访问测试
- **映射测试**：AutoMapper配置正确性测试
- **验证测试**：DTO验证规则测试

### 集成测试
- **API测试**：PrescriptionsController端到端测试
- **数据库测试**：Entity Framework查询性能测试
- **业务流程测试**：完整处方创建流程测试

## 🔗 依赖关系

### 依赖模块
- **Patients模块** - 患者信息获取
- **Users模块** - 医生用户信息
- **Herbs模块** - 药材信息和价格
- **MedicalCases模块** - 医疗案例关联
- **Infrastructure** - 数据库上下文和基础仓储
- **Shared.Models** - DTO和枚举定义

### 被依赖模块
- **诊疗工作台模块** - 处方开具界面
- **药房管理模块** - 处方配药流程
- **收费管理模块** - 处方费用结算
- **统计报表模块** - 处方数据分析

## 💡 业务特性

### 中医药特色功能
- **验方支持**：支持从验方库调用经典方剂
- **剂量管理**：支持中药传统计量单位（克、钱、两等）
- **用法用量**：支持复杂的中药用法说明（如：先煎、后下、冲服等）
- **配伍记录**：记录药材配伍来源和依据

### 业务规则
- **处方完整性**：每个处方必须包含至少一味中药材
- **剂数限制**：单次处方剂数限制在1-30帖之间
- **价格计算**：支持折扣计算，总价 = 单帖价格 × 剂数 × 折扣
- **状态管理**：处方状态简化为草稿和已完成两种状态

### 扩展接口预留
- **处方审核接口**：预留处方审核相关方法
- **库存集成接口**：预留与药材库存系统的集成点
- **打印接口**：预留处方打印相关功能
- **统计接口**：预留处方统计分析功能

## 📋 开发规范

### 命名约定
- **实体类**：`Prescription`、`PrescriptionItem`
- **DTO类**：`PrescriptionDto`、`PrescriptionCreateDto`
- **服务接口**：`IPrescriptionService`
- **仓储接口**：`IPrescriptionRepository`

### 代码风格
- **异步操作**：所有数据访问操作使用async/await
- **错误处理**：使用ServiceResult包装返回结果
- **日志记录**：关键操作记录详细日志
- **参数验证**：严格的输入参数验证

### 文档更新
- **API文档**：使用Swagger自动生成API文档
- **数据库文档**：维护实体关系图和表结构说明
- **业务文档**：更新业务流程和规则说明
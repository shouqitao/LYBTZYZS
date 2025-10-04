# Herbs模块设计 - Server端

## 📋 模块概述
**职责**：中药材管理、库存控制、分类管理、价格维护
**命名空间**：`LYBT.Module.Herbs`
**API路径**：`/api/v1/herbs/*`

## 🏗️ 架构设计

### 分层结构
```
├── Controllers/           # HTTP控制器（位于WebAPI项目）
│   └── HerbsController.cs
├── Services/             # 业务服务（简化架构）
│   └── HerbService.cs       # 主服务（实现IHerbService）
├── Repositories/         # 数据访问
│   └── HerbRepository.cs    # 数据访问实现
├── Interfaces/           # 服务接口
│   ├── IHerbService.cs      # 业务服务接口
│   └── IHerbRepository.cs   # 仓储接口
├── Mapping/             # 对象映射
│   └── HerbMappingProfile.cs
├── HerbsModule.cs       # 模块依赖注入注册
└── README.md
```

## 🔌 API接口设计

### GET /api/v1/herbs
**功能**：分页查询中药材
```csharp
// Query Parameters
?page=1&pageSize=20&keyword=人参&category=补益药&lowStock=true

// Response 200
{
  "items": [
    {
      "id": "guid",
      "name": "人参",
      "latinName": "Panax ginseng",
      "code": "RS001",
      "category": "补益药",
      "specification": "特级",
      "unit": "克",
      "unitPrice": 15.50,
      "stock": 2500,
      "safetyStock": 500,
      "isLowStock": false,
      "supplier": "长白山药材有限公司",
      "origin": "吉林长白山",
      "storageCondition": "阴凉干燥处保存",
      "shelfLife": 36,
      "status": "Active"
    }
  ],
  "totalCount": 500,
  "currentPage": 1,
  "pageSize": 20
}
```

### GET /api/v1/herbs/{id}
**功能**：获取中药材详情
```csharp
// Response 200
{
  "id": "guid",
  "name": "人参",
  "latinName": "Panax ginseng",
  "code": "RS001",
  "categoryId": "category-guid",
  "category": "补益药",
  "aliases": ["红参", "白参"],
  "specification": "特级",
  "unit": "克",
  "unitPrice": 15.50,
  "wholesalePrice": 12.00,
  "retailPrice": 18.00,
  "stock": 2500,
  "safetyStock": 500,
  "maxStock": 10000,
  "isLowStock": false,
  "supplier": "长白山药材有限公司",
  "supplierContact": "13900139000",
  "origin": "吉林长白山",
  "storageLocation": "A-01-001",
  "storageCondition": "阴凉干燥处保存，温度不超过20℃",
  "shelfLife": 36,
  "productionDate": "2023-10-15",
  "expiryDate": "2026-10-15",
  "batchNumber": "20231015001",
  "qualityStandard": "中国药典2020版",
  "efficacy": "大补元气，复脉固脱，补脾益肺，生津止渴，安神益智",
  "usage": "煎汤，3-9g；或入丸、散",
  "contraindications": "不宜与藜芦同用",
  "sideEffects": "大量使用可能引起兴奋、失眠",
  "description": "人参为五加科植物人参的根，具有大补元气的功效",
  "imageUrl": "/images/herbs/renshen.jpg",
  "status": "Active",
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

### POST /api/v1/herbs
**功能**：新增中药材
```csharp
// Request
{
  "name": "当归",
  "latinName": "Angelica sinensis",
  "code": "DG001",
  "categoryId": "category-guid",
  "specification": "一级",
  "unit": "克",
  "unitPrice": 8.50,
  "stock": 1000,
  "safetyStock": 200,
  "supplier": "甘肃当归种植基地",
  "origin": "甘肃岷县",
  "storageCondition": "阴凉干燥处保存",
  "shelfLife": 24,
  "efficacy": "补血活血，调经止痛，润燥滑肠",
  "usage": "煎汤，6-12g"
}

// Response 201
{
  "id": "new-guid",
  "name": "当归",
  // ... other fields
}
```

csharp
// Request
{
  "type": "IN",  // IN: 入库, OUT: 出库, ADJUST: 调整
  "quantity": 500,
  "reason": "采购入库",
  "operator": "张三",
  "batchNumber": "20240120001",
  "expiryDate": "2026-01-20"
}

// Response 200
{
  "currentStock": 3000,
  "operation": {
    "type": "IN",
    "quantity": 500,
    "beforeStock": 2500,
    "afterStock": 3000,
    "timestamp": "2024-01-20T14:30:00Z"
  }
}
```

csharp
// Response 200
[
  {
    "id": "guid",
    "name": "补益药",
    "code": "BYY",
    "description": "具有补益人体气血阴阳不足的药物",
    "parentId": null,
    "level": 1,
    "sortOrder": 1,
    "herbCount": 50
  },
  {
    "id": "guid2",
    "name": "补气药",
    "code": "BQY",
    "description": "主要用于治疗气虚证的药物",
    "parentId": "guid",
    "level": 2,
    "sortOrder": 1,
    "herbCount": 20
  }
]
```

## 🔧 核心服务

### HerbService (业务服务)
**职责**：中药材业务逻辑（简化版）
```csharp
public interface IHerbService
{
    // 基础CRUD - 已实现
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
    Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
    Task<ServiceResult> DeleteAsync(Guid id);
    
    // 库存管理功能 - 待实现
    // Task<ServiceResult<HerbDto>> UpdateStockAsync(Guid id, StockUpdateDto dto);
    // Task<ServiceResult<List<HerbDto>>> GetLowStockHerbsAsync();
}
```csharp
public interface IHerbService
{
    Task<ServiceResult<PagedResult<HerbDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
    Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto dto);
    Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto dto);
    Task<ServiceResult<List<HerbDto>>> SearchAsync(string keyword);
    Task<ServiceResult> DeleteAsync(Guid id);
    Task<ServiceResult<HerbDto>> UpdateStockAsync(Guid id, StockUpdateDto dto);
    Task<ServiceResult<List<HerbDto>>> GetLowStockHerbsAsync();
}
```

csharp
public interface IHerbQueryService
{
    Task<PagedResult<HerbDto>> GetPagedHerbsAsync(HerbSearchDto searchDto);
    Task<HerbDto?> GetHerbByIdAsync(Guid herbId);
    Task<List<HerbDto>> SearchHerbsByNameAsync(string name);
    Task<List<HerbDto>> GetHerbsByCategoryAsync(Guid categoryId);
    Task<List<HerbDto>> GetExpiringHerbsAsync(int days = 30);
}
```

csharp
public interface IHerbCategoryService
{
    Task<ServiceResult<List<HerbCategoryDto>>> GetAllCategoriesAsync();
    Task<ServiceResult<HerbCategoryDto>> CreateCategoryAsync(HerbCategoryCreateDto dto);
    Task<ServiceResult<HerbCategoryDto>> UpdateCategoryAsync(Guid id, HerbCategoryUpdateDto dto);
    Task<ServiceResult> DeleteCategoryAsync(Guid id);
    Task<ServiceResult<List<HerbCategoryDto>>> GetCategoryTreeAsync();
}
```

## 📊 数据模型

### 核心实体 (Herb)
```csharp
public class Herb : BaseEntity
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;        // 中文名
    
    [StringLength(200)]
    public string? LatinName { get; set; }                  // 拉丁名
    
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;       // 编码
    
    [StringLength(100)]
    public string? PinYinCode { get; set; }                 // 拼音码
    
    [StringLength(100)]
    public string Specification { get; set; } = string.Empty; // 规格
    
    [StringLength(20)]
    public string Unit { get; set; } = "g";                // 单位
    
    public decimal UnitPrice { get; set; }                  // 单价
    
    [StringLength(200)]
    public string? Origin { get; set; }                     // 产地
    
    [StringLength(500)]
    public string? Efficacy { get; set; }                   // 功效
    
    [StringLength(200)]
    public string? Usage { get; set; }                      // 用法用量
    
    public CommonStatus Status { get; set; } = CommonStatus.Enabled; // 状态
    
    // 简化库存字段（基础版本）
    public int Stock { get; set; } = 0;                     // 当前库存
    public int SafetyStock { get; set; } = 0;               // 安全库存
    
    // 计算属性
    [NotMapped]
    public bool IsLowStock => Stock <= SafetyStock;         // 是否低库存
}
```csharp
public class Herb : BaseEntity
{
    public string Name { get; set; }                     // 中文名
    public string? LatinName { get; set; }               // 拉丁名
    public string Code { get; set; }                     // 编码
    public Guid CategoryId { get; set; }                 // 分类ID
    public string[]? Aliases { get; set; }               // 别名
    public string Specification { get; set; }            // 规格
    public string Unit { get; set; }                     // 单位
    
    // 价格信息
    public decimal UnitPrice { get; set; }               // 单价
    public decimal? WholesalePrice { get; set; }         // 批发价
    public decimal? RetailPrice { get; set; }            // 零售价
    
    // 库存信息
    public int Stock { get; set; }                       // 当前库存
    public int SafetyStock { get; set; }                 // 安全库存
    public int? MaxStock { get; set; }                   // 最大库存
    public bool IsLowStock => Stock <= SafetyStock;      // 是否低库存
    
    // 供应商信息
    public string? Supplier { get; set; }                // 供应商
    public string? SupplierContact { get; set; }         // 供应商联系方式
    public string? Origin { get; set; }                  // 产地
    
    // 存储信息
    public string? StorageLocation { get; set; }         // 存储位置
    public string? StorageCondition { get; set; }        // 存储条件
    public int? ShelfLife { get; set; }                  // 保质期（月）
    public DateTime? ProductionDate { get; set; }        // 生产日期
    public DateTime? ExpiryDate { get; set; }            // 到期日期
    public string? BatchNumber { get; set; }             // 批次号
    
    // 药物信息
    public string? QualityStandard { get; set; }         // 质量标准
    public string? Efficacy { get; set; }                // 功效
    public string? Usage { get; set; }                   // 用法用量
    public string? Contraindications { get; set; }       // 禁忌
    public string? SideEffects { get; set; }             // 副作用
    public string? Description { get; set; }             // 描述
    public string? ImageUrl { get; set; }                // 图片链接
    
    public HerbStatus Status { get; set; }               // 状态
    
    // 导航属性
    public HerbCategory Category { get; set; }
    public List<StockTransaction> StockTransactions { get; set; }
}
```

csharp
public class StockTransaction : BaseEntity
{
    public Guid HerbId { get; set; }
    public StockTransactionType Type { get; set; }       // 入库/出库/调整
    public int Quantity { get; set; }                    // 数量
    public int BeforeStock { get; set; }                 // 操作前库存
    public int AfterStock { get; set; }                  // 操作后库存
    public string? Reason { get; set; }                  // 原因
    public string? Operator { get; set; }                // 操作人
    public string? BatchNumber { get; set; }             // 批次号
    public DateTime? ExpiryDate { get; set; }            // 到期日期
    public string? Remarks { get; set; }                 // 备注
    
    // 导航属性
    public Herb Herb { get; set; }
}
```

### 枚举定义
```csharp
// 药材状态继承自CommonStatus
public enum CommonStatus
{
    Enabled = 1,     // 启用
    Disabled = 2     // 禁用
}
```csharp
public enum HerbStatus
{
    Active = 1,          // 正常
    Discontinued = 2,    // 停用
    OutOfStock = 3,      // 缺货
    Expired = 4          // 过期
}

public enum StockTransactionType
{
    In = 1,              // 入库
    Out = 2,             // 出库
    Adjust = 3           // 调整
}
```

## 🛡️ 验证与安全

### 数据验证
```csharp
public class HerbCreateDtoValidator : AbstractValidator<HerbCreateDto>
{
    public HerbCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("药材名称不能为空")
            .MaximumLength(100).WithMessage("药材名称长度不能超过100个字符");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("药材编码不能为空")
            .MaximumLength(50).WithMessage("药材编码长度不能超过50个字符");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("单价必须大于0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("库存不能为负数");

        RuleFor(x => x.SafetyStock)
            .GreaterThanOrEqualTo(0).WithMessage("安全库存不能为负数");
    }
}
```

### 权限控制
- 药材查看：所有角色
- 药材新增/修改：药房管理员及以上
- 库存操作：药房管理员及以上
- 价格修改：系统管理员

## 📋 实现状态

### ✅ 已实现
- **HerbService基础CRUD** - 完整的药材增删改查操作
- **HerbRepository数据访问** - 基础数据访问层实现
- **AutoMapper映射配置** - 实体与DTO间的映射
- **药材搜索功能** - 支持名称和拼音码搜索
- **基础库存字段** - Stock和SafetyStock字段已定义

### 🔄 部分实现
- **HerbsController** - 基础API已实现，功能相对简单
- **药材分类** - 实体层可能支持，但未在当前服务中实现
- **库存管理** - 基础字段存在，但缺少更新和查询逻辑

### ❌ 待实现
- **库存流水记录** - 库存变更的历史追踪
- **批次管理** - 药材批次号和有效期管理
- **过期提醒** - 药材过期时间提醒功能
- **供应商管理** - 药材供应商信息管理
- **药材分类API** - 完整的分类管理接口
- **低库存预警** - 自动检测和提醒低库存药材
- **库存盘点** - 定期库存盘点功能

## 🧪 测试覆盖

### 单元测试
- HerbService业务逻辑
- 库存计算逻辑
- 数据验证规则
- 分类树结构

### 集成测试
- 药材API完整流程
- 库存操作事务
- 并发库存更新

## 🔗 依赖关系

### 依赖组件
- **Infrastructure** - 数据库上下文
- **Shared.Models** - DTO定义
- **Users模块** - 操作人信息

### 被依赖模块
- **Formula模块** - 方剂组方
- **Prescriptions模块** - 处方用药
- **Inventory模块** - 库存管理

## 📈 性能优化

### 查询优化
- 药材名称索引
- 编码唯一索引
- 分类ID索引
- 库存状态索引

### 缓存策略
- 分类树结构缓存
- 常用药材信息缓存
- 库存警告列表缓存

## 🔍 监控指标

### 业务指标
- 库存周转率
- 低库存药材数量
- 即将过期药材
- 药材种类数量

### 技术指标
- 查询响应时间
- 库存更新频率
- 缓存命中率
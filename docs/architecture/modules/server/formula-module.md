# Formula模块设计 - Server端

## 📋 模块概述
**职责**：中医方剂管理、药材配伍关系、剂量管理、方剂分享与复制
**命名空间**：`LYBT.Module.Formula`
**API路径**：`/api/v1/formulas/*`

## 🏗️ 架构设计

### 分层结构
```
├── Controllers/               # HTTP控制器（位于WebAPI项目）
│   └── FormulasController.cs
├── Services/                 # 业务服务
│   ├── FormulaService.cs        # 主服务（实现IFormulaService）
│   └── FormulaRepository.cs     # 数据仓储实现
├── Interfaces/               # 服务接口
│   └── IFormulaRepository.cs    # 仓储接口
├── Mapping/                  # AutoMapper映射配置
│   └── FormulaMappingProfile.cs
├── FormulaModule.cs          # 模块依赖注入注册
└── README.md
```

## 🔌 API接口设计

### GET /api/v1/formulas
**功能**：分页查询方剂列表
```csharp
// Request Query Parameters
{
  "page": 1,
  "pageSize": 20,
  "keyword": "感冒方"    // 可选，支持方剂名称、功效、用法、药材名称搜索
}

// Response 200
{
  "code": 200,
  "message": "查询成功",
  "data": {
    "items": [
      {
        "id": "guid",
        "name": "银翘散",
        "effect": "疏风解表，清热解毒",
        "usage": "水煎服，日二次",
        "property": "辛凉解表",
        "isShared": true,
        "remark": "治疗风热感冒",
        "herbs": [
          {
            "id": "guid",
            "herbId": "guid",
            "herbName": "金银花", 
            "quantity": 15,
            "unit": "g",
            "usage": "后下",
            "herb": {
              "name": "金银花",
              "price": 12.50
            }
          }
        ],
        "herbCount": 8,
        "totalPrice": 85.60,
        "herbNames": "金银花(15g)、连翘(15g)..."
      }
    ],
    "totalCount": 156,
    "currentPage": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

### GET /api/v1/formulas/{id}
**功能**：获取方剂详情
```csharp
// Response 200
{
  "code": 200,
  "message": "查询成功", 
  "data": {
    "id": "guid",
    "name": "银翘散",
    "effect": "疏风解表，清热解毒",
    "usage": "水煎服，日二次",
    "property": "辛凉解表",
    "isShared": true,
    "remark": "治疗风热感冒",
    "herbs": [
      {
        "id": "guid",
        "herbId": "guid", 
        "herbName": "金银花",
        "quantity": 15,
        "unit": "g",
        "usage": "后下",
        "processingMethod": "生用",
        "herb": {
          "id": "guid",
          "name": "金银花",
          "price": 12.50,
          "unit": "g"
        }
      }
    ]
  }
}

// Response 404
{
  "code": 404,
  "message": "方剂不存在",
  "errorCode": "FORMULANOTFOUND"
}
```

### POST /api/v1/formulas
**功能**：创建新方剂
```csharp
// Request
{
  "name": "自拟感冒方",
  "effect": "疏风解表",
  "usage": "水煎服，日三次",
  "isShared": false,
  "remark": "适用于风寒感冒初期",
  "herbs": [
    {
      "herbId": "guid",
      "quantity": 10,
      "preparation": "生用",
      "usage": "先煎",
      "sortOrder": 1
    }
  ]
}

// Response 200
{
  "code": 200,
  "message": "方剂创建成功",
  "data": {
    "id": "new-guid",
    "name": "自拟感冒方",
    // ... 完整方剂信息
  }
}
```

### PUT /api/v1/formulas/{id}
**功能**：更新方剂信息
```csharp
// Request
{
  "id": "guid",
  "name": "自拟感冒方（修订版）",
  "effect": "疏风解表，宣肺止咳",
  "usage": "水煎服，日三次，温服",
  "isShared": true,
  "herbs": [
    {
      "id": "existing-herb-guid",  // 更新现有药材
      "herbId": "guid",
      "quantity": 12,
      "sortOrder": 1
    },
    {
      "herbId": "new-herb-guid",   // 新增药材
      "quantity": 6,
      "sortOrder": 2
    }
  ]
}

// Response 200
{
  "code": 200,
  "message": "方剂更新成功",
  "data": { /* 更新后的方剂信息 */ }
}
```

### DELETE /api/v1/formulas/{id}
**功能**：删除方剂（软删除）
```csharp
// Response 200
{
  "code": 200,
  "message": "删除成功"
}
```

## 🔧 核心服务

### FormulaService
**职责**：方剂业务逻辑处理
```csharp
public interface IFormulaService
{
    // 基础CRUD
    Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null);
    Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto);
    Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto);
    Task<ServiceResult> DeleteAsync(Guid id);
    
    // 扩展功能
    Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword);
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId);
}
```

### FormulaRepository
**职责**：方剂数据访问层
```csharp
public interface IFormulaRepository : IRepository<Formula>
{
    // 优化查询方法（解决N+1查询问题）
    Task<List<Formula>> GetTemplatesAsync();
    Task<Formula> GetByIdWithHerbsAsync(Guid id);
    Task<PagedResult<Formula>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, string keyword = null);
    
    // 业务查询方法
    Task<List<Formula>> GetByUserIdAsync(Guid userId);
    Task<List<Formula>> GetSharedFormulasAsync();
    Task<List<Formula>> GetByCategoryAsync(string category);
}
```

## 📊 数据模型

### 主要实体

#### Formula（方剂实体）
```csharp
[Table("Formulas")]
public class Formula : BaseEntity
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;           // 方剂名称
    
    [StringLength(500)]
    public string? Effect { get; set; }                        // 功效
    
    [StringLength(500)] 
    public string? Usage { get; set; }                         // 用法
    
    [StringLength(500)]
    public string? Remark { get; set; }                        // 备注
    
    [StringLength(200)]
    public string? Property { get; set; }                      // 性味归经
    
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;  // 状态
    public bool IsShared { get; set; } = false;               // 是否共享
    
    [StringLength(50)]
    public string? Category { get; set; }                      // 方剂分类
    
    public FormulaType FormulaType { get; set; } = FormulaType.Experience;  // 方剂类型
    public Guid? UserId { get; set; }                          // 创建用户ID
    
    // 导航属性
    public List<FormulaHerbItem> Herbs { get; set; } = new();  // 药材组成
}

public enum FormulaType
{
    Classic = 1,     // 经典方
    Experience = 2   // 经验方
}
```

#### FormulaHerbItem（方剂药材组成）
```csharp
[Table("FormulaHerbItems")]
public class FormulaHerbItem
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid FormulaId { get; set; }                        // 所属方剂ID
    public Guid HerbId { get; set; }                           // 药材ID
    
    [Required, StringLength(100)]
    public string HerbName { get; set; } = string.Empty;       // 药材名称
    
    public int Quantity { get; set; } = 1;                     // 剂量（整数）
    
    [StringLength(16)]
    public string Unit { get; set; } = "g";                    // 单位
    
    [StringLength(200)]
    public string? Usage { get; set; }                         // 用法说明
    
    [StringLength(200)]
    public string? Remark { get; set; }                        // 备注信息
    
    [StringLength(100)]
    public string? ProcessingMethod { get; set; }              // 炮制方法
    
    // 兼容性属性
    [NotMapped]
    public int Dosage 
    { 
        get => Quantity; 
        set => Quantity = value; 
    }
}
```

### DTO模型

#### FormulaDto（方剂信息DTO）
```csharp
public class FormulaDto : StatusDto, IRemarkable
{
    public string Name { get; set; } = string.Empty;
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public string? Property { get; set; }
    public bool IsShared { get; set; } = false;
    public string? Remark { get; set; }
    public List<FormulaHerbItemDto> Herbs { get; set; } = new();
    
    // 计算属性
    public int HerbCount => Herbs?.Count ?? 0;
    public decimal TotalPrice => Herbs?.Sum(h => (h.Herb?.Price ?? 0m) * h.Quantity) ?? 0m;
    public string HerbNames => GetHerbNamesList();
    public string Category => GetCategoryByName();  // 智能分类
}
```

#### FormulaHerbItemDto（方剂药材组成DTO）
```csharp
public class FormulaHerbItemDto : BaseDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Preparation { get; set; }
    public string? Usage { get; set; }
    public decimal Price { get; set; }
    public string? ProcessingMethod { get; set; }
    public int SortOrder { get; set; }
    
    // 导航属性
    public HerbDto? Herb { get; set; }
}
```

## 🛡️ 数据验证规则

### 方剂验证规则
- **方剂名称**：必填，1-100字符，不能重复
- **功效描述**：可选，最多500字符
- **用法说明**：可选，最多500字符
- **药材组成**：至少包含1味药材，最多50味
- **备注信息**：可选，最多500字符

### 药材组成验证规则
- **药材选择**：必须从药材库中选择有效药材
- **剂量设置**：必须为正整数，范围1-1000
- **单位规范**：默认"g"，支持"钱"、"两"等中医单位
- **炮制方法**：可选，最多100字符
- **用法说明**：可选，最多200字符

### 业务规则
- **权限控制**：只能编辑自己创建的方剂或共享方剂
- **删除限制**：已被处方引用的方剂不能删除，只能禁用
- **分享规则**：共享后的方剂其他用户可查看和复制，但不能修改
- **复制机制**：复制方剂时自动添加"_副本"后缀，保持药材组成不变

## 🔒 权限与安全

### 访问权限
- **查看权限**：登录用户可查看自己创建的方剂和所有共享方剂
- **创建权限**：所有登录用户都可创建方剂
- **编辑权限**：只能编辑自己创建的方剂
- **删除权限**：只能删除自己创建且未被引用的方剂
- **分享权限**：只能设置自己创建方剂的分享状态

### 数据安全
- ✅ 软删除机制，防止数据误删
- ✅ 创建者权限控制
- ✅ 方剂引用检查，防止删除被使用的方剂
- ✅ 输入验证和SQL注入防护
- ✅ 敏感操作审计日志

## 📝 实现状态

### ✅ 已实现
- 方剂基础CRUD操作
- 药材组成管理
- 分页查询和关键词搜索
- AutoMapper映射配置
- N+1查询优化（Include策略）
- 方剂复制功能
- 软删除机制

### 🔄 待优化
- API控制器完整实现（仅基础CRUD）
- 方剂分类管理
- 从处方创建方剂功能
- 方剂使用统计
- 导入导出功能
- 方剂效果评估
- 药材配伍冲突检查

### ❌ 待实现
- 方剂模板功能
- 智能推荐相似方剂
- 方剂历史版本管理
- 批量操作功能
- 方剂打印和导出
- 中医药理论验证

## 🧪 测试覆盖

### 单元测试
- FormulaService业务逻辑测试
- FormulaRepository数据访问测试
- AutoMapper映射测试
- 数据验证规则测试

### 集成测试
- FormulasController API测试
- 数据库集成测试
- 权限控制测试
- 错误场景测试

## 🔗 依赖关系

### 依赖模块
- **Herbs模块** - 药材数据访问和价格信息
- **Users模块** - 用户信息和权限验证
- **Infrastructure** - 数据库上下文和基础仓储
- **Shared.Models** - DTO定义和通用契约

### 被依赖模块
- **Prescriptions模块** - 处方开药时的方剂模板
- **MedicalCase模块** - 病例记录中的方剂引用
- **Reports模块** - 方剂使用统计报表

## 📈 性能考虑

### 查询优化
- **Include策略**：预加载Herbs集合，避免N+1查询
- **分页查询**：大数据量下的分页性能优化
- **索引设计**：方剂名称、创建者、分类字段建立索引
- **缓存策略**：常用方剂和共享方剂缓存

### 数据库设计
- **外键约束**：确保药材引用的完整性
- **软删除标识**：IsDeleted字段提高查询性能
- **审计字段**：CreatedAt, UpdatedAt支持时间范围查询

### 扩展性考虑
- **模块化设计**：独立的业务模块，便于维护和扩展
- **接口抽象**：基于接口的依赖注入，便于测试和替换实现
- **配置化管理**：方剂分类、单位等可配置化
- **事件机制**：方剂变更事件，支持其他模块监听

## 🚀 未来规划

### 中医特色功能
- **配伍分析**：药材配伍的相使相畏分析
- **剂量智能建议**：基于患者体质的剂量调整建议
- **方证对应**：症状与方剂的智能匹配
- **效果追踪**：方剂疗效的长期跟踪统计

### 系统集成
- **第三方药材库**：接入权威中药材数据库
- **处方审核**：与医保系统的集成审核
- **智能推荐**：基于AI的方剂推荐系统
- **移动端支持**：移动设备的方剂查看和编辑
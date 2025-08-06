# Herbs模块实现总结

## 模块概述
Herbs模块负责中药材的基础信息管理，包括药材的增删改查、库存状态管理、价格管理、预警功能等。该模块管理药材的**状态**（如库存数量、价格等），而库存的**行为**（入库、出库等操作）由Pharmacy模块负责。

## 已完成功能

### 1. 药材CRUD操作 ✅
**核心文件**:
- `HerbsController.cs` - 控制器层
- `HerbService.cs` - 服务层
- `HerbRepository.cs` - 数据访问层

**实现特点**:
- 软删除策略（通过IsActive字段控制）
- 支持分页查询和多条件筛选
- 自动生成拼音码便于快速检索
- 支持批量导入导出

### 2. 药材查询功能 ✅
**端点列表**:
- `GET /api/v1/herbs` - RESTful标准查询（支持分页和筛选）
- `POST /api/v1/herbs/paged` - 分页查询（传统方式）
- `GET /api/v1/herbs/{id}` - 获取药材详情
- `GET /api/v1/herbs/active` - 获取启用的药材列表
- `GET /api/v1/herbs/available` - 获取可用药材列表

**查询参数支持**:
- 名称、拼音码搜索
- 产地、规格筛选
- 价格区间筛选
- 状态筛选（启用/禁用）
- 库存状态筛选

### 3. 库存管理功能 ✅
**端点列表**:
- `GET /api/v1/herbs/stock-warning` - 获取库存预警药材列表
- `GET /api/v1/herbs/stock-statistics` - 获取库存统计信息
- `PATCH /api/v1/herbs/{id}/stock` - 更新药材库存
- `PATCH /api/v1/herbs/batch-stock` - 批量更新库存（盘点）
- `PATCH /api/v1/herbs/{id}/warning-level` - 设置库存预警值
- `GET /api/v1/herbs/expiry-warning` - 获取即将过期药材

**库存字段**:
- `Stock` - 当前库存量
- `StockWarningLevel` - 库存预警值
- `MaxStock` - 最高库存限制
- `ExpiryDate` - 有效期
- `BatchNumber` - 批号
- `Supplier` - 供应商

**预警级别**:
- Critical（严重缺货）：库存 < 预警值的10%
- Low（缺货）：库存 < 预警值的50%
- Warning（预警）：库存 < 预警值的100%

### 4. 价格管理功能 ✅
**端点列表**:
- `PATCH /api/v1/herbs/{id}/price` - 更新药材价格
- `PATCH /api/v1/herbs/batch-price` - 批量更新价格
- `POST /api/v1/herbs/{id}/special-price` - 设置特价促销
- `DELETE /api/v1/herbs/{id}/special-price` - 取消特价促销
- `GET /api/v1/herbs/special-price` - 获取特价药材列表
- `GET /api/v1/herbs/{id}/price-history` - 获取价格历史
- `GET /api/v1/herbs/by-price-range` - 按价格区间查询

**价格字段**:
- `Price` - 零售价（基础价格）
- `CostPrice` - 成本价
- `MemberPrice` - 会员价
- `SpecialPrice` - 特价（促销价）
- `SpecialPriceStartTime` - 特价开始时间
- `SpecialPriceEndTime` - 特价结束时间

### 5. 批量操作功能 ✅
- 批量导入药材（支持Excel/CSV）
- 批量导出药材数据
- 批量更新库存（盘点功能）
- 批量更新价格

### 6. 状态管理功能 ✅
**端点列表**:
- `PATCH /api/v1/herbs/{id}/enable` - 启用药材
- `PATCH /api/v1/herbs/{id}/disable` - 禁用药材
- `PATCH /api/v1/herbs/{id}/toggle-status` - 切换状态
- `PATCH /api/v1/herbs/status` - 更新状态

## 技术实现亮点

### 1. 分层架构
```
Controller层（HerbsController）
    ↓
Service层（HerbService）
    ↓
Repository层（HerbRepository）
    ↓
Infrastructure层（AppDbContext）
```

### 2. 依赖注入配置
```csharp
// ServiceCollectionExtension.cs
services.AddScoped<IHerbService, HerbService>();
services.AddScoped<IHerbRepository, HerbRepository>();
```

### 3. 缓存策略
- 药材列表缓存10分钟
- 活跃药材缓存15分钟
- 特价药材缓存5分钟
- 更新操作自动清除相关缓存

### 4. 数据验证
- 使用DataAnnotations进行模型验证
- 价格必须大于等于0
- 库存量必须大于等于0
- 特价时间验证（结束时间必须大于开始时间）

## 数据模型

### HerbModel（数据库实体）
```csharp
public class HerbModel : BaseHerbModel {
    // 基础信息
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? PinYinCode { get; set; }
    public string? Origin { get; set; }
    public string? Spec { get; set; }
    public string Unit { get; set; }
    
    // 库存信息
    public decimal Stock { get; set; }
    public decimal StockWarningLevel { get; set; }
    public decimal MaxStock { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    
    // 价格信息
    public decimal Price { get; set; }
    public decimal CostPrice { get; set; }
    public decimal MemberPrice { get; set; }
    public decimal? SpecialPrice { get; set; }
    public DateTime? SpecialPriceStartTime { get; set; }
    public DateTime? SpecialPriceEndTime { get; set; }
    
    // 功效说明
    public string? Effect { get; set; }
    public string? Usage { get; set; }
    public string? Remark { get; set; }
    
    // 系统字段
    public bool IsActive { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public Guid? LastOperatorId { get; set; }
    public string? LastOperatorName { get; set; }
}
```

## API接口清单

### 基础CRUD接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/herbs` | GET | 获取药材列表 |
| `/api/v1/herbs/{id}` | GET | 获取药材详情 |
| `/api/v1/herbs` | POST | 创建药材 |
| `/api/v1/herbs/{id}` | PUT | 更新药材 |
| `/api/v1/herbs/add` | POST | 新增药材（兼容） |

### 库存管理接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/herbs/stock-warning` | GET | 库存预警列表 |
| `/api/v1/herbs/stock-statistics` | GET | 库存统计信息 |
| `/api/v1/herbs/{id}/stock` | PATCH | 更新库存 |
| `/api/v1/herbs/batch-stock` | PATCH | 批量更新库存 |
| `/api/v1/herbs/{id}/warning-level` | PATCH | 设置预警值 |
| `/api/v1/herbs/expiry-warning` | GET | 过期预警 |

### 价格管理接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/herbs/{id}/price` | PATCH | 更新价格 |
| `/api/v1/herbs/batch-price` | PATCH | 批量更新价格 |
| `/api/v1/herbs/{id}/special-price` | POST | 设置特价 |
| `/api/v1/herbs/{id}/special-price` | DELETE | 取消特价 |
| `/api/v1/herbs/special-price` | GET | 特价药材列表 |
| `/api/v1/herbs/by-price-range` | GET | 价格区间查询 |

### 状态管理接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/herbs/{id}/enable` | PATCH | 启用药材 |
| `/api/v1/herbs/{id}/disable` | PATCH | 禁用药材 |
| `/api/v1/herbs/{id}/toggle-status` | PATCH | 切换状态 |

### 批量操作接口
| 端点 | 方法 | 说明 |
|-----|-----|------|
| `/api/v1/herbs/import` | POST | 批量导入 |
| `/api/v1/herbs/export` | GET | 导出数据 |

## 请求/响应示例

### 创建药材请求
```json
POST /api/v1/herbs
{
    "name": "麻黄",
    "pinYinCode": "MH",
    "origin": "内蒙古",
    "spec": "统货",
    "unit": "克",
    "price": 12.5,
    "costPrice": 8.0,
    "memberPrice": 11.0,
    "effect": "发汗解表，宣肺平喘",
    "usage": "3-10克，水煎服",
    "stockWarningLevel": 500,
    "maxStock": 5000
}
```

### 库存预警响应
```json
GET /api/v1/herbs/stock-warning
[
    {
        "id": "uuid",
        "name": "人参",
        "pinYinCode": "RS",
        "stock": 50,
        "stockWarningLevel": 200,
        "unit": "克",
        "supplier": "东北药材公司",
        "shortageQuantity": 150,
        "warningLevel": "Low"
    }
]
```

### 库存统计响应
```json
GET /api/v1/herbs/stock-statistics
{
    "totalCount": 256,
    "outOfStockCount": 12,
    "warningCount": 35,
    "sufficientCount": 209,
    "totalStockValue": 125680.50,
    "expiringCount": 8,
    "expiredCount": 2
}
```

## 与其他模块的协作

### 1. 与Pharmacy模块的协作
- Herbs提供库存状态查询接口
- Pharmacy调用UpdateStockAsync更新库存
- Herbs负责库存预警和统计

### 2. 与Prescriptions模块的协作
- Prescriptions查询药材信息和价格
- Prescriptions检查药材库存是否充足
- Herbs提供药材搜索接口

### 3. 与Formula模块的协作
- Formula查询药材基础信息
- Formula使用药材组成验方
- Herbs提供可用药材列表

## 安全特性

1. **数据验证**
   - 所有输入参数验证
   - 价格和库存量范围检查
   - 时间有效性验证

2. **权限控制**
   - 基于JWT的身份认证
   - 角色权限控制
   - 操作日志记录

3. **数据保护**
   - 软删除机制
   - 操作者信息记录
   - 缓存策略防止频繁查询

## 性能优化

1. **缓存策略**
   - 热点数据缓存（药材列表、特价药材）
   - 自动缓存失效机制
   - 分级缓存时间设置

2. **查询优化**
   - 支持分页查询
   - 索引优化（名称、拼音码）
   - 延迟加载相关数据

3. **批量操作**
   - 批量更新减少数据库交互
   - 事务处理保证数据一致性
   - 异步处理提高响应速度

## 测试覆盖

- [x] 药材CRUD操作测试
- [x] 库存管理功能测试
- [x] 价格管理功能测试
- [x] 批量操作测试
- [x] 预警功能测试
- [ ] 并发更新测试
- [ ] 性能压力测试

## 待优化项

1. **功能增强**
   - 价格历史记录表
   - 库存变动日志
   - 药材图片管理
   - 药材分类管理

2. **性能优化**
   - 更细粒度的缓存策略
   - 异步批量操作
   - 数据库查询优化

3. **业务扩展**
   - 多供应商管理
   - 采购订单集成
   - 库存盘点历史
   - 成本利润分析

## 总结

Herbs模块已完成所有核心功能：
- ✅ 完整的CRUD操作
- ✅ 强大的库存管理
- ✅ 灵活的价格管理
- ✅ 实用的预警机制
- ✅ 高效的批量操作
- ✅ 完善的缓存策略

该模块为中药材管理提供了全面的功能支持，与Pharmacy模块配合实现了状态与行为的分离，为整个系统的药材管理奠定了坚实基础。
# LYBT.Module.Herbs 功能说明文档

## 模块概述

药材管理模块负责中药材的完整库存管理，包括药材基础信息维护、库存管理、有效期监控、状态控制、批量操作等功能。本模块支持智能状态管理、过期预警、库存预警等特性，为处方开具和药房管理提供基础数据支持。

## 数据模型

### HerbModel (药材实体)

**文件位置**: `Models/HerbModel.cs`

| 字段名              | 类型         | 说明         | 验证规则             |
| ---------------- | ---------- | ---------- | ---------------- |
| Id               | Guid       | 药材唯一标识（主键） | 必填               |
| Name             | string     | 药材名称       | 最长64字符，必填        |
| PinyinCode       | string?    | 拼音码        | 最长32字符，用于快速检索    |
| Origin           | string?    | 产地         | 最长64字符，可选        |
| Spec             | string?    | 规格         | 最长32字符，可选        |
| Unit             | string?    | 单位         | 最长16字符，可选        |
| Price            | decimal    | 单价         | decimal(18,2)，必填 |
| Stock            | int        | 库存数量       | 整数，必填            |
| BatchNo          | string?    | 批号         | 最长32字符，可选        |
| ExpireDate       | DateTime?  | 有效期        | 可选，用于过期预警        |
| Effect           | string?    | 功效说明       | 最长128字符，可选       |
| Remark           | string?    | 备注信息       | 最长256字符，可选       |
| Status           | HerbStatus | 药材状态       | 枚举值，默认Active     |
| CreatedAt        | DateTime   | 创建时间       | 系统自动设置，UTC时间     |
| UpdatedAt        | DateTime?  | 更新时间       | 系统自动维护           |
| LastOperatorId   | Guid?      | 最后操作人ID    | 可选，用于操作追踪        |
| LastOperatorName | string?    | 最后操作人姓名    | 最长50字符，用于操作追踪    |

### 枚举类型

#### HerbStatus (药材状态)
- `Inactive (0)`: 停用 - 不能开具处方，但保留历史记录
- `Active (1)`: 正常 - 可以开具处方
- `OutOfStock (2)`: 缺货 - 临时缺货，可以开具但需要提醒
- `Discontinued (3)`: 停产 - 永久停产，建议替换
- `Expired (4)`: 过期 - 需要更新或移除
- `UnderReview (5)`: 审核中 - 新药材等待审核

## DTO 数据传输对象

### HerbDto (药材列表展示)

**使用场景**: 药材列表展示、简单药材信息返回
**特点**: 包含药材基本信息和计算字段

```csharp
- Id: 药材ID
- Name: 药材名称
- Pinyin: 拼音码
- Origin: 产地
- Spec: 规格
- Unit: 单位
- Price: 单价
- TotalPrice: 总价（计算字段）
- Stock: 库存数量
- BatchNo: 批号
- ExpireDate: 有效期
- Effect: 功效说明
- Status: 药材状态
- StatusDescription: 状态描述（中文）
```

### HerbDetailDto (药材详情)

**使用场景**: 药材详情展示、完整信息查看
**特点**: 包含药材完整信息和时间戳

```csharp
- 包含HerbDto的所有字段
- Remark: 备注信息
- CreatedAt: 创建时间
- UpdatedAt: 更新时间
```

### HerbCreateDto (药材创建)

**使用场景**: 新建药材信息
**特点**: 包含数据验证规则

```csharp
- Name: 药材名称（必填）
- Pinyin: 拼音码（可选，可自动生成）
- Origin: 产地（可选）
- Spec: 规格（可选）
- Unit: 单位（可选）
- Price: 单价（必填，不能为负数）
- Stock: 库存数量
- BatchNo: 批号（可选）
- ExpireDate: 有效期（可选）
- Effect: 功效说明（可选）
- Remark: 备注（可选）
- Status: 药材状态（默认Active）
```

### HerbEditDto (药材编辑)

**使用场景**: 编辑药材信息
**特点**: 继承自HerbCreateDto，包含ID字段

```csharp
- Id: 药材ID（必填，标识更新目标）
- 其他字段同HerbCreateDto
```

### HerbPagedQueryDto (药材分页查询)

**使用场景**: 药材列表的分页查询和条件筛选
**特点**: 支持多种筛选条件和智能查询

```csharp
- Keyword: 关键词（模糊匹配药材名称或拼音码）
- Page: 页码（默认1）
- PageSize: 每页大小（默认20）
- Status: 药材状态筛选（可选）
- IncludeInactive: 是否包含停用药材（默认false）
- OnlyLowStock: 是否只查询库存不足药材（默认false）
- LowStockThreshold: 库存不足阈值（默认10）
- OnlyExpiring: 是否只查询即将过期药材（默认false）
- ExpiringDays: 过期预警天数（默认30天）
```

### HerbImportDto (药材导入)

**使用场景**: 批量导入药材数据
**特点**: 简化的数据结构，适用于Excel导入

### HerbStatusUpdateDto (状态更新)

**使用场景**: 单个药材状态更新
**特点**: 包含状态变更原因记录

```csharp
- Id: 药材ID（必填）
- Status: 新状态（必填）
- Reason: 状态变更原因（可选，最长200字符）
```

### HerbBatchStatusUpdateDto (批量状态更新)

**使用场景**: 批量药材状态更新
**特点**: 支持批量操作，有数量限制

```csharp
- Ids: 药材ID列表（必填，最少1个，最多100个）
- Status: 新状态（必填）
- Reason: 状态变更原因（可选，最长200字符）
```

## 服务层 (IHerbService & HerbService)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<HerbDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取药材详情
**使用场景**: 药材详情页面、编辑前数据加载

#### GetListAsync

```csharp
Task<List<HerbDto>> GetListAsync()
```

**功能**: 获取所有药材列表
**特点**: 不分页，返回所有药材
**使用场景**: 下拉选择框、简单列表展示

#### GetPagedAsync

```csharp
Task<PagedResultDto<HerbDto>> GetPagedAsync(HerbPagedQueryDto query)
```

**功能**: 分页条件查询药材列表
**查询特性**: 
- 关键词模糊匹配（药材名称、拼音码）
- 药材状态筛选
- 库存不足筛选
- 即将过期筛选
- 停用药材包含控制

**使用场景**: 药材管理页面的列表展示

#### AddAsync

```csharp
Task<bool> AddAsync(HerbCreateDto dto)
```

**功能**: 创建新药材
**业务逻辑**: 
- 药材名称必填验证
- 价格非负数验证
- 自动生成拼音码（如未提供）
- 设置创建时间和操作人信息

**使用场景**: 新药材录入

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(HerbEditDto dto)
```

**功能**: 更新药材信息
**业务逻辑**: 
- 药材ID和名称验证
- 更新时间和操作人信息维护
- 重新生成拼音码（如名称变更）

**使用场景**: 药材信息维护

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除药材
**注意**: 根据业务需求，可能是物理删除或改为停用状态
**使用场景**: 药材移除

### 状态管理方法

#### UpdateStatusAsync

```csharp
Task<bool> UpdateStatusAsync(HerbStatusUpdateDto dto)
```

**功能**: 更新单个药材状态
**业务逻辑**: 
- 状态变更验证
- 记录变更原因
- 更新操作人信息

**使用场景**: 药材状态管理

#### BatchUpdateStatusAsync

```csharp
Task<int> BatchUpdateStatusAsync(HerbBatchStatusUpdateDto dto)
```

**功能**: 批量更新药材状态
**业务逻辑**: 
- 批量大小限制（最多100个）
- 返回实际更新数量
- 统一状态变更

**使用场景**: 批量药材状态管理

#### GetByStatusAsync

```csharp
Task<List<HerbDto>> GetByStatusAsync(HerbStatus status)
```

**功能**: 根据状态获取药材列表
**使用场景**: 特定状态药材查看

#### GetAvailableHerbsAsync

```csharp
Task<List<HerbDto>> GetAvailableHerbsAsync()
```

**功能**: 获取可用药材列表（状态为Active）
**使用场景**: 处方开具时的药材选择

### 智能管理方法

#### GetOutOfStockHerbsAsync

```csharp
Task<List<HerbDto>> GetOutOfStockHerbsAsync()
```

**功能**: 获取缺货药材列表
**业务逻辑**: 库存为0或状态为OutOfStock的药材
**使用场景**: 库存预警、采购计划

#### GetExpiringHerbsAsync

```csharp
Task<List<HerbDto>> GetExpiringHerbsAsync(int days = 30)
```

**功能**: 获取即将过期药材列表
**参数**: days - 过期预警天数（默认30天）
**业务逻辑**: 有效期在指定天数内到期的药材
**使用场景**: 过期预警、库存管理

#### CheckAndUpdateExpiredHerbsAsync

```csharp
Task<int> CheckAndUpdateExpiredHerbsAsync()
```

**功能**: 检查并自动更新过期药材状态
**业务逻辑**: 
- 检查所有药材有效期
- 自动将过期药材状态更新为Expired
- 返回更新数量

**使用场景**: 定时任务、系统维护

#### GetStatusStatisticsAsync

```csharp
Task<Dictionary<HerbStatus, int>> GetStatusStatisticsAsync()
```

**功能**: 获取药材状态统计信息
**返回**: 状态和对应数量的字典
**使用场景**: 数据统计、仪表板展示

### 批量操作方法

#### ImportAsync

```csharp
Task<int> ImportAsync(List<HerbImportDto> dtos)
```

**功能**: 批量导入药材数据
**业务逻辑**: 
- 逐条验证和导入
- 失败记录跳过，继续处理
- 返回成功导入的数量

**使用场景**: 药材数据批量导入

#### ExportAsync

```csharp
Task<List<HerbDetailDto>> ExportAsync()
```

**功能**: 导出药材数据
**使用场景**: 药材数据导出、备份

## 仓储层 (IHerbRepository & HerbRepository)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<HerbModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取药材实体
**使用场景**: 服务层调用的底层数据操作

#### GetListAsync

```csharp
Task<List<HerbModel>> GetListAsync()
```

**功能**: 获取所有药材实体列表
**使用场景**: 批量操作、全量查询

#### AddAsync / UpdateAsync

```csharp
Task<bool> AddAsync(HerbModel herb)
Task<bool> UpdateAsync(HerbModel herb)
```

**功能**: 基础的增加和更新操作
**使用场景**: 服务层调用的底层数据操作

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除药材
**实现**: 根据业务需求，可能是物理删除或状态更新

#### AddRangeAsync

```csharp
Task<bool> AddRangeAsync(List<HerbModel> herbs)
```

**功能**: 批量新增药材
**特点**: 使用EF Core的批量操作，性能优化
**使用场景**: 批量导入操作

### 查询方法

#### GetPagedAsync

```csharp
Task<(List<HerbModel> list, int total)> GetPagedAsync(string? keyword, int page, int pageSize)
```

**功能**: 分页查询药材
**查询条件**: 
- 关键词模糊匹配（药材名称、拼音码）
- 分页参数

**排序**: 按创建时间倒序或名称排序
**使用场景**: 分页列表展示

## 权限控制策略

### 数据访问权限

- **管理员**: 可查看和操作所有药材（包括停用药材）
- **普通用户**: 只能查看可用药材，不能进行管理操作
- **医生**: 可查看可用药材用于处方开具

### 操作权限

- **药材创建**: 仅管理员和药房人员
- **药材编辑**: 仅管理员和药房人员
- **状态管理**: 仅管理员和药房人员
- **批量操作**: 需要特殊权限，有数量限制

### 数据安全

- 所有操作记录操作人信息
- 重要状态变更记录变更原因
- 敏感操作需要审计日志

## 业务规则

### 数据完整性

- **药材名称**: 必填，系统内唯一性建议
- **价格验证**: 不能为负数
- **库存管理**: 支持负库存（预借）
- **有效期**: 可选，但建议填写以支持过期预警

### 状态流转

- **新建**: 默认为Active状态
- **停用**: 可从任何状态变为Inactive
- **过期**: 系统自动或手动设置
- **缺货**: 可手动设置或系统检测

### 智能功能

- **拼音码**: 自动生成，支持快速检索
- **过期预警**: 可配置预警天数
- **库存预警**: 可配置库存阈值
- **状态统计**: 实时统计各状态药材数量

## 集成依赖

### 模块依赖

- **LYBT.Module.Prescriptions**: 处方模块（药材使用）
- **LYBT.Module.Pharmacy**: 药房模块（库存管理）
- **LYBT.Infrastructure**: 基础设施（日志、缓存、配置）

### 技术依赖

- **AutoMapper**: 对象映射
- **Entity Framework Core**: 数据访问
- **CommonHelper**: 拼音码生成工具

## 使用示例

### 创建药材

```csharp
var herbDto = new HerbCreateDto {
    Name = "当归",
    Pinyin = "DG",
    Origin = "甘肃岷县",
    Spec = "统货",
    Unit = "公斤",
    Price = 45.5m,
    Stock = 100,
    BatchNo = "20240301",
    ExpireDate = DateTime.Now.AddYears(2),
    Effect = "补血活血，调经止痛",
    Status = HerbStatus.Active
};

var result = await herbService.AddAsync(herbDto);
```

### 分页查询药材

```csharp
var query = new HerbPagedQueryDto {
    Keyword = "当归",
    Status = HerbStatus.Active,
    Page = 1,
    PageSize = 20,
    OnlyLowStock = false,
    LowStockThreshold = 10
};

var result = await herbService.GetPagedAsync(query);
```

### 批量状态更新

```csharp
var updateDto = new HerbBatchStatusUpdateDto {
    Ids = new List<Guid> { herbId1, herbId2, herbId3 },
    Status = HerbStatus.OutOfStock,
    Reason = "供应商临时缺货"
};

var count = await herbService.BatchUpdateStatusAsync(updateDto);
```

### 获取预警信息

```csharp
// 获取缺货药材
var outOfStockHerbs = await herbService.GetOutOfStockHerbsAsync();

// 获取即将过期药材
var expiringHerbs = await herbService.GetExpiringHerbsAsync(15); // 15天内过期

// 获取状态统计
var statistics = await herbService.GetStatusStatisticsAsync();
```

### 定时任务示例

```csharp
// 每日检查过期药材
var expiredCount = await herbService.CheckAndUpdateExpiredHerbsAsync();
if (expiredCount > 0) {
    // 发送过期药材通知
    await notificationService.SendExpiredHerbAlert(expiredCount);
}
```

## 扩展建议

### 功能扩展

- **供应商管理**: 添加药材供应商信息
- **价格历史**: 记录药材价格变动历史
- **库存流水**: 详细的出入库记录
- **质量检测**: 药材质量检测记录
- **图片管理**: 支持药材图片上传

### 技术优化

- **缓存策略**: 对常用药材信息进行缓存
- **搜索优化**: 集成全文搜索引擎
- **库存同步**: 与ERP系统库存同步
- **预警通知**: 集成消息推送服务
- **报表分析**: 药材使用量和成本分析
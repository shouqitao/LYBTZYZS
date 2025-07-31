# LYBT.Module.Billing 功能说明文档

## 模块概述

费用结算模块负责医疗费用的完整管理流程，包括费用计算、账单生成、支付处理、退款管理等功能。本模块与患者、医生、处方模块深度集成，支持多种支付方式、状态流转和财务管理，为医院收费业务提供全面支撑。

## 数据模型

### BillingModel (账单主表实体)

**文件位置**: `Models/BillingModel.cs`

| 字段名            | 类型               | 说明          | 验证规则             |
| -------------- | ---------------- | ----------- | ---------------- |
| Id             | Guid             | 账单唯一标识（主键） | 必填               |
| BillingId      | string           | 账单业务编码      | 最长64字符，流水号等     |
| PatientId      | Guid             | 患者ID        | 必填，外键关联患者表       |
| PrescriptionId | Guid?            | 处方ID        | 可选，关联处方表         |
| Items          | List&lt;BillingItem&gt; | 账单明细列表      | 必填，至少一个明细项       |
| TotalAmount    | decimal          | 账单总金额       | decimal(18,2)，必填 |
| PaidAmount     | decimal          | 已缴金额        | decimal(18,2)      |
| Status         | BillingStatus    | 账单状态        | 枚举值，默认Pending    |
| PaymentMethod  | string           | 缴费方式        | 最长32字符，现金、微信等   |
| DoctorId       | Guid             | 开单医生ID      | 必填，外键关联医生表       |
| CreatedTime    | DateTime         | 创建时间        | 必填，系统自动设置        |
| PaidTime       | DateTime?        | 支付时间        | 可选，支付完成时设置       |
| CompletedTime  | DateTime?        | 完成时间        | 可选，整个流程完成时设置     |
| RefundTime     | DateTime?        | 退款时间        | 可选，退款完成时设置       |
| RefundReason   | string?          | 退款理由        | 最长128字符，退款时填写    |
| IsDeleted      | bool             | 是否删除        | 软删除标记，默认false    |
| BillingTime    | DateTime         | 账单时间        | 必填，可与创建时间区分      |
| Remark         | string?          | 备注信息        | 最长256字符，可选       |

### BillingItem (账单明细实体)

**文件位置**: `Models/BillingModel.cs`

| 字段名       | 类型      | 说明         | 验证规则          |
| --------- | ------- | ---------- | ------------- |
| ItemId    | Guid    | 明细唯一标识     | 系统自动生成        |
| Name      | string  | 项目名称       | 最长64字符，必填     |
| UnitPrice | decimal | 单价         | decimal(18,2) |
| Quantity  | decimal | 数量         | decimal(18,2) |
| SubTotal  | decimal | 小计（计算属性）   | 单价 × 数量，只读    |

### 枚举类型

#### BillingStatus (账单状态)
- `Pending (0)`: 待付款 - 账单已生成，等待支付
- `Paid (1)`: 已付款 - 已全额支付
- `PartiallyPaid (2)`: 部分付款 - 已部分支付，有余额
- `Refunded (-1)`: 已退款 - 已完成退款
- `Cancelled (-2)`: 已取消 - 账单被取消

## DTO 数据传输对象

### BillingDto (账单列表展示)

**使用场景**: 账单列表展示、简单账单信息返回
**特点**: 包含账单基本信息和患者名称

```csharp
- Id: 账单ID
- PatientName: 患者姓名
- TotalAmount: 账单总金额
- PaidAmount: 已缴金额
- Status: 账单状态
- CreatedTime: 创建时间
- BillingTime: 账单时间
```

### BillingDetailDto (账单详情)

**使用场景**: 账单详情展示、完整账单信息查看
**特点**: 包含账单完整信息和明细列表

```csharp
- Id: 账单ID
- PatientId: 患者ID
- PatientName: 患者姓名
- PrescriptionId: 处方ID（可选）
- DoctorId: 开单医生ID
- Items: 账单明细列表（BillingItemDto集合）
- TotalAmount: 账单总金额
- PaidAmount: 已缴金额
- Status: 账单状态
- CreatedTime: 创建时间
- PaidTime: 支付时间
- CompletedTime: 完成时间
- RefundTime: 退款时间
- RefundReason: 退款理由
- PaymentMethod: 缴费方式
- BillingTime: 账单时间
- Remark: 备注信息
```

### BillingCreateDto (账单创建)

**使用场景**: 创建新账单
**特点**: 包含数据验证规则

```csharp
- PatientId: 患者ID（必填）
- PrescriptionId: 处方ID（可选）
- DoctorId: 开单医生ID（必填）
- Items: 账单明细列表（必填）
- TotalAmount: 账单总金额
- PaidAmount: 已缴金额（默认0）
- Status: 账单状态（默认Pending）
- CreatedTime: 创建时间（默认当前时间）
- PaymentMethod: 缴费方式（可选）
- BillingTime: 账单时间（默认当前时间）
- Remark: 备注信息（可选）
```

### BillingItemDto (账单明细)

**使用场景**: 账单明细展示和创建

```csharp
- Name: 项目名称
- UnitPrice: 单价
- Quantity: 数量
- SubTotal: 小计（计算属性）
```

### BillingEditDto (账单编辑)

**使用场景**: 编辑账单信息
**特点**: 继承自BillingCreateDto，包含ID字段

```csharp
- Id: 账单ID（必填，标识更新目标）
- 其他字段同BillingCreateDto
```

### RequestRefundDto (退款申请)

**使用场景**: 申请退款
**特点**: 包含退款理由

```csharp
- Id: 账单ID（必填）
- Reason: 退款理由（必填）
```

## 服务层 (IBillingService & BillingService)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<BillingDetailDto?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取账单详情
**使用场景**: 账单详情页面、支付确认

#### GetListAsync

```csharp
Task<List<BillingDto>> GetListAsync()
```

**功能**: 获取所有账单列表
**特点**: 返回简化的账单信息
**使用场景**: 账单管理页面的列表展示

#### AddAsync

```csharp
Task<bool> AddAsync(BillingCreateDto billingCreateDto)
```

**功能**: 创建新账单
**业务逻辑**: 
- 患者ID和医生ID必填验证
- 账单明细验证（至少一个项目）
- 总金额自动计算验证
- 处方关联验证（如有）
- 生成账单编码

**使用场景**: 医生开单、收费处创建账单

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(BillingEditDto billingEditDto)
```

**功能**: 更新账单信息
**业务逻辑**: 
- 账单ID验证
- 状态检查（只有特定状态可编辑）
- 明细更新处理
- 总金额重新计算

**使用场景**: 账单信息修改、明细调整

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除账单
**注意**: 软删除，设置IsDeleted标记
**业务逻辑**: 
- 状态检查（只有未支付账单可删除）
- 软删除标记设置

**使用场景**: 错误账单删除

### 支付管理方法

#### MarkAsPaidAsync

```csharp
Task<bool> MarkAsPaidAsync(Guid id)
```

**功能**: 标记账单为已支付
**业务逻辑**: 
- 状态检查（只有待付款或部分付款可标记为已付款）
- 更新状态为Paid
- 设置支付时间
- 更新已缴金额为总金额

**使用场景**: 收费员确认收款

#### MarkAsCompletedAsync

```csharp
Task<bool> MarkAsCompletedAsync(Guid id)
```

**功能**: 标记账单为已完成
**业务逻辑**: 
- 状态检查（只有已支付账单可完成）
- 更新相关业务状态
- 设置完成时间

**使用场景**: 整个医疗流程完成

### 退款管理方法

#### RequestRefundAsync

```csharp
Task<bool> RequestRefundAsync(Guid id, string reason)
```

**功能**: 申请退款
**业务逻辑**: 
- 状态检查（只有已支付账单可申请退款）
- 记录退款理由
- 状态更新为退款申请中（或保持原状态等待审批）

**使用场景**: 患者或医生申请退款

#### ApproveRefundAsync

```csharp
Task<bool> ApproveRefundAsync(Guid id)
```

**功能**: 批准退款
**业务逻辑**: 
- 权限检查（只有管理员可批准）
- 状态更新为Refunded
- 设置退款时间
- 触发退款流程

**使用场景**: 管理员审批退款

#### RejectRefundAsync

```csharp
Task<bool> RejectRefundAsync(Guid id)
```

**功能**: 拒绝退款
**业务逻辑**: 
- 权限检查
- 恢复原状态
- 记录拒绝原因

**使用场景**: 管理员拒绝退款申请

#### CancelAsync

```csharp
Task<bool> CancelAsync(Guid id)
```

**功能**: 取消未支付账单
**业务逻辑**: 
- 状态检查（只有未支付账单可取消）
- 状态更新为Cancelled

**使用场景**: 取消错误或不需要的账单

### 查询方法

#### GetByPatientIdAsync

```csharp
Task<List<BillingDto>> GetByPatientIdAsync(Guid patientId)
```

**功能**: 获取患者的所有账单
**使用场景**: 患者费用查询、历史账单

#### SearchAsync

```csharp
Task<List<BillingDto>> SearchAsync(string keyword)
```

**功能**: 关键词搜索账单
**搜索范围**: 患者姓名、账单编码等
**使用场景**: 账单快速查找

#### GetByStatusAsync

```csharp
Task<List<BillingDto>> GetByStatusAsync(BillingStatus status)
```

**功能**: 根据状态获取账单列表
**使用场景**: 特定状态账单查询

#### GetRefundableBillsAsync

```csharp
Task<List<BillingDto>> GetRefundableBillsAsync()
```

**功能**: 获取可退款账单列表
**业务逻辑**: 筛选已支付状态的账单
**使用场景**: 退款管理、财务审核

### 扩展业务方法 (建议扩展)

#### GetPagedAsync

```csharp
Task<PagedResultDto<BillingDto>> GetPagedAsync(BillingQueryDto query)
```

**功能**: 分页条件查询账单
**查询条件**: 
- 患者姓名关键词搜索
- 医生筛选
- 状态筛选
- 金额范围筛选
- 时间范围筛选

**使用场景**: 账单管理页面的分页列表

#### ProcessPaymentAsync

```csharp
Task<bool> ProcessPaymentAsync(Guid id, decimal amount, string paymentMethod)
```

**功能**: 处理支付（支持部分支付）
**业务逻辑**: 
- 支付金额验证
- 部分支付处理
- 支付方式记录
- 状态自动更新

**使用场景**: 多种支付方式、分期支付

#### GetStatisticsAsync

```csharp
Task<BillingStatisticsDto> GetStatisticsAsync(DateTime startDate, DateTime endDate)
```

**功能**: 获取账单统计信息
**统计内容**: 
- 各状态账单数量
- 收入统计
- 退款统计
- 医生开单量统计

**使用场景**: 财务报表、数据分析

## 仓储层 (IBillingRepository & BillingRepository)

### 基础CRUD方法

#### GetByIdAsync

```csharp
Task<BillingModel?> GetByIdAsync(Guid id)
```

**功能**: 根据ID获取账单实体
**特点**: 包含账单明细
**使用场景**: 服务层调用的底层数据操作

#### GetListAsync

```csharp
Task<List<BillingModel>> GetListAsync()
```

**功能**: 获取所有账单实体列表
**使用场景**: 批量操作、全量查询

#### AddAsync

```csharp
Task<bool> AddAsync(BillingModel billingModel)
```

**功能**: 新增账单
**特点**: 同时保存账单明细
**使用场景**: 创建新账单

#### UpdateAsync

```csharp
Task<bool> UpdateAsync(BillingModel billingModel)
```

**功能**: 更新账单
**特点**: 处理明细的增删改
**使用场景**: 账单信息修改

#### DeleteAsync

```csharp
Task<bool> DeleteAsync(Guid id)
```

**功能**: 删除账单
**实现**: 软删除，设置IsDeleted标记
**使用场景**: 账单删除操作

### 查询方法

#### GetByPatientIdAsync

```csharp
Task<List<BillingModel>> GetByPatientIdAsync(Guid patientId)
```

**功能**: 根据患者ID查询账单
**使用场景**: 患者账单查询

#### SearchAsync

```csharp
Task<List<BillingModel>> SearchAsync(string keyword)
```

**功能**: 关键词搜索账单
**搜索范围**: 账单编码、患者信息等
**使用场景**: 账单搜索功能

#### GetByStatusAsync

```csharp
Task<List<BillingModel>> GetByStatusAsync(BillingStatus status)
```

**功能**: 根据状态查询账单
**使用场景**: 状态筛选查询

## 权限控制策略

### 角色级别权限

- **收费员**: 可创建、查看、收款、退款申请
- **医生**: 可创建账单（开单），查看自己开具的账单
- **财务人员**: 可查看所有账单，处理退款审批
- **管理员**: 可查看和操作所有账单，审批退款
- **患者**: 可查看自己的账单，申请退款

### 操作权限

- **创建账单**: 医生、收费员
- **收款操作**: 收费员、财务人员
- **退款申请**: 患者、医生、收费员
- **退款审批**: 财务人员、管理员
- **账单删除**: 管理员、收费员（限制条件）

### 数据访问控制

- 医生只能访问自己开具的账单
- 患者只能访问自己的账单
- 收费员可访问当日账单
- 财务人员和管理员可访问所有账单

## 业务规则

### 账单状态流转

```
Pending → Paid → Completed
   ↓        ↓
Cancelled  Refunded
   ↑
PartiallyPaid → Paid
```

**状态流转规则**:
- 待付款可以支付、取消
- 部分付款可以继续支付、取消
- 已付款可以完成、申请退款
- 已完成和已退款为终态
- 已取消不能再变更

### 业务约束

- **金额验证**: 支付金额不能超过账单总额
- **状态限制**: 只有特定状态可以进行特定操作
- **权限验证**: 退款审批需要特殊权限
- **时间限制**: 可设置退款申请时间限制

### 数据完整性

- **关联验证**: 患者、医生、处方必须存在且有效
- **明细验证**: 至少包含一个账单明细
- **金额验证**: 总金额与明细金额一致
- **状态验证**: 状态流转必须符合业务规则

## 集成依赖

### 模块依赖

- **LYBT.Module.Patients**: 患者模块（患者信息验证）
- **LYBT.Module.Doctors**: 医生模块（医生信息验证）
- **LYBT.Module.Prescriptions**: 处方模块（处方费用计算）
- **LYBT.Infrastructure**: 基础设施（日志、缓存、配置）

### 外部集成

- **支付系统**: 多种支付方式集成
- **财务系统**: 账务数据同步
- **打印系统**: 收费凭证打印
- **通知系统**: 支付成功、退款等通知

## 使用示例

### 创建账单

```csharp
var createDto = new BillingCreateDto {
    PatientId = patientId,
    DoctorId = doctorId,
    PrescriptionId = prescriptionId,
    Items = new List<BillingItemDto> {
        new() {
            Name = "挂号费",
            UnitPrice = 10m,
            Quantity = 1m
        },
        new() {
            Name = "药材费",
            UnitPrice = 85.5m,
            Quantity = 1m
        }
    },
    TotalAmount = 95.5m,
    PaymentMethod = "微信支付",
    Remark = "门诊费用"
};

var result = await billingService.AddAsync(createDto);
```

### 处理支付

```csharp
// 标记为已支付
var paymentResult = await billingService.MarkAsPaidAsync(billingId);

// 处理部分支付
var partialPayment = await billingService.ProcessPaymentAsync(billingId, 50m, "现金");
```

### 退款处理

```csharp
// 申请退款
var refundRequest = await billingService.RequestRefundAsync(billingId, "患者要求退药");

// 审批退款
var approveResult = await billingService.ApproveRefundAsync(billingId);
```

### 查询账单

```csharp
// 查询患者账单
var patientBills = await billingService.GetByPatientIdAsync(patientId);

// 查询待付款账单
var pendingBills = await billingService.GetByStatusAsync(BillingStatus.Pending);

// 搜索账单
var searchResults = await billingService.SearchAsync("张三");
```

### 获取统计信息

```csharp
var statistics = await billingService.GetStatisticsAsync(
    DateTime.Today.AddDays(-30), 
    DateTime.Today
);

Console.WriteLine($"本月收入总额: {statistics.TotalRevenue:C}");
Console.WriteLine($"待收金额: {statistics.PendingAmount:C}");
Console.WriteLine($"退款总额: {statistics.RefundAmount:C}");
Console.WriteLine($"账单总数: {statistics.TotalBills}");
```

### 账单明细计算

```csharp
// 自动计算总额
var items = new List<BillingItemDto> {
    new() { Name = "诊疗费", UnitPrice = 30m, Quantity = 1m },
    new() { Name = "检查费", UnitPrice = 120m, Quantity = 1m },
    new() { Name = "药品费", UnitPrice = 45.5m, Quantity = 2m }
};

var totalAmount = items.Sum(item => item.SubTotal); // 自动计算: 30 + 120 + (45.5 * 2) = 241
```

## 扩展建议

### 功能扩展

- **分期支付**: 支持账单分期支付
- **优惠折扣**: 支持各种优惠政策
- **医保结算**: 集成医保支付接口
- **电子发票**: 电子发票生成和管理
- **自动对账**: 与银行流水自动对账

### 技术优化

- **支付异步**: 支付结果异步通知处理
- **缓存策略**: 账单统计数据缓存
- **批量处理**: 批量账单处理功能
- **数据归档**: 历史账单数据归档
- **性能优化**: 大数据量查询优化

### 集成增强

- **移动支付**: 支付宝、微信等移动支付
- **POS机**: 刷卡支付设备集成
- **财务软件**: 用友、金蝶等财务软件集成
- **银行接口**: 银行支付接口集成
- **税务系统**: 税务申报系统集成

### 业务优化

- **智能定价**: 基于成本和市场的智能定价
- **欠费管理**: 欠费提醒和催收管理
- **财务分析**: 收入分析和预测
- **成本控制**: 成本核算和控制
- **合规审计**: 财务合规性检查和审计
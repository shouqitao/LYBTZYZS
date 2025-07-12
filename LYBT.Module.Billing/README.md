## AGENTS.md — 费用模块（LYBT.Module.Billing）

### 1. Agent 概述

费用模块负责诊疗过程中所有应收项目的账单生成、管理与查询，支持多项目组合计费，包括挂号费、处方费、治疗费用等。

### 2. 核心能力

- 创建账单记录（含账单明细）
- 修改账单金额或支付状态
- 删除账单
- 标记已支付、完成及退款
- 获取账单列表和单条详情

### 3. 输入输出规范

#### 输入

- `BillingCreateDto`：新增账单信息（包含患者ID、项目列表、金额等）
- `BillingEditDto`：修改账单信息
- `BillingQueryDto`：分页/过滤参数

#### 输出

- `BillingDto`：账单列表项
- `BillingDetailDto`：账单详情（含账单项目）
- `(IList<BillingDto>, int TotalCount)`：分页结果

### 4. 协作与依赖模块

- **处方模块**：处方费用计入账单
- **诊疗模块**：治疗项目计费
- **挂号模块**：挂号费计入账单
- **系统设置模块**：默认费用配置（如代煎费）
- **日志模块**：记录账单生成与修改行为
- **基础设施模块**：持久化账单数据

### 5. 示例场景

#### 创建账单

```csharp
var dto = new BillingCreateDto {
  PatientId = patientId,
  DoctorId = doctorId,
  Items = new List<BillingItemDto> {
    new BillingItemDto { Name = "挂号费", Quantity = 1, UnitPrice = 10 }
  },
  TotalAmount = 10,
  PaidAmount = 0
};
bool ok = await _billingService.AddAsync(dto);
```

#### 查询账单

```csharp
var query = new BillingQueryDto {
  PatientId = patientId,
  DateRange = (DateTime.Today.AddDays(-7), DateTime.Today)
};
var (list, total) = await _billingService.SearchAsync(query);
```

### 6. 接口列表

- `Task<List<BillingDto>> GetListAsync()`
- `Task<BillingDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(BillingCreateDto dto)`
- `Task<bool> UpdateAsync(BillingEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`
- `Task<bool> MarkAsPaidAsync(Guid id)`
- `Task<bool> MarkAsCompletedAsync(Guid id)`
- `Task<bool> RequestRefundAsync(Guid id, string reason)`
- `Task<bool> ApproveRefundAsync(Guid id)`
- `Task<bool> RejectRefundAsync(Guid id)`
- `Task<bool> CancelAsync(Guid id)`


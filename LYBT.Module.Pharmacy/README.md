## AGENTS.md — 药房模块（LYBT.Module.Pharmacy）

### 1. Agent 概述

药房模块负责管理医生流转的处方配药、抓药、代煎等操作，是医生开方与患者取药之间的桥梁，支撑诊所收费与药品流转全过程。

### 2. 核心能力

- 药房配药单创建与编辑
- 删除配药单
- 获取配药单列表和详情
- 获取待抓药列表并标记完成

### 3. 输入输出规范

#### 输入

- `PharmacyCreateDto`：新建配药单（需含处方ID、操作员ID、代煎标记等）
- `PharmacyEditDto`：编辑配药信息
- `PharmacyQueryDto`：按状态、患者、时间等条件搜索

#### 输出

- `PharmacyDto`：配药历史记录
- `(IList<PharmacyDto>, int TotalCount)`：分页结果
- `bool`：操作成功与否

### 4. 协作与依赖模块

- **处方模块**：药房需获取医生处方信息
- **费用模块**：代煎费与抓药费需加入账单
- **系统设置模块**：药房工作流、状态配置
- **基础设施模块**：药房配药数据持久化

### 5. 示例场景

#### 新建配药单

```csharp
var dto = new PharmacyCreateDto {
  PrescriptionId = presId,
  OperatorId = userId,
  IsDecoct = true
};
bool ok = await _pharmacyService.AddAsync(dto);
```

#### 查询药房状态

```csharp
var query = new PharmacyQueryDto {
  Status = PharmacyStatus.Waiting
};
var (list, total) = await _pharmacyService.SearchAsync(query);
```

### 6. 接口列表

- `Task<List<PharmacyDto>> GetListAsync()`
- `Task<PharmacyDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(PharmacyCreateDto dto)`
- `Task<bool> UpdateAsync(PharmacyEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`
- `Task<List<PharmacyDto>> GetWaitingListAsync()`
- `Task<bool> MarkAsPreparedAsync(Guid id)`


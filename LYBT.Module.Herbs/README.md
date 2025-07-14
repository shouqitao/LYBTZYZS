## AGENTS.md — 药材模块（LYBT.Module.Herbs）

### 1. Agent 概述

药材模块用于管理诊所中使用的中药材信息，包括药材名称、功效、单位、单价等，支持维护药材目录及配合处方模块计算费用。

### 2. 核心能力

- 添加新药材
- 编辑药材信息（如单价、功效、单位）
- 删除药材
- 获取药材列表
- Excel 导入导出

### 3. 输入输出规范

#### 输入

- `HerbCreateDto`：新增药材
- `HerbEditDto`：编辑药材
- `HerbImportDto`：批量导入
- `HerbQueryDto`：模糊搜索与分页参数

#### 输出

- `HerbDto`：药材信息展示
- `(IList<HerbDto>, int TotalCount)`：分页结果
- `bool`：表示操作成功与否

### 4. 协作与依赖模块

- **处方模块**：引用药材信息用于处方开具
- **经验方模块**：引用药材用于组成药方模板
- **诊疗模块**：药方部分引用药材
- **费用模块**：根据药材单价计算处方总价
- **基础设施模块**：持久化药材记录

### 5. 示例场景

#### 新增药材

```csharp
var dto = new HerbCreateDto {
  Name = "黄芪",
  Effect = "补气固表",
  Unit = "g",
  UnitPrice = 2.0M
};
bool result = await _herbService.AddAsync(dto);
```

#### 批量导入药材

```csharp
var list = new List<HerbImportDto> {
  new HerbImportDto { Name = "党参", Unit = "g", UnitPrice = 1.8M, Effect = "补中益气" },
  // ...
};
bool ok = await _herbService.BatchImportAsync(list);
```

### 6. 接口列表

- `Task<List<HerbDto>> GetListAsync()`
- `Task<HerbDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(HerbCreateDto dto)`
- `Task<bool> UpdateAsync(HerbEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/herbs` | 已实现 |
| `PUT /api/herbs/{id}` | 已实现 |
| `GET /api/herbs` | 已实现 |
| `GET /api/herbs/{id}` | 已实现 |
| `DELETE /api/herbs/{id}` | 已实现 |
| `POST /api/herbs/import` | 已实现 | JSON 批量导入 |
| `POST /api/herbs/importExcel` | 已实现 | Excel 批量导入 |
| `GET /api/herbs/exportExcel` | 已实现 | Excel 导出 |



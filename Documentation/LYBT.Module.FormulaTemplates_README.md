## AGENTS.md — 药方模板模块（LYBT.Module.FormulaTemplates）

### 1. Agent 概述

药方模板模块用于管理中医常用经验方，包括模板名称、组成药材及剂量，医生可在诊疗过程中引用模板快速生成处方，提升开方效率。

### 2. 核心能力

- 创建药方模板（包含名称和药材组成）
- 编辑药方模板（添加/修改/删除药材）
- 删除药方模板
- 获取模板列表及详情

### 3. 输入输出规范

#### 输入

- `FormulaTemplateCreateDto`：新增药方模板
- `FormulaTemplateEditDto`：编辑模板
- `FormulaTemplateQueryDto`：查询参数

#### 输出

- `FormulaTemplateDto`：模板列表项
- `FormulaTemplateDetailDto`：模板详情（含药材组成）
- `(IList<FormulaTemplateDto>, int TotalCount)`：分页结果

### 4. 协作与依赖模块

- **药材模块**：药方模板使用药材定义
- **处方模块**：处方可导入模板后调整生成药方
- **诊疗模块**：可在诊断过程中引用经验方
- **基础设施模块**：持久化模板数据

### 5. 示例场景

#### 创建药方模板

```csharp
var dto = new FormulaTemplateCreateDto {
  Name = "清热解毒方",
  Herbs = new List<HerbItemDto> {
    new HerbItemDto { Name = "金银花", Quantity = 10, Unit = "g" },
    new HerbItemDto { Name = "连翘", Quantity = 10, Unit = "g" }
  }
};
bool success = await _formulaTemplateService.AddAsync(dto);
```

### 6. 接口列表

- `Task<List<FormulaTemplateDto>> GetListAsync()`
- `Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id)`
- `Task<bool> AddAsync(FormulaTemplateCreateDto dto)`
- `Task<bool> UpdateAsync(FormulaTemplateEditDto dto)`
- `Task<bool> DeleteAsync(Guid id)`

### Web API 接口对照

| 设计接口 | 状态 | 备注 |
| --- | --- | --- |
| `POST /api/formulatemplates` | 已实现 | |
| `PUT /api/formulatemplates/{id}` | 已实现 | Id 由 DTO 传入 |
| `GET /api/formulatemplates` | 已实现 | |
| `GET /api/formulatemplates/{id}` | 已实现 | |
| `DELETE /api/formulatemplates/{id}` | 已实现 | |
| `POST /api/formulatemplates/import` | 已实现 | JSON 批量导入 |

## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```

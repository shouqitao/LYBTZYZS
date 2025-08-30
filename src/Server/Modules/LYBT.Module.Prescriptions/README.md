# LYBT.Module.Prescriptions

处方模块，保存和编辑患者处方信息。

主要支持新增处方、编辑处方、删除处方以及作废处方等操作，可获取全部处方列表和单条处方详情。

## 主要服务及接口
- `IPrescriptionService` / `PrescriptionService`
- `IPrescriptionApi` 外部接口调用

## 重要模型和DTO
- `PrescriptionModel`、`PrescriptionHerbModel`
- 枚举 `PrescriptionStatus`

## 用法
在 Prism 应用中加载 `PrescriptionsModule`，通过 `IPrescriptionService` 操作处方数据。

### 接口概览

- `Task<List<PrescriptionDto>> GetAllAsync()`
- `Task<PrescriptionDetailDto?> GetByIdAsync(string id)`
- `Task<bool> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)`
- `Task<bool> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)`
- `Task<bool> DeleteAsync(string id, Guid operatorId, string operatorName)`
- `Task<bool> CancelAsync(string id, Guid operatorId, string operatorName)`

## Running Tests / 运行测试

Execute this project's unit tests with:

```bash
dotnet test
```

使用以下命令运行本项目的单元测试：

```bash
dotnet test
```

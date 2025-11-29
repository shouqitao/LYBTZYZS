# OpenSpec Proposal: 统一MedicalCase模块DTO到Shared层

## 元数据

| 字段 | 值 |
|------|-----|
| **Proposal ID** | consolidate-medicalcase-dtos |
| **状态** | Draft |
| **创建时间** | 2025-11-29 15:53 |
| **作者** | Claude Code |
| **影响范围** | Server端 + Shared层 - DTO定义 |
| **优先级** | 中 (架构规范遵从) |

---

## Why

### 当前问题

1. **架构违规**: 根据`project.md`规范，DTO应统一定义在`LYBT.Shared.Models`层，但`src/Server/Modules/LYBT.Module.MedicalCase/Dtos/`包含4个DTO文件

2. **代码重复**: `SetPrescriptionFlagRequest`在两处都有定义：
   - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/SetPrescriptionFlagRequest.cs`
   - `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDtos.cs` (行524-529)

3. **功能重叠**: 
   - `MedicalCaseDetailResponse` vs `MedicalCaseDetailDto` - 字段大部分相同
   - `MedicalCasePrescriptionDto` vs `PrescriptionDto` - 简化版本

4. **命名不一致**:
   - Server层: `*Response`, `*Request` 后缀
   - Shared层: `*Dto`, `*Request` 后缀

### 现有Server层DTO清单

| 文件 | 行数 | 问题 |
|------|------|------|
| `SetPrescriptionFlagRequest.cs` | 17 | 完全重复，Shared层已有相同定义 |
| `MedicalCaseDetailResponse.cs` | 79 | 与`MedicalCaseDetailDto`功能重叠 |
| `MedicalCasePrescriptionDto.cs` | 77 | 与`PrescriptionDto`功能重叠 |
| `UpdateMedicalCaseRequest.cs` | 134 | 包含嵌套类型，需迁移 |

---

## What Changes

### 变更策略

1. **删除重复**: 直接删除`SetPrescriptionFlagRequest.cs`，使用Shared层版本
2. **合并或迁移**: 评估其他3个DTO，决定合并到现有DTO或迁移到Shared层
3. **更新引用**: 修改所有使用这些DTO的代码引用
4. **统一命名**: 遵循Shared层命名规范

### 变更范围

```
删除文件:
  src/Server/Modules/LYBT.Module.MedicalCase/Dtos/SetPrescriptionFlagRequest.cs

迁移到Shared层 (需详细设计):
  src/Server/Modules/LYBT.Module.MedicalCase/Dtos/MedicalCaseDetailResponse.cs
  src/Server/Modules/LYBT.Module.MedicalCase/Dtos/MedicalCasePrescriptionDto.cs
  src/Server/Modules/LYBT.Module.MedicalCase/Dtos/UpdateMedicalCaseRequest.cs

更新引用 (12个文件):
  src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs
  src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMappingProfile.cs
  src/Server/Modules/LYBT.Module.MedicalCase/Controllers/MedicalCaseController.cs
  src/Client/Desktop/Core/LYBT.Desktop.DataManagers/DataManagers/MedicalCaseDataManager.cs
  src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/Api/IMedicalCaseApi.cs
  src/Client/Desktop/Core/LYBT.Desktop.DataManagers/Services/Interfaces/IMedicalCaseService.cs
  tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/...
```

---

## 验证计划

### 测试覆盖

1. **编译验证** - 确保所有项目编译通过
2. **单元测试** - 运行MedicalCase模块测试
3. **API测试** - 验证MedicalCase相关API端点

### 验证步骤

```bash
# 1. 编译验证
dotnet build LYBT.All.sln

# 2. 运行测试
dotnet test tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests

# 3. 验证API (手动)
# GET /api/v1/medicalcases/{id}
# PUT /api/v1/medicalcases/{id}
```

---

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 引用更新遗漏 | 中 | 使用IDE全局查找替换，编译验证 |
| 命名空间冲突 | 低 | 逐步迁移，每步验证 |
| 序列化兼容性 | 低 | API未发布，无外部兼容性需求 |

---

## 决策

- [ ] **批准** - 执行此提案
- [ ] **拒绝** - 保持现状
- [ ] **修改** - 需要调整后再审批

---

## 相关Issue/PR

- 无直接关联Issue（技术债务清理）

---

## 审批

| 审批人 | 日期 | 决定 |
|--------|------|------|
| | | |

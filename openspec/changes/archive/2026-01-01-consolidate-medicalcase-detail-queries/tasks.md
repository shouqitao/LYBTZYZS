# Tasks: consolidate-medicalcase-detail-queries

## Phase 1: Server端API实现

### Task 1.1: 创建BatchDetailQueryDto
- **文件**: `src/Server/LYBT.Shared.Models/Contracts/MedicalCase/BatchDetailQueryDto.cs`
- **内容**: 
  - `List<Guid> Ids` 属性
  - 验证注解（最多50个）

### Task 1.2: 添加Controller端点
- **文件**: `src/Server/LYBT.Api/Controllers/MedicalCasesController.cs`
- **方法**: `GetBatchDetailsAsync`
- **路由**: `POST /api/v1/medicalcases/batch-details`

### Task 1.3: 添加Service方法
- **文件**: `src/Server/LYBT.Api/Services/MedicalCaseService.cs`
- **方法**: `GetBatchDetailsAsync(List<Guid> ids)`
- **实现**: 使用EF Core `Contains`优化查询

## Phase 2: Client端API接口

### Task 2.1: 添加IMedicalCaseApi方法
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **方法**: `GetBatchDetailsAsync`

### Task 2.2: 添加IMedicalCaseRepository方法
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseRepository.cs`
- **方法**: `GetBatchDetailsAsync`

### Task 2.3: 实现Repository方法
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **方法**: `GetBatchDetailsAsync`

## Phase 3: 调用点迁移

### Task 3.1: 重构HistoryPrescriptionSelectionDialogViewModel
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/HistoryPrescriptionSelectionDialogViewModel.cs`
- **变更**:
  - 改用`QueryAsync`获取医案列表
  - 改用`GetBatchDetailsAsync`批量获取详情
  - 删除N+1循环

## Phase 4: 验证

### Task 4.1: 编译验证
- `dotnet build LYBT.All.sln -c Release --no-restore`

### Task 4.2: 功能测试
- 验证历史处方选择对话框正常工作
- 验证API请求次数（应为2次而非N+1次）

## 完成标准

- [ ] Server端`/batch-details`端点可用
- [ ] Client端批量查询方法实现
- [ ] `HistoryPrescriptionSelectionDialogViewModel`无N+1查询
- [ ] 编译通过
- [ ] 功能正常

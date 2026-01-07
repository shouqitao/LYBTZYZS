# Tasks: optimize-medicalcase-api

## 实施进度

| 阶段 | 状态 | 完成日期 | 备注 |
|------|------|----------|------|
| Phase 1 | ✅ 已完成 | 2025-12-31 | CloseCaseAsync返回类型统一 |
| Phase 2 | ✅ 已完成 | 2025-12-31 | GetById端点简化+Obsolete标注 |
| Phase 3 | ✅ 已完成 | 2025-12-31 | 查询端点整合+Obsolete标注 |
| Phase 4 | ✅ 已完成 | 2025-12-31 | CHANGELOG已更新，测试覆盖充分 |
| Phase 5 | ✅ 已验证 | 2025-12-31 | 核心功能已实现，5.2延后到v2.0 |
| Phase 6 | ⏳ 待实施 | - | 废弃代码清理(需先完成迁移) |

### Phase 5 验证详情 (2025-12-31)

**已验证通过的任务:**
- 5.1 处方Items全量替换: `MedicalCaseCommandService.UpdateExistingPrescription`使用`Clear()+Add()`模式 ✅
- 5.3.3 Client SaveDraft API: 已集成到`IMedicalCaseApi.SaveDraftAsync` ✅
- 5.3.5-5.3.7 四选对话框: `MedicalCaseStartCoordinator`完整实现四种选择逻辑 ✅
- 5.3.9 暂存医案测试: `MedicalCaseStateServiceTests`和`MedicalCaseQueryServiceTests`覆盖充分 ✅
- 5.4.4 权限测试覆盖: `MedicalCaseAuthorizationServiceTests`测试覆盖率>90% ✅
- 5.5.1-5.5.2 处方生命周期: 使用`NeedsPrescription`标志控制（过渡方案）✅

**延后任务:**
- 5.2 移除NeedsPrescription字段: 54个文件深度耦合，建议延后到v2.0大版本

**清理工作:**
- 删除Patients模块重复的UnfinishedCaseDialog（3个文件）
- 统一使用`CommonDialogService.ShowUnfinishedCaseDialogAsync`

## 任务概览

| 阶段 | 任务数 | 预估工时 | 依赖 |
|------|--------|----------|------|
| Phase 1: CloseCaseAsync返回类型修正 | 8 | 2h | 无 |
| Phase 2: GetById端点简化 + 废弃标注 | 8 | 2h | Phase 1 |
| Phase 3: 查询端点整合 + 废弃标注 | 18 | 4.5h | Phase 2 |
| Phase 4: 测试与文档 | 10 | 2h | Phase 3 |
| Phase 5: 设计决策实现(Q1-Q5) | 25 | 6h | Phase 1 |
| Phase 6: 废弃代码清理 | 12 | 2h | Phase 4, 5 |
| **总计** | **81** | **18.5h** | - |

---

## Phase 1: CloseCaseAsync返回类型修正

**目标**: 统一状态操作的返回类型，使CloseCaseAsync与其他状态方法一致

### 1.1 Server端修改

#### Task 1.1.1: 修改MedicalCaseStateService.CloseCaseAsync返回类型
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs`
- **变更**:
  - 修改方法签名: `Task<ApiResponse>` → `Task<ApiResponse<MedicalCaseDetailDto>>`
  - 关闭成功后调用QueryService获取完整DetailDto
  - 返回包含更新后数据的ApiResponse
- **验证**: 编译通过，单元测试通过

#### Task 1.1.2: 修改IMedicalCaseStateService接口
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseStateService.cs`
- **变更**: 更新接口方法签名与实现一致
- **验证**: 编译通过

#### Task 1.1.3: 修改MedicalCaseController.CloseCase端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  - 更新方法返回类型注解
  - 更新Swagger文档注释
- **验证**: Swagger UI显示正确返回类型

#### Task 1.1.4: 更新MedicalCaseStateServiceTests
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseStateServiceTests.cs`
- **变更**:
  - 修改CloseCaseAsync测试方法，验证返回DetailDto
  - 添加返回数据完整性断言
- **验证**: 测试通过

### 1.2 Client端修改

#### Task 1.2.1: 修改IMedicalCaseApi.CloseCaseAsync
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **变更**:
  - `Task<ApiResponse>` → `Task<ApiResponse<MedicalCaseDetailDto>>`
- **验证**: 编译通过

#### Task 1.2.2: 修改MedicalCaseRepository.CloseCaseAsync
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **变更**:
  - 更新方法签名和返回类型
  - 处理返回的DetailDto
- **验证**: 编译通过

#### Task 1.2.3: 修改MedicalCaseService.CloseCaseAsync
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**:
  - 更新方法签名
  - 使用返回的DetailDto更新本地状态
  - 避免额外的GetById调用
- **验证**: 编译通过，功能测试正常

#### Task 1.2.4: 更新调用方ViewModel
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- **变更**:
  - 更新CloseCase命令处理
  - 使用返回数据刷新UI
- **验证**: UI关闭医案后正确显示最终状态

---

## Phase 2: GetById端点简化 + 废弃标注

**目标**: 简化GetById端点设计，统一返回完整DetailDto，删除冗余的with-details端点

> **设计决策**: 取消`includeDetails`参数设计
> - 当前两个端点底层执行相同的Include查询，参数不产生性能优化
> - 简化端点只在映射层过滤数据，数据库开销相同
> - v1.0功能完善阶段，优先简化API设计

### 2.0 废弃标注规划（任务启动前分析）

| 位置 | 废弃项 | Obsolete消息 | 替代方案 |
|------|--------|--------------|----------|
| Controller | `GET /{id}/with-details` | "Use GetById. Remove in v2.0" | `GET /{id}` (返回完整数据) |
| QueryService | `GetByIdWithDetailsAsync` | "Use GetByIdAsync. Remove in v2.0" | `GetByIdAsync(id)` |
| IMedicalCaseApi | `GetMedicalCaseByIdWithDetailsAsync` | "Use GetMedicalCaseByIdAsync. Remove in v2.0" | `GetMedicalCaseByIdAsync(id)` |
| Repository | `GetByIdWithDetailsAsync` | 同上 | 同上 |

### 2.1 Server端修改

#### Task 2.1.1: 统一GetById端点返回完整数据
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  - 确保GetById端点始终返回完整的MedicalCaseDetailDto（含Consultation+Prescription）
  - 使用现有的GetMedicalCaseByIdWithDetails实现逻辑
  - 更新Swagger文档注释
- **验证**: API返回完整DetailDto

#### Task 2.1.2: 标记GetByIdWithDetails为Obsolete
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  ```csharp
  [Obsolete("Use GetById instead. Will be removed in v2.0")]
  [HttpGet("{id}/with-details")]
  public async Task<ApiResponse<MedicalCaseDetailDto>> GetByIdWithDetails(...)
  ```
- **验证**: 编译警告显示Obsolete提示

#### Task 2.1.3: 更新QueryService（可选优化）
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- **变更**:
  - 合并GetByIdAsync和GetByIdWithDetailsAsync实现
  - GetByIdAsync始终执行Include查询
  - 标记GetByIdWithDetailsAsync为Obsolete
- **验证**: 编译通过，单元测试通过

#### Task 2.1.4: 更新MedicalCaseQueryServiceTests
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseQueryServiceTests.cs`
- **变更**:
  - 更新GetByIdAsync测试验证返回完整数据
  - 移除或标记obsolete相关测试
- **验证**: 测试通过

### 2.2 Client端修改

#### Task 2.2.1: 标记IMedicalCaseApi旧方法为Obsolete
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **变更**:
  - 标记GetMedicalCaseByIdWithDetailsAsync为Obsolete
  - 确保GetMedicalCaseByIdAsync返回完整DetailDto
- **验证**: 编译通过

#### Task 2.2.2: 更新调用方使用统一端点
- **文件**: 
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**:
  - 将所有GetByIdWithDetailsAsync调用替换为GetByIdAsync
  - 验证返回数据正确处理
- **验证**: 编译通过，功能测试正常

#### Task 2.2.3: 添加集成测试
- **文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`
- **变更**:
  - 添加GetById_ReturnsFullDetailDto测试（含Consultation+Prescription）
  - 验证返回数据完整性
- **验证**: 集成测试通过

#### Task 2.2.4: 清理旧方法调用
- **文件**: 全局搜索 `GetByIdWithDetailsAsync` 或 `GetMedicalCaseByIdWithDetails`
- **变更**: 替换为统一的GetByIdAsync调用
- **验证**: 无编译错误，Obsolete警告仅在声明处

---

## Phase 3: 查询端点整合 + 废弃标注

**目标**: 将8个查询端点整合为3个核心端点，旧端点标注[Obsolete]

### 3.0 废弃标注规划（任务启动前分析）

| 位置 | 废弃项 | Obsolete消息 | 替代方案 |
|------|--------|--------------|----------|
| Controller | `GET /pending` | "Use GetMedicalCases with QueryType=Pending. Remove in v2.0" | `GET /?queryType=Pending` |
| Controller | `GET /patient/{id}` | "Use GetMedicalCases with QueryType=ByPatient. Remove in v2.0" | `GET /?queryType=ByPatient&patientId=` |
| Controller | `GET /patient/{id}/unfinished` | "Use GetMedicalCases with QueryType=Unfinished. Remove in v2.0" | `GET /?queryType=Unfinished&patientId=` |
| Controller | `GET /patient/{id}/recent` | "Use GetMedicalCases with QueryType=Recent. Remove in v2.0" | `GET /?queryType=Recent&patientId=` |
| QueryService | `GetPendingCasesAsync` | "Use QueryAsync with QueryType.Pending. Remove in v2.0" | `QueryAsync(Pending)` |
| QueryService | `GetByPatientIdAsync` | "Use QueryAsync with QueryType.ByPatient. Remove in v2.0" | `QueryAsync(ByPatient)` |
| QueryService | `GetUnfinishedByPatientAsync` | "Use QueryAsync with QueryType.Unfinished. Remove in v2.0" | `QueryAsync(Unfinished)` |
| QueryService | `GetPatientRecentAsync` | "Use QueryAsync with QueryType.Recent. Remove in v2.0" | `QueryAsync(Recent)` |
| IMedicalCaseApi | `GetPendingCasesAsync` | 同Controller | 同Controller |
| IMedicalCaseApi | `GetMedicalCasesByPatientIdAsync` | 同Controller | 同Controller |
| IMedicalCaseApi | `GetUnfinishedCaseByPatientIdAsync` | 同Controller | 同Controller |
| IMedicalCaseApi | `GetPatientRecentMedicalCasesAsync` | 同Controller | 同Controller |

### 3.1 统一列表查询端点

#### Task 3.1.1: 定义QueryType枚举
- **文件**: `src/Shared/LYBT.Shared.Models/Enums/MedicalCaseQueryType.cs` (新建)
- **内容**:
  ```csharp
  public enum MedicalCaseQueryType
  {
      All,           // 默认：分页列表
      ByPatient,     // 按患者ID
      Pending,       // 待看诊
      Unfinished,    // 未完成
      Recent         // 最近(处方参考)
  }
  ```
- **验证**: 编译通过

#### Task 3.1.2: 定义统一查询参数DTO
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseQueryDto.cs` (新建)
- **内容**:
  ```csharp
  public class MedicalCaseQueryDto
  {
      public MedicalCaseQueryType QueryType { get; set; } = MedicalCaseQueryType.All;
      public Guid? PatientId { get; set; }
      public Guid? DoctorId { get; set; }
      public string? Keyword { get; set; }
      public int PageIndex { get; set; } = 1;
      public int PageSize { get; set; } = 20;
      public bool IncludeAllDoctors { get; set; } = false;
      public int? Limit { get; set; }  // 用于Recent查询
  }
  ```
- **验证**: 编译通过

#### Task 3.1.3: 实现统一查询方法
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- **变更**:
  - 添加 `QueryAsync(MedicalCaseQueryDto query)` 方法
  - 根据QueryType分发到不同查询逻辑
  - 复用现有查询实现
- **验证**: 编译通过，单元测试通过

#### Task 3.1.4: 更新IMedicalCaseQueryService接口
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs`
- **变更**: 添加QueryAsync方法到接口
- **验证**: 编译通过

#### Task 3.1.5: 添加统一查询Controller端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  ```csharp
  [HttpGet]
  public async Task<ApiResponse<PagedResult<MedicalCaseListDto>>> GetMedicalCases(
      [FromQuery] MedicalCaseQueryDto query)
  ```
- **验证**: Swagger显示正确参数

#### Task 3.1.6: 标记旧查询端点为Obsolete
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**: 为以下端点添加Obsolete特性:
  - `GetMedicalCasesByPatientId` → "Use GetMedicalCases with QueryType=ByPatient"
  - `GetPendingCases` → "Use GetMedicalCases with QueryType=Pending"
  - `GetUnfinishedCaseByPatientId` → "Use GetMedicalCases with QueryType=Unfinished"
  - `GetPatientRecentMedicalCases` → "Use GetMedicalCases with QueryType=Recent"
- **验证**: 编译警告显示Obsolete提示

#### Task 3.1.7: 更新QueryServiceTests
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseQueryServiceTests.cs`
- **变更**:
  - 添加QueryAsync测试方法
  - 测试各种QueryType场景
- **验证**: 测试通过

### 3.2 Client端适配

#### Task 3.2.1: 添加QueryType枚举到Client
- **文件**: 共享LYBT.Shared.Models，Client自动可用
- **验证**: Client项目可引用枚举

#### Task 3.2.2: 修改IMedicalCaseApi添加统一查询
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **变更**:
  - 添加 `GetMedicalCasesAsync(MedicalCaseQueryDto query)` 方法
  - 标记旧方法为Obsolete
- **验证**: 编译通过

#### Task 3.2.3: 修改MedicalCaseRepository
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **变更**:
  - 添加统一查询方法
  - 旧方法内部调用新方法
- **验证**: 编译通过

#### Task 3.2.4: 修改MedicalCaseService
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**:
  - 更新各场景使用新API
  - GetByPatientAsync → QueryType.ByPatient
  - GetPendingAsync → QueryType.Pending
- **验证**: 编译通过，功能测试正常

#### Task 3.2.5: 更新ViewModel调用
- **文件**: 各MedicalCase相关ViewModel
- **变更**: 更新查询调用使用新API
- **验证**: UI功能正常

### 3.3 高级搜索端点

#### Task 3.3.1: 保持SearchMedicalCasesAsync不变
- **文件**: 已存在，无需修改
- **说明**: Search端点用于复杂条件搜索，保持独立
- **验证**: 确认功能正常

---

## Phase 4: 测试与文档

### 4.1 单元测试补充

#### Task 4.1.1: 补充StateService单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseStateServiceTests.cs`
- **变更**: 确保所有状态方法有测试覆盖
- **验证**: 测试覆盖率 > 80%

#### Task 4.1.2: 补充QueryService单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseQueryServiceTests.cs`
- **变更**: 确保新QueryAsync方法有完整测试
- **验证**: 测试覆盖率 > 80%

#### Task 4.1.3: 补充CommandService单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseCommandServiceTests.cs`
- **变更**: 确保聚合保存逻辑有测试覆盖
- **验证**: 测试覆盖率 > 80%

### 4.2 集成测试

#### Task 4.2.1: 添加API集成测试
- **文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`
- **变更**:
  - 测试统一查询端点
  - 测试CloseCaseAsync返回数据
  - 测试GetById返回完整DetailDto（含Consultation+Prescription）
- **验证**: 集成测试全部通过

#### Task 4.2.2: 添加聚合保存集成测试
- **文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseControllerIntegrationTests.cs`
- **变更**:
  - 测试创建带处方的医案
  - 测试更新处方Items
  - 测试删除处方(NeedsPrescription=false)
- **验证**: 集成测试全部通过

### 4.3 文档更新

#### Task 4.3.1: 更新API文档
- **文件**: `docs/reference/api/medicalcase.md` (新建或更新)
- **内容**:
  - 端点清单
  - 请求/响应示例
  - 错误码说明
- **验证**: 文档完整

#### Task 4.3.2: 更新Swagger注释
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  - 完善XML注释
  - 添加请求/响应示例
- **验证**: Swagger UI显示完整文档

#### Task 4.3.3: 更新CHANGELOG
- **文件**: `CHANGELOG.md`
- **变更**: 添加本次优化的变更记录
- **验证**: 格式正确

#### Task 4.3.4: 更新架构文档
- **文件**: `docs/explanation/architecture/medicalcase-module.md` (如存在)
- **变更**: 更新API设计说明
- **验证**: 文档与实现一致

---

## Phase 5: 设计决策实现(Q1-Q4)

**目标**: 实现设计讨论中确定的4个关键决策

### 5.1 Q1: 处方Items全量替换策略

#### Task 5.1.1: 修改PrescriptionService.UpdateItemsAsync
- **文件**: `src/Server/Modules/LYBT.Module.Prescription/Services/PrescriptionService.cs`
- **变更**:
  - 实现全量替换逻辑: 删除现有Items → 添加新Items
  - 使用单一事务保证原子性
  ```csharp
  await _context.PrescriptionItems
      .Where(i => i.PrescriptionId == prescriptionId)
      .ExecuteDeleteAsync(cancellationToken);
  await _context.PrescriptionItems.AddRangeAsync(newItems, cancellationToken);
  ```
- **验证**: 单元测试通过

#### Task 5.1.2: 更新MedicalCaseCommandService聚合保存
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- **变更**:
  - 在SaveAsync中使用全量替换策略保存处方Items
  - 确保事务边界正确
- **验证**: 聚合保存功能测试通过

#### Task 5.1.3: 添加全量替换单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Prescription.Tests/Services/PrescriptionServiceTests.cs`
- **变更**:
  - 测试Items全部替换场景
  - 测试空Items替换场景
  - 测试事务回滚场景
- **验证**: 测试覆盖率 > 90%

### 5.2 Q2: 移除NeedsPrescription字段

#### Task 5.2.1: 移除DTO中的NeedsPrescription字段
- **文件**: 
  - `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationInputDto.cs`
  - `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDetailDto.cs`
- **变更**: 删除NeedsPrescription属性
- **验证**: 编译通过

#### Task 5.2.2: 移除Entity中的NeedsPrescription字段
- **文件**: `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`
- **变更**: 删除NeedsPrescription属性
- **验证**: 编译通过

#### Task 5.2.3: 创建数据库迁移
- **命令**: `dotnet ef migrations add RemoveNeedsPrescription`
- **文件**: `src/Server/Core/LYBT.Infrastructure/Migrations/`
- **验证**: 迁移文件正确生成

#### Task 5.2.4: 更新Server端逻辑使用Items.Any()
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- **变更**:
  - 替换所有`NeedsPrescription`检查为`Prescription?.Items.Any() == true`
- **验证**: 编译通过，功能测试通过

#### Task 5.2.5: 更新Client端逻辑
- **文件**: 
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- **变更**: 移除NeedsPrescription相关逻辑，使用Items判断
- **验证**: UI功能正常

### 5.3 Q3: 暂存医案机制

#### Task 5.3.1: 添加SuspendCaseAsync方法到StateService
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseStateService.cs`
- **变更**:
  ```csharp
  public async Task<ApiResponse<MedicalCaseDetailDto>> SuspendCaseAsync(
      Guid medicalCaseId,
      MedicalCaseInputDto input,
      CancellationToken cancellationToken)
  {
      // 1. 保存当前诊断和处方数据
      await _commandService.SaveAsync(medicalCaseId, input, cancellationToken);
      // 2. 更新状态为Draft
      var entity = await _repository.GetByIdAsync(medicalCaseId);
      entity.Status = MedicalCaseStatus.Draft;
      await _repository.UpdateAsync(entity, cancellationToken);
      // 3. 返回更新后的DetailDto
      return await _queryService.GetByIdAsync(medicalCaseId);
  }
  ```
- **验证**: 编译通过

#### Task 5.3.2: 更新IMedicalCaseStateService接口
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseStateService.cs`
- **变更**: 添加SuspendCaseAsync方法签名
- **验证**: 编译通过

#### Task 5.3.3: 添加SuspendCase API端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**:
  ```csharp
  /// <summary>
  /// 暂存医案 - 保存当前诊断和处方，状态变为Draft
  /// </summary>
  [HttpPost("{id}/suspend")]
  public async Task<ApiResponse<MedicalCaseDetailDto>> SuspendCase(
      Guid id,
      [FromBody] MedicalCaseInputDto input)
  ```
- **验证**: Swagger显示端点

#### Task 5.3.4: 更新Client端IMedicalCaseApi
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **变更**: 添加SuspendCaseAsync方法
- **验证**: 编译通过

#### Task 5.3.5: 实现暂存对话框逻辑（入口1: 编辑界面暂存按钮）
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- **变更**:
  - 添加SuspendCaseCommand命令
  - 点击"暂存医案"按钮后调用SuspendCaseAsync API
  - 成功后: 状态Active→Draft，界面从编辑模式切换为查看模式
  - 查看状态(Draft)离开时直接退出，不弹出对话框
- **验证**: UI功能测试通过

#### Task 5.3.6: 实现未完成医案四选对话框（入口3: 选择患者时）
- **文件**:
  - `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/UnfinishedCaseDialog.xaml`
  - `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/UnfinishedCaseDialogViewModel.cs`
- **变更**:
  - 四项选择对话框:
    1. **继续暂存医案**: 打开Draft医案继续编辑（状态Draft→Active）
    2. **关闭暂存医案后新建**: 取消Draft医案，然后创建新Active医案
    3. **仅关闭暂存医案**: 取消Draft医案，不创建新医案
    4. **取消**: 不执行任何操作
  - 对话框显示Draft医案基本信息（创建时间、诊断摘要等）
- **验证**: 对话框正确显示四个选项

#### Task 5.3.7: 实现同一患者Draft检查逻辑
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- **变更**:
  ```csharp
  public async Task<MedicalCaseDetailDto?> GetPatientDraftCaseAsync(Guid patientId)
  {
      var query = new MedicalCaseQueryDto
      {
          QueryType = MedicalCaseQueryType.Unfinished,
          PatientId = patientId
      };
      var result = await _api.GetMedicalCasesAsync(query);
      return result.Data?.Items?.FirstOrDefault(c => c.Status == MedicalCaseStatus.Draft);
  }
  ```
  - 选择患者时调用此方法检查是否存在Draft
  - 存在则弹出四选对话框
  - 规则: 同一患者最多只能有1个Draft医案
- **验证**: 选择有Draft的患者时正确弹出对话框

#### Task 5.3.8: 集成四选对话框到患者选择流程
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- **变更**:
  - 选择患者后调用GetPatientDraftCaseAsync检查Draft
  - 无Draft: 直接创建新医案
  - 有Draft: 弹出四选对话框，根据选择执行对应操作
- **验证**: 流程完整测试通过

#### Task 5.3.9: 添加暂存医案单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseStateServiceTests.cs`
- **变更**:
  - 测试Active→Draft状态转换
  - 测试数据保存完整性
  - 测试Draft状态医案可恢复
  - 测试同一患者Draft唯一性检查
- **验证**: 测试通过

### 5.4 Q4: 角色+时间权限控制

#### Task 5.4.1: 创建IMedicalCaseAuthorizationService接口
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseAuthorizationService.cs` (新建)
- **内容**:
  ```csharp
  public interface IMedicalCaseAuthorizationService
  {
      bool CanModify(MedicalCaseModel medicalCase, UserContext user);
      bool CanDelete(MedicalCaseModel medicalCase, UserContext user);
      bool CanRestore(MedicalCaseModel medicalCase, UserContext user);
  }
  ```
- **验证**: 编译通过

#### Task 5.4.2: 实现MedicalCaseAuthorizationService
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseAuthorizationService.cs` (新建)
- **内容**:
  ```csharp
  public class MedicalCaseAuthorizationService : IMedicalCaseAuthorizationService
  {
      public bool CanModify(MedicalCaseModel medicalCase, UserContext user)
      {
          // 管理员：无任何限制
          if (user.IsAdmin)
              return true;
          
          // 医生：只能操作自己的医案
          if (medicalCase.UserId != user.Id)
              return false;
          
          // Active/Draft：自己的可编辑
          if (medicalCase.Status is MedicalCaseStatus.Active or MedicalCaseStatus.Draft)
              return true;
          
          // Completed：自己的 + 当天完成的（本地日期）
          if (medicalCase.Status == MedicalCaseStatus.Completed)
              return medicalCase.CompletedAt?.Date == DateTime.Today;
          
          // Cancelled：自己的 + 当天取消的（本地日期）
          if (medicalCase.Status == MedicalCaseStatus.Cancelled)
              return medicalCase.CancelledAt?.Date == DateTime.Today;
          
          return false;
      }
  }
  ```
- **验证**: 编译通过

#### Task 5.4.3: 集成权限检查到CommandService
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- **变更**:
  - 注入IMedicalCaseAuthorizationService
  - 在SaveAsync开始时检查权限
  - 无权限时返回403 Forbidden
- **验证**: 权限控制生效

#### Task 5.4.4: 添加权限控制单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseAuthorizationServiceTests.cs` (新建)
- **变更**:
  - 测试医生当天可修改Completed
  - 测试医生隔天不可修改Completed
  - 测试管理员任意时间可修改
  - 测试Cancelled状态权限规则
  - 测试医生只能操作自己的医案（UserId检查）
- **验证**: 测试覆盖率 > 90%

### 5.5 Q5: 处方生命周期管理

#### Task 5.5.1: 实现处方按需创建逻辑
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- **变更**:
  ```csharp
  private async Task SavePrescriptionAsync(MedicalCaseModel medicalCase, List<PrescriptionItemDto>? items)
  {
      if (items?.Any() == true)
      {
          // Items非空：创建或更新Prescription
          if (medicalCase.Prescription == null)
          {
              medicalCase.Prescription = new PrescriptionModel { MedicalCaseId = medicalCase.Id };
              _context.Prescriptions.Add(medicalCase.Prescription);
          }
          // 全量替换Items
          await _context.PrescriptionItems
              .Where(i => i.PrescriptionId == medicalCase.Prescription.Id)
              .ExecuteDeleteAsync();
          medicalCase.Prescription.Items = MapToItems(items);
      }
      else
      {
          // Items为空：删除Prescription
          if (medicalCase.Prescription != null)
          {
              _context.Prescriptions.Remove(medicalCase.Prescription);
              medicalCase.Prescription = null;
          }
      }
  }
  ```
- **验证**: 编译通过，单元测试通过

#### Task 5.5.2: 更新MedicalCaseQueryService处方判断逻辑
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- **变更**:
  - 查询时根据`Prescription != null`判断是否有处方
  - 移除对NeedsPrescription字段的依赖
- **验证**: 编译通过

#### Task 5.5.3: 更新Client端处方显示逻辑
- **文件**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionPanelViewModel.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- **变更**:
  - 使用`Prescription != null`判断是否显示处方面板
  - 移除NeedsPrescription相关绑定
  ```csharp
  public bool HasPrescription => MedicalCase?.Prescription != null;
  public bool ShowPrescriptionPanel => IsEditing || HasPrescription;
  ```
- **验证**: UI正确显示/隐藏处方面板

#### Task 5.5.4: 添加处方生命周期单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseCommandServiceTests.cs`
- **变更**:
  - 测试首次添加Item时Prescription创建
  - 测试所有Items移除后Prescription删除
  - 测试空Items保存不会创建Prescription
  - 测试Prescription删除后相关数据清理
- **验证**: 测试覆盖率 > 90%

---

## 验收标准

### 编译验收
- [ ] `dotnet build LYBT.All.sln` 无错误
- [ ] 无新增编译警告（Obsolete警告除外）

### 测试验收
- [ ] 单元测试全部通过
- [ ] 集成测试全部通过
- [ ] 新增功能测试覆盖率 > 80%

### 功能验收
- [ ] CloseCaseAsync返回完整DetailDto
- [ ] GetById返回完整DetailDto（含Consultation+Prescription）
- [ ] 统一查询端点支持所有QueryType
- [ ] 旧端点标记Obsolete但仍可用

### 文档验收
- [ ] API文档更新完成
- [ ] Swagger注释完整
- [ ] CHANGELOG已更新

---

## 风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 旧端点调用方未迁移 | 中 | 低 | Obsolete保留兼容，渐进迁移 |
| QueryType分发逻辑错误 | 低 | 中 | 充分单元测试覆盖 |
| CloseCaseAsync性能下降 | 低 | 低 | 返回数据复用现有查询 |
| Client端遗漏更新 | 中 | 中 | 全局搜索确认所有调用点 |

---

## Phase 6: 废弃代码清理

**目标**: 统一清理所有标注[Obsolete]的代码，消除技术债务
**前提**: Phase 1-5全部完成，所有调用方已迁移到新API

### 6.0 清理前验证（必做）

#### Task 6.0.1: 全局搜索废弃方法调用
- **命令**: 
  ```bash
  rg "GetByIdWithDetailsAsync|GetPendingCasesAsync|GetMedicalCasesByPatientIdAsync" src/
  rg "GetUnfinishedCaseByPatientIdAsync|GetPatientRecentMedicalCasesAsync" src/
  rg "NeedsPrescription" src/
  ```
- **验证**: 除[Obsolete]标注外无其他调用
- **输出**: 调用清单文档

#### Task 6.0.2: 编译检查Obsolete警告
- **命令**: `dotnet build LYBT.All.sln 2>&1 | grep -i "obsolete"`
- **验证**: 所有Obsolete警告都在清理清单中
- **输出**: 警告清单

#### Task 6.0.3: 运行完整测试套件
- **命令**: `dotnet test LYBT.All.sln`
- **验证**: 所有测试通过
- **条件**: 测试失败则停止清理流程

### 6.1 Server端Controller清理

#### Task 6.1.1: 删除GetByIdWithDetails端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**: 删除 `[HttpGet("{id}/details")]` 端点方法
- **验证**: 编译通过

#### Task 6.1.2: 删除GetPendingCases端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**: 删除 `[HttpGet("pending")]` 端点方法
- **验证**: 编译通过

#### Task 6.1.3: 删除GetMedicalCasesByPatientId端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**: 删除 `[HttpGet("patient/{patientId}")]` 端点方法
- **验证**: 编译通过

#### Task 6.1.4: 删除GetUnfinishedCaseByPatientId端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**: 删除 `[HttpGet("patient/{patientId}/unfinished")]` 端点方法
- **验证**: 编译通过

#### Task 6.1.5: 删除GetPatientRecentMedicalCases端点
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **变更**: 删除 `[HttpGet("patient/{patientId}/recent")]` 端点方法
- **验证**: 编译通过

### 6.2 Server端Service/Repository清理

#### Task 6.2.1: 删除QueryService废弃方法
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- **变更**: 删除以下方法:
  - `GetByIdWithDetailsAsync`
  - `GetPendingCasesAsync`
  - `GetByPatientIdAsync`
  - `GetUnfinishedByPatientAsync`
  - `GetPatientRecentAsync`
- **验证**: 编译通过

#### Task 6.2.2: 删除IMedicalCaseQueryService接口废弃方法
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs`
- **变更**: 删除对应接口方法签名
- **验证**: 编译通过

#### Task 6.2.3: 删除Repository废弃方法
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **变更**: 删除废弃的查询方法
- **验证**: 编译通过

### 6.3 Client端API/Repository清理

#### Task 6.3.1: 删除IMedicalCaseApi废弃方法
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- **变更**: 删除以下方法:
  - `GetMedicalCaseByIdWithDetailsAsync`
  - `GetPendingCasesAsync`
  - `GetMedicalCasesByPatientIdAsync`
  - `GetUnfinishedCaseByPatientIdAsync`
  - `GetPatientRecentMedicalCasesAsync`
- **验证**: 编译通过

#### Task 6.3.2: 删除MedicalCaseRepository废弃方法
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs`
- **变更**: 删除对应的Repository方法
- **验证**: 编译通过

### 6.4 DTO/Entity字段清理

#### Task 6.4.1: 删除Entity中NeedsPrescription字段
- **文件**: `src/Server/Core/LYBT.Entities/Consultations/ConsultationModel.cs`
- **变更**: 删除 `public bool NeedsPrescription { get; set; }` 属性
- **验证**: 编译通过

#### Task 6.4.2: 删除DTO中NeedsPrescription字段
- **文件**: 
  - `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationInputDto.cs`
  - `src/Shared/LYBT.Shared.Models/Contracts/Consultation/ConsultationDetailDto.cs`
- **变更**: 删除NeedsPrescription属性
- **验证**: 编译通过

#### Task 6.4.3: 创建数据库迁移删除列
- **命令**: `dotnet ef migrations add RemoveObsoleteColumns`
- **变更**: 删除Consultations表的NeedsPrescription列
- **验证**: 迁移文件正确生成

### 6.5 清理后验证

#### Task 6.5.1: 编译检查无Obsolete警告
- **命令**: `dotnet build LYBT.All.sln`
- **验证**: 无任何Obsolete相关警告

#### Task 6.5.2: 运行完整测试套件
- **命令**: `dotnet test LYBT.All.sln`
- **验证**: 所有测试通过

#### Task 6.5.3: 更新API文档
- **文件**: `docs/reference/api/medicalcase.md`
- **变更**: 移除废弃端点的文档
- **验证**: 文档与实现一致

#### Task 6.5.4: 更新CHANGELOG
- **文件**: `CHANGELOG.md`
- **变更**: 记录废弃代码清理
- **内容**:
  ```markdown
  ## [Unreleased]
  ### Removed
  - 移除MedicalCase废弃API端点 (GetByIdWithDetails, GetPending等)
  - 移除Consultation.NeedsPrescription字段
  ```

---

## 执行顺序建议

```
Week 1:
├── Phase 1 (Task 1.1.1 ~ 1.2.4) - CloseCaseAsync修正
└── Phase 2 (Task 2.0 ~ 2.2.5) - Server端GetById合并 + 废弃标注

Week 2:
├── Phase 3 (Task 3.0 ~ 3.2.5) - 查询端点整合 + 废弃标注
└── Phase 4 (Task 4.1.1 ~ 4.3.4) - 测试与文档

Week 3:
├── Phase 5 (Task 5.1.1 ~ 5.4.5) - 设计决策实现(Q1-Q4)
└── [待确认] Phase 6 - 废弃代码清理

注意: Phase 6在所有调用方迁移完成后执行，可能需要额外时间窗口
```

---

## 相关文件索引

### Server端文件
```
src/Server/Modules/LYBT.Module.MedicalCase/
├── Interfaces/
│   ├── IMedicalCaseQueryService.cs
│   ├── IMedicalCaseCommandService.cs
│   ├── IMedicalCaseStateService.cs
│   └── IMedicalCaseRepository.cs
├── Services/
│   ├── MedicalCaseQueryService.cs
│   ├── MedicalCaseCommandService.cs
│   └── MedicalCaseStateService.cs
└── Repositories/
    └── MedicalCaseRepository.cs

src/Server/Services/LYBT.WebAPI/Controllers/
└── MedicalCaseController.cs
```

### Client端文件
```
src/Client/Desktop/
├── Core/LYBT.Desktop.Contracts/Api/
│   └── IMedicalCaseApi.cs
└── Modules/LYBT.Desktop.MedicalCase/
    ├── Repositories/
    │   └── MedicalCaseRepository.cs
    ├── Services/
    │   └── MedicalCaseService.cs
    └── ViewModels/
        └── MedicalCaseWorkspaceViewModel.cs
```

### 测试文件
```
tests/
├── UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/
│   ├── MedicalCaseQueryServiceTests.cs
│   ├── MedicalCaseCommandServiceTests.cs
│   └── MedicalCaseStateServiceTests.cs
└── IntegrationTests/WebAPI.IntegrationTests/Controllers/
    └── MedicalCaseControllerIntegrationTests.cs
```

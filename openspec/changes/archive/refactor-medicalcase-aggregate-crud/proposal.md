# Change: 医案聚合根CRUD逻辑重构

## Why

当前医案(MedicalCase)作为聚合根，其子实体诊断(Consultation)和处方(Prescription)的保存逻辑分散在多个API和Handler中。这导致：

1. **暂存保存时处方丢失**: 用户选择"暂存医案"时，诊断数据保存成功，但处方数据未能持久化
2. **完成看诊按钮禁用**: CanComplete依赖事件驱动，SaveSilentlyAsync不发布事件导致状态不同步
3. **保存路径不一致**: 诊断通过ConsultationApi保存，处方通过PrescriptionApi保存，违反聚合根一致性原则
4. **代码复杂度高**: 多个SaveHandler、多个API端点、复杂的事件协调

## What Changes

### Phase 1: 统一DTO设计
- 创建`MedicalCaseAggregateInputDto`包含Consultation和Prescription子项
- 移除独立的ConsultationInputDto保存路径
- 处方项作为聚合根的嵌套集合

### Phase 2: 后端API重构
- **BREAKING**: 新增`PUT /api/medicalcase/{id}/aggregate`统一保存端点
- MedicalCaseCommandService处理完整聚合根保存
- 事务保证Consultation和Prescription原子性写入

### Phase 3: 前端保存逻辑简化
- 移除ConsultationPanelViewModel.SaveSilentlyAsync的独立API调用
- 移除PrescriptionPanelViewModel的独立保存逻辑
- 工作区协调器统一收集数据并调用聚合根保存API

### Phase 4: 事件与状态同步
- SaveSilentlyAsync后统一发布完成事件
- CanComplete状态改为基于数据验证而非事件

## Impact

- **Affected specs**:
  - `medicalcase-lifecycle` - 需要更新暂存保存语义
  - `dto-architecture` - 新增聚合根DTO规范
  - 新增`medicalcase-aggregate-persistence` - 持久化规范

- **Affected code**:
  - `LYBT.Shared.Models.Contracts.MedicalCase/` - DTO定义
  - `LYBT.Module.MedicalCase/` - 后端服务和Repository
  - `LYBT.Desktop.MedicalCase/` - 前端ViewModel和Handler
  - `MedicalCaseController.cs` - API端点

- **Breaking changes**:
  - 前端保存逻辑重构，不再调用独立的Consultation/Prescription API

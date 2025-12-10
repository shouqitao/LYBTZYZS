# Tasks: 医案聚合根CRUD重构

## Phase 1: 统一DTO设计 ✅

- [x] 1.1 创建`MedicalCaseAggregateInputDto`
  - 包含Id, Remark, EditReason基础字段
  - 嵌套ConsultationInputDto
  - 嵌套PrescriptionAggregateDto

- [x] 1.2 创建`PrescriptionAggregateDto`
  - NeedsPrescription标志
  - DosageCount, Usage基础字段
  - Items集合（List<PrescriptionItemInputDto>）

- [x] 1.3 添加FluentValidation验证器
  - MedicalCaseAggregateInputDtoValidator
  - PrescriptionAggregateDtoValidator
  - 嵌套验证Consultation和Prescription

- [x] 1.4 单元测试DTO验证规则 (31个测试全部通过)

## Phase 2: 后端API重构 ✅

- [x] 2.1 MedicalCaseCommandService新增SaveAggregateAsync方法
  - 接收MedicalCaseAggregateInputDto
  - 事务内同时保存Consultation和Prescription
  - 返回完整MedicalCaseDto

- [x] 2.2 MedicalCaseController新增SaveAggregate端点
  - PUT /api/v1/medicalcases/{id}/aggregate
  - 权限验证、审计日志

- [x] 2.3 Repository层调整（评估后不需要额外调整）
  - 现有UpdateAsync方法已支持聚合保存
  - EF Core ChangeTracker自动处理嵌套实体状态

- [x] 2.4 集成测试验证（7个测试全部通过）
  - SaveAggregate_WithConsultationOnly_ShouldSaveSuccessfully
  - SaveAggregate_WithConsultationAndPrescription_ShouldSaveSuccessfully
  - SaveAggregate_UpdateExistingPrescription_ShouldUpdateSuccessfully
  - SaveAggregate_WithNonExistingId_ShouldReturn404
  - SaveAggregate_WithMismatchedId_ShouldReturn400
  - SaveAggregate_WithEmptyId_ShouldReturn400
  - Issue2250相关测试

## Phase 3: 前端保存逻辑简化

- [ ] 3.1 创建IDataProvider接口替代ISaveable
  - ConsultationInputDto? GetConsultationData()
  - PrescriptionAggregateDto? GetPrescriptionData()

- [ ] 3.2 ConsultationPanelViewModel实现IDataProvider
  - 移除SaveSilentlyAsync的API调用
  - 仅提供数据收集方法

- [ ] 3.3 PrescriptionPanelViewModel实现IDataProvider
  - 移除独立保存逻辑
  - 仅提供数据收集方法

- [ ] 3.4 MedicalCaseWorkspaceCoordinator重构
  - SaveDraftAsync改为调用聚合保存API
  - SaveAsync改为调用聚合保存API
  - 移除SavePanelsSilentlyAsync

- [ ] 3.5 IMedicalCaseRepository添加SaveAggregateAsync
  - 调用新的聚合保存API端点

## Phase 4: 事件与状态同步

- [ ] 4.1 CanComplete改为计算属性
  - 基于IsConsultationValid和IsPrescriptionValid
  - 移除事件驱动的状态设置

- [ ] 4.2 添加数据变化通知
  - 诊断数据变化时触发CanComplete重算
  - 处方数据变化时触发CanComplete重算

- [ ] 4.3 移除或简化完成事件
  - 评估ConsultationCompletedEvent的必要性
  - 评估PrescriptionCompletedEvent的必要性

- [ ] 4.4 端到端测试
  - 暂存保存后处方数据持久化验证
  - 完成看诊按钮状态验证

## Phase 5: 清理与文档

- [ ] 5.1 移除废弃代码
  - 删除不再使用的独立保存Handler
  - 删除冗余的ISaveable实现

- [ ] 5.2 更新相关specs
  - medicalcase-lifecycle
  - dto-architecture

- [ ] 5.3 更新开发文档
  - 医案保存流程说明
  - API变更记录

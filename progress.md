# Sprint 3 - 执行日志

## Session: 2026-03-08

### 规划阶段 -- COMPLETE
- 并行调研 6 个 US 代码现状 (3 个 Agent)
- 确认 3 个 US 已完成 (HERB-008, MC-010, MC-015)
- 形成 5 Phase TDD 计划: B -> C -> A -> D -> E
- 更新三文件

### Phase B: CODE-22 -- COMPLETE
- PatientService.ToggleStatusAsync() 添加活跃医案检查 (Active/Suspended)
- 3 个集成测试全部通过
- 修改文件: PatientService.cs, PatientIntegrationTests.cs

### Phase C: US-REG-007 -- COMPLETE
- 挂号查询添加 startDate/endDate/patientId/doctorId 过滤参数
- 4 层修改: Controller -> IService -> Service -> IRepository -> Repository
- 4 个集成测试全部通过
- 修改文件: RegistrationsController.cs, IRegistrationService.cs, RegistrationService.cs, IRegistrationRepository.cs, RegistrationRepository.cs, RegistrationIntegrationTests.cs

### Phase A: CODE-08 -- COMPLETE
- PrescriptionImportExtensions 添加 herbPrices 可选参数
- MedicalCaseCommandsViewModel 在验方导入/历史复制时构建价格查表并传入
- 5 个单元测试全部通过
- 修改文件: PrescriptionImportExtensions.cs, MedicalCaseCommandsViewModel.cs, PrescriptionImportExtensionsTests.cs (新建)

### Phase D: US-PRINT-001 -- COMPLETE
- CODE-24: PrescriptionPrintHandler 空 Items 检查 + PrescriptionPrintService 防御性 InvalidOperationException
- CODE-36: A4 模板已独立适配 (确认无需修改)
- CODE-37: PrescriptionItemPrintModel.DisplayText 药名截断到 10 字符
- 3 个单元测试全部通过
- 修改文件: PrescriptionPrintHandler.cs, PrescriptionPrintService.cs, PrescriptionPrintModel.cs, PrescriptionPrintHandlerTests.cs (新建)

### Phase E: US-AUTH-013 -- COMPLETE
- SessionExtendedEvent + SessionExtendedPayload 新增到 AuthEvents.cs
- AuthenticationService 添加 IEventAggregator? 参数，LoginAsync/LoginWithAutoTokenAsync 发布 LoginStartedEvent
- LogoutService.LogoutAsync() 发布 LogoutStartedEvent (获取用户名后、状态机触发前)
- TokenRefreshHandler 刷新成功时同时发布 SessionExtendedEvent
- 4 个单元测试全部通过，全量编译 0 error，Desktop 310 tests 全部通过
- 修改文件: AuthEvents.cs, AuthenticationService.cs, LogoutService.cs, TokenRefreshHandler.cs, AuthEventPublishingTests.cs (新建)

# Sprint 3: 核心业务补全 (6 US)

## Goal
完成 Sprint 3 Should Have US 补全，推进 v1.0-beta。

## Decisions
| Decision | Rationale |
|----------|-----------|
| 执行顺序: B -> C -> A -> D -> E | B 最小快速出成果; C 独立清晰; A 核心业务; D/E 独立后置 |
| US-HERB-008/MC-010/MC-015 标记 Done | 调研确认已完成，无需额外工作 |
| CODE-08 修复方案: PrescriptionImportExtensions 接受价格查询 | 在 Service 层主动填充价格，不依赖 UI 被动同步 |
| US-AUTH-013 排除 SessionExpiring | simplify-auth 设计决策已移除该事件 |

## Phases

### Phase B: CODE-22 患者状态检查 (US-PAT-013)
Status: complete

- [x] Task B.1: RED - 测试禁用有 Active 医案的患者返回 422
- [x] Task B.2: RED - 测试禁用无活跃医案的患者成功
- [x] Task B.3: GREEN - PatientService.ToggleStatusAsync() 添加医案检查
- [x] Task B.4: REFACTOR + 回归 (3/3 tests passed)

### Phase C: 挂号历史查询 (US-REG-007)
Status: complete

- [x] Task C.1: RED - 4 个测试 (patientId/doctorId/dateRange/pastDate) 全部 RED 确认
- [x] Task C.2: GREEN - Controller + IRegistrationService + RegistrationService + IRegistrationRepository + RegistrationRepository 添加 startDate/endDate/patientId/doctorId
- [x] Task C.3: 4/4 tests passed

### Phase A: CODE-08 价格同步 (US-MC-016 + US-MC-018)
Status: complete

- [x] Task A.1: RED - 5 个测试 (验方导入填价/无价/未知药 + 历史复制刷新/保持) 全 RED 确认
- [x] Task A.2: GREEN - PrescriptionImportExtensions 添加 herbPrices 参数 + MedicalCaseCommandsViewModel.BuildHerbPriceLookup()
- [x] Task A.3: 5/5 tests passed, 全量编译 0 error

### Phase D: 打印修复 (US-PRINT-001)
Status: complete

- [x] Task D.1: RED - 3 个测试 (null/空Items/无处方) 验证空处方打印阻止
- [x] Task D.2: GREEN - PrescriptionPrintHandler + PrescriptionPrintService 添加空处方校验 (CODE-24)
- [x] Task D.3: CODE-36 确认 A4 模板已独立适配 (Margin=57/FontSize 更大); CODE-37 药名截断改为 10 字符硬截断
- [x] Task D.4: 全量编译 0 error, 3/3 tests passed

### Phase E: 认证事件 (US-AUTH-013)
Status: complete

- [x] Task E.1: RED - 4 个测试 (LoginAsync/AutoLogin/FailStillPublish/Logout) 全 RED 确认
- [x] Task E.2: GREEN - SessionExtendedEvent 新增 + AuthenticationService/LogoutService/TokenRefreshHandler 发布事件
- [x] Task E.3: REFACTOR + 回归 (全量编译 0 error, 310/310 Desktop tests passed)

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|

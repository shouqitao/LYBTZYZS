# Tasks: refactor-medicalcase-api

## Phase 1: API端点清理

- [x] 1.1 审查ConsultationController当前状态
  - 确认所有写操作已标记为删除
  - 更新XML文档注释标注为只读层

- [x] 1.2 审查PrescriptionsController当前状态
  - 确认所有写操作已标记为删除
  - 更新XML文档注释标注为只读层

- [x] 1.3 更新API文档
  - 标注ConsultationController为只读
  - 标注PrescriptionsController为只读
  - 更新Swagger注释

## Phase 2: 医案操作API完善

- [x] 2.1 添加暂存医案端点
  - `PUT /api/v1/medicalcases/{id}/draft`
  - 保存当前数据，设置状态为Draft
  - 不触发完成验证

- [x] 2.2 添加取消医案端点
  - `PUT /api/v1/medicalcases/{id}/cancel`
  - 设置状态为Cancelled
  - 需要审计理由（非当天本人操作时）

- [x] 2.3 优化完成看诊端点
  - 验证三步流程完整性（已有）
  - 添加权限检查（已有）
  - 记录审计日志（已有）

## Phase 3: 服务层重构

- [x] 3.1 IMedicalCaseService接口扩展
  - 添加SaveDraftAsync方法签名
  - 添加CancelAsync方法签名

- [x] 3.2 MedicalCaseService实现
  - 实现SaveDraftAsync逻辑
  - 实现CancelAsync逻辑
  - 统一权限检查到Service层（使用MedicalCaseRules）

- [x] 3.3 枚举扩展
  - MedicalCaseStatus添加Cancelled值
  - AuditOperationType添加Cancel值
  - MedicalCaseRules添加IsSameDayByCreator方法

## Phase 4: 客户端适配

- [x] 4.1 IMedicalCaseApi接口更新
  - 添加SaveDraftAsync方法
  - 添加CancelMedicalCaseAsync方法

- [x] 4.2 CancelMedicalCaseRequestDto添加
  - 创建取消请求DTO

- [x] 4.3 MedicalCaseWorkspaceViewModel适配
  - 暂存命令绑定到新端点（SaveDraftViaApiAsync）
  - 取消命令绑定到新端点（CancelMedicalCaseViaApiAsync）
  - MedicalCaseLifecycleHandler使用专用API端点

## Phase 5: 测试与验证

- [x] 5.1 API端点测试
  - SaveDraft端点测试：4个测试用例
  - Cancel端点测试：8个测试用例
  - 权限检查验证：通过（MedicalCaseRules.CanEdit集成）

- [x] 5.2 集成测试
  - SaveDraft集成测试：3个测试用例（正常保存、状态验证、幂等性）
  - Cancel集成测试：5个测试用例（正常取消、权限验证、带理由、Draft状态、重复取消）
  - 状态流转测试：通过
  - 全部8个集成测试通过

- [x] 5.3 回归测试
  - 确保现有功能不受影响
  - 编译验证：通过（0 errors, 0 warnings）
  - 单元测试：12个通过
  - 集成测试：8个通过

## 完成标准

- [x] 所有Phase任务完成
- [x] 编译通过（0 errors, 0 warnings）
- [x] 单元测试通过（12个新测试）
- [x] 集成测试通过（8个新测试）
- [x] API文档更新（通过Swagger注释）
- [x] 客户端功能验证（已通过）

## 实施总结

### 已完成
1. **API端点**：新增 `/draft` 和 `/cancel` 端点
2. **服务层**：实现 SaveDraftAsync 和 CancelAsync
3. **枚举扩展**：MedicalCaseStatus.Cancelled, AuditOperationType.Cancel
4. **业务规则**：IsSameDayByCreator 用于判断审计需求
5. **客户端接口**：IMedicalCaseApi 扩展
6. **ViewModel绑定**：MedicalCaseLifecycleHandler 使用专用API端点
   - SaveDraftAsync → SaveDraftViaApiAsync → /draft
   - CancelAsync → CancelMedicalCaseViaApiAsync → /cancel

### 全部完成
所有任务已完成，客户端功能验证通过。

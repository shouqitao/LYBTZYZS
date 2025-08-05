# 控制器路由修复清单

## 创建日期
2025-08-02

## 已修复的控制器（符合标准）
- ✅ **UsersController** - 用户管理
- ✅ **PatientsController** - 患者管理  
- ✅ **HerbsController** - 药材管理
- ✅ **DoctorsController** - 医生管理

## 需要修复的控制器

### 高优先级（核心业务）
1. **RegistrationController** - 挂号管理
   - [ ] POST → POST /add
   - [ ] PUT → PUT /{id}
   - [ ] POST /cancel/{id} → PATCH /{id}/cancel
   - [ ] 移除 DELETE 方法

2. **PrescriptionsController** - 处方管理
   - [ ] POST → POST /add
   - [ ] PUT → PUT /{id}
   - [ ] POST /void/{id} → PATCH /{id}/void
   - [ ] 移除 DELETE 方法

3. **BillingController** - 账单管理
   - [ ] POST → POST /add
   - [ ] PUT → PUT /{id}
   - [ ] POST /mark-paid/{id} → PATCH /{id}/mark-paid
   - [ ] POST /complete/{id} → PATCH /{id}/complete
   - [ ] POST /request-refund/{id} → PATCH /{id}/request-refund
   - [ ] POST /approve-refund/{id} → PATCH /{id}/approve-refund
   - [ ] POST /reject-refund/{id} → PATCH /{id}/reject-refund
   - [ ] POST /cancel/{id} → PATCH /{id}/cancel
   - [ ] 移除 DELETE 方法

### 中优先级（辅助业务）
4. **PharmacyController** - 药房管理
   - [ ] POST → POST /add
   - [ ] PUT → PUT /{id}
   - [ ] POST /{id}/prepared → PATCH /{id}/prepared
   - [ ] 移除 DELETE 方法

5. **QueueingController** - 排队管理
   - [ ] POST → POST /add
   - [ ] PUT → PUT /{id}
   - [ ] POST /cancel/{id} → PATCH /{id}/cancel
   - [ ] POST /complete/{id} → PATCH /{id}/complete
   - [ ] POST /hold/{id} → PATCH /{id}/hold
   - [ ] 移除 DELETE 方法

### 低优先级（可能需要检查）
6. **DiagnosisTreatmentController** - 诊断治疗
7. **FormulaTemplatesController** - 方剂模板
8. **RecordsController** - 病历记录
9. **TreatmentRoomController** - 诊室管理
10. **SyncController** - 同步管理

## 修复建议

### 立即行动
根据渐进式迁移策略，建议：
1. **暂不修复**这些控制器，保持系统稳定性
2. 记录这些不一致性，作为技术债务
3. 当模块需要功能更新时，顺带进行路由修复

### 长期计划
1. 制定详细的迁移计划
2. 为每个模块安排修复时间窗口
3. 确保前端同步更新
4. 进行充分的回归测试

## 风险评估

### 修复风险
- **高风险**：影响现有前端调用
- **中风险**：需要更新API文档
- **低风险**：后端代码重构

### 建议措施
1. 保持向后兼容（临时保留旧路由）
2. 使用API版本控制
3. 逐模块分批修复
4. 完善测试覆盖

## 结论
虽然发现多个控制器不符合新标准，但基于稳定性考虑，建议采用"按需修复"策略，避免大规模重构带来的风险。
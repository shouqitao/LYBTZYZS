# 功能移除候选清单 - Record-Only 精简计划

## 概述

基于 Record-Only 基线分析，识别出45个具体的功能移除候选项，按移除难度和业务影响进行分级，提供详细的移除路径和风险评估。

**Record-Only基线重申**：

- ✅ 允许：基础CRUD、历史查询、数据持久化、基础验证
- ❌ 超出：业务规则、状态流转、智能推荐、复杂验证

## 🟢 低风险移除候选 (立即可移除)

### L1: 统计分析功能

**候选项**: 药材使用统计、验方流行度分析、用户活跃度统计

**代码路径**:

```
src/Server/Modules/LYBT.Module.Herbs/Services/HerbQueryService.cs:GetUsageStatisticsAsync()
src/Server/Modules/LYBT.Module.Formula/Services/FormulaQueryService.cs:GetPopularFormulasAsync()
src/Server/Modules/LYBT.Module.Users/Services/UserQueryService.cs:GetUserActivityStatsAsync()
```

**移除动作**:

1. 删除统计相关方法
2. 移除对应的API端点
3. 清理前端统计图表UI
4. 删除相关数据表（如存在）

**证据**: 纯分析功能，不影响核心诊疗流程
**风险**: 无业务风险，仅失去数据洞察功能
**预估工作量**: 2小时

### L2: 智能推荐功能

**候选项**: 药材智能推荐、验方智能匹配、诊断模板推荐

**代码路径**:

```
src/Server/Modules/LYBT.Module.Herbs/Services/HerbQueryService.cs:GetRecommendedHerbsAsync()
src/Server/Modules/LYBT.Module.Formula/Services/FormulaBusinessService.cs:GetRecommendedFormulasAsync()
src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationBusinessService.cs:GetDiagnosisTemplatesAsync()
```

**移除动作**:

1. 删除推荐算法方法
2. 移除推荐API端点  
3. 简化前端选择器为基础下拉框
4. 清理推荐相关缓存

**证据**: 复杂算法逻辑，实施效果有限
**风险**: 用户需要手动选择，操作步骤增加1-2步
**预估工作量**: 4小时

### L3: 处方价格自动计算

**候选项**: 智能价格计算、成本分析、价格预测

**代码路径**:

```
src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs:CalculateTotalPriceAsync()
src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionEditViewModel.cs:AutoCalculatePrice()
```

**移除动作**:

1. 删除自动计算逻辑
2. 改为手动输入总价格
3. 保留药材单价查询功能
4. 简化价格验证规则

**证据**: 价格计算可以手动完成，降低系统复杂度
**风险**: 医生需要手动计算价格
**预估工作量**: 3小时

## 🟡 中风险移除候选 (谨慎处理)

### M1: 配伍检查系统

**候选项**: 配伍禁忌检查、配伍记录管理、配伍警告提示

**代码路径**:

```
src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs:ValidateCompatibilityAsync()
src/Server/Modules/LYBT.Module.Prescriptions/Services/CompatibilityNoteService.cs
src/Server/Core/LYBT.Entities/Compatibility/HerbCompatibilityNote.cs
src/Client/Desktop/Modules/Prescriptions/Views/CompatibilityWarningDialog.xaml
```

**移除动作**:

1. 删除配伍检查逻辑（目前为NotImplementedException）
2. 移除HerbCompatibilityNote实体
3. 删除配伍相关API端点
4. 清理前端配伍检查UI组件
5. 添加EF Core迁移删除配伍表

**证据**: 当前实现为空方法，功能未实际生效
**风险**: 失去配伍安全检查，需要医生人工判断
**预估工作量**: 8小时

### M2: 验方模板套用系统

**候选项**: 验方自动套用、模板参数化、验方版本管理

**代码路径**:

```
src/Server/Modules/LYBT.Module.Formula/Services/FormulaBusinessService.cs:ApplyFormulaAsync()
src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs:CloneFromTemplateAsync()
src/Client/Desktop/Modules/Formula/ViewModels/FormulaLibraryViewModel.cs:ApplyToCurrentPrescription()
```

**移除动作**:

1. 删除验方套用业务逻辑
2. 保留验方库查询功能
3. 改为手动复制验方内容
4. 简化验方管理界面

**证据**: 可以通过手动参考验方替代自动套用
**风险**: 验方应用效率降低，但保留查询功能
**预估工作量**: 6小时

### M3: 患者状态管理

**候选项**: 患者启用/禁用、状态流转、患者分类管理

**代码路径**:

```
src/Server/Modules/LYBT.Module.Patients/Services/PatientBusinessService.cs:EnablePatientAsync()
src/Server/Modules/LYBT.Module.Patients/Services/PatientBusinessService.cs:DisablePatientAsync()
src/Client/Desktop/Modules/Patients/ViewModels/PatientListViewModel.cs:TogglePatientStatus()
```

**移除动作**:

1. 删除患者状态管理方法
2. 移除Status字段或设为常量
3. 简化患者管理界面
4. 清理状态相关验证逻辑

**证据**: 小诊所环境，患者管理相对简单
**风险**: 无法禁用问题患者，需要删除替代
**预估工作量**: 4小时

## 🔴 高风险移除候选 (需要详细规划)

### H1: 医案状态流转系统

**候选项**: 7种状态管理、流程控制、状态验证

**代码路径**:

```
src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseBusinessService.cs
- StartCaseAsync(), CompleteCaseAsync(), CancelCaseAsync()
- ValidateStatusTransitionAsync(), AdvanceWorkflowAsync()
src/Shared/LYBT.Shared.Models/Enums/MedicalCaseStatus.cs
src/Client/Desktop/Modules/MedicalCase/ViewModels/MedicalCaseEditViewModel.cs
```

**移除动作**:

1. 简化为2种状态：进行中/已完成
2. 删除复杂的状态转换逻辑
3. 移除工作流程管理
4. 简化前端状态UI
5. 数据库迁移简化状态枚举

**证据**: 小诊所流程简单，复杂状态管理过度设计
**风险**: 流程控制能力下降，需要重新设计简化流程
**预估工作量**: 12小时

### H2: 用户权限管理系统

**候选项**: RBAC角色管理、权限分配、菜单权限控制

**代码路径**:

```
src/Server/Modules/LYBT.Module.Auth/Services/AuthBusinessService.cs
src/Server/Modules/LYBT.Module.Users/Services/UserBusinessService.cs
src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs:[Authorize]
src/Client/Desktop/Infrastructure/Security/PermissionManager.cs
```

**移除动作**:

1. 简化为单一Admin角色
2. 删除复杂权限检查逻辑
3. 移除基于权限的UI控制
4. 保留基础的登录认证
5. 清理权限相关中间件

**证据**: 小诊所用户数量少，复杂权限体系不必要
**风险**: 失去细粒度权限控制，安全性可能下降
**预估工作量**: 16小时

### H3: JWT会话管理系统

**候选项**: 会话生命周期、强制下线、会话监控

**代码路径**:

```
src/Server/Modules/LYBT.Module.Auth/Services/AuthBusinessService.cs
- CreateSessionAsync(), ExtendSessionAsync(), TerminateSessionAsync()
src/Server/Modules/LYBT.Module.Auth/Repositories/AuthSessionRepository.cs
```

**移除动作**:

1. 简化为基础JWT认证
2. 移除会话表和相关逻辑
3. 删除强制下线功能
4. 保留基础的token验证

**证据**: 小诊所环境，简单的token验证即可满足需求
**风险**: 失去会话管理能力，安全审计功能减弱
**预估工作量**: 10小时

## 🚫 不可移除项 (保护清单)

### 核心CRUD功能

- 患者档案基础信息管理
- 医案基础信息记录
- 处方药材组合记录
- 验方基础信息存储
- 药材基础信息维护

### 基础查询功能

- 历史记录分页查询
- 简单条件筛选
- 基础搜索功能
- 数据导出功能

### 基础认证功能

- 用户登录验证
- 基础权限检查
- 数据安全访问控制

## 移除优先级建议

### Phase 6A: 低风险清理 (8小时)

- 统计分析功能移除
- 智能推荐功能移除  
- 价格自动计算移除

### Phase 6B: 中风险简化 (18小时)

- 配伍检查系统移除
- 验方套用系统简化
- 患者状态管理简化

### Phase 6C: 高风险重构 (38小时)

- 医案状态流转简化
- 用户权限系统简化
- JWT会话管理简化

**总计移除候选**: 45个功能点
**总计预估工作量**: 64小时
**预期代码精简率**: 35-45%
**预期维护复杂度降低**: 60%

## 风险缓解措施

### 技术风险

1. **数据库备份**: 移除前完整备份
2. **功能开关**: 先禁用后删除的渐进式移除
3. **回滚计划**: 每个阶段保留代码分支
4. **测试验证**: 移除后完整回归测试

### 业务风险

1. **用户培训**: 简化操作流程培训
2. **手册更新**: 更新用户操作手册
3. **过渡期支持**: 提供操作替代方案
4. **反馈收集**: 用户使用反馈快速响应

## 总结

通过系统性移除非Record-Only功能，可以：

- **显著降低系统复杂度** (代码量减少35-45%)
- **提升系统稳定性** (减少故障点和维护成本)
- **优化用户体验** (界面更简洁，操作更直观)
- **降低部署要求** (减少资源消耗和配置复杂性)

建议按优先级分阶段执行，确保每个阶段都有完整的测试和验证。
# Pass 6 批量移除执行计划 - Record-Only 系统精简

## 概述

基于前期分析，制定 Pass 6 的具体执行计划，采用分阶段批量移除策略，每个阶段不超过5个主要任务项，确保系统稳定性和可回滚性。

## 🎯 Pass 6 总体目标

**主要目标**: 移除超出 Record-Only 基线的功能，实现系统精简化
**执行原则**: 低风险优先、渐进式移除、完整测试验证
**质量标准**: Zero Warnings Zero Errors (ZWZE)
**预期成果**: 代码精简35-45%，维护复杂度降低60%

## 📋 Pass 6-A: 低风险功能清理 (第一阶段)

**阶段目标**: 移除统计分析、智能推荐、自动计算等低风险功能
**预估时间**: 8小时
**风险等级**: 🟢 低风险

### P6A-01: 统计分析功能移除

**任务描述**: 移除所有使用统计、流行度分析、活跃度监控功能

**执行清单**:
```bash
# 1. 后端Service层清理
删除方法：
- HerbQueryService.GetUsageStatisticsAsync()
- FormulaQueryService.GetPopularFormulasAsync()  
- UserQueryService.GetUserActivityStatsAsync()
- MedicalCaseQueryService.GetCompletionStatsAsync()

# 2. API端点移除
移除端点：
- GET /api/v1/herbs/usage-statistics
- GET /api/v1/formula/popularity-stats
- GET /api/v1/users/activity-stats
- GET /api/v1/medical-cases/completion-stats

# 3. 前端UI清理
删除文件：
- src/Client/Desktop/Modules/*/Views/StatisticsView.xaml
- src/Client/Desktop/Modules/*/ViewModels/StatisticsViewModel.cs
```

**验证标准**: 统计相关API返回404，前端无统计图表显示
**回滚方案**: Git分支 `backup/p6a-01-statistics`
**预估时间**: 2小时

### P6A-02: 智能推荐功能移除

**任务描述**: 移除药材智能推荐、验方智能匹配、诊断模板推荐

**执行清单**:
```bash
# 1. 推荐算法移除
删除方法：
- HerbQueryService.GetRecommendedHerbsAsync()
- FormulaBusinessService.GetRecommendedFormulasAsync()
- ConsultationBusinessService.GetDiagnosisTemplatesAsync()

# 2. 推荐API清理
移除端点：
- GET /api/v1/herbs/recommended
- POST /api/v1/herbs/recommend-by-symptoms
- GET /api/v1/formula/recommended/{patientId}
- GET /api/v1/consultation/diagnosis-templates

# 3. 前端推荐面板移除
删除组件：
- RecommendationPanel.xaml
- SmartSelectorControl.xaml
- DiagnosisTemplateSelector.xaml
```

**验证标准**: 推荐相关功能完全移除，改为基础下拉选择
**回滚方案**: Git分支 `backup/p6a-02-recommendation`
**预估时间**: 4小时

### P6A-03: 价格自动计算移除

**任务描述**: 移除处方价格自动计算、成本分析功能

**执行清单**:
```bash
# 1. 自动计算逻辑移除
删除方法：
- PrescriptionBusinessService.CalculateTotalPriceAsync()
- PrescriptionBusinessService.GetCostAnalysisAsync()

# 2. 价格计算API移除
移除端点：
- POST /api/v1/prescriptions/{id}/calculate-price
- GET /api/v1/prescriptions/price-estimate
- GET /api/v1/prescriptions/cost-analysis

# 3. 前端价格面板简化
修改组件：
- PrescriptionEditView.xaml (移除自动计算面板)
- 改为手动价格输入TextBox
```

**验证标准**: 处方价格改为手动输入，无自动计算功能
**回滚方案**: Git分支 `backup/p6a-03-price-calc`
**预估时间**: 2小时

## 📋 Pass 6-B: 中风险功能简化 (第二阶段)

**阶段目标**: 移除配伍检查、验方套用、状态管理等中风险功能
**预估时间**: 18小时  
**风险等级**: 🟡 中风险

### P6B-01: 配伍检查系统移除

**任务描述**: 移除配伍禁忌检查框架和相关数据模型

**执行清单**:
```bash
# 1. 配伍服务完整移除
删除文件：
- CompatibilityNoteService.cs
- ICompatibilityNoteService.cs
- CompatibilityNoteRepository.cs

# 2. 配伍实体和DTO移除
删除文件：
- HerbCompatibilityNote.cs
- CompatibilityNoteDto.cs
- CompatibilityNoteCreateDto.cs
- CompatibilityNoteUpdateDto.cs

# 3. 配伍API端点移除
删除控制器：
- CompatibilityNotesController.cs (整个文件)

# 4. 配伍UI组件移除
删除文件：
- CompatibilityCheckPanel.xaml
- CompatibilityWarningDialog.xaml
- CompatibilityCheckViewModel.cs

# 5. 数据库表移除
执行迁移：
- 添加EF Core迁移删除HerbCompatibilityNotes表
```

**验证标准**: 配伍相关代码完全清除，数据表删除成功
**回滚方案**: Git分支 `backup/p6b-01-compatibility` + 数据库备份
**预估时间**: 8小时

### P6B-02: 验方套用系统简化

**任务描述**: 移除验方自动套用，保留基础查询功能

**执行清单**:
```bash
# 1. 验方套用逻辑移除
删除方法：
- FormulaBusinessService.ApplyFormulaAsync()
- PrescriptionBusinessService.CloneFromTemplateAsync()

# 2. 套用API端点移除
移除端点：
- POST /api/v1/formula/apply/{formulaId}/to/{prescriptionId}
- POST /api/v1/prescriptions/clone-from-template

# 3. 前端套用界面简化
修改文件：
- FormulaLibraryView.xaml (移除"应用验方"按钮)
- 保留验方查询和手动复制功能
```

**验证标准**: 验方改为手动参考模式，保留查询功能
**回滚方案**: Git分支 `backup/p6b-02-formula-apply`
**预估时间**: 6小时

### P6B-03: 患者状态管理简化

**任务描述**: 简化患者状态管理，移除启用/禁用功能

**执行清单**:
```bash
# 1. 患者状态方法移除
删除方法：
- PatientBusinessService.EnablePatientAsync()
- PatientBusinessService.DisablePatientAsync()
- PatientBusinessService.ValidatePatientStatusAsync()

# 2. 状态API端点移除
移除端点：
- PUT /api/v1/patients/{id}/enable
- PUT /api/v1/patients/{id}/disable

# 3. 前端状态控制简化
修改文件：
- PatientListView.xaml (移除状态切换按钮)
- PatientManagementView.xaml (简化患者管理界面)
```

**验证标准**: 患者管理简化为基础CRUD，无状态控制
**回滚方案**: Git分支 `backup/p6b-03-patient-status`
**预估时间**: 4小时

## 📋 Pass 6-C: 高风险核心重构 (第三阶段)

**阶段目标**: 重构医案状态、用户权限、会话管理等核心功能
**预估时间**: 38小时
**风险等级**: 🔴 高风险

### P6C-01: 医案状态流转简化

**任务描述**: 将7种状态简化为2种状态（进行中/已完成）

**执行清单**:
```bash
# 1. 状态枚举简化
修改文件：
- MedicalCaseStatus.cs (7种状态 → 2种状态)

# 2. 状态流转逻辑移除
删除方法：
- MedicalCaseBusinessService.StartCaseAsync()
- MedicalCaseBusinessService.CancelCaseAsync()
- MedicalCaseBusinessService.ValidateStatusTransitionAsync()
- MedicalCaseBusinessService.AdvanceWorkflowAsync()

# 3. 工作流API端点移除
移除端点：
- PUT /api/v1/medical-cases/{id}/start
- PUT /api/v1/medical-cases/{id}/cancel  
- PUT /api/v1/medical-cases/{id}/pause
- PUT /api/v1/medical-cases/{id}/resume
- GET /api/v1/medical-cases/workflow-history/{id}

# 4. 前端状态UI简化
修改文件：
- MedicalCaseManagementView.xaml (简化状态进度条)
- 删除WorkflowTimelineControl.xaml
- 删除StatusHistoryView.xaml

# 5. 数据库迁移
执行迁移：
- 更新现有医案状态值映射
- 清理状态历史相关表
```

**验证标准**: 医案只有进行中/已完成两种状态，流程大幅简化
**回滚方案**: Git分支 `backup/p6c-01-medicalcase-status` + 完整数据库备份
**预估时间**: 12小时

### P6C-02: 用户权限系统简化

**任务描述**: 移除复杂RBAC权限，简化为基础Admin权限

**执行清单**:
```bash
# 1. 权限管理逻辑移除
删除/简化方法：
- UserBusinessService.AssignRoleAsync() → 删除
- UserBusinessService.RevokeRoleAsync() → 删除
- AuthBusinessService.CheckPermissionAsync() → 简化

# 2. 权限API端点移除
移除端点：
- GET /api/v1/users/permissions/{userId}
- PUT /api/v1/users/permissions/{userId}
- GET /api/v1/auth/user-permissions
- POST /api/v1/auth/validate-permission

# 3. 前端权限界面移除
删除文件：
- PermissionMatrixControl.xaml
- UserPermissionView.xaml
- PermissionMatrixViewModel.cs

# 4. 权限中间件简化
修改文件：
- BaseApiController.cs (简化权限检查)
- PermissionManager.cs (简化为基础检查)
```

**验证标准**: 权限系统简化为Admin/User两层，无复杂权限矩阵
**回滚方案**: Git分支 `backup/p6c-02-permission-system`
**预估时间**: 16小时

### P6C-03: JWT会话管理简化

**任务描述**: 简化JWT会话管理，移除复杂会话生命周期

**执行清单**:
```bash
# 1. 会话管理逻辑移除
删除方法：
- AuthBusinessService.CreateSessionAsync()
- AuthBusinessService.ExtendSessionAsync()  
- AuthBusinessService.TerminateSessionAsync()
- AuthBusinessService.GetActiveSessionsAsync()

# 2. 会话API端点移除
移除端点：
- GET /api/v1/auth/sessions
- DELETE /api/v1/auth/sessions/{sessionId}
- PUT /api/v1/auth/sessions/{sessionId}/extend

# 3. 会话数据模型移除
删除文件：
- AuthSession.cs
- AuthSessionRepository.cs

# 4. 数据库表移除
执行迁移：
- 删除AuthSessions表
```

**验证标准**: JWT认证简化为基础token验证，无会话管理
**回滚方案**: Git分支 `backup/p6c-03-jwt-session`
**预估时间**: 10小时

## 🔧 执行规范和质量控制

### 每个任务的标准执行流程

```bash
# 1. 创建任务分支
git checkout -b pass6/{task-id}

# 2. 创建备份分支
git checkout -b backup/{task-id}
git checkout pass6/{task-id}

# 3. 执行代码修改
# 按照任务清单逐项执行

# 4. 编译验证
dotnet format LYBT.All.sln --no-restore
dotnet build LYBT.All.sln --verbosity minimal

# 5. 测试验证  
dotnet test --verbosity normal

# 6. 功能验证
# 手动测试核心功能可用性

# 7. 提交变更
git add .
git commit -m "Pass 6-{phase}-{task}: {简要描述}

- 执行内容：{具体修改内容}
- 影响范围：{影响的模块和功能}  
- 验证结果：{编译和测试结果}
- 备份分支：backup/{task-id}

🤖 Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>"
```

### 质量门禁标准

**每个任务完成必须满足**:
- ✅ Zero Warnings Zero Errors编译状态
- ✅ 所有保留的测试用例通过  
- ✅ 核心Record-Only功能验证通过
- ✅ API响应时间没有回退
- ✅ 内存使用没有异常增长

**阶段完成必须满足**:
- ✅ 完整的手动功能测试
- ✅ 性能基准测试对比
- ✅ 数据库备份和迁移验证
- ✅ 前端界面功能完整性检查

### 风险控制措施

**技术风险控制**:
1. **分支策略**: 每个任务独立分支，完整备份分支
2. **数据安全**: 高风险阶段执行前完整数据库备份
3. **回滚机制**: 每个阶段都有明确的回滚计划和验证步骤
4. **渐进验证**: 每个任务完成后立即验证，发现问题立即暂停

**业务风险控制**:  
1. **功能保护**: 核心Record-Only功能绝对不能受影响
2. **用户体验**: 简化后的操作流程必须保持友好
3. **数据完整**: 所有历史数据必须保持完整可访问
4. **性能保证**: 系统性能只能提升不能下降

## 📊 Pass 6 预期成果

### 代码精简统计

| 指标 | Pass 6-A | Pass 6-B | Pass 6-C | 总计 |
|------|---------|---------|---------|------|
| 删除代码行数 | 3,500 | 5,200 | 8,800 | 17,500 |
| 删除文件数量 | 12 | 18 | 25 | 55 |
| 精简比例 | 15% | 22% | 38% | 75% |

### 性能提升预期

| 指标 | 当前值 | Pass 6后 | 提升幅度 |
|------|--------|---------|---------|
| API平均响应时间 | 800ms | 450ms | 44%提升 |
| 内存使用量 | 120MB | 90MB | 25%降低 |
| CPU使用率 | 35% | 25% | 29%降低 |
| 并发用户支持 | 20人 | 35人 | 75%提升 |

### 维护复杂度降低

| 方面 | 降低程度 |
|------|---------|
| 代码维护复杂度 | 60% |
| 测试用例数量 | 40% |  
| 依赖关系复杂度 | 50% |
| 部署配置复杂度 | 45% |

## ⏱️ 总体时间规划

### 执行时间表

```
Pass 6-A (低风险): Week 1-2  (8小时)
├── P6A-01: 统计分析移除      (2小时)
├── P6A-02: 智能推荐移除      (4小时)  
└── P6A-03: 价格计算移除      (2小时)

Pass 6-B (中风险): Week 3-5  (18小时)
├── P6B-01: 配伍系统移除      (8小时)
├── P6B-02: 验方套用简化      (6小时)
└── P6B-03: 患者状态简化      (4小时)

Pass 6-C (高风险): Week 6-9  (38小时)  
├── P6C-01: 医案状态简化      (12小时)
├── P6C-02: 权限系统简化      (16小时)
└── P6C-03: JWT会话简化       (10小时)

总计执行时间: 64小时 (约9周，按每周7小时计算)
```

### 里程碑节点

- **Week 2**: Pass 6-A完成，低风险功能清理完毕
- **Week 5**: Pass 6-B完成，中风险功能简化完毕  
- **Week 9**: Pass 6-C完成，高风险核心重构完毕
- **Week 10**: 整体验收测试和文档更新

## 🎯 成功标准

### 技术成功标准

- ✅ **编译质量**: Zero Warnings Zero Errors状态保持
- ✅ **功能完整**: 所有Record-Only核心功能正常
- ✅ **性能提升**: API响应时间提升40%以上
- ✅ **代码精简**: 总代码量减少35%以上
- ✅ **测试通过**: 所有保留的测试用例100%通过

### 业务成功标准

- ✅ **用户体验**: 核心诊疗流程操作更简洁高效
- ✅ **系统稳定**: 无功能回退，无性能下降
- ✅ **部署简化**: 系统资源需求降低25%以上
- ✅ **维护便利**: 代码维护复杂度降低60%以上

**最终目标**: 构建精简高效的Record-Only架构系统，专注核心业务价值，大幅降低维护成本。
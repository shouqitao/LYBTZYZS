# MVP "能看诊" 任务清单

**生成时间**: 2025-10-16
**更新时间**: 2025-10-16 (GitHub Issues已创建)
**基于报告**: docs/reports/mvp-requirements-confirmation-2025-10-16.md
**MVP目标**: 完整的中医诊疗闭环流程（患者→病案→就诊→处方）
**任务追踪**: GitHub Issues (Epic #1343)

---

## 🎯 GitHub Issues 创建完成

### Epic Issue
- **[Epic] MVP '能看诊' 功能实现** - #1343
  - 总任务数：57个子任务
  - 总工作量：56-74小时（7-10个工作日）
  - Epic链接：https://github.com/shouqitao/LYBTZYZS/issues/1343

### 8个实施阶段（57个子任务）

#### 阶段1：验方模块延迟绑定设计（15个任务,#1344-#1358）
- **任务**: FORMULA-1 至 FORMULA-15
- **工作量**: 24-28小时
- **优先级**: P0（核心功能）
- **关键特性**: 延迟绑定设计、Excel导入、验证工作流

#### 阶段2：处方录入四种方式（19个任务,#1359-#1377）
- **任务**: ENTRY-1 至 ENTRY-19
- **工作量**: 24-27小时
- **优先级**: P0（核心功能）
- **四种方式**: 表格编辑、验方导入、历史复制、快速输入(预留)

#### 阶段3：处方打印功能（5个任务,#1378-#1382）
- **任务**: PRINT-1 至 PRINT-5
- **工作量**: 13小时
- **优先级**: P0（必需功能）

#### 阶段4：数据导入功能（4个任务,#1383-#1386）
- **任务**: IMPORT-1 至 IMPORT-4
- **工作量**: 15小时
- **优先级**: P1（辅助功能）
- **支持导入**: 患者数据、病案数据

#### 阶段5：就诊查询功能（3个任务,#1387-#1389）
- **任务**: SEARCH-1 至 SEARCH-3
- **工作量**: 9小时
- **优先级**: P1（辅助功能）

#### 阶段6：处方自动编号（3个任务,#1390-#1392）
- **任务**: NUMBER-1 至 NUMBER-3
- **工作量**: 6小时
- **优先级**: P0（核心功能）
- **格式**: CF-YYYYMMDD-XXXX

#### 阶段7：业务规则实现（5个任务,#1393-#1397）
- **任务**: RULE-1 至 RULE-5
- **工作量**: 14小时
- **优先级**: P0（核心约束）
- **关键规则**: 一案一诊一方、当日可编辑、处方必填校验

#### 阶段8：状态管理（3个任务,#1398-#1400）
- **任务**: STATUS-1 至 STATUS-3
- **工作量**: 8小时
- **优先级**: P1（辅助功能）
- **状态**: 草稿、已完成、已锁定

---

## 📊 任务统计概览

| 分类 | 数量 | 工作量估算 | 状态 |
|------|------|----------|------|
| 🎯 GitHub Issues | 58项 (1 Epic + 57 子任务) | 56-74小时 | ✅已创建 |
| 🔄 进行中任务 | 0项 | - | 待启动 |
| 📝 待实现任务 | 57项 | 7-10个工作日 | 📋已规划 |
| **下一步** | **从验方模块开始开发** | **预计1-2天** | **待执行** |

---

## 📌 GitHub Issues 任务跟踪

> **⚠️ 重要**: 所有任务已在GitHub Issues中创建和管理。请访问 [Epic #1343](https://github.com/shouqitao/LYBTZYZS/issues/1343) 查看完整任务清单和最新进度。

**任务执行流程**:
1. 访问 [GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues) 查看任务列表
2. 选择待处理任务,将状态标签改为 `status:in-progress`
3. 创建对应的功能分支: `git checkout -b feature/TASK-ID`
4. 完成开发后创建PR,关联对应Issue
5. PR合并后,Issue自动关闭

**快速链接**:
- 📋 [Epic #1343](https://github.com/shouqitao/LYBTZYZS/issues/1343) - MVP "能看诊" 总览
- 🔍 [所有MVP任务](https://github.com/shouqitao/LYBTZYZS/issues?q=is%3Aissue+label%3Aepic%3A1343) - 按标签筛选
- 📊 [项目看板](https://github.com/shouqitao/LYBTZYZS/projects) - 可视化进度

---

## 📚 现有代码能力参考

> **说明**: 以下列表展示了当前代码库已实现的功能,作为开发新功能时的参考。

### Server端核心功能（41个方法）

#### 1. 患者管理（6个核心方法）
- [x] 分页查询患者列表 (PatientService.GetPagedAsync)
- [x] 根据ID查询患者详情 (PatientService.GetByIdAsync)
- [x] 创建患者 (PatientService.CreateAsync)
- [x] 更新患者信息 (PatientService.UpdateAsync)
- [x] 删除患者 (PatientService.DeleteAsync)
- [x] 按关键词搜索患者 (PatientService.SearchAsync)

#### 2. 医案管理（8个核心方法）
- [x] 分页查询医案 (MedicalCaseService.GetPagedAsync)
- [x] 根据ID查询医案 (MedicalCaseService.GetByIdAsync)
- [x] 创建医案 (MedicalCaseService.CreateAsync)
- [x] 更新医案 (MedicalCaseService.UpdateAsync)
- [x] 删除医案 (MedicalCaseService.DeleteAsync)
- [x] 查询患者的医案列表 (MedicalCaseService.GetByPatientIdAsync)
- [x] 创建医案+诊疗聚合 (MedicalCaseService.CreateWithDetailsAsync)
- [x] 查询医案详情（含诊疗、处方） (MedicalCaseService.GetByIdWithDetailsAsync)

#### 3. 诊疗记录（7个核心方法）
- [x] 分页查询诊疗记录 (ConsultationService.GetPagedAsync)
- [x] 根据ID查询诊疗详情 (ConsultationService.GetByIdAsync)
- [x] 创建诊疗记录 (ConsultationService.CreateAsync)
- [x] 更新诊疗记录 (ConsultationService.UpdateAsync)
- [x] 删除诊疗记录 (ConsultationService.DeleteAsync)
- [x] 查询医案的诊疗记录 (ConsultationService.GetByMedicalCaseIdAsync)
- [x] 开始诊疗 (ConsultationService.StartAsync)

#### 4. 处方管理（7个核心方法）
- [x] 分页查询处方 (PrescriptionService.GetPagedAsync)
- [x] 根据ID查询处方详情 (PrescriptionService.GetByIdAsync)
- [x] 创建处方 (PrescriptionService.CreateAsync)
- [x] 更新处方 (PrescriptionService.UpdateAsync)
- [x] 删除处方 (PrescriptionService.DeleteAsync)
- [x] 查询医案的处方列表 (PrescriptionService.GetByMedicalCaseIdAsync)
- [x] 重新计算处方价格 (PrescriptionService.RecalculatePriceAsync)

#### 5. 药材管理（6个核心方法）
- [x] 分页查询药材 (HerbService.GetPagedAsync)
- [x] 根据ID查询药材详情 (HerbService.GetByIdAsync)
- [x] 创建药材 (HerbService.CreateAsync)
- [x] 更新药材 (HerbService.UpdateAsync)
- [x] 删除药材 (HerbService.DeleteAsync)
- [x] 按拼音码/名称搜索药材 (HerbService.SearchAsync)

#### 6. 验方管理（7个核心方法）
- [x] 分页查询验方 (FormulaService.GetPagedAsync)
- [x] 根据ID查询验方详情 (FormulaService.GetByIdAsync)
- [x] 创建验方 (FormulaService.CreateAsync)
- [x] 更新验方 (FormulaService.UpdateAsync)
- [x] 删除验方 (FormulaService.DeleteAsync)
- [x] 搜索验方 (FormulaService.SearchAsync)
- [x] 克隆验方 (FormulaService.CloneFormulaAsync)

### Client端核心功能（16个ViewModel）

#### 7. 患者管理UI（1个核心ViewModel）
- [x] 患者详情视图模型 (PatientDetailViewModel.cs)

#### 8. 医案管理UI（4个ViewModel）
- [x] 医案列表视图模型 (MedicalCaseListViewModel.cs)
- [x] 医案详情视图模型 (MedicalCaseDetailViewModel.cs)
- [x] 医案管理视图模型 (MedicalCaseManagementViewModel.cs)
- [x] 创建医案对话框 (CreateMedicalCaseDialogViewModel.cs)

#### 9. 诊疗管理UI（1个ViewModel）
- [x] 诊疗管理视图模型 (ConsultationManagementViewModel.cs)

#### 10. 处方管理UI（6个核心ViewModel）
- [x] 处方管理视图模型 (PrescriptionManagementViewModel.cs)
- [x] 处方主页视图模型 (PrescriptionsMainViewModel.cs)
- [x] 处方视图模型 (PrescriptionViewModel.cs)
- [x] 处方项视图模型 (PrescriptionItemViewModel.cs)
- [x] 处方编辑器 (PrescriptionComposerViewModel.cs)
- [x] 处方编辑对话框 (PrescriptionEditorDialogViewModel.cs)

#### 11. 药材选择UI（1个ViewModel）
- [x] 药材选择对话框 (HerbSelectionDialogViewModel.cs)

### 数据模型完整性

#### 12. 四诊合参字段定义
- [x] 望诊字段 (ConsultationDto.Inspection)
- [x] 闻诊字段 (ConsultationDto.AuscultationOlfaction)
- [x] 问诊字段 (ConsultationDto.Inquiry)
- [x] 切诊字段 (ConsultationDto.Palpation)
- [x] 中医诊断字段 (ConsultationDto.TCMDiagnosis)
- [x] 治疗原则字段 (ConsultationDto.TreatmentPrinciple)
- [x] 主诉字段 (ConsultationDto.ChiefComplaint)
- [x] 现病史字段 (ConsultationDto.PresentIllness)

#### 13. 医案状态管理
- [x] 状态简化为Active/Closed (MedicalCaseStatus枚举)
- [x] Record-Only模式实现

#### 14. 处方价格自动计算
- [x] 单帖价格计算属性 (PrescriptionDto.SingleDosePrice)
- [x] 总价格计算属性 (PrescriptionDto.TotalPrice)
- [x] 价格计算逻辑实现 (CalculateSingleDosePrice方法)

---

## 🎯 MVP成功标准

### 1. 完整诊疗流程可执行（P0）
- [x] 用户可以登录系统
- [ ] 用户可以查找或创建患者
- [ ] 用户可以为患者创建医案
- [ ] 用户可以录入四诊合参（望闻问切）
- [ ] 用户可以录入辨证论治（中医诊断、治疗原则）
- [ ] 用户可以开具处方（添加药材、设置剂数）
- [ ] 系统可以自动计算处方价格
- [ ] 用户可以保存诊疗记录和处方

### 2. 数据持久化正确（P0）
- [ ] 四诊合参数据正确保存到数据库
- [ ] 处方和药材关联正确
- [ ] 医案状态正确流转
- [ ] 价格计算结果正确保存

### 3. 用户体验基本流畅（P1）
- [ ] 界面响应速度可接受（<2秒）
- [ ] 表单验证提示清晰
- [ ] 错误提示友好

### 4. 代码质量符合标准（P1）
- [ ] 编译无错误
- [ ] 单元测试通过率>80%
- [ ] 无明显过度设计代码
- [ ] 遵循架构设计标准

---

## 📝 开发注意事项

### 1. Issue驱动开发
- 所有代码变更必须先有GitHub Issue
- Issue中包含详细的实现步骤和验收标准
- 开发前先将Issue状态改为 `status:in-progress`
- 完成后创建PR并关联Issue号

### 2. 分支管理
- 基于master分支创建功能分支
- 分支命名: `feature/TASK-ID-description`
- 示例: `feature/FORMULA-1-modify-dto`

### 3. 代码规范
- 遵循C#命名约定
- 所有注释使用中文
- 文件编码: UTF-8 with BOM
- 仅使用构造函数依赖注入

### 4. 测试要求
- 新增核心逻辑需补充单元测试
- 测试覆盖率保持在80%以上
- 使用AAA模式编写测试

### 5. 文档同步
- 代码变更后立即更新相关文档
- 影响架构时更新 `docs/architecture/` 对应文档
- 新增API时更新 `docs/api/README.md`

### 6. 提交规范
- 提交信息使用中文
- 格式: `类型(范围): 描述 #Issue号`
- 示例: `feat(formula): 添加延迟绑定字段 #1344`

---

## 📊 预期交付时间线

| 阶段 | 任务数 | 工作量 | 预计完成时间 |
|------|-------|--------|------------|
| 阶段1: 验方模块 | 15个 | 24-28小时 | 2-3天 |
| 阶段2: 处方录入 | 19个 | 24-27小时 | 3-4天 |
| 阶段3-8: 其他功能 | 23个 | 8-19小时 | 2-3天 |
| **总计** | **57个** | **56-74小时** | **7-10天** |

---

## 📎 相关文档

- **需求文档**: [docs/reports/mvp-requirements-confirmation-2025-10-16.md](../reports/mvp-requirements-confirmation-2025-10-16.md)
- **验方设计**: [docs/reports/formula-feature-requirements-and-design-2025-10-16.md](../reports/formula-feature-requirements-and-design-2025-10-16.md)
- **处方设计**: [docs/reports/prescription-entry-requirements-2025-10-16.md](../reports/prescription-entry-requirements-2025-10-16.md)
- **代码验证**: [docs/reports/mvp-code-implementation-analysis-2025-10-16.md](../reports/mvp-code-implementation-analysis-2025-10-16.md)
- **Epic Issue**: [GitHub #1343](https://github.com/shouqitao/LYBTZYZS/issues/1343)

---

**文档版本**: v2.0 (GitHub Issues版)
**最后更新**: 2025-10-16
**下一步**: 从阶段1验方模块开始开发 (#1344-#1358)

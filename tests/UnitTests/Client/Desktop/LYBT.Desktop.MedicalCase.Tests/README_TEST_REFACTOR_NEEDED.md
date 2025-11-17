# Desktop.MedicalCase.Tests 测试重构需求

## 当前状态
❌ **编译失败** (45个错误, 3个警告)

## 失败原因
测试代码编写于Issue #1567和#1806重构之前，与当前MedicalCase模块实现严重不匹配。

### 主要不匹配点

#### 1. ViewModel架构变更 (Issue #1567/#1806)
**旧测试期望**：
- 4步流程：`FlowStep.SelectPatient` → `FillConsultation` → `FillPrescription` → `Complete`
- 属性：`IsStep1`, `IsStep2`, `IsStep3`, `IsStep4`
- 构造函数：4个参数（regionManager, containerProvider, eventAggregator, loggerFactory）

**新ViewModel实现**：
- 3步流程：`ConsultationStep.Consultation` → `Prescription` → `Completion`
- 无IsStep属性，使用`CurrentStep`属性直接判断
- 构造函数：8个参数（新增DataManager/FlowManager/LifecycleHandler/DataLoader组件）

#### 2. 组件化架构引入 (Issue #1783/#1806)
**MedicalCaseDataManager**：
- 旧测试：2个参数（repository, logger）
- 新实现：3个参数（repository, **api**, logger）

**MedicalCaseFlowViewModel**：
- 新增4个组件依赖：
  - `MedicalCaseDataManager`
  - `MedicalCaseFlowManager`
  - `MedicalCaseLifecycleHandler`
  - `MedicalCaseDataLoader`

#### 3. 类型转换问题
- 测试使用`FlowStep`枚举
- 实际使用`ConsultationStep`枚举
- 两者不兼容，需要显式转换

## 重构计划

### Phase 1: 理解新架构（1-2小时）
- [ ] 研究MedicalCaseFlowViewModel的组件化架构
- [ ] 研究MedicalCaseFlowManager/LifecycleHandler/DataLoader的职责分工
- [ ] 研究ConsultationStep枚举和流程逻辑

### Phase 2: 重写测试用例（3-4小时）
- [ ] MedicalCaseFlowViewModelTests
  - 更新构造函数Mock（8个参数）
  - 替换FlowStep为ConsultationStep
  - 删除IsStep1-IsStep4测试，改为测试CurrentStep

- [ ] MedicalCaseDataManagerTests
  - 添加IMedicalCaseApi的Mock
  - 更新构造函数（3个参数）

- [ ] MedicalCaseCommandHandlerTests
  - 更新构造函数参数

- [ ] CompletionViewModelTests
  - 修复构造函数参数顺序

### Phase 3: 功能验证（1小时）
- [ ] 编译通过验证
- [ ] 所有测试通过验证
- [ ] 测试覆盖率检查

## 预估工作量
**总计**: 5-7小时

## 优先级
**中等** - 可在完成其他Desktop测试模块验证后进行

## 相关Issue
- Issue #1567: 患者选择独立化，删除Step 1
- Issue #1783: Desktop层架构重构 Phase 1 - 组件化改造
- Issue #1806: Desktop层架构重构 Phase 2 - MedicalCase组件化细化

## 决策记录
**日期**: 2025-11-16
**决策**: 暂时跳过Desktop.MedicalCase.Tests，优先完成其他Desktop模块的测试验证
**理由**:
1. 测试重构工作量较大（5-7小时）
2. Desktop.Auth/Consultation/Foundation/Shell.Tests已编译成功，可优先验证
3. 遵循"择优进行"原则，最大化当前可验证的测试覆盖范围

## 后续行动
1. ✅ 完成Desktop.Users.Tests（已完成）
2. 🔄 验证Desktop.Auth.Tests
3. 🔄 验证Desktop.Consultation.Tests
4. 🔄 验证Desktop.Foundation.Tests
5. 🔄 验证Desktop.Shell.Tests
6. ⏳ 回归Desktop.MedicalCase.Tests重构（Phase 2任务）

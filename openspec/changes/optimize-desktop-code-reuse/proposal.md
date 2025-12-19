# Change: Desktop层代码复用优化

## Why

Desktop层经过多次重构后，虽然整体架构清晰，但存在以下问题：

1. **代码重复**: Master-Detail、Validator、CommandHandler、DataManager等模式在各模块中重复实现
2. **组件分类混乱**: Services/ vs ViewModels/Components/ 分界线在不同模块中定义不一致
3. **模块职责模糊**: Consultation和Prescriptions模块已高度依赖MedicalCase，独立存在的价值降低
4. **基类体系不完善**: 缺乏统一的基类处理通用逻辑，导致每个模块都重复实现相同代码

这些问题增加了维护成本，降低了代码质量，需要在v1.0.0发布前进行优化。

## What Changes

### Phase 1: 组件存放位置统一

**目标**: 建立清晰的组件分类标准

- **Services/**: 业务逻辑组件（DataManager、CommandHandler、Validator、Manager）
- **ViewModels/Components/**: 视图层辅助组件（仅处理UI相关逻辑）

**涉及模块**:
- Formula模块: 将FormulaCommandHandler、FormulaDataManager、FormulaValidator从ViewModels/Components/移至Services/
- Consultation模块: 确认当前结构符合规范（已在Services/）

### Phase 2: 基类体系完善

**目标**: 提取公共逻辑到基类，消除代码重复

#### 2.1 ComponentValidatorBase
- 提取公共的异常处理和日志记录逻辑
- ConsultationValidator和MedicalCaseValidator继承此基类
- 位置: Infrastructure/Components/

#### 2.2 CommandHandlerBase
- 提取通用的Register/Execute/CanExecute逻辑
- 各模块CommandHandler继承此基类
- 位置: Infrastructure/Components/

#### 2.3 DataManagerBase<T>
- 定义IDataManager<T>通用接口
- 提取通用的Load/Reload/Save逻辑
- 位置: Infrastructure/Components/

### Phase 3: 模块边界优化（需用户审批）

**评估结论**: 当前Consultation和Prescriptions模块职责已大幅精简

- **Prescriptions模块**: 仅提供IPrescriptionPrintService和IPrescriptionEditorService
  - 选项A: 保持现状（服务库角色）
  - 选项B: 将服务迁移至Infrastructure，删除独立模块

- **Consultation模块**: 仅包含ConsultationFormView和组件服务
  - 选项A: 保持现状（Step 2面板载体）
  - 选项B: 合并入MedicalCase作为子模块

## Impact

- Affected specs: desktop-structure-cleanup, viewmodel-conventions, client-layer-architecture
- Affected code:
  - Infrastructure/Components/ (新增基类)
  - Formula/Services/ (组件迁移)
  - Consultation/Services/ (基类继承)
  - MedicalCase/Services/ (基类继承)

## Risk Assessment

- **低风险**: Phase 1组件位置调整 - 仅涉及文件移动和命名空间更新
- **中风险**: Phase 2基类提取 - 需要仔细测试继承后的行为一致性
- **高风险**: Phase 3模块合并 - 需要用户明确决策，影响项目结构

## Pre-Release适用性评估

| Phase | 复杂度 | 风险 | Pre-Release适合度 |
|-------|--------|------|-------------------|
| Phase 1 | 低 | 低 | ✅ 适合 |
| Phase 2 | 中 | 中 | ⚠️ 谨慎执行 |
| Phase 3 | 高 | 高 | ❌ 建议v1.1.0后 |

**建议**: Pre-Release阶段仅执行Phase 1，Phase 2可选执行，Phase 3推迟到v1.1.0。

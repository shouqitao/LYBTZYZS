# Tasks: Desktop层代码复用优化

## Phase 1: 组件存放位置统一

### 1.1 Formula模块组件迁移
- [ ] 1.1.1 将FormulaCommandHandler从ViewModels/Components/移至Services/
- [ ] 1.1.2 将FormulaDataManager从ViewModels/Components/移至Services/
- [ ] 1.1.3 将FormulaValidator从ViewModels/Components/移至Services/
- [ ] 1.1.4 更新FormulaModule.cs中的命名空间引用
- [ ] 1.1.5 更新所有引用这些组件的ViewModel命名空间

### 1.2 编译验证
- [ ] 1.2.1 执行完整编译确认无错误
- [ ] 1.2.2 运行Formula模块相关测试

## Phase 2: 基类体系完善（可选）

### 2.1 ComponentValidatorBase提取
- [ ] 2.1.1 在Infrastructure/Components/创建ComponentValidatorBase.cs
- [ ] 2.1.2 提取公共的异常处理和日志记录逻辑
- [ ] 2.1.3 修改ConsultationValidator继承ComponentValidatorBase
- [ ] 2.1.4 修改MedicalCaseValidator继承ComponentValidatorBase
- [ ] 2.1.5 编译验证并运行相关测试

### 2.2 CommandHandlerBase提取
- [ ] 2.2.1 在Infrastructure/Components/创建CommandHandlerBase.cs
- [ ] 2.2.2 提取通用的Register/Execute/CanExecute逻辑
- [ ] 2.2.3 修改各模块CommandHandler继承CommandHandlerBase
- [ ] 2.2.4 编译验证并运行相关测试

### 2.3 IDataManager<T>接口定义
- [ ] 2.3.1 在Infrastructure/Interfaces/创建IDataManager.cs
- [ ] 2.3.2 定义通用的Load/Reload/Save方法签名
- [ ] 2.3.3 各模块DataManager实现IDataManager<T>
- [ ] 2.3.4 编译验证并运行相关测试

## Phase 3: 模块边界优化（需用户审批，建议v1.1.0后执行）

### 3.1 Prescriptions模块评估
- [ ] 3.1.1 分析IPrescriptionPrintService使用范围
- [ ] 3.1.2 分析IPrescriptionEditorService使用范围
- [ ] 3.1.3 用户决策：保持现状 vs 迁移至Infrastructure
- [ ] 3.1.4 根据决策执行迁移或记录保持现状

### 3.2 Consultation模块评估
- [ ] 3.2.1 分析ConsultationFormView的独立价值
- [ ] 3.2.2 分析ConsultationModule与MedicalCaseModule的依赖关系
- [ ] 3.2.3 用户决策：保持现状 vs 合并入MedicalCase
- [ ] 3.2.4 根据决策执行合并或记录保持现状

## 验证清单

### 编译验证
- [ ] Release配置编译通过(0错误)
- [ ] 无新增编译警告

### 功能验证
- [ ] Formula模块功能正常（经验方CRUD）
- [ ] Consultation模块功能正常（诊断填写）
- [ ] MedicalCase模块功能正常（医案流程）

## 清理总结模板

```markdown
**Phase 1完成情况**:
- 迁移文件: X个
- 更新引用: X处

**Phase 2完成情况**:
- 新增基类: X个
- 重构组件: X个
- 代码减少: X行

**跳过项**:
- [原因说明]
```

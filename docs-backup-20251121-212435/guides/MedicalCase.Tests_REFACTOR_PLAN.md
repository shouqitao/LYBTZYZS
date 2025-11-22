# MedicalCase.Tests 重构计划

## 问题概述

MedicalCase.Tests项目存在大量编译错误（45+），需要全面重构以匹配MedicalCase模块的当前实现。

## 错误类型分析

### 1. 构造函数参数不匹配

#### MedicalCaseDataManager
- 错误: `未提供logger参数`
- 修复: 添加`ILogger<MedicalCaseDataManager>`参数

#### MedicalCaseCommandHandler
- 错误: `不包含采用5个参数的构造函数`
- 修复: 查看当前构造函数签名并更新测试

#### CompletionViewModel
- 错误: 参数类型不匹配（顺序和类型都错误）
- 修复: 
  * 参数1: 从`IMedicalCaseRepository`改为`MedicalCaseDataManager`
  * 参数3: 从`ICommonDialogService`改为`IEventAggregator`
  * 参数4: 从`IEventAggregator`改为`ILoggerFactory`
  * 参数5: 从`ILoggerFactory`改为`ICommonDialogService?`

#### MedicalCaseFlowViewModel  
- 错误: `未提供regionManager参数`
- 修复: 添加`IRegionManager`参数到测试

### 2. 类型转换问题

- 错误: `无法将FlowStep隐式转换为ConsultationStep`
- 影响: MedicalCaseFlowViewModelTests多处
- 修复: 理解FlowStep vs ConsultationStep的关系，使用正确类型

### 3. 缺失属性/方法

- 错误: `MedicalCaseFlowViewModel未包含IsStep1/2/3/4定义`
- 影响: MedicalCaseFlowViewModelTests多处断言
- 修复: 
  * 查看MedicalCaseFlowViewModel当前公开的属性
  * 可能改用`CurrentStep`或其他步骤追踪属性

## 工作量估算

- **总错误数**: 45+
- **影响文件**: 
  * MedicalCaseDataManagerTests.cs
  * MedicalCaseCommandHandlerTests.cs
  * CompletionViewModelTests.cs
  * MedicalCaseFlowViewModelTests.cs
- **预计工时**: 5-7小时

## 执行策略

1. **第一步**: 读取所有被测试类的当前实现，理解构造函数签名和公开API
2. **第二步**: 逐个修复测试类，按优先级：
   - MedicalCaseDataManagerTests (基础组件)
   - MedicalCaseCommandHandlerTests (业务逻辑)
   - CompletionViewModelTests (ViewModel)
   - MedicalCaseFlowViewModelTests (流程控制)
3. **第三步**: 运行测试验证修复
4. **第四步**: 补充缺失的测试用例（如果发现新增功能未覆盖）

## 前置条件

- Issue #2162 (Desktop测试项目重构) 完成
- MedicalCase模块稳定（无大的架构变更计划）

## 建议

建议作为独立Issue追踪，不阻塞Issue #2162的完成。

---
创建日期: 2025-11-19  
相关Issue: #2162 Phase 2.3

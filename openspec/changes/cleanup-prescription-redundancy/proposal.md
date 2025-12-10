# OpenSpec Proposal: cleanup-prescription-redundancy

## 元数据
- **变更ID**: cleanup-prescription-redundancy
- **状态**: proposed
- **创建日期**: 2025-12-10
- **关联Issue**: 待创建

## 背景与动机

### 问题描述
Desktop层Prescriptions模块存在大量冗余代码。经过深度分析发现：

1. **重复实现**: MedicalCase模块已有独立的处方相关组件实现，与Prescriptions模块存在重复：
   - `PrescriptionCalculator` - 两个模块各有一份
   - `PrescriptionValidator` - 两个模块各有一份
   - `PrescriptionItemViewModel` - 两个模块各有一份

2. **架构演进**: 根据之前的`refactor-prescription-module-consolidation`变更：
   - MedicalCase成为聚合根，包含Prescription作为组成部分
   - Prescriptions模块已精简为服务提供者角色
   - 但仍保留了大量未使用的ViewModels和Components

3. **实际使用情况**: 
   - MedicalCase模块不使用`using LYBT.Desktop.Prescriptions`命名空间
   - 仅通过接口使用两个服务：`IPrescriptionPrintService`、`IPrescriptionEditorService`

### 代码规模分析

| 分类 | 文件 | 行数 | 状态 |
|------|------|------|------|
| **保留 - 核心服务** | | | |
| 模块入口 | PrescriptionsModule.cs | 44 | KEEP |
| 编辑器服务 | PrescriptionEditorService.cs | 348 | KEEP |
| 打印服务 | PrescriptionPrintService.cs | 420 | KEEP |
| 打印接口 | IPrescriptionPrintService.cs | 107 | KEEP |
| 打印文档构建器 | PrescriptionFlowDocumentBuilder.cs | 440 | KEEP |
| 打印DTO | PrescriptionPrintDto.cs | 57 | KEEP |
| **待删除 - 重复代码** | | | |
| 计算器(重复) | ViewModels/Components/PrescriptionCalculator.cs | 128 | DELETE |
| 验证器(重复) | ViewModels/Components/PrescriptionValidator.cs | 168 | DELETE |
| 项ViewModel(重复) | ViewModels/PrescriptionItemViewModel.cs | 178 | DELETE |
| **待分析 - 可能删除** | | | |
| 基础验证器 | Components/BasicValidator.cs | 383 | ANALYZE |
| 价格计算器 | Components/PriceCalculator.cs | 218 | ANALYZE |
| 事件协调器 | ViewModels/Components/PrescriptionEventCoordinator.cs | 502 | ANALYZE |
| 项行模型 | ViewModels/PrescriptionItemRow.cs | 30 | ANALYZE |
| 处方项模型 | Models/PrescriptionItem.cs | 480 | ANALYZE |
| 常量定义 | Constants/PrescriptionConstants.cs | 129 | ANALYZE |

**统计**:
- 保留代码: ~1,416行 (6个文件)
- 确认删除: ~474行 (3个文件)  
- 待分析: ~1,742行 (6个文件)
- 总计: ~3,632行

## 目标

1. **删除确认重复的代码** - 移除与MedicalCase模块重复的ViewModels/Components
2. **分析并清理未使用代码** - 验证Components、Models、Constants的实际使用情况
3. **保持模块最小化** - Prescriptions模块仅提供打印服务和编辑器服务
4. **确保编译通过** - 所有更改后项目正常编译运行

## 范围

### 包含
- LYBT.Desktop.Prescriptions模块内的冗余代码清理
- csproj文件更新（移除已删除文件的引用）

### 不包含
- MedicalCase模块的任何修改
- 打印功能的任何修改
- API层或Server层的修改
- 新功能开发（经验方导入、历史处方导入等）

## 风险评估

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 删除被间接引用的代码 | 编译失败 | 每个Phase后执行完整编译验证 |
| 打印功能受影响 | 功能回归 | 打印相关代码完全保留 |
| PrescriptionEditorService依赖断裂 | 运行时错误 | 仔细分析依赖关系后再删除 |

## 成功标准

1. 编译通过：`dotnet build LYBT.All.sln -c Release`
2. 单元测试通过：所有现有测试继续通过
3. 功能验证：医案处方CRUD功能正常
4. 代码减少：至少移除400行冗余代码

## 批准

- [ ] 技术评审通过
- [ ] 用户确认范围

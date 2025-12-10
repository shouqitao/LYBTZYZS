# Design: cleanup-prescription-redundancy

## 1. 架构概述

### 1.1 当前架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    Desktop Modules                               │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐    ┌─────────────────────────────────┐ │
│  │   MedicalCase       │    │      Prescriptions              │ │
│  │   (聚合根)          │    │      (服务提供者)               │ │
│  │                     │    │                                 │ │
│  │  ┌───────────────┐  │    │  ┌───────────────────────────┐  │ │
│  │  │ Prescription  │  │    │  │ Services (保留)           │  │ │
│  │  │ PanelViewModel│  │    │  │ - PrescriptionPrintService│  │ │
│  │  └───────────────┘  │    │  │ - PrescriptionEditorService│ │ │
│  │  ┌───────────────┐  │    │  └───────────────────────────┘  │ │
│  │  │ Components/   │  │    │  ┌───────────────────────────┐  │ │
│  │  │ - Calculator  │  │    │  │ ViewModels (冗余)         │  │ │
│  │  │ - Validator   │  │    │  │ - PrescriptionCalculator  │  │ │
│  │  │ - DataLoader  │  │    │  │ - PrescriptionValidator   │  │ │
│  │  │ - SaveHandler │  │    │  │ - PrescriptionItemVM      │  │ │
│  │  └───────────────┘  │    │  └───────────────────────────┘  │ │
│  └─────────────────────┘    │  ┌───────────────────────────┐  │ │
│                             │  │ Components (待分析)       │  │ │
│                             │  │ - BasicValidator          │  │ │
│                             │  │ - PriceCalculator         │  │ │
│                             │  └───────────────────────────┘  │ │
│                             └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 目标架构

```
┌─────────────────────────────────────────────────────────────────┐
│                    Desktop Modules                               │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────┐    ┌─────────────────────────────────┐ │
│  │   MedicalCase       │    │      Prescriptions              │ │
│  │   (聚合根)          │    │      (最小化服务模块)           │ │
│  │                     │    │                                 │ │
│  │  ┌───────────────┐  │    │  ┌───────────────────────────┐  │ │
│  │  │ Prescription  │◄─────────│ IPrescriptionEditorService│  │ │
│  │  │ PanelViewModel│  │    │  └───────────────────────────┘  │ │
│  │  └───────────────┘  │    │  ┌───────────────────────────┐  │ │
│  │  ┌───────────────┐  │    │  │ IPrescriptionPrintService │  │ │
│  │  │ 独立Components│  │    │  └───────────────────────────┘  │ │
│  │  │ (完整实现)    │  │    │  ┌───────────────────────────┐  │ │
│  │  └───────────────┘  │    │  │ Print相关实现             │  │ │
│  └─────────────────────┘    │  │ - FlowDocumentBuilder     │  │ │
│                             │  │ - PrintDto                │  │ │
│                             │  └───────────────────────────┘  │ │
│                             └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## 2. 设计决策

### 2.1 保留的代码

| 文件 | 理由 |
|------|------|
| PrescriptionsModule.cs | 模块入口，注册服务 |
| Services/PrescriptionEditorService.cs | 实现IPrescriptionEditorService接口 |
| Services/Print/PrescriptionPrintService.cs | 打印功能核心实现 |
| Services/Print/IPrescriptionPrintService.cs | 打印服务接口定义 |
| Services/Print/PrescriptionFlowDocumentBuilder.cs | WPF FlowDocument构建 |
| Services/Print/PrescriptionPrintDto.cs | 打印数据传输对象 |

### 2.2 删除的代码

| 文件 | 理由 |
|------|------|
| ViewModels/Components/PrescriptionCalculator.cs | MedicalCase有独立实现 |
| ViewModels/Components/PrescriptionValidator.cs | MedicalCase有独立实现 |
| ViewModels/PrescriptionItemViewModel.cs | MedicalCase有独立实现 |

### 2.3 待分析的代码

这些文件需要进一步分析其被引用情况：

1. **Components/BasicValidator.cs** (383行)
   - 可能被PrescriptionEditorService.ValidatePrescriptionAsync使用
   - 需检查是否有调用关系

2. **Components/PriceCalculator.cs** (218行)
   - 可能被PrescriptionEditorService.CalculateTotalAmountAsync使用
   - 需检查是否有调用关系

3. **ViewModels/Components/PrescriptionEventCoordinator.cs** (502行)
   - 事件协调器，可能与已删除的ViewModel相关
   - 大概率可删除

4. **Models/PrescriptionItem.cs** (480行)
   - 本地模型类，检查是否仍被使用
   - 可能被打印服务引用

5. **ViewModels/PrescriptionItemRow.cs** (30行)
   - 小型ViewModel，检查引用

6. **Constants/PrescriptionConstants.cs** (129行)
   - 常量定义，可能被多处使用
   - 需仔细检查

## 3. 依赖关系分析

### 3.1 PrescriptionEditorService依赖

```csharp
// 当前依赖
- IPrescriptionApi (来自Contracts)
- IHerbRepository (来自Herbs模块)
- ILogger<PrescriptionEditorService>

// 使用的DTO
- HerbDto
- PrescriptionSearchResultDto
- FormulaDto
- PrescriptionDto
- PrescriptionCreateDto
- PrescriptionItemDto
```

**结论**: PrescriptionEditorService不依赖本模块的ViewModels/Components。

### 3.2 PrescriptionPrintService依赖

需要分析其对本地Models和Components的依赖。

## 4. 实施策略

### 4.1 渐进式删除

1. **Phase 1**: 删除确认重复的ViewModels (低风险)
2. **Phase 2**: 分析并删除未使用Components (中等风险)
3. **Phase 3**: 分析并删除未使用Models (中等风险)
4. **Phase 4**: 最终验证和清理

### 4.2 回滚策略

- 每个Phase提交一次Git commit
- 如果Phase N编译失败，回滚到Phase N-1
- 使用`git diff`追踪所有更改

## 5. 验证计划

### 5.1 编译验证
```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

### 5.2 功能验证清单

- [ ] 创建新医案
- [ ] 添加处方药材
- [ ] 修改药材剂量
- [ ] 删除处方药材
- [ ] 价格自动计算
- [ ] 保存医案
- [ ] 打印医案预览

## 6. 影响评估

### 6.1 正面影响
- 减少代码重复
- 降低维护成本
- 清晰模块职责
- 减少编译时间

### 6.2 潜在风险
- 删除被间接引用的代码
- 打印功能回归

### 6.3 缓解措施
- 每阶段编译验证
- 保留完整的打印相关代码
- Git版本控制支持回滚

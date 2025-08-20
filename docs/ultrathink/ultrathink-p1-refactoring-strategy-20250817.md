# UltraThink P1模块依赖优化策略

**生成时间**: 2025-08-17  
**重构目标**: Core层瘦身 + 模块自治增强  
**预期工期**: 3个Phase，每个Phase 1-2天

## 🎯 总体重构目标

### 1. **Core层职责明确化**
将Core层从"万能层"重构为"纯基础设施层"

### 2. **模块自治性提升**  
每个模块管理自己的业务协调器和特定服务

### 3. **依赖关系清洁化**
消除循环依赖，建立清晰的分层结构

## 📋 Phase 1: Core层Coordinators迁移

### 🎯 目标
将业务特定的Coordinators从Core层迁移到对应模块

### 📊 迁移清单

```bash
# 源位置 → 目标位置
Core/Coordinators/PatientCoordinator.cs 
→ Modules/Patients/Coordinators/PatientCoordinator.cs

Core/Coordinators/ConsultationCoordinator.cs 
→ Modules/Consultation/Coordinators/ConsultationCoordinator.cs

Core/Coordinators/PrescriptionCoordinator.cs 
→ Modules/Prescriptions/Coordinators/PrescriptionCoordinator.cs

Core/Coordinators/FormulaCoordinator.cs 
→ Modules/Formula/Coordinators/FormulaCoordinator.cs

Core/Coordinators/HerbCoordinator.cs 
→ Modules/Herbs/Coordinators/HerbCoordinator.cs

Core/Coordinators/MedicalCaseCoordinator.cs 
→ Modules/MedicalCase/Coordinators/MedicalCaseCoordinator.cs
```

### 🔧 实施步骤

#### Step 1.1: 创建目标目录结构
```bash
# 为每个模块创建Coordinators目录
Modules/Patients/Coordinators/
Modules/Consultation/Coordinators/
Modules/Prescriptions/Coordinators/
Modules/Formula/Coordinators/
Modules/Herbs/Coordinators/
Modules/MedicalCase/Coordinators/
```

#### Step 1.2: 复制文件到新位置
- 保留原文件作为备份
- 更新命名空间引用
- 修复编译错误

#### Step 1.3: 更新项目引用
```xml
<!-- 每个模块的.csproj需要添加新的文件引用 -->
<ItemGroup>
  <Compile Include="Coordinators\*.cs" />
</ItemGroup>
```

#### Step 1.4: 更新依赖注入配置
```csharp
// 在每个ModuleClass中注册自己的Coordinator
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IPatientCoordinator, PatientCoordinator>();
}
```

#### Step 1.5: 验证和清理
- 编译验证
- 单元测试验证
- 删除原Core中的Coordinator文件

### ⚠️ 风险与缓解

**风险1**: 命名空间变更导致编译错误  
**缓解**: 使用全局查找替换，分模块逐步迁移

**风险2**: 依赖注入配置遗漏  
**缓解**: 为每个Coordinator添加接口，便于验证注册

**风险3**: 循环依赖暴露  
**缓解**: 先分析依赖关系，再执行迁移

## 📋 Phase 2: 模块结构标准化

### 🎯 目标
建立统一的模块内部结构标准

### 📊 标准模块结构

```
Modules/{ModuleName}/
├── Coordinators/           # 业务协调器
│   └── {ModuleName}Coordinator.cs
├── Services/              # 模块服务
│   ├── Interfaces/
│   └── *.cs
├── ViewModels/            # 视图模型
│   └── *.cs
├── Views/                # 视图
│   └── *.xaml
├── Models/               # 模块特定模型 (新增)
│   ├── Info/            # UI绑定模型
│   ├── Events/          # 模块事件
│   └── Constants/       # 模块常量
├── Resources/            # 模块资源 (新增)
│   └── *.xaml
└── {ModuleName}Module.cs # 模块注册
```

### 🔧 实施步骤

#### Step 2.1: 分析现有模块差异
```bash
# 现状分析
Users模块:        简单 (Services/ViewModels/Views)
Consultation模块: 复杂 (Components/Constants/Controls/...)
```

#### Step 2.2: 创建标准目录
为每个模块添加缺失的标准目录

#### Step 2.3: 迁移模块特定内容
```bash
# 示例：Consultation模块重构
Consultation/Components/ → Consultation/Views/Components/
Consultation/Constants/ → Consultation/Models/Constants/
Consultation/Controls/ → Consultation/Views/Controls/
```

#### Step 2.4: 更新项目文件
标准化每个模块的.csproj配置

### 📈 预期收益

1. **开发体验一致性**: 所有模块遵循相同的组织规则
2. **新人上手友好**: 标准化结构降低学习成本  
3. **工具支持增强**: IDE和构建工具更好支持

## 📋 Phase 3: 依赖关系优化

### 🎯 目标
优化模块间依赖，引入事件驱动架构

### 📊 当前依赖问题

```
问题1: 所有模块 → Core (Core职责过重)
问题2: Core → Shared.* (合理)
问题3: 模块间可能存在直接依赖 (需验证)
```

### 🔧 优化策略

#### Strategy 3.1: 依赖倒置
```csharp
// Before: 直接依赖
public class PatientCoordinator 
{
    private readonly ConsultationCoordinator _consultationCoordinator;
}

// After: 依赖接口
public class PatientCoordinator 
{
    private readonly IConsultationService _consultationService;
}
```

#### Strategy 3.2: 事件驱动通信
```csharp
// 模块间通过事件通信，而非直接调用
public class PatientCoordinator 
{
    public async Task CreatePatient(PatientCreateInfo info)
    {
        var patient = await _service.CreateAsync(info);
        
        // 发布事件而非直接调用其他模块
        _eventAggregator.PublishAsync(new PatientCreatedEvent 
        { 
            PatientId = patient.Id 
        });
    }
}
```

#### Strategy 3.3: 共享契约
```bash
# 创建模块间共享契约
Shared/Contracts/
├── Events/              # 跨模块事件定义
├── Interfaces/          # 跨模块接口
└── Models/             # 跨模块数据模型
```

### 📊 优化后的依赖结构

```
理想依赖关系:
Modules → Core.Abstractions (纯接口)
Modules → Shared.Contracts (跨模块契约)
Core → Shared.* (基础设施)

消除:
Modules ↔ Modules (直接依赖)
Core → Modules (反向依赖)
```

## 🚀 实施时间表

### Week 1
- **Day 1-2**: Phase 1 - Coordinators迁移
- **Day 3**: 编译验证和测试

### Week 2  
- **Day 1-2**: Phase 2 - 模块标准化
- **Day 3**: Phase 3 - 依赖优化开始

### Week 3
- **Day 1-2**: Phase 3 - 完成依赖优化
- **Day 3**: 全面测试和文档更新

## 📈 成功指标

### 技术指标
- [ ] 编译时间减少 20%
- [ ] Core层文件数量减少 40%
- [ ] 模块测试独立性达到 100%

### 质量指标  
- [ ] 循环依赖数量 = 0
- [ ] 模块间直接依赖 < 3个
- [ ] 架构合规性 > 95%

### 开发指标
- [ ] 新模块创建时间减少 50%
- [ ] 模块修改影响范围减少 60%
- [ ] 开发团队满意度提升

## ⚠️ 回退计划

### 回退触发条件
- 编译错误无法在2小时内解决
- 核心功能测试失败率 > 10%
- 性能下降 > 20%

### 回退步骤
1. 恢复git备份分支
2. 重新评估实施策略  
3. 调整实施计划

---

**总结**: 这个分阶段的重构策略可以系统性地解决当前架构问题，同时最小化风险。每个Phase都有明确的目标和验证标准，确保重构的成功。
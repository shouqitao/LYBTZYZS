# 三模块协作关系设计文档

## 📋 当前协作现状

### 涉及的三个核心模块
1. **Herbs (药材模块)** - 药材基础信息管理
2. **Formula (验方模块)** - 经典验方模板库
3. **Prescriptions (处方模块)** - 处方开具和管理

## 🎯 协作关系图

### 数据流向关系
```
Herbs (药材库)
    ↓ (提供药材基础信息)
Formula (验方模板)
    ↓ (提供验方模板)
Prescriptions (实际处方)
```

### 核心协作流程
```
1. 药材管理员维护 → Herbs (药材信息)
2. 医生创建/导入 → Formula (验方模板) 
3. 医生开具处方 → Prescriptions (基于验方模板和药材库)
```

## 🔗 模块间具体协作关系

### 1. Herbs → Formula 协作
**关系**: Formula引用Herbs进行验方组成定义

```csharp
// Formula模块中的FormulaHerbItem
public class FormulaHerbItem : IHerbItem
{
    public Guid HerbId { get; set; }        // 引用 Herbs.Id
    public string HerbName { get; set; }    // 药材名称快照
    public decimal Quantity { get; set; }   // 验方中的标准用量
    public string Unit { get; set; }        // 单位
    public string? Usage { get; set; }      // 特殊用法
}
```

**协作规则**:
- ✅ Formula可以引用任何启用状态的Herb
- ✅ Formula保存药材名称快照，避免依赖问题
- ✅ 药材价格变动不影响验方模板
- ❌ 不实施复杂的药材可用性检查

### 2. Formula → Prescriptions 协作 (命名统一后)
**关系**: Prescriptions可以应用Formula模板快速开方

```csharp
// 命名统一后的协作方法
public async Task<ServiceResult<Prescription>> ApplyFormulaAsync(Guid prescriptionId, Guid formulaId)
{
    var formula = await GetFormulaWithHerbsAsync(formulaId);
    var prescription = await GetPrescriptionAsync(prescriptionId);
    
    // 统一使用 Herbs 命名 ✅
    foreach (var formulaHerb in formula.Herbs)
    {
        prescription.Herbs.Add(new PrescriptionHerbItem // ✅ 统一类型名
        {
            HerbId = formulaHerb.HerbId,
            HerbName = formulaHerb.HerbName,
            Quantity = formulaHerb.Quantity,
            Unit = formulaHerb.Unit,
            Usage = formulaHerb.Usage
        });
    }
    
    // 记录验方来源
    prescription.FormulaSource = formula.Name;
    return ServiceResult<Prescription>.Success(prescription);
}
```

**协作规则**:
- ✅ 一个处方可以应用一个或多个验方模板
- ✅ 应用验方后医生可以调整药材用量
- ✅ 记录验方来源以便追踪
- ❌ 不实施复杂的验方冲突检查

### 3. Herbs → Prescriptions 协作
**关系**: Prescriptions直接引用Herbs进行价格计算和药材选择

```csharp
// 处方中药材价格实时计算
public async Task<ServiceResult<decimal>> CalculatePrescriptionAmountAsync(Prescription prescription)
{
    decimal totalAmount = 0;
    
    foreach (var herbItem in prescription.Herbs) // ✅ 统一命名
    {
        var herb = await _herbService.GetByIdAsync(herbItem.HerbId);
        if (herb != null && herb.Status == CommonStatus.Enabled)
        {
            herbItem.UnitPrice = herb.Price;
            totalAmount += herbItem.Amount; // UnitPrice * Quantity
        }
    }
    
    prescription.TotalAmount = totalAmount;
    prescription.FinalAmount = totalAmount * prescription.Discount;
    
    return ServiceResult<decimal>.Success(prescription.FinalAmount);
}
```

**协作规则**:
- ✅ 处方药材价格与药材库实时同步
- ✅ 只能选择启用状态的药材
- ✅ 处方保存价格快照，避免后续调价影响
- ❌ 不实施库存检查功能

## 🎯 数据一致性保证

### 引用完整性规则
1. **软删除策略**: 所有模块使用Status字段控制启用/禁用，不物理删除
2. **名称快照**: Formula和Prescription都保存药材名称快照
3. **价格快照**: Prescription保存开方时的药材价格快照

### 数据同步规则
```csharp
// 药材信息变更时的影响范围
public async Task<ServiceResult<bool>> UpdateHerbAsync(Guid herbId, HerbUpdateDto dto)
{
    // 更新药材基础信息
    var herb = await _herbRepository.GetByIdAsync(herbId);
    // ... 更新逻辑

    // 影响范围说明 (不自动更新):
    // 1. 现有验方模板不受影响 (保持快照)
    // 2. 已开具处方不受影响 (保持价格快照)
    // 3. 新开处方将使用新价格

    return ServiceResult<bool>.Success(true);
}
```

## 📊 命名统一后的统一协作模式

### 统一的接口设计
```csharp
// 三个模块都实现的通用药材项接口
public interface IHerbItem
{
    Guid HerbId { get; set; }        // 药材ID
    string HerbName { get; set; }    // 药材名称
    decimal Quantity { get; set; }   // 用量
    string Unit { get; set; }        // 单位
    string? Usage { get; set; }      // 用法
    string? Remark { get; set; }     // 备注
}

// 三个模块的药材项实现
public class FormulaHerbItem : IHerbItem { }      // 验方药材项
public class PrescriptionHerbItem : IHerbItem { } // 处方药材项 (重命名后)
```

### 统一的命名规范
- **导航属性**: 所有模块都使用 `Herbs` 命名 ✅
- **类型名称**: 统一使用 `XxxHerbItem` 命名模式 ✅
- **字段名称**: HerbId, HerbName 保持一致 ✅

## 🚀 API接口协作设计

### 验方应用到处方的API
```csharp
[HttpPost("{prescriptionId}/apply-formula")]
public async Task<ActionResult<ApiResponse<PrescriptionDto>>> ApplyFormulaAsync(
    Guid prescriptionId, 
    [FromBody] ApplyFormulaRequest request)
{
    try
    {
        var result = await _prescriptionService.ApplyFormulaAsync(prescriptionId, request.FormulaId);
        return HandleServiceResult(result, "验方应用成功");
    }
    catch (Exception ex)
    {
        return HandleException<PrescriptionDto>(ex, "应用验方", prescriptionId);
    }
}
```

### 处方费用重新计算API
```csharp
[HttpPost("{prescriptionId}/recalculate")]
public async Task<ActionResult<ApiResponse<decimal>>> RecalculateAmountAsync(Guid prescriptionId)
{
    try
    {
        var result = await _prescriptionService.RecalculateAmountAsync(prescriptionId);
        return HandleServiceResult(result, "费用计算完成");
    }
    catch (Exception ex)
    {
        return HandleException<decimal>(ex, "重新计算费用", prescriptionId);
    }
}
```

## 📝 开发约束与实施原则

### 当前阶段约束 (用户明确指示)
1. **不做功能扩展** - 以实现当前需求为前提
2. **精简设计** - 删除过多的复杂协作逻辑
3. **专注核心** - 只实现必要的协作关系

### 协作关系开发结论
- ✅ **基础协作**: 药材引用、验方应用、价格计算
- ✅ **命名统一**: 所有模块使用统一的Herbs命名
- ✅ **数据快照**: 保证数据一致性的简单策略
- ❌ **不实施复杂协作**: 配伍检查、库存同步、审批流程等
- ❌ **不实施自动化逻辑**: 数据变更自动同步等功能

### 实施优先级
1. **最高优先级**: 完成Prescriptions模块命名统一
2. **高优先级**: 验证现有协作代码是否正常工作
3. **中优先级**: 更新API文档反映协作关系
4. **低优先级**: 前端界面适配命名变更

## 📊 协作关系测试要点

### 核心协作功能测试
1. **验方应用测试**: Formula → Prescription 数据复制
2. **价格计算测试**: Herb价格 → Prescription金额计算
3. **命名统一测试**: 确保Herbs命名在三个模块中一致工作

### 数据一致性测试
1. **药材禁用测试**: 禁用药材后对验方和处方的影响
2. **价格调整测试**: 药材调价后对现有处方的影响
3. **验方删除测试**: 删除验方后对处方记录的影响

---

**文档版本**: v1.0  
**创建时间**: 2025-09-01  
**更新状态**: 协作关系精简设计完成，专注核心功能
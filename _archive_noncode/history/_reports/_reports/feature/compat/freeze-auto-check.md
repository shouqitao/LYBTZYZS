# 医疗安全 - 配伍禁忌自动检查系统冻结清单

**冻结时间**: 2025-09-09  
**冻结范围**: 所有自动配伍检查、阻断、验证相关功能  
**冻结原因**: 编译优先级 + 小诊所实用化需求降级  
**替代方案**: MVP记录型配伍备注功能

## 🚫 强制冻结的核心组件

### 1. 后端自动检查核心模块

#### A. 智能处方服务 (FROZEN)
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/IntelligentPrescriptionService.cs`
**状态**: 🔒 **完全冻结** - 禁止本迭代实现
**影响**: 验方组合自动配伍检查功能
```csharp
/// 智能处方服务实现 - 核心配伍和验方组合功能
public class IntelligentPrescriptionService : IIntelligentPrescriptionService
{
    // TODO: 实现验方组合逻辑 (Line 28)
    // 包含：获取验方模板、合并药材清单、去重处理、生成新处方
}
```
**冻结内容**: 整个类及其接口，直至过度设计清理批次

#### B. 配伍验证事务步骤 (FROZEN)
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/Steps/ValidateCompatibilityStep.cs`
**状态**: 🔒 **完全冻结** - 高度复杂事务系统组件
**影响**: 处方保存时的自动配伍阻断功能
```csharp
public class ValidateCompatibilityStep : DatabaseTransactionStep<PrescriptionTransactionContext>
{
    // 包含：十八反检查、十九畏检查、妊娠用药检查等完整中医配伍规则
    // 涉及复杂的事务协调和数据库操作
}
```
**高风险标记**: ⚠️ **与事务流水线强耦合** - 建议在过度设计清理批次中剥离

#### C. 处方业务服务配伍方法 (FROZEN)
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs`
**状态**: 🔒 **方法级冻结** - ValidateCompatibilityAsync方法
**影响**: 处方配伍安全性验证API
```csharp
/// 验证处方配伍安全性 (简化版)
public async Task<ServiceResult<bool>> ValidateCompatibilityAsync(Guid prescriptionId)
{
    // TODO: 实现配伍禁忌检查逻辑 (Line 387)
}
```

### 2. 前端自动检查组件

#### A. 处方验证器 (PARTIAL FROZEN)
**位置**: `src/Client/Desktop/Modules/Prescriptions/ViewModels/Components/PrescriptionValidator.cs`
**状态**: 🔒 **方法级冻结** - CheckCommonIncompatibilities方法
**影响**: 前端处方录入时的实时配伍提示
```csharp
/// 检查常见配伍禁忌
private void CheckCommonIncompatibilities(ValidationResult result, List<PrescriptionItemViewModel> items)
{
    // 简化版配伍冲突检查逻辑
    result.AddError($"配伍禁忌：{pair.Key} 与 {conflicts} 不宜同用");
}
```
**允许保留**: 基础数据验证功能，仅冻结自动配伍判定逻辑

#### B. 验方分析功能 (FROZEN)
**位置**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaBusinessService.cs`
**状态**: 🔒 **功能级冻结** - AnalyzeFormulaAsync方法中的配伍检查
**影响**: 验方复方配伍分析功能
```csharp
// 检查基本配伍禁忌（简化版）
var herbNames = formula.Herbs.Select(h => h.HerbName).ToList();
if (herbNames.Contains("甘草") && herbNames.Contains("甘遂"))
{
    analysis.Risks.Add("检测到甘草与甘遂配伍，请注意监督用药安全");
}
```

### 3. 事务协调相关组件

#### A. 创建处方事务定义 (FROZEN)
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/CreatePrescriptionTransaction.cs`
**状态**: 🔒 **配伍检查部分冻结**
**影响**: 处方创建时的配伍验证流程
```csharp
// 第四步：验证配伍安全性
if (options.IncludeValidateCompatibility) {
    var validateCompatibilityStep = _serviceProvider.GetRequiredService<ValidateCompatibilityStep>();
}
```
**高风险标记**: ⚠️ **事务系统强耦合** - 整个事务系统计划清理

#### B. 事务上下文配伍标记 (FROZEN)
**位置**: `src/Server/Modules/LYBT.Module.Prescriptions/Transactions/PrescriptionTransactionContext.cs`
**状态**: 🔒 **属性级冻结**
```csharp
/// 是否需要验证配伍安全性
public bool RequireCompatibilityCheck { get; set; } = true;
```

## 🚫 发现的Demo/样例代码清单

### 潜在迁移到 samples/ 的内容

#### 1. 简化版配伍检查演示
- **FormulaBusinessService.cs**: 甘草与甘遂配伍检查演示代码
- **PrescriptionValidator.cs**: 基础配伍冲突检查演示逻辑
- **ValidateCompatibilityStep.cs**: 完整的中医配伍规则演示实现

#### 2. 前端兼容性处理代码
```csharp
// using Prism.Dialogs; // Removed for Prism 8.1.97 compatibility
// 大量因Prism版本兼容性问题的临时处理代码
```
**建议**: 版本升级稳定后，移除兼容性处理代码

## 📊 冻结影响评估

### 受影响的API端点 (推测)
基于代码结构分析，以下API端点可能存在但需要冻结：
- `POST /api/v1/prescriptions/{id}/validate-compatibility` - 配伍验证
- `POST /api/v1/prescriptions/intelligent-combine` - 智能验方组合
- `GET /api/v1/herbs/compatibility-check` - 药材配伍检查

**注意**: 当前编译错误导致无法确认实际端点实现状态

### 受影响的前端功能

#### 保留功能 (继续可用)
- ✅ 处方基础录入和保存
- ✅ 药材选择和剂量设置  
- ✅ 处方打印和导出
- ✅ 验方模板应用

#### 冻结功能 (暂停开发)
- 🔒 实时配伍冲突提示
- 🔒 自动配伍安全阻断
- 🔒 智能验方组合推荐
- 🔒 配伍风险等级评估

## 🎯 MVP替代方案 - HerbCompatNotes

### 记录型功能设计
代替自动检查系统，实现轻量级的配伍备注记录功能：

#### 数据模型设计 (概念)
```sql
-- 建议的配伍备注表结构 (仅作为设计参考，不实施)
CREATE TABLE HerbCompatibilityNotes (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    PrescriptionId UNIQUEIDENTIFIER NOT NULL,
    HerbName NVARCHAR(100) NOT NULL,
    CompatibilityNote NVARCHAR(1000) NULL,
    RiskLevel NVARCHAR(20) DEFAULT 'Low', -- Low/Medium/High
    DoctorId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id)
);
```

#### API端点设计 (概念)
```csharp
// 建议的API端点 (遵循基线约束，不实施)
[HttpPost("/api/v1/prescriptions/{prescriptionId}/compat-notes")]
public async Task<ApiResponse<HerbCompatNoteDto>> AddCompatibilityNote(
    Guid prescriptionId, 
    HerbCompatNoteCreateDto dto)

[HttpGet("/api/v1/prescriptions/{prescriptionId}/compat-notes")]
public async Task<ApiResponse<List<HerbCompatNoteDto>>> GetCompatibilityNotes(
    Guid prescriptionId)
```

## 📋 冻结执行清单

### 立即执行项
- [x] 标记ValidateCompatibilityStep类为FROZEN状态
- [x] 标记IntelligentPrescriptionService为FROZEN状态
- [x] 标记所有自动配伍检查TODO为DEFERRED状态
- [x] 记录高风险事务耦合组件清单

### 清理计划项 (后续批次)
- [ ] 剥离事务系统中的配伍检查依赖
- [ ] 移除或重构复杂的配伍规则硬编码
- [ ] 清理Prism兼容性临时代码
- [ ] 整合简化版配伍检查为可选功能

### 风险控制措施
1. **文档标记**: 所有相关代码添加FROZEN注释
2. **编译隔离**: 避免新建配伍相关依赖
3. **接口保持**: 现有接口签名不变，仅返回默认值
4. **测试维护**: 保留现有测试用例但不新增配伍相关测试

## 📈 后续演进路径

### Phase 1 (当前): 功能冻结
- 完全停止自动配伍检查开发
- 实现MVP记录型配伍备注功能
- 保持处方核心流程正常运作

### Phase 2 (3-6个月后): 选择性恢复
- 评估小诊所实际需求
- 如有强烈需求，重新设计轻量级配伍提示
- 基于用户反馈决定功能优先级

### Phase 3 (长期): 专业化升级
- 集成第三方中医药数据库
- 实现基于规则引擎的配伍检查
- 提供可配置的安全级别设置

---
**冻结执行**: 2025-09-09  
**预期解冻评估**: 编译修复完成 + 过度设计清理完成后  
**责任人**: 项目收敛官 + 架构委员会
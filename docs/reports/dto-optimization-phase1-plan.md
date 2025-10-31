# DTO优化 Phase 1 执行计划

**创建时间**: 2025-10-31
**优先级**: 🔥 高优先级
**目标**: 删除MVP超前设计DTO，符合Constitution约束
**前置文档**: [DTO过度设计分析报告](dto-over-design-analysis-2025-10-31.md)

---

## 📋 Phase 1 概览

### 目标

删除所有违反MVP原则的超前设计DTO：
- 审核工作流
- 数据分析/性能监控
- 供应链管理
- AI优化
- 与Issue #1733冲突的遗留代码

### 统计

| 模块 | 待删除DTO | 行数 | 验证状态 |
|------|----------|------|----------|
| MedicalCase | 11个 | ~350行 | ✅ 零引用 |
| Herbs | 8个 | ~300行 | ✅ 零引用 |
| Formula | 2个 | ~50行 | ✅ 零引用 |
| Prescriptions | 1个 | ~20行 | ✅ 零引用 |
| Patients | 0个 | 0行 | N/A |
| **总计** | **22个** | **~720行** | ✅ **安全删除** |

---

## 🎯 删除清单

### 1. MedicalCaseDtos.cs - 删除11个DTO（~350行）

**文件路径**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDtos.cs`

#### 1.1 性能监控DTO（2个）- 与Issue #1733冲突

```csharp
// DELETE: 案例查询性能统计DTO
public class MedicalCaseQueryPerformanceStatDto
{
    public long TotalQueryCount { get; set; }
    public double AverageResponseTime { get; set; }
    public long SlowQueryCount { get; set; }
    public double SlowQueryThresholdMs { get; set; }
    public double P50ResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
}

// DELETE: 医疗案例缓存统计DTO
public class MedicalCaseCacheStatisticsDto
{
    public long CacheHitCount { get; set; }
    public long CacheMissCount { get; set; }
    public double CacheHitRate { get; set; }
    public long TotalCacheSize { get; set; }
    public long CacheEntryCount { get; set; }
    public DateTime LastEvictionTime { get; set; }
}
```

**删除原因**:
- Issue #1733已删除PerformanceController
- 性能监控委托Application Insights
- 零引用，安全删除

**行号**: 约行700-770（估计）

---

#### 1.2 数据分析DTO（9个）- MVP超前设计

```csharp
// DELETE: 案例统计趋势DTO
public class MedicalCaseStatisticsTrendDto { ... }

// DELETE: 案例频率统计DTO
public class MedicalCaseFrequencyStatDto { ... }

// DELETE: 症状统计DTO
public class SymptomStatisticsDto { ... }

// DELETE: 诊断统计DTO
public class DiagnosisStatisticsDto { ... }

// DELETE: 治疗方法统计DTO
public class TreatmentStatisticsDto { ... }

// DELETE: 医生工作量统计DTO
public class DoctorWorkloadStatDto { ... }

// DELETE: 科室统计DTO
public class DepartmentStatisticsDto { ... }

// DELETE: 月度汇总统计DTO
public class MonthlySummaryStatDto { ... }

// DELETE: 年度汇总统计DTO
public class AnnualSummaryStatDto { ... }
```

**删除原因**:
- MVP阶段不需要如此细粒度统计
- 数据分析属于高级功能
- 零引用，安全删除

**行号**: 约行350-700（估计）

---

### 2. HerbOperationDtos.cs - 删除8个DTO（~300行）

**文件路径**: `src/Shared/LYBT.Shared.Models/Contracts/Herbs/HerbOperationDtos.cs`

#### 2.1 审核工作流DTO（1个）

```csharp
// DELETE: 药材审核信息DTO
public class HerbApprovalDto
{
    public bool IsApproved { get; set; }
    public string? ApprovalReason { get; set; }
    public string? RejectionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalTime { get; set; }
}
```

**删除原因**:
- MVP不涉及审核工作流
- 零引用，安全删除

**行号**: 504-511

---

#### 2.2 数据分析DTO（2个）

```csharp
// DELETE: 药材使用模式分析DTO
public class HerbUsagePatternDto
{
    public List<HerbUsageStatDto> UsageStats { get; set; } = new List<HerbUsageStatDto>();
    public int TotalPrescriptions { get; set; }
    public DateTime AnalysisPeriodStart { get; set; }
    public DateTime AnalysisPeriodEnd { get; set; }
}

// DELETE: 药材使用统计DTO
public class HerbUsageStatDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AverageDosage { get; set; }
    public decimal UsagePercentage { get; set; }
}
```

**删除原因**:
- 数据分析属于高级功能
- 零引用，安全删除

**行号**: 516-535

---

#### 2.3 供应链管理DTO（1个）

```csharp
// DELETE: 药材采购建议DTO
public class HerbPurchaseSuggestionDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public decimal RecommendedPurchaseQuantity { get; set; }
    public decimal EstimatedUsage { get; set; }
    public string Unit { get; set; } = "克";
    public string? Supplier { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent
}
```

**删除原因**:
- MVP不涉及采购管理
- 供应链管理超出范围
- 零引用，安全删除

**行号**: 540-550

---

#### 2.4 AI优化DTO（1个）

```csharp
// DELETE: 处方优化建议DTO
public class PrescriptionOptimizationDto
{
    public List<HerbDosageDto> OptimizedFormula { get; set; } = new List<HerbDosageDto>();
    public List<string> Improvements { get; set; } = new List<string>();
    public decimal OriginalCost { get; set; }
    public decimal OptimizedCost { get; set; }
    public decimal CostSavings { get; set; }
    public string? OptimizationReason { get; set; }
}
```

**删除原因**:
- AI优化功能过度超前
- 属于Prescriptions模块（模块职责不清）
- 零引用，安全删除

**行号**: 555-563

---

#### 2.5 专业领域知识DTO（3个）- MVP可能超前

```csharp
// DELETE: 配伍禁忌检查结果
public class CompatibilityCheckResult
{
    public bool IsSafe { get; set; }
    public List<CompatibilityConflict> Conflicts { get; set; } = new List<CompatibilityConflict>();
    public List<string> Warnings { get; set; } = new List<string>();
    public List<string> Suggestions { get; set; } = new List<string>();
}

// DELETE: 配伍冲突信息
public class CompatibilityConflict
{
    public Guid Herb1Id { get; set; }
    public string Herb1Name { get; set; } = string.Empty;
    public Guid Herb2Id { get; set; }
    public string Herb2Name { get; set; } = string.Empty;
    public string ConflictType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
}

// DELETE: 药材使用注意事项DTO
public class HerbUsagePrecautionDto
{
    public Guid HerbId { get; set; }
    public string HerbName { get; set; } = string.Empty;
    public List<string> Precautions { get; set; } = new List<string>();
    public List<string> Contraindications { get; set; } = new List<string>();
    public List<string> SideEffects { get; set; } = new List<string>();
    public string? MaxDailyDosage { get; set; }
    public string? PregnancyCategory { get; set; }
}
```

**删除原因**:
- 配伍禁忌检查需要专业中医药学知识库
- MVP阶段数据库无此数据
- 零引用，安全删除

**行号**: 379-433

---

### 3. FormulaDtos.cs - 删除2个DTO（~50行）

**文件路径**: `src/Shared/LYBT.Shared.Models/Contracts/Formula/FormulaDtos.cs`

#### 3.1 克隆功能DTO（1个）- 与Issue #1733冲突

```csharp
// DELETE: 复制验方DTO
public class CopyFormulaDto
{
    [Required(ErrorMessage = "新验方名称不能为空")]
    [StringLength(100, ErrorMessage = "新验方名称不能超过100个字符")]
    [DisplayName("新验方名称")]
    public string NewName { get; set; } = string.Empty;
}
```

**删除原因**:
- Issue #1733已删除克隆端点（DELETE /api/formulas/{id}/clone）
- 遗留冗余代码
- 零引用，安全删除

**行号**: 760-766

---

#### 3.2 重复DTO（1个）

```csharp
// DELETE: 从处方创建验方DTO（重复）
public class CreateFromPrescriptionDto
{
    [Required(ErrorMessage = "验方名称不能为空")]
    [StringLength(100, ErrorMessage = "验方名称不能超过100个字符")]
    [DisplayName("验方名称")]
    public string Name { get; set; } = string.Empty;
}
```

**删除原因**:
- 与CreateFormulaFromPrescriptionDto功能重复
- 保留CreateFormulaFromPrescriptionDto（更完整）
- 零引用，安全删除

**行号**: 749-755

---

### 4. PrescriptionDtos.cs - 删除1个DTO（~20行）

**文件路径**: `src/Shared/LYBT.Shared.Models/Contracts/Prescriptions/PrescriptionDtos.cs`

#### 4.1 克隆功能DTO（1个）- 与Issue #1733冲突

```csharp
// DELETE: 处方复制DTO
public class PrescriptionCopyDto
{
    [Required(ErrorMessage = "新处方名称不能为空")]
    [StringLength(200, ErrorMessage = "新处方名称不能超过200个字符")]
    [DisplayName("新处方名称")]
    public string NewName { get; set; } = string.Empty;

    [DisplayName("复制处方项目")]
    public bool CopyItems { get; set; } = true;

    [DisplayName("复制用法用量")]
    public bool CopyUsage { get; set; } = true;

    [DisplayName("复制备注")]
    public bool CopyRemark { get; set; } = false;
}
```

**删除原因**:
- Issue #1733删除克隆功能
- 遗留冗余代码
- 零引用，安全删除

**行号**: 491-507

---

## ✅ 执行步骤

### Step 1: 备份当前代码

```bash
git checkout -b feature/dto-optimization-phase1
git add -A
git commit -m "backup: Phase 1开始前的代码快照"
```

---

### Step 2: 删除DTO（按文件顺序）

#### 2.1 删除 MedicalCaseDtos.cs 中的11个DTO

**工具**: 使用`Edit`工具或`mcp__serena__replace_regex`

**删除顺序**（从后往前，避免行号变化）:
1. MedicalCaseQueryPerformanceStatDto（行~760）
2. MedicalCaseCacheStatisticsDto（行~740）
3. AnnualSummaryStatDto（行~680）
4. MonthlySummaryStatDto（行~660）
5. DepartmentStatisticsDto（行~640）
6. DoctorWorkloadStatDto（行~620）
7. TreatmentStatisticsDto（行~600）
8. DiagnosisStatisticsDto（行~580）
9. SymptomStatisticsDto（行~560）
10. MedicalCaseFrequencyStatDto（行~540）
11. MedicalCaseStatisticsTrendDto（行~520）

**验证**: 每删除一个DTO后，编译验证

```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

---

#### 2.2 删除 HerbOperationDtos.cs 中的8个DTO

**删除顺序**（从后往前）:
1. PrescriptionOptimizationDto（行555-563）
2. HerbPurchaseSuggestionDto（行540-550）
3. HerbUsageStatDto（行527-535）
4. HerbUsagePatternDto（行516-522）
5. HerbApprovalDto（行504-511）
6. HerbUsagePrecautionDto（行424-433）
7. CompatibilityConflict（行398-407）
8. CompatibilityCheckResult（行379-393）

**验证**: 每删除一个DTO后，编译验证

---

#### 2.3 删除 FormulaDtos.cs 中的2个DTO

**删除顺序**:
1. CopyFormulaDto（行760-766）
2. CreateFromPrescriptionDto（行749-755）

**验证**: 编译验证

---

#### 2.4 删除 PrescriptionDtos.cs 中的1个DTO

**删除**:
1. PrescriptionCopyDto（行491-507）

**验证**: 编译验证

---

### Step 3: 删除相关using语句（如有）

检查是否有专门为这些DTO添加的using语句，删除不再需要的using。

---

### Step 4: 全量编译验证

```bash
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

**预期结果**: ✅ 0 errors, 0 warnings

---

### Step 5: 搜索残留引用（保险检查）

```bash
# 在项目根目录执行
grep -r "MedicalCaseQueryPerformanceStatDto" --include="*.cs" .
grep -r "HerbApprovalDto" --include="*.cs" .
grep -r "CopyFormulaDto" --include="*.cs" .
grep -r "PrescriptionCopyDto" --include="*.cs" .
# ... 其他DTO
```

**预期结果**: 无任何引用

---

### Step 6: 文档同步

#### 6.1 更新API文档

**文件**: `docs/reference/api/README.md`

**操作**: 确认这些DTO未在API文档中提及（因为它们从未实现）

---

#### 6.2 更新架构文档（如有引用）

**文件**: `docs/explanation/architecture/server/*.md`

**操作**: 搜索是否有文档提到这些DTO，如有则删除

---

### Step 7: Git提交

```bash
git add -A
git status # 确认只修改了4个DTO文件

git commit -m "feat(dto): Phase 1优化 - 删除22个MVP超前设计DTO

Phase 1目标：删除审核、分析、采购、优化相关DTO

删除清单：
- MedicalCaseDtos.cs: 11个DTO（性能监控2个，数据分析9个）
- HerbOperationDtos.cs: 8个DTO（审核1个，分析2个，采购1个，AI优化1个，专业知识3个）
- FormulaDtos.cs: 2个DTO（克隆功能1个，重复DTO 1个）
- PrescriptionDtos.cs: 1个DTO（克隆功能）

统计：
- DTO类减少：22个
- 代码减少：~720行
- 编译状态：✅ 0 errors, 0 warnings

原因：
- 违反MVP原则"够用即好，拒绝超前设计"
- 违反Constitution技术黑名单
- 与Issue #1733冲突（性能监控、克隆功能）
- 零引用，安全删除

关联文档：
- 分析报告：docs/reports/dto-over-design-analysis-2025-10-31.md
- 执行计划：docs/reports/dto-optimization-phase1-plan.md

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## 📊 验证清单

### 编译验证

- [ ] `dotnet restore LYBT.All.sln` 成功
- [ ] `dotnet build LYBT.All.sln -c Release --no-restore` 成功
- [ ] 0 errors, 0 warnings

### 功能验证

- [ ] 删除的DTO确实零引用
- [ ] 无残留using语句导致编译警告
- [ ] 文档无引用这些DTO

### 统计验证

- [ ] 删除DTO数量：22个
- [ ] 代码行数减少：~720行
- [ ] 4个文件被修改

---

## 🔗 后续Phase计划

### Phase 2: 移除业务逻辑和计算属性

**优先级**: 🔥 高优先级
**预计减少**: ~500行业务逻辑代码
**风险**: 中（需同步修改Service层）

---

### Phase 3: 合并Create/Update DTO

**优先级**: ⚠️ 中优先级
**预计减少**: 10对DTO，~400行
**风险**: 中（需修改Controller签名）

---

### Phase 4: 清理属性别名

**优先级**: ℹ️ 低优先级
**预计减少**: ~100行
**风险**: 低（仅API兼容性）

---

### Phase 5: 修复继承设计

**优先级**: ℹ️ 低优先级
**预计减少**: ~300行
**风险**: 低

---

## 📋 问题与风险

### 已知风险

1. **删除顺序重要**: 从后往前删除，避免行号变化
2. **编译检查必需**: 每删除一个DTO后立即编译
3. **文档同步**: 确保文档无引用

### 应对措施

1. **备份代码**: 创建feature分支
2. **渐进式删除**: 一个一个删除并验证
3. **全量测试**: 最后执行完整编译

---

**创建人员**: Claude Code
**审核状态**: 待用户审核
**执行时间**: 预计30-60分钟
**下一步**: 用户批准后立即执行Phase 1

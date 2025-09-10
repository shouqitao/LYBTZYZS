# Prescriptions模块第一批收敛执行计划 (plan-pass1.md)

**目标**: 第一批小改（≤5项）低风险、零业务影响的删除或移动操作
**原则**: 仅包含"低风险、零业务影响、删除或移到samples/"的条目

## 🎯 Pass 1 执行范围

### 风险评估标准
```
🟢 LOW RISK - 可纳入Pass 1:
- 无外部引用的内部文件
- 仅测试代码引用的文件  
- 明确过度工程化的组件
- 有完整替代方案的功能

🟡 MEDIUM RISK - 暂不纳入:
- 有少量外部引用需要适配
- 需要NoOp桩的接口方法
- 影响DI注册的服务类

🔴 HIGH RISK - 严禁纳入:
- 核心API接口定义
- 数据库相关操作
- 前端直接调用的服务
```

## 📋 Pass 1 具体执行项目

### 项目 1: 删除复杂事务处理目录
```
📁 目标: src/Server/Modules/LYBT.Module.Prescriptions/Transactions/
动作: 完整删除整个目录
风险: 🟢 LOW - 仅内部引用，有简单替代方案
```

#### 详细分析
**删除文件清单**:
```
❌ CreatePrescriptionTransaction.cs (290行) - 复杂事务编排
❌ PrescriptionTransactionContext.cs (150行) - 过度复杂上下文
❌ Steps/AddPrescriptionItemsStep.cs (380行) - 26个配置属性  
❌ Steps/CreatePrescriptionStep.cs (270行) - 复杂验证管道
❌ Steps/UpdateMedicalCaseStep.cs (320行) - 跨模块操作
❌ Steps/ValidateCompatibilityStep.cs (450行) - 企业级验证
❌ Steps/ValidatePrerequisitesStep.cs (280行) - 24个前置条件
```

**引用关系检查**:
```
✅ 内部引用: 仅被PrescriptionBusinessService.CreatePrescriptionAsync()调用1处
✅ 外部引用: 0处外部引用
✅ 测试引用: 仅单元测试引用，可同步删除
```

**替代方案**:
```csharp
// 在PrescriptionBusinessService中用简单事务替代
public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(PrescriptionCreateDto dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 简单的CRUD + 基础验证，替代复杂事务编排
        var prescription = new Prescription { /* 映射属性 */ };
        await _repository.CreateAsync(prescription);
        await transaction.CommitAsync();
        return ServiceResult<PrescriptionDto>.Success(prescriptionDto);
    }
    catch 
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**回滚策略**: Git还原整个目录，重新实现简单替代方法

---

### 项目 2: 删除智能推荐服务
```
📁 目标: src/Server/Modules/LYBT.Module.Prescriptions/Services/IntelligentPrescriptionService.cs
动作: 删除文件 + 删除对应接口文件
风险: 🟢 LOW - 无生产引用，功能超出需求
```

#### 详细分析
**删除文件清单**:
```
❌ Services/IntelligentPrescriptionService.cs (420行)
❌ Interfaces/IIntelligentPrescriptionService.cs (80行)
```

**引用关系检查**:
```
✅ 生产引用: 0处生产代码引用
✅ 测试引用: 2处单元测试引用，可同步删除
✅ DI注册: 未在服务注册中发现
```

**功能确认**:
```
超出小诊所需求的功能:
- AI症状分析 (AnalyzeSymptomsAsync)
- 智能用药推荐 (GetRecommendationsAsync)  
- 用法优化建议 (OptimizeDosageAsync)
- 机器学习模型调用 (MLService依赖)
```

**回滚策略**: Git还原文件，重新注册服务

---

### 项目 3: 删除过度复杂的DTO验证特性
```
📁 目标: src/Shared/LYBT.Shared.Models/DTOs/Prescription*.cs中的复杂验证
动作: 移除高级验证特性，保留基础验证
风险: 🟢 LOW - 仅影响验证逻辑，不影响数据结构
```

#### 详细分析
**简化目标文件**:
```
📝 PrescriptionCreateDto.cs - 移除复杂验证特性
📝 PrescriptionUpdateDto.cs - 移除复杂验证特性
📝 PrescriptionItemDto.cs - 移除复杂验证特性
```

**移除的验证特性**:
```csharp
// ❌ 移除 - 复杂验证
[CustomValidation(typeof(PrescriptionValidator), "ValidateComplexRules")]
[ConditionalValidation("Status", "Active", "InactiveValidation")]  
[CrossFieldValidation("StartDate", "EndDate", "DateRangeValidation")]
[AsyncValidation(typeof(DatabaseValidator), "CheckDuplicateAsync")]

// ✅ 保留 - 基础验证
[Required(ErrorMessage = "处方药材不能为空")]
[Range(1, 9999, ErrorMessage = "数量必须在1-9999之间")]
[MaxLength(500, ErrorMessage = "用法说明不能超过500字符")]
```

**影响范围**:
```
✅ 数据结构: 不变，仅移除验证特性
✅ API契约: 不变，DTO属性保持一致
✅ 前端调用: 不变，仍可正常传输数据
```

**回滚策略**: Git还原验证特性，恢复复杂验证规则

---

### 项目 4: 移除未使用的NuGet包引用
```
📁 目标: src/Server/Modules/LYBT.Module.Prescriptions/LYBT.Module.Prescriptions.csproj
动作: 移除与智能功能相关的包引用
风险: 🟢 LOW - 仅依赖清理，不影响编译
```

#### 详细分析
**移除的包引用**:
```xml
<!-- ❌ 移除 - 智能功能相关 -->
<PackageReference Include=\"Microsoft.ML\" Version=\"2.0.1\" />
<PackageReference Include=\"Microsoft.ML.TensorFlow\" Version=\"2.0.1\" />
<PackageReference Include=\"NLP.Chinese\" Version=\"1.2.0\" />
<PackageReference Include=\"Redis.StackExchange\" Version=\"2.6.122\" />

<!-- ✅ 保留 - 基础功能 -->
<PackageReference Include=\"Microsoft.EntityFrameworkCore\" Version=\"8.0.17\" />
<PackageReference Include=\"AutoMapper\" Version=\"15.0.1\" />
<PackageReference Include=\"Microsoft.Extensions.Logging\" Version=\"8.0.0\" />
```

**清理确认**:
```
✅ ML.NET: 仅IntelligentPrescriptionService使用，可安全移除
✅ TensorFlow: 深度学习模型，小诊所用不到
✅ NLP处理: 智能文本分析，超出需求
✅ Redis缓存: 分布式缓存，单机部署无需
```

**回滚策略**: 重新添加包引用，恢复智能服务

---

### 项目 5: 清理测试项目中的复杂测试
```
📁 目标: tests/Server/LYBT.Module.Prescriptions.Tests/中的复杂功能测试
动作: 删除智能功能和复杂事务的测试文件
风险: 🟢 LOW - 测试代码，不影响生产功能
```

#### 详细分析
**删除的测试文件**:
```
❌ IntelligentPrescriptionServiceTests.cs (200行)
❌ CreatePrescriptionTransactionTests.cs (180行)  
❌ ComplexValidationTests.cs (150行)
❌ PerformanceTests.cs (120行)
```

**保留的测试文件**:
```
✅ PrescriptionServiceTests.cs - 基础CRUD测试
✅ PrescriptionRepositoryTests.cs - 数据访问测试
✅ BasicValidationTests.cs - 基础验证测试
✅ CompatibilityCheckTests.cs - 配伍检查测试
```

**回滚策略**: Git还原测试文件，保持测试覆盖率

## 📊 Pass 1 执行总结

### 预期收益
```
🗂️ 删除文件: 12个主要文件
📉 代码量减少: ~2500行 (42%减少)
🚀 编译时间: 减少15-20%  
📦 依赖包: 减少4个外部依赖
🧪 测试简化: 减少650行测试代码
```

### 风险控制
```
✅ 零破坏性变更: 所有外部API保持不变
✅ 零数据影响: 不涉及数据库结构修改
✅ 零业务中断: 核心功能完全保留
✅ 完整回滚: 每个项目都有明确回滚方案
```

### 执行顺序
```
优先级排序(建议按顺序执行):

1️⃣ 项目4 (NuGet包清理) - 最安全，立即收益
2️⃣ 项目5 (测试清理) - 降低测试维护成本  
3️⃣ 项目2 (智能服务删除) - 移除超范围功能
4️⃣ 项目3 (验证简化) - 降低复杂度
5️⃣ 项目1 (事务删除) - 最大收益，需要实现替代方案
```

## ✅ 执行检查清单

### 执行前验证
```
□ 确认当前分支干净，无未提交变更
□ 创建专用分支: prescriptions-pass1-cleanup
□ 备份当前代码状态
□ 确认所有测试通过
□ 确认编译无错误
```

### 每个项目执行后验证
```
□ 代码编译成功
□ 相关测试通过(如果有保留的测试)
□ 核心功能运行正常
□ 创建独立commit，便于回滚
□ 更新文档说明变更内容
```

### 完整Pass 1后验证
```
□ 完整回归测试通过
□ API功能验证正常
□ 前端集成测试通过
□ 性能基准测试 (确认没有性能退化)
□ 生成Pass 1完成报告
```

## 🚨 紧急回滚程序

### 发现问题时立即执行
```bash
# 1. 立即停止当前操作
git status

# 2. 回滚到最近的正常提交
git reset --hard HEAD~1  # 回滚一个commit
# 或
git revert <commit-hash>  # 保留历史的安全回滚

# 3. 验证系统恢复正常
dotnet build --no-restore
dotnet test --no-build

# 4. 分析问题，重新规划
```

## 🎯 成功标准

### Pass 1被认为成功的条件
```
✅ 所有5个项目按计划完成
✅ 代码减少40%+，编译无错误
✅ 核心API功能100%保持
✅ 前端WPF正常集成
✅ 基础处方CRUD完全正常
✅ 简单配伍检查正常工作
✅ 性能无退化，内存使用优化
```

**总结**: Pass 1专注于移除明确的过度工程化组件，预期减少42%代码量，同时保持100%的核心功能和外部兼容性。所有操作都有明确的回滚方案，风险完全可控。
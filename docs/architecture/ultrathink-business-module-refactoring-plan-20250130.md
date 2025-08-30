# UltraThink 业务模块重构计划 v3.0
> 生成时间：2025-01-30
> 架构版本：UltraThink v2.0 (移除Info层)
> 项目定位：中小型中医诊所管理系统（<20用户）

## 一、系统架构总览

### 1.1 三层架构体系
```
后端API层 (ASP.NET Core Web API)
    ↓ DTOs
前端服务层 (ModuleServices)
    ↓ DTOs (直接使用，无Info转换)
UI展示层 (WPF ViewModels)
```

### 1.2 模块关系架构
```
MedicalCase（医案 - 顶层聚合根）
    ├── ConsultationId → Consultation（诊断模块）
    │   ├── MedicalCaseId (反向关联)
    │   ├── 四诊（望闻问切）
    │   └── 诊断结果
    └── PrescriptionId → Prescription（处方模块）
        ├── MedicalCaseId (反向关联)
        ├── 药材列表
        └── 价格计算
```

### 1.3 关键设计决策
- **纯中医系统**：删除所有西医指标（体征、血压等）
- **软删除机制**：Enable/Disable统一命名
- **无Info层**：DTO直接用于UI绑定
- **极简原则**：删除所有过度设计功能

## 二、业务模块重构清单

### 2.1 模块重构统计
| 模块 | 原始行数 | 目标行数 | 削减率 | 优先级 |
|------|---------|---------|--------|--------|
| Consultation | 1912 | 400 | 79% | P0 |
| Prescriptions | 1277 | 500 | 61% | P0 |
| MedicalCase | 924 | 300 | 68% | P0 |
| Auth | 743 | 400 | 46% | P1 |
| Users | 898 | 500 | 44% | P1 |
| Herbs | 713 | 400 | 44% | P2 |
| Patients | 563 | 350 | 38% | P2 |
| Formula | 443 | 350 | 21% | P2 |
| **总计** | **8473** | **3200** | **62%** | - |

### 2.2 各模块详细重构方案

#### 2.2.1 ConsultationModule（诊断模块）
**核心职责**：纯诊断功能，不含处方

**保留功能**：
- 基础CRUD（6个方法）
- UpdateDiagnosisAsync - 更新诊断（含四诊+诊断结果）
- GetDiagnosisSuggestionsAsync - 诊断建议（可选）

**删除功能**：
- ❌ 所有处方相关功能（移到PrescriptionModule）
- ❌ 流程控制（移到MedicalCase）
- ❌ 西医体征管理
- ❌ AI功能、统计、模板、导出
- ❌ 批量操作

**数据结构调整**：
```csharp
public class DiagnosisUpdateDto
{
    public string ChiefComplaint { get; set; }     // 主诉
    public string PresentIllness { get; set; }     // 现病史
    public string Inspection { get; set; }         // 望诊
    public string Auscultation { get; set; }       // 闻诊
    public string Inquiry { get; set; }            // 问诊
    public string Palpation { get; set; }          // 切诊
    public string TCMDiagnosis { get; set; }       // 中医诊断
    public string Syndrome { get; set; }           // 证型
    public string TreatmentPrinciple { get; set; } // 治法
}
```

#### 2.2.2 PrescriptionModule（处方模块）
**核心职责**：处方开具和管理

**保留功能**：
- 基础CRUD
- Enable/DisableAsync（软删除）
- 药材管理（增删改）
- 验方应用（ApplyFormulaAsync）
- 价格计算（CalculatePriceAsync）

**删除功能**：
- ❌ 批量操作
- ❌ 统计分析
- ❌ 模板功能
- ❌ 热门处方

**价格计算**：
```
总价 = 单副价格 × 副数 × 折扣率
```

#### 2.2.3 MedicalCaseModule（医案聚合根）
**核心职责**：协调诊断和处方，管理诊疗流程

**保留功能**：
- 基础CRUD
- 流程控制（StartConsultation/Complete/Cancel）
- 状态管理（UpdateStatusAsync）
- 查询功能（ByPatient/ByDoctor/ByStatus/ByDate）
- 验证功能（CanCreate/CanModify）
- GetMedicalCaseDetailsAsync（聚合诊断+处方）

**删除功能**：
- ❌ 批量操作
- ❌ 统计功能
- ❌ 操作历史

**状态流转**（v1.0）：
```
Created → InConsultation → Completed
                ↓
            Cancelled
```

#### 2.2.4 AuthModule（认证模块）
**保留功能**：
- LoginAsync/LogoutAsync
- RefreshTokenAsync
- ChangePasswordAsync
- ValidatePasswordStrengthAsync
- GetCurrentUserAsync

**删除功能**：
- ❌ 设备指纹
- ❌ 账户锁定
- ❌ 多因素认证
- ❌ IP追踪
- ❌ 复杂API监控

#### 2.2.5 UserModule（用户管理）
**保留功能**：
- 基础CRUD
- ResetPasswordAsync（管理员重置）
- Enable/DisableAsync

**删除功能**：
- ❌ 统计功能
- ❌ 角色管理（在创建时选择）
- ❌ 导入导出
- ❌ Lock/Unlock

#### 2.2.6 HerbModule（药材管理）
**保留功能**：
- 基础CRUD
- Enable/DisableAsync
- GetByCategoryAsync
- 导入导出

**删除功能**：
- ❌ 库存管理
- ❌ 批量操作
- ❌ 价格历史

**新增字段**：
- Category（分类）
- Efficacy（功效）

#### 2.2.7 PatientModule（患者管理）
**保留功能**：
- 基础CRUD
- Enable/DisableAsync
- 导入导出
- 搜索功能

**删除功能**：
- ❌ 统计功能
- ❌ 最近活跃

#### 2.2.8 FormulaModule（验方管理）
**保留功能**：
- 基础CRUD
- Enable/DisableAsync
- CopyAsync
- GetByCategoryAsync
- 导入导出

**新增字段**：
- IsShared（共享标记）
- CreatorId（创建者）
- IsDeleted（软删除）

## 三、重构执行计划

### Phase 1: 架构调整（2天）
**目标**：建立正确的模块关系

1. **Day 1 上午**：ConsultationModule重构
   - 删除处方相关功能
   - 简化为纯诊断模块
   - 合并四诊更新方法

2. **Day 1 下午**：PrescriptionModule整理
   - 清理统计功能
   - 优化价格计算
   - 简化验方应用

3. **Day 2 上午**：MedicalCaseModule调整
   - 建立聚合根地位
   - 实现协调功能
   - 管理关联关系

4. **Day 2 下午**：关系验证
   - 测试三模块协作
   - 验证数据流

### Phase 2: 功能清理（2天）
**目标**：删除过度设计功能

5. **Day 3 上午**：Auth/User模块清理
   - 删除复杂认证功能
   - 简化用户管理

6. **Day 3 下午**：Herb/Patient模块清理
   - 删除库存管理
   - 简化患者功能

7. **Day 4 上午**：Formula模块优化
   - 添加共享机制
   - 清理冗余功能

8. **Day 4 下午**：整体测试
   - 功能验证
   - 性能测试

### Phase 3: UI适配（1天）
**目标**：更新前端绑定

9. **Day 5**：
   - 更新XAML绑定
   - 调整ViewModel
   - 集成测试

## 四、关键技术决策

### 4.1 数据访问模式
```csharp
// 统一使用LINQ，禁止原生SQL
await _context.Users
    .Where(u => u.IsActive)
    .OrderBy(u => u.CreateTime)
    .ToListAsync();
```

### 4.2 软删除实现
```csharp
// 统一命名规范
public async Task<ServiceResult> EnableAsync(Guid id);
public async Task<ServiceResult> DisableAsync(Guid id);
```

### 4.3 缓存策略
- 基础数据（患者、药材、验方）：应用级缓存
- 诊断库：内存缓存
- 不使用分布式缓存

### 4.4 导入导出
- 基础数据保留导入导出
- 看诊数据暂不实现（太复杂）

## 五、风险与应对

### 5.1 主要风险
1. **数据迁移**：Info层移除可能影响现有数据
2. **功能缺失**：删除功能可能影响用户体验
3. **性能问题**：简化可能导致某些场景性能下降

### 5.2 应对措施
1. **渐进式重构**：分模块进行，确保稳定
2. **功能预留**：保留扩展接口
3. **性能监控**：关键操作添加性能日志

## 六、验收标准

### 6.1 代码质量
- 代码行数减少60%以上
- 单个方法不超过50行
- 单个类不超过500行

### 6.2 功能完整性
- 核心业务流程正常
- 数据一致性保证
- 错误处理完善

### 6.3 性能指标
- 页面响应 < 2秒
- API响应 < 500ms
- 内存占用 < 500MB

## 七、后续优化

### v2.0 规划
- 添加挂号模块
- 添加收费模块
- 多诊所支持

### v3.0 规划
- 添加药房模块
- 添加理疗模块
- 移动端支持

---

## 附录：重要决策记录

1. **2025-01-30**：确定Consultation只负责诊断，Prescription独立处理处方
2. **2025-01-30**：明确MedicalCase作为聚合根，通过ID关联诊断和处方
3. **2025-01-30**：决定删除所有西医指标，专注纯中医
4. **2025-01-30**：统一软删除为Enable/Disable命名
5. **2025-01-30**：看诊数据导入功能延后到v2.0
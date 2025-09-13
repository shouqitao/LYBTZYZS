# Infra Batch 2 — Obsolete Components Final Cleanup 执行计划

## 📊 执行概览
- **项目**: Infra Batch 2 — Obsolete Components Final Cleanup（APPLY）
- **分支**: infra/batch2-obsolete-cleanup
- **执行时间**: 2025-09-13
- **目标**: 彻底移除已标记[Obsolete]且确认无引用的基础设施组件

## 🎯 候选组件分析

### 1. Infrastructure目录中的候选组件

#### 1.1 数据库迁移文件
**文件**: `src/Server/Core/LYBT.Infrastructure/Migrations/20250908110242_AddTransactionCoordinatorTables.cs`
- **状态**: `[Obsolete("Complex transaction coordination tables removed in Record-Only mode.")]`
- **类型**: 数据库迁移类
- **分析**: ⚠️ **不能删除** - 迁移文件是EF Core历史记录的一部分
- **建议**: 保留，但添加文档说明

#### 1.2 Infrastructure组件查找结果
**搜索结果**: 
- SimplifiedConfigurationService: ❌ 已在Batch 2-harden中删除
- SensitiveDataInterceptor: ❌ 已在Batch 2-harden中删除  
- DataEncryptionService: ❌ 已在Batch 2-harden中删除

### 2. 桌面客户端中的候选组件

#### 2.1 FeatureToggleService - 复杂特性开关服务
**文件**: `src/Client/Desktop/Core/Services/Configuration/FeatureToggleService.cs`
- **整个类标记**: `[Obsolete("Complex feature toggle features removed in Record-Only mode. Use simple configuration instead.", false)]`
- **接口**: `IFeatureToggleService` 也标记为过时
- **过时方法数量**: 6个方法标记为Obsolete
- **分析**: 🔍 **需要引用检查** - 可能在客户端代码中被使用

### 3. 共享模型中的候选组件

#### 3.1 枚举值 - AuthEnums.UserRole
**文件**: `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs`
- Doctor, Pharmacist, Receptionist, Cashier, Therapist 枚举值（5个）
- **状态**: 已合并到User角色，标记为Obsolete
- **分析**: 🔍 **需要引用检查** - 可能在授权代码中仍被使用

#### 3.2 枚举值 - PatientStatus
**文件**: `src/Shared/LYBT.Shared.Models/Enums/PatientStatus.cs`
- Normal, Deleted, Blacklisted 枚举值（3个）
- **状态**: 已简化为Active/Inactive，标记为Obsolete
- **分析**: 🔍 **需要引用检查** - 可能在患者管理中仍被使用

#### 3.3 枚举值 - MedicalCaseEnums.MedicalCaseStatus  
**文件**: `src/Shared/LYBT.Shared.Models/Enums/MedicalCaseEnums.cs`
- Registered, InConsultation, Completed, Cancelled, Suspended, Archived 枚举值（6个）
- **状态**: 已简化为Active/Closed，标记为Obsolete
- **分析**: 🔍 **需要引用检查** - 可能在医案管理中仍被使用

#### 3.4 DTO属性 - UserStatisticsDto
**文件**: `src/Shared/LYBT.Shared.Models/Contracts/Users/UserDtos.cs`
- DoctorCount, CashierCount, TherapistCount, PharmacistCount, ReceptionistCount 属性（5个）
- **状态**: 已合并到UserCount，标记为Obsolete
- **分析**: 🔍 **需要引用检查** - 可能在统计报表中仍被使用

### 4. 业务模块中的候选组件

#### 4.1 处方模块过时方法
**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionService.cs`
- `CreateFromTemplateAsync` 方法
- **状态**: `[Obsolete("Automatic formula application removed in Record-Only mode. Use manual template import instead.", false)]`

**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs`
- `CreateFromTemplateAsync` 方法

#### 4.2 配伍检查服务
**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/CompatibilityNoteService.cs`
- **整个类**: `[Obsolete("Compatibility checking feature removed in Record-Only mode. Use manual notes instead.", false)]`
- 多个方法标记为Obsolete

#### 4.3 验方查询服务过时方法
**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaQueryService.cs`
- `CalculateConfidence` 和 `CalculateMatchScore` 方法
- **状态**: `[Obsolete("Smart recommendation calculation removed in Record-Only mode.", false)]`

## 📋 引用分析结果

### 需要详细检查的组件

| 组件 | 位置 | 类型 | 预计引用状态 | 建议操作 |
|------|------|------|-------------|----------|
| FeatureToggleService | Desktop/Core/Services | 整个类 | 可能有引用 | 检查后决定 |
| UserRole过时枚举值 | Shared.Models/Enums | 枚举值 | 可能有引用 | 检查后决定 |
| PatientStatus过时枚举值 | Shared.Models/Enums | 枚举值 | 可能有引用 | 检查后决定 |
| MedicalCaseStatus过时枚举值 | Shared.Models/Enums | 枚举值 | 可能有引用 | 检查后决定 |
| UserStatisticsDto过时属性 | Shared.Models/Contracts | DTO属性 | 可能有引用 | 检查后决定 |
| CreateFromTemplateAsync | Prescriptions模块 | 服务方法 | 可能有引用 | 检查后决定 |
| CompatibilityNoteService | Prescriptions模块 | 整个类 | 可能有引用 | 检查后决定 |
| Formula推荐算法方法 | Formula模块 | 私有方法 | 无外部引用 | 可删除 |

### 确定可以保留的组件

| 组件 | 原因 | 处理方式 |
|------|------|----------|
| AddTransactionCoordinatorTables迁移 | EF Core历史记录，不能删除 | 保留+文档说明 |

## 🔍 下一步操作

### 步骤②：引用检查
1. 搜索每个过时组件的引用情况
2. 区分"无引用"和"有引用"的组件
3. 生成删除/保留决策清单

### 步骤③：执行清理
1. 删除确认无引用的组件
2. 保留有引用的组件并添加保留说明
3. 更新相关的DI注册

### 步骤④：验证
1. 编译测试
2. 架构测试
3. 功能验证

## 📊 预期成果

- **预计可删除**: Formula模块中2个私有方法
- **预计需保留**: 大部分枚举值和DTO属性（可能仍有引用）
- **需特别关注**: FeatureToggleService和CompatibilityNoteService（整个类）

---

**步骤①候选确认完成，准备进入引用分析阶段。**
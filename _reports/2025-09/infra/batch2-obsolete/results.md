# Infra Batch 2 — Obsolete Components Final Cleanup 执行结果

## 📊 执行概览
- **项目**: Infra Batch 2 — Obsolete Components Final Cleanup（APPLY）
- **分支**: infra/batch2-obsolete-cleanup
- **执行时间**: 2025-09-13
- **状态**: 步骤②③完成，已删除无引用组件，记录保留组件

## ✅ 已删除的过时组件（无引用）

### 1. Formula模块私有方法（2个）
**文件**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaQueryService.cs`

#### 1.1 CalculateConfidence方法
- **状态**: ✅ 已删除
- **原因**: 私有方法，无外部引用
- **删除理由**: Record-Only模式下不再需要智能推荐算法

#### 1.2 CalculateMatchScore方法
- **状态**: ✅ 已删除  
- **原因**: 私有方法，无外部引用
- **删除理由**: Record-Only模式下不再需要智能推荐算法

### 2. PatientStatus过时枚举值（3个）
**文件**: `src/Shared/LYBT.Shared.Models/Enums/PatientStatus.cs`

#### 2.1 Normal枚举值
- **状态**: ✅ 已删除
- **原因**: 无任何代码引用，已完全替换为Active
- **删除理由**: Record-Only模式简化，统一使用Active状态

#### 2.2 Deleted枚举值  
- **状态**: ✅ 已删除
- **原因**: 无任何代码引用，已完全替换为Inactive
- **删除理由**: Record-Only模式简化，统一使用Inactive状态

#### 2.3 Blacklisted枚举值
- **状态**: ✅ 已删除
- **原因**: 无任何代码引用，已完全替换为Inactive  
- **删除理由**: Record-Only模式简化，统一使用Inactive状态

## ⚠️ 保留的过时组件（有引用）

### 1. 桌面客户端服务

#### 1.1 FeatureToggleService
**文件**: `src/Client/Desktop/Core/Services/Configuration/FeatureToggleService.cs`
- **状态**: 🔶 保留
- **引用位置**: `HotReloadService.cs` (构造函数注入和功能调用)
- **保留原因**: HotReloadService大量使用其GetAllFeatures()和OnFeatureChanged()方法
- **风险评估**: 低风险，虽然标记过时但仍被核心功能依赖

### 2. 共享模型枚举值

#### 2.1 UserRole过时枚举值
**文件**: `src/Shared/LYBT.Shared.Models/Enums/AuthEnums.cs`

##### UserRole.Doctor
- **状态**: 🔶 保留
- **引用数量**: 26个文件，71处引用
- **主要引用**: PermissionService、UserSessionManager、WorkbenchRouter、JWT认证等
- **保留原因**: 核心角色，在权限系统中广泛使用

##### UserRole.Pharmacist  
- **状态**: 🔶 保留
- **引用数量**: 3个文件，5处引用
- **主要引用**: PermissionService、WorkbenchRouter、UserRoleExtensions
- **保留原因**: 仍有权限判断和显示逻辑引用

#### 2.2 MedicalCaseStatus过时枚举值
**文件**: `src/Shared/LYBT.Shared.Models/Enums/MedicalCaseEnums.cs`

##### MedicalCaseStatus.Registered
- **状态**: 🔶 保留  
- **引用数量**: 15个文件，60+处引用
- **主要引用**: 前端UI显示、业务逻辑、测试代码
- **保留原因**: 医案注册状态在整个业务流程中核心使用

### 3. 业务服务方法

#### 3.1 CreateFromTemplateAsync方法
**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/`
- **状态**: 🔶 保留
- **引用数量**: 4个文件，8处引用  
- **主要引用**: 接口定义、委托调用、测试代码
- **保留原因**: 接口方法有实现和测试依赖

#### 3.2 CompatibilityNoteService
**文件**: `src/Server/Modules/LYBT.Module.Prescriptions/Services/CompatibilityNoteService.cs`
- **状态**: 🔶 保留
- **引用数量**: 有DI注册  
- **主要引用**: PrescriptionsModule中的依赖注入注册
- **保留原因**: 已注册为服务，可能有运行时依赖

### 4. 确认无法删除的组件

#### 4.1 数据库迁移文件
**文件**: `src/Server/Core/LYBT.Infrastructure/Migrations/20250908110242_AddTransactionCoordinatorTables.cs`
- **状态**: 🔶 保留（强制）
- **保留原因**: EF Core迁移历史记录，删除会破坏数据库版本管理
- **处理方式**: 仅标记过时，添加文档说明

## 📊 清理统计

### 删除成果
- **删除过时方法**: 2个（Formula模块私有方法）
- **删除过时枚举值**: 3个（PatientStatus枚举）
- **删除代码行数**: 约25行（包含注释和空行）
- **清理文件数量**: 2个

### 保留统计  
- **保留过时类**: 2个（FeatureToggleService、CompatibilityNoteService）
- **保留过时方法**: 1个（CreateFromTemplateAsync）
- **保留过时枚举值**: 10个+（UserRole、MedicalCaseStatus等）
- **保留原因**: 有活跃引用，需要保持向后兼容

## 🔍 分析结论

### 主要发现
1. **Infrastructure层已基本清理**: 在之前的Batch 2-harden中已清理大部分过时组件
2. **客户端代码引用复杂**: FeatureToggleService虽过时但被核心服务依赖
3. **枚举值迁移不完整**: 部分过时枚举值仍在业务逻辑中使用
4. **测试代码维护滞后**: 部分过时API仍有测试代码引用

### 风险评估  
- **删除风险**: 低 - 仅删除确认无引用的私有方法和枚举值
- **保留风险**: 中等 - 过时组件会产生编译警告，但不影响功能
- **维护成本**: 需要后续制定迁移计划逐步移除有引用的过时组件

## 📋 后续建议

### 短期行动（1-2个迭代）
1. **制定迁移计划**: 为保留的过时组件制定逐步迁移策略
2. **更新文档**: 在过时组件上增加更详细的替代方案说明
3. **监控使用**: 设置告警监控过时API的使用情况

### 长期规划（3-6个月）
1. **FeatureToggleService重构**: 将HotReloadService迁移到简化配置管理
2. **枚举值统一**: 完全迁移UserRole和MedicalCaseStatus的过时值
3. **API清理**: 移除CreateFromTemplateAsync等过时业务方法

---

**执行状态**: ✅ 步骤②③完成，准备进入构建验证阶段
# 配伍/智能/规则/流水线 触点清单

## 概述

本文档详细列出LYBTZYZS系统中所有超出Record-Only基线的触点，按四大类别分类：配伍检查、智能推荐、业务规则、流程管道。

## 🔬 配伍检查 (Compatibility) 触点

### 1. 配伍记录服务
**代码位置**: 
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/CompatibilityNoteService.cs`
- 类: `CompatibilityNoteService`
- 方法: `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetByPrescriptionIdAsync`, `GetByIdAsync`

**依赖包**: Entity Framework Core, AutoMapper

**WebAPI端点**: 
- `GET /api/v1/prescriptions/{prescriptionId}/compat-notes`
- `POST /api/v1/prescriptions/{prescriptionId}/compat-notes`
- `PUT /api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}`
- `DELETE /api/v1/prescriptions/{prescriptionId}/compat-notes/{noteId}`

**WPF UI入口**: 
- 处方编辑器配伍检查面板
- 配伍记录列表视图
- 配伍警告对话框

### 2. 配伍检查框架
**代码位置**:
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs:ValidateCompatibilityAsync()`
- `src/Server/Core/LYBT.Entities/Compatibility/HerbCompatibilityNote.cs`

**依赖包**: 无特殊依赖

**注意**: 框架存在但实现为空方法 `throw new NotImplementedException()`

### 3. 配伍数据模型
**代码位置**:
- `src/Shared/LYBT.Shared.Models/Contracts/Compatibility/`
  - `CompatibilityNoteDto.cs`
  - `CompatibilityNoteCreateDto.cs`
  - `CompatibilityNoteUpdateDto.cs`

**依赖包**: System.Text.Json

### 4. 配伍UI组件
**代码位置**: 
- `src/Client/Desktop/Modules/Prescriptions/Views/PrescriptionEditView.xaml`
- `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionEditViewModel.cs`

**WPF UI入口**: 处方编辑界面的配伍检查按钮和面板

## 🧠 智能推荐 (Intelligent) 触点  

### 5. 验方模板系统
**代码位置**:
- `src/Server/Modules/LYBT.Module.Formula/Services/FormulaBusinessService.cs`
- 方法: `ApplyFormulaAsync`, `GetRecommendedFormulasAsync`
- `src/Client/Desktop/Modules/Formula/ViewModels/FormulaLibraryViewModel.cs`

**WPF UI入口**:
- 验方库管理界面
- "应用验方"按钮
- 验方推荐列表

### 6. 智能处方复制
**代码位置**:
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs:CloneFromTemplateAsync()`
- `src/Client/Desktop/Modules/Prescriptions/ViewModels/PrescriptionEditViewModel.cs`

**WPF UI入口**: 
- "从验方创建"按钮
- "复制处方"功能

### 7. 诊断模板系统  
**代码位置**:
- `src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationBusinessService.cs`
- 诊断模板相关方法
- `src/Client/Desktop/Modules/Consultation/ViewModels/ConsultationEditViewModel.cs`

**WPF UI入口**:
- 症状选择器
- 诊断模板选择下拉框

### 8. 智能药材推荐
**代码位置**:
- `src/Server/Modules/LYBT.Module.Herbs/Services/HerbQueryService.cs:GetRecommendedHerbsAsync()`
- `src/Client/Desktop/Modules/Prescriptions/ViewModels/HerbSelectorViewModel.cs`

**WPF UI入口**: 
- 药材选择器的推荐面板
- "智能推荐"按钮

### 9. 处方价格智能计算
**代码位置**:
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs:CalculateTotalPriceAsync()`

**WebAPI端点**: 
- `POST /api/v1/prescriptions/{id}/calculate-price`

### 10. 统计分析功能
**代码位置**:
- `src/Server/Modules/LYBT.Module.Herbs/Services/HerbQueryService.cs:GetUsageStatisticsAsync()`
- `src/Server/Modules/LYBT.Module.Formula/Services/FormulaQueryService.cs:GetPopularFormulasAsync()`

**WPF UI入口**:
- 统计报表界面
- 使用频次图表

## ⚖️ 业务规则 (Rules) 触点

### 11. 用户状态管理
**代码位置**:
- `src/Server/Modules/LYBT.Module.Users/Services/UserBusinessService.cs`
- 方法: `EnableUserAsync`, `DisableUserAsync`, `ValidateUserStatusAsync`

**WebAPI端点**:
- `PUT /api/v1/users/{id}/enable`  
- `PUT /api/v1/users/{id}/disable`

**WPF UI入口**:
- 用户管理界面的启用/禁用按钮

### 12. 医案状态流转
**代码位置**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseBusinessService.cs`
- 方法: `StartCaseAsync`, `CompleteCaseAsync`, `CancelCaseAsync`, `ValidateStatusTransitionAsync`

**状态枚举**: `MedicalCaseStatus` (7种状态)
- Registered, InProgress, Consultation, Prescription, Completed, Cancelled, Archived

**WebAPI端点**:
- `PUT /api/v1/medical-cases/{id}/start`
- `PUT /api/v1/medical-cases/{id}/complete`  
- `PUT /api/v1/medical-cases/{id}/cancel`

### 13. JWT认证规则
**代码位置**:
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthBusinessService.cs`
- 方法: `ValidateCredentialsAsync`, `GenerateTokenAsync`, `ValidateTokenAsync`

**依赖包**: 
- Microsoft.AspNetCore.Authentication.JwtBearer
- System.IdentityModel.Tokens.Jwt

### 14. 权限检查系统
**代码位置**:
- `src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs`
- 属性: `[Authorize(Roles = "Admin,Doctor")]`
- `src/Client/Desktop/Infrastructure/Security/PermissionManager.cs`

**WPF UI入口**: 
- 菜单项权限控制
- 功能按钮可见性控制

### 15. 数据验证规则
**代码位置**:
- `src/Server/Modules/LYBT.Module.Patients/Services/PatientBusinessService.cs:ValidatePatientDataAsync()`
- `src/Shared/LYBT.Shared.Models/Validation/` (各种Validator类)

**依赖包**: FluentValidation (如果使用)

### 16. 处方验证规则  
**代码位置**:
- `src/Server/Modules/LYBT.Module.Prescriptions/Services/PrescriptionBusinessService.cs`
- 方法: `ValidatePrescriptionAsync`, `ValidateDosageAsync`

### 17. 业务约束检查
**代码位置**:
- 各模块BusinessService中的约束验证方法
- `ValidateBusinessRulesAsync` 类型的方法

## 🔄 流程管道 (Pipeline) 触点

### 18. 诊疗流程管理
**代码位置**:
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseBusinessService.cs`
- 流程控制方法：`InitiateWorkflowAsync`, `AdvanceWorkflowAsync`

**WPF UI入口**:
- 诊疗流程进度条
- 流程状态指示器
- "下一步"/"完成"按钮

### 19. 批量操作管道
**代码位置**:
- `src/Server/Modules/LYBT.Module.Users/Services/UserBusinessService.cs:BatchUpdateUsersAsync()`
- `src/Server/Modules/LYBT.Module.Patients/Services/PatientBusinessService.cs:BatchImportPatientsAsync()`

**WebAPI端点**:
- `POST /api/v1/users/batch`
- `POST /api/v1/patients/import`

### 20. 会话生命周期管理
**代码位置**:
- `src/Server/Modules/LYBT.Module.Auth/Services/AuthBusinessService.cs`
- `src/Server/Modules/LYBT.Module.Auth/Repositories/AuthSessionRepository.cs`
- 方法: `CreateSessionAsync`, `ExtendSessionAsync`, `TerminateSessionAsync`

### 21. 事务协调器 (已隔离)
**代码位置**:
- `src/Server/Core/LYBT.Infrastructure/Transactions/` (整个目录)
- `TransactionCoordinator.cs`, `DatabaseTransactionStep.cs`

**状态**: 已在Pass 5中验证隔离，无业务模块依赖

### 22. 异步后台任务
**代码位置**:
- `src/Server/Core/LYBT.Infrastructure/BackgroundTasks/` (如果存在)
- 后台服务相关代码

### 23. 审计日志流水线
**代码位置**:
- `src/Server/Core/LYBT.Infrastructure/Security/SecurityAuditService.cs`
- 方法: `LogOperationAsync`, `LogSecurityEventAsync`

### 24. 数据同步管道
**代码位置**:
- 各模块中的同步相关方法
- `SyncDataAsync` 类型方法

## 📋 测试触点

### 25. 单元测试  
**代码位置**:
- `tests/Units/LYBT.Module.*/Services/` 
- 测试超出Record-Only功能的业务逻辑

### 26. 集成测试
**代码位置**:
- `tests/Integration/` 
- API端点业务逻辑测试

### 27. 架构测试
**代码位置**:
- `tests/Architecture/ArchTests.cs`
- 架构约束验证 (当前有编译错误)

## 🔍 依赖包汇总

**配伍/智能功能相关**:
- Entity Framework Core (所有模块)
- AutoMapper (DTO映射)
- System.Text.Json (序列化)

**规则/认证相关**:
- Microsoft.AspNetCore.Authentication.JwtBearer
- System.IdentityModel.Tokens.Jwt
- Microsoft.AspNetCore.Authorization

**流水线/基础设施相关**:
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Caching.Memory

**WPF/UI相关**:
- Prism.DryIoc
- Microsoft.Toolkit.Wpf.UI.Controls
- System.Windows.Interactivity

## 💡 触点影响分析

### 高影响触点 (谨慎处理)
1. JWT认证系统 - 安全基础，建议保留
2. 用户权限管理 - 影响UI可见性
3. 医案状态管理 - 核心业务流程

### 中影响触点 (可逐步移除)  
1. 配伍检查系统 - 框架存在但实现有限
2. 验方智能推荐 - 可降级为静态模板
3. 批量操作功能 - 可简化为单项操作

### 低影响触点 (可直接移除)
1. 使用统计功能 - 纯分析功能
2. 智能计算功能 - 可改为手动录入
3. 诊断模板 - 可改为自由文本输入

## 总结

共识别30个主要触点，分布在：
- **配伍**: 4个触点 (配伍记录、检查框架、数据模型、UI组件)  
- **智能**: 10个触点 (验方、推荐、模板、计算、统计)
- **规则**: 9个触点 (状态、权限、验证、约束)  
- **流水线**: 7个触点 (流程、批量、会话、任务、审计)

建议按影响程度分阶段处理，优先移除低影响触点，谨慎处理高影响安全相关功能。
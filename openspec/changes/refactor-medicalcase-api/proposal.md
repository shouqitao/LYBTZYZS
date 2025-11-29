# Change: 重构医案模块API端点

## Why

当前医案模块API结构基于Epic #1612建立，但随着业务演进，需要重新整理端点以实现：
1. 完整的医案生命周期管理（新建、查看、修改、暂存、取消）
2. 确保聚合根模式（医案=诊断+处方）的一致性
3. 清理过期和冗余的端点
4. 优化前后端交互流程

## What Changes

### Phase 1: API端点清理与整合
- **BREAKING** 移除冗余的ConsultationController写操作入口（已标记为deleted但代码仍存在）
- **BREAKING** 移除冗余的PrescriptionsController写操作入口（已标记为deleted但代码仍存在）
- 整合MedicalCaseController端点命名规范

### Phase 2: 医案操作流程完善
- 添加"暂存医案"API端点 `PUT /api/v1/medicalcases/{id}/draft`
- 修改"完成看诊"逻辑，支持状态流转验证
- 添加"取消医案"API端点 `PUT /api/v1/medicalcases/{id}/cancel`

### Phase 3: 聚合根服务层重构
- IMedicalCaseService方法签名优化
- 添加SaveDraftAsync方法（保存草稿，不完成）
- 添加CancelAsync方法（取消医案，需审计）
- 优化权限检查逻辑统一到Service层

### Phase 4: 过期代码清理
- 删除ConsultationController中的注释掉的写操作方法
- 删除PrescriptionsController中的注释掉的写操作方法
- 清理不再使用的DTO和Request类型

## Impact

### Affected Specs
- `medicalcase-lifecycle` - 医案生命周期管理
- `medicalcase-edit-modes` - 编辑模式和状态管理

### Affected Code

**Server层**：
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` - 主控制器
- `src/Server/Services/LYBT.WebAPI/Controllers/ConsultationController.cs` - 只读控制器
- `src/Server/Services/LYBT.WebAPI/Controllers/PrescriptionsController.cs` - 只读控制器
- `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseService.cs` - 服务接口
- `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs` - 服务实现

**Client层**（Desktop）：
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseApiService.cs` - API客户端
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseWorkspaceViewModel.cs` - 工作区ViewModel

### Breaking Changes
1. ConsultationController完全变为只读（移除遗留的写操作注释）
2. PrescriptionsController完全变为只读（移除遗留的写操作注释）
3. 所有医案写操作必须通过MedicalCaseController

### Migration Notes
- 客户端代码已使用MedicalCaseController端点，无需迁移
- 如有第三方集成，需更新API文档说明

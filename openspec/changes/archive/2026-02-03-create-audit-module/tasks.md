# Tasks: create-audit-module

## 任务概览

| 阶段 | 任务数 | 预估工时 | 依赖 |
|------|--------|----------|------|
| Phase 1: 基础设施 | 8 | 3h | 无 |
| Phase 2: Server端实现 | 10 | 4h | Phase 1 |
| Phase 3: Client端实现 | 8 | 3h | Phase 2 |
| Phase 4: 业务模块集成 | 6 | 2h | Phase 3 |
| Phase 5: 测试与文档 | 5 | 2h | Phase 4 |
| **总计** | **37** | **14h** | - |

---

## Phase 1: 基础设施

### Task 1.1: 创建项目结构
- **操作**: 创建 `src/Server/Modules/LYBT.Module.Audit/` 目录
- **文件**: LYBT.Module.Audit.csproj
- **验证**: 项目可添加到解决方案

### Task 1.2: 添加项目到解决方案
- **文件**: LYBT.All.sln
- **操作**: 添加LYBT.Module.Audit项目引用
- **验证**: 解决方案可打开

### Task 1.3: 创建AuditLogModel实体
- **文件**: `src/Server/Core/LYBT.Entities/Audit/AuditLogModel.cs`
- **内容**: 定义审计日志实体类
- **验证**: 编译通过

### Task 1.4: 创建EF Configuration
- **文件**: `src/Server/Core/LYBT.Infrastructure/Configurations/AuditLogConfiguration.cs`
- **内容**: 配置表名、索引、字段长度
- **验证**: 编译通过

### Task 1.5: 注册DbSet
- **文件**: `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs`
- **操作**: 添加 `DbSet<AuditLogModel> AuditLogs`
- **验证**: 编译通过

### Task 1.6: 创建数据库迁移
- **命令**: `dotnet ef migrations add AddAuditLogs`
- **验证**: 迁移文件生成正确

### Task 1.7: 创建DTO类
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Audit/`
  - AuditLogDto.cs
  - AuditLogListDto.cs
  - AuditLogCreateDto.cs
- **验证**: 编译通过

### Task 1.8: 创建Mapping Profile
- **文件**: `src/Server/Modules/LYBT.Module.Audit/Mapping/AuditMappingProfile.cs`
- **验证**: 编译通过

---

## Phase 2: Server端实现

### Task 2.1: 定义IAuditService接口
- **文件**: `src/Server/Modules/LYBT.Module.Audit/Interfaces/IAuditService.cs`
- **内容**: LogCreateAsync, LogUpdateAsync, LogDeleteAsync, GetEntityLogsAsync
- **验证**: 编译通过

### Task 2.2: 定义IAuditRepository接口
- **文件**: `src/Server/Modules/LYBT.Module.Audit/Interfaces/IAuditRepository.cs`
- **验证**: 编译通过

### Task 2.3: 实现AuditRepository
- **文件**: `src/Server/Modules/LYBT.Module.Audit/Repositories/AuditRepository.cs`
- **验证**: 编译通过

### Task 2.4: 实现AuditService
- **文件**: `src/Server/Modules/LYBT.Module.Audit/Services/AuditService.cs`
- **内容**:
  - 注入ICurrentUserService获取操作人
  - 使用JsonSerializer序列化快照
- **验证**: 编译通过

### Task 2.5: 创建AuditModule
- **文件**: `src/Server/Modules/LYBT.Module.Audit/AuditModule.cs`
- **内容**: DI注册
- **验证**: 编译通过

### Task 2.6: 创建AuditController
- **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/AuditController.cs`
- **端点**:
  - GET /api/v1/audit/{entityType}/{entityId}
  - GET /api/v1/audit/logs/{logId}
- **验证**: Swagger显示端点

### Task 2.7: 注册模块到WebAPI
- **文件**: `src/Server/Services/LYBT.WebAPI/Program.cs`
- **操作**: 添加AuditModule注册
- **验证**: 应用启动正常

### Task 2.8: 添加Swagger文档
- **文件**: AuditController.cs
- **操作**: 添加XML注释
- **验证**: Swagger文档完整

### Task 2.9: 创建单元测试项目
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Audit.Tests/`
- **验证**: 测试项目可运行

### Task 2.10: 编写AuditService单元测试
- **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.Audit.Tests/Services/AuditServiceTests.cs`
- **验证**: 测试通过

---

## Phase 3: Client端实现

### Task 3.1: 创建IAuditApi接口
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IAuditApi.cs`
- **验证**: 编译通过

### Task 3.2: 注册Refit客户端
- **文件**: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`
- **验证**: 编译通过

### Task 3.3: 创建AuditDiffService
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/AuditDiffService.cs`
- **内容**: JSON diff对比算法
- **验证**: 编译通过

### Task 3.4: 创建AuditLogDialogViewModel
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/AuditLogDialogViewModel.cs`
- **验证**: 编译通过

### Task 3.5: 创建AuditLogDialog视图
- **文件**:
  - `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/AuditLogDialog.xaml`
  - `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Views/AuditLogDialog.xaml.cs`
- **验证**: 编译通过

### Task 3.6: 添加Diff高亮样式
- **文件**: AuditLogDialog.xaml
- **内容**: 新增(绿色)、修改(黄色)、删除(红色)
- **验证**: UI显示正确

### Task 3.7: 注册对话框服务
- **文件**: ServiceCollectionExtensions.cs
- **验证**: 编译通过

### Task 3.8: 添加到ICommonDialogService
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ICommonDialogService.cs`
- **操作**: 添加ShowAuditLogDialogAsync方法
- **验证**: 编译通过

---

## Phase 4: 业务模块集成

### Task 4.1: MedicalCase模块注入IAuditService
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/MedicalCaseModule.cs`
- **验证**: 编译通过

### Task 4.2: MedicalCaseCommandService集成审计
- **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseCommandService.cs`
- **操作**: 在SaveAsync中添加审计调用
- **条件**: 仅Completed状态的医案修改记录审计
- **验证**: 编译通过

### Task 4.3: MedicalCaseInputDto添加AuditReason字段
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseInputDto.cs`
- **验证**: 编译通过

### Task 4.4: Desktop端添加审计原因输入
- **文件**: MedicalCaseWorkspaceViewModel.cs
- **操作**: 已完成医案修改时弹出原因输入框
- **验证**: UI功能正常

### Task 4.5: 医案详情页添加审计日志按钮
- **文件**: MedicalCaseWorkspaceView.xaml
- **操作**: 添加"审计日志"按钮
- **验证**: UI显示正确

### Task 4.6: 绑定审计日志命令
- **文件**: MedicalCaseWorkspaceViewModel.cs
- **操作**: 实现ShowAuditLogCommand
- **验证**: 点击按钮显示审计对话框

---

## Phase 5: 测试与文档

### Task 5.1: 添加集成测试
- **文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/AuditControllerTests.cs`
- **验证**: 测试通过

### Task 5.2: 添加MedicalCase审计集成测试
- **文件**: `tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/MedicalCaseAuditTests.cs`
- **验证**: 测试通过

### Task 5.3: 更新API文档
- **文件**: `docs/reference/api/audit.md`
- **验证**: 文档完整

### Task 5.4: 更新CHANGELOG
- **文件**: `CHANGELOG.md`
- **验证**: 格式正确

### Task 5.5: 更新README
- **文件**: `src/Server/Modules/LYBT.Module.Audit/README.md`
- **验证**: 文档完整

---

## 验收标准

### 编译验收
- [ ] `dotnet build LYBT.All.sln` 无错误

### 功能验收
- [ ] 审计API端点可访问
- [ ] 医案修改后审计记录生成
- [ ] 审计日志对话框正常显示
- [ ] Diff高亮正确

### 测试验收
- [ ] 单元测试通过
- [ ] 集成测试通过

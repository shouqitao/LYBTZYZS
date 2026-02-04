# implement-data-sync Tasks

## Overview

- **变更类型**: Feature（新增功能）
- **风险等级**: Medium
- **影响范围**: Desktop + Server + Shared

## Phase 1: 引用检查实现

### 1.1 Herb 引用检查完善

#### 1.1.1 实现 CheckReferenceAsync 查询逻辑
- **文件**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
- **位置**: 第 500-525 行 `CheckReferenceAsync` 方法
- **变更**: 替换 TODO 注释，实现查询 `PrescriptionItem.HerbId`
- **验证**: 调用方法返回正确的引用计数

#### 1.1.2 注入 DbContext 依赖
- **文件**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
- **变更**: 添加 `ApplicationDbContext` 注入，用于跨聚合查询
- **验证**: DI 容器正确解析

#### 1.1.3 更新 DeleteAsync 添加引用检查
- **文件**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
- **变更**: 删除前调用 CheckReferenceAsync，有引用时返回失败并提示禁用
- **验证**: 尝试删除被引用药材时返回错误

#### 1.1.4 更新 BatchDeleteAsync 添加引用检查
- **文件**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`
- **变更**: 批量删除前检查每条记录的引用
- **验证**: 批量删除正确跳过被引用药材

#### 1.1.5 编译验证
- **命令**: `dotnet build src/Server/Modules/LYBT.Module.Herbs -c Release`
- **验证**: 零编译错误

### 1.2 Patient 引用检查新增

#### 1.2.1 创建 PatientReferenceCheckDto
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientReferenceCheckDto.cs` (新建)
- **变更**: 参考 `HerbReferenceCheckDto` 创建患者引用检查 DTO
- **验证**: 编译通过

#### 1.2.2 创建 MedicalCaseReferenceDto
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientReferenceCheckDto.cs`
- **变更**: 在同文件中添加医案引用详情 DTO
- **验证**: 编译通过

#### 1.2.3 添加 IPatientService 接口方法
- **文件**: `src/Server/Modules/LYBT.Module.Patients/Interfaces/IPatientService.cs`
- **变更**: 添加 `CheckReferenceAsync` 和 `BatchCheckReferenceAsync` 方法签名
- **验证**: 编译通过

#### 1.2.4 实现 PatientService.CheckReferenceAsync
- **文件**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- **变更**: 查询 `MedicalCase.PatientId` 检查引用
- **验证**: 调用方法返回正确的医案计数

#### 1.2.5 实现 PatientService.BatchCheckReferenceAsync
- **文件**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- **变更**: 批量检查患者引用
- **验证**: 批量调用返回正确结果

#### 1.2.6 更新 DeleteAsync 添加引用检查
- **文件**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- **变更**: 删除前检查医案引用
- **验证**: 尝试删除有医案的患者时返回错误

#### 1.2.7 更新 BatchDeleteAsync 添加引用检查
- **文件**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`
- **变更**: 批量删除前检查引用
- **验证**: 批量删除正确跳过有医案的患者

#### 1.2.8 编译验证
- **命令**: `dotnet build src/Server/Modules/LYBT.Module.Patients -c Release`
- **验证**: 零编译错误

## Phase 2: 共享层 DTO

### 2.1 创建 Sync 目录和 DTO

#### 2.1.1 创建 SyncDiffDto 和 DiffType 枚举
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Sync/SyncDiffDto.cs` (新建)
- **变更**: 定义差异类型和差异数据结构
- **验证**: 编译通过

#### 2.1.2 创建 SyncMetadataDto
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Sync/SyncMetadataDto.cs` (新建)
- **变更**: 定义同步元数据 DTO
- **验证**: 编译通过

#### 2.1.3 创建 Compare 请求/响应 DTO
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Sync/SyncCompareDto.cs` (新建)
- **变更**: 定义 SyncCompareRequestDto 和 SyncCompareResponseDto
- **验证**: 编译通过

#### 2.1.4 创建 Upload 请求/响应 DTO
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Sync/SyncUploadDto.cs` (新建)
- **变更**: 定义上传相关 DTO
- **验证**: 编译通过

#### 2.1.5 创建 Download 请求/响应 DTO
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Sync/SyncDownloadDto.cs` (新建)
- **变更**: 定义下载相关 DTO
- **验证**: 编译通过

#### 2.1.6 创建 Delete 请求/响应 DTO
- **文件**: `src/Shared/LYBT.Shared.Models/Contracts/Sync/SyncDeleteDto.cs` (新建)
- **变更**: 定义删除同步相关 DTO
- **验证**: 编译通过

#### 2.1.7 编译验证
- **命令**: `dotnet build src/Shared/LYBT.Shared.Models -c Release`
- **验证**: 零编译错误

## Phase 3: 服务器端同步模块

### 3.1 模块基础结构

#### 3.1.1 创建 LYBT.Module.Sync 项目
- **目录**: `src/Server/Modules/LYBT.Module.Sync/`
- **命令**: `dotnet new classlib -n LYBT.Module.Sync`
- **验证**: 项目创建成功

#### 3.1.2 添加项目引用
- **文件**: `LYBT.Server.sln`
- **变更**: 添加 LYBT.Module.Sync 到解决方案
- **验证**: 解决方案正确识别项目

#### 3.1.3 创建 SyncModule.cs
- **文件**: `src/Server/Modules/LYBT.Module.Sync/SyncModule.cs` (新建)
- **变更**: 实现 IModule 接口，注册服务
- **验证**: 模块正确加载

### 3.2 实体和仓储

#### 3.2.1 创建 SyncMetadata 实体
- **文件**: `src/Server/Core/LYBT.Entities/Sync/SyncMetadata.cs` (新建)
- **变更**: 定义同步元数据实体
- **验证**: 编译通过

#### 3.2.2 更新 ApplicationDbContext
- **文件**: `src/Server/Core/LYBT.Infrastructure/Data/ApplicationDbContext.cs`
- **变更**: 添加 `DbSet<SyncMetadata> SyncMetadata`
- **验证**: DbContext 正确配置

#### 3.2.3 创建 EF Core 迁移
- **命令**: `dotnet ef migrations add AddSyncMetadata`
- **验证**: 迁移文件正确生成

#### 3.2.4 创建 ISyncMetadataRepository
- **文件**: `src/Server/Modules/LYBT.Module.Sync/Repositories/ISyncMetadataRepository.cs` (新建)
- **变更**: 定义仓储接口
- **验证**: 编译通过

#### 3.2.5 实现 SyncMetadataRepository
- **文件**: `src/Server/Modules/LYBT.Module.Sync/Repositories/SyncMetadataRepository.cs` (新建)
- **变更**: 实现仓储
- **验证**: 编译通过

### 3.3 服务层

#### 3.3.1 创建 ChecksumHelper
- **文件**: `src/Server/Modules/LYBT.Module.Sync/Helpers/ChecksumHelper.cs` (新建)
- **变更**: 实现 SHA256 Checksum 计算
- **验证**: 单元测试通过

#### 3.3.2 创建 ISyncService 接口
- **文件**: `src/Server/Modules/LYBT.Module.Sync/Services/ISyncService.cs` (新建)
- **变更**: 定义服务接口
- **验证**: 编译通过

#### 3.3.3 实现 SyncService
- **文件**: `src/Server/Modules/LYBT.Module.Sync/Services/SyncService.cs` (新建)
- **变更**: 实现同步服务逻辑
- **验证**: 编译通过

### 3.4 API 控制器

#### 3.4.1 创建 SyncController
- **文件**: `src/Server/Modules/LYBT.Module.Sync/Controllers/SyncController.cs` (新建)
- **变更**: 实现所有 API 端点
- **验证**: Swagger 正确显示端点

#### 3.4.2 编译验证
- **命令**: `dotnet build src/Server -c Release`
- **验证**: Server 解决方案零编译错误

## Phase 4: 客户端同步模块

### 4.1 LocalData 扩展

#### 4.1.1 创建 SyncLog 实体
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Entities/SyncLog.cs` (新建)
- **变更**: 定义 SQLite 同步日志实体
- **验证**: 编译通过

#### 4.1.2 更新 LocalDbContext
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Context/LocalDbContext.cs`
- **变更**: 添加 `DbSet<SyncLog> SyncLogs`
- **验证**: DbContext 正确配置

#### 4.1.3 创建 SQLite 迁移
- **命令**: 手动添加迁移脚本或使用 EF Core
- **验证**: 迁移正确应用

#### 4.1.4 创建 ISyncLogRepository
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Repositories/ISyncLogRepository.cs` (新建)
- **变更**: 定义仓储接口
- **验证**: 编译通过

#### 4.1.5 实现 SyncLogRepository
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Repositories/SyncLogRepository.cs` (新建)
- **变更**: 实现仓储
- **验证**: 编译通过

### 4.2 Contracts 层

#### 4.2.1 创建 ISyncService 接口
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISyncService.cs` (新建)
- **变更**: 定义客户端同步服务接口
- **验证**: 编译通过

#### 4.2.2 创建 ISyncApiClient 接口
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/ISyncApiClient.cs` (新建)
- **变更**: 定义 API 客户端接口
- **验证**: 编译通过

### 4.3 Infrastructure 层

#### 4.3.1 创建 ChecksumHelper (客户端版)
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Helpers/ChecksumHelper.cs` (新建)
- **变更**: 与服务器端保持一致的实现
- **验证**: 编译通过

#### 4.3.2 实现 SyncApiClient
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SyncApiClient.cs` (新建)
- **变更**: 实现 HTTP 调用
- **验证**: 编译通过

#### 4.3.3 实现 SyncService
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/SyncService.cs` (新建)
- **变更**: 实现同步逻辑
- **验证**: 编译通过

#### 4.3.4 注册 DI 服务
- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DependencyInjection.cs`
- **变更**: 注册 ISyncService 和 ISyncApiClient
- **验证**: DI 正确解析

#### 4.3.5 编译验证
- **命令**: `dotnet build src/Client/Desktop/Core -c Release`
- **验证**: 零编译错误

## Phase 5: 同步 UI

### 5.1 模块结构

#### 5.1.1 创建 LYBT.Desktop.Sync 项目
- **目录**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/`
- **命令**: `dotnet new wpflib -n LYBT.Desktop.Sync`
- **验证**: 项目创建成功

#### 5.1.2 添加项目引用
- **文件**: `LYBT.Desktop.sln`
- **变更**: 添加到解决方案
- **验证**: 解决方案正确识别项目

#### 5.1.3 创建 SyncModule.cs
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/SyncModule.cs` (新建)
- **变更**: 实现 Prism IModule
- **验证**: 模块正确加载

### 5.2 ViewModel

#### 5.2.1 创建 SyncViewModel
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs` (新建)
- **变更**: 实现同步主界面 ViewModel
- **验证**: 编译通过

#### 5.2.2 创建 ConflictResolutionViewModel
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/ConflictResolutionViewModel.cs` (新建)
- **变更**: 实现冲突处理 ViewModel
- **验证**: 编译通过

### 5.3 View

#### 5.3.1 创建 SyncView.xaml
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Views/SyncView.xaml` (新建)
- **变更**: 实现同步主界面
- **验证**: XAML 编译通过

#### 5.3.2 创建 ConflictResolutionDialog.xaml
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Sync/Views/ConflictResolutionDialog.xaml` (新建)
- **变更**: 实现冲突处理弹窗
- **验证**: XAML 编译通过

#### 5.3.3 添加导航菜单入口
- **文件**: Shell 或设置模块
- **变更**: 添加"数据同步"菜单项
- **验证**: 可导航到同步页面

#### 5.3.4 编译验证
- **命令**: `dotnet build src/Client/Desktop -c Release`
- **验证**: Desktop 解决方案零编译错误

## Phase 6: 测试与验证

### 6.1 单元测试

#### 6.1.1 ChecksumHelper 测试
- **文件**: 创建测试项目
- **测试**: 相同数据产生相同 Checksum
- **验证**: 测试通过

#### 6.1.2 引用检查测试
- **测试**: Herb/Patient 引用检查正确性
- **验证**: 测试通过

### 6.2 集成测试

#### 6.2.1 SyncController API 测试
- **测试**: 所有 API 端点正常响应
- **验证**: 测试通过

### 6.3 文档更新

#### 6.3.1 更新 CHANGELOG
- **文件**: `CHANGELOG.md`
- **变更**: 记录新功能

#### 6.3.2 归档 OpenSpec 提案
- **操作**: 将提案移动到 archive 目录

## Dependencies

```
Phase 1 ─────────────────────┐
                             │
Phase 2 ─────────────────────┤
                             │
Phase 3 ─────────────────────┼──> Phase 4 ──> Phase 5 ──> Phase 6
```

- Phase 1 (引用检查) 可独立完成
- Phase 2 (共享 DTO) 需先于 Phase 3、4
- Phase 3 (Server) 需先于 Phase 4 (Client)
- Phase 4 (Client) 需先于 Phase 5 (UI)

## Validation Checklist

- [x] Server 解决方案编译通过
- [x] Desktop 解决方案编译通过
- [x] 引用检查正确阻止非法删除
- [x] 同步 API 端点正常响应
- [x] 同步 UI 可正常操作
- [x] 冲突处理界面正常
- [x] 单元测试通过 (37/37)

---

**生成时间**: 2026-02-04
**完成时间**: 2026-02-04
**状态**: 已完成

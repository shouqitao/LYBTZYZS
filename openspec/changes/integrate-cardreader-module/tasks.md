# integrate-cardreader-module Tasks

## Overview

- **变更类型**: Feature
- **风险等级**: Low
- **预估工作量**: 4-6小时

## Phase 1: 基础集成（解决方案+Shell）

### 1.1 添加CardReader到解决方案

- **文件**: `LYBT.Desktop.sln`
- **变更**: 添加CardReader项目到Modules文件夹
- **验证**: VS打开解决方案可见CardReader项目

### 1.2 Shell添加项目引用

- **文件**: `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
- **变更**: 在Business Modules ItemGroup添加CardReader引用
- **验证**: 编译通过

### 1.3 注册CardReaderModule

- **文件**: `src/Client/Desktop/Shell/App.xaml.cs`
- **变更**:
  - 添加 `using LYBT.Desktop.CardReader;`
  - 在ConfigureModuleCatalog添加 `moduleCatalog.AddModule<CardReaderModule>`
- **验证**: 应用启动时CardReaderModule被加载

### 1.4 Phase 1编译验证

- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 2: Patients模块实现集成接口

### 2.1 创建PatientCardReaderIntegration服务

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PatientCardReaderIntegration.cs` (新建)
- **变更**: 实现IPatientCardReaderIntegration接口
- **依赖**: IPatientRepository, IPatientService

### 2.2 扩展Repository支持身份证号查询

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientRepository.cs`
- **变更**: 添加 `Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber);`
- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories/PatientRepository.cs`
- **变更**: 实现GetByIdNumberAsync（调用API或SearchAsync过滤）

### 2.3 实现FindPatientByIdNumberAsync

- **文件**: `PatientCardReaderIntegration.cs`
- **变更**: 调用Repository查询患者，返回PatientFromCardResult

### 2.4 实现QuickCreatePatientAsync（预填表单模式）

- **文件**: `PatientCardReaderIntegration.cs`
- **变更**:
  - 从CardReadResult构建PatientInputDto（预填可用字段）
  - 返回需用户确认的DTO标记（IsPreFilled=true）
  - 实际创建需用户UI确认后执行

### 2.5 实现FindOrCreatePatientAsync

- **文件**: `PatientCardReaderIntegration.cs`
- **变更**: 组合FindPatientByIdNumberAsync和QuickCreatePatientAsync

### 2.6 注册集成服务

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/PatientsModule.cs`
- **变更**: 注册 `IPatientCardReaderIntegration -> PatientCardReaderIntegration`

### 2.7 Patients模块添加CardReader引用

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/LYBT.Desktop.Patients.csproj`
- **变更**: 添加CardReader项目引用（用于IPatientCardReaderIntegration接口）

### 2.8 Phase 2编译验证

- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 3: 就诊工作台UI集成

### 3.1 创建读卡器状态控件

- **文件**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/CardReaderStatusControl.xaml` (新建)
- **变更**: 显示读卡器连接状态、读卡按钮、自动读卡开关
- **样式**: 使用UnifiedDesignSystem样式

### 3.2 MedicalCaseWorkspaceView添加读卡器区域

- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`
- **变更**: 在左侧面板（患者信息卡片上方或下方）添加CardReaderStatusControl

### 3.3 MedicalCaseWorkspaceViewModel添加读卡器逻辑

- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- **变更**:
  - 注入ICardReaderService
  - 添加CardReaderStatus属性
  - 添加InitializeCardReaderCommand
  - 添加ManualReadCardCommand
  - 添加ToggleAutoReadCommand
  - 处理CardReadCompleted事件

### 3.4 创建CardReaderWorkspaceHandler

- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Handlers/CardReaderWorkspaceHandler.cs` (新建)
- **变更**: 提取读卡器相关逻辑（遵循SRP）
- **依赖**: ICardReaderService, IPatientCardReaderIntegration

### 3.5 自动读卡模式实现

- **文件**: `CardReaderWorkspaceHandler.cs`
- **变更**:
  - 支持启动/停止自动读卡
  - 读卡成功后调用FindOrCreatePatientAsync
  - 自动导航到患者医案或显示预填表单

### 3.6 Clinical模块添加CardReader引用

- **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/LYBT.Desktop.Clinical.csproj`
- **变更**: 添加CardReader项目引用

### 3.7 Phase 3编译验证

- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Phase 4: 患者管理页读卡支持（可选）

### 4.1 PatientMasterDetailViewModel添加读卡支持

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
- **变更**:
  - 添加ReadCardCommand
  - 读卡后预填当前编辑表单或跳转到已存在患者

### 4.2 PatientMasterDetailControl添加读卡按钮

- **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
- **变更**: 在工具栏添加"刷卡录入"按钮

### 4.3 Phase 4编译验证

- 运行 `dotnet build LYBT.Desktop.sln -c Release --no-restore`
- 确保零编译错误

## Dependencies

```
Phase 1 ────────────────────┐
(基础集成)                   │
                            │
Phase 2 ────────────────────┼──> Phase 3 ──> Phase 4
(Patients实现接口)            │    (就诊工作台)  (患者管理)
                            │
                            │
                            ▼
                        完成集成
```

Phase 2和Phase 3可并行开发，但Phase 3依赖Phase 2的接口实现。
Phase 4为可选增强，可延后执行。

## Validation Checklist

- [ ] Desktop解决方案编译通过
- [ ] CardReaderModule在应用启动时加载
- [ ] MockCardReader模式可正常读卡（开发测试）
- [ ] FindPatientByIdNumberAsync返回正确结果
- [ ] 就诊工作台显示读卡器状态
- [ ] 自动读卡模式可开关
- [ ] 读卡后能正确导航到患者/显示预填表单

## Notes

1. HDstdapi.dll需要随应用部署，建议放置在Native子目录
2. MockCardReader在DEBUG模式下作为Fallback
3. 自动读卡默认关闭，需用户手动开启
4. 预填表单模式需要新的Dialog或修改现有PatientEditControl

---

**生成时间**: 2026-01-20
**状态**: 草稿（待设计阶段细化）

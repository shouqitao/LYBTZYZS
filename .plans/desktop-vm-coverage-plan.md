# Desktop ViewModel 测试全覆盖计划

## 目标
完成 LYBTZYZS 项目 Desktop 层所有 ViewModel 的测试覆盖，实现 100% 单元测试覆盖。

## 背景知识（来自 SuperMemory）
- **ViewModel 总数**: 34个
- **有单元测试**: 3个 (9%) - LoginViewModel, PatientMasterDetailViewModel, MedicalCaseMasterDetailViewModel
- **完全无测试**: 15个 (44%)
- **部分测试**: 19个 (56%)
- **测试基类**: UserJourneyTestBase (SQLite InMemory + NSubstitute)

## 优先级划分

### P0 - 核心业务 (12个)
1. MedicalCaseWorkspaceViewModel - 医案工作区
2. PrescriptionEditorViewModel - 处方编辑器
3. ConsultationEditorViewModel - 诊断编辑器
4. RegistrationMasterDetailViewModel - 挂号管理
5. PatientMasterDetailViewModel - 患者管理
6. HerbMasterDetailViewModel - 药材管理
7. FormulaMasterDetailViewModel - 方剂管理
8. UserManagementViewModel - 用户管理
9. RegistrationDialogViewModel - 挂号对话框
10. PrescriptionDialogViewModel - 处方对话框
11. ConsultationDialogViewModel - 诊断对话框
12. MedicalCaseSummaryViewModel - 医案摘要

### P1 - 支持功能 (4个)
1. ImportExportViewModel - 导入导出
2. CardReaderViewModel - 读卡器
3. SyncViewModel - 同步
4. FormulaImportDialogViewModel - 方剂导入

### P2 - 辅助功能 (18个)
Shell/Admin/Clinical/Sync 相关 ViewModel

## 执行策略

### Phase 1: P0 核心 ViewModel (第1天)
- [ ] MedicalCaseWorkspaceViewModelTests
- [ ] PrescriptionEditorViewModelTests
- [ ] ConsultationEditorViewModelTests
- [ ] RegistrationMasterDetailViewModelTests

### Phase 2: P0 管理模块 (第1天)
- [ ] PatientMasterDetailViewModelTests (补充)
- [ ] HerbMasterDetailViewModelTests
- [ ] FormulaMasterDetailViewModelTests
- [ ] UserManagementViewModelTests

### Phase 3: P0 对话框 (第2天)
- [ ] RegistrationDialogViewModelTests
- [ ] PrescriptionDialogViewModelTests
- [ ] ConsultationDialogViewModelTests
- [ ] MedicalCaseSummaryViewModelTests

### Phase 4: P1 支持功能 (第2天)
- [ ] ImportExportViewModelTests
- [ ] CardReaderViewModelTests
- [ ] SyncViewModelTests

### Phase 5: P2 辅助功能 (第3天)
- [ ] Shell/Admin/Clinical ViewModel 测试
- [ ] 对话框 ViewModel 测试

### Phase 6: 验证和优化 (第3天)
- [ ] 运行所有测试
- [ ] 检查代码覆盖率
- [ ] 修复失败测试

## 测试模式（从现有测试提取）

### 标准 ViewModel 测试结构
```csharp
public class XxxViewModelTests : UserJourneyTestBase
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        // Arrange
        var vm = CreateViewModel();
        
        // Assert
        vm.Property.Should().Be(expected);
    }
    
    [Fact]
    public async Task Command_ExecutesSuccessfully()
    {
        // Arrange
        var vm = CreateViewModel();
        
        // Act
        await vm.Command.ExecuteAsync();
        
        // Assert
        vm.Property.Should().Be(expected);
    }
}
```

## 文件创建清单

### Phase 1
- [ ] tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseWorkspaceViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/PrescriptionEditorViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/ConsultationEditorViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/Registration/RegistrationMasterDetailViewModelTests.cs

### Phase 2
- [ ] tests/LYBT.Tests.Desktop/PureLogic/Patients/PatientMasterDetailViewModelTests.cs (补充)
- [ ] tests/LYBT.Tests.Desktop/PureLogic/Herbs/HerbMasterDetailViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/Formula/FormulaMasterDetailViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/Users/UserManagementViewModelTests.cs

### Phase 3
- [ ] tests/LYBT.Tests.Desktop/PureLogic/Registration/RegistrationDialogViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/PrescriptionDialogViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/ConsultationDialogViewModelTests.cs
- [ ] tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/MedicalCaseSummaryViewModelTests.cs

## 测试基类信息

**UserJourneyTestBase** 提供：
- SQLite InMemory 数据库
- 依赖注入容器
- Mock 服务设置
- 测试数据构建器

**关键 Mock 服务**：
- IRegionManager
- IDialogService
- IEventAggregator
- IRepository<T>
- IApplicationStateService

## 当前状态
- **开始时间**: 2025-04-07
- **当前 Phase**: 1
- **已完成**: 0/34 ViewModel

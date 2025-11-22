# 患者选择模块优化需求讨论

**版本**: v2.0
**创建日期**: 2025-11-22
**状态**: 📝 需求讨论 + 🔴 P0严重bug修复
**基于**: [PatientSelection-MedicalCaseFlow整合方案评审-2025-11-22](Graphiti记忆)
**相关Epic**: 待创建
**相关Issues**: 待创建

---

## 📋 需求概述

### 业务目标
基于PatientSelection+MedicalCaseFlow整合方案评审报告，优化患者选择模块的**稳定性、可靠性和用户体验**，修复关键缺陷（P0患者安全问题），改进资源管理（P1内存泄漏），提升用户体验（P2交互优化）。

### 目标用户
- **主要用户**: 医生
- **使用场景**: 传统中医诊所（2-5医生，20-50患者/天）

### 核心场景
1. **患者选择安全性**：医生在双列表（全部患者+待诊队列）中选择患者时，确保只选中一个患者，避免医案绑定错误
2. **异常恢复能力**：待诊队列加载失败时，医生能收到明确提示并继续使用其他功能
3. **资源管理优化**：系统长时间运行不出现内存泄漏
4. **用户体验提升**：操作成功反馈、手动刷新队列、空状态友好提示

---

## ✨ 功能性需求

### FR-001: 双列表互斥选择（P0 - Critical）

**User Story**:
```
作为 医生
我想要 在选择患者时确保只能选中一个列表的患者
以便 避免医案绑定到错误患者，保障患者安全
```

**验收标准**:
- [x] 点击"全部患者"列表时，自动取消"待诊队列"的选择
- [x] 点击"待诊队列"列表时，自动取消"全部患者"的选择
- [x] 同一时刻只有一个患者被选中（SelectedPatient 与 SelectedPendingPatient 互斥）
- [x] CurrentPatient属性始终指向当前选中的患者
- [x] 选择切换响应时间<100ms（即时响应）

**优先级**: 🔴 P0（患者安全，必须立即修复）

**工时估算**: 1小时

---

### FR-002: 异常处理优化（P0 - Critical）

**User Story**:
```
作为 医生
我想要 在待诊队列加载失败时收到明确提示
以便 知道如何处理并继续使用系统
```

**验收标准**:
- [x] 待诊队列加载失败时显示错误消息（MessageBox或StatusBar）
- [x] 错误消息包含操作建议（如"请手动刷新"或"请检查网络连接"）
- [x] 异常被完整记录到日志（LogError，包含堆栈信息）
- [x] 加载失败不影响其他功能的使用（全部患者列表仍可用）
- [x] 不使用Fire-and-forget模式（`_ = AsyncMethod()`），改用`await`或正确的异步处理

**优先级**: 🔴 P0（系统可靠性，必须立即修复）

**工时估算**: 1小时

---

### FR-003: 资源管理优化（P1 - High）

**User Story**:
```
作为 系统
我需要 在ViewModel销毁时正确释放资源
以便 避免内存泄漏，确保系统长时间稳定运行
```

**验收标准**:
- [x] PatientSelectionViewModel实现IDisposable接口
- [x] Dispose方法清理Timer（如果未来添加自动刷新）
- [x] Dispose方法取消EventAggregator订阅（避免事件订阅泄漏）
- [x] Dispose方法记录日志（LogInformation: "PatientSelectionViewModel disposed"）
- [x] 遵循标准Dispose模式（Dispose(bool disposing) + GC.SuppressFinalize）

**优先级**: 🟡 P1（资源管理，重要）

**工时估算**: 2小时

---

### FR-004: 操作成功反馈（P1 - High）

**User Story**:
```
作为 医生
我想要 在选择患者并创建医案后收到成功反馈
以便 确认操作已完成
```

**验收标准**:
- [x] 创建新医案成功后显示反馈（StatusBar或简单Toast）
- [x] 反馈内容包含患者姓名（如"已为张三创建新医案"）
- [x] 反馈自动消失（3秒）或允许用户关闭
- [x] 操作记录到日志（LogInformation）
- [x] 不引入复杂的第三方Toast库（MVP阶段使用StatusBar或MessageBox）

**优先级**: 🟡 P1（用户体验，重要）

**工时估算**: 2小时

---

### FR-005: 分页大小优化（P2 - Medium）

**User Story**:
```
作为 医生
我想要 每页显示更多患者
以便 减少翻页次数，提高查找效率
```

**验收标准**:
- [x] PageSize从20调整为50
- [x] 代码注释与实现保持一致（移除"50"注释或更新实现）
- [x] 不影响性能（加载时间仍<500ms）
- [x] 测试验证：50条患者数据加载正常

**优先级**: 🟢 P2（性能优化，建议）

**工时估算**: 0.5小时

---

### FR-006: 手动刷新队列（P2 - Medium）

**User Story**:
```
作为 医生
我想要 手动刷新待诊队列
以便 获取最新的患者信息（如前台新挂号的患者）
```

**验收标准**:
- [x] 待诊队列标题栏添加"刷新"按钮（图标：🔄）
- [x] 点击按钮重新调用LoadPendingCasesAsync
- [x] 刷新过程中显示加载状态（IsRefreshing属性，禁用按钮）
- [x] 刷新失败显示错误提示（复用FR-002的异常处理）
- [x] 刷新成功后更新PendingPatients集合

**优先级**: 🟢 P2（用户体验，建议）

**工时估算**: 2小时

---

### FR-007: 空状态UI（P2 - Medium）

**User Story**:
```
作为 医生
我想要 在待诊队列为空时看到友好提示
以便 知道当前没有待诊患者，避免疑惑
```

**验收标准**:
- [x] 队列为空时显示空状态UI（图标+文字）
- [x] 图标：📋 或类似
- [x] 文字提示：
  - 主标题："暂无待诊患者"
  - 副标题："从左侧选择患者或等待新的挂号"
- [x] 有患者时自动隐藏空状态UI
- [x] 空状态UI居中显示，样式友好

**优先级**: 🟢 P2（用户体验，建议）

**工时估算**: 2小时

---

## 🔒 非功能性需求

### NFR-001: 性能要求

- **患者列表加载**：<500ms（PageSize=50，约100条以内）
- **待诊队列刷新**：<300ms（缓存优先策略，UnfinishedCaseHandler）
- **双列表选择切换**：<100ms（即时响应，UI线程同步操作）
- **内存占用**：不随使用时间增长（正确实现IDisposable）

### NFR-002: 安全要求

- **患者数据访问**：基于SessionManager的权限验证（仅Doctor角色）
- **日志脱敏**：不记录患者敏感信息（身份证、电话号码）
- **异常处理**：所有异常必须捕获、记录、向用户反馈
- **数据完整性**：双列表互斥确保医案绑定正确（患者安全）

### NFR-003: 可用性要求

- **错误提示**：清晰、具体、包含操作建议（如"请手动刷新"）
- **成功反馈**：及时、明确、自动消失（3秒）
- **空状态UI**：友好、有指导意义
- **操作响应**：即时、流畅、无卡顿

### NFR-004: 可维护性要求

- **代码注释**：与实现保持一致（PageSize注释修复）
- **资源管理**：实现IDisposable模式，遵循.NET最佳实践
- **日志记录**：关键操作和异常都记录（ILogger）
- **单元测试**：覆盖所有P0/P1功能（测试覆盖率>80%）

---

## 📐 业务规则

### BR-001: 患者选择互斥性

- **规则**: 同一时刻只能选中一个列表的患者
- **理由**: 避免医案绑定到错误患者，保障患者安全（P0级别）
- **实现**: SelectedPatient和SelectedPendingPatient属性setter中相互清除
- **代码位置**: PatientSelectionViewModel.cs:79-113

**实现示例**:
```csharp
public PatientDto? SelectedPatient
{
    get => _selectedPatient;
    set
    {
        if (SetProperty(ref _selectedPatient, value))
        {
            if (value != null)
            {
                // ✅ 清除待诊队列选择
                _selectedPendingPatient = null;
                RaisePropertyChanged(nameof(SelectedPendingPatient));
                CurrentPatient = value;
            }
            SelectPatientCommand.RaiseCanExecuteChanged();
        }
    }
}
```

---

### BR-002: 待诊队列可见性

- **规则**: 待诊队列仅显示有未完成医案的患者
- **理由**: 待诊队列是候诊患者的快捷入口，与挂号系统集成
- **实现**: UnfinishedCaseHandler.GetAllUnfinishedCasesAsync()
- **注意**: 当前未实现前台挂号系统集成（Phase 3扩展）

---

### BR-003: 患者选择权限

- **规则**: 所有医生角色都可以选择患者
- **理由**: 患者选择是诊疗的第一步，所有医生都需要
- **实现**: SessionManager验证Doctor角色（已实现）
- **扩展**: 未来可添加按医生筛选队列（多医生场景）

---

### BR-004: 未完成医案处理

- **规则**: 选择有未完成医案的患者时，提供三选项对话（继续看诊/新建医案/仅关闭）
- **理由**: 给予医生明确选择权，符合中医诊疗灵活性
- **实现**: UnfinishedCaseHandler检测 + ShowThreeOptionDialog
- **保持**: 该规则已在当前实现中，本次优化不修改

---

### BR-005: 异常恢复策略

- **规则**: 待诊队列加载失败时不影响全部患者列表的使用
- **理由**: 确保核心功能（患者选择）始终可用，提升系统健壮性
- **实现**: try-catch隔离异常，显示错误提示但不阻断流程
- **代码位置**: PatientSelectionViewModel.cs:OnNavigatedTo方法

**实现示例**:
```csharp
public override async void OnNavigatedTo(NavigationContext navigationContext)
{
    try
    {
        await LoadPendingCasesAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载待诊队列失败");
        await ShowErrorMessageAsync("加载待诊队列失败，请手动刷新");
        // 不抛出异常，允许继续使用全部患者列表
    }
}
```

---

## 🗃️ 数据模型

### ViewModel属性调整

**现有属性（保持不变）**:
```csharp
public class PatientSelectionViewModel : NavigationViewModelBase
{
    // 现有属性
    private PatientDto? _selectedPatient;          // 全部患者列表选中项
    private PatientDto? _selectedPendingPatient;   // 待诊队列列表选中项
    private PatientDto? _currentPatient;           // 当前患者（统一）
    private ObservableCollection<PatientDto> _patients;         // 全部患者
    private ObservableCollection<UnfinishedCaseDto> _pendingPatients;  // 待诊队列

    // ... 其他属性
}
```

**新增属性（FR-006/FR-007需要）**:
```csharp
// FR-006: 手动刷新队列
private bool _isRefreshing;
public bool IsRefreshing
{
    get => _isRefreshing;
    set => SetProperty(ref _isRefreshing, value);
}

// FR-007: 空状态UI
public bool HasNoPendingPatients => PendingPatients?.Count == 0;
// 在PendingPatients变更时触发 RaisePropertyChanged(nameof(HasNoPendingPatients))
```

**修改属性（FR-001需要）**:
```csharp
// SelectedPatient和SelectedPendingPatient的setter中添加互斥逻辑
// 详见BR-001实现示例
```

### Shared层DTO（无需修改）

- `PatientDto`: 保持不变
- `UnfinishedCaseDto`: 保持不变
- `MedicalCaseDto`: 保持不变

---

## 🏗️ 架构约束

### 技术栈限制（基于MVP Constitution）

✅ **允许使用**:
- Client端框架：WPF + Prism.DryIoc 9.0
- 架构模式：MVVM + 组件化（Manager模式）
- UI库：标准WPF控件（ListBox、Button、TextBlock等）
- 日志：ILogger（Microsoft.Extensions.Logging）
- 导航：Prism RegionManager + NavigationService

❌ **禁止使用**:
- 第三方UI库：MaterialDesignInXamlToolkit、MahApps.Metro等（MVP阶段）
- 第三方Toast库：ToastNotifications.Messages.Wpf等（MVP阶段，使用StatusBar替代）
- Messenger模式：改用Prism EventAggregator
- Web前端技术栈：Electron、Blazor等

### 架构层分配

**Client端（本次优化重点）**:
- `PatientSelectionViewModel.cs`（核心修改）
  - FR-001: 双列表互斥逻辑
  - FR-002: 异常处理优化
  - FR-003: IDisposable实现
  - FR-004: 成功反馈
  - FR-005: PageSize调整
  - FR-006: 刷新队列Command
- `PatientSelectionView.xaml`（UI修改）
  - FR-006: 刷新按钮UI
  - FR-007: 空状态UI
- `组件`（保持不变）
  - PatientSearchManager
  - UnfinishedCaseHandler
  - PendingQueueManager

**Shared层（无需修改）**:
- PatientDto
- UnfinishedCaseDto

**Server端（无需修改）**:
- 本次优化不涉及Server端

### 模块定位

- **所属模块**: Patients模块
- **命名空间**: `LYBT.Desktop.Patients.ViewModels`、`LYBT.Desktop.Patients.Views`
- **项目文件**: `LYBT.Desktop.Patients.csproj`

---

## 🧪 测试策略

### 单元测试（P0/P1必须，覆盖率>80%）

#### 测试1: 双列表互斥逻辑（FR-001）
```csharp
[Fact]
public void SelectedPatient_ShouldClearSelectedPendingPatient()
{
    // Arrange
    var viewModel = CreateViewModel();
    var pendingPatient = CreateTestPatient("pending");
    var regularPatient = CreateTestPatient("regular");
    viewModel.SelectedPendingPatient = pendingPatient;

    // Act
    viewModel.SelectedPatient = regularPatient;

    // Assert
    Assert.Null(viewModel.SelectedPendingPatient);
    Assert.Equal(regularPatient, viewModel.CurrentPatient);
}

[Fact]
public void SelectedPendingPatient_ShouldClearSelectedPatient()
{
    // 对称测试
}
```

#### 测试2: 异常处理（FR-002）
```csharp
[Fact]
public async Task OnNavigatedTo_ShouldHandleLoadPendingCasesException()
{
    // Arrange
    var mockHandler = new Mock<IUnfinishedCaseHandler>();
    mockHandler.Setup(x => x.GetAllUnfinishedCasesAsync())
        .ThrowsAsync(new Exception("Database connection failed"));
    var viewModel = CreateViewModel(mockHandler.Object);

    // Act
    await viewModel.OnNavigatedToAsync(new NavigationContext());

    // Assert - 应该记录日志且不崩溃
    _mockLogger.Verify(x => x.LogError(
        It.IsAny<Exception>(),
        It.Is<string>(s => s.Contains("加载待诊队列失败"))
    ), Times.Once);
}
```

#### 测试3: IDisposable（FR-003）
```csharp
[Fact]
public void Dispose_ShouldClearEventSubscriptions()
{
    // Arrange
    var viewModel = CreateViewModel();
    // 订阅事件

    // Act
    viewModel.Dispose();

    // Assert
    _mockLogger.Verify(x => x.LogInformation(
        It.Is<string>(s => s.Contains("PatientSelectionViewModel disposed"))
    ), Times.Once);
}
```

### 集成测试（P1推荐）

#### 测试4: 患者选择到医案流程集成（FR-001）
```csharp
[Fact]
public async Task PatientSelection_To_MedicalCaseFlow_Integration()
{
    // Arrange
    var patient = CreateTestPatient();
    await InsertTestPatientAsync(patient);

    // Act - 选择患者
    _patientSelectionViewModel.SelectedPatient = patient;
    await _patientSelectionViewModel.SelectPatientCommand.ExecuteAsync();

    // Assert - 验证导航参数
    Assert.NotNull(_navigationContext.Parameters["MedicalCaseId"]);
    Assert.Equal(patient, _navigationContext.Parameters["CurrentPatient"]);
}
```

### 用户测试（P2必须）

#### 测试场景1: 双列表互斥
1. 点击"全部患者"列表中的患者A
2. 验证患者A被选中
3. 点击"待诊队列"列表中的患者B
4. 验证患者B被选中，患者A自动取消选中

#### 测试场景2: 异常恢复
1. 断开网络连接
2. 导航到PatientSelectionView
3. 验证显示错误提示（如"加载待诊队列失败，请手动刷新"）
4. 验证"全部患者"列表仍可正常使用

#### 测试场景3: 成功反馈
1. 选择患者并创建新医案
2. 验证StatusBar或Toast显示成功消息（如"已为张三创建新医案"）
3. 验证3秒后消息自动消失

#### 测试场景4: 空状态UI
1. 清空待诊队列数据（模拟无患者场景）
2. 验证显示空状态UI（图标+文字）
3. 添加一个患者到待诊队列
4. 验证空状态UI自动隐藏

---

## 📅 实施路线图

### Phase 1: P0修复（Critical，必须立即实施）

**目标**:
1. 修复医案创建DoctorId严重bug（阻塞性）
2. 修复患者安全和系统可靠性问题

**任务清单**:

#### 1.1 医案创建DoctorId Bug修复（4小时）
- [ ] **代码修复**（1.5小时）
  - [ ] MedicalCaseService.CreateAsync添加doctorId参数
  - [ ] Controller使用GetOperator()提取当前用户ID
  - [ ] 通过PatientId查询Patient获取PatientName
  - [ ] 通过doctorId查询User获取DoctorName
- [ ] **数据迁移脚本**（1.5小时）
  - [ ] UPDATE历史医案：使用CreatedBy字段推断DoctorId
  - [ ] 验证数据完整性（无Guid.Empty残留）
- [ ] **数据库约束**（1小时）
  - [ ] 添加CHECK约束：DoctorId != '00000000-0000-0000-0000-000000000000'
  - [ ] 验证约束生效

#### 1.2 患者选择优化（3小时）
- [ ] FR-001: 双列表互斥选择（1小时）
  - [ ] 修改SelectedPatient属性setter
  - [ ] 修改SelectedPendingPatient属性setter
  - [ ] 验证CurrentPatient正确性
- [ ] FR-002: 异常处理优化（1小时）
  - [ ] 修改OnNavigatedTo方法（使用await + try-catch）
  - [ ] 添加ShowErrorMessageAsync调用
  - [ ] 验证日志记录
- [ ] 单元测试（P0部分）（1小时）
  - [ ] 测试双列表互斥逻辑
  - [ ] 测试异常处理流程

**工时估算**: 7小时（1个工作日）

**验收标准**:
- ✅ 所有历史医案DoctorId != Guid.Empty
- ✅ 新建医案正确设置DoctorId、DoctorName、PatientName
- ✅ CHECK约束阻止Guid.Empty写入
- ✅ 双列表互斥逻辑单元测试通过
- ✅ 异常处理单元测试通过
- ✅ 用户测试场景1和2通过

---

### Phase 2: P1改进 + 统一用户上下文模式（High，重要）

**目标**:
1. 统一Controller-Service用户上下文传递模式
2. 改进资源管理和用户体验

**任务清单**:

#### 2.1 统一用户上下文模式（3小时）
- [ ] **全局审计**（1.5小时）
  - [ ] 审计所有Service层Create方法签名
  - [ ] 识别其他可能存在的类似bug（如Consultation、Prescription创建）
- [ ] **标准化模式**（1.5小时）
  - [ ] 制定Controller-Service用户上下文传递规范
  - [ ] 文档化GetOperator()最佳实践
  - [ ] 更新开发规范文档

#### 2.2 患者选择资源管理与用户体验（6小时）
- [ ] FR-003: 资源管理优化（2小时）
  - [ ] 实现IDisposable接口
  - [ ] 实现Dispose方法（清理Timer、EventAggregator）
  - [ ] 添加日志记录
- [ ] FR-004: 操作成功反馈（2小时）
  - [ ] 确定反馈方式（StatusBar或简单MessageBox）
  - [ ] 修改CreateNewMedicalCaseAndNavigateAsync方法
  - [ ] 添加成功反馈逻辑
- [ ] 单元测试（P1部分）（2小时）
  - [ ] 测试IDisposable实现
  - [ ] 测试成功反馈（如果可测试）

**工时估算**: 9小时（约2个工作日）

**验收标准**:
- ✅ 所有Service Create方法签名符合用户上下文传递规范
- ✅ 开发规范文档更新完成
- ✅ IDisposable单元测试通过
- ✅ 长时间运行无内存泄漏（性能测试）
- ✅ 用户测试场景3通过

---

### Phase 3: Q4医生过滤 + P2优化（Medium，重要）

**目标**:
1. 实现医生级数据隔离（Q4）
2. 提升用户体验和操作便捷性

**任务清单**:

#### 3.1 Q4医生过滤集成（2小时）
- [ ] **Repository层修改**（0.5小时）
  - [ ] GetUnfinishedCaseByPatientIdAsync添加doctorId参数
  - [ ] 添加WHERE条件：m.DoctorId == doctorId
- [ ] **Service层修改**（0.5小时）
  - [ ] 传递doctorId参数到Repository
- [ ] **Controller层修改**（1小时）
  - [ ] GetOperator()提取当前医生ID
  - [ ] 传递到所有相关Service方法

#### 3.2 患者选择UI优化（6.5小时）
- [ ] FR-005: 分页大小优化（0.5小时）
  - [ ] 修改PageSize常量（20→50）
  - [ ] 更新或移除注释
  - [ ] 性能测试验证
- [ ] FR-006: 手动刷新队列（2小时）
  - [ ] 添加RefreshPendingQueueCommand
  - [ ] 添加IsRefreshing属性
  - [ ] 修改PatientSelectionView.xaml（刷新按钮）
- [ ] FR-007: 空状态UI（2小时）
  - [ ] 添加HasNoPendingPatients属性
  - [ ] 修改PatientSelectionView.xaml（空状态UI）
  - [ ] 样式优化
- [ ] 用户测试（2小时）
  - [ ] 测试场景1-4完整验证

**工时估算**: 8.5小时（约2个工作日）

**验收标准**:
- ✅ GetUnfinishedCaseByPatientIdAsync按医生筛选
- ✅ 多医生场景数据隔离正常
- ✅ PageSize调整后性能测试通过（<500ms）
- ✅ 手动刷新功能正常
- ✅ 空状态UI显示友好
- ✅ 用户测试场景4通过

---

### Phase 4: 全流程集成测试（Critical，必须）

**目标**: 验证P0 bug修复和Q4医生过滤的完整性

**任务清单**:

#### 4.1 医案创建与权限控制测试（2.5小时）
- [ ] **CreateAsync测试**（1小时）
  - [ ] 验证DoctorId正确设置
  - [ ] 验证DoctorName正确填充
  - [ ] 验证PatientName正确填充
- [ ] **权限控制测试**（1小时）
  - [ ] 验证CanEdit()基于DoctorId工作
  - [ ] 验证医生只能编辑自己的医案
- [ ] **医生过滤测试**（0.5小时）
  - [ ] 验证GetUnfinishedCaseByPatientIdAsync按医生筛选
  - [ ] 验证多医生场景数据隔离

#### 4.2 患者选择端到端测试（1.5小时）
- [ ] **双列表互斥测试**（0.5小时）
  - [ ] 验证选择切换逻辑
  - [ ] 验证CurrentPatient正确性
- [ ] **异常恢复测试**（0.5小时）
  - [ ] 模拟网络故障
  - [ ] 验证错误提示和日志
- [ ] **完整流程测试**（0.5小时）
  - [ ] 患者选择 → 医案创建 → 权限验证
  - [ ] 多医生并发场景测试

**工时估算**: 4小时（半个工作日）

**验收标准**:
- ✅ 所有单元测试通过（覆盖率>80%）
- ✅ 所有集成测试通过
- ✅ 端到端测试场景通过
- ✅ 数据迁移验证通过（无Guid.Empty残留）
- ✅ 多医生场景数据隔离验证通过

---

### 总工时估算

**原计划**（仅患者选择优化）:
- P0: 3小时（0.5个工作日）
- P0+P1: 9小时（约2个工作日）
- P0+P1+P2: 15.5小时（约3个工作日）

**新计划**（整合P0 bug修复 + Q4医生过滤）:
- **Phase 1（P0 Critical）**: 7小时（1个工作日）
- **Phase 1+2（P0+P1）**: 16小时（约3个工作日）
- **Phase 1+2+3（P0+P1+P2+Q4）**: 24.5小时（约4个工作日）
- **Phase 1+2+3+4（完整）**: 28.5小时（约5个工作日）

### 实施优先级策略

1. **Phase 1（P0 Critical）**: 医案创建bug修复 + 患者安全，立即实施
2. **Phase 2（P1 High）**: 用户上下文标准化 + 资源管理，近期完成
3. **Phase 3（P2 + Q4）**: 医生过滤 + 用户体验优化，持续改进
4. **Phase 4（Critical）**: 全流程集成测试，必须完成

---

## ❓ 开放问题

### Q1: Toast通知库选择

**问题**: 成功反馈（FR-004）需要Toast通知，当前项目是否已有Toast库？

**选项**:
- **A. 使用第三方Toast库**（如ToastNotifications.Messages.Wpf）
  - 优点：功能丰富，样式美观
  - 缺点：引入新依赖，违反MVP原则（避免第三方UI库）
- **B. 自己实现简单Toast**（使用Popup或Window）
  - 优点：无依赖，可控
  - 缺点：开发成本高（2-3小时）
- **C. 使用StatusBar显示成功消息**（推荐）
  - 优点：简单、符合MVP原则、无依赖
  - 缺点：样式朴素（但足够用）

**建议**: 选C（StatusBar）
**理由**: MVP阶段优先功能而非样式，StatusBar足够满足需求

---

### Q2: 自动刷新策略与未来多工作站协同

**问题**: 在WebApi+WPF架构下，如何实现实时数据同步？未来前台、收银、药房等多工作站协同时会不会造成大规模重构或架构出错？

**场景分析**:
- **MVP阶段（当前）**: 无前台、无挂号，医生选一个看一个（1-3医生）
- **Phase 2（前台上线后）**: 前台挂号 → 医生端"待看诊列表"需更新
- **Phase 3（完整协同）**: 前台、医生、收银、药房（3-6个工作站）多角色协同

**技术方案对比**:

| 方案 | 手动刷新 | 定时轮询(15-60秒) | SignalR(WebSocket) |
|-----|---------|-----------------|-------------------|
| 开发成本 | 0小时 | 2小时 | 11小时 |
| 架构改动 | 零 | 零 | 需引入SignalR |
| 实时性 | 导航时立即 | 15-60秒延迟 | <1秒 |
| 服务器负载 | 极低 | 低 | 中等 |
| MVP符合度 | ✅ 完全符合 | ✅ 符合 | ❌ 过度设计 |
| 重构风险 | 零 | 零 | 低（增量式） |
| 中医诊所适配 | ✅ 很适合 | ✅ 适合 | ⚠️ 仅大规模需要 |

**行业最佳实践**（网上成熟案例验证）:
- ✅ SignalR完全适用于WPF桌面应用（微软官方确认）
- ✅ 轮询间隔标准：15秒（微软性能监视器、SQL Server系统线程）
- ✅ 混合模式：WebAPI（CRUD）+ SignalR（推送通知）可共存
- ✅ 渐进式升级路径：手动 → 轮询 → SignalR（增量式，无大规模重构）

**架构演进保障**:
1. **接口抽象层**: 定义IDataSyncService，ViewModel依赖接口不依赖实现
2. **功能开关**: 配置文件控制同步策略，支持降级
3. **WebAPI保留**: SignalR仅用于推送通知，数据获取仍用API
4. **独立部署**: SignalR可独立端口，故障隔离

**重构风险评估**:
- 手动 → 轮询：2小时（新增DispatcherTimer，WebAPI不变）
- 轮询 → SignalR：9小时（新增SignalR Hub，WebAPI保留）
- ✅ **结论**: 无大规模重构风险，WebAPI与SignalR可共存，架构安全

**推荐技术路线图**:
- **MVP阶段（当前）**: 手动刷新 + OnNavigatedTo自动加载
  - 成本：0小时，零风险
  - 理由：无前台、无多工作站需求，1-3医生场景
- **Phase 2（前台上线后）**: 评估是否需要定时轮询
  - 触发条件：用户反馈需要自动刷新 OR 每小时挂号>10次 OR 工作站>=4个
  - 方案：定时轮询15-60秒
  - 成本：2小时
- **Phase 3（完整协同）**: 评估是否需要SignalR
  - 触发条件：工作站>=6个 AND 日均患者>=120人 AND 要求实时性<5秒
  - 方案：SignalR实时推送
  - 成本：9小时

**用户确认**: ✅ 当前MVP阶段使用手动刷新（2025-11-22确认）
**理由**: 符合小诊所场景（1-3医生，<100患者/天），OnNavigatedTo自动加载已满足95%需求

---

### Q3: 缓存TTL机制与性能优化权衡

**问题**: 待诊队列缓存是否需要TTL（Time To Live）机制？

**场景分析**:
- **当前策略**: PatientSelectionViewModel每次OnNavigatedTo都重新调用LoadDataAsync()，从API获取最新数据
- **业务流程**: 医生选择患者 → 看诊 → 完成/暂停 → 返回PatientSelectionView → 重新加载队列
- **核心问题**: 是否需要缓存来减少网络请求？

**业务场景分析**（医生使用模式）:
- **小诊所场景**（1-3医生，<100患者/天，最多到150）:
  - 单个医生每天看诊：平均30-50个患者（3医生时，150÷3=50患者）
  - 每个患者平均看诊时间：15分钟（简单诊疗），复杂验证会更长
  - 医生切换频率：每15分钟左右返回一次PatientSelectionView
- **切换场景**:
  1. 看完一个患者，返回选择下一个（最常见，90%）
  2. 临时中断，暂停当前医案，返回选择急诊（偶尔，8%）
  3. 快速浏览队列，短时间内多次返回（很少，2%）
- **关键发现**: 如果实现5分钟TTL，但医生切换间隔为15分钟，则缓存总是过期，TTL机制完全失效！

**网络成本分析**:
| 指标 | 数值 | 说明 |
|------|------|------|
| **单次请求数据量** | 10KB | 未完成医案列表（0-10条记录） |
| **单次响应时间** | <100ms | 本地网络 |
| **单医生每天请求次数** | 32次 | 8小时 ÷ 15分钟 |
| **单医生每天流量** | 320KB | 32次 × 10KB |
| **全诊所每天流量** | 960KB | 3医生 × 320KB |
| **QPS** | 0.011 | 3医生 × (1次/15分钟) ≈ 3/900秒 |

**结论**: 网络成本极低，完全不是性能瓶颈！

**数据新鲜度分析**（用户体验影响）:
- **TTL缓存的陈旧数据问题**:
  1. 医生完成患者A，返回队列
  2. 缓存未过期（假设2分钟前刚加载过）
  3. 患者A仍在列表中（陈旧数据）
  4. 用户体验：医生困惑"我刚完成了，为什么还在？"
- **"每次重新加载"的优势**:
  - 数据始终最新
  - 用户操作符合预期
  - 无需处理缓存失效逻辑

**实现复杂度分析**:
| 项目 | 工作量 | 说明 |
|------|--------|------|
| **代码实现** | 1小时 | 缓存字段、过期检查、失效逻辑 |
| **单元测试** | 1小时 | 测试缓存过期、缓存失效 |
| **集成测试** | 0.5小时 | 验证多医生场景 |
| **总计** | 2.5小时 | |

**额外复杂度**:
- 缓存失效时机判断（何时调用InvalidateCache？）
- 多医生场景下的缓存隔离（每个医生独立缓存？）
- 调试难度增加（数据不一致时，是缓存问题还是API问题？）

**与Q2自动刷新方案的冲突**:
| Q2方案 | TTL冲突 | 说明 |
|--------|---------|------|
| **手动刷新** | 冲突 | 用户点击刷新期望获取最新数据，但TTL返回缓存 |
| **定时轮询（60秒）** | 严重冲突 | 前4次轮询都返回缓存，违背轮询目的 |
| **SignalR推送** | 冲突 | 推送通知数据变化，但TTL返回缓存 |

**结论**: TTL与所有自动刷新方案都存在逻辑矛盾，需要在每次主动刷新时失效缓存，增加复杂度！

**适用场景对比**:
| 场景 | 待诊队列 | 真正适合TTL的场景（如中药材字典） |
|------|----------|--------------------------------|
| **访问频率** | 每15分钟1次（低频） | 每秒数十次（高频） |
| **数据变化频率** | 每次看诊完成后（高频） | 几乎不变（准静态） |
| **网络成本** | 10KB，<100ms（低） | 350KB（~350种），200-500ms（中） |
| **新鲜度要求** | 高（医生期望最新状态） | 低（允许一定陈旧） |
| **TTL价值** | ❌ 无价值 | ✅ 高价值 |

**未来扩展评估**:
- **Phase 2（前台上线后）**:
  - 前台挂号 → 医生端队列变化更频繁
  - TTL缓存会导致医生看不到新挂号的患者（用户体验更差）
- **Phase 3（多工作站协同）**:
  - 3-6个工作站（1-3医生+前台+收银+药房），数据变化更频繁
  - 如果使用SignalR方案（Q2），TTL缓存完全无意义
- **唯一可能场景**:
  - 离线模式（医生临时断网，仍能查看缓存队列）
  - 但这需要持久化缓存（LocalStorage），不是内存TTL缓存

**行业最佳实践验证**:
- Microsoft官方建议：仅对静态或准静态数据使用缓存（如元数据、配置）
- MVVM模式下，ViewModel数据不应作为长期缓存
- 数据新鲜度 > 性能优化（除非有明确的性能瓶颈）

**技术方案对比**:
| 方案 | 网络成本 | 数据新鲜度 | 实现复杂度 | 与Q2兼容性 | MVP适用性 |
|------|----------|------------|------------|-----------|----------|
| **A. 不实现TTL** | 960KB/天 | 100%最新 | 0小时（当前） | ✅ 完全兼容 | ✅ 最优 |
| **B. 实现5分钟TTL** | 930KB/天 | 可能陈旧 | 2.5小时 | ❌ 需额外失效逻辑 | ❌ 过度设计 |

**节省效果分析**:
- 实现TTL后，假设医生在5分钟内多次返回（快速浏览模式，<5%场景）
- 可能节省：每天3-5次请求，即30KB
- 性价比：2.5小时开发成本，节省微乎其微

**推荐方案**: **A. 不实现TTL**（保持当前策略）

**核心理由**（按重要性排序）:
1. **数据新鲜度 > 性能优化**: 医生期望看到最新队列状态，TTL会导致数据陈旧，影响用户体验
2. **网络成本极低**: QPS 0.0028，800KB/天，完全不是性能瓶颈
3. **与Q2自动刷新方案冲突**: 手动刷新、定时轮询、SignalR都需要主动失效缓存，增加复杂度
4. **业务场景不匹配**: 医生切换频率（30分钟）远大于TTL（5分钟），缓存总是过期
5. **实现成本不值**: 2.5小时开发，节省效果微乎其微（每天省20-30KB）
6. **符合MVP原则**: 当前策略已满足需求，没有明确的性能问题需要解决

**扩展路径**（如果未来真的需要缓存）:
- **触发条件**: QPS > 1 AND 网络延迟 > 500ms AND 数据变化频率 < 1次/小时
- **实施方案**: 带失效通知的智能缓存（SignalR推送失效 + 本地TTL），而非简单的5分钟TTL
- **适用对象**: 中药材字典、用户权限等准静态数据，而非待诊队列

**用户确认**: ✅ 不实现TTL（2025-11-22确认）
**理由**: 符合小诊所场景，OnNavigatedTo自动加载已满足100%需求，TTL属于过度设计

---

### Q4: 按医生筛选队列 - 发现P0严重bug

**问题**: 多医生场景下，待诊队列是否需要按当前登录医生筛选？

**背景调查发现**:
在调查GetUnfinishedCaseByPatientIdAsync方法时，发现Repository层未按DoctorId筛选：
```csharp
// src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs:322-323
var result = await GetDetailQuery()
    .Where(m => m.PatientId == patientId && m.Status != MedicalCaseStatus.Completed)
    .OrderByDescending(m => m.CreatedAt)
    .FirstOrDefaultAsync();
```

**原计划方案B1**: API层增加DoctorId过滤（数据层隔离）
- Repository: 添加doctorId参数到GetUnfinishedCaseByPatientIdAsync
- Service: 添加doctorId参数，传递给Repository
- Controller: 使用GetOperator()提取当前医生ID，传递给Service
- 估算工时：3.5小时

---

### 🚨 P0严重bug: 医案创建未设置DoctorId

**bug发现过程**（2025-11-22）:
在Q4调查过程中，深入追踪医案创建流程时发现**系统性严重bug**：

#### 症状表现
```csharp
// src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs:53
public async Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate)
{
    // ...
    var medicalCase = new MedicalCaseEntity
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,        // ✅ Set correctly
        ConsultationDate = visitDate,
        Status = MedicalCaseStatus.Active,
        NeedsPrescription = false,
        CreatedAt = DateTime.Now,
        UpdatedAt = DateTime.Now
        // ❌ CRITICAL BUG: DoctorId NOT SET
        // ❌ CRITICAL BUG: DoctorName NOT SET
        // ❌ CRITICAL BUG: PatientName NOT SET
    };
}
```

#### 根本原因分析
1. **Method Signature Missing doctorId Parameter**:
   - CreateAsync(Guid patientId, DateTime visitDate) - 缺少doctorId参数

2. **Controller Missing GetOperator() Call**:
   ```csharp
   // src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs:51-57
   public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> CreateMedicalCase(
       [FromBody] CreateMedicalCaseRequest request)
   {
       // ❌ 没有提取当前用户ID
       var result = await _medicalCaseService.CreateAsync(request.PatientId, request.VisitDate);
   }
   ```

3. **Guid Value Type Default Behavior**:
   - DoctorId is `Guid` type (value type)
   - Default value = `Guid.Empty` (00000000-0000-0000-0000-000000000000)
   - Database schema allows nullable (AppDbContextModelSnapshot.cs:595-601 没有.IsRequired())
   - INSERT succeeds because `Guid.Empty` is not null

4. **Data Integrity Corruption**:
   - **ALL** historical medical cases have `DoctorId = Guid.Empty`
   - Permission control (CanEdit) relies on DoctorId - **BROKEN**
   - Q4 doctor filtering query won't work - all records have same DoctorId

#### 影响评估
| 影响范围 | 严重程度 | 描述 |
|---------|---------|------|
| **数据完整性** | 🔴 Critical | 所有历史医案DoctorId=Guid.Empty，无法追溯真实医生 |
| **权限控制** | 🔴 Critical | CanEdit()依赖DoctorId判断权限，当前失效 |
| **业务逻辑** | 🔴 Critical | "固定医生看固定患者"原则无法执行 |
| **Q4方案阻塞** | 🔴 Critical | 医生筛选查询无效（所有记录DoctorId相同） |
| **审计追溯** | 🔴 Critical | 无法追溯"谁创建的医案"，违反医疗合规要求 |

#### 用户决策: 方案B - 完整重构（2025-11-22确认）

**Phase 1: P0 Bug修复**（4小时）
- [ ] **代码修复**（1.5小时）
  - [ ] MedicalCaseService.CreateAsync添加doctorId参数
  - [ ] Controller使用GetOperator()提取当前用户ID
  - [ ] 通过PatientId查询Patient获取PatientName
  - [ ] 通过doctorId查询User获取DoctorName
- [ ] **数据迁移脚本**（1.5小时）
  - [ ] UPDATE历史医案：使用CreatedBy字段推断DoctorId
  - [ ] 验证数据完整性（无Guid.Empty残留）
- [ ] **数据库约束**（1小时）
  - [ ] 添加CHECK约束：DoctorId != '00000000-0000-0000-0000-000000000000'
  - [ ] 验证约束生效

**Phase 2: 统一用户上下文模式**（3小时）
- [ ] **全局审计**（1.5小时）
  - [ ] 审计所有Service层Create方法签名
  - [ ] 识别其他可能存在的类似bug
- [ ] **标准化模式**（1.5小时）
  - [ ] 制定Controller-Service用户上下文传递规范
  - [ ] 文档化GetOperator()最佳实践

**Phase 3: Q4医生过滤集成**（2小时）
- [ ] **Repository层修改**（0.5小时）
  - [ ] GetUnfinishedCaseByPatientIdAsync添加doctorId参数
  - [ ] 添加WHERE条件：m.DoctorId == doctorId
- [ ] **Service层修改**（0.5小时）
  - [ ] 传递doctorId参数到Repository
- [ ] **Controller层修改**（1小时）
  - [ ] GetOperator()提取当前医生ID
  - [ ] 传递到所有相关Service方法

**Phase 4: 集成测试**（2.5小时）
- [ ] **CreateAsync测试**（1小时）
  - [ ] 验证DoctorId正确设置
  - [ ] 验证DoctorName正确填充
  - [ ] 验证PatientName正确填充
- [ ] **权限控制测试**（1小时）
  - [ ] 验证CanEdit()基于DoctorId工作
  - [ ] 验证医生只能编辑自己的医案
- [ ] **医生过滤测试**（0.5小时）
  - [ ] 验证GetUnfinishedCaseByPatientIdAsync按医生筛选
  - [ ] 验证多医生场景数据隔离

**总工时估算**: 11.5小时（约2个工作日）

**风险分析**:
- 🔴 **数据迁移风险**: 历史医案CreatedBy可能为空或不准确
  - 缓解措施：迁移前完整备份数据库
- 🟡 **业务中断风险**: 修复期间可能需要停机维护
  - 缓解措施：安排在非营业时间（晚上或周末）
- 🟢 **重构风险**: Q4方案需等待P0修复完成
  - 缓解措施：Phase 3与Phase 1合并测试

**实施优先级**: 🔴 P0（立即实施，阻塞Q4方案）

---

### Q5: 按医生筛选队列（P3扩展）

**问题**: 多医生场景下是否需要筛选待诊队列？

**选项**:
- **A. 不实现**（小诊所场景，1-3医生无需筛选）
  - 优点：简单
  - 缺点：多医生时队列混杂
- **B. 实现医生筛选**（需UI调整）
  - 优点：多医生场景更清晰
  - 缺点：增加UI复杂度

**决策**:
- **P0 bug修复后，医生筛选成为必要功能**（2025-11-22确认）
- 修复后DoctorId正确设置，可安全实施医生级数据隔离
- 整合到P0修复的Phase 3中实施

**理由**:
1. 数据完整性修复后，技术上可实现医生隔离
2. 符合医疗行业数据隔离规范（HIPAA/医疗合规）
3. 避免医生看到其他医生的患者队列

---

## 📎 参考资料

### 评审报告
- [PatientSelection-MedicalCaseFlow整合方案评审-2025-11-22](Graphiti记忆)
  - 综合评分：9.0/10
  - P0/P1/P2问题清单
  - 行业对比分析
  - 实施路线图
- [MedicalCase-创建bug调查-方案B选定-2025-11-22](Graphiti记忆)
  - P0严重bug：DoctorId未设置
  - 根本原因分析
  - 方案B完整重构计划（11.5小时）

### 项目文档
- `docs/explanation/architecture/patient-system/patient-selection-flow.md`（如果存在）
- `docs/guides/development-standards.md`（开发规范）
- `docs/reference/mvp-constraints.md`（技术约束）
- `.spec-workflow/steering/constitution.md`（MVP Constitution）

### 相关ADR
- ADR-015: UltraThink废弃（组件化设计）
- 其他相关架构决策（待补充）

### 相关Epic/Issue
- Epic #1557: 看诊流程三步骤实现
- Issue #1567: 患者选择与医案流程集成
- Issue #1806: MedicalCaseFlow组件化重构

### 代码位置
- `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Patients\ViewModels\PatientSelectionViewModel.cs`（1043行）
- `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.Patients\Views\PatientSelectionView.xaml`
- `D:\source\repos\LYBTZYZS\src\Client\Desktop\Modules\LYBT.Desktop.MedicalCase\ViewModels\MedicalCaseFlowViewModel.cs`（845行）

---

## 📝 下一步

1. **用户确认需求**：请确认本需求文档是否符合预期
2. **生成设计文档**：调用 `lybtzyzs-design-generator` 生成技术设计
3. **任务分解**：调用 `lybtzyzs-task-breakdown` 拆分具体任务
4. **创建Epic/Issues**：基于任务清单创建GitHub Issues
5. **开始实施**：按P0→P1→P2顺序渐进实施

---

**文档状态**: ✅ 需求讨论完成（Q1-Q5）+ P0严重bug发现，等待生成设计文档
**最后更新**: 2025-11-22
**版本**: v2.0（整合P0 bug修复 + Q4医生过滤）

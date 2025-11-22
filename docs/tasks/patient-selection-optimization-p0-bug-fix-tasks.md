# 患者选择优化 + P0医案创建Bug修复 任务分解文档

## 📋 元数据
- **Epic**: 待创建
- **设计文档**: [docs/explanation/design/patient-selection-optimization-p0-bug-fix-design.md](../explanation/design/patient-selection-optimization-p0-bug-fix-design.md)
- **需求文档**: [docs/explanation/requirements/patient-selection-optimization-discussion.md](../explanation/requirements/patient-selection-optimization-discussion.md)
- **总工作量**: 28.5小时
- **实施阶段**: Phase 1-4
- **创建日期**: 2025-11-22
- **状态**: 待执行

---

## 🎯 任务清单（Task Checklist）

### Phase 1: P0 Critical修复（预计7小时，1个工作日）

#### Task 1.1.1: 修改MedicalCaseService添加doctorId参数
- **工作量**: 1-1.5小时
- **依赖**: 无
- **类型**: Service层
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/IMedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] CreateAsync方法签名添加`Guid doctorId`参数
  - [ ] 构造函数注入`IPatientRepository`和`IUserRepository`
  - [ ] 查询Patient表获取PatientName
  - [ ] 查询User表获取DoctorName
  - [ ] doctorId==Guid.Empty时抛出ArgumentException
  - [ ] 正确设置`DoctorId`、`DoctorName`、`PatientName`字段
  - [ ] 添加日志记录（医案创建成功）
- **技术要点**:
  - 依赖注入新增两个Repository（IPatientRepository、IUserRepository）
  - 参数验证：doctorId不能为Guid.Empty
  - 跨Repository查询：Patient和User
  - 异常处理：EntityNotFoundException

#### Task 1.1.2: 修改MedicalCaseController提取当前医生ID
- **工作量**: 0.5-1小时
- **依赖**: Task 1.1.1
- **类型**: Controller层
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] CreateMedicalCase方法调用`GetOperator()`提取当前用户
  - [ ] 验证currentUser不为null且ID不为Empty
  - [ ] 验证currentUser.Role == "Doctor"
  - [ ] 传递currentUser.Id到Service.CreateAsync
  - [ ] 异常处理：EntityNotFoundException、ArgumentException
  - [ ] HTTP状态码正确：200/400/401/404/500
- **技术要点**:
  - 使用BaseController.GetOperator()提取JWT Claims
  - 角色验证：仅Doctor角色可创建医案
  - 异常映射：EntityNotFoundException → 404, ArgumentException → 400
  - 日志记录：LogWarning/LogError

#### Task 1.1.3: 编写数据迁移SQL脚本
- **工作量**: 1-1.5小时
- **依赖**: 无（可并行Task 1.1.1）
- **类型**: 数据库迁移
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `scripts/migrations/medicalcase-doctorid-migration.sql`（新建）
- **验收标准**:
  - [ ] 创建备份表：MedicalCase_Backup_20251122
  - [ ] 分析脚本：检查DoctorId=Guid.Empty记录数量
  - [ ] 主迁移脚本：UPDATE DoctorId/DoctorName（基于CreatedBy）
  - [ ] 主迁移脚本：UPDATE PatientName（基于PatientId）
  - [ ] 验证脚本：检查残留Guid.Empty记录
  - [ ] 人工核查脚本：通过患者历史医案推断DoctorId
  - [ ] 事务控制：BEGIN/COMMIT/ROLLBACK
  - [ ] 错误处理：TRY/CATCH/THROW
  - [ ] 临时表：#ProblematicRecords保存问题记录
- **技术要点**:
  - 安全第一：先备份数据
  - 基于CreatedBy推断DoctorId
  - 处理CreatedBy为NULL的情况
  - 记录残留问题到临时表供人工处理

#### Task 1.1.4: 添加CHECK约束防止Guid.Empty
- **工作量**: 0.5-1小时
- **依赖**: Task 1.1.3（数据迁移完成后）
- **类型**: 数据库约束
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `scripts/constraints/medicalcase-doctorid-check.sql`（新建）
  - EF Core Migration（待生成）
- **验收标准**:
  - [ ] SQL脚本：添加CHECK约束`CK_MedicalCase_DoctorId_NotEmpty`
  - [ ] 约束条件：`DoctorId != '00000000-0000-0000-0000-000000000000'`
  - [ ] 验证约束生效：INSERT Guid.Empty失败（ERROR_NUMBER=547）
  - [ ] 生成EF Core Migration文件（AddDoctorIdCheckConstraint）
  - [ ] Migration Up方法：AddCheckConstraint
  - [ ] Migration Down方法：DropCheckConstraint
- **技术要点**:
  - 检查约束是否已存在，避免重复添加
  - 验证约束：尝试插入Guid.Empty应失败
  - EF Core Migration命令：Add-Migration AddDoctorIdCheckConstraint

#### Task 1.1.5: 单元测试MedicalCaseService P0修复
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1.1
- **类型**: 单元测试
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `tests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`（新建或修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：CreateAsync_ShouldSetDoctorId_WhenValidDoctorIdProvided
  - [ ] 测试：CreateAsync_ShouldThrowException_WhenDoctorIdIsEmpty
  - [ ] 测试：CreateAsync_ShouldSetDoctorName_FromUserRepository
  - [ ] 测试：CreateAsync_ShouldSetPatientName_FromPatientRepository
  - [ ] 测试：CreateAsync_ShouldThrowEntityNotFoundException_WhenPatientNotFound
  - [ ] 测试：CreateAsync_ShouldThrowEntityNotFoundException_WhenDoctorNotFound
  - [ ] Mock Repository（IPatientRepository、IUserRepository、IMedicalCaseRepository）
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - 使用Mock框架（Moq/NSubstitute）
  - AAA模式：Arrange-Act-Assert
  - 测试边界条件：Guid.Empty、NULL、NotFound

#### Task 1.2.1: 实现双列表互斥选择逻辑
- **工作量**: 0.5-1小时
- **依赖**: 无（可并行Task 1.1.x）
- **类型**: ViewModel层
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] SelectedPatient setter：清除SelectedPendingPatient
  - [ ] SelectedPendingPatient setter：清除SelectedPatient
  - [ ] CurrentPatient属性：始终指向唯一选中患者
  - [ ] RaisePropertyChanged通知UI更新
  - [ ] 日志记录：LogDebug选择来源（全部患者/待诊队列）
  - [ ] SelectPatientCommand.RaiseCanExecuteChanged
- **技术要点**:
  - ViewModel属性setter中的互斥逻辑
  - 避免循环通知：检查value != null
  - 使用_字段赋值，避免递归调用setter

#### Task 1.2.2: 实现异常处理优化
- **工作量**: 1-1.5小时
- **依赖**: Task 1.2.1
- **类型**: ViewModel层 + View层
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] OnNavigatedTo方法：LoadPendingCasesAsync包裹在try-catch
  - [ ] 异常处理：HttpRequestException和通用Exception
  - [ ] ShowErrorMessageAsync方法：设置StatusBarMessage和StatusBarIsError
  - [ ] StatusBar属性：StatusBarMessage、StatusBarIsError
  - [ ] 3秒后自动清除StatusBarMessage
  - [ ] XAML：StatusBar绑定StatusBarMessage
  - [ ] XAML：TextBlock.Foreground根据StatusBarIsError变色（黑/红）
  - [ ] 日志记录：LogError异常堆栈
  - [ ] 异常不阻断：全部患者列表仍可用
- **技术要点**:
  - 异常不抛出：catch后不re-throw
  - StatusBar数据绑定：DataTrigger控制颜色
  - Task.Delay(3000)自动清除消息
  - 避免覆盖：清除前检查StatusBarMessage == message

#### Task 1.2.3: 单元测试PatientSelection P0优化
- **工作量**: 0.5-1小时
- **依赖**: Task 1.2.1, Task 1.2.2
- **类型**: 单元测试
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `tests/Client/Modules/LYBT.Desktop.Patients.Tests/ViewModels/PatientSelectionViewModelTests.cs`（新建或修改）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：SelectedPatient_ShouldClearSelectedPendingPatient
  - [ ] 测试：SelectedPendingPatient_ShouldClearSelectedPatient
  - [ ] 测试：CurrentPatient_ShouldAlwaysPointToSelectedPatient
  - [ ] 测试：OnNavigatedTo_ShouldHandleException_AndNotCrash
  - [ ] 测试：OnNavigatedTo_ShouldSetStatusBarMessage_OnException
  - [ ] Mock依赖：IUnfinishedCaseHandler
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - Mock IUnfinishedCaseHandler.GetAllUnfinishedCasesAsync抛出异常
  - Assert不抛出异常：await不会throw
  - Assert StatusBarMessage不为空
  - Assert StatusBarIsError == true

---

### Phase 2: P1改进 + 用户上下文标准化（预计9小时，约2个工作日）

#### Task 2.1.1: 全局审计Service Create方法签名
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1.1（了解修复模式后）
- **类型**: 代码审计
- **优先级**: 🟡 P1 High
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.*/Services/*Service.cs`（多个模块）
- **验收标准**:
  - [ ] 审计所有8个业务模块的Service层Create方法
  - [ ] 生成审计报告：受影响方法清单（Excel或Markdown）
  - [ ] 识别类似bug：Consultation/Prescription/Formula创建方法
  - [ ] 分类：已修复/待修复/无需修复
  - [ ] 优先级排序：P0/P1/P2
  - [ ] 审计报告包含：模块名、方法名、当前签名、建议修复
- **技术要点**:
  - 使用Grep搜索：`public.*Create.*Async`
  - 检查方法签名是否包含userId/doctorId参数
  - 关注Create/Add/Insert等新增操作
  - 跨模块分析：MedicalCase、Consultation、Prescription、Formula、Herbs等

#### Task 2.1.2: 制定用户上下文传递规范
- **工作量**: 1-1.5小时
- **依赖**: Task 2.1.1
- **类型**: 文档化
- **优先级**: 🟡 P1 High
- **文件范围**:
  - `docs/guides/development-standards.md`（更新）
  - `docs/architecture/decisions/ADR-XXX-user-context-pattern.md`（新建）
- **验收标准**:
  - [ ] 文档化GetOperator()最佳实践
  - [ ] Controller-Service用户上下文传递规范
  - [ ] 示例代码：正确用法vs错误用法
  - [ ] 规范内容：何时传递userId、如何提取、如何验证
  - [ ] ADR文档：记录架构决策（背景、决策、后果）
  - [ ] 规范包含：命名约定、参数顺序、异常处理
  - [ ] 开发规范文档更新完成
- **技术要点**:
  - ADR格式：Context/Decision/Status/Consequences
  - 最佳实践：GetOperator() → 验证 → 传递
  - 反例：不传递userId、使用硬编码、依赖HTTP上下文
  - 代码review检查清单

#### Task 2.2.1: 实现PatientSelectionViewModel IDisposable
- **工作量**: 1-1.5小时
- **依赖**: Task 1.2.2
- **类型**: ViewModel层
- **优先级**: 🟡 P1 High
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 实现IDisposable接口
  - [ ] Dispose(bool disposing)方法：标准Dispose模式
  - [ ] GC.SuppressFinalize(this)
  - [ ] 清理EventAggregator订阅（Unsubscribe）
  - [ ] 清理SubscriptionToken（设为null）
  - [ ] 日志记录：LogInformation("PatientSelectionViewModel disposed")
  - [ ] _disposed字段：防止重复Dispose
  - [ ] 构造函数：订阅PatientUpdatedEvent
  - [ ] OnPatientUpdated方法：处理患者更新事件
- **技术要点**:
  - 标准Dispose模式：Dispose() → Dispose(bool) → GC.SuppressFinalize
  - EventAggregator订阅：保存SubscriptionToken
  - Unsubscribe时检查token != null
  - 未来扩展：Timer清理（如果添加自动刷新）

#### Task 2.2.2: 实现操作成功反馈机制
- **工作量**: 1-1.5小时
- **依赖**: Task 1.2.2（StatusBar已实现）
- **类型**: ViewModel层
- **优先级**: 🟡 P1 High
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] ShowSuccessMessageAsync方法：设置StatusBarMessage
  - [ ] StatusBarIsError = false（成功为黑色）
  - [ ] CreateNewMedicalCaseAndNavigateAsync：调用ShowSuccessMessageAsync
  - [ ] 成功消息：包含患者姓名（如"已为张三创建新医案"）
  - [ ] 3秒后自动清除消息
  - [ ] 避免覆盖：检查StatusBarMessage == message
  - [ ] 日志记录：LogInformation医案创建成功
- **技术要点**:
  - 复用StatusBar机制（Task 1.2.2）
  - StatusBarIsError = false → 黑色文字
  - Task.Delay(3000)自动清除
  - 防止覆盖：if (StatusBarMessage == message)

#### Task 2.2.3: 单元测试PatientSelection P1优化
- **工作量**: 1.5-2小时
- **依赖**: Task 2.2.1, Task 2.2.2
- **类型**: 单元测试
- **优先级**: 🟡 P1 High
- **文件范围**:
  - `tests/Client/Modules/LYBT.Desktop.Patients.Tests/ViewModels/PatientSelectionViewModelTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：Dispose_ShouldClearEventSubscriptions
  - [ ] 测试：Dispose_ShouldSetDisposedFlag
  - [ ] 测试：Dispose_ShouldLogInformation
  - [ ] 测试：ShowSuccessMessage_ShouldDisplayForThreeSeconds（可选）
  - [ ] Mock EventAggregator
  - [ ] Verify Unsubscribe调用
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - Mock IEventAggregator
  - Mock PatientUpdatedEvent
  - Verify Unsubscribe(SubscriptionToken)
  - 测试Dispose可重入性（调用两次Dispose）

---

### Phase 3: Q4医生过滤 + P2优化（预计8.5小时，约2个工作日）

#### Task 3.1.1: MedicalCaseRepository添加doctorId筛选
- **工作量**: 0.5-1小时
- **依赖**: Task 1.1.1
- **类型**: Repository层
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/IMedicalCaseRepository.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] GetUnfinishedCaseByPatientIdAsync添加`Guid doctorId`参数
  - [ ] 接口IMedicalCaseRepository同步更新
  - [ ] 添加WHERE条件：`m.DoctorId == doctorId`（仅当doctorId != Guid.Empty）
  - [ ] 保留现有GetDetailQuery()逻辑（Include预加载）
  - [ ] OrderByDescending(m => m.CreatedAt)排序
  - [ ] 返回FirstOrDefaultAsync
- **技术要点**:
  - 条件查询：if (doctorId != Guid.Empty) query = query.Where(...)
  - EF Core LINQ查询
  - Include()预加载：Consultation、Prescription、Patient、Doctor
  - 已软删除过滤：Where(mc => !mc.IsDeleted)

#### Task 3.1.2: MedicalCaseService传递doctorId参数
- **工作量**: 0.5小时
- **依赖**: Task 3.1.1
- **类型**: Service层
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/IMedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] GetUnfinishedCaseByPatientIdAsync添加`Guid doctorId`参数
  - [ ] 接口IMedicalCaseService同步更新
  - [ ] 直接传递doctorId到Repository
  - [ ] 无额外业务逻辑（简单传递）
- **技术要点**:
  - Service层传递参数到Repository
  - 无需额外验证（Repository层处理）

#### Task 3.1.3: MedicalCaseController提取doctorId并传递
- **工作量**: 0.5小时
- **依赖**: Task 3.1.2
- **类型**: Controller层
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] GetUnfinishedCaseByPatientId添加`[FromQuery] Guid? doctorId`参数
  - [ ] 如果doctorId为null或Empty，使用GetOperator()提取当前医生ID
  - [ ] 验证currentUser不为null且Role == "Doctor"
  - [ ] 传递doctorId到Service.GetUnfinishedCaseByPatientIdAsync
  - [ ] 异常处理：Unauthorized(401)、Forbid(403)
  - [ ] 日志记录：LogDebug未找到医案
- **技术要点**:
  - 可选参数：`Guid? doctorId = null`
  - 如果未传递，默认使用当前登录医生ID
  - 支持管理员查询其他医生的医案（未来扩展）

#### Task 3.1.4: Desktop端调用方传递doctorId
- **工作量**: 0.5小时
- **依赖**: Task 3.1.3
- **类型**: Desktop端组件 + ViewModel
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Components/UnfinishedCaseHandler.cs`
  - `src/Client/Desktop/Shared/API/IMedicalCaseApi.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] UnfinishedCaseHandler.GetUnfinishedCaseByPatientIdAsync添加`Guid doctorId`参数
  - [ ] IMedicalCaseApi接口：添加`[Query] Guid doctorId`参数（Refit）
  - [ ] PatientSelectionViewModel.SelectPatientAsync：从SessionManager获取currentDoctorId
  - [ ] 传递currentDoctorId到UnfinishedCaseHandler
  - [ ] SessionManager.CurrentUser?.Id ?? Guid.Empty
- **技术要点**:
  - Refit接口：[Query]特性标记查询参数
  - SessionManager获取当前登录用户
  - 空合并运算符：?? Guid.Empty

#### Task 3.2.1: PageSize调整从20到50
- **工作量**: 0.5小时
- **依赖**: 无（独立任务）
- **类型**: ViewModel层
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] PageSize常量修改：20 → 50
  - [ ] 注释更新或移除（与代码一致）
  - [ ] LoadPatientsAsync：使用新的PageSize
  - [ ] 性能测试：加载时间<500ms（需手动测试）
  - [ ] 日志记录：LoadPatientsAsync添加性能监控
  - [ ] Stopwatch监控加载时间
  - [ ] 如果>500ms，LogWarning性能警告
- **技术要点**:
  - const int PageSize = 50
  - System.Diagnostics.Stopwatch性能监控
  - LogInformation("患者列表加载完成: 数量={Count}, 耗时={ElapsedMs}ms")

#### Task 3.2.2: 实现手动刷新队列功能
- **工作量**: 1.5-2小时
- **依赖**: Task 1.2.2
- **类型**: ViewModel层 + View层
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 添加IsRefreshing属性（bool）
  - [ ] 添加RefreshPendingQueueCommand（DelegateCommand）
  - [ ] RefreshPendingQueueAsync方法：调用LoadPendingCasesAsync
  - [ ] 刷新过程：IsRefreshing = true
  - [ ] 刷新完成：IsRefreshing = false（finally块）
  - [ ] Command CanExecute：!IsRefreshing
  - [ ] 成功反馈：ShowSuccessMessageAsync("待诊队列已刷新")
  - [ ] 异常处理：HttpRequestException、Exception
  - [ ] XAML：刷新按钮UI（Content="🔄"）
  - [ ] XAML：Button.Command绑定RefreshPendingQueueCommand
  - [ ] XAML：IsEnabled绑定IsRefreshing（使用InverseBooleanConverter）
  - [ ] XAML：ToolTip="刷新待诊队列"
  - [ ] 日志记录：LogInformation手动刷新
- **技术要点**:
  - DelegateCommand.ObservesProperty(() => IsRefreshing)
  - 按钮禁用：刷新过程中不可点击
  - finally块确保IsRefreshing = false
  - InverseBooleanConverter：true → false（Enabled）

#### Task 3.2.3: 实现空状态UI
- **工作量**: 1.5-2小时
- **依赖**: Task 1.2.2
- **类型**: ViewModel层 + View层 + Converter
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientSelectionViewModel.cs`
  - `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Views/PatientSelectionView.xaml`
  - `src/Client/Desktop/Shared/Converters/InverseBooleanToVisibilityConverter.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 添加HasNoPendingPatients属性：`PendingPatients?.Count == 0`
  - [ ] PendingPatients setter：RaisePropertyChanged(nameof(HasNoPendingPatients))
  - [ ] XAML：空状态UI（StackPanel）
  - [ ] XAML：图标TextBlock（Text="📋", FontSize=48）
  - [ ] XAML：主标题TextBlock（Text="暂无待诊患者", FontSize=16, FontWeight=Bold）
  - [ ] XAML：副标题TextBlock（Text="从左侧选择患者或等待新的挂号", FontSize=12）
  - [ ] XAML：空状态UI Visibility绑定HasNoPendingPatients（BooleanToVisibilityConverter）
  - [ ] XAML：队列ListBox Visibility绑定HasNoPendingPatients（InverseBooleanToVisibilityConverter）
  - [ ] 创建InverseBooleanToVisibilityConverter
  - [ ] Converter：true → Collapsed, false → Visible
  - [ ] 样式：居中显示，灰色文字
- **技术要点**:
  - 计算属性：HasNoPendingPatients无setter，仅getter
  - 依赖通知：PendingPatients变化时通知HasNoPendingPatients
  - Visibility双向控制：列表和空状态互斥显示
  - Converter：IValueConverter接口实现

#### Task 3.2.4: 用户验收测试执行
- **工作量**: 2-2.5小时
- **依赖**: Task 3.1.4, Task 3.2.3（所有Phase 3任务完成）
- **类型**: 用户验收测试（UAT）
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - 测试记录文档：`docs/testing/uat-patient-selection-phase3.md`（新建）
- **验收标准**:
  - [ ] UAT场景1：双列表互斥（6步验证）
  - [ ] UAT场景2：异常恢复（8步验证）
  - [ ] UAT场景3：成功反馈（5步验证）
  - [ ] UAT场景4：空状态UI（8步验证）
  - [ ] UAT场景5：P0 Bug修复验证（11步验证）
  - [ ] UAT场景6：多医生数据隔离（5步验证）
  - [ ] 测试记录文档：记录所有场景执行结果（通过/失败/备注）
  - [ ] 缺陷清单：如有失败，记录缺陷并分配修复任务
- **技术要点**:
  - 真实环境测试：连接真实数据库、API服务器
  - 模拟网络故障：停止API服务器
  - 多医生场景：创建两个医生账号（医生A、医生B）
  - 数据验证：使用SQL查询验证数据库记录

---

### Phase 4: 全流程集成测试（预计4小时，半个工作日）

#### Task 4.1.1: 编写MedicalCase创建集成测试
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1.2（Controller修复完成）
- **类型**: 集成测试
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `tests/IntegrationTests/Server/MedicalCase/MedicalCaseCreationTests.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：CreateMedicalCase_ShouldSetDoctorId_WhenCalled
  - [ ] 测试：CreateMedicalCase_ShouldSetDoctorName_FromUserTable
  - [ ] 测试：CreateMedicalCase_ShouldSetPatientName_FromPatientTable
  - [ ] 测试：CreateMedicalCase_ShouldThrowException_WhenGuidEmpty
  - [ ] 使用WebApplicationFactory
  - [ ] Mock JWT认证（HttpContext.User）
  - [ ] 真实数据库（In-Memory或TestContainer）
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - WebApplicationFactory<Startup>
  - 配置测试数据库（In-Memory SQLite或SQL Server TestContainer）
  - Mock JWT Token：设置HttpContext.User.Claims
  - 集成测试清理：使用DatabaseFixture

#### Task 4.1.2: 编写权限控制集成测试
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1.2, Task 3.1.3
- **类型**: 集成测试
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `tests/IntegrationTests/Server/MedicalCase/PermissionControlTests.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：CanEdit_ShouldReturnTrue_WhenSameDoctorId
  - [ ] 测试：CanEdit_ShouldReturnFalse_WhenDifferentDoctorId
  - [ ] 测试：GetUnfinishedCase_ShouldFilterByDoctorId
  - [ ] 测试：GetUnfinishedCase_ShouldNotReturnOtherDoctorsCases
  - [ ] 使用WebApplicationFactory
  - [ ] 准备测试数据：两个医生、两个患者、多个医案
  - [ ] 验证数据隔离：医生A只能看到自己的医案
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - Seed测试数据：两个医生（DoctorA、DoctorB）
  - 创建医案：DoctorA创建PatientX的医案
  - 验证权限：DoctorB不能编辑DoctorA的医案
  - 验证筛选：DoctorB查询PatientX时，不返回DoctorA的医案

#### Task 4.1.3: 编写医生过滤集成测试
- **工作量**: 0.5-1小时
- **依赖**: Task 3.1.4
- **类型**: 集成测试
- **优先级**: 🟢 P2 Medium
- **文件范围**:
  - `tests/IntegrationTests/Server/MedicalCase/DoctorFilterTests.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：GetUnfinishedCase_ShouldReturnOnlyCurrentDoctorCases
  - [ ] 测试：GetUnfinishedCase_ShouldReturnNull_WhenOtherDoctorCase
  - [ ] 使用真实数据库
  - [ ] Seed测试数据：同一患者有两个医生的医案
  - [ ] 验证筛选：医生A查询时只返回医生A的医案
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - Repository层集成测试
  - EF Core查询验证
  - 测试数据：PatientX有两条医案（DoctorA、DoctorB）

#### Task 4.2.1: 编写PatientSelection端到端测试
- **工作量**: 1-1.5小时
- **依赖**: Task 1.2.3, Task 2.2.3
- **类型**: 集成测试
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `tests/IntegrationTests/Client/Patients/PatientSelectionE2ETests.cs`（新建）
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 测试：PatientSelection_To_MedicalCaseCreation_Integration
  - [ ] 测试：DoubleListMutex_ShouldWork_InRealScenario
  - [ ] 测试：ExceptionHandling_ShouldNotCrash_WhenNetworkFailure
  - [ ] Mock API服务器（或使用真实API）
  - [ ] Seed测试数据：患者、医生
  - [ ] 验证完整流程：选择患者 → 创建医案 → 验证DoctorId
  - [ ] 所有测试通过：100% Pass
- **技术要点**:
  - WPF集成测试：使用UI Automation或直接测试ViewModel
  - Mock SessionManager：设置CurrentUser
  - Mock API：使用WireMock或真实API
  - 端到端验证：从UI操作到数据库验证

#### Task 4.2.2: 数据迁移验证测试
- **工作量**: 0.5-1小时
- **依赖**: Task 1.1.3, Task 1.1.4
- **类型**: 数据库测试
- **优先级**: 🔴 P0 Critical
- **文件范围**:
  - `scripts/migrations/medicalcase-doctorid-migration-test.sql`（新建）
- **验收标准**:
  - [ ] 测试数据库：创建独立测试数据库
  - [ ] 准备测试数据：插入模拟旧数据（DoctorId=Guid.Empty）
  - [ ] 执行迁移脚本：medicalcase-doctorid-migration.sql
  - [ ] 验证结果：所有记录DoctorId != Guid.Empty
  - [ ] 验证统计：FixedRecords数量 = TotalRecords数量
  - [ ] 验证约束：INSERT Guid.Empty失败（ERROR_NUMBER=547）
  - [ ] 残留记录处理：如有残留，执行人工核查脚本
  - [ ] 备份恢复测试：验证可回滚
- **技术要点**:
  - 使用测试数据库（非生产环境）
  - 验证脚本：SELECT COUNT检查统计
  - 约束测试：TRY/CATCH验证CHECK约束
  - 备份测试：DROP TABLE恢复测试

---

## 📊 任务统计

- **总任务数**: 26个
- **总工作量**: 28.5小时（24-33小时区间）
- **Phase数量**: 4个阶段
- **关键路径长度**: 11个任务

### 按Phase统计

| Phase | 任务数 | 工作量 | 优先级分布 |
|-------|--------|--------|------------|
| Phase 1 | 7个 | 7小时 | P0: 7个 |
| Phase 2 | 5个 | 9小时 | P1: 5个 |
| Phase 3 | 8个 | 8.5小时 | P2: 8个 |
| Phase 4 | 6个 | 4小时 | P0: 4个, P2: 2个 |

### 按类型统计

| 类型 | 任务数 | 总工时 |
|------|--------|--------|
| Service层 | 3个 | 3.5小时 |
| Controller层 | 3个 | 2小时 |
| Repository层 | 2个 | 1.5小时 |
| ViewModel层 | 6个 | 8小时 |
| View层 | 3个 | 5.5小时 |
| 单元测试 | 3个 | 4小时 |
| 集成测试 | 4个 | 4小时 |
| 数据库 | 2个 | 2.5小时 |

---

## 🔗 依赖关系图

### Phase 1依赖（关键路径）

```
Task 1.1.1 (Service修改) → Task 1.1.2 (Controller修改) → Task 1.1.5 (单元测试)
    ↓
Task 1.1.3 (数据迁移) → Task 1.1.4 (CHECK约束)

Task 1.2.1 (双列表互斥) → Task 1.2.2 (异常处理) → Task 1.2.3 (单元测试)
```

### Phase 2依赖

```
Task 1.1.1 → Task 2.1.1 (全局审计) → Task 2.1.2 (规范制定)

Task 1.2.2 → Task 2.2.1 (IDisposable) → Task 2.2.3 (单元测试)
             Task 2.2.2 (成功反馈) ↗
```

### Phase 3依赖（关键路径）

```
Task 1.1.1 → Task 3.1.1 (Repository) → Task 3.1.2 (Service) → Task 3.1.3 (Controller) → Task 3.1.4 (Desktop) → Task 3.2.4 (UAT)

Task 1.2.2 → Task 3.2.1 (PageSize) → Task 3.2.4 (UAT)
             Task 3.2.2 (刷新队列) ↗
             Task 3.2.3 (空状态UI) ↗
```

### Phase 4依赖

```
Task 1.1.2 → Task 4.1.1 (创建集成测试)
Task 1.1.2 + Task 3.1.3 → Task 4.1.2 (权限测试)
Task 3.1.4 → Task 4.1.3 (医生过滤测试)

Task 1.2.3 + Task 2.2.3 → Task 4.2.1 (端到端测试)
Task 1.1.3 + Task 1.1.4 → Task 4.2.2 (迁移验证)
```

### 跨Phase依赖

```
Phase 1 (完成) → Phase 2 (开始)
Phase 1 (完成) → Phase 3 (开始)
Phase 1 + Phase 2 + Phase 3 (完成) → Phase 4 (开始)

关键依赖：
- Task 1.1.1 → Task 2.1.1（了解修复模式）
- Task 1.1.1 → Task 3.1.1（Service修复完成）
- Task 1.2.2 → Task 3.2.2, Task 3.2.3（StatusBar已实现）
```

---

## ⚠️ 关键路径

**主线任务**（必须按顺序完成）：

### P0 Critical关键路径（Phase 1 + Phase 4）
1. **Task 1.1.1**: 修改MedicalCaseService添加doctorId参数（1-1.5小时）
2. **Task 1.1.2**: 修改MedicalCaseController提取当前医生ID（0.5-1小时）
3. **Task 1.1.3**: 编写数据迁移SQL脚本（1-1.5小时）
4. **Task 1.1.4**: 添加CHECK约束防止Guid.Empty（0.5-1小时）
5. **Task 1.1.5**: 单元测试MedicalCaseService P0修复（1-1.5小时）
6. **Task 1.2.1**: 实现双列表互斥选择逻辑（0.5-1小时）
7. **Task 1.2.2**: 实现异常处理优化（1-1.5小时）
8. **Task 1.2.3**: 单元测试PatientSelection P0优化（0.5-1小时）
9. **Task 4.1.1**: 编写MedicalCase创建集成测试（1-1.5小时）
10. **Task 4.1.2**: 编写权限控制集成测试（1-1.5小时）
11. **Task 4.2.1**: 编写PatientSelection端到端测试（1-1.5小时）

**关键路径总时长**: 约10-13.5小时

### Q4医生过滤关键路径（Phase 3）
1. **Task 3.1.1**: MedicalCaseRepository添加doctorId筛选（0.5-1小时）
2. **Task 3.1.2**: MedicalCaseService传递doctorId参数（0.5小时）
3. **Task 3.1.3**: MedicalCaseController提取doctorId并传递（0.5小时）
4. **Task 3.1.4**: Desktop端调用方传递doctorId（0.5小时）
5. **Task 4.1.3**: 编写医生过滤集成测试（0.5-1小时）

**Q4路径总时长**: 约2.5-3.5小时

---

## 🔄 并行任务（可同时进行）

### Phase 1并行机会

**并行组1**（Server端 vs Client端）:
- Task 1.1.1 + Task 1.1.2 + Task 1.1.3（Server端）|| Task 1.2.1 + Task 1.2.2（Client端）
- 可节省时间：2-3小时

**并行组2**（代码 vs 测试）:
- Task 1.1.1 + Task 1.1.2（代码实现）|| Task 1.1.3 + Task 1.1.4（数据库）
- 可节省时间：1-2小时

### Phase 2并行机会

**并行组3**（文档 vs 代码）:
- Task 2.1.1 + Task 2.1.2（文档和规范）|| Task 2.2.1 + Task 2.2.2（代码实现）
- 可节省时间：2-3小时

### Phase 3并行机会

**并行组4**（Q4医生过滤 vs UI优化）:
- Task 3.1.1 + Task 3.1.2 + Task 3.1.3 + Task 3.1.4（Q4医生过滤链）|| Task 3.2.1 + Task 3.2.2 + Task 3.2.3（UI优化）
- 可节省时间：5-6小时

### Phase 4并行机会

**并行组5**（多个测试并行）:
- Task 4.1.1 || Task 4.1.2 || Task 4.1.3 || Task 4.2.1 || Task 4.2.2
- 可节省时间：2-3小时（如果有多个测试人员）

**总并行节省时间**: 12-17小时（理论值，实际取决于人力资源）

---

## 📝 实施建议

### 优先级排序

1. **🔴 P0 Critical**（11个任务，约15-18.5小时）
   - **必须完成**: Phase 1全部任务 + Phase 4关键测试
   - **阻塞级别**: 阻塞生产环境部署
   - **顺序**: Phase 1 → Phase 4测试

2. **🟡 P1 High**（5个任务，约9小时）
   - **重要但不阻塞**: Phase 2全部任务
   - **价值**: 预防未来类似bug，提升代码质量
   - **顺序**: Phase 1完成后开始

3. **🟢 P2 Medium**（10个任务，约10.5小时）
   - **增强体验**: Phase 3全部任务
   - **价值**: 用户体验优化，数据隔离
   - **顺序**: 可与Phase 2并行

### 并行策略

#### 单人开发顺序
```
Week 1:
  Day 1: Phase 1 (Task 1.1.1 → 1.1.5)（7小时）
  Day 2: Phase 4.1测试（Task 4.1.1, 4.1.2）+ 数据迁移执行（Task 4.2.2）（4小时）
  Day 3: Phase 2.1规范（Task 2.1.1, 2.1.2）+ Phase 2.2开始（Task 2.2.1）（5小时）

Week 2:
  Day 4: Phase 2.2完成（Task 2.2.2, 2.2.3）（4小时）
  Day 5: Phase 3.1 Q4过滤（Task 3.1.1 → 3.1.4）（2小时）
  Day 6: Phase 3.2 UI优化（Task 3.2.1 → 3.2.3）（4.5小时）
  Day 7: Phase 3.2 UAT（Task 3.2.4）+ Phase 4收尾（Task 4.1.3, 4.2.1）（4小时）
```

#### 多人协作（2人）
```
开发者A（Server端专家）:
  Day 1: Task 1.1.1, 1.1.2, 1.1.3, 1.1.4, 1.1.5（5.5小时）
  Day 2: Task 2.1.1, 2.1.2（3小时）
  Day 3: Task 3.1.1, 3.1.2, 3.1.3, 3.1.4（2小时）
  Day 4: Task 4.1.1, 4.1.2, 4.1.3, 4.2.2（4小时）

开发者B（Desktop端专家）:
  Day 1: Task 1.2.1, 1.2.2, 1.2.3（3小时）
  Day 2: Task 2.2.1, 2.2.2, 2.2.3（5.5小时）
  Day 3: Task 3.2.1, 3.2.2, 3.2.3（4.5小时）
  Day 4: Task 3.2.4, 4.2.1（3.5小时）
```

**总耗时**（多人协作）: **4个工作日**（并行节省约40%时间）

### 风险提示

1. **Task 1.1.3（数据迁移）风险 🔴 High**
   - 历史数据CreatedBy可能为NULL
   - **缓解**: 在测试数据库先执行，验证残留记录数量<10%
   - **应急**: 准备人工核查脚本（Task 1.1.3包含）

2. **Task 1.1.2（Controller修改）破坏性变更 🟡 Medium**
   - 方法签名变更可能影响其他调用方
   - **缓解**: Task 2.1.1全局审计识别所有调用方
   - **应急**: 编译时强制修复所有调用点

3. **Task 3.2.1（PageSize性能）风险 🟢 Low**
   - PageSize=50可能导致加载>500ms
   - **缓解**: 添加性能监控（Stopwatch）
   - **应急**: 回退PageSize=30或40

4. **Task 4.2.2（数据迁移验证）生产风险 🔴 High**
   - 迁移脚本在生产环境执行风险高
   - **缓解**: 非营业时间执行，完整备份，停机时间<30分钟
   - **应急**: 准备回滚脚本（DROP TABLE恢复）

---

## 🧪 测试策略

### 单元测试

**覆盖范围**:
- **Task 1.1.5**: MedicalCaseService.CreateAsync（6个测试用例）
- **Task 1.2.3**: PatientSelectionViewModel双列表互斥 + 异常处理（5个测试用例）
- **Task 2.2.3**: PatientSelectionViewModel IDisposable + 成功反馈（3个测试用例）

**总测试用例**: 14个

**工具**:
- xUnit或NUnit
- Moq或NSubstitute（Mock框架）
- FluentAssertions（断言库）

**执行时机**: 每个Phase完成后执行对应单元测试

### 集成测试

**覆盖范围**:
- **Task 4.1.1**: MedicalCase创建（4个测试用例）
- **Task 4.1.2**: 权限控制（4个测试用例）
- **Task 4.1.3**: 医生过滤（2个测试用例）
- **Task 4.2.1**: PatientSelection端到端（3个测试用例）

**总测试用例**: 13个

**工具**:
- WebApplicationFactory
- In-Memory SQLite或SQL Server TestContainer
- WPF UI Automation（可选）

**执行时机**: Phase 4专门执行

### 数据库测试

**覆盖范围**:
- **Task 4.2.2**: 数据迁移脚本验证（8个验证步骤）

**工具**:
- SQL Server测试数据库
- SQL脚本验证

**执行时机**: Phase 1完成后，生产部署前

### 用户验收测试（UAT）

**覆盖范围**:
- **Task 3.2.4**: 6个UAT场景，共53个验证步骤

**执行时机**: Phase 3完成后

**参与人**: 医生用户、测试人员、产品经理

---

## 📦 交付物清单

### 代码文件

**Server端**（8个文件修改/新建）:
1. `MedicalCaseService.cs`（修改）
2. `IMedicalCaseService.cs`（修改）
3. `MedicalCaseController.cs`（修改）
4. `MedicalCaseRepository.cs`（修改）
5. `IMedicalCaseRepository.cs`（修改）
6. `MedicalCaseServiceTests.cs`（新建）
7. `MedicalCaseCreationTests.cs`（新建）
8. `PermissionControlTests.cs`（新建）

**Desktop端**（7个文件修改/新建）:
1. `PatientSelectionViewModel.cs`（修改）
2. `PatientSelectionView.xaml`（修改）
3. `UnfinishedCaseHandler.cs`（修改）
4. `IMedicalCaseApi.cs`（修改）
5. `InverseBooleanToVisibilityConverter.cs`（新建）
6. `PatientSelectionViewModelTests.cs`（新建）
7. `PatientSelectionE2ETests.cs`（新建）

**数据库**（3个文件新建）:
1. `medicalcase-doctorid-migration.sql`（新建）
2. `medicalcase-doctorid-check.sql`（新建）
3. `AddDoctorIdCheckConstraint.cs`（EF Migration，新建）

### 文档文件

**开发规范**（2个文件更新/新建）:
1. `docs/guides/development-standards.md`（更新）
2. `docs/architecture/decisions/ADR-XXX-user-context-pattern.md`（新建）

**测试文档**（2个文件新建）:
1. `docs/testing/uat-patient-selection-phase3.md`（新建）
2. `scripts/migrations/medicalcase-doctorid-migration-test.sql`（新建）

**审计报告**（1个文件新建）:
1. `docs/audits/service-create-methods-audit-2025-11-22.md`（新建）

### 总交付物统计

- **代码文件**: 18个（Server 8个 + Desktop 7个 + Database 3个）
- **文档文件**: 5个（规范2个 + 测试2个 + 审计1个）
- **总计**: 23个文件

---

## 💡 下一步操作

1. **审查task文档**: 确认任务拆分合理性、工作量估算准确性、依赖关系正确性
2. **调整任务粒度**（如果需要）: 如果某些任务>4小时，手动拆分成更小任务
3. **确认Epic编号**: 创建GitHub Epic Issue，获取Epic编号
4. **批量生成Issues**: 使用 `lybtzyzs-issue-template` skill读取本task文档，批量生成26个子Issues
5. **分配任务**: 根据团队成员技能分配任务（Server端专家 vs Desktop端专家）
6. **启动实施**: 按Phase顺序开始执行任务

---

## 📞 支持与反馈

- **任务分解文档**: 本文档
- **设计文档**: [docs/explanation/design/patient-selection-optimization-p0-bug-fix-design.md](../explanation/design/patient-selection-optimization-p0-bug-fix-design.md)
- **需求文档**: [docs/explanation/requirements/patient-selection-optimization-discussion.md](../explanation/requirements/patient-selection-optimization-discussion.md)
- **问题反馈**: 创建GitHub Issue，标签 `question` 或 `task-breakdown`

---

**文档状态**: ✅ 任务分解完成，等待审查和批量生成Issues
**下一步**: 调用 `lybtzyzs-issue-template` 批量生成GitHub Issues
**最后更新**: 2025-11-22
**版本**: v1.0
**总任务数**: 26个
**总工作量**: 28.5小时（24-33小时区间）

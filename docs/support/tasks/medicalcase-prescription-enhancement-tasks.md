# 医案模块-处方功能增强 任务分解文档

## 📋 元数据

- **Epic**: 医案模块完善
- **设计文档**: `docs/explanation/architecture/client/medicalcase-prescription-enhancement-design.md` (v1.0)
- **需求文档**: `docs/explanation/architecture/client/medicalcase-prescription-enhancement-requirements.md` (v1.1)
- **总工作量**: 72-98小时 (约9-12天，按每天8小时计算)
- **实施阶段**: Phase 1-4
- **任务总数**: 34个
- **关键路径**: 14个任务

## 🎯 任务清单（Task Checklist）

### Phase 1: 数据层与BF-002基础（预计9.5-13小时）

#### Task 1.1: 创建BF-002数据库Migration
- **工作量**: 1-1.5小时
- **依赖**: 无
- **类型**: Database Migration
- **文件范围**:
  - `src/Server/Infrastructure/Migrations/{timestamp}_AddBF002Fields.cs`
- **验收标准**:
  - [ ] Migration脚本创建成功
  - [ ] Up方法包含3个字段添加: `MedicalCases.NeedsPrescription`, `Consultations.Step1CompletedAt`, `Consultations.Step2CompletedAt`
  - [ ] Down方法包含字段删除逻辑
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 使用`dotnet ef migrations add AddBF002Fields`命令
  - `NeedsPrescription`: `bool?` nullable类型
  - `Step1CompletedAt`, `Step2CompletedAt`: `DateTime?` nullable类型
  - 添加XML注释说明BF-002业务规则

#### Task 1.2: 更新Entity模型
- **工作量**: 1-1.5小时
- **依赖**: Task 1.1
- **类型**: Entity Model
- **文件范围**:
  - `src/Server/Domain/Entities/MedicalCase.cs`
  - `src/Server/Domain/Entities/Consultation.cs`
- **验收标准**:
  - [ ] `MedicalCase.NeedsPrescription`属性添加成功
  - [ ] `Consultation.Step1CompletedAt`属性添加成功
  - [ ] `Consultation.Step2CompletedAt`属性添加成功
  - [ ] 所有属性包含XML注释
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 属性定义格式: `public bool? NeedsPrescription { get; set; }`
  - XML注释说明BF-002业务含义
  - 不需要修改导航属性

#### Task 1.3: 创建BF-002相关DTO
- **工作量**: 2-3小时
- **依赖**: Task 1.2
- **类型**: DTO
- **文件范围**:
  - `src/Shared/LYBT.Shared.Models/DTOs/MedicalCase/UpdateConsultationRequest.cs`
  - `src/Shared/LYBT.Shared.Models/DTOs/MedicalCase/SetPrescriptionFlagRequest.cs`
  - `src/Shared/LYBT.Shared.Models/DTOs/MedicalCase/ConsultationDto.cs`
- **验收标准**:
  - [ ] 3个DTO类创建成功
  - [ ] 包含FluentValidation验证规则（主诉、中医诊断必填）
  - [ ] 包含XML注释
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - `UpdateConsultationRequest`: 包含所有诊断字段
  - `SetPrescriptionFlagRequest`: 只包含`NeedsPrescription`
  - `ConsultationDto`: 包含`Step1CompletedAt`, `Step2CompletedAt`响应字段
  - 使用`[Required]`, `[MaxLength]`特性

#### Task 1.4: 创建Prescription相关DTO
- **工作量**: 2-3小时
- **依赖**: 无（可与Task 1.3并行）
- **类型**: DTO
- **文件范围**:
  - `src/Shared/LYBT.Shared.Models/DTOs/Prescription/PrescriptionInputDto.cs`
  - `src/Shared/LYBT.Shared.Models/DTOs/Prescription/PrescriptionItemInputDto.cs`
  - `src/Shared/LYBT.Shared.Models/DTOs/Prescription/PrescriptionDetailResponse.cs`
- **验收标准**:
  - [ ] 3个DTO类创建成功
  - [ ] `PrescriptionInputDto`包含Items集合、剂数、折扣
  - [ ] `PrescriptionItemInputDto`包含HerbId、剂量、单位
  - [ ] `PrescriptionDetailResponse`包含价格快照字段（UnitPrice, ItemAmount）
  - [ ] 包含FluentValidation验证规则（至少1个药材、剂量范围、剂数范围）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - `PrescriptionInputDto.Items`: `List<PrescriptionItemInputDto>`
  - `PrescriptionItemInputDto.Dosage`: `decimal`类型，验证范围0.1-999.9
  - `DosageCount`: 验证范围1-100
  - `Discount`: 验证范围0-1（0%到100%）

#### Task 1.5: 配置AutoMapper Profile
- **工作量**: 1.5-2小时
- **依赖**: Task 1.3, Task 1.4
- **类型**: Configuration
- **文件范围**:
  - `src/Server/Application/MappingProfiles/MedicalCaseMappingProfile.cs`
- **验收标准**:
  - [ ] Entity → DTO映射配置完成
  - [ ] DTO → Entity映射配置完成
  - [ ] 嵌套对象映射配置正确（Consultation, Prescription, PrescriptionItems）
  - [ ] AutoMapper配置验证测试通过
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - `CreateMap<MedicalCase, MedicalCaseDetailDto>()`
  - `CreateMap<Consultation, ConsultationDto>()`
  - `CreateMap<Prescription, PrescriptionDetailResponse>()`
  - `ForMember`配置嵌套映射
  - 单元测试: `MappingProfileTests.cs`验证所有映射

#### Task 1.6: 更新Repository接口和实现
- **工作量**: 1.5-2小时
- **依赖**: Task 1.2
- **类型**: Repository
- **文件范围**:
  - `src/Server/Domain/Repositories/IMedicalCaseRepository.cs`
  - `src/Server/Infrastructure/Repositories/MedicalCaseRepository.cs`
- **验收标准**:
  - [ ] `GetByIdAsync`方法Include优化（.Include(Consultation).Include(Prescription).ThenInclude(Items)）
  - [ ] `UpdateAsync`方法支持BF-002字段更新
  - [ ] Repository单元测试通过（Mock DbContext）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - Include优化避免N+1查询
  - 使用`AsNoTracking()`优化只读查询
  - Repository可见性: `internal`（遵循AC-001）

---

### Phase 2: 业务逻辑与API实现（预计19-26小时）

#### Task 2.1: 实现MedicalCaseService - Consultation相关方法
- **工作量**: 3-4小时
- **依赖**: Task 1.5, Task 1.6
- **类型**: Service
- **文件范围**:
  - `src/Server/Application/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] `UpdateConsultationAsync`方法实现（保存诊断数据）
  - [ ] `CompleteConsultationStep1Async`方法实现（设置Step1CompletedAt）
  - [ ] `SetPrescriptionFlagAsync`方法实现（设置NeedsPrescription + Step2CompletedAt）
  - [ ] 包含业务规则验证（主诉+中医诊断必填才能完成Step1）
  - [ ] 包含异常处理（NotFoundException, BusinessRuleException）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - `UpdateConsultationAsync`: 更新Consultation实体，不触发时间戳
  - `CompleteConsultationStep1Async`: 验证数据有效性，设置`Step1CompletedAt = DateTime.UtcNow`
  - `SetPrescriptionFlagAsync`: 设置`NeedsPrescription`和`Step2CompletedAt`
  - 使用AutoMapper进行DTO映射
  - 异步操作: `async Task<T>`

#### Task 2.2: 实现MedicalCaseService - Prescription相关方法
- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Application/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] `CreatePrescriptionAsync`方法实现
  - [ ] `UpdatePrescriptionAsync`方法实现
  - [ ] 价格计算逻辑实现（BR-003: 查询Herbs表获取当前价格）
  - [ ] 价格快照保存到`PrescriptionItem.UnitPrice`
  - [ ] `ItemAmount`和`TotalAmount`计算正确
  - [ ] 包含业务规则验证（AR-003: 一诊一方，不能重复创建处方）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 价格计算公式: `ItemAmount = UnitPrice × Dosage`
  - 总价计算: `TotalAmount = Σ(ItemAmount) × DosageCount × (1 - Discount)`
  - 查询Herbs表: `await _herbRepository.GetByIdAsync(herbId)`
  - AR-003验证: `if (medicalCase.Prescription != null) throw new BusinessRuleException(...)`
  - 使用EF Core批量插入PrescriptionItems

#### Task 2.3: 实现MedicalCaseService - Complete方法
- **工作量**: 2-3小时
- **依赖**: Task 2.1, Task 2.2
- **类型**: Service
- **文件范围**:
  - `src/Server/Application/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] `CompleteAsync`方法实现
  - [ ] BF-002完整验证逻辑（Step1 + Step2时间戳必须存在）
  - [ ] 如果NeedsPrescription=true，验证Prescription存在
  - [ ] 更新`MedicalCase.Status = MedicalCaseStatus.Completed`
  - [ ] 包含详细异常消息
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - BF-002验证:
    ```csharp
    if (medicalCase.Consultation?.Step1CompletedAt == null)
        throw new BusinessRuleException("未完成辨证 (Step 1)");
    if (medicalCase.Consultation?.Step2CompletedAt == null)
        throw new BusinessRuleException("未标记处方需求 (Step 2)");
    if (medicalCase.NeedsPrescription == true && medicalCase.Prescription == null)
        throw new BusinessRuleException("已标记需要处方，但未开具处方");
    ```
  - 状态更新: `medicalCase.Status = MedicalCaseStatus.Completed`

#### Task 2.4: 实现MedicalCaseController - BF-002端点
- **工作量**: 2-3小时
- **依赖**: Task 2.1
- **类型**: Controller
- **文件范围**:
  - `src/Server/Presentation/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] `PUT /api/v1/medicalcases/{id}/consultation`端点实现
  - [ ] `PUT /api/v1/medicalcases/{id}/consultation/complete-step1`端点实现
  - [ ] `PUT /api/v1/medicalcases/{id}/prescription-flag`端点实现
  - [ ] Swagger文档生成正确
  - [ ] 异常处理中间件集成（400/404/422响应）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 遵循聚合根约束（BR-001）: 路径必须通过`/medicalcases/{id}/...`
  - 使用`[HttpPut]`, `[Route]`, `[ProducesResponseType]`特性
  - 返回`ActionResult<T>`
  - 异步操作: `async Task<ActionResult<T>>`

#### Task 2.5: 实现MedicalCaseController - Prescription端点
- **工作量**: 2-3小时
- **依赖**: Task 2.2
- **类型**: Controller
- **文件范围**:
  - `src/Server/Presentation/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] `POST /api/v1/medicalcases/{caseId}/prescription`端点实现
  - [ ] `PUT /api/v1/medicalcases/{caseId}/prescription`端点实现
  - [ ] `GET /api/v1/medicalcases/{caseId}/prescription`端点实现
  - [ ] Swagger文档生成正确
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - POST: 创建处方，返回201 Created
  - PUT: 更新处方，返回200 OK
  - GET: 查询处方详情，返回`PrescriptionDetailResponse`
  - 遵循聚合根路径约束

#### Task 2.6: 实现MedicalCaseController - Complete和Read端点
- **工作量**: 1.5-2小时
- **依赖**: Task 2.3
- **类型**: Controller
- **文件范围**:
  - `src/Server/Presentation/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] `PUT /api/v1/medicalcases/{id}/complete`端点实现
  - [ ] `GET /api/v1/medicalcases/{id}`端点实现（包含Consultation和Prescription详情）
  - [ ] Swagger文档生成正确
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - Complete端点: 调用`CompleteAsync`方法，处理BF-002验证异常
  - Read端点: 返回`MedicalCaseDetailDto`，包含嵌套数据
  - 使用AutoMapper映射

#### Task 2.7: 编写Service层单元测试
- **工作量**: 3-4小时
- **依赖**: Task 2.3
- **类型**: Unit Test
- **文件范围**:
  - `tests/UnitTests/Server/Application/MedicalCaseServiceTests.cs`
- **验收标准**:
  - [ ] 测试`UpdateConsultationAsync`方法
  - [ ] 测试`CompleteConsultationStep1Async`方法（包含验证逻辑）
  - [ ] 测试`SetPrescriptionFlagAsync`方法
  - [ ] 测试`CreatePrescriptionAsync`方法（包含价格计算）
  - [ ] 测试`CompleteAsync`方法（包含BF-002验证）
  - [ ] 测试AR-003验证（一诊一方）
  - [ ] 所有测试通过
  - [ ] 代码覆盖率 ≥ 80%
- **技术要点**:
  - 使用NSubstitute Mock Repository
  - 使用AAA模式（Arrange-Act-Assert）
  - 测试成功和失败场景
  - 验证异常类型和消息

#### Task 2.8: 编写Controller集成测试
- **工作量**: 2-3小时
- **依赖**: Task 2.6
- **类型**: Integration Test
- **文件范围**:
  - `tests/IntegrationTests/Server/Presentation/MedicalCaseControllerTests.cs`
- **验收标准**:
  - [ ] 测试完整BF-002流程（创建医案 → 辨证 → 标记 → 开处方 → 完成）
  - [ ] 测试所有API端点
  - [ ] 测试错误场景（404, 422等）
  - [ ] 所有测试通过
  - [ ] 使用真实数据库（TestServer + InMemory Database）
- **技术要点**:
  - 使用`WebApplicationFactory<Program>`
  - 使用InMemory Database或TestContainers
  - Mock认证中间件
  - 验证HTTP状态码和响应内容

---

### Phase 3: Client端UI与交互（预计28-37小时）

#### Task 3.1: 创建一体化界面XAML基础布局
- **工作量**: 2-3小时
- **依赖**: 无
- **类型**: XAML View
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEditorView.xaml`
- **验收标准**:
  - [ ] Grid分栏布局完成（左40% + 右60%）
  - [ ] 诊断区基础控件（主诉、中医诊断、望闻问切、处方选择RadioBox）
  - [ ] 处方区Overlay提示层
  - [ ] 底部按钮区（[保存草稿] [保存并完成]）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 使用`<Grid.ColumnDefinitions>`定义分栏: `4*` 和 `6*`
  - Overlay使用`<Border>`覆盖处方区，绑定`Visibility`到`CanEditPrescription`的反转
  - MaterialDesign样式

#### Task 3.2: 实现MedicalCaseFormViewModel - 状态管理
- **工作量**: 3-4小时
- **依赖**: Task 2.6
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFormViewModel.cs`
- **验收标准**:
  - [ ] 状态属性实现: `IsConsultationCompleted`, `IsPrescriptionFlagSet`, `CanEditPrescription`
  - [ ] 诊断区属性绑定: `ChiefComplaint`, `TCMDiagnosis`, `NeedsPrescription`
  - [ ] 处方区属性绑定: `PrescriptionItems`, `DosageCount`, `Discount`, `SubTotal`, `TotalAmount`
  - [ ] `PropertyChanged`事件触发正确
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 继承`UnifiedViewModelBase`
  - 状态属性使用计算属性:
    ```csharp
    public bool CanEditPrescription =>
        IsConsultationCompleted &&
        IsPrescriptionFlagSet &&
        _needsPrescription == true;
    ```
  - 使用`SetProperty`触发PropertyChanged
  - 注入`IMedicalCaseApiClient`（Refit接口）

#### Task 3.3: 实现MedicalCaseFormViewModel - SaveDraftCommand
- **工作量**: 3-4小时
- **依赖**: Task 3.2
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseFormViewModel.cs`
- **验收标准**:
  - [ ] `SaveDraftCommand`实现
  - [ ] `SaveDraftAsync`方法实现（BF-002自动化流程）
  - [ ] 自动完成Step1逻辑（如果数据有效）
  - [ ] 自动完成Step2逻辑（如果已勾选处方选择）
  - [ ] 刷新医案状态后触发`CanEditPrescription`更新
  - [ ] 包含异常处理和通知
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 核心自动化流程:
    ```csharp
    await _apiClient.UpdateConsultationAsync(dto);
    if (!IsConsultationCompleted && IsConsultationDataValid())
        await _apiClient.CompleteConsultationStep1Async(id);
    if (NeedsPrescription.HasValue && !IsPrescriptionFlagSet)
        await _apiClient.SetPrescriptionFlagAsync(id, NeedsPrescription.Value);
    await RefreshMedicalCaseAsync();
    ```
  - 使用`INotificationService`显示成功/失败消息
  - 异常处理: `try-catch`包裹所有API调用

#### Task 3.4: 实现HerbCardControl组件
- **工作量**: 2-3小时
- **依赖**: 无（可与其他任务并行）
- **类型**: UserControl
- **文件范围**:
  - `src/Client/Desktop/Shared/Components/HerbCardControl.xaml`
  - `src/Client/Desktop/Shared/Components/HerbCardControl.xaml.cs`
- **验收标准**:
  - [ ] 复制Formula模块的HerbCardControl成功
  - [ ] 添加`IsPriceVisible`依赖属性
  - [ ] 添加`UnitPrice`和`ItemAmount`绑定
  - [ ] 删除按钮（编辑模式）正常工作
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 依赖属性定义:
    ```csharp
    public static readonly DependencyProperty IsPriceVisibleProperty =
        DependencyProperty.Register(nameof(IsPriceVisible), typeof(bool), typeof(HerbCardControl));
    ```
  - XAML绑定: `Visibility="{Binding IsPriceVisible, Converter={StaticResource BoolToVisibility}}"`
  - 价格显示格式: `¥{UnitPrice:N2}`, `小计: ¥{ItemAmount:N2}`

#### Task 3.5: 实现PrescriptionItemViewModel和价格计算
- **工作量**: 2-3小时
- **依赖**: Task 3.4
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionItemViewModel.cs`
- **验收标准**:
  - [ ] 属性定义: `HerbId`, `HerbName`, `Dosage`, `Unit`, `UnitPrice`, `ItemAmount`
  - [ ] `ItemAmount`计算属性实现（UnitPrice × Dosage）
  - [ ] `PropertyChanged`事件触发`TotalAmount`更新
  - [ ] 剂量范围验证（0.1-999.9）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 计算属性:
    ```csharp
    public decimal ItemAmount => UnitPrice * Dosage;
    ```
  - `Dosage`属性setter触发`OnPropertyChanged(nameof(ItemAmount))`
  - 使用`INotifyPropertyChanged`接口

#### Task 3.6: 实现7级拼音过滤算法
- **工作量**: 3-4小时
- **依赖**: 无（独立工具类）
- **类型**: Utility
- **文件范围**:
  - `src/Client/Desktop/Shared/Utilities/PrescriptionHerbFilterManager.cs`
- **验收标准**:
  - [ ] `GetMatchScore`方法实现（100/90/80/70/50/40/30分评分算法）
  - [ ] `IsPinyinFuzzyMatch`方法实现
  - [ ] `FilterHerbs`方法实现（返回前5个结果）
  - [ ] 响应时间 < 100ms（性能要求）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 7级评分逻辑（参考Formula模块）:
    - 100分: 中文名精确匹配
    - 90分: 拼音全码精确匹配
    - 80分: 拼音首字母全匹配
    - 70分: 中文名包含
    - 50分: 拼音全码模糊匹配
    - 40分: 拼音首字母模糊匹配
    - 30分: 部分字符匹配
  - 使用`PinyinHelper.GetPinyin()`
  - 排序: `OrderByDescending(x => x.Score).Take(5)`

#### Task 3.7: 实现键盘导航功能
- **工作量**: 2-3小时
- **依赖**: Task 3.4, Task 3.5
- **类型**: Behavior
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEditorView.xaml.cs`
  - `src/Client/Desktop/Shared/Behaviors/KeyboardNavigationBehavior.cs`
- **验收标准**:
  - [ ] Enter键焦点跳转: HerbName → Dosage → Next Card
  - [ ] 最后一个卡片Enter时自动创建新卡片
  - [ ] Tab键切换支持
  - [ ] 全程键盘操作可录入10个药材
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 使用`PreviewKeyDown`事件
  - 焦点管理: `element.Focus()`, `Keyboard.Focus(element)`
  - 新卡片创建: `PrescriptionItems.Add(new PrescriptionItemViewModel())`
  - 使用Prism Behaviors或Attached Properties

#### Task 3.8: 实现经验方导入对话框
- **工作量**: 3-4小时
- **依赖**: Task 3.5
- **类型**: Dialog
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialog.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialogViewModel.cs`
- **验收标准**:
  - [ ] 左右分栏布局（40% 经验方列表 + 60% 详情）
  - [ ] 经验方列表绑定、搜索框实现
  - [ ] 经验方详情显示（包含药材列表）
  - [ ] 导入操作实现（调用Formula API获取数据）
  - [ ] 导入后查询当前价格（调用Herbs API）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 注入`IFormulaApiClient`和`IHerbsApiClient`
  - 导入逻辑:
    ```csharp
    var formula = await _formulaApi.GetByIdAsync(selectedId);
    foreach (var item in formula.Items)
    {
        var herb = await _herbsApi.GetByIdAsync(item.HerbId);
        PrescriptionItems.Add(new PrescriptionItemViewModel
        {
            HerbId = item.HerbId,
            HerbName = item.HerbName,
            Dosage = item.Dosage,
            UnitPrice = herb.Price  // 使用当前价格
        });
    }
    ```

#### Task 3.9: 实现历史处方导入对话框
- **工作量**: 3-4小时
- **依赖**: Task 3.5
- **类型**: Dialog
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryPrescriptionImportDialog.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryPrescriptionImportDialogViewModel.cs`
- **验收标准**:
  - [ ] 左右分栏布局（40% 历史处方列表 + 60% 详情）
  - [ ] 历史处方列表绑定、筛选框实现
  - [ ] 处方详情显示（包含药材列表、价格快照）
  - [ ] 导入操作实现（使用当前价格，忽略快照价格）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 查询当前患者历史处方:
    ```csharp
    var prescriptions = await _medicalCaseApi.GetPatientHistoryPrescriptionsAsync(patientId);
    ```
  - 导入时重新查询当前价格（与Task 3.8相同逻辑）

#### Task 3.10: 实现重复药材聚合提醒
- **工作量**: 1.5-2小时
- **依赖**: Task 3.8, Task 3.9
- **类型**: Dialog
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/DuplicateHerbAlertDialog.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/DuplicateHerbAlertDialogViewModel.cs`
- **验收标准**:
  - [ ] 一次性聚合提醒对话框
  - [ ] 显示所有重复项: "当归: 10g → 15g (合并为15g)"
  - [ ] 合并规则: `Math.Max(currentDosage, importedDosage)`
  - [ ] 用户确认后执行合并
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - 重复检测:
    ```csharp
    var duplicates = importedItems
        .Where(i => existingItems.Any(e => e.HerbId == i.HerbId))
        .ToList();
    ```
  - 合并逻辑:
    ```csharp
    var existing = existingItems.First(e => e.HerbId == duplicate.HerbId);
    existing.Dosage = Math.Max(existing.Dosage, duplicate.Dosage);
    ```

#### Task 3.11: 集成所有组件并完成UI绑定
- **工作量**: 2-3小时
- **依赖**: Task 3.3, Task 3.7, Task 3.10
- **类型**: XAML View
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseEditorView.xaml`
- **验收标准**:
  - [ ] 完整XAML绑定到ViewModel属性
  - [ ] Overlay `Visibility`绑定到`CanEditPrescription`反转
  - [ ] Command绑定: `SaveDraftCommand`, `ImportFormulaCommand`, `ImportHistoryCommand`
  - [ ] `IsEnabled`绑定到`CanEditPrescription`
  - [ ] 编译通过: 0 errors, 0 warnings
  - [ ] UI功能测试通过（完整流程）
- **技术要点**:
  - 使用`InverseBoolToVisibilityConverter`
  - 完整流程测试:
    1. 填写诊断 → 保存草稿 → 处方区解锁
    2. 添加药材（键盘操作）→ 价格实时计算
    3. 导入经验方 → 重复提醒 → 合并
    4. 保存并完成

---

### Phase 4: 测试与优化（预计15-21.5小时）

#### Task 4.1: 编写拼音过滤算法单元测试
- **工作量**: 2-3小时
- **依赖**: Task 3.6
- **类型**: Unit Test
- **文件范围**:
  - `tests/UnitTests/Client/Utilities/PrescriptionHerbFilterManagerTests.cs`
- **验收标准**:
  - [ ] 测试7级评分逻辑（每个级别至少1个测试用例）
  - [ ] 测试边界条件（空字符串、特殊字符、超长输入）
  - [ ] 测试性能（100ms内完成1000个药材过滤）
  - [ ] 所有测试通过
  - [ ] 代码覆盖率 ≥ 90%
- **技术要点**:
  - 使用xUnit或NUnit
  - 测试数据: 准备50+中药材样本
  - 性能测试: `Stopwatch.StartNew()`

#### Task 4.2: 编写ViewModel单元测试
- **工作量**: 3-4小时
- **依赖**: Task 3.11
- **类型**: Unit Test
- **文件范围**:
  - `tests/UnitTests/Client/ViewModels/MedicalCaseFormViewModelTests.cs`
  - `tests/UnitTests/Client/ViewModels/PrescriptionItemViewModelTests.cs`
- **验收标准**:
  - [ ] 测试状态管理逻辑（`CanEditPrescription`计算）
  - [ ] 测试`SaveDraftCommand`自动化流程
  - [ ] 测试价格计算逻辑（`ItemAmount`, `TotalAmount`）
  - [ ] 测试PropertyChanged事件触发
  - [ ] 所有测试通过
  - [ ] 代码覆盖率 ≥ 80%
- **技术要点**:
  - Mock API Client: `NSubstitute.Substitute.For<IMedicalCaseApiClient>()`
  - 测试PropertyChanged: 订阅事件并验证触发
  - 测试Command: `command.Execute(null)`并验证结果

#### Task 4.3: 性能优化 - 拼音过滤响应
- **工作量**: 1.5-2小时
- **依赖**: Task 4.1
- **类型**: Optimization
- **文件范围**:
  - `src/Client/Desktop/Shared/Utilities/PrescriptionHerbFilterManager.cs`
- **验收标准**:
  - [ ] 响应时间 < 100ms（1000个药材数据集）
  - [ ] 使用并行处理或缓存优化
  - [ ] 性能测试通过
- **技术要点**:
  - 缓存拼音码: 预计算所有药材的拼音全码和首字母
  - 使用`Parallel.ForEach`或LINQ并行查询
  - 减少字符串操作: 使用`StringBuilder`或`Span<T>`

#### Task 4.4: 性能优化 - Repository Include预加载
- **工作量**: 1.5-2小时
- **依赖**: Task 2.8
- **类型**: Optimization
- **文件范围**:
  - `src/Server/Infrastructure/Repositories/MedicalCaseRepository.cs`
- **验收标准**:
  - [ ] Include优化避免N+1查询
  - [ ] 查询性能测试通过（100个医案查询 < 500ms）
  - [ ] 使用`AsNoTracking()`优化只读查询
- **技术要点**:
  - Include链:
    ```csharp
    .Include(m => m.Consultation)
    .Include(m => m.Prescription)
        .ThenInclude(p => p.Items)
    ```
  - 只读查询使用`AsNoTracking()`
  - 性能测试: 使用BenchmarkDotNet或Stopwatch

#### Task 4.5: 同步Server端架构文档
- **工作量**: 1-1.5小时
- **依赖**: Task 2.6
- **类型**: Documentation
- **文件范围**:
  - `docs/explanation/architecture/server/README.md`
- **验收标准**:
  - [ ] 更新MedicalCase模块API文档（8个新端点）
  - [ ] 更新BF-002业务规则说明
  - [ ] 更新聚合根API路径示例
  - [ ] Markdown格式正确
- **技术要点**:
  - 添加API端点表格（路径、方法、说明、请求体、响应体）
  - 添加BF-002流程图（Mermaid）
  - 添加代码示例

#### Task 4.6: 同步Client端架构文档
- **工作量**: 1-1.5小时
- **依赖**: Task 3.11
- **类型**: Documentation
- **文件范围**:
  - `docs/explanation/architecture/client/README.md`
- **验收标准**:
  - [ ] 更新一体化界面设计说明
  - [ ] 更新ViewModel状态管理模式
  - [ ] 更新组件复用说明（HerbCardControl）
  - [ ] Markdown格式正确
- **技术要点**:
  - 添加UI布局截图（ASCII艺术图或Mermaid）
  - 添加状态管理代码示例
  - 添加键盘导航使用说明

#### Task 4.7: 同步业务规则文档
- **工作量**: 1-1.5小时
- **依赖**: Task 2.3
- **类型**: Documentation
- **文件范围**:
  - `docs/explanation/business-rules.md`
- **验收标准**:
  - [ ] 更新BF-002策略C说明
  - [ ] 添加自动化流程描述
  - [ ] 添加时间戳验证规则
  - [ ] Markdown格式正确
- **技术要点**:
  - 对比策略A/B/C差异表格
  - 添加自动化流程伪代码
  - 添加BF-002验证逻辑代码示例

#### Task 4.8: 代码质量检查和修复
- **工作量**: 2-3小时
- **依赖**: Task 3.11, Task 4.2
- **类型**: Quality Check
- **文件范围**:
  - 全部新增和修改的代码文件
- **验收标准**:
  - [ ] 运行`lybtzyzs-code-review` skill完成
  - [ ] 修复所有严重问题（Severity: High）
  - [ ] 修复所有警告（Severity: Medium）
  - [ ] 补充XML注释（public方法和类）
  - [ ] 编译通过: 0 errors, 0 warnings
- **技术要点**:
  - Code Review检查项:
    - 命名规范（PascalCase, camelCase）
    - MVVM模式遵循
    - 异步方法命名（AsyncSuffix）
    - 中文注释
  - 使用`/// <summary>`添加XML注释

#### Task 4.9: 架构合规性检查
- **工作量**: 1.5-2小时
- **依赖**: Task 4.8
- **类型**: Compliance Check
- **文件范围**:
  - Server端API路径、Repository可见性
- **验收标准**:
  - [ ] 运行`lybtzyzs-arch-compliance` skill完成
  - [ ] 验证聚合根API路径正确（所有Prescription端点通过`/medicalcases/{id}/...`）
  - [ ] 验证Repository可见性（`internal`修饰符）
  - [ ] 验证Service依赖注入（通过构造函数）
  - [ ] 所有检查通过
- **技术要点**:
  - 聚合根约束验证（BR-001）
  - Repository封装验证（AC-001）
  - 三层架构验证（Controller → Service → Repository）

---

## 📊 任务统计

- **总任务数**: 34个
- **总工作量**: 72-98小时（约9-12天，按每天8小时计算）
- **Phase数量**: 4个
- **关键路径长度**: 14个任务

### Phase统计

| Phase | 任务数 | 工作量（小时） | 工作量（天） |
|-------|--------|----------------|--------------|
| Phase 1 | 6 | 9.5-13 | 1.2-1.6 |
| Phase 2 | 8 | 19-26 | 2.4-3.3 |
| Phase 3 | 11 | 28-37 | 3.5-4.6 |
| Phase 4 | 9 | 15-21.5 | 1.9-2.7 |
| **总计** | **34** | **71.5-97.5** | **8.9-12.2** |

### 任务类型分布

| 类型 | 任务数 | 占比 |
|------|--------|------|
| Service | 3 | 8.8% |
| Controller | 3 | 8.8% |
| ViewModel | 6 | 17.6% |
| XAML View | 2 | 5.9% |
| DTO | 2 | 5.9% |
| Repository | 1 | 2.9% |
| Component | 1 | 2.9% |
| Utility | 1 | 2.9% |
| Dialog | 3 | 8.8% |
| Test | 5 | 14.7% |
| Documentation | 3 | 8.8% |
| Other | 4 | 11.8% |

---

## 🔗 依赖关系图

### Phase 1依赖

```
Task 1.1 (Migration)
  └─> Task 1.2 (Entity)
        ├─> Task 1.3 (BF-002 DTO)
        │     └─> Task 1.5 (AutoMapper)
        ├─> Task 1.4 (Prescription DTO) [并行]
        │     └─> Task 1.5 (AutoMapper)
        └─> Task 1.6 (Repository)
```

### Phase 2依赖

```
Task 1.5, Task 1.6
  └─> Task 2.1 (Service - Consultation)
        ├─> Task 2.2 (Service - Prescription)
        │     ├─> Task 2.3 (Service - Complete)
        │     │     ├─> Task 2.6 (Controller - Complete)
        │     │     │     └─> Task 2.8 (Controller Tests)
        │     │     └─> Task 2.7 (Service Tests)
        │     └─> Task 2.5 (Controller - Prescription)
        └─> Task 2.4 (Controller - BF-002)
```

### Phase 3依赖

```
无依赖 → Task 3.1 (XAML基础)
无依赖 → Task 3.4 (HerbCard)
  └─> Task 3.5 (PrescriptionItemViewModel)
        ├─> Task 3.7 (键盘导航)
        ├─> Task 3.8 (经验方导入)
        │     └─> Task 3.10 (重复提醒)
        └─> Task 3.9 (历史处方导入)
              └─> Task 3.10 (重复提醒)

Task 2.6 → Task 3.2 (ViewModel状态)
  └─> Task 3.3 (SaveDraftCommand)

Task 3.3, Task 3.7, Task 3.10
  └─> Task 3.11 (UI集成)

无依赖 → Task 3.6 (拼音算法)
```

### Phase 4依赖

```
Task 3.6 → Task 4.1 (拼音测试)
  └─> Task 4.3 (拼音优化)

Task 3.11 → Task 4.2 (ViewModel测试)

Task 2.8 → Task 4.4 (Repository优化)

Task 2.6 → Task 4.5 (Server文档)
Task 3.11 → Task 4.6 (Client文档)
Task 2.3 → Task 4.7 (业务规则文档)

Task 3.11, Task 4.2 → Task 4.8 (代码质量)
  └─> Task 4.9 (架构合规)
```

### 跨Phase依赖

```
Phase 1 → Phase 2
  Task 1.5, Task 1.6 → Task 2.1

Phase 2 → Phase 3
  Task 2.6 → Task 3.2

Phase 3 → Phase 4
  Task 3.6 → Task 4.1
  Task 3.11 → Task 4.2
```

---

## ⚠️ 关键路径

**主线任务**（必须按顺序完成，不可并行）：

1. 🔴 Task 1.1: 创建BF-002数据库Migration
2. 🔴 Task 1.2: 更新Entity模型
3. 🔴 Task 1.3: 创建BF-002相关DTO
4. 🔴 Task 1.5: 配置AutoMapper Profile
5. 🔴 Task 2.1: 实现MedicalCaseService - Consultation相关方法
6. 🔴 Task 2.2: 实现MedicalCaseService - Prescription相关方法
7. 🔴 Task 2.3: 实现MedicalCaseService - Complete方法
8. 🔴 Task 2.6: 实现MedicalCaseController - Complete和Read端点
9. 🔴 Task 3.2: 实现MedicalCaseFormViewModel - 状态管理
10. 🔴 Task 3.3: 实现MedicalCaseFormViewModel - SaveDraftCommand
11. 🔴 Task 3.11: 集成所有组件并完成UI绑定
12. 🔴 Task 4.2: 编写ViewModel单元测试
13. 🔴 Task 4.8: 代码质量检查和修复
14. 🔴 Task 4.9: 架构合规性检查

**关键路径长度**: 14个任务
**关键路径工作量**: 约24-33小时

**并行任务**（可同时进行）：

**Phase 1并行**:
- Task 1.3 (BF-002 DTO) || Task 1.4 (Prescription DTO)
  - 两者都依赖Task 1.2，但互不依赖
  - 可由不同开发者并行完成

**Phase 2并行**:
- Task 2.4 (Controller - BF-002) 和 Task 2.5 (Controller - Prescription) 可部分并行
  - Task 2.4依赖Task 2.1
  - Task 2.5依赖Task 2.2
  - 如Task 2.1完成时Task 2.2尚未开始，可提前开始Task 2.4
- Task 2.7 (Service Tests) || Task 2.8 (Controller Tests)
  - Task 2.7依赖Task 2.3
  - Task 2.8依赖Task 2.6
  - 可由不同开发者并行完成

**Phase 3并行**:
- Task 3.1 (XAML基础) || Task 3.4 (HerbCard) || Task 3.6 (拼音算法)
  - 三者完全独立，可并行
- Task 3.8 (经验方导入) || Task 3.9 (历史处方导入)
  - 两者都依赖Task 3.5，但互不依赖
  - 可由不同开发者并行完成

**Phase 4并行**:
- Task 4.1 (拼音测试) || Task 4.2 (ViewModel测试)
  - 依赖不同的前置任务
- Task 4.5 (Server文档) || Task 4.6 (Client文档) || Task 4.7 (业务规则文档)
  - 三者完全独立，可并行

---

## 📝 实施建议

### 优先级排序

1. 🔴 **高优先级**：关键路径任务（14个任务）
   - 必须按顺序完成，是项目的主线
   - 延期会直接影响整体进度

2. 🟡 **中优先级**：功能增强任务
   - Task 1.4 (Prescription DTO)
   - Task 2.4, 2.5 (Controller端点)
   - Task 3.1, 3.4, 3.6, 3.7 (UI组件和工具)
   - Task 3.8, 3.9, 3.10 (导入对话框)

3. 🟢 **低优先级**：测试和文档任务
   - Task 2.7 (Service Tests)
   - Task 4.1, 4.3, 4.4 (性能测试和优化)
   - Task 4.5, 4.6, 4.7 (文档同步)

### 并行策略

**阶段1：Phase 1 + Phase 2前期（约4-5天）**
- 主线开发者: Task 1.1 → 1.2 → 1.3 → 1.5 → 2.1 → 2.2 → 2.3
- 并行开发者: Task 1.4 (Phase 1) → Task 2.4, 2.5 (Phase 2)
- 测试工程师: 准备测试数据、Mock环境

**阶段2：Phase 2后期 + Phase 3前期（约3-4天）**
- 主线开发者: Task 2.6 → 3.2 → 3.3
- 并行开发者1: Task 2.7 (Service Tests) → Task 3.1 (XAML基础)
- 并行开发者2: Task 2.8 (Controller Tests) → Task 3.4 (HerbCard) → 3.5
- 独立工具开发: Task 3.6 (拼音算法)

**阶段3：Phase 3中后期（约3-4天）**
- 主线开发者: Task 3.11 (UI集成)
- 并行开发者1: Task 3.7 (键盘导航) → Task 3.8 (经验方导入)
- 并行开发者2: Task 3.9 (历史处方导入) → Task 3.10 (重复提醒)

**阶段4：Phase 4（约2-3天）**
- 测试工程师: Task 4.1 (拼音测试) → Task 4.3 (优化)
- 主线开发者: Task 4.2 (ViewModel测试) → Task 4.8 (质量检查) → Task 4.9 (合规检查)
- 文档工程师: Task 4.5 || Task 4.6 || Task 4.7 (并行完成文档)
- 性能优化: Task 4.4 (Repository优化)

### 风险提示

**技术风险**:
1. **BF-002自动化流程复杂度** (Task 3.3)
   - 风险: 自动完成Step1/Step2逻辑可能与UI状态不同步
   - 缓解: 提前编写详细单元测试，验证所有状态转换场景

2. **价格计算精度** (Task 2.2)
   - 风险: `decimal`浮点精度问题导致价格偏差
   - 缓解: 使用`decimal.Round(value, 2)`四舍五入到2位小数

3. **7级拼音算法性能** (Task 3.6)
   - 风险: 大数据集（1000+药材）响应时间超过100ms
   - 缓解: 提前进行性能测试，必要时使用缓存或并行处理

**依赖风险**:
1. **Task 2.6是关键瓶颈**
   - Task 2.6完成前，Phase 3主线无法开始
   - 建议: 尽早完成Task 2.6，或提前Mock API让Phase 3并行开发

2. **Task 3.11是Phase 3瓶颈**
   - 所有Phase 3任务最终汇聚到Task 3.11
   - 建议: 预留缓冲时间处理集成问题

**团队协作风险**:
1. **并行任务需要良好协调**
   - 风险: 并行开发可能导致代码冲突或接口不一致
   - 缓解: 使用Git分支策略，定期合并和代码评审

2. **DTO定义需提前对齐**
   - 风险: Server端和Client端DTO定义不一致
   - 缓解: Task 1.3和Task 1.4完成后立即进行接口对齐会议

### 质量关卡

每个Phase完成后需通过以下关卡：

**Phase 1关卡**:
- [ ] Migration执行成功，数据库字段正确
- [ ] Entity和DTO映射测试通过
- [ ] 编译通过: 0 errors, 0 warnings

**Phase 2关卡**:
- [ ] Service层单元测试覆盖率 ≥ 80%
- [ ] 所有API端点Postman测试通过
- [ ] BF-002验证逻辑正确
- [ ] 编译通过: 0 errors, 0 warnings

**Phase 3关卡**:
- [ ] UI功能测试通过（诊断 → 保存 → 处方解锁 → 药材录入 → 完成）
- [ ] 键盘导航测试通过（全程键盘录入10个药材）
- [ ] 拼音过滤测试通过（输入"dg"匹配到"当归"）
- [ ] 价格计算测试通过（实时计算正确）
- [ ] 编译通过: 0 errors, 0 warnings

**Phase 4关卡**:
- [ ] 单元测试覆盖率 ≥ 80%
- [ ] 性能指标达标（拼音过滤 < 100ms，UI渲染 ≥ 60fps）
- [ ] 文档同步完成
- [ ] Code Review通过（0 High Severity问题）
- [ ] 架构合规性检查通过

---

## 🧪 测试策略

### 单元测试

**Server端单元测试**:
- **Task 1.5**: AutoMapper映射测试
  - 验证所有Entity ↔ DTO映射正确
  - 验证嵌套对象映射
- **Task 1.6**: Repository单元测试
  - Mock DbContext
  - 测试Include预加载
  - 测试UpdateAsync方法
- **Task 2.7**: Service层单元测试
  - Mock Repository
  - 测试业务规则验证（BF-002, AR-003, BR-003等）
  - 测试价格计算逻辑
  - 测试异常场景

**Client端单元测试**:
- **Task 4.1**: 拼音过滤算法测试
  - 测试7级评分逻辑
  - 测试边界条件
  - 测试性能（< 100ms）
- **Task 4.2**: ViewModel单元测试
  - Mock API Client
  - 测试状态管理（CanEditPrescription计算）
  - 测试Command逻辑（SaveDraftCommand自动化流程）
  - 测试PropertyChanged事件
  - 测试价格计算（ItemAmount, TotalAmount）

### 集成测试

**Server端集成测试**:
- **Task 2.8**: Controller集成测试
  - 使用WebApplicationFactory
  - 使用InMemory Database或TestContainers
  - 测试完整BF-002流程（创建 → 辨证 → 标记 → 开处方 → 完成）
  - 测试错误场景（404, 422等）
  - 验证HTTP状态码和响应内容

**Client端集成测试**:
- **Task 3.11**: UI功能测试
  - 手动测试或使用UI自动化工具
  - 测试完整用户流程
  - 测试键盘导航
  - 测试数据绑定
  - 测试价格实时计算

### E2E测试

**完整流程测试**（Phase 4完成后）:
1. **患者选择 → 新建医案**
2. **填写诊断信息** → 保存草稿 → 验证Step1/Step2时间戳
3. **处方区解锁** → 添加药材（键盘操作）
4. **导入经验方** → 重复药材提醒 → 合并
5. **价格计算验证** → 保存处方
6. **保存并完成** → 验证BF-002验证
7. **查询医案详情** → 验证所有数据正确保存

### 性能测试

**性能指标**:
- 拼音过滤响应时间 < 100ms（1000个药材）
- UI渲染帧率 ≥ 60fps
- Repository查询性能（100个医案查询 < 500ms）
- 价格计算O(1)复杂度

**性能测试工具**:
- BenchmarkDotNet（Server端）
- Stopwatch（简单性能测试）
- WPF Performance Profiler（Client端UI）

---

## 💡 下一步操作

1. **审查task文档**
   - 检查任务拆分粒度是否合理
   - 检查依赖关系是否准确
   - 检查工作量估算是否合理

2. **调整任务粒度**（如果需要）
   - 如果某个任务 > 4小时，考虑拆分
   - 如果某个任务 < 1小时且独立性低，考虑合并

3. **批量生成Issues**
   - 使用`lybtzyzs-issue-template` skill
   - 模式: 批量模式（读取本task文档）
   - 自动创建Epic + 34个子Issues
   - 自动设置依赖关系和标签

4. **开始Phase 1开发**
   - 从Task 1.1开始
   - 遵循关键路径顺序
   - 利用并行任务加速开发

---

**文档版本**: v1.0
**生成日期**: 2025-11-20
**生成工具**: lybtzyzs-task-breakdown skill
**下次更新**: 根据实际执行情况调整工作量估算

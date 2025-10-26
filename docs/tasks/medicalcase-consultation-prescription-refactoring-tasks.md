# MedicalCase/Consultation/Prescription 重构任务分解文档

## 📋 元数据
- **Epic**: 待确定
- **设计文档**: docs/design/medicalcase-consultation-prescription-refactoring-design.md
- **需求文档**: docs/requirements/medicalcase-consultation-prescription-refactoring-requirements.md
- **总工作量**: 80小时（约10天）
- **实施阶段**: Phase 1-3
- **总任务数**: 28个

## 🎯 任务清单（Task Checklist）

### Phase 1: 基础架构和数据层（预计16小时 / 2天）

#### Task 1.1: 创建Migration脚本
- **工作量**: 2-3小时
- **依赖**: 无
- **类型**: Migration
- **文件范围**:
  - `src/Server/Infrastructure/LYBT.Infrastructure/Migrations/AddNeedsPrescriptionFlag.cs`
  - `src/Server/Infrastructure/LYBT.Infrastructure/Migrations/MakeConsultationFieldsNullable.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] Migration脚本可正常执行（dotnet ef database update）
  - [ ] 数据库结构符合设计（MedicalCases.NeedsPrescription字段已添加）
  - [ ] Consultation表字段Nullable设置正确
- **技术要点**:
  - SQL Server Migration语法
  - Nullable列处理（ALTER COLUMN ... NULL）
  - 默认值设置（DEFAULT 0 for NeedsPrescription）
  - Rollback脚本准备

#### Task 1.2: 更新Entity模型
- **工作量**: 1-2小时
- **依赖**: Task 1.1
- **类型**: Entity
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Entities/MedicalCase.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 新增NeedsPrescription属性（bool类型）
  - [ ] EF Core FluentAPI配置正确
  - [ ] 导航属性配置正确（Consultation、Prescriptions）
- **技术要点**:
  - EF Core FluentAPI配置
  - 导航属性设置（HasOne/HasMany）
  - 级联删除配置
  - 索引优化（如果需要）

#### Task 1.3: 创建请求/响应DTO
- **工作量**: 3-4小时
- **依赖**: Task 1.2
- **类型**: DTO
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/UpdateConsultationRequest.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/SetPrescriptionFlagRequest.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/CreatePrescriptionRequest.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/UpdatePrescriptionRequest.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/MedicalCaseDetailResponse.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/ConsultationDetailDto.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/PrescriptionDetailDto.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Dtos/PrescriptionItemDto.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] DTO字段完整（参考设计文档API端点设计章节）
  - [ ] Data Annotations正确（Required、MaxLength、Range）
  - [ ] 嵌套对象设计合理（避免循环引用）
- **技术要点**:
  - Data Annotations验证规则
  - 字段验证规则（Required、MaxLength）
  - 嵌套对象设计（ConsultationDetailDto包含四诊字段）
  - 循环引用避免（MedicalCaseDetailResponse不包含Patient完整信息）

#### Task 1.4: 配置AutoMapper映射关系
- **工作量**: 2-3小时
- **依赖**: Task 1.3
- **类型**: Configuration
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Mappings/MedicalCaseMappingProfile.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] Entity ↔ DTO映射测试通过
  - [ ] 嵌套对象映射正确（MedicalCase → MedicalCaseDetailResponse包含Consultation和Prescriptions）
  - [ ] AutoMapper配置验证通过（AssertConfigurationIsValid）
- **技术要点**:
  - AutoMapper Profile配置
  - 嵌套对象映射（ForMember、MapFrom）
  - 循环引用处理（Ignore、MaxDepth）
  - 映射测试（单元测试验证）

#### Task 1.5: 重构MedicalCaseRepository
- **工作量**: 4-5小时
- **依赖**: Task 1.2
- **类型**: Repository
- **文件范围**:
  - `src/Server/Infrastructure/LYBT.Infrastructure/Repositories/MedicalCaseRepository.cs`
  - `src/Server/Infrastructure/LYBT.Infrastructure/Repositories/IMedicalCaseRepository.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] GetByIdWithDetailsAsync方法实现（Include预加载Consultation和Prescriptions）
  - [ ] UpdateAsync方法实现（保存聚合根变更）
  - [ ] GetPagedListAsync方法实现（分页查询）
  - [ ] Repository单元测试通过（覆盖率≥70%）
- **技术要点**:
  - Include预加载（.Include(x => x.Consultation).Include(x => x.Prescriptions).ThenInclude(p => p.Items)）
  - IQueryable优化（避免N+1查询）
  - 异步方法实现（async/await）
  - 事务管理（SaveChangesAsync）

#### Task 1.6: 删除冗余DTO（代码清理）
- **工作量**: 1-2小时
- **依赖**: 无（可与其他任务并行）
- **类型**: Refactoring
- **文件范围**:
  - 删除：`ConsultationUpdateDto.cs`、`ConsultationCreateDto.cs`、`PrescriptionUpdateDto.cs`等
  - 更新：所有引用这些DTO的Service、Controller代码
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 无引用错误（使用Find All References验证）
  - [ ] Git历史记录清晰（单独commit）
- **技术要点**:
  - Visual Studio "Find All References"工具
  - 代码搜索（Ctrl+Shift+F）
  - Git commit策略（单独commit便于回滚）

---

### Phase 2: 业务逻辑和API实现（预计36小时 / 4.5天）

#### Task 2.1: 创建IMedicalCaseService接口
- **工作量**: 1-2小时
- **依赖**: Phase 1完成
- **类型**: Interface
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/IMedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 接口定义清晰（14个方法签名）
  - [ ] 方法命名符合规范（AsyncSuffix、动词+名词）
  - [ ] XML注释完整（描述业务规则和参数）
- **技术要点**:
  - 接口方法签名设计（参数、返回值、Task<>）
  - Write Layer方法（8个）：UpdateConsultationAsync、SetPrescriptionFlagAsync等
  - Read Layer方法（4个）：GetByIdAsync、GetListAsync等
  - Helper Layer方法（2个）：CanEditAsync、CanDeletePrescriptionAsync

#### Task 2.2a: 实现Service辨证相关方法（Write Layer）
- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] UpdateConsultationAsync方法实现（业务规则AR-001、BF-002）
  - [ ] SaveDraftAsync方法实现（暂存病案）
  - [ ] 业务规则验证正确（聚合根约束、三步流程）
  - [ ] 单元测试通过（Mock Repository）
- **技术要点**:
  - 业务规则验证（AR-001聚合根约束、BF-002三步流程）
  - 聚合根更新（通过MedicalCase.Consultation更新）
  - 状态管理（Draft/InProgress/Completed）
  - 异常处理（BusinessRuleException）

#### Task 2.2b: 实现Service处方相关方法（Write Layer）
- **工作量**: 3-4小时
- **依赖**: Task 2.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] CreatePrescriptionAsync方法实现（业务规则AR-001、AR-003）
  - [ ] UpdatePrescriptionAsync方法实现
  - [ ] DeletePrescriptionAsync方法实现
  - [ ] 业务规则验证正确（一诊断一处方）
  - [ ] 单元测试通过
- **技术要点**:
  - 业务规则验证（AR-001聚合根、AR-003一诊断一处方）
  - 聚合根处方管理（MedicalCase.Prescriptions集合操作）
  - 处方项Items关联（PrescriptionItems集合）
  - 事务管理（确保Prescription和Items一起保存）

#### Task 2.2c: 实现Service状态管理方法（Write Layer）
- **工作量**: 2-3小时
- **依赖**: Task 2.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] SetPrescriptionFlagAsync方法实现（更新NeedsPrescription标志）
  - [ ] CompleteCaseAsync方法实现（完成病案）
  - [ ] 状态转换逻辑正确（Draft → InProgress → Completed）
  - [ ] 单元测试通过
- **技术要点**:
  - 状态机逻辑（Draft/InProgress/Completed状态转换）
  - NeedsPrescription标志更新
  - 完成验证（Consultation必填、处方可选）
  - 状态转换约束（不允许非法转换）

#### Task 2.3: 实现Service查询方法（Read Layer）
- **工作量**: 2-3小时
- **依赖**: Task 2.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] GetByIdAsync方法实现（查询单个病案详情）
  - [ ] GetListAsync方法实现（分页查询病案列表）
  - [ ] AutoMapper映射正确（Entity → Response DTO）
  - [ ] 查询测试通过
- **技术要点**:
  - Repository查询调用（GetByIdWithDetailsAsync、GetPagedListAsync）
  - AutoMapper映射（Entity → MedicalCaseDetailResponse）
  - 分页参数处理（pageIndex、pageSize）
  - 查询优化（Include预加载）

#### Task 2.4: 实现Service辅助方法（Helper Layer）
- **工作量**: 1-2小时
- **依赖**: Task 2.1
- **类型**: Service
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseService.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] CanEditAsync方法实现（判断病案是否可编辑）
  - [ ] CanDeletePrescriptionAsync方法实现（判断处方是否可删除）
  - [ ] 辅助方法测试通过
- **技术要点**:
  - 业务规则判断（Completed状态不可编辑）
  - 权限验证（当前用户是否为病案创建者）
  - 返回bool或详细原因（ValidationResult模式）

#### Task 2.5: 创建Controller端点（Write/Read/Helper分层）
- **工作量**: 4-5小时
- **依赖**: Task 2.2a/b/c, Task 2.3, Task 2.4
- **类型**: Controller
- **文件范围**:
  - `src/Server/WebAPI/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 13个API端点实现（Write 8 + Read 5）
  - [ ] Swagger文档生成正确（ProducesResponseType注解）
  - [ ] API测试通过（Postman/Swagger）
  - [ ] 异步处理正确（async/await）
- **技术要点**:
  - ASP.NET Core Controller设计
  - RESTful API设计（PUT /medicalcases/{id}/consultation等）
  - Swagger注解（[HttpPut]、[ProducesResponseType]）
  - 异常处理（try-catch、返回统一错误格式）
  - 依赖注入（IMedicalCaseService）

#### Task 2.6: 标记废弃端点为Obsolete（ARCH-001 Phase 1）
- **工作量**: 1-2小时
- **依赖**: 无（可并行）
- **类型**: Refactoring
- **文件范围**:
  - `src/Server/WebAPI/LYBT.WebAPI/Controllers/ConsultationController.cs`
  - `src/Server/WebAPI/LYBT.WebAPI/Controllers/PrescriptionController.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings（有Obsolete警告）
  - [ ] 5个违规端点标记为[Obsolete]
  - [ ] Swagger注释更新（说明废弃原因和替代端点）
  - [ ] API文档更新（标注Deprecated）
- **技术要点**:
  - [Obsolete("message", error: false)] 注解
  - Swagger XML注释（<remarks>标签说明废弃）
  - Git commit信息清晰（标明ARCH-001）

#### Task 2.7: 配置依赖注入
- **工作量**: 1小时
- **依赖**: Task 2.5
- **类型**: Configuration
- **文件范围**:
  - `src/Server/WebAPI/LYBT.WebAPI/Program.cs` 或 `Startup.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] Service注册正确（services.AddScoped<IMedicalCaseService, MedicalCaseService>）
  - [ ] Repository注册正确
  - [ ] AutoMapper注册正确（services.AddAutoMapper）
  - [ ] 应用启动正常
- **技术要点**:
  - ASP.NET Core依赖注入（IServiceCollection）
  - 生命周期选择（Scoped/Transient/Singleton）
  - AutoMapper注册（AddAutoMapper(Assembly)）

#### Task 2.8: 实现错误处理和异常封装
- **工作量**: 2-3小时
- **依赖**: 无（可并行）
- **类型**: Infrastructure
- **文件范围**:
  - `src/Server/Modules/LYBT.Module.MedicalCase/Exceptions/BusinessRuleException.cs`
  - `src/Server/WebAPI/LYBT.WebAPI/Middlewares/GlobalExceptionHandlerMiddleware.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] BusinessRuleException定义清晰（RuleCode、Message）
  - [ ] 全局异常处理中间件实现
  - [ ] 错误响应格式统一（ErrorResponse DTO）
  - [ ] 异常处理测试通过
- **技术要点**:
  - 自定义异常设计（BusinessRuleException、NotFoundException）
  - 全局异常处理中间件（IExceptionHandler）
  - 统一错误响应格式（ErrorResponse DTO包含Code、Message、Details）
  - 日志记录（ILogger）

#### Task 2.9: 编写Service单元测试
- **工作量**: 6-8小时
- **依赖**: Task 2.2a/b/c, Task 2.3, Task 2.4
- **类型**: Test
- **文件范围**:
  - `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseServiceTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 覆盖14个Service方法
  - [ ] 边界条件测试（null参数、非法状态）
  - [ ] 业务规则验证测试（AR-001、BF-002、AR-003）
  - [ ] 测试覆盖率≥80%
- **技术要点**:
  - xUnit测试框架
  - NSubstitute Mock框架（Mock IMedicalCaseRepository）
  - AAA模式（Arrange-Act-Assert）
  - 边界条件测试（null、空集合、非法状态）

#### Task 2.10: 编写Controller集成测试
- **工作量**: 4-5小时
- **依赖**: Task 2.5
- **类型**: Test
- **文件范围**:
  - `tests/IntegrationTests/Server/WebAPI/LYBT.WebAPI.Tests/Controllers/MedicalCaseControllerTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 覆盖13个API端点
  - [ ] HTTP状态码验证（200/201/400/404）
  - [ ] 请求/响应JSON验证
  - [ ] 所有端点测试通过
- **技术要点**:
  - WebApplicationFactory集成测试
  - HttpClient调用API
  - JSON序列化/反序列化（System.Text.Json）
  - 测试数据库设置（InMemory或TestContainers）

---

### Phase 3: UI集成和端到端测试（预计28小时 / 3.5天）

#### Task 3.1: 更新WebAPI Client（迁移到新端点）
- **工作量**: 3-4小时
- **依赖**: Phase 2完成
- **类型**: ApiClient
- **文件范围**:
  - `src/Client/Desktop/Shared/LYBT.Desktop.Shared/ApiClients/MedicalCaseApiClient.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 调用新端点（PUT /medicalcases/{id}/consultation等）
  - [ ] 删除旧端点调用（POST /consultations/{id}/complete等）
  - [ ] HttpClient调用测试通过
  - [ ] API契约对齐（Request/Response DTO一致）
- **技术要点**:
  - HttpClient封装（PUT/POST/DELETE方法）
  - API契约对齐（与Server端DTO一致）
  - 异常处理（HttpRequestException、JsonException）
  - 认证Token传递（Bearer Token）

#### Task 3.2: 实现MedicalCaseConsultationViewModel
- **工作量**: 5-6小时
- **依赖**: Task 3.1
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseConsultationViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 辨证信息属性绑定（ChiefComplaint、PresentIllness、四诊字段）
  - [ ] NeedsPrescription属性双向绑定（RadioBox）
  - [ ] Command实现（SaveCommand、SaveDraftCommand、CompleteCommand）
  - [ ] ShowPrescriptionPanel计算属性（基于NeedsPrescription）
  - [ ] ViewModel单元测试通过
- **技术要点**:
  - Prism BindableBase（PropertyChanged通知）
  - DelegateCommand实现（SaveCommand、SaveDraftCommand）
  - 双向绑定（NeedsPrescription属性setter触发API调用）
  - 计算属性（ShowPrescriptionPanel基于NeedsPrescription）
  - 异步Command（async/await）

#### Task 3.3: 更新MedicalCaseView.xaml（RadioBox控件）
- **工作量**: 2-3小时
- **依赖**: Task 3.2
- **类型**: View
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseView.xaml`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Views/MedicalCaseView.xaml.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] RadioBox控件显示正确（"是"/"否"选项）
  - [ ] 处方输入面板Visibility绑定正确（ShowPrescriptionPanel）
  - [ ] 数据绑定工作正常（辨证字段、RadioBox、处方字段）
  - [ ] UI布局合理（辨证→决策点→施治流程清晰）
- **技术要点**:
  - WPF RadioButton绑定（IsChecked绑定到NeedsPrescription）
  - Visibility绑定（BooleanToVisibilityConverter）
  - 动态面板显示（处方输入区域根据RadioBox显示/隐藏）
  - 布局设计（StackPanel/Grid）

#### Task 3.4: 实现RadioBox变化时的自动保存逻辑
- **工作量**: 2小时
- **依赖**: Task 3.2, Task 3.3
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseConsultationViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] NeedsPrescription属性setter自动调用SetPrescriptionFlagAsync
  - [ ] 防抖处理（避免频繁API调用）
  - [ ] 错误处理（API调用失败时回滚RadioBox状态）
  - [ ] 功能测试通过（RadioBox变化自动保存）
- **技术要点**:
  - PropertySetter触发API调用（NeedsPrescription.set → SetPrescriptionFlagAsync）
  - 防抖处理（Task.Delay或Throttle）
  - 错误处理（API失败时恢复原值）
  - 用户体验优化（加载指示器）

#### Task 3.5: 实现暂存病案功能
- **工作量**: 2-3小时
- **依赖**: Task 3.2
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseConsultationViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] SaveDraftCommand实现（调用SaveDraftAsync API）
  - [ ] 暂存后状态正确（MedicalCase.Status = Draft）
  - [ ] 数据保存完整（辨证信息、RadioBox状态、处方信息）
  - [ ] 功能测试通过（暂存→关闭→重新打开，数据正确恢复）
- **技术要点**:
  - SaveDraftCommand实现（调用PUT /medicalcases/{id}/status API）
  - 状态管理（Draft状态保存）
  - 数据完整性验证（所有字段保存）
  - 用户提示（"暂存成功"消息）

#### Task 3.6: 实现继续看诊功能
- **工作量**: 3-4小时
- **依赖**: Task 3.2
- **类型**: ViewModel
- **文件范围**:
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseConsultationViewModel.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] LoadAsync方法恢复所有数据（辨证、处方、RadioBox状态）
  - [ ] NeedsPrescription属性正确恢复（RadioBox状态）
  - [ ] 处方面板正确显示（基于NeedsPrescription）
  - [ ] 功能测试通过（加载暂存病案后所有字段正确）
- **技术要点**:
  - LoadAsync方法实现（调用GET /medicalcases/{id} API）
  - 数据映射（Response → ViewModel属性）
  - RadioBox状态恢复（NeedsPrescription赋值）
  - UI状态同步（ShowPrescriptionPanel更新）

#### Task 3.7: 物理删除废弃端点（ARCH-001 Phase 3）
- **工作量**: 1-2小时
- **依赖**: Task 3.1
- **类型**: Refactoring
- **文件范围**:
  - 删除：5个Controller方法、相关Service方法、冗余DTO
  - `src/Server/WebAPI/LYBT.WebAPI/Controllers/ConsultationController.cs`
  - `src/Server/WebAPI/LYBT.WebAPI/Controllers/PrescriptionController.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 5个废弃端点物理删除
  - [ ] API文档不再显示废弃端点
  - [ ] Client端无任何调用废弃端点
  - [ ] Git历史记录清晰
- **技术要点**:
  - 物理删除代码（Controller方法、Service方法、DTO）
  - 验证无引用（Find All References）
  - Git commit策略（标明ARCH-001 Phase 3）
  - 回归测试（确保现有功能正常）

#### Task 3.8: 端到端功能测试
- **工作量**: 4-5小时
- **依赖**: Task 3.3, Task 3.4, Task 3.5, Task 3.6
- **类型**: Test
- **文件范围**:
  - `tests/E2ETests/LYBT.E2ETests/MedicalCase/MedicalCaseFlowTests.cs`
- **验收标准**:
  - [ ] 编译通过：0 errors, 0 warnings
  - [ ] 场景1测试通过：辨证 → RadioBox选择"是" → 开处方 → 完成
  - [ ] 场景2测试通过：辨证 → RadioBox选择"否" → 完成
  - [ ] 场景3测试通过：辨证 → 暂存 → 继续看诊 → 完成
  - [ ] 场景4测试通过：辨证 → 开处方 → 删除处方 → 重新开处方
  - [ ] 数据库状态验证通过
  - [ ] UI状态验证通过
  - [ ] 错误场景覆盖（非法操作、边界条件）
- **技术要点**:
  - E2E测试框架（Selenium或Playwright）
  - 数据库状态验证（直接查询数据库）
  - UI状态验证（RadioBox状态、面板显示）
  - 错误场景测试（API失败、网络超时）
  - 测试数据准备（Fixture）

#### Task 3.9: 更新用户文档
- **工作量**: 2-3小时
- **依赖**: 无（可随时进行）
- **类型**: Documentation
- **文件范围**:
  - `docs/user-manual/medicalcase-flow.md`
  - `docs/api/medicalcase-api.md`
  - `docs/architecture/decisions/ADR-XXX-medicalcase-refactoring.md`
- **验收标准**:
  - [ ] 用户手册更新（动态流程说明、RadioBox使用、暂存和继续看诊）
  - [ ] API文档更新（新端点文档、废弃端点标注）
  - [ ] ADR文档创建（架构决策记录）
  - [ ] 文档清晰易懂（截图、流程图）
- **技术要点**:
  - 用户手册编写（Markdown格式）
  - 流程图绘制（Mermaid或diagrams.net）
  - API文档生成（Swagger导出）
  - ADR文档格式（Context、Decision、Consequences）

---

## 📊 任务统计

| 维度 | 数值 |
|------|------|
| **总任务数** | 28个 |
| **总工作量** | 80小时（约10天） |
| **Phase数量** | 3个 |
| **关键路径长度** | 13个任务 |
| **并行任务组** | 6组 |

### Phase统计

| Phase | 任务数 | 工作量 | 关键任务数 |
|-------|-------|--------|-----------|
| **Phase 1** | 6个 | 16小时（2天） | 5个 |
| **Phase 2** | 12个 | 36小时（4.5天） | 5个 |
| **Phase 3** | 9个 | 28小时（3.5天） | 3个 |

### 任务类型分布

| 类型 | 数量 | 占比 |
|------|------|------|
| **Service** | 6个 | 21% |
| **ViewModel** | 4个 | 14% |
| **Test** | 3个 | 11% |
| **DTO** | 2个 | 7% |
| **Repository** | 1个 | 4% |
| **Controller** | 1个 | 4% |
| **其他** | 11个 | 39% |

---

## 🔗 依赖关系图

### Phase 1依赖
```
Task 1.1 (无依赖)
  ├─> Task 1.2
  │     ├─> Task 1.3 → Task 1.4
  │     └─> Task 1.5
  └─> Task 1.6 (独立，可并行)
```

### Phase 2依赖
```
Phase 1完成 → Task 2.1
  ├─> Task 2.2a (辨证方法)
  ├─> Task 2.2b (处方方法) } 可并行
  ├─> Task 2.2c (状态方法)
  ├─> Task 2.3 (Read层) } 可与2.2并行
  ├─> Task 2.4 (Helper层)
  └─> 所有Service完成 → Task 2.5 (Controller)
        ├─> Task 2.7 (依赖注入)
        ├─> Task 2.9 (Service测试)
        └─> Task 2.10 (Controller测试)

Task 2.6 (标记废弃) - 独立，可并行
Task 2.8 (错误处理) - 独立，可并行
```

### Phase 3依赖
```
Phase 2完成 → Task 3.1 (WebAPI Client)
  └─> Task 3.2 (ViewModel)
        └─> Task 3.3 (View)
              ├─> Task 3.4 (RadioBox自动保存)
              ├─> Task 3.5 (暂存功能) } 可并行
              └─> Task 3.6 (继续看诊)
                    └─> Task 3.8 (E2E测试)

Task 3.1完成 → Task 3.7 (删除废弃端点)
Task 3.9 (文档) - 独立，可随时进行
```

### 跨Phase依赖
```
Phase 1完成 → Phase 2开始
Phase 2完成 → Phase 3开始

关键依赖链：
- Task 1.2 → Task 2.1 (Entity先于Service接口)
- Task 2.5 → Task 3.1 (API端点先于WebAPI Client)
- Task 3.1 → Task 3.7 (Client迁移先于删除废弃端点)
```

---

## ⚠️ 关键路径

### 主线任务（必须按顺序完成）

**总计：13个任务，预计52小时（约6.5天）**

1. **Task 1.1**: 创建Migration脚本（2.5小时）
2. **Task 1.2**: 更新Entity模型（1.5小时）
3. **Task 1.3**: 创建请求/响应DTO（3.5小时）
4. **Task 1.4**: 配置AutoMapper映射（2.5小时）
5. **Task 1.5**: 重构MedicalCaseRepository（4.5小时）
6. **Task 2.1**: 创建IMedicalCaseService接口（1.5小时）
7. **Task 2.2a**: 实现Service辨证方法（3.5小时）
8. **Task 2.5**: 创建Controller端点（4.5小时）
9. **Task 2.10**: 编写Controller集成测试（4.5小时）
10. **Task 3.1**: 更新WebAPI Client（3.5小时）
11. **Task 3.2**: 实现ViewModel（5.5小时）
12. **Task 3.3**: 更新View.xaml（2.5小时）
13. **Task 3.8**: 端到端功能测试（4.5小时）

### 并行任务组（可同时进行）

#### 组1：Phase 2 Service方法（可3人并行）
- Task 2.2a: 辨证方法（3.5小时）
- Task 2.2b: 处方方法（3.5小时）
- Task 2.2c: 状态方法（2.5小时）

#### 组2：Phase 2 Service层（可2人并行）
- Task 2.3: Read层（2.5小时）
- Task 2.4: Helper层（1.5小时）

#### 组3：Phase 2 基础设施（可随时进行）
- Task 2.6: 标记废弃端点（1.5小时）
- Task 2.8: 错误处理（2.5小时）

#### 组4：Phase 3 ViewModel功能（可2人并行）
- Task 3.4: RadioBox自动保存（2小时）
- Task 3.5: 暂存功能（2.5小时）
- Task 3.6: 继续看诊（3.5小时）

#### 组5：清理任务（可随时进行）
- Task 1.6: 删除冗余DTO（1.5小时）
- Task 3.7: 删除废弃端点（1.5小时）

#### 组6：文档任务（可随时进行）
- Task 3.9: 更新用户文档（2.5小时）

---

## 📝 实施建议

### 优先级排序

#### 🔴 高优先级（关键路径任务 - 52小时）
1. Task 1.1-1.5: Phase 1核心任务（14.5小时）
2. Task 2.1, 2.2a, 2.5, 2.10: Phase 2核心任务（14小时）
3. Task 3.1-3.3, 3.8: Phase 3核心任务（16小时）

#### 🟡 中优先级（并行任务 - 20小时）
1. Task 2.2b, 2.2c: 处方和状态方法（6小时）
2. Task 2.3, 2.4: Read和Helper层（4小时）
3. Task 2.9: Service单元测试（7小时）
4. Task 3.4-3.6: ViewModel功能（8小时）

#### 🟢 低优先级（清理和文档 - 8小时）
1. Task 1.6, 2.6, 3.7: 代码清理任务（4.5小时）
2. Task 2.7, 2.8: 配置和基础设施（3.5小时）
3. Task 3.9: 文档更新（2.5小时）

### 并行策略

#### 单人开发（顺序执行）
- **推荐顺序**：严格按Phase 1 → 2 → 3顺序执行
- **总时长**：80小时（约10天）
- **优势**：依赖清晰，风险低
- **劣势**：时间较长

#### 2人并行开发（推荐）
- **Person A**：负责关键路径（Phase 1-2-3核心任务）
- **Person B**：负责并行任务（Service方法、测试、文档）
- **协作点**：
  - Phase 1完成后，B开始Task 2.2b/c并行开发
  - Phase 2完成后，B开始Task 3.4-3.6并行开发
- **总时长**：约7天（节省30%时间）

#### 3人并行开发（最快）
- **Person A**：Phase 1全部 + Phase 2关键路径（Task 2.1, 2.2a, 2.5）
- **Person B**：Phase 2并行任务（Task 2.2b/c, 2.3, 2.4, 2.9, 2.10）
- **Person C**：Phase 3全部（Task 3.1-3.9）
- **协作点**：
  - Phase 1完成后，A和B并行开发Phase 2
  - Task 2.5完成后，C开始Phase 3开发
- **总时长**：约5-6天（节省50%时间）

### 风险提示

#### 高风险任务（需要重点关注）

1. **Task 1.5: Repository重构（4.5小时）**
   - **风险**：可能影响现有功能，导致其他模块调用失败
   - **缓解措施**：
     - 充分的单元测试（覆盖率≥70%）
     - 回归测试（测试现有功能）
     - 保留原有方法（标记Obsolete），渐进式迁移

2. **Task 2.2: Service层业务规则（9小时）**
   - **风险**：业务规则复杂，容易遗漏验证逻辑
   - **缓解措施**：
     - 详细的业务规则文档（AR-001、BF-002、AR-003）
     - 边界条件测试（null、非法状态、并发冲突）
     - Code Review（重点检查业务规则验证）

3. **Task 3.2: ViewModel实现（5.5小时）**
   - **风险**：RadioBox双向绑定+自动保存逻辑复杂，容易出现死循环或性能问题
   - **缓解措施**：
     - 防抖处理（避免频繁API调用）
     - 单元测试（Mock API调用）
     - 性能测试（验证无内存泄漏）

4. **Task 3.8: E2E测试（4.5小时）**
   - **风险**：E2E测试依赖完整环境，容易受网络、数据库、环境配置影响
   - **缓解措施**：
     - 测试环境隔离（独立数据库、Mock外部服务）
     - 测试数据准备（Fixture、Seed Data）
     - 失败重试机制（处理网络抖动）

#### 中风险任务（需要注意）

1. **Task 2.5: Controller端点（4.5小时）**
   - **风险**：13个端点，容易遗漏异常处理或Swagger注解
   - **缓解措施**：使用Checklist逐一验证

2. **Task 3.1: WebAPI Client迁移（3.5小时）**
   - **风险**：API契约不一致，导致运行时错误
   - **缓解措施**：集成测试验证API调用

3. **Task 3.4: RadioBox自动保存（2小时）**
   - **风险**：用户体验问题（频繁保存、加载指示器缺失）
   - **缓解措施**：防抖处理、加载指示器

### 测试策略

#### 测试金字塔

```
        E2E测试（4.5小时）
           /\
          /  \
         /集成\  (9小时)
        /测试  \
       /________\
      /          \
     / 单元测试   \ (9小时)
    /____________\
```

#### 单元测试（16-18小时，覆盖率目标≥70%）

1. **Repository单元测试**（Task 1.5包含）
   - Mock DbContext（EF Core InMemory）
   - 测试Include预加载、分页查询
   - 覆盖率目标：≥70%

2. **Service单元测试**（Task 2.9）
   - Mock IMedicalCaseRepository（NSubstitute）
   - AAA模式（Arrange-Act-Assert）
   - 边界条件测试（null参数、非法状态）
   - 业务规则验证测试（AR-001、BF-002、AR-003）
   - 覆盖率目标：≥80%

3. **ViewModel单元测试**（Task 3.2包含）
   - Mock ApiClient（NSubstitute）
   - PropertyChanged通知测试
   - Command CanExecute测试
   - 覆盖率目标：≥70%

#### 集成测试（9小时）

1. **Controller集成测试**（Task 2.10）
   - WebApplicationFactory集成测试
   - 测试数据库设置（InMemory或TestContainers）
   - HTTP状态码验证（200/201/400/404）
   - 请求/响应JSON验证
   - 覆盖13个API端点

2. **数据库集成测试**（包含在Task 1.5和2.10中）
   - EF Core Migration测试
   - Repository真实数据库测试（可选）

#### E2E测试（4.5小时）

1. **端到端功能测试**（Task 3.8）
   - 场景1：辨证 → RadioBox选择"是" → 开处方 → 完成
   - 场景2：辨证 → RadioBox选择"否" → 完成
   - 场景3：辨证 → 暂存 → 继续看诊 → 完成
   - 场景4：辨证 → 开处方 → 删除处方 → 重新开处方
   - 数据库状态验证
   - UI状态验证
   - 错误场景覆盖

#### 测试工具和框架

- **单元测试**：xUnit + NSubstitute + FluentAssertions
- **集成测试**：WebApplicationFactory + EF Core InMemory
- **E2E测试**：Selenium WebDriver 或 Playwright
- **测试覆盖率**：Coverlet + ReportGenerator

---

## 🧪 测试覆盖率目标

| 层级 | 覆盖率目标 | 测试工时 |
|------|-----------|---------|
| **Repository** | ≥70% | 包含在Task 1.5 |
| **Service** | ≥80% | 7小时 |
| **Controller** | ≥70% | 4.5小时 |
| **ViewModel** | ≥70% | 包含在Task 3.2 |
| **E2E** | 核心场景100% | 4.5小时 |

---

## 💡 下一步操作

1. **审查Task文档**：
   - 确认任务粒度合理（1-6小时/任务）
   - 确认依赖关系准确
   - 确认工作量估算（80小时 ≈ 10天）

2. **调整任务粒度**（如果需要）：
   - 如果某任务超过6小时，考虑拆分
   - 如果某任务小于1小时，考虑合并

3. **创建Epic和Issues**（下一阶段）：
   - 使用 `lybtzyzs-issue-template` 批量生成GitHub Issues
   - 关联到Epic（待用户确认Epic编号）
   - 标注依赖关系（GitHub Issue Dependencies）

4. **分配开发人员**（根据并行策略）：
   - 单人开发：按Phase顺序执行
   - 2-3人并行：按并行策略分配任务

5. **开始实施**：
   - 严格按照验收标准执行
   - 每完成一个Task立即创建Commit
   - Phase完成后进行阶段性验收

---

## 📚 相关文档

- **需求文档**: docs/requirements/medicalcase-consultation-prescription-refactoring-requirements.md
- **设计文档**: docs/design/medicalcase-consultation-prescription-refactoring-design.md
- **架构文档**: docs/architecture/server/README.md
- **API文档**: docs/api/medicalcase-api.md
- **业务规则**: docs/business-rules.md
- **开发规范**: docs/development/shared/code-standards.md

---

**生成时间**: 2025-10-26
**生成工具**: lybtzyzs-task-breakdown (v1.0)
**维护者**: Claude Code

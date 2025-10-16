# 模块开发检查清单

> 使用此清单确保新模块或重构模块符合统一设计标准

---

## Phase 1: 目录结构检查

### 必需目录
- [ ] `Models/` - UI专用模型目录已创建
- [ ] `ViewModels/` - 视图模型目录已创建
- [ ] `Views/` - XAML视图目录已创建
- [ ] `{ModuleName}Module.cs` - Prism模块注册类已创建
- [ ] `README.md` - 模块说明文档已创建

### 禁止目录（确保不存在）
- [ ] 无 `Interfaces/` 目录（接口在 Shared 层）
- [ ] 无 `Mappings/` 目录（AutoMapper配置在 Desktop.Services/Mapping/）
- [ ] 无 `Services/` 目录（业务服务在 Desktop.Services/Business/）

---

## Phase 2: ViewModel 检查

### 2.1 基类选择
- [ ] 列表管理 ViewModel 继承 `UnifiedListViewModelBase<TDto>`
- [ ] 详情/单项 ViewModel 继承 `UnifiedViewModelBase`
- [ ] 对话框 ViewModel 继承 `UnifiedViewModelBase`

### 2.2 构造函数
- [ ] 依赖注入顺序符合标准：
  1. 业务服务（如 `IPatientService`）
  2. 基类必需依赖（`IEventAggregator`, `ILoggerFactory`, `IRegionManager`）
  3. 可选依赖（末尾，使用 `= null`）
- [ ] 所有必需参数使用 `?? throw new ArgumentNullException`
- [ ] 调用 `base(...)` 传递基类依赖

### 2.3 命令命名
- [ ] CRUD 命令：`AddCommand`, `EditCommand`, `DeleteCommand`, `SaveCommand`
- [ ] 导航命令：`BackCommand`, `NextCommand`, `GotoXxxCommand`
- [ ] 搜索命令：`SearchCommand`, `ClearSearchCommand`
- [ ] 自定义命令：`{Verb}{Noun}Command`

### 2.4 属性命名
- [ ] 数据集合：`Items`
- [ ] 当前选中：`SelectedItem` / `CurrentItem`
- [ ] 状态标志：`IsLoading`, `IsBusy`, `IsReadOnly`
- [ ] UI文本：`PageTitle`, `StatusText`, `ErrorMessage`

### 2.5 异步处理
- [ ] 所有异步方法使用 `async`/`await`
- [ ] 使用 `try-catch` 捕获异常
- [ ] 使用基类的 `ShowErrorMessageAsync` 等方法显示消息

### 2.6 导航处理
- [ ] 重写 `OnNavigatedTo` 时调用 `base.OnNavigatedTo(navigationContext)`
- [ ] 使用 `NavigationParameters` 传递参数

---

## Phase 3: Service 层检查

### 3.1 Service 实现
- [ ] 位置在 `Desktop.Services/Business/{Entity}Service.cs`
- [ ] 实现 `Shared.Interfaces.Services.I{Entity}Service`
- [ ] 命名为 `{Entity}Service`

### 3.2 构造函数
- [ ] 依赖注入顺序符合标准：
  1. `I{Entity}Repository repository`
  2. `ILogger<{Entity}Service> logger`
  3. `IExceptionHandler exceptionHandler`
  4. `IMapper mapper`
- [ ] 所有依赖使用 `?? throw new ArgumentNullException`

### 3.3 方法实现
- [ ] 所有方法返回 `ServiceResult<T>` 或 `ServiceResult`
- [ ] 使用 `_exceptionHandler.SafeExecuteAsync` 包装
- [ ] 使用 `_logger.LogInformation` 记录关键操作
- [ ] **强制使用 `_mapper.Map<T>()` 进行 DTO 转换（不再手动映射）**

### 3.4 AutoMapper 配置
- [ ] 创建 `Desktop.Services/Mapping/{Entity}MappingProfile.cs`
- [ ] 继承 `AutoMapper.Profile`
- [ ] 配置所有必需的映射：
  - Entity → Dto
  - CreateDto → Entity
  - UpdateDto → Entity

---

## Phase 4: View 层检查

### 4.1 XAML 结构
- [ ] 使用 `prism:ViewModelLocator.AutoWireViewModel="True"`
- [ ] 三段式布局：标题栏 + 内容区 + 加载遮罩
- [ ] 标题栏使用 `{StaticResource TitleBarStyle}`
- [ ] 内容区包裹在 `ScrollViewer` 中
- [ ] 加载遮罩使用统一模式（`IsLoading` + Converter）

### 4.2 数据绑定
- [ ] 命令绑定：`Command="{Binding XxxCommand}"`
- [ ] 双向绑定：指定 `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`
- [ ] 只读绑定：指定 `Mode=OneWay`
- [ ] 可见性绑定：使用 Converter（如 `BooleanToVisibilityConverter`）

### 4.3 样式和资源
- [ ] 样式使用 `{StaticResource XxxStyle}`
- [ ] 主题资源使用 `{DynamicResource XxxBrush}`
- [ ] Converter 已在 `Desktop.Infrastructure/Converters/` 定义
- [ ] 无内联样式（除非有注释说明特殊原因）

### 4.4 代码后置
- [ ] 代码后置仅包含 `InitializeComponent()`
- [ ] 无业务逻辑
- [ ] 无 ViewModel 访问

---

## Phase 5: 命名约定检查

### 5.1 文件命名
- [ ] ViewModel: `{Entity}{ViewType}ViewModel.cs`
- [ ] View (XAML): `{Entity}{ViewType}View.xaml`
- [ ] Model: `{Entity}{Suffix}.cs` (如 `PatientItem.cs`)
- [ ] Service: `{Entity}Service.cs`
- [ ] Repository: `{Entity}Repository.cs`

### 5.2 ViewType 后缀
- [ ] 列表管理：`Management` (如 `PatientManagementViewModel`)
- [ ] 详情查看：`Detail` (如 `PatientDetailViewModel`)
- [ ] 创建表单：`Create` (如 `PatientCreateViewModel`)
- [ ] 编辑表单：`Edit` (如 `PatientEditViewModel`)
- [ ] 对话框：`Dialog` (如 `ConfirmDialogViewModel`)

---

## Phase 6: 注册与配置检查

### 6.1 Prism 模块注册
- [ ] `{ModuleName}Module.cs` 实现 `IModule`
- [ ] 在 `RegisterTypes` 中注册 ViewModel
- [ ] 在 `OnInitialized` 中注册导航 View

### 6.2 依赖注入注册
- [ ] Service 已在 `Desktop.Services/ServiceRegistration.cs` 注册
- [ ] Repository 已在 `Desktop.Services/ServiceRegistration.cs` 注册
- [ ] AutoMapper Profile 已注册（`services.AddAutoMapper(...)`）

### 6.3 项目引用
- [ ] 模块项目引用 `LYBT.Desktop.Infrastructure`
- [ ] 模块项目引用 `LYBT.Desktop.Models`
- [ ] 模块项目引用 `LYBT.Desktop.Services`
- [ ] 模块项目引用 `LYBT.Shared.Models`
- [ ] 模块项目引用 `LYBT.Shared.Interfaces`
- [ ] 模块项目引用 `Prism.Wpf`

---

## Phase 7: 编译与测试检查

### 7.1 编译验证
- [ ] `dotnet build` 通过（0 errors, 0 warnings）
- [ ] 无未使用的 using 语句
- [ ] 无编译器警告

### 7.2 功能测试
- [ ] 列表管理功能正常（加载、搜索、分页）
- [ ] 详情查看功能正常（导航、数据显示）
- [ ] CRUD 操作正常（创建、更新、删除）
- [ ] 错误处理正常（显示错误消息）
- [ ] 加载状态正常（IsLoading 遮罩）

### 7.3 性能测试
- [ ] 列表加载速度 < 2秒
- [ ] 详情加载速度 < 1秒
- [ ] 无明显卡顿

---

## Phase 8: 文档与注释检查

### 8.1 代码注释
- [ ] 所有 public 类有 XML 文档注释（`/// <summary>`）
- [ ] 所有 public 方法有 XML 文档注释
- [ ] 复杂业务逻辑有内联注释说明

### 8.2 模块文档
- [ ] `README.md` 包含模块功能说明
- [ ] `README.md` 包含主要功能列表
- [ ] `README.md` 包含依赖说明
- [ ] `README.md` 包含使用示例（如需要）

---

## Phase 9: 最终审核

### 9.1 设计标准符合性
- [ ] 完全符合 `unified-design-standard.md` 所有规范
- [ ] 无标准之外的自定义模式（除非有充分理由并记录）

### 9.2 代码质量
- [ ] 无重复代码
- [ ] 无魔法数字/字符串（使用常量）
- [ ] 符合 SOLID 原则
- [ ] 符合 DRY 原则

### 9.3 可维护性
- [ ] 代码结构清晰
- [ ] 命名语义化
- [ ] 易于理解和修改

---

## 检查清单签署

- **模块名称**: ____________________
- **开发人员**: ____________________
- **审核人员**: ____________________
- **检查日期**: ____________________
- **审核结果**: □ 通过  □ 需改进

**备注**:
```
（记录需要改进的项目或特殊说明）

```

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

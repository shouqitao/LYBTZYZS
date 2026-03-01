# LYBT.Desktop.Herbs 模块说明

## 代码文件结构

### HerbsModule.cs
- `HerbsModule : IModule` -- Prism模块入口，依赖AuthenticationModule
  - `OnInitialized()` -- 空实现
  - `RegisterTypes()` -- 注册IHerbRepository(Singleton)、IHerbSearchProvider、MasterDetail服务、HerbMasterDetailViewModel

### Controls/

- `HerbEditControl.xaml.cs` -- 药材编辑UserControl，DependencyProperty: HerbName/PinYinCode/Origin/Spec/Unit/Price/CostPrice/Status/StatusOptions/HerbEffect/Usage/Remark/IsNameEditable/ShowStatus/ErrorsSource
- `HerbMasterDetailControl.xaml.cs` -- `HerbMasterDetailControl : MasterDetailControlBase`，InitializeViewModel<HerbMasterDetailViewModel>()，供Admin和Clinical角色台复用
- `HerbViewControl.xaml.cs` -- 药材只读预览UserControl，DependencyProperty: HerbName/PinYinCode/Category/Properties/Origin/Spec/Unit/Price/CostPrice/HerbEffect/Usage/Remark/Status/ShowStatus/CreatedAt/UpdatedAt

### Interfaces/

- `IHerbRepository.cs` -- 药材仓储接口(RESTful设计)
  - CRUD: GetPagedAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync/SearchAsync
  - 导入导出: BatchImportAsync(Stream)/ExportTemplateAsync/ExportHerbsAsync
  - 状态/批量: ToggleStatusAsync/RestoreAsync/BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
  - 包装方法: CreateWithResultAsync/UpdateWithResultAsync/DeleteWithResultAsync/GetByIdWithResultAsync -- 统一返回元组(success, data, error)

### Mappers/

- `HerbMapper.cs` -- Mapperly编译时映射器 [Mapper]，映射HerbDetailDto<->HerbDetailModel，HerbDetailModel->HerbInputDto
  - `ToItem()` -- DTO->Model (忽略CreatedBy)
  - `ToDto()` -- Model->DTO
  - `ToInputDtoCore()` / `ToInputDto()` -- Model->InputDto (Id空Guid转null)

### Models/

- `HerbDetailModel.cs` -- `HerbDetailModel : ValidatableModelBase`，Master-Detail Detail区域编辑模型
  - 属性: Id/IsNew(计算)/Name([Required], 自动生成PinYinCode)/PinYinCode/Category/Properties/Origin/Spec/Unit([Required])/Price([Required][Range])/CostPrice([Range])/Effect([StringLength])/Usage([StringLength])/Remark([StringLength])/Status/CreatedAt/UpdatedAt
  - 方法: `CreateNew()` (静态工厂，默认Unit="克") / `Clone()` (直接赋值_name/_pinYinCode避免触发自动生成)

### Repositories/

- `HerbRepository.cs` -- `HerbRepository : IHerbRepository`，DataSource抽象层，支持Local/Remote模式
  - 依赖: IHerbDataSource + IHerbApi?(可选，仅Remote批量/导入导出)
  - CRUD: GetPagedAsync/GetByIdAsync/CreateAsync/UpdateAsync/DeleteAsync/SearchAsync
  - 导入导出: BatchImportAsync(Refit.StreamPart)/ExportTemplateAsync/ExportHerbsAsync -- 仅Remote模式支持
  - 状态/批量: ToggleStatusAsync/RestoreAsync/BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
  - 包装方法: CreateWithResultAsync/UpdateWithResultAsync/DeleteWithResultAsync/GetByIdWithResultAsync -- 使用ClientErrorMessageMapper包装错误
  - 本地模式: BatchDelete逐个执行，BatchImport/ExportTemplate/ExportHerbs/BatchEnable/BatchDisable返回null

### Services/

- `HerbSearchProvider.cs` -- `HerbSearchProvider : IHerbSearchProvider`，跨模块搜索提供者(D5-3)，委托IHerbRepository
  - `SearchHerbsAsync()` -- 关键词搜索
  - `GetAllHerbsAsync()` -- 分页加载全量数据(pageSize=100循环)

### ViewModels/

- `HerbMasterDetailViewModel.cs` -- `HerbMasterDetailViewModel : MasterDetailViewModelBase<HerbListDto, HerbDetailModel>` [partial]
  - 依赖: IHerbRepository/IDialogService
  - 属性: IsNameEditable(计算, 仅新建可编辑)/IsAdmin/StatusOptions/DetailTitle(计算)
  - 基类实现: LoadListAsync()/LoadDetailAsync()/CreateNewDetail()/SaveDetailAsync(含Name/Unit/Price/CostPrice验证)/DeleteItemAsync()
  - 命令: ToggleStatusCommand/CopyHerbCommand(Clone+重命名)/RestoreCommand/ImportHerbsCommand(Excel导入)/ExportHerbsCommand(Excel导出)/SearchByCategoryCommand

---

## 死代码与废弃标记

| 类型 | 名称 | 状态 | 说明 |
|------|------|------|------|
| 全部文件 | -- | **活跃** | 无明显死代码，所有类型均有外部引用 |

### 已删除的类型 (历史记录)

- `HerbService` -- OpenSpec: simplify-desktop-data-layer，功能合并到HerbRepository
- `HerbDetailView` / `HerbDetailViewModel` -- OpenSpec: migrate-views-to-role-modules，已删除
- `HerbCreateViewModel` -- Issue #2168 CRUD统一架构，已删除
- `MappingService` -- OpenSpec: standardize-api-architecture，已删除

### OpenSpec标记

- `simplify-desktop-data-layer` -- HerbService已删除，功能合并到Repository
- `migrate-views-to-role-modules` -- HerbDetailView/HerbDetailViewModel已删除
- `refactor-viewmodel-composition` -- V2组合模式ViewModel
- `refactor-admin-workspace` -- Control模式重构
- `implement-local-mode` -- DataSource抽象层Local/Remote切换
- `extract-detail-controls` -- HerbEditControl/HerbViewControl提取
- `refactor-master-detail-layout` -- 详情区域UI优化
- `refactor-frontend-srp-patterns` -- MasterDetailControlBase基类
- `adopt-mapperly-unified-mapping` -- Mapperly映射器
- `enhance-viewmodel-architecture` -- IViewModelServices聚合服务
- `ui-validation-framework` -- 验证错误显示

---

## 设计分析

### 架构模式
- **Master-Detail组合模式**: HerbMasterDetailViewModel继承MasterDetailViewModelBase，使用IMasterDetailServices聚合Loading/Pagination/Dialog/ErrorHandler/DetailEditor服务
- **Control复用模式**: HerbMasterDetailControl继承MasterDetailControlBase，由Admin和Clinical角色台的HerbManagementView嵌入使用
- **三控件分离**: HerbMasterDetailControl(主框架) + HerbEditControl(编辑表单) + HerbViewControl(只读预览)
- **DataSource抽象层**: Repository通过IHerbDataSource抽象，支持Local(SQLite)/Remote(API)模式切换
- **跨模块解耦**: 通过IHerbSearchProvider接口供Formula和MedicalCase模块使用

### 包装方法模式
Repository同时提供标准方法(抛异常)和包装方法(返回元组)两套接口:
- `CreateAsync()` -- 抛异常，内部方法使用
- `CreateWithResultAsync()` -- 返回(success, data, error)，ViewModel使用

---

## 已知陷阱

1. **HerbEditControl的HerbEffect属性**: 使用HerbEffect而非Effect命名，与FormulaEditControl的FormulaEffect命名一致，均为避免与UIElement.Effect冲突
2. **Name自动生成PinYinCode**: HerbDetailModel.Name的setter会自动调用PinYinHelper.GetPinYinCode，Clone()方法中直接赋值私有字段_name/_pinYinCode避免触发
3. **导入导出仅Remote模式**: BatchImportAsync/ExportTemplateAsync/ExportHerbsAsync在本地模式返回null，ViewModel需处理null情况
4. **BatchEnable/BatchDisable本地模式不支持**: 返回null而非异常，调用方需检查
5. **HerbViewControl未被外部模块引用**: 仅在HerbMasterDetailControl.xaml中使用，如需在其他模块显示药材信息需评估可见性

---

## 控件架构 (2026-01-04统一)

### 当前架构

**处方/验方药材编辑** (新架构):
```
Controls/
├── HerbList/                    # 药材列表控件
│   ├── HerbListControl.xaml     # 管理多个HerbItemControl
│   ├── HerbListControlViewModel.cs
│   └── HerbListChangedEventArgs.cs
├── HerbItem/                    # 单个药材项控件
│   ├── HerbItemControl.xaml     # 药材名+剂量+煎法
│   ├── HerbItemControlViewModel.cs
│   └── HerbItemChangedEventArgs.cs
└── Shared/                      # 共享组件
```

**药材管理MasterDetail**:
```
Controls/
├── HerbMasterDetailControl.xaml  # 药材管理主控件
├── HerbEditControl.xaml          # 编辑表单
└── HerbViewControl.xaml          # 只读预览
```

### 已删除的过期控件

- `HerbCardControl` - 旧版药材卡片，被 `HerbItemControl` 替代
- `HerbListView` - 旧版只读列表，被 `HerbListControl(IsEditMode=False)` 替代

### 使用方式

**编辑模式** (处方/验方编辑):
```xml
<herbList:HerbListControl
    AllHerbs="{Binding AllHerbs}"
    HerbItems="{Binding HerbItems, Mode=TwoWay}"
    IsEditMode="True"
    Columns="4" />
```

**只读模式** (医案预览):
```xml
<herbList:HerbListControl
    HerbItems="{Binding HerbItems}"
    AllHerbs="{Binding AllHerbs}"
    IsEditMode="False"
    Columns="4" />
```

### 相关OpenSpec

- `unify-herb-controls-to-herbs-module` - 统一药材控件到Herbs模块
- `herb-editor-control-refactoring` - HerbListControl/HerbItemControl重构

---

最后更新: 2026-03-01

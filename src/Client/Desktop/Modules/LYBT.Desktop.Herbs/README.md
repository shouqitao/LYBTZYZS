# LYBT.Desktop.Herbs

> 中药材管理模块 | 药材 CRUD / 拼音搜索 / 分类筛选 / 跨模块搜索提供者 / 批量导入导出

## 项目定位

- **层级**: Client → Desktop → Modules
- **职责**: 中药材基础数据管理，提供药材 CRUD、拼音快速搜索（全拼+首字母）、分类筛选、批量导入导出；通过 IHerbSearchProvider 为 Formula 和 MedicalCase 模块提供跨模块搜索能力

## 目录结构

```
LYBT.Desktop.Herbs/
├── Controls/
│   ├── HerbEditControl.xaml/.xaml.cs              # 药材编辑控件（双向绑定+验证）
│   ├── HerbMasterDetailControl.xaml/.xaml.cs      # Master-Detail 可复用控件
│   └── HerbViewControl.xaml/.xaml.cs              # 药材只读预览控件
├── Interfaces/
│   ├── IHerbRepository.cs                         # 药材仓储接口（CRUD+搜索+批量）
│   ├── IHerbService.cs                            # 药材业务服务接口
│   ├── IHerbSearchProvider.cs                     # 跨模块搜索提供者接口
│   └── IHerbStatusHandler.cs                      # 状态处理接口
├── Mappers/
│   └── HerbMapper.cs                              # Mapperly 编译时映射器
├── Models/
│   ├── Items/
│   │   └── HerbEditContext.cs                     # 编辑上下文模型
│   └── HerbDetailModel.cs                         # Detail 编辑模型（ValidatableModelBase）
├── Repositories/
│   └── HerbRepository.cs                          # 仓储实现（Repository 抽象层）
├── Services/
│   ├── HerbSearchProvider.cs                      # 跨模块搜索提供者（分页循环加载）
│   └── RemoteHerbService.cs                       # 业务服务实现
├── ViewModels/
│   ├── Handlers/
│   │   ├── IHerbStatusHandler.cs                  # 状态处理接口
│   │   └── HerbStatusHandler.cs                   # 状态处理实现（ToggleStatus）
│   ├── HerbEditorViewModel.cs                     # 编辑器 ViewModel
│   └── HerbMasterDetailViewModel.cs               # 核心 ViewModel（组合模式）
└── HerbsModule.cs                                 # Prism 模块注册
```

## 核心组件

| 类 | 设计依据 | 职责 |
|---|---|---|
| **HerbsModule** : IModule | Prism 模块注册，仅依赖 Auth | RegisterTypes: HerbMasterDetailVM + EditorVM + IHerbService + IHerbSearchProvider + IHerbStatusHandler + MasterDetailServices |
| **HerbMasterDetailViewModel** : MasterDetailViewModelBase | 组合模式 ViewModel | 属性: IsNameEditable（计算，仅新建可编辑）/ IsAdmin / StatusOptions / DetailTitle（计算）。基类实现: LoadListAsync / LoadDetailAsync / CreateNewDetail / SaveDetailAsync（含 Name/Unit/Price/CostPrice 验证）/ DeleteItemAsync。命令: ToggleStatusCommand / CopyHerbCommand（Clone+重命名）/ RestoreCommand / SearchByCategoryCommand / ImportHerbsCommand / ExportHerbsCommand |
| **HerbEditorViewModel** | HerbEditContext 编辑上下文 | Validate / GetHerbData 方法，编辑表单逻辑分离 |
| **HerbStatusHandler** : BaseStatusHandler | Handler 组件拆分，SRP | ExecuteToggleStatusAsync（启用/禁用切换），比 Patients 模块多了 ToggleStatus |
| **RemoteHerbService** : IHerbService | 统一错误处理 | 10 个方法: CreateAsync / UpdateAsync / DeleteAsync / GetByIdAsync / GetPagedAsync / SearchAsync / ToggleStatusAsync / RestoreAsync / BatchDeleteAsync / GetAllAsync |
| **HerbSearchProvider** : IHerbSearchProvider | 跨模块解耦（D5-3），委托 IHerbRepository | SearchHerbsAsync（关键词搜索）/ GetAllHerbsAsync（分页循环加载，pageSize=100）。供 Formula 和 MedicalCase 模块使用 |
| **HerbRepository** : IHerbRepository | Repository 抽象层，Local/Remote 切换 | 标准 CRUD + 包装方法（CreateWithResultAsync 等返回元组）。导入导出仅 Remote 模式。依赖 IHerbApi?（可选，仅 Remote 批量/导入导出） |
| **HerbMapper** | Mapperly 编译时映射 | HerbDetailDto ↔ HerbDetailModel ↔ HerbInputDto。ToItem / ToDto / ToInputDtoCore / ToInputDto（Id 空 Guid 转 null） |
| **HerbDetailModel** : ValidatableModelBase | Detail 区域编辑模型 | 属性: Id / IsNew（计算）/ Name（[Required]，自动生成 PinYinCode）/ PinYinCode / Category / Properties / Origin / Spec / Unit（[Required]）/ Price（[Required][Range]）/ CostPrice（[Range]）/ Effect / Usage / Remark / Status / CreatedAt / UpdatedAt。方法: CreateNew()（默认 Unit="克"）/ Clone()（直接赋值私有字段） |

## 依赖关系

### 依赖
- LYBT.Desktop.Infrastructure（MasterDetailControlBase / ViewModelBase / Services）
- LYBT.Desktop.Foundation（BaseApiRepository / Security）
- LYBT.Desktop.Contracts（IHerbApi / IHerbRepository）
- LYBT.Desktop.Models（ValidatableModelBase）
- LYBT.Shared.Models（HerbListDto / HerbDetailDto / HerbInputDto）
- Prism.DryIoc（8.x）

### 被依赖
- LYBT.Desktop.Formula（通过 IHerbSearchProvider 搜索药材）
- LYBT.Desktop.MedicalCase（通过 IHerbSearchProvider 搜索药材）
- LYBT.Desktop.Admin（HerbManagementView 嵌入 HerbMasterDetailControl）
- LYBT.Desktop.Clinical（HerbMasterDetailControl 复用）

## 设计决策

| 决策 | 原因 |
|------|------|
| IHerbSearchProvider 跨模块接口 | 解耦 Herbs 与 Formula/MedicalCase 模块，通过接口提供搜索能力（D5-3） |
| HerbSearchProvider 分页循环加载（pageSize=100） | 全量加载避免单次请求过大，分页循环保证数据完整性 |
| Handler 组件拆分（HerbStatusHandler 含 ToggleStatus） | 与 Patients 不同，Herbs 需要启用/禁用切换功能 |
| Repository 包装方法模式 | 标准方法抛异常（内部使用），包装方法返回 (success, data, error) 元组（ViewModel 使用） |
| HerbDetailModel.Name 自动生成 PinYinCode | 中医师拼音检索习惯，输入"hq"即可定位"黄芪" |
| 三控件分离（MasterDetail + Edit + View） | Admin 和 Clinical 角色台复用同一 MasterDetail 框架，Edit/View 独立可替换 |
| Mapperly 编译时映射替代 AutoMapper | 零运行时开销，编译期类型安全 |
| 仅依赖 Auth 模块 | 药材是独立基础数据模块，与业务流程解耦，最小依赖 |

## 已知陷阱

1. **HerbEditControl 的 HerbEffect 属性**: 使用 HerbEffect 而非 Effect 命名，避免与 UIElement.Effect 冲突（与 FormulaEditControl 的 FormulaEffect 命名一致）
2. **Name 自动生成 PinYinCode**: HerbDetailModel.Name 的 setter 自动调用 PinYinHelper.GetPinYinCode，Clone() 方法中直接赋值私有字段 _name/_pinYinCode 避免触发
3. **导入导出仅 Remote 模式**: BatchImportAsync / ExportTemplateAsync / ExportHerbsAsync 在本地模式返回 null，ViewModel 需处理 null 情况
4. **BatchEnable / BatchDisable 本地模式不支持**: 返回 null 而非异常，调用方需检查
5. **HerbViewControl 未被外部模块引用**: 仅在 HerbMasterDetailControl.xaml 中使用，如需在其他模块显示药材信息需评估可见性
6. **HerbRepository.IHerbApi 可选注入**: Local 模式下为 null，调用 Remote-only 方法时返回 null / NotSupportedException
7. **Name 仅新建可编辑**: HerbMasterDetailViewModel.IsNameEditable 计算属性控制，编辑模式下 Name 只读

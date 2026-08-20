# Desktop 层架构设计规范（SSOT）

> 版本: v1.0 | 日期: 2026-08-09 | 维护者: 技术总监
> 状态: 📝 草稿（待用户确认后定稿）
> 依据: Prism Library 官方文档 + Microsoft Learn MVVM 指南 + 本项目实际代码审计

---

本文档定义 WPF Desktop 客户端的架构设计规范，涵盖 Prism MVVM 模式、数据流、模块目录结构、命名约定和架构约束。目标是统一所有业务域（Herb/Formula/Patient/User/Registration/MedicalCase）的 Desktop 层实现方式，确保 DTO 不暴露到 UI 层、EditContext 支持取消恢复。

## 一、设计原则

### 1.1 Prism MVVM 核心原则（官方）

| 原则 | 说明 |
|------|------|
| View 只绑定 ViewModel | View 的 DataContext = ViewModel，不直接操作 Model/DTO |
| ViewModel 不引用 UI 类型 | ViewModel 禁止引用 Button/ListView 等控件类型 |
| Model 是可编辑的数据副本 | ViewModel 操作 Model（可修改），不是操作 DTO（传输对象） |
| Service 是唯一数据通道 | ViewModel 通过 Service 获取/保存数据，不直接调用 API Client |
| 1 View = 1 ViewModel | 每个 View 有对应 ViewModel，通过 AutoWire 绑定 |
| 模块自治 | 每个模块封装自己的 Views/ViewModels/Models/Services |

### 1.2 本项目额外原则

| 原则 | 说明 |
|------|------|
| **DTO 不暴露到 UI 层** | DTO 是 API 传输对象，不是 UI 编辑对象。XAML 绑定的目标必须是 Model 或 ViewModel 属性 |
| **EditContext 支持取消** | 有编辑状态的域必须有 EditContext，支持「编辑→取消→恢复」 |
| **简单域可以简化** | 映射 ≤12 字段且无嵌套集合的域，ViewModel 内联方法即可，不需要独立 Mapper 类 |
| **复杂域用 Mapperly** | 有嵌套集合/多向转换的域，用 Mapperly Mapper 类 |
| **目录结构必须一致** | 所有模块的子目录命名和存在性必须统一 |

---

## 二、标准数据流

### 2.1 理想链路

```text
API Response (DTO)
    ↓ Desktop Service（封装 API 调用 + 错误处理）
DTO
    ↓ ViewModel（调用 Service 获取 DTO → 转换为 Model）
Model（可编辑的 UI 数据副本）
    ↓ 用户编辑
Model
    ↓ ViewModel（调用 Service 保存 → Model 转换为 InputDto）
InputDto → API Request
```text

### 2.2 关键转换点

| 转换 | 方式 | 规范 |
|------|------|------|
| DTO → Model | `InitializeFromDto(TDto dto)` | ViewModel 或 EditorViewModel 内的方法 |
| Model → InputDto | `ToInputDto()` | EditorViewModel 或 Model 自身方法 |
| Model ↔ EditContext | `CreateEditContext()` / `ApplyEditContext()` | 有编辑状态的域 |

### 2.3 各层职责

| 层 | 职责 | 禁止 |
|----|------|------|
| **Service** | 调用 API、错误处理、返回 CommandResult<T> | 不操作 Model |
| **ViewModel** | 编排流程、持有 Model/EditContext、暴露命令和属性 | 不直接调用 IApiClient |
| **Model** | UI 可编辑数据、计算属性、验证规则 | 不持有 DTO 引用 |
| **EditContext** | 编辑快照、支持取消恢复 | 不含业务逻辑 |
| **View** | XAML 绑定、纯展示 | 不含业务逻辑（极少 code-behind） |

---

## 三、模块目录结构规范

### 3.1 标准目录

```text
LYBT.Desktop.{ModuleName}/
├── {ModuleName}Module.cs          ← IModule 实现（DI 注册）
├── Models/                        ← UI 数据模型
│   ├── {Entity}DetailModel.cs     ← 主详情模型（继承 ValidatableModelBase）
│   └── Items/                     ← 子项模型（可选）
│       └── {Entity}EditContext.cs ← 编辑上下文（可选，有编辑状态时需要）
├── ViewModels/
│   ├── {Entity}MasterDetailViewModel.cs  ← 主列表+详情 VM
│   ├── {Entity}EditorViewModel.cs        ← 编辑器子 VM（可选）
│   └── Handlers/                         ← 状态处理器（可选）
├── Views/
│   ├── {Entity}MasterDetailControl.xaml   ← 主控件
│   ├── {Entity}ViewControl.xaml           ← 只读视图
│   └── {Entity}EditControl.xaml           ← 编辑视图
├── Mappers/                       ← 映射器（可选，复杂域才需要）
│   └── {Entity}DetailModelMapper.cs
├── Services/                      ← 模块内服务
│   └── Remote{Entity}Service.cs
├── Repositories/                  ← 仓储（可选）
│   └── {Entity}Repository.cs
├── Controls/                      ← 自定义控件（可选）
├── Dialogs/                       ← 对话框（可选）
└── Events/                        ← 模块内事件（可选）
```text

### 3.2 目录存在性矩阵

| 子目录 | 必须 | 条件 |
|--------|:---:|------|
| Models/ | ✅ | 所有模块必须有（即使只有空标记） |
| Models/Items/ | ⬜ | 有子项模型时 |
| ViewModels/ | ✅ | 所有模块必须有 |
| Views/ | ✅ | 所有模块必须有 |
| Services/ | ✅ | 所有模块必须有（即使只是 Remote 调用封装） |
| Mappers/ | ⬜ | 映射复杂（嵌套集合/多向转换）时 |
| Repositories/ | ⬜ | 有本地数据缓存时 |
| Controls/ | ⬜ | 有自定义控件时 |
| Dialogs/ | ⬜ | 有对话框时 |
| Events/ | ⬜ | 有模块内事件时 |

### 3.3 当前模块对标

| 子目录 | Auth | Catalog | Patients | Users | Registration | MedicalCase |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|
| Models/ | ✅ | ✅ | ✅ | ✅ | ❌ → 需补 | ✅ |
| ViewModels/ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Views/ | ✅ | ❌ 用 Controls/ | ❌ 用 Controls/ | ❌ 用 Controls/ | ✅ | ❌ 用 Controls/ |
| Services/ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Mappers/ | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Repositories/ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |

**注意**：Controls/ vs Views/ 的选择取决于 Prism Region 导航方式。当前项目用 Controls/（UserControl）而非 Views/，这是合理的 Prism 用法，不需要强制改为 Views/。

---

## 四、各域目标状态

### 4.1 Herb（药材）— 保持现状 ✅

| 项 | 现状 | 目标 |
|----|------|------|
| Model | HerbDetailModel ✅ | 不变 |
| EditContext | HerbEditContext ✅ | 不变 |
| Mapper | VM 内联（10 字段直拷） | 不变（简单域不需要 Mapper） |
| Service | IHerbService ✅ | 不变 |

### 4.2 Formula（验方）— 修复 Herbs 集合类型 ⚠️

| 项 | 现状 | 目标 |
|----|------|------|
| Model | FormulaDetailModel ✅ | 不变 |
| EditContext | FormulaEditContext ✅ | 不变 |
| Herbs 集合 | `ObservableCollection<FormulaHerbItemDto>` 🔴 | → `ObservableCollection<FormulaHerbItemModel>` |
| Mapper | FormulaDetailModelMapper ✅ | 更新映射目标类型 |

**改动**：新建 `FormulaHerbItemModel`，替换 DTO 直接暴露。

### 4.3 Patient（患者）— 保持现状 ✅

同 Herb，简单域，VM 内联映射足够。

### 4.4 User（用户）— 保持现状 ✅

同 Herb，简单域，VM 内联映射足够。

### 4.5 Registration（挂号）— 补 Model 层 🔴

| 项 | 现状 | 目标 |
|----|------|------|
| Model | ❌ 无 | → 新建 `RegistrationDetailModel` |
| EditContext | ❌ 无 | → 新建 `RegistrationEditContext` |
| ViewModel | 直接持有 DTO | → 改为持有 Model |
| 创建对话框 | 直接构造 DTO | → 改为构造 EditContext → 转 InputDto |

**理由**：即使 Registration 当前简单，补 Model 层是为了：
1. 与其他 5 域一致（可维护性）
2. 未来扩展时不需要大改（可扩展性）
3. DTO 不暴露到 UI 层（分层原则）

### 4.6 MedicalCase（医案）— 编辑外壳重新设计 🔴

**背景（2026-08-10 定案）**：现有编辑链路为「DTO 快照 + Clone 恢复」四件套（`Services/MedicalCaseEditContext` 持 `MedicalCaseDetailDto` 快照，CommandService 逐字段比较 DTO 判变更，LifecycleService Clone 深拷贝恢复），违反分层原则且笨重。产品负责人拍板：**重新设计**为真正的 Model + EditContext 体系，不做兼容层。

| 项 | 现状 | 目标 |
|----|------|------|
| 编辑真源 | `Services/MedicalCaseEditContext`（DTO 快照） | **删除**；改为 Model 为基础 |
| Model | `MedicalCaseDetailModel`（`PrescriptionItems` 持 `ObservableCollection<PrescriptionItemDto>`） | `PrescriptionItems` 改为 `ObservableCollection<PrescriptionItemModel>`（前后端各自实例原则） |
| EditContext | `Models/Items/MedicalCaseEditContext`（死代码，0 引用） | **重建**为完整编辑会话：诊断字段 + 处方行集合 + 状态，支持 BeginEdit/Commit/Cancel |
| 处方行 | `PrescriptionItemViewModel.Items = ObservableCollection<PrescriptionItemDto>` | 新建 `PrescriptionItemModel : ObservableObject, IHerbItemEditable`；`Items` 改为 Model 集合 |
| 共享控件 | `HerbListControl.HerbItems` DP 类型 `IList<PrescriptionItemDto>` | 改为宽松 `IEnumerable`（与 FormulaEditControl 同款），内部已用 IHerbItemEditable 抽象 |
| 变更检测 | CommandService 三方法逐字段比较 DTO | EditContext 脏标记（IsDirty） |
| Clone 恢复 | `MedicalCaseCloneMapper`（DTO 深拷贝） | **删除**；EditContext 快照恢复 |
| Mapper | 4 个 Mapper ✅ | 保留 + 扩展 PrescriptionItemDto↔Model 映射 |

**前后端各自定义实例原则（必选，2026-08-10 产品负责人确认）**：前端（Desktop）与后端（Server/Shared）各自定义属于自己的实例——
- Server/Shared 侧：`LYBT.Shared.Models` 定义 DTO（`PrescriptionItemDto` 等），仅作 API 传输契约，不承载 UI 编辑；
- Desktop 侧：`Models/` 定义 Model（`MedicalCaseDetailModel`、`PrescriptionItemModel` 等），是 UI 可编辑数据副本，不持有 DTO 引用；
- 两侧实例只在 Service/Mapper 边界转换（`InitializeFromDto` / `ToInputDto`），禁止在 UI 层直接编辑对方实例。

**理由**：医案编辑涉及诊断+处方，取消编辑需要恢复。EditContext 快照（Model 级）比 DTO Clone 更优雅、更符合分层；处方行 Model 化是 DP-M1（DTO 仅传输）的最终闭合。

---

## 五、命名规范

### 5.1 文件命名

| 类型 | 命名规则 | 示例 |
|------|---------|------|
| Model | `{Entity}DetailModel.cs` | `HerbDetailModel.cs` |
| EditContext | `{Entity}EditContext.cs` | `HerbEditContext.cs` |
| ViewModel | `{Entity}MasterDetailViewModel.cs` | `HerbMasterDetailViewModel.cs` |
| EditorVM | `{Entity}EditorViewModel.cs` | `HerbEditorViewModel.cs` |
| Mapper | `{Entity}DetailModelMapper.cs` | `FormulaDetailModelMapper.cs` |
| Service | `I{Entity}Service.cs` + `{Entity}Service.cs` | `IHerbService.cs` + `HerbService.cs` |

### 5.2 命名空间

```
LYBT.Desktop.{ModuleName}
LYBT.Desktop.{ModuleName}.Models
LYBT.Desktop.{ModuleName}.Models.Items
LYBT.Desktop.{ModuleName}.ViewModels
LYBT.Desktop.{ModuleName}.ViewModels.Handlers
LYBT.Desktop.{ModuleName}.Views
LYBT.Desktop.{ModuleName}.Services
LYBT.Desktop.{ModuleName}.Mappers
LYBT.Desktop.{ModuleName}.Repositories
```text

**禁止**：`LYBT.Desktop.Modules.{ModuleName}` （MedicalCase 当前违规）

### 5.3 转换方法命名

| 方法 | 签名 | 用途 |
|------|------|------|
| `InitializeFromDto` | `void InitializeFromDto(TDto dto)` | DTO → Model/Context（加载） |
| `ToInputDto` | `TInputDto ToInputDto()` | Model/Context → InputDto（保存） |
| `CreateNew` | `static TModel CreateNew()` | 创建空 Model |
| `Clone` | `TModel Clone()` | 克隆 Model（取消恢复备用） |

---

## 六、架构约束（写入架构测试）

| 编号 | 约束 | 说明 |
|------|------|------|
| DP-M1 | Desktop ViewModel 禁止直接持有 DTO 做编辑 | DTO 是传输对象，编辑必须通过 Model |
| DP-M2 | 每个 MasterDetail 模块必须有 DetailModel | 即使简单域也要有 Model（可以只是 DTO 的薄包装） |
| DP-M3 | Desktop 命名空间禁止 `LYBT.Desktop.Modules.` 前缀 | 统一 `LYBT.Desktop.{ModuleName}` |

---

## 七、执行计划

### 第 1 步：文档定稿（本次）
- 本文档经用户确认后定稿
- 同步更新蓝图 §2.x Desktop 章节

### 第 2 步：T1 命名+属性对齐（小任务）
- 9 项命名/属性修改
- 独立 commit

### 第 3 步：Desktop 层重构（中任务）
- Registration 补 Model + EditContext
- Formula Herbs 集合类型修复
- MedicalCase 补 EditContext
- MedicalCase 命名空间修复
- PrescriptionItemViewModel 位置修复
- 架构测试补全（DP-M1/M2/M3）

### 第 4 步：验证
- `dotnet build --no-incremental` 0/0
- 架构测试通过
- 全量 commit + push

---

*本文档是 Desktop 层架构设计的 SSOT。所有 Desktop 相关的重构决策以此为准。*

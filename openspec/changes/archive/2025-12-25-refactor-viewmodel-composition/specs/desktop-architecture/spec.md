# Desktop Architecture - ViewModel组合模式

## ADDED Requirements

### Requirement: ViewModel服务接口层

**ID**: REQ-VMCOMP-001

系统SHALL提供可组合的ViewModel服务接口，实现关注点分离。

**验收标准**:
- 系统MUST定义9个独立服务接口（ILoadingStateManager, IPaginationService等）
- 每个接口MUST职责单一，符合SRP原则
- 接口MUST继承INotifyPropertyChanged支持XAML绑定

#### Scenario: 加载状态管理服务

**Given**: ViewModel需要管理加载状态
**When**: 注入ILoadingStateManager服务
**Then**: 可以通过服务管理IsLoading、IsBusy、BusyMessage属性
**And**: 支持嵌套加载计数
**And**: ExecuteWithLoadingAsync自动管理加载状态

#### Scenario: 分页服务

**Given**: 列表ViewModel需要分页功能
**When**: 注入IPaginationService服务
**Then**: 可以通过服务管理CurrentPage、PageSize、TotalCount
**And**: 自动计算TotalPages
**And**: 提供FirstPage/PreviousPage/NextPage/LastPage导航方法

---

### Requirement: 服务组合模式

**ID**: REQ-VMCOMP-002

系统SHALL提供组合服务接口，简化依赖注入。

**验收标准**:
- 系统MUST提供IListViewServices<T>组合加载、分页、搜索、选择服务
- 系统MUST提供IMasterDetailServices<TListItem, TDetail>组合列表服务和详情编辑服务
- 组合服务MUST通过DI容器自动装配子服务

#### Scenario: 列表视图服务组合

**Given**: 列表ViewModel需要多个服务
**When**: 注入IListViewServices<T>
**Then**: 可以通过单一接口访问LoadingState、Pagination、Search、Selection服务
**And**: 减少构造函数参数数量

#### Scenario: Master-Detail服务组合

**Given**: Master-Detail ViewModel需要列表和详情服务
**When**: 注入IMasterDetailServices<TListItem, TDetail>
**Then**: 可以通过单一接口访问ListView服务和DetailEditor服务
**And**: 属性委托给相应服务实现

---

### Requirement: 轻量级ViewModel基类

**ID**: REQ-VMCOMP-003

系统SHALL提供最小化的ViewModel基类，支持组合模式。

**验收标准**:
- LightViewModelBase MUST仅提供INotifyPropertyChanged
- ComposableViewModelBase MUST支持导航、生命周期、服务注入
- 新基类代码量MUST减少50%以上

#### Scenario: 组合式ViewModel创建

**Given**: 需要创建新的MasterDetail ViewModel
**When**: 继承ComposableViewModelBase并注入服务
**Then**: ViewModel代码仅包含业务逻辑
**And**: 通用功能由注入的服务提供
**And**: 构造函数参数清晰反映依赖

---

### Requirement: 渐进式迁移支持

**ID**: REQ-VMCOMP-004

系统SHALL支持现有ViewModel渐进迁移到新模式。

**验收标准**:
- 旧基类MUST标记为Obsolete但保持功能
- 新旧ViewModel MUST可以共存
- XAML绑定MUST无需修改

#### Scenario: 旧ViewModel兼容

**Given**: 现有ViewModel继承MasterDetailViewModelBase
**When**: 添加新的组合模式ViewModel
**Then**: 旧ViewModel继续正常工作
**And**: 编译时显示Obsolete警告
**And**: 可以逐步迁移

---

### Requirement: DI服务注册

**ID**: REQ-VMCOMP-005

系统SHALL提供服务注册扩展方法。

**验收标准**:
- 系统MUST提供AddViewModelServices扩展方法注册所有服务
- 服务生命周期MUST配置正确（Transient/Singleton）
- 系统MUST支持泛型服务注册

#### Scenario: 服务注册

**Given**: 应用程序启动
**When**: 调用services.AddViewModelServices()
**Then**: 所有ViewModel服务注册到DI容器
**And**: 可以通过构造函数注入获取服务
**And**: 服务实例按配置的生命周期创建

---

## 影响范围

**影响模块**:
- LYBT.Desktop.Models（新增接口）
- LYBT.Desktop.Infrastructure（新增实现）
- LYBT.Desktop.Herbs（迁移试点）
- LYBT.Desktop.Formula（迁移）
- LYBT.Desktop.Patients（迁移）
- LYBT.Desktop.MedicalCase（迁移）
- LYBT.Desktop.Users（迁移）

**不影响**:
- 后端API
- 数据库Schema
- 现有XAML视图

---

## 技术约束

1. **依赖框架**: 
   - CommunityToolkit.Mvvm >= 8.0
   - Microsoft.Extensions.DependencyInjection >= 8.0
   - Prism.DryIoc >= 8.1

2. **兼容性**:
   - .NET 8.0
   - WPF

3. **性能**:
   - 服务实例化开销可忽略
   - 内存占用与现有方案相当

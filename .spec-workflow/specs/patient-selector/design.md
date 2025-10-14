# 患者选择器组件 - 设计文档

## 📋 文档信息

| 项目 | 内容 |
|-----|------|
| **功能名称** | 患者选择器组件 (Patient Selector Component) |
| **Spec编号** | SPEC-2025-002 |
| **设计日期** | 2025-10-14 |
| **设计人** | Claude Code |
| **状态** | 待审批 |
| **版本** | v1.0 |
| **需求文档** | `.spec-workflow/specs/patient-selector/requirements.md` |

---

## 1. Overview

**患者选择器组件** 是一个独立可复用的 WPF UserControl 组件,提供患者搜索、选择和快速创建功能。该组件基于 MVVM 架构和 Prism 框架,通过 EventAggregator 发布患者选择事件,与使用方松耦合。

### 1.1 核心价值

- **独立可复用**: 可在多个场景使用(临床工作台、报表查询、病案管理等)
- **职责单一**: 专注于患者选择,不涉及业务逻辑
- **事件驱动**: 通过 Prism EventAggregator 解耦,支持多订阅者
- **高性能**: 搜索响应 ≤300ms,创建患者 ≤1s

### 1.2 在系统中的位置

```
LYBT.Desktop.Common (通用组件库)
  └── Components/
      └── PatientSelector/
          ├── PatientSelectorControl.xaml        (View)
          ├── PatientSelectorControl.xaml.cs     (View Code-behind)
          └── PatientSelectorViewModel.cs        (ViewModel)

LYBT.Desktop.Infrastructure (基础设施)
  └── Events/
      ├── PatientSelectedEvent.cs                (事件定义)
      └── PatientSelectedPayload.cs              (事件负载)
```

---

## 2. Steering Document Alignment

### 2.1 技术标准 (tech.md)

**遵循项目技术约束**:
- ✅ **MVVM 三层架构**: View(XAML) + ViewModel + Model(Dto)
- ✅ **Prism 框架**: 使用 EventAggregator 进行事件通信
- ✅ **MaterialDesignInXaml**: UI 组件遵循 MD 设计规范
- ✅ **AutoMapper**: Dto ↔ Item 转换
- ✅ **依赖注入**: 构造函数注入 IPatientRepository
- ✅ **异步优先**: 所有 I/O 操作使用 async/await

**禁止技术**:
- ❌ Container.Resolve
- ❌ ServiceLocator
- ❌ 同步阻塞调用

### 2.2 项目结构 (structure.md)

**组件位置**:
- `LYBT.Desktop.Common`: 通用可复用组件,不依赖具体业务模块
- `LYBT.Desktop.Infrastructure`: 基础设施(Events、Converters、Base类)

**命名约定**:
- Control: `PatientSelectorControl`
- ViewModel: `PatientSelectorViewModel`
- Event: `PatientSelectedEvent`
- Payload: `PatientSelectedPayload`

---

## 3. Code Reuse Analysis

### 3.1 现有组件复用

| 组件/工具 | 如何使用 |
|-----------|----------|
| **IPatientRepository** | 通过依赖注入获取,调用 `SearchAsync()` 和 `CreateAsync()` |
| **PatientDto** | 搜索结果的数据传输对象,直接使用 |
| **PatientCreateDto** | 创建新患者时使用的请求对象 |
| **PatientItem** | PatientDto 的 UI 模型,用于 ViewModel 绑定 |
| **AutoMapper** | Dto ↔ Item 自动转换 |
| **Prism EventAggregator** | 发布患者选择事件 |
| **MaterialDesign TextBox** | 搜索框 UI 组件 |
| **MaterialDesign Button** | 操作按钮 UI 组件 |
| **BooleanToVisibilityConverter** | 现有转换器,控制可见性 |

### 3.2 新增组件

| 组件 | 目的 | 位置 |
|------|------|------|
| **PatientSelectorControl** | 患者选择器视图 | `LYBT.Desktop.Common/Components/PatientSelector` |
| **PatientSelectorViewModel** | 患者选择器视图模型 | `LYBT.Desktop.Common/Components/PatientSelector` |
| **PatientSelectedEvent** | 患者选择事件 | `LYBT.Desktop.Infrastructure/Events` |
| **PatientSelectedPayload** | 事件负载 | `LYBT.Desktop.Infrastructure/Events` |

### 3.3 集成点

**与现有模块的集成**:
- **Patients 模块**: 使用 `IPatientRepository` 接口(已存在)
- **临床工作台**: 订阅 `PatientSelectedEvent`,响应患者选择
- **报表模块**: 可复用相同组件进行患者筛选
- **病案管理**: 可复用相同组件进行患者关联

---

## 4. Architecture

### 4.1 整体架构

**MVVM + Event-Driven Architecture**

```
┌────────────────────────────────────────────────────────────┐
│                     使用方 (订阅者)                          │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  ClinicalWorkbenchViewModel                          │  │
│  │  ├─ 订阅: PatientSelectedEvent                       │  │
│  │  └─ 处理: LoadPatientHistory(PatientId)             │  │
│  └──────────────────────────────────────────────────────┘  │
└───────────────────────┬────────────────────────────────────┘
                        │ (EventAggregator)
                        │ Subscribe/Publish
┌───────────────────────▼────────────────────────────────────┐
│              PatientSelectorControl (Component)            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  PatientSelectorControl.xaml (View)                  │  │
│  │  ├─ SearchBox (TextBox)                              │  │
│  │  ├─ SearchResults (ListBox)                          │  │
│  │  └─ QuickCreatePanel (StackPanel)                    │  │
│  └──────────────────────────────────────────────────────┘  │
│                        ↕ Binding                            │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  PatientSelectorViewModel                            │  │
│  │  ├─ Properties:                                      │  │
│  │  │   ├─ SearchKeyword                                │  │
│  │  │   ├─ SearchResults (ObservableCollection)        │  │
│  │  │   ├─ SelectedPatient                              │  │
│  │  │   └─ QuickCreateForm (Name, Gender, Phone)       │  │
│  │  ├─ Commands:                                        │  │
│  │  │   ├─ SearchCommand                                │  │
│  │  │   ├─ SelectPatientCommand                         │  │
│  │  │   └─ QuickCreateCommand                           │  │
│  │  └─ Dependencies:                                    │  │
│  │      ├─ IPatientRepository (注入)                    │  │
│  │      └─ IEventAggregator (注入)                      │  │
│  └──────────────────────────────────────────────────────┘  │
│                        ↕ Repository                         │
│  ┌──────────────────────────────────────────────────────┐  │
│  │  PatientRepository                                   │  │
│  │  ├─ SearchAsync(keyword)                             │  │
│  │  └─ CreateAsync(PatientCreateDto)                    │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
                        ↕ HTTP
┌────────────────────────────────────────────────────────────┐
│                      WebAPI Server                         │
│  GET /api/patients/search?keyword={keyword}               │
│  POST /api/patients                                        │
└────────────────────────────────────────────────────────────┘
```

### 4.2 模块化设计原则

✅ **单一职责**:
- PatientSelectorControl: 仅负责患者选择 UI
- PatientSelectorViewModel: 仅负责搜索、选择、创建逻辑
- PatientSelectedEvent: 仅负责事件通知

✅ **依赖倒置**:
- ViewModel 依赖 `IPatientRepository` 接口,不依赖具体实现
- ViewModel 依赖 `IEventAggregator` 接口

✅ **开闭原则**:
- 通过事件发布,可扩展多个订阅者,无需修改组件代码
- 可通过 DependencyProperty 扩展配置(如默认过滤条件)

---

## 5. Components and Interfaces

### 5.1 PatientSelectorControl (View)

**目的**: 提供患者选择的用户界面

**位置**: `LYBT.Desktop.Common/Components/PatientSelector/PatientSelectorControl.xaml`

**UI 结构**:
```xml
<UserControl>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />    <!-- 搜索框 -->
            <RowDefinition Height="*" />       <!-- 搜索结果列表 -->
            <RowDefinition Height="Auto" />    <!-- 快速创建面板 -->
        </Grid.RowDefinitions>

        <!-- Row 0: 搜索框 -->
        <TextBox Text="{Binding SearchKeyword, UpdateSourceTrigger=PropertyChanged}"
                 Style="{StaticResource MaterialDesignOutlinedTextBox}" />

        <!-- Row 1: 搜索结果 -->
        <ListBox ItemsSource="{Binding SearchResults}"
                 SelectedItem="{Binding SelectedPatient}">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel>
                        <TextBlock Text="{Binding DisplayText}" />
                        <TextBlock Text="{Binding PhoneNumber}" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>

        <!-- Row 2: 快速创建 -->
        <StackPanel Visibility="{Binding ShowQuickCreate, Converter={...}}">
            <TextBox Text="{Binding NewPatientName}" />
            <ComboBox SelectedItem="{Binding NewPatientGender}" />
            <TextBox Text="{Binding NewPatientPhone}" />
            <Button Command="{Binding QuickCreateCommand}" />
        </StackPanel>
    </Grid>
</UserControl>
```

**DependencyProperty**:
- 无(纯 ViewModel 绑定)

---

### 5.2 PatientSelectorViewModel (ViewModel)

**目的**: 处理患者搜索、选择、创建逻辑

**位置**: `LYBT.Desktop.Common/Components/PatientSelector/PatientSelectorViewModel.cs`

**接口定义**:
```csharp
namespace LYBT.Desktop.Common.Components.PatientSelector
{
    public class PatientSelectorViewModel : BindableBase
    {
        // === 依赖注入 ===
        private readonly IPatientRepository _patientRepository;
        private readonly IEventAggregator _eventAggregator;
        private readonly IMapper _mapper;

        // === 属性 ===
        /// <summary>
        /// 搜索关键词(双向绑定)
        /// </summary>
        public string SearchKeyword { get; set; }

        /// <summary>
        /// 搜索结果列表(ObservableCollection)
        /// </summary>
        public ObservableCollection<PatientItem> SearchResults { get; }

        /// <summary>
        /// 选中的患者
        /// </summary>
        public PatientItem? SelectedPatient { get; set; }

        /// <summary>
        /// 是否显示快速创建面板
        /// </summary>
        public bool ShowQuickCreate { get; set; }

        /// <summary>
        /// 快速创建 - 姓名
        /// </summary>
        public string NewPatientName { get; set; }

        /// <summary>
        /// 快速创建 - 性别
        /// </summary>
        public string NewPatientGender { get; set; }

        /// <summary>
        /// 快速创建 - 手机号
        /// </summary>
        public string NewPatientPhone { get; set; }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; set; }

        // === 命令 ===
        public DelegateCommand SearchCommand { get; }
        public DelegateCommand<PatientItem> SelectPatientCommand { get; }
        public DelegateCommand QuickCreateCommand { get; }
        public DelegateCommand ToggleQuickCreateCommand { get; }

        // === 构造函数 ===
        public PatientSelectorViewModel(
            IPatientRepository patientRepository,
            IEventAggregator eventAggregator,
            IMapper mapper)
        {
            _patientRepository = patientRepository;
            _eventAggregator = eventAggregator;
            _mapper = mapper;

            SearchResults = new ObservableCollection<PatientItem>();

            // 初始化命令
            SearchCommand = new DelegateCommand(ExecuteSearchAsync, CanExecuteSearch);
            SelectPatientCommand = new DelegateCommand<PatientItem>(ExecuteSelectPatient, CanSelectPatient);
            QuickCreateCommand = new DelegateCommand(ExecuteQuickCreateAsync, CanQuickCreate);
            ToggleQuickCreateCommand = new DelegateCommand(ExecuteToggleQuickCreate);

            // 监听SearchKeyword变化,自动触发搜索
            PropertyChanged += OnSearchKeywordChanged;
        }

        // === 方法 ===
        private async void ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchKeyword) || SearchKeyword.Length < 2)
            {
                SearchResults.Clear();
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var dtos = await _patientRepository.SearchAsync(SearchKeyword);
                var items = _mapper.Map<List<PatientItem>>(dtos);

                SearchResults.Clear();
                foreach (var item in items)
                {
                    SearchResults.Add(item);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"搜索失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteSelectPatient(PatientItem patient)
        {
            if (patient == null) return;

            // 构造事件负载
            var payload = new PatientSelectedPayload
            {
                PatientId = patient.Id,
                PatientName = patient.Name,
                Gender = patient.Gender,
                Age = patient.Age,
                PhoneNumber = patient.PhoneNumber,
                LastVisitDate = patient.LastVisitDate,
                VisitCount = patient.VisitCount,
                AllergyHistory = patient.AllergyHistory,
                SelectedAt = DateTime.Now
            };

            // 发布事件
            _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);

            // 清空搜索
            SearchKeyword = string.Empty;
            SearchResults.Clear();
            ShowQuickCreate = false;
        }

        private async void ExecuteQuickCreateAsync()
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                var createDto = new PatientCreateDto
                {
                    Name = NewPatientName.Trim(),
                    Gender = NewPatientGender,
                    PhoneNumber = NewPatientPhone.Trim()
                };

                var createdDto = await _patientRepository.CreateAsync(createDto);
                var createdItem = _mapper.Map<PatientItem>(createdDto);

                // 自动选中新创建的患者
                ExecuteSelectPatient(createdItem);

                // 清空表单
                NewPatientName = string.Empty;
                NewPatientGender = "未知";
                NewPatientPhone = string.Empty;
                ShowQuickCreate = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"创建患者失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteToggleQuickCreate()
        {
            ShowQuickCreate = !ShowQuickCreate;
        }

        private bool CanExecuteSearch() => !IsLoading;
        private bool CanSelectPatient(PatientItem patient) => patient != null && !IsLoading;
        private bool CanQuickCreate() =>
            !IsLoading &&
            !string.IsNullOrWhiteSpace(NewPatientName) &&
            !string.IsNullOrWhiteSpace(NewPatientGender) &&
            !string.IsNullOrWhiteSpace(NewPatientPhone);
    }
}
```

**复用**:
- `BindableBase`: Prism 提供的 INotifyPropertyChanged 基类
- `DelegateCommand`: Prism 提供的命令实现
- `IMapper`: AutoMapper 接口

---

### 5.3 PatientSelectedEvent (Event)

**目的**: 定义患者选择事件

**位置**: `LYBT.Desktop.Infrastructure/Events/PatientSelectedEvent.cs`

**接口定义**:
```csharp
using Prism.Events;

namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 患者选择事件
    /// </summary>
    public class PatientSelectedEvent : PubSubEvent<PatientSelectedPayload>
    {
    }
}
```

**复用**:
- `PubSubEvent<T>`: Prism.Events 提供的事件基类

---

### 5.4 PatientSelectedPayload (Event Payload)

**目的**: 定义事件负载数据结构

**位置**: `LYBT.Desktop.Infrastructure/Events/PatientSelectedPayload.cs`

**接口定义**:
```csharp
namespace LYBT.Desktop.Infrastructure.Events
{
    /// <summary>
    /// 患者选择事件负载
    /// </summary>
    public class PatientSelectedPayload
    {
        /// <summary>
        /// 患者ID(核心标识)
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>
        /// 性别(男/女/未知)
        /// </summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// 上次就诊日期
        /// </summary>
        public DateTime? LastVisitDate { get; set; }

        /// <summary>
        /// 就诊次数
        /// </summary>
        public int VisitCount { get; set; }

        /// <summary>
        /// 过敏史(重要提醒)
        /// </summary>
        public string? AllergyHistory { get; set; }

        /// <summary>
        /// 选择时间戳
        /// </summary>
        public DateTime SelectedAt { get; set; } = DateTime.Now;
    }
}
```

---

## 6. Data Models

### 6.1 PatientItem (UI Model)

**目的**: ViewModel 绑定的 UI 数据模型

**位置**: `LYBT.Desktop.Patients/Models/PatientItem.cs` (已存在)

**结构**:
```csharp
public class PatientItem : BindableBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? IdCard { get; set; }
    public string? AllergyHistory { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int VisitCount { get; set; }
    public DateTime CreatedAt { get; set; }

    // 计算属性
    public string DisplayText => $"{Name} ({Gender}/{Age}岁)";
    public bool IsNewPatient => CreatedAt > DateTime.Now.AddDays(-30) && VisitCount <= 1;
}
```

**复用**: 直接使用现有的 PatientItem 模型,无需新增。

---

### 6.2 PatientDto (Data Transfer Object)

**目的**: Repository 返回的数据传输对象

**位置**: `LYBT.Shared.Models.Contracts.Patients/PatientDto.cs` (已存在)

**结构**: (与 PatientItem 类似,通过 AutoMapper 自动映射)

---

### 6.3 PatientCreateDto (Create Request)

**目的**: 创建患者的请求对象

**位置**: `LYBT.Shared.Models.Contracts.Patients/PatientCreateDto.cs` (已存在)

**结构**:
```csharp
public class PatientCreateDto
{
    [Required(ErrorMessage = "姓名不能为空")]
    [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "性别不能为空")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? IdCard { get; set; }
    public string? AllergyHistory { get; set; }
}
```

**复用**: 直接使用现有的 PatientCreateDto。

---

## 7. Error Handling

### 7.1 错误场景

#### Scenario 1: 搜索失败(网络异常)

**描述**: 调用 `IPatientRepository.SearchAsync()` 时发生网络错误

**处理**:
```csharp
try
{
    var dtos = await _patientRepository.SearchAsync(SearchKeyword);
}
catch (HttpRequestException ex)
{
    ErrorMessage = "网络连接失败,请检查网络设置";
    // 记录日志
}
catch (Exception ex)
{
    ErrorMessage = $"搜索失败: {ex.Message}";
}
```

**用户影响**:
- 搜索结果清空
- 显示红色错误提示: "网络连接失败,请检查网络设置"

---

#### Scenario 2: 创建患者失败(手机号重复)

**描述**: 调用 `IPatientRepository.CreateAsync()` 时,手机号已存在

**处理**:
```csharp
try
{
    var createdDto = await _patientRepository.CreateAsync(createDto);
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
{
    ErrorMessage = "手机号已存在,请直接搜索该患者";
}
catch (Exception ex)
{
    ErrorMessage = $"创建患者失败: {ex.Message}";
}
```

**用户影响**:
- 快速创建面板保持打开
- 显示错误提示: "手机号已存在,请直接搜索该患者"
- 建议用户切换到搜索模式

---

#### Scenario 3: 搜索关键词过短

**描述**: 用户输入少于2个字符

**处理**:
```csharp
if (string.IsNullOrWhiteSpace(SearchKeyword) || SearchKeyword.Length < 2)
{
    SearchResults.Clear();
    return;
}
```

**用户影响**:
- 搜索结果自动清空
- 无错误提示(正常行为)

---

#### Scenario 4: 必填项未填写

**描述**: 快速创建时,姓名/性别/手机号未填写

**处理**:
```csharp
private bool CanQuickCreate() =>
    !IsLoading &&
    !string.IsNullOrWhiteSpace(NewPatientName) &&
    !string.IsNullOrWhiteSpace(NewPatientGender) &&
    !string.IsNullOrWhiteSpace(NewPatientPhone);
```

**用户影响**:
- "创建"按钮禁用(灰色)
- 必填项标注红色星号(*)

---

## 8. Testing Strategy

### 8.1 Unit Testing

**测试对象**: `PatientSelectorViewModel`

**测试工具**:
- xUnit
- Moq (Mock IPatientRepository, IEventAggregator)
- AutoMapper (真实实例)

**关键测试用例**:

#### Test 1: SearchAsync 成功场景
```csharp
[Fact]
public async Task SearchAsync_ValidKeyword_ReturnsResults()
{
    // Arrange
    var mockRepo = new Mock<IPatientRepository>();
    mockRepo.Setup(r => r.SearchAsync("张三"))
        .ReturnsAsync(new List<PatientDto> { /* 测试数据 */ });

    var vm = new PatientSelectorViewModel(mockRepo.Object, ...);

    // Act
    vm.SearchKeyword = "张三";
    await Task.Delay(500); // 等待防抖

    // Assert
    Assert.NotEmpty(vm.SearchResults);
    Assert.Equal("张三", vm.SearchResults[0].Name);
}
```

---

#### Test 2: SelectPatient 发布事件
```csharp
[Fact]
public void SelectPatient_ValidPatient_PublishesEvent()
{
    // Arrange
    var mockEventAggregator = new Mock<IEventAggregator>();
    var mockEvent = new Mock<PatientSelectedEvent>();
    mockEventAggregator.Setup(ea => ea.GetEvent<PatientSelectedEvent>())
        .Returns(mockEvent.Object);

    var vm = new PatientSelectorViewModel(..., mockEventAggregator.Object, ...);
    var patient = new PatientItem { Id = Guid.NewGuid(), Name = "张三" };

    // Act
    vm.SelectPatientCommand.Execute(patient);

    // Assert
    mockEvent.Verify(e => e.Publish(It.Is<PatientSelectedPayload>(
        p => p.PatientId == patient.Id && p.PatientName == "张三"
    )), Times.Once);
}
```

---

#### Test 3: QuickCreate 成功场景
```csharp
[Fact]
public async Task QuickCreate_ValidData_CreatesAndSelectsPatient()
{
    // Arrange
    var mockRepo = new Mock<IPatientRepository>();
    mockRepo.Setup(r => r.CreateAsync(It.IsAny<PatientCreateDto>()))
        .ReturnsAsync(new PatientDto { Id = Guid.NewGuid(), Name = "李四" });

    var mockEventAggregator = new Mock<IEventAggregator>();
    var mockEvent = new Mock<PatientSelectedEvent>();
    mockEventAggregator.Setup(ea => ea.GetEvent<PatientSelectedEvent>())
        .Returns(mockEvent.Object);

    var vm = new PatientSelectorViewModel(mockRepo.Object, mockEventAggregator.Object, ...);
    vm.NewPatientName = "李四";
    vm.NewPatientGender = "男";
    vm.NewPatientPhone = "13800138000";

    // Act
    vm.QuickCreateCommand.Execute();
    await Task.Delay(500); // 等待异步完成

    // Assert
    mockRepo.Verify(r => r.CreateAsync(It.Is<PatientCreateDto>(
        dto => dto.Name == "李四" && dto.PhoneNumber == "13800138000"
    )), Times.Once);
    mockEvent.Verify(e => e.Publish(It.IsAny<PatientSelectedPayload>()), Times.Once);
}
```

---

#### Test 4: 搜索失败错误处理
```csharp
[Fact]
public async Task SearchAsync_NetworkError_SetsErrorMessage()
{
    // Arrange
    var mockRepo = new Mock<IPatientRepository>();
    mockRepo.Setup(r => r.SearchAsync(It.IsAny<string>()))
        .ThrowsAsync(new HttpRequestException("网络连接失败"));

    var vm = new PatientSelectorViewModel(mockRepo.Object, ...);

    // Act
    vm.SearchKeyword = "张三";
    await Task.Delay(500);

    // Assert
    Assert.NotNull(vm.ErrorMessage);
    Assert.Contains("网络", vm.ErrorMessage);
}
```

---

### 8.2 Integration Testing

**测试对象**: `PatientSelectorControl` + `PatientSelectorViewModel` + `PatientRepository`

**测试工具**:
- WPF UI Automation (或手动测试)
- 真实 Repository (指向测试环境 API)

**关键测试流程**:

#### Test 1: 端到端搜索流程
1. 输入搜索关键词 "张"
2. 等待300ms(性能要求)
3. 验证搜索结果列表显示
4. 点击第一个结果
5. 验证事件发布

---

#### Test 2: 端到端快速创建流程
1. 点击"快速创建"按钮
2. 填写姓名/性别/手机号
3. 点击"创建"按钮
4. 等待1s(性能要求)
5. 验证创建成功且自动选中

---

### 8.3 End-to-End Testing

**测试对象**: 临床工作台 + 患者选择器组件

**测试场景**:

#### Scenario 1: 新患者首诊完整流程
1. 打开临床工作台
2. 搜索患者 "新患者"
3. 搜索结果为空
4. 使用快速创建功能创建新患者
5. 验证工作台接收到 PatientSelectedEvent
6. 验证患者信息卡显示正确
7. 验证"新建病案"按钮启用

---

#### Scenario 2: 复诊患者流程
1. 打开临床工作台
2. 搜索患者 "张三"
3. 选择搜索结果中的第一个
4. 验证工作台加载历史病案列表
5. 验证患者信息卡显示正确(含就诊次数)

---

## 9. Performance Optimization

### 9.1 搜索防抖 (Debounce)

**问题**: 用户每输入一个字符就触发一次搜索,导致大量无效请求

**解决方案**: 使用防抖机制,输入停止300ms后才触发搜索

```csharp
private CancellationTokenSource? _searchCts;

private void OnSearchKeywordChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName != nameof(SearchKeyword)) return;

    // 取消之前的搜索
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();

    Task.Delay(300, _searchCts.Token)
        .ContinueWith(task =>
        {
            if (!task.IsCanceled)
            {
                Application.Current.Dispatcher.Invoke(() => ExecuteSearchAsync());
            }
        });
}
```

---

### 9.2 搜索结果缓存

**问题**: 用户可能多次搜索相同关键词

**解决方案**: 使用 LRU 缓存(最多保留20条)

```csharp
private readonly Dictionary<string, List<PatientItem>> _searchCache = new();
private readonly Queue<string> _cacheKeys = new();

private async Task<List<PatientItem>> SearchWithCacheAsync(string keyword)
{
    if (_searchCache.TryGetValue(keyword, out var cachedResult))
    {
        return cachedResult;
    }

    var dtos = await _patientRepository.SearchAsync(keyword);
    var items = _mapper.Map<List<PatientItem>>(dtos);

    // 添加到缓存
    _searchCache[keyword] = items;
    _cacheKeys.Enqueue(keyword);

    // LRU淘汰
    if (_cacheKeys.Count > 20)
    {
        var oldest = _cacheKeys.Dequeue();
        _searchCache.Remove(oldest);
    }

    return items;
}
```

---

### 9.3 UI 虚拟化

**问题**: 搜索结果可能返回100+条记录,影响渲染性能

**解决方案**: 使用 VirtualizingStackPanel

```xml
<ListBox ItemsSource="{Binding SearchResults}"
         VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling">
    <!-- ... -->
</ListBox>
```

---

## 10. Deployment and Configuration

### 10.1 依赖注入配置

**位置**: 使用该组件的模块(如 ClinicalWorkbench)需要注册依赖

```csharp
// 在模块的 RegisterTypes 方法中
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册 ViewModel
    containerRegistry.Register<PatientSelectorViewModel>();

    // 注册 View (如果需要通过 DI 解析)
    containerRegistry.Register<PatientSelectorControl>();

    // 确保依赖项已注册
    // - IPatientRepository (应该在 PatientsModule 中已注册)
    // - IEventAggregator (Prism 自动注册)
    // - IMapper (Infrastructure 中注册)
}
```

---

### 10.2 AutoMapper 配置

**位置**: `LYBT.Desktop.Infrastructure/Mapping/PatientMappingProfile.cs` (需新增)

```csharp
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // PatientDto → PatientItem
        CreateMap<PatientDto, PatientItem>();

        // PatientItem → PatientSelectedPayload
        CreateMap<PatientItem, PatientSelectedPayload>()
            .ForMember(dest => dest.SelectedAt, opt => opt.MapFrom(_ => DateTime.Now));
    }
}
```

---

## 11. Security Considerations

### 11.1 输入验证

- **搜索关键词**: 限制长度 ≤50 字符,禁止 SQL 注入字符
- **快速创建**: 使用 `DataAnnotations` 验证(PatientCreateDto)
- **手机号验证**: 使用正则表达式验证格式

### 11.2 权限控制

- MVP 阶段不考虑权限
- Phase 2: 可通过 DependencyProperty 传入 `AllowQuickCreate` 控制是否显示快速创建功能

---

## 12. Future Enhancements (Phase 2+)

### Phase 2: 增强功能
- **高级搜索**: 支持身份证号、地址搜索
- **搜索历史**: 记住最近10次搜索关键词
- **患者标签**: 显示患者标签(VIP、慢性病等)
- **拼音搜索**: 支持拼音首字母搜索(如 "zs" 匹配 "张三")

### Phase 3: 智能化
- **智能推荐**: 根据历史就诊频率排序
- **重复检测**: 创建前提示可能重复的患者
- **OCR识别**: 扫描身份证自动填充信息

---

## 13. 相关文档

- **需求文档**: `.spec-workflow/specs/patient-selector/requirements.md`
- **任务分解**: `.spec-workflow/specs/patient-selector/tasks.md` (待创建)
- **架构标准**: `docs/architecture/client/unified-design-standard.md`
- **编码规范**: `docs/development/standards.md`
- **MVVM指南**: `docs/architecture/client/mvvm-best-practices.md` (如果存在)

---

**文档结束**

_此文档将提交Dashboard审批,审批通过后进入任务分解阶段。_

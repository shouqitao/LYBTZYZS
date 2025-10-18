# PatientSelector 组件

## 📋 组件概览

`PatientSelector` 是一个可复用的 WPF UserControl，用于患者搜索、选择和快速创建功能。该组件采用 MVVM 架构，基于 Prism 事件聚合器实现松耦合通信。

**主要功能：**
- ✅ 患者关键字搜索（支持防抖）
- ✅ 搜索结果列表展示
- ✅ 患者选择并发布事件
- ✅ 快速创建新患者
- ✅ 输入验证（姓名、性别、手机号）
- ✅ 加载状态和错误提示
- ✅ MaterialDesign 风格 UI

**架构特性：**
- **层级位置**：Presentation 层公共组件
- **通信机制**：Prism EventAggregator（`PatientSelectedEvent`）
- **映射策略**：反射手动映射（避免循环依赖）
- **性能优化**：VirtualizingStackPanel、搜索防抖（300ms）

---

## 🚀 快速开始

### 1. XAML 嵌入示例

在任意 WPF 视图中嵌入 PatientSelector：

```xml
<Window x:Class="YourNamespace.YourView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:ps="clr-namespace:LYBT.Desktop.Presentation.Components.PatientSelector;assembly=LYBT.Desktop.Presentation"
        Title="患者管理" Height="600" Width="800">

    <Grid>
        <!-- 嵌入 PatientSelector 组件 -->
        <ps:PatientSelectorControl />
    </Grid>
</Window>
```

### 2. PatientSelectedEvent 订阅示例

在需要接收患者选择事件的 ViewModel 中订阅：

```csharp
using LYBT.Desktop.Infrastructure.Events;
using Prism.Events;

public class YourViewModel : BindableBase
{
    private readonly IEventAggregator _eventAggregator;

    public YourViewModel(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;

        // 订阅患者选择事件
        _eventAggregator.GetEvent<PatientSelectedEvent>()
            .Subscribe(OnPatientSelected);
    }

    private void OnPatientSelected(PatientSelectedPayload payload)
    {
        // 处理患者选择事件
        Console.WriteLine($"选中患者: {payload.PatientName}");
        Console.WriteLine($"患者ID: {payload.PatientId}");
        Console.WriteLine($"性别: {payload.Gender}");
        Console.WriteLine($"年龄: {payload.Age}");
        Console.WriteLine($"手机号: {payload.PhoneNumber}");

        // 示例：自动填充表单
        PatientId = payload.PatientId;
        PatientName = payload.PatientName;
    }
}
```

---

## 📚 API 参考

### PatientSelectorViewModel 属性

| 属性名 | 类型 | 描述 |
|-------|------|------|
| `SearchKeyword` | `string` | 搜索关键字（支持双向绑定） |
| `SearchResults` | `ObservableCollection<dynamic>` | 搜索结果列表 |
| `SelectedPatient` | `dynamic` | 当前选中的患者 |
| `HasNoResults` | `bool` | 是否无搜索结果（计算属性） |
| `IsLoading` | `bool` | 是否正在加载 |
| `HasError` | `bool` | 是否有错误（计算属性） |
| `ErrorMessage` | `string` | 错误消息 |
| `ShowQuickCreate` | `bool` | 是否显示快速创建面板 |
| `NewPatientName` | `string` | 新患者姓名（创建时使用） |
| `NewPatientGender` | `string` | 新患者性别（创建时使用） |
| `NewPatientPhone` | `string` | 新患者手机号（创建时使用） |

### PatientSelectorViewModel 命令

| 命令名 | CanExecute 条件 | 功能描述 |
|-------|----------------|---------|
| `SearchCommand` | `true` | 执行患者搜索（带防抖） |
| `SelectPatientCommand` | `patient != null` | 选择患者并发布事件 |
| `ToggleQuickCreateCommand` | `true` | 切换快速创建面板显示 |
| `QuickCreateCommand` | 姓名、性别非空 且 手机号≥6位 | 创建新患者并发布事件 |
| `ClearSearchCommand` | `!string.IsNullOrWhiteSpace(SearchKeyword)` | 清空搜索关键字 |

### PatientSelectedPayload 结构

```csharp
public class PatientSelectedPayload
{
    public Guid PatientId { get; set; }           // 患者ID（新创建时为 Guid.NewGuid()）
    public string PatientName { get; set; }       // 患者姓名
    public string? Gender { get; set; }           // 性别
    public int? Age { get; set; }                 // 年龄
    public string? PhoneNumber { get; set; }      // 手机号
}
```

---

## 🎯 常见使用场景

### 场景1：门诊接诊流程

**需求**：医生开始接诊时，需要选择或创建患者。

**实现**：
```xml
<!-- 接诊页面 -->
<Grid>
    <ps:PatientSelectorControl />
</Grid>
```

```csharp
// ConsultationViewModel.cs
public class ConsultationViewModel : BindableBase
{
    public ConsultationViewModel(IEventAggregator eventAggregator)
    {
        eventAggregator.GetEvent<PatientSelectedEvent>()
            .Subscribe(patient =>
            {
                // 自动填充患者信息到接诊单
                CurrentPatient = patient;
                StartConsultation();
            });
    }
}
```

---

### 场景2：病历创建

**需求**：创建病历时需要关联患者。

**实现**：
```csharp
// MedicalRecordViewModel.cs
eventAggregator.GetEvent<PatientSelectedEvent>()
    .Subscribe(patient =>
    {
        MedicalRecord.PatientId = patient.PatientId;
        MedicalRecord.PatientName = patient.PatientName;
    });
```

---

### 场景3：快速建档（新患者）

**操作步骤**：
1. 用户点击"快速创建"按钮
2. 展开创建面板
3. 填写姓名、性别、手机号
4. 点击"创建患者"
5. 系统自动发布 `PatientSelectedEvent`（PatientId 为新生成的 GUID）
6. 订阅方接收事件，完成后续流程

**验证规则**：
- 姓名：不能为空
- 性别：不能为空
- 手机号：长度必须 ≥ 6 位

---

## 🔧 故障排除

### 问题1：事件未触发

**症状**：选择患者后，订阅方未收到 `PatientSelectedEvent`。

**排查步骤**：
1. 检查订阅方是否正确订阅事件：
   ```csharp
   _eventAggregator.GetEvent<PatientSelectedEvent>().Subscribe(OnPatientSelected);
   ```
2. 检查事件聚合器实例是否为同一个（DI 应配置为单例）
3. 在 `OnPatientSelected` 方法中设置断点，确认是否被调用
4. 检查事件发布代码是否正确：
   ```csharp
   _eventAggregator.GetEvent<PatientSelectedEvent>().Publish(payload);
   ```

---

### 问题2：搜索功能无响应

**症状**：输入搜索关键字后无搜索结果。

**排查步骤**：
1. 检查 `SearchKeyword` 是否正确绑定到 UI
2. 确认防抖延迟（300ms）已过
3. 检查 `SearchCommand` 是否被正确触发
4. 查看 `ErrorMessage` 属性是否有错误信息
5. 检查后台日志（如有集成实际搜索服务）

**临时解决方案**：
```csharp
// 手动触发搜索（绕过防抖）
viewModel.SearchCommand.Execute();
```

---

### 问题3：快速创建按钮禁用

**症状**：填写信息后"创建患者"按钮仍为灰色。

**原因**：未满足 `QuickCreateCommand.CanExecute` 条件。

**检查清单**：
- [ ] `NewPatientName` 是否非空？
- [ ] `NewPatientGender` 是否非空？
- [ ] `NewPatientPhone` 长度是否 ≥ 6 位？

**调试代码**：
```csharp
// 在 ViewModel 中添加临时调试
Console.WriteLine($"Name: {NewPatientName}, Gender: {NewPatientGender}, Phone: {NewPatientPhone}");
Console.WriteLine($"CanExecute: {QuickCreateCommand.CanExecute()}");
```

---

### 问题4：性能问题（搜索结果多时卡顿）

**症状**：搜索结果超过 100 条时滚动卡顿。

**已实现优化**：
- ✅ XAML 中使用 `VirtualizingStackPanel.IsVirtualizing="True"`
- ✅ `ScrollViewer.CanContentScroll="True"`（启用虚拟化）
- ✅ 搜索防抖 300ms

**进一步优化建议**：
1. 后端分页加载（每次仅返回前 50 条）
2. 实现滚动加载更多（Infinite Scroll）
3. 优化数据模板复杂度

---

### 问题5：循环依赖错误

**症状**：编译时报错 `CS0234: 命名空间"LYBT.Desktop"中不存在类型或命名空间名"Modules"`。

**原因**：Presentation 层不能引用 Modules 层（会导致循环依赖）。

**解决方案**：
- ✅ 已采用反射手动映射（见 `PatientSelectorViewModel.CreatePatientSelectedPayload()`）
- ❌ 不要尝试使用 AutoMapper 映射 Modules 层类型
- ✅ 使用 `dynamic` 类型或基类接口作为中间层

**正确的映射代码示例**：
```csharp
// PatientSelectorViewModel.cs
private PatientSelectedPayload CreatePatientSelectedPayload(dynamic patient)
{
    return new PatientSelectedPayload
    {
        PatientId = patient.GetType().GetProperty("Id")?.GetValue(patient) ?? Guid.Empty,
        PatientName = patient.GetType().GetProperty("Name")?.GetValue(patient)?.ToString() ?? "",
        Gender = patient.GetType().GetProperty("Gender")?.GetValue(patient)?.ToString(),
        Age = patient.GetType().GetProperty("Age")?.GetValue(patient) as int?,
        PhoneNumber = patient.GetType().GetProperty("PhoneNumber")?.GetValue(patient)?.ToString()
    };
}
```

---

## 🧪 测试覆盖

### 单元测试
- 测试文件：`tests/UnitTests/Client/Desktop/LYBT.Desktop.PatientSelector.Tests/ViewModels/PatientSelectorViewModelTests.cs`
- 测试用例：20 个
- 覆盖率：核心逻辑 100%

### 集成测试
- 测试文件：`tests/IntegrationTests/Client/Desktop/LYBT.Desktop.PatientSelector.IntegrationTests/PatientSelectorIntegrationTests.cs`
- 测试用例：7 个
- 测试场景：
  - ✅ 初始化正确性
  - ✅ 搜索和选择工作流
  - ✅ 快速创建工作流
  - ✅ 输入验证
  - ✅ 错误状态处理
  - ✅ 加载状态
  - ✅ 无结果状态

---

## 📖 相关文档

- [Spec 需求文档](/.spec-workflow/specs/patient-selector/requirements.md)
- [Spec 设计文档](/.spec-workflow/specs/patient-selector/design.md)
- [Spec 任务清单](/.spec-workflow/specs/patient-selector/tasks.md)
- [Client 端架构指南](/docs/architecture/client/README.md)
- [Presentation 层开发规范](/docs/development/client/README.md)

---

## 🔗 依赖项

| 依赖项 | 用途 |
|-------|------|
| `Prism.Events` | 事件聚合器 |
| `LYBT.Desktop.Infrastructure.Events` | PatientSelectedEvent 定义 |
| `MaterialDesignThemes` | UI 样式 |
| `System.Reflection` | 动态对象映射 |

---

## 📝 版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| v1.0.0 | 2025-10-18 | 初始版本，实现基础搜索、选择、创建功能 |

---

**维护人员**：Desktop 开发团队
**最后更新**：2025-10-18

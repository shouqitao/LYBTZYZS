# WPF MVVM 事件副作用问题排查指南

> **问题类型**: WPF MVVM事件驱动
> **技术栈**: WPF, Prism, MVVM
> **难度等级**: 中级

## 问题现象

### 数据加载后显示位置异常

- 加载数据后，项目显示在错误的位置
- 集合中出现意外的空项
- UI显示与数据模型不一致

### 典型症状

```
预期: [药材1, 药材2, 空, 空, 空]
实际: [空, 空, 空, 空, 药材1, 药材2]
```

## 根因分析

### PropertyChanged事件触发时机问题

在WPF MVVM中，`PropertyChanged`事件可能在数据加载完成前触发副作用操作:

```csharp
// 问题代码
private void LoadFromDto(PrescriptionDto dto)
{
    foreach (var item in dto.Items)
    {
        var herbItem = CreateHerbItem();
        herbItem.HerbId = item.HerbId;  // 触发PropertyChanged
        // PropertyChanged → EnsureMinimumBlankRows() → 添加空槽位
        HerbItems.Add(herbItem);  // 药材被添加到空槽位之后
    }
}
```

### Bug触发流程

```
LoadFromDto()
    → CreateHerbItem()
    → 设置 HerbId
    → PropertyChanged 触发
    → EnsureMinimumBlankRows() 添加4个空槽位
    → HerbItems.Add(herb)
    → 结果: [空, 空, 空, 空, 药材]
```

## 解决方案

### 方案1: 加载标志模式 (推荐)

使用标志位隔离加载期间的事件副作用:

```csharp
private bool _isLoadingData;

private void LoadFromDto(PrescriptionDto dto)
{
    _isLoadingData = true;
    try
    {
        HerbItems.Clear();

        foreach (var item in dto.Items)
        {
            var herbItem = CreateHerbItem();
            herbItem.HerbId = item.HerbId;  // PropertyChanged触发但被跳过
            HerbItems.Add(herbItem);
        }

        EnsureMinimumBlankRows();  // 在所有数据加载完成后执行
    }
    finally
    {
        _isLoadingData = false;
    }
}

private HerbItemViewModel CreateHerbItem()
{
    var item = new HerbItemViewModel();
    item.PropertyChanged += (s, e) =>
    {
        if (_isLoadingData) return;  // 加载期间跳过

        if (e.PropertyName == nameof(HerbItemViewModel.HerbId))
        {
            EnsureMinimumBlankRows();
        }
    };
    return item;
}
```

### 方案2: 批量更新模式

使用批量更新避免中间状态:

```csharp
private void LoadFromDto(PrescriptionDto dto)
{
    // 先准备所有数据
    var items = dto.Items.Select(CreateHerbItemFromDto).ToList();

    // 补充空槽位
    while (items.Count < MinimumRows)
    {
        items.Add(CreateEmptyHerbItem());
    }

    // 一次性替换集合
    HerbItems = new ObservableCollection<HerbItemViewModel>(items);
}
```

### 方案3: 延迟订阅模式

延迟事件订阅到数据加载完成后:

```csharp
private void LoadFromDto(PrescriptionDto dto)
{
    var items = new List<HerbItemViewModel>();

    foreach (var item in dto.Items)
    {
        var herbItem = new HerbItemViewModel();
        herbItem.HerbId = item.HerbId;  // 此时无事件订阅
        items.Add(herbItem);
    }

    // 数据加载完成后再订阅事件
    foreach (var item in items)
    {
        SubscribeToPropertyChanged(item);
        HerbItems.Add(item);
    }

    EnsureMinimumBlankRows();
}
```

## 修复后流程

```
LoadFromDto() (_isLoadingData=true)
    → CreateHerbItem()
    → 设置 HerbId
    → PropertyChanged 触发 → 检测到 _isLoadingData=true → 跳过
    → HerbItems.Add(herb)
    → 循环完成
    → EnsureMinimumBlankRows()
    → finally (_isLoadingData=false)
    → 结果: [药材, 空, 空, 空, 空]
```

## 预防措施

### 1. ViewModel初始化规范

```csharp
public async Task InitializeAsync(Dto dto)
{
    _isLoadingData = true;
    try
    {
        // 重置状态
        ResetState();

        // 加载数据
        LoadFromDto(dto);

        // 初始化完成后的操作
        OnInitializationComplete();
    }
    finally
    {
        _isLoadingData = false;
    }
}
```

### 2. 事件处理守卫

所有PropertyChanged处理程序都应检查加载状态:

```csharp
item.PropertyChanged += (s, e) =>
{
    // 守卫条件
    if (_isLoadingData) return;
    if (_isDisposing) return;

    // 业务逻辑
    HandlePropertyChanged(s, e);
};
```

### 3. 代码审查检查点

- [ ] 数据加载方法是否设置了加载标志
- [ ] PropertyChanged处理程序是否检查加载状态
- [ ] 批量操作是否在加载完成后执行
- [ ] finally块是否正确重置标志

## 排查清单

- [ ] 检查PropertyChanged事件的订阅时机
- [ ] 检查是否有副作用操作在数据加载期间执行
- [ ] 检查集合操作的顺序是否正确
- [ ] 检查ViewModel复用时是否正确重置状态
- [ ] 使用调试器单步跟踪事件触发顺序

## 相关报告

- [医案工作区问题修复反思报告](../../reports/medicalcase-workspace-bug-reflection-2025-11-29.md)

## 参考资料

- [WPF Data Binding Overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/)
- [INotifyPropertyChanged Best Practices](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged)

---

**文档类型**: Troubleshooting Guide
**更新时间**: 2025-11-29
**维护团队**: 架构组

# Design: 重构MasterDetail编辑模式

## Context

MasterDetail模式是本项目Desktop端的核心交互模式，用于Users、Patients、Herbs、Formula、MedicalCase五个模块。当前实现存在代码冗余和模式不一致问题，且有一个P0级别的Bug导致新建功能失效。

**利益相关者**: 前端开发、产品验收
**约束条件**: Pre-Release阶段，必须保证功能稳定性

## Goals / Non-Goals

**Goals**:
- 修复新建保存失败的P0 Bug
- 消除Edit属性冗余代码（预计减少500+行）
- 统一为单一编辑模式

**Non-Goals**:
- 不改变用户交互流程
- 不修改后端API
- 不引入新的MVVM框架或依赖

## Decisions

### Decision 1: 使用标志位修复P0 Bug

**选择**: 添加`_isCreatingNew`标志位，在RefreshCanExecuteChanged中检查

**理由**: 
- 最小侵入性修改
- 不改变现有执行流程
- 易于理解和维护

**备选方案**:
- A) 调整执行顺序 - 风险较高，可能引入其他问题
- B) 使用async/await延迟 - 不够可靠，依赖时序

```csharp
// 修复方案
private bool _isCreatingNew;

protected override async Task OnExecuteAddAsync()
{
    _isCreatingNew = true;
    try
    {
        var newDetail = CreateNewDetail();
        RunOnUIThread(() =>
        {
            SelectedItem = null;  // 先清空选中项
            CurrentDetail = newDetail;  // 后设置详情
            IsEditMode = true;
            HasUnsavedChanges = false;
        });
    }
    finally
    {
        _isCreatingNew = false;
    }
    await Task.CompletedTask;
}

protected override void RefreshCanExecuteChanged()
{
    base.RefreshCanExecuteChanged();
    if (SelectedItem != null)
    {
        SafeFireAndForgetLoadDetail();
    }
    else if (!_isCreatingNew)  // 关键：创建新项时不清空
    {
        CancelLoadDetail();
        CurrentDetail = null;
        IsEditMode = false;
    }
}
```

### Decision 2: 统一使用CurrentDetail直接绑定

**选择**: 移除所有Edit属性，XAML直接绑定`CurrentDetail.PropertyName`

**理由**:
- Formula模块已验证此模式可行
- 消除200-300行/模块的冗余代码
- 简化数据流，减少同步错误

**当前冗余模式** (Users为例):
```csharp
// 7个冗余属性
private string _editUserName = string.Empty;
public string EditUserName { get => _editUserName; set => SetProperty(ref _editUserName, value); }
// ... 重复6次

// 清空方法
private void ClearEditProperties()
{
    EditUserName = string.Empty;
    // ... 重复6次
}

// 保存时从Edit属性取值
var dto = new UserInputDto { UserName = EditUserName, ... };
```

**统一模式** (参考Formula):
```csharp
// 直接使用CurrentDetail，无需Edit属性
// XAML: Text="{Binding CurrentDetail.UserName, Mode=TwoWay}"

// 保存时直接使用CurrentDetail
var dto = new UserInputDto { UserName = CurrentDetail.UserName, ... };
```

### Decision 3: DetailModel必须支持INotifyPropertyChanged

**要求**: 所有DetailModel必须继承BindableBase或实现INPC

**理由**: 直接绑定模式要求DetailModel属性变更能触发UI更新

**验证**: 当前所有DetailModel已继承BindableBase，无需额外修改

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| XAML绑定修改可能遗漏 | 编译时验证 + 运行时测试 |
| Clone逻辑变更影响取消功能 | 保留_originalDetail机制 |
| 回归风险 | 逐模块修改并验证 |

## Migration Plan

1. **Phase 1**: 修复P0 Bug (独立可交付)
2. **Phase 2-4**: 逐模块迁移 (Users → Patients → Herbs)
3. **Phase 5**: 验证与文档更新

**回滚方案**: 每个Phase独立提交，可按需回滚单个模块

## Open Questions

1. ~~Edit属性是否有其他用途?~~ → 已确认仅用于编辑绑定
2. ~~Formula模式是否稳定?~~ → 已验证可行

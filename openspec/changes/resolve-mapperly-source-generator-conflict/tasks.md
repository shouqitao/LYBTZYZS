# Tasks: resolve-mapperly-source-generator-conflict

## Phase 1: Users模块

- [ ] 1.1 重构`UserItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 1.2 恢复`UserMapper.cs`为Mapperly partial方法
- [ ] 1.3 删除`UserItem.cs`中的`[Obsolete]`映射方法
- [ ] 1.4 验证Users模块编译无警告

## Phase 2: Patients模块

- [ ] 2.1 重构`PatientItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 2.2 恢复`PatientMapper.cs`为Mapperly partial方法
- [ ] 2.3 删除`PatientItem.cs`中的`[Obsolete]`映射方法
- [ ] 2.4 验证Patients模块编译无警告

## Phase 3: Formula模块

- [ ] 3.1 重构`FormulaItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 3.2 重构`FormulaHerbItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 3.3 恢复`FormulaMapper.cs`为Mapperly partial方法
- [ ] 3.4 恢复`FormulaHerbItemMapper.cs`为Mapperly partial方法
- [ ] 3.5 删除Formula Item类中的`[Obsolete]`映射方法
- [ ] 3.6 验证Formula模块编译无警告

## Phase 4: Consultation模块

- [ ] 4.1 重构`ConsultationItem.cs`（Consultation模块）：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 4.2 恢复`ConsultationMapper.cs`为Mapperly partial方法
- [ ] 4.3 删除ConsultationItem中的`[Obsolete]`映射方法
- [ ] 4.4 验证Consultation模块编译无警告

## Phase 5: MedicalCase模块

- [ ] 5.1 重构`ConsultationItem.cs`（MedicalCase模块）：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 5.2 重构`PrescriptionItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 5.3 重构`PrescriptionHerbItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 5.4 重构`MedicalCaseItem.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 5.5 恢复MedicalCase模块所有Mapper为Mapperly partial方法
- [ ] 5.6 删除MedicalCase Item类中的`[Obsolete]`映射方法
- [ ] 5.7 验证MedicalCase模块编译无警告

## Phase 6: Herbs模块

- [ ] 6.1 重构`HerbItemDto.cs`：从`ObservableObject + [ObservableProperty]`改为`BindableBase + 显式属性`
- [ ] 6.2 恢复`HerbMapper.cs`为Mapperly partial方法（如存在）
- [ ] 6.3 验证Herbs模块编译无警告

## Phase 7: 最终验证

- [ ] 7.1 全量编译验证：0错误0警告
- [ ] 7.2 验证Mapperly生成的映射代码正确（检查obj/generated目录）
- [ ] 7.3 运行单元测试验证映射功能
- [ ] 7.4 运行应用验证UI绑定正常

## 重构模式说明

### 从ObservableProperty到显式属性的转换

**转换前**：
```csharp
public partial class UserItem : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _userName = string.Empty;
}
```

**转换后**：
```csharp
public class UserItem : BindableBase
{
    private Guid _id;
    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _userName = string.Empty;
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }
}
```

### 注意事项

1. **移除`partial`关键字**：BindableBase不需要partial
2. **移除`[ObservableProperty]`特性**：改为显式属性定义
3. **移除`[NotifyPropertyChangedFor]`**：改为在set中手动调用`RaisePropertyChanged`
4. **保留计算属性**：如`DisplayText`、`IsActive`等保持只读属性
5. **保留业务方法**：如`UpdateFromDto`、`Clear`等方法保留
6. **删除`[Obsolete]`方法**：移除`FromDto()`、`ToDto()`、`ToInputDto()`

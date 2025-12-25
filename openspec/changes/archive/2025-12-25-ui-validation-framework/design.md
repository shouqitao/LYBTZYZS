# UI层数据验证框架 - 技术设计

## 现状分析

### 已有基础设施

```csharp
// ViewModelBase 已实现 INotifyDataErrorInfo
public abstract class ViewModelBase : BindableBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _validationErrors = new();

    // 验证错误访问器 - 支持XAML索引器绑定
    public ValidationErrorsAccessor Errors { get; }
    public ValidationHasErrorsAccessor HasErrorsDictionary { get; }

    // 验证方法
    protected void AddValidationError(string propertyName, string errorMessage);
    protected void ClearValidationErrors(string? propertyName = null);
}

// UnifiedViewModelBase 添加 DataAnnotations 验证
protected virtual void ValidateProperty([CallerMemberName] string? propertyName = null)
{
    ClearValidationErrors(propertyName);
    var validationResults = new List<ValidationResult>();
    if (!Validator.TryValidateProperty(value, context, validationResults))
        foreach (var result in validationResults)
            AddValidationError(propertyName, result.ErrorMessage);
}
```

### 当前问题

1. **XAML绑定未启用验证**
   ```xml
   <!-- 当前写法 - 无验证 -->
   <TextBox Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"/>
   ```

2. **无错误消息显示**
   - 输入框下方无错误提示区域
   - 用户不知道验证失败原因

3. **必填标识不明显**
   - 仅用文本 `*` 标识，视觉效果弱
   - 无统一样式

4. **DetailModel无验证支持**
   - MasterDetail模式中DetailModel继承BindableBase
   - 无INotifyDataErrorInfo支持

---

## 技术方案

### 架构设计

```
┌─────────────────────────────────────────────────────────────────┐
│                        UI Layer (XAML)                          │
├─────────────────────────────────────────────────────────────────┤
│  ValidatingTextBox       ValidatingComboBox       FormField     │
│  ├── TextBox             ├── ComboBox             ├── Label     │
│  ├── ErrorBorder         ├── ErrorBorder         ├── Required   │
│  └── ErrorMessage        └── ErrorMessage        └── Input      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ViewModel Layer                               │
├─────────────────────────────────────────────────────────────────┤
│  DetailModel (extends ValidatableModelBase)                      │
│  ├── INotifyDataErrorInfo                                       │
│  ├── DataAnnotations [Required], [StringLength], [Range]        │
│  └── ValidateProperty() on PropertyChanged                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Validation Rules                              │
├─────────────────────────────────────────────────────────────────┤
│  ValidationConstants (统一常量)                                  │
│  ├── StringLength limits                                        │
│  ├── Range constraints                                          │
│  └── Error messages                                             │
└─────────────────────────────────────────────────────────────────┘
```

---

### 组件设计

#### 1. ValidatableModelBase - 可验证模型基类

```csharp
/// <summary>
/// 可验证模型基类 - 为DetailModel提供验证支持
/// OpenSpec: ui-validation-framework
/// </summary>
public abstract class ValidatableModelBase : BindableBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _validationErrors = new();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
    public bool HasErrors => _validationErrors.Any();

    public IEnumerable GetErrors(string? propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? _validationErrors.SelectMany(x => x.Value)
            : _validationErrors.TryGetValue(propertyName, out var errors)
                ? errors
                : Enumerable.Empty<string>();

    /// <summary>验证错误访问器 - 支持XAML索引器绑定</summary>
    public ValidationErrorsAccessor Errors { get; }

    /// <summary>属性错误状态访问器</summary>
    public ValidationHasErrorsAccessor HasErrorsDictionary { get; }

    protected ValidatableModelBase()
    {
        Errors = new ValidationErrorsAccessor(_validationErrors);
        HasErrorsDictionary = new ValidationHasErrorsAccessor(_validationErrors);
    }

    /// <summary>设置属性并验证</summary>
    protected bool SetPropertyAndValidate<T>(ref T storage, T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref storage, value, propertyName)) return false;
        ValidateProperty(propertyName);
        return true;
    }

    /// <summary>验证属性</summary>
    protected virtual void ValidateProperty([CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName)) return;
        ClearValidationErrors(propertyName);

        var property = GetType().GetProperty(propertyName);
        if (property == null) return;

        var value = property.GetValue(this);
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(this) { MemberName = propertyName };

        if (!Validator.TryValidateProperty(value, context, validationResults))
        {
            foreach (var result in validationResults)
                AddValidationError(propertyName, result.ErrorMessage ?? "验证失败");
        }
    }

    /// <summary>验证所有属性</summary>
    public virtual bool ValidateAll()
    {
        var properties = GetType().GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(ValidationAttribute), true).Any());

        foreach (var property in properties)
            ValidateProperty(property.Name);

        return !HasErrors;
    }

    protected void AddValidationError(string propertyName, string errorMessage)
    {
        if (!_validationErrors.TryGetValue(propertyName, out var errors))
        {
            errors = new List<string>();
            _validationErrors[propertyName] = errors;
        }
        if (!errors.Contains(errorMessage))
        {
            errors.Add(errorMessage);
            OnErrorsChanged(propertyName);
            RaisePropertyChanged(nameof(HasErrors));
        }
    }

    protected void ClearValidationErrors(string? propertyName = null)
    {
        if (propertyName == null)
        {
            var names = _validationErrors.Keys.ToList();
            _validationErrors.Clear();
            foreach (var name in names) OnErrorsChanged(name);
        }
        else if (_validationErrors.ContainsKey(propertyName))
        {
            _validationErrors.Remove(propertyName);
            OnErrorsChanged(propertyName);
        }
        RaisePropertyChanged(nameof(HasErrors));
    }

    protected virtual void OnErrorsChanged(string propertyName) =>
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
}
```

---

#### 2. ValidationConstants - 使用现有验证常量

**注意**: 项目已有完整的ValidationConstants类，直接复用：

```csharp
// 位置: LYBT.Shared.Primitives.Validation.ValidationConstants
// 已包含：长度限制、数值范围、正则表达式、错误消息模板

// 使用示例：
using LYBT.Shared.Primitives.Validation;

[Required(ErrorMessage = ValidationConstants.RequiredErrorMessage)]
[StringLength(ValidationConstants.NameMaxLength)]
public string Name { get; set; }

[Range(ValidationConstants.AgeMinValue, ValidationConstants.AgeMaxValue)]
public int Age { get; set; }
```

**现有常量概览**:
- 长度限制: `NameMaxLength`, `RemarkMaxLength`, `PhoneMaxLength`, `IdCardMaxLength`等
- 数值范围: `AgeMinValue/MaxValue`, `PriceMinValue/MaxValue`, `HerbDosageMinValue/MaxValue`等
- 错误消息: `RequiredErrorMessage`, `MaxLengthErrorMessage`, `RangeErrorMessage`等
- 正则表达式: `IdCardRegex`, `PhoneRegex`, `EmailRegex`

---

#### 3. XAML样式和模板

```xml
<!-- ValidationStyles.xaml -->
<ResourceDictionary>

    <!-- 必填字段标签样式 -->
    <Style x:Key="RequiredLabelStyle" TargetType="TextBlock" BasedOn="{StaticResource FormLabelStyle}">
        <Setter Property="local:FormFieldBehavior.IsRequired" Value="True"/>
    </Style>

    <!-- 必填星号样式 -->
    <Style x:Key="RequiredIndicatorStyle" TargetType="Run">
        <Setter Property="Foreground" Value="#DC3545"/>
        <Setter Property="FontWeight" Value="Bold"/>
    </Style>

    <!-- 验证错误边框样式 -->
    <Style x:Key="ValidationErrorBorderStyle" TargetType="Border">
        <Setter Property="BorderBrush" Value="Transparent"/>
        <Setter Property="BorderThickness" Value="0,0,0,2"/>
        <Style.Triggers>
            <DataTrigger Binding="{Binding Path=(Validation.HasError), RelativeSource={RelativeSource Self}}" Value="True">
                <Setter Property="BorderBrush" Value="#DC3545"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>

    <!-- 验证错误消息样式 -->
    <Style x:Key="ValidationErrorMessageStyle" TargetType="TextBlock">
        <Setter Property="Foreground" Value="#DC3545"/>
        <Setter Property="FontSize" Value="12"/>
        <Setter Property="Margin" Value="0,4,0,0"/>
        <Setter Property="TextWrapping" Value="Wrap"/>
        <Setter Property="Visibility" Value="Collapsed"/>
        <Style.Triggers>
            <DataTrigger Binding="{Binding Text, RelativeSource={RelativeSource Self}, Converter={StaticResource StringNullOrEmptyToBoolConverter}}" Value="False">
                <Setter Property="Visibility" Value="Visible"/>
            </DataTrigger>
        </Style.Triggers>
    </Style>

    <!-- 带验证的TextBox样式 -->
    <Style x:Key="ValidatingTextBoxStyle" TargetType="TextBox" BasedOn="{StaticResource EditableTextBoxStyle}">
        <Setter Property="Validation.ErrorTemplate">
            <Setter.Value>
                <ControlTemplate>
                    <Border BorderBrush="#DC3545" BorderThickness="0,0,0,2">
                        <AdornedElementPlaceholder/>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
        <Style.Triggers>
            <Trigger Property="Validation.HasError" Value="True">
                <Setter Property="ToolTip" Value="{Binding RelativeSource={RelativeSource Self}, Path=(Validation.Errors)[0].ErrorContent}"/>
            </Trigger>
        </Style.Triggers>
    </Style>

</ResourceDictionary>
```

---

> **设计决策**: 不创建FormField UserControl，采用Style模式。
>
> 理由：WPF原生`Validation.ErrorTemplate`机制与INotifyDataErrorInfo天然集成，
> Style模式更灵活、性能更好、与项目现有模式一致。

---

### 使用示例

#### 修改前 (当前代码)

```xml
<StackPanel>
    <TextBlock Text="用户名 *" Style="{StaticResource FormLabelStyle}"/>
    <TextBox Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
             Style="{StaticResource EditableTextBoxStyle}"/>
</StackPanel>
```

#### 修改后 (应用验证框架)

```xml
<StackPanel>
    <TextBlock Style="{StaticResource FormLabelStyle}">
        <Run Text="用户名"/>
        <Run Text=" *" Style="{StaticResource RequiredIndicatorStyle}"/>
    </TextBlock>
    <TextBox Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged,
                    ValidatesOnNotifyDataErrors=True}"
             Style="{StaticResource ValidatingTextBoxStyle}"/>
    <TextBlock Text="{Binding Errors[UserName]}"
               Style="{StaticResource ValidationErrorMessageStyle}"/>
</StackPanel>
```


---

### DetailModel改造

#### 修改前

```csharp
public class UserDetailModel : BindableBase
{
    private string _userName = string.Empty;

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }
}
```

#### 修改后

```csharp
public class UserDetailModel : ValidatableModelBase
{
    private string _userName = string.Empty;

    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(ValidationConstants.StringLength.UserNameMax,
                  MinimumLength = ValidationConstants.StringLength.UserNameMin,
                  ErrorMessage = "用户名长度必须在3-32个字符之间")]
    public string UserName
    {
        get => _userName;
        set => SetPropertyAndValidate(ref _userName, value);
    }
}
```

---

## 迁移策略

### Phase 1: 基础设施 (Infrastructure)

1. 创建 `ValidatableModelBase` 基类
2. 创建 `ValidationConstants` 常量类
3. 添加验证相关XAML样式到 `UnifiedComponents.xaml`

### Phase 2: Users模块 (Pilot)

1. 改造 `UserDetailModel` 继承 `ValidatableModelBase`
2. 添加DataAnnotations验证属性
3. 更新 `UserEditControl.xaml` 添加验证绑定和错误显示

### Phase 3: 其他模块推广

按顺序改造:
- Herbs模块
- Patients模块
- Formula模块
- MedicalCase模块

### Phase 4: 验证规则审计

1. 对齐所有层的验证规则
2. 确保Entity、DTO、DetailModel规则一致
3. 移除冗余验证

---

## 设计约束

1. **向后兼容** - 现有ViewModel验证继续工作
2. **渐进式迁移** - 模块可逐个升级
3. **无性能影响** - 验证仅在PropertyChanged时触发
4. **规则统一** - 使用ValidationConstants集中管理

## 验收标准

1. 必填字段有红色星号标识
2. 验证失败时输入框下方显示红色错误消息
3. 提交按钮在验证失败时禁用
4. 验证规则与服务端一致

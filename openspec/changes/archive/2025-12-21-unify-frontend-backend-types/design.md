# Design: 统一前后端实体类型与命名

## 设计原则

### 1. 类型一致性优先
UI Model的属性类型应与DTO保持一致，避免运行时类型转换。

### 2. 显示逻辑分离
UI显示文本通过独立的Display属性或XAML Converter处理，不混入数据属性。

### 3. 向后兼容
对于广泛使用的属性（如IsActive），保留计算属性以减少改动范围。

### 4. DTO层优先
先修复DTO层的类型不一致，再修复UI Model层。

## 详细设计

### Phase 0: DTO层修复

**MedicalCaseDetailDto当前实现：**
```csharp
public class MedicalCaseDetailDto
{
    [DisplayName("患者性别")]
    public string? PatientGender { get; set; }  // 错误：应为Gender枚举
}
```

**改造后：**
```csharp
public class MedicalCaseDetailDto
{
    [DisplayName("患者性别")]
    public Gender PatientGender { get; set; }  // 正确：使用Gender枚举
}
```

**MedicalCaseMappingProfile更新：**
```csharp
CreateMap<MedicalCase, MedicalCaseDetailDto>()
    .ForMember(dest => dest.PatientGender,
               opt => opt.MapFrom(src => src.Patient != null ? src.Patient.Gender : Gender.Unknown));
```

### Phase 1: PatientItem改造

**当前实现：**
```csharp
public class PatientItem : BindableBase
{
    private string _gender = string.Empty;
    public string Gender
    {
        get => _gender;
        set => SetProperty(ref _gender, value);
    }

    public static PatientItem FromDto(PatientDetailDto dto)
    {
        return new PatientItem
        {
            Gender = dto.Gender.ToString(), // 枚举转字符串
        };
    }

    public PatientDetailDto ToDto()
    {
        return new PatientDetailDto
        {
            Gender = Enum.Parse<Gender>(Gender), // 字符串转枚举
        };
    }
}
```

**改造后：**
```csharp
public class PatientItem : BindableBase
{
    private Gender _gender;
    public Gender Gender
    {
        get => _gender;
        set
        {
            if (SetProperty(ref _gender, value))
            {
                RaisePropertyChanged(nameof(GenderDisplay));
            }
        }
    }

    /// <summary>UI显示用</summary>
    public string GenderDisplay => Gender switch
    {
        Enums.Gender.Male => "男",
        Enums.Gender.Female => "女",
        _ => "未知"
    };

    public static PatientItem FromDto(PatientDetailDto dto)
    {
        return new PatientItem
        {
            Gender = dto.Gender, // 直接赋值
        };
    }

    public PatientDetailDto ToDto()
    {
        return new PatientDetailDto
        {
            Gender = Gender, // 直接赋值
        };
    }
}
```

### Phase 2: MedicalCaseItem改造

**当前实现：**
```csharp
public class MedicalCaseItem : BindableBase
{
    private string _patientGender = string.Empty;
    public string PatientGender
    {
        get => _patientGender;
        set => SetProperty(ref _patientGender, value);
    }

    public static MedicalCaseItem FromDto(MedicalCaseDetailDto dto)
    {
        return new MedicalCaseItem
        {
            PatientGender = "未知", // DTO中没有此属性，使用默认值
        };
    }
}
```

**改造后：**
```csharp
public class MedicalCaseItem : BindableBase
{
    private Gender _patientGender;
    public Gender PatientGender
    {
        get => _patientGender;
        set
        {
            if (SetProperty(ref _patientGender, value))
            {
                RaisePropertyChanged(nameof(PatientGenderDisplay));
            }
        }
    }

    /// <summary>UI显示用</summary>
    public string PatientGenderDisplay => PatientGender switch
    {
        Enums.Gender.Male => "男",
        Enums.Gender.Female => "女",
        _ => "未知"
    };

    public static MedicalCaseItem FromDto(MedicalCaseDetailDto dto)
    {
        return new MedicalCaseItem
        {
            PatientGender = dto.PatientGender, // 直接使用Gender枚举
        };
    }
}
```

### Phase 3-4: HerbItem/FormulaItem改造

**当前实现：**
```csharp
public class HerbItem : BindableBase
{
    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public static HerbItem FromDto(HerbListDto dto)
    {
        return new HerbItem
        {
            IsActive = dto.Status == CommonStatus.Enabled, // 枚举转bool
        };
    }

    public HerbDetailDto ToDto()
    {
        return new HerbDetailDto
        {
            Status = IsActive ? CommonStatus.Enabled : CommonStatus.Disabled, // bool转枚举
        };
    }
}
```

**改造后：**
```csharp
public class HerbItem : BindableBase
{
    private CommonStatus _status;
    public CommonStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                RaisePropertyChanged(nameof(IsActive));
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(StatusColor));
            }
        }
    }

    /// <summary>向后兼容计算属性</summary>
    public bool IsActive => Status == CommonStatus.Enabled;

    public string StatusText => Status switch
    {
        CommonStatus.Enabled => "启用",
        CommonStatus.Disabled => "停用",
        _ => "未知"
    };

    public static HerbItem FromDto(HerbListDto dto)
    {
        return new HerbItem
        {
            Status = dto.Status, // 直接赋值
        };
    }

    public HerbDetailDto ToDto()
    {
        return new HerbDetailDto
        {
            Status = Status, // 直接赋值
        };
    }
}
```

## XAML绑定处理

### 方案A：使用Display属性（推荐）
```xml
<!-- 改造前 -->
<TextBlock Text="{Binding Gender}" />

<!-- 改造后 -->
<TextBlock Text="{Binding GenderDisplay}" />
```

### 方案B：使用Converter
```xml
<TextBlock Text="{Binding Gender, Converter={StaticResource GenderToStringConverter}}" />
```

**推荐方案A**：Display属性更简单，不需要额外的Converter类。

## 影响分析

### Phase 0: DTO层
- 文件：`src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDetailDto.cs`
- 文件：`src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseListDto.cs`
- 文件：`src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMappingProfile.cs`
- 影响：需更新映射配置

### Phase 1: PatientItem
- 文件：`src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/PatientItem.cs`
- 绑定：`PatientMasterDetailView.xaml`, `PatientSelectionView.xaml`
- 影响：需更新绑定或添加GenderDisplay

### Phase 2: MedicalCaseItem
- 文件：`src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseItem.cs`
- 绑定：相关医案列表视图
- 影响：需更新绑定或添加PatientGenderDisplay

### Phase 3: HerbItem
- 文件：`src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Models/HerbItem.cs`
- 绑定：`HerbMasterDetailView.xaml`
- 影响：保持IsActive兼容，新增Status属性

### Phase 4: FormulaItem
- 文件：`src/Client/Desktop/Modules/LYBT.Desktop.Formula/Models/FormulaItem.cs`
- 绑定：`FormulaMasterDetailView.xaml`
- 影响：保持IsActive兼容，新增Status属性

## 测试策略

### 单元测试
1. 验证FromDto/ToDto正确转换
2. 验证Display属性返回正确文本
3. 验证IsActive计算属性正确工作

### UI测试
1. 验证列表显示正确
2. 验证枚举绑定正常工作
3. 验证向后兼容属性正常

### 集成测试
1. 验证CRUD功能正常
2. 验证API请求/响应正确序列化

## 关键代码位置

### DTO层
```
src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseDetailDto.cs:46
src/Shared/LYBT.Shared.Models/Contracts/MedicalCase/MedicalCaseListDto.cs
```

### UI Model层
```
src/Client/Desktop/Modules/LYBT.Desktop.Patients/Models/PatientItem.cs
src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/MedicalCaseItem.cs
src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Models/HerbItem.cs
src/Client/Desktop/Modules/LYBT.Desktop.Formula/Models/FormulaItem.cs
```

### Mapping层
```
src/Server/Modules/LYBT.Module.MedicalCase/Mapping/MedicalCaseMappingProfile.cs
```

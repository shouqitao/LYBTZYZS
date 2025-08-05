# 三层模型设计架构

## 一、架构概览

### 1.1 模型分层位置
```
┌─────────────────────────────────────────────────┐
│ 前端项目 (WPF/Web)                               │
│ └── ViewModels/Models (如: UserInfo)            │
├─────────────────────────────────────────────────┤
│ 共享项目 (LYBT.Shared.Models)                    │
│ └── DTOs (如: UserDto, UserCreateDto)           │
├─────────────────────────────────────────────────┤
│ 后端项目 (LYBT.Models)                           │
│ └── Entity Models (如: UserModel)               │
└─────────────────────────────────────────────────┘
```

### 1.2 项目结构示例
```
LYBTZYZS/
├── src/
│   ├── Backend/
│   │   ├── Core/
│   │   │   └── LYBT.Models/           # Entity Models
│   │   │       └── Users/
│   │   │           └── UserModel.cs   # 数据库实体
│   │   └── Services/
│   │       └── LYBT.WebAPI/           # Web API
│   │
│   ├── Shared/
│   │   └── LYBT.Shared.Models/        # 共享DTOs
│   │       └── Contracts/
│   │           └── Users/
│   │               ├── UserDto.cs
│   │               ├── UserCreateDto.cs
│   │               └── UserUpdateDto.cs
│   │
│   └── Frontend/
│       └── Desktop/
│           └── LYBT.WPF.Client/       # WPF客户端
│               └── Models/
│                   └── UserInfo.cs    # 前端模型
```

## 二、各层模型设计

### 2.1 后端Entity Model (UserModel)
**位置**：`LYBT.Models/Users/UserModel.cs`
```csharp
namespace LYBT.Models.Users {
    /// <summary>
    /// 用户实体模型 - 直接映射数据库表
    /// </summary>
    public class UserModel {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }      // 敏感信息
        public string Salt { get; set; }              // 敏感信息
        public string RealName { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public int FailedLoginCount { get; set; }    // 内部使用
        public DateTime? LockoutEnd { get; set; }     // 内部使用
        
        // 导航属性
        public virtual ICollection<DoctorModel> Doctors { get; set; }
        public virtual ICollection<LogModel> Logs { get; set; }
    }
}
```

### 2.2 共享DTO (Shared Models)
**位置**：`LYBT.Shared.Models/Contracts/Users/`

#### 列表DTO
```csharp
namespace LYBT.Shared.Models.Contracts.Users {
    /// <summary>
    /// 用户列表DTO - 用于API传输
    /// </summary>
    public class UserDto {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string RealName { get; set; }
        public string RoleName { get; set; }         // 友好显示
        public bool IsActive { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        // 注意：不包含密码等敏感信息
    }
}
```

#### 创建DTO
```csharp
public class UserCreateDto {
    [Required]
    public string Username { get; set; }
    
    [Required]
    public string Password { get; set; }          // 明文密码，后端加密
    
    [Required]
    public string RealName { get; set; }
    
    public UserRole Role { get; set; }
    // 注意：不包含Id、CreateTime等自动生成字段
}
```

### 2.3 前端Model (UserInfo)
**位置**：`LYBT.WPF.Client/Models/UserInfo.cs`
```csharp
namespace LYBT.WPF.Client.Models {
    /// <summary>
    /// 前端用户信息模型 - 用于界面绑定
    /// </summary>
    public class UserInfo : ObservableObject {
        private Guid _id;
        private string _username;
        private string _realName;
        private string _roleName;
        private bool _isActive;
        private string _statusText;
        private string _avatar;

        public Guid Id {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Username {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string RealName {
            get => _realName;
            set => SetProperty(ref _realName, value);
        }

        public string RoleName {
            get => _roleName;
            set => SetProperty(ref _roleName, value);
        }

        public bool IsActive {
            get => _isActive;
            set {
                SetProperty(ref _isActive, value);
                StatusText = value ? "正常" : "已禁用";
            }
        }

        public string StatusText {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string Avatar {
            get => _avatar;
            set => SetProperty(ref _avatar, value);
        }

        // 前端特有的计算属性
        public string DisplayName => $"{RealName} ({Username})";
        
        // UI相关属性
        public bool CanEdit => IsActive;
        public string StatusColor => IsActive ? "Green" : "Red";
    }
}
```

## 三、数据转换流程

### 3.1 查询流程
```
数据库 → UserModel → UserDto → UserInfo → UI显示
```

#### 后端转换（UserModel → UserDto）
```csharp
// 使用AutoMapper
public class UserMappingProfile : Profile {
    public UserMappingProfile() {
        CreateMap<UserModel, UserDto>()
            .ForMember(dest => dest.RoleName, 
                opt => opt.MapFrom(src => src.Role.GetDescription()));
    }
}

// Service层
public async Task<List<UserDto>> GetUsersAsync() {
    var users = await _repository.GetListAsync();
    return _mapper.Map<List<UserDto>>(users);
}
```

#### 前端转换（UserDto → UserInfo）
```csharp
// WPF ViewModel
public class UserListViewModel : ViewModelBase {
    public async Task LoadUsersAsync() {
        var response = await _apiClient.GetAsync<List<UserDto>>("/api/users");
        
        var userInfos = response.Data.Select(dto => new UserInfo {
            Id = dto.Id,
            Username = dto.Username,
            RealName = dto.RealName,
            RoleName = dto.RoleName,
            IsActive = dto.IsActive,
            Avatar = GetAvatarUrl(dto.Username)  // 前端特有逻辑
        }).ToList();
        
        Users = new ObservableCollection<UserInfo>(userInfos);
    }
}
```

### 3.2 创建流程
```
UI输入 → UserCreateInfo → UserCreateDto → UserModel → 数据库
```

#### 前端收集数据
```csharp
// WPF创建用户窗口
public class CreateUserViewModel : ViewModelBase {
    public UserCreateInfo NewUser { get; set; }
    
    private async Task CreateUserAsync() {
        // 前端验证
        if (!ValidateInput()) return;
        
        // 转换为DTO
        var dto = new UserCreateDto {
            Username = NewUser.Username,
            Password = NewUser.Password,
            RealName = NewUser.RealName,
            Role = NewUser.SelectedRole
        };
        
        // 调用API
        var response = await _apiClient.PostAsync("/api/users/add", dto);
        if (response.Success) {
            await CloseDialogAsync();
        }
    }
}
```

## 四、设计优势

### 4.1 清晰的职责分离
- **Entity Model**：数据持久化，包含所有数据库字段
- **DTO**：数据传输，控制API暴露的数据
- **前端Model**：UI展示，包含界面特有属性

### 4.2 安全性增强
- 敏感信息（如密码）只存在于Entity层
- DTO层过滤敏感信息
- 前端完全接触不到敏感数据

### 4.3 灵活性提升
- 数据库结构变化不影响前端
- 前端UI需求变化不影响后端
- API契约（DTO）独立演进

### 4.4 可维护性
- 各层模型职责单一
- 修改影响范围可控
- 便于单元测试

## 五、最佳实践

### 5.1 命名规范
```
后端Entity:  [Entity]Model     (如: UserModel)
共享DTO:     [Entity]Dto       (如: UserDto)
前端Model:   [Entity]Info      (如: UserInfo)
             或 [Entity]ViewModel
```

### 5.2 DTO设计原则
1. **按使用场景设计**：不同操作使用不同DTO
2. **最小化原则**：只包含必需字段
3. **扁平化结构**：避免深层嵌套
4. **版本控制**：API版本升级时保持向后兼容

### 5.3 转换策略
1. **后端使用AutoMapper**：自动化Entity-DTO转换
2. **前端手动映射**：保持灵活性，添加UI特有逻辑
3. **统一转换位置**：Service层负责Entity→DTO，ViewModel负责DTO→Model

### 5.4 共享模型注意事项
1. **避免依赖特定框架**：DTO应该是POCO
2. **使用基础类型**：确保跨平台兼容
3. **清晰的文档**：每个字段都要有注释
4. **版本管理**：考虑API版本升级策略

## 六、实际应用示例

### 6.1 患者管理模块
```
// Entity (后端)
PatientModel {
    Id, Name, IdCard, PhoneNumber, 
    MedicalHistory, CreateTime, CreatedBy...
}

// DTO (共享)
PatientDto {
    Id, Name, Gender, Age, PhoneNumber, 
    LastVisitTime, VisitCount
}

// Model (前端)
PatientInfo {
    // 继承DTO属性
    Id, Name, Gender, Age...
    // 前端特有
    DisplayAge: "25岁"
    GenderIcon: "👨"/"👩"
    IsVip: bool
}
```

### 6.2 数据流示例
```csharp
// 1. 后端查询
var patients = await _context.Patients
    .Where(p => p.IsActive)
    .ToListAsync();

// 2. 转换为DTO
var dtos = _mapper.Map<List<PatientDto>>(patients);

// 3. API返回
return Ok(ApiResponse<List<PatientDto>>.Success(dtos));

// 4. 前端接收
var response = await _apiClient.GetAsync<List<PatientDto>>("/api/patients");

// 5. 转换为前端Model
var patientInfos = response.Data.Select(dto => new PatientInfo(dto) {
    GenderIcon = dto.Gender == 1 ? "👨" : "👩",
    IsVip = CheckVipStatus(dto.Id)
});

// 6. 绑定到UI
Patients = new ObservableCollection<PatientInfo>(patientInfos);
```

## 七、总结

这种三层模型设计实现了：
1. **完美的关注点分离**
2. **高度的安全性**
3. **良好的可维护性**
4. **灵活的扩展性**

是大型企业应用的推荐架构模式。
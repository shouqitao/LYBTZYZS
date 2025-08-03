# LYBT 前端编程规范

## 1. 总体原则

### 1.1 架构原则
- **前端只定义自己的展示模型**，如 `UserInfo`、`HerbInfo` 等，这些模型继承自 Shared 模块的基础模型
- **所有 DTO（数据传输对象）必须使用 Shared 模块**中定义的，前端不得自定义 DTO
- **所有枚举类型必须使用 Shared 模块**中定义的，确保前后端一致性
- **前端模型可以包含额外的展示属性**，但核心属性必须与 Shared 模块保持一致

### 1.2 命名约定
- **用户名统一使用 `Username`**，不使用 `UserName`（注意大小写）
- **属性名称必须与 Shared 模块保持一致**，如：
  - `CreatedTime` 而不是 `CreateTime`
  - `UpdatedTime` 而不是 `UpdateTime`
  - `SearchKeyword` 而不是 `Keyword`
  - `CurrentPage` 而不是 `Page`

## 2. 模块引用规范

### 2.1 必须引用的命名空间
```csharp
// 使用 Shared 模块的 DTO
using LYBT.Shared.Models.Auth;          // 认证相关 DTO
using LYBT.Shared.Models.Users;         // 用户相关 DTO
using LYBT.Shared.Models.Herbs;         // 药材相关 DTO
using LYBT.Shared.Models.Contracts.*;   // 各模块查询 DTO
using LYBT.Shared.Models.Common;        // 通用模型（ApiResponse、PaginationRequest 等）
using LYBT.Shared.Models.Enums;         // 所有枚举定义
```

### 2.2 前端专用命名空间
```csharp
// 前端展示模型
using LYBT.WPF.Client.Core.Models.*;    // 前端展示模型
using LYBT.WPF.Client.Core.Interfaces.*;// 前端服务接口
using LYBT.WPF.Client.Core.ViewModels.*;// 基础 ViewModel
```

## 3. API 配置规范

### 3.1 API 地址配置逻辑
- **默认地址**：`http://localhost:5297/`
- **配置文件地址**：在 `appsettings.json` 中配置
- **使用规则**：
  - 如果配置文件中的地址为空或等于默认地址，使用默认地址
  - 否则使用配置文件中的自定义地址
  - 确保 URL 始终以 `/` 结尾

### 3.2 配置示例
```json
{
  "ApiSettings": {
    "BaseUrl": "http://192.168.190.243:5000/",  // 自定义地址
    "TimeoutSeconds": 60
  }
}
```

## 4. 数据模型规范

### 4.1 DTO 命名规范

#### 4.1.1 标准命名格式
**DTO 命名规范：实体 + 动作（可选） + Dto**

- **展示模型**：`实体 + Dto`
  - `UserDto` - 用户展示模型（用于列表、详情展示）
  - `HerbDto` - 药材展示模型
  - `PatientDto` - 患者展示模型
  - `DoctorDto` - 医生展示模型

- **操作模型**：`实体 + 动作 + Dto`
  - `UserCreateDto` - 用户创建 DTO
  - `UserUpdateDto` - 用户更新 DTO
  - `PatientQueryDto` - 患者查询 DTO
  - `HerbBatchStatusUpdateDto` - 药材批量状态更新 DTO

#### 4.1.2 使用原则
1. **展示模型（如 UserDto）用于数据展示**，包含实体的所有或大部分属性
2. **操作模型（如 UserCreateDto）用于特定操作**，只包含该操作所需的属性
3. **前端必须使用 Shared 模块中定义的 DTO**，不得自行创建

**注意**：Shared 模块中存在两套 DTO：
- `Contracts` 文件夹中的 DTO 遵循标准命名（推荐使用）
- 根目录下的部分 DTO 使用非标准命名（如 `CreateUserDto`），应逐步迁移到标准命名

当前 Shared 模块中的 DTO 命名情况：
- **Users 模块**：使用 `CreateUserDto`、`UpdateUserDto`（动作在前）
- **Herbs 模块**：使用 `CreateHerbDto`、`UpdateHerbDto`（动作在前）
- **Contracts 模块**：大部分遵循实体+动作+Dto 格式

**前端开发原则**：
1. 优先使用 Shared 模块中已定义的 DTO，即使命名不完全符合规范
2. 不得自行创建 DTO 类
3. 如需新的 DTO，应向后端团队提出需求

### 4.2 请求模型使用
```csharp
// ❌ 错误：前端自定义请求类
public class UserQueryRequest { }

// ✅ 正确：使用 Shared 模块的 DTO
var request = new UserPagedQueryDto
{
    SearchKeyword = keyword,
    CurrentPage = 1,
    PageSize = 20
};
```

### 4.2 枚举使用
```csharp
// ❌ 错误：前端定义枚举
public enum ConsultationStatus { }

// ✅ 正确：使用 Shared 模块的枚举
using LYBT.Shared.Models.Enums;
var status = ConsultationStatus.InProgress;
```

### 4.3 角色处理
```csharp
// 角色字符串转枚举
var role = UserRole.Admin;
var roleString = role.ToString();  // "Admin"

// 创建用户时使用字符串
var createDto = new CreateUserDto
{
    Role = selectedRole.ToString()  // 枚举转字符串
};
```

## 5. 服务层规范

### 5.1 服务接口定义
```csharp
public interface IUserService
{
    // 使用 Shared 的 DTO 作为参数
    Task<PaginatedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request);
    Task<ApiResponse<object>> CreateUserAsync(CreateUserDto request);
    Task<ApiResponse<object>> UpdateUserAsync(UpdateUserDto request);
}
```

### 5.2 服务实现
```csharp
public class UserService : IUserService
{
    public async Task<ApiResponse<object>> CreateUserAsync(CreateUserDto request)
    {
        // 直接使用 request，不需要转换
        var response = await _userApiService.CreateUserAsync(request);
        // ...
    }
}
```

## 6. ViewModel 规范

### 6.1 命令定义
```csharp
public class UserManagementViewModel : BindableBase
{
    // 查询时使用 Shared DTO
    private async Task LoadUsers()
    {
        var request = new UserPagedQueryDto
        {
            SearchKeyword = SearchKeyword,
            CurrentPage = CurrentPage,
            PageSize = PageSize
        };
        
        var result = await _userService.SearchUsersAsync(request);
    }
}
```

### 6.2 数据绑定模型
```csharp
// 前端展示模型可以包含额外属性
public class UserInfo : BaseUserModel  // 继承自 Shared
{
    // 前端专用的展示属性
    public string StatusDescription { get; set; }
    public bool IsSelected { get; set; }
}
```

## 7. 转换器规范

### 7.1 全局转换器
- **所有 WPF 转换器都在 App.xaml 中全局定义**
- **不在单个窗口或控件中定义转换器**

```xml
<Application.Resources>
    <!-- 全局转换器 -->
    <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
    <converters:StringToVisibilityConverter x:Key="StringToVisibilityConverter"/>
</Application.Resources>
```

## 8. 异常处理规范

### 8.1 API 调用异常处理
```csharp
try
{
    var response = await _apiService.CallAsync();
    if (response.IsSuccess)
    {
        // 处理成功
    }
    else
    {
        // 处理业务错误
        ShowError(response.Message);
    }
}
catch (Exception ex)
{
    // 处理系统异常
    _logger.LogError(ex, "API调用失败");
    ShowError("系统错误，请稍后重试");
}
```

## 9. 编码习惯

### 9.1 异步方法
- 所有 API 调用必须使用 `async/await`
- 方法名以 `Async` 结尾

### 9.2 依赖注入
- 仅使用构造函数注入
- 接口优于具体实现

### 9.3 日志记录
- 使用结构化日志
- 记录关键操作和异常

## 10. 文件组织

### 10.1 项目结构
```
src/Frontend/Desktop/
├── Core/                    # 核心层
│   ├── Models/             # 前端展示模型
│   ├── Interfaces/         # 服务接口
│   ├── ViewModels/         # 基础 ViewModel
│   └── Converters/         # 转换器
├── Services/               # 服务实现
├── Modules/                # 功能模块
│   ├── Authentication/     # 认证模块
│   ├── SystemManagement/   # 系统管理
│   └── ...
└── Shell/                  # 主程序壳
```

### 10.2 命名空间组织
- 按照文件夹结构组织命名空间
- 保持命名空间与文件路径一致

## 11. 注释规范

### 11.1 类和方法注释
```csharp
/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    /// <summary>
    /// 搜索用户
    /// </summary>
    /// <param name="request">查询请求</param>
    /// <returns>分页结果</returns>
    public async Task<PaginatedResult<UserInfo>> SearchUsersAsync(UserPagedQueryDto request)
    {
        // 实现代码
    }
}
```

### 11.2 TODO 注释
```csharp
// TODO: 需要后端添加 Id 属性到 UpdateUserDto
// TODO: 以下属性在 CreateRecordDto 中缺失，需要后端添加
```

## 12. 基础数据管理规范

### 12.1 数据删除策略
基础数据（用户、草药、模板等）不进行物理删除，只进行逻辑删除：
- 使用 `IsActive` 或 `IsEnabled` 字段控制数据状态
- 删除操作改为禁用/启用切换操作
- 前后端统一使用此策略

### 12.2 操作规范
- **新建**：创建新记录，默认状态为启用
- **编辑**：修改现有记录信息
- **禁用/启用**：切换记录的可用状态
- **不提供删除功能**：避免数据丢失和引用完整性问题

### 12.3 界面设计原则
- 删除按钮改为"禁用/启用"切换按钮
- 根据当前状态显示对应操作：
  - 启用状态显示"禁用"按钮
  - 禁用状态显示"启用"按钮
- 使用不同颜色和图标区分状态

## 13. DTO-UserControl 映射规范

### 12.1 设计原则
每个展示型 DTO 应该有对应的 UserControl，实现数据展示的标准化和可重用性。

### 12.2 命名规范
- **控件命名**：`{实体名}DisplayControl`
  - 示例：`UserDisplayControl`、`HerbDisplayControl`
- **控件文件位置**：`Controls/{实体名复数}/`
  - 示例：`Controls/Users/UserDisplayControl.xaml`

### 12.3 基础架构

#### 12.3.1 基类设计
所有 DTO 展示控件应继承自 `BaseDisplayControl<TDto>`：

```csharp
public abstract class BaseDisplayControl<TDto> : UserControl
    where TDto : class
{
    // Data 依赖属性
    public TDto Data { get; set; }
    
    // 显示模式
    public DisplayMode DisplayMode { get; set; }
    
    // 是否编辑模式
    public bool IsEditMode { get; set; }
}
```

#### 12.3.2 显示模式
```csharp
public enum DisplayMode
{
    Default,    // 默认模式
    Compact,    // 紧凑模式
    Detailed,   // 详细模式
    List,       // 列表模式
    Card,       // 卡片模式
    TableRow    // 表格行模式
}
```

### 12.4 实现示例

#### 12.4.1 XAML 定义
```xml
<UserControl x:Class="LYBT.WPF.Client.Controls.Users.UserDisplayControl"
             xmlns:base="clr-namespace:LYBT.WPF.Client.Controls.Base">
    <materialDesign:Card>
        <!-- 控件内容 -->
    </materialDesign:Card>
</UserControl>
```

#### 12.4.2 代码后置
```csharp
public partial class UserDisplayControl : BaseDisplayControl<UserDto>
{
    protected override void OnDataChanged(UserDto oldValue, UserDto newValue)
    {
        // 数据变更时的处理逻辑
    }
}
```

### 12.5 使用方式

#### 12.5.1 在 XAML 中使用
```xml
<controls:UserDisplayControl 
    Data="{Binding SelectedUser}"
    DisplayMode="Card"/>
```

#### 12.5.2 在列表中使用
```xml
<ListBox ItemsSource="{Binding Users}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <controls:UserDisplayControl 
                Data="{Binding}"
                DisplayMode="ListItem"/>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### 12.6 最佳实践

1. **数据绑定**：使用依赖属性确保数据绑定正常工作
2. **性能优化**：对于列表场景，使用紧凑模式减少资源消耗
3. **样式一致性**：使用 Material Design 组件保持 UI 一致性
4. **响应式设计**：根据不同显示模式调整布局
5. **可访问性**：确保控件支持键盘导航和屏幕阅读器

### 12.7 控件功能扩展

#### 12.7.1 交互功能
- 点击事件处理
- 右键菜单
- 拖拽支持

#### 12.7.2 编辑功能
- 内联编辑模式
- 验证支持
- 撤销/重做

#### 12.7.3 状态指示
- 加载状态
- 错误状态
- 选中状态

---

**最后更新时间**：2025-08-03

**注意**：本规范是活文档，会根据项目发展持续更新。所有开发人员必须遵循这些规范以确保代码质量和一致性。
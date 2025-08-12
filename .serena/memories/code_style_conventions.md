# 代码风格和约定

## C# 编码规范

### 命名约定
- **PascalCase**: 类名、方法名、属性、接口
- **camelCase**: 参数、局部变量
- **_camelCase**: 私有字段
- **IInterface**: 接口以 I 开头

### 代码结构
```csharp
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    
    public async Task<UserDto> GetUserAsync(int userId)
    {
        // 实现
    }
}
```

### XML文档注释
```csharp
/// <summary>
/// 方法功能简述
/// </summary>
/// <param name="参数名">参数说明</param>
/// <returns>返回值说明</returns>
```

## 架构模式

### 分层架构
- **Controllers** - API端点
- **Services** - 业务逻辑
- **Repositories** - 数据访问
- **Models** - 领域模型
- **DTOs** - 数据传输对象

### 依赖注入
- 构造函数注入模式
- 使用接口而非具体实现
- 在模块的Module类中注册服务

### 异步编程
- 所有数据库操作使用 async/await
- 异步方法命名以Async结尾
- 避免 async void（事件处理器除外）

## 前端WPF规范

### MVVM模式
- View（XAML）
- ViewModel（业务逻辑）
- Model（数据模型）
- 使用Prism框架

### 命名约定
- View: xxxView.xaml
- ViewModel: xxxViewModel.cs
- 命令: xxxCommand
- 属性通知: INotifyPropertyChanged

## 文件组织

### 禁止规则
- 不在根目录创建文档
- 使用英文文件名
- 使用kebab-case命名
- 报告包含日期（YYYYMMDD）

### 目录规范
- docs/ - 所有文档
- scripts/ - 脚本文件
- tests/ - 测试代码
- src/ - 源代码
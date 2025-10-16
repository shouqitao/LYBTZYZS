# 问题解决方案（增强版）

**按紧急程度分类的快速问题解决指南** - 解决日常开发中95%的常见问题

---

## 🚨 紧急问题 (P0 - 立即解决)

### 🔥 应用无法启动

#### 问题1: 数据库连接失败
**症状**: 应用启动后立即崩溃，日志显示数据库连接错误

**快速诊断**:
```bash
# 1. 检查数据库服务状态
sqlcmd -S localhost -E -Q "SELECT 1"

# 2. 测试连接字符串
dotnet run --project src/Server/Services/LYBT.WebAPI --no-build

# 3. 检查连接字符串格式
echo $env:DATABASE_CONNECTION_STRING
```

**解决方案**:
```json
// 开发环境连接字符串
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}

// 生产环境连接字符串
{
  "ConnectionStrings": {
    "DefaultConnection": "#{DATABASE_CONNECTION_STRING}#"
  }
}
```

**验证步骤**:
```bash
# 1. 验证数据库存在
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name='LYBTDB'"

# 2. 验证连接权限
sqlcmd -S localhost -E -d LYBTDB -Q "SELECT 1"

# 3. 检查表结构
sqlcmd -S localhost -E -d LYBTDB -Q "SELECT COUNT(*) FROM Users"
```

#### 问题2: JWT密钥配置错误
**症状**: 登录接口返回401未授权，即使使用正确的用户名密码

**快速诊断**:
```bash
# 1. 检查JWT配置
grep -n "SecretKey" appsettings.json

# 2. 验证密钥长度
echo "配置的密钥长度: $(grep -o '"SecretKey": "[^"]*"' appsettings.json | wc -c)"
```

**解决方案**:
```json
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "YOUR_256_BIT_BASE64_ENCODED_SECRET_KEY_HERE",
        "Issuer": "LYBT.WebAPI",
        "Audience": "LYBT.Client"
      }
    }
  }
}
```

**生成新密钥**:
```bash
# 生成256位随机密钥
openssl rand -base64 32

# 或使用PowerShell
Add-Type -AssemblyName System.Web
$random = [System.Web.Security.Cryptography]::MachineKey.GenerateKey(32)
$base64 = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($random))
echo $base64
```

### 🔥 认证授权问题

#### 问题3: 用户登录失败
**症状**: 登录接口返回"用户名或密码错误"

**快速诊断**:
```csharp
// 检查用户是否存在
var user = await _userRepository.GetByUsernameAsync(username);
if (user == null) {
    // 检查数据库中的用户数据
}

// 检查密码哈希
bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
if (!isValid) {
    // 检查密码哈希格式和BCrypt版本
}
```

**解决方案**:
```bash
# 1. 重置管理员密码
dotnet run --project src/Server/Services/LYBT.WebAPI --no-restore -- --admin-reset

# 2. 检查密码策略
grep -A 10 "PasswordPolicy" appsettings.json
```

**创建用户脚本**:
```sql
-- 直接在数据库中创建用户
INSERT INTO Users (Id, Username, Email, PasswordHash, Role, CreatedAt)
VALUES (
    NEWID(),
    'admin',
    'admin@lybt.com',
    '$2a$10$YourHashedPasswordHere',
    'Admin',
    GETDATE()
);
```

---

## ⚠️ 重要问题 (P1 - 1小时内解决)

### 🔧 API调用错误

#### 问题4: API返回500内部错误
**症状**: API调用返回500状态码，响应体包含错误信息

**快速诊断**:
```bash
# 1. 检查应用日志
tail -f logs/lybt-web-api-*.log

# 2. 检查数据库连接
dotnet ef database update

# 3. 检查配置文件
cat appsettings.json | grep -E "(ConnectionStrings|Lybt)"
```

**常见原因和解决方案**:

**数据库连接问题**:
```json
// 确保连接字符串正确
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true"
  }
}
```

**配置文件格式错误**:
```bash
# 验证JSON格式
python -m json.tool appsettings.json

# 或使用在线JSON验证器
```

**依赖服务未启动**:
```bash
# 检查必需服务
dotnet ef database update --verbose
```

#### 问题5: CORS跨域错误
**症状**: 浏览器控制台显示CORS错误

**快速诊断**:
```javascript
// 检查OPTIONS预检请求
fetch('http://localhost:5001/api/v1/auth/login', {
  method: 'OPTIONS',
  headers: {
    'Content-Type': 'application/json'
  }
});
```

**解决方案**:
```csharp
// 在Program.cs中配置CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowAll");
```

### 🗄️ 数据库问题

#### 问题6: 迁移失败
**症状**: `dotnet ef database update` 执行失败

**快速诊断**:
```bash
# 1. 检查迁移状态
dotnet ef migrations list

# 2. 检查数据库连接
dotnet ef database update --verbose

# 3. 检查SQL脚本
dotnet ef migrations script --id LastMigration
```

**解决方案**:
```bash
# 1. 重置数据库
dotnet ef database drop
dotnet ef database update

# 2. 单独应用迁移
dotnet ef database update --migration 20241001000000_InitialCreate

# 3. 手动执行SQL
dotnet ef migrations script | sqlcmd -S localhost -d LYBTDB
```

#### 问题7: 实体验证失败
**症状**: 保存数据时出现验证错误

**快速诊断**:
```csharp
// 检查验证规则
var validationResults = new List<ValidationResult>();
var context = new ValidationContext(entity);
Validator.TryValidateObject(entity, context, validationResults, true);

// 检查必填字段
if (string.IsNullOrEmpty(entity.Name)) {
    Console.WriteLine("名称是必填字段");
}
```

**解决方案**:
```csharp
// 修复验证规则
public class UserCreateDto
{
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-50个字符之间")]
    public string Username { get; set; }

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; }
}
```

---

## 📋 一般问题 (P2 - 24小时内解决)

### 🚀 性能问题

#### 问题8: API响应缓慢
**症状**: API调用响应时间超过5秒

**快速诊断**:
```bash
# 1. 检查数据库查询
dotnet run --project src/Server/Services/LYBT.WebAPI --no-restore -- --diagnostics

# 2. 检查内存使用
dotnet-counters monitor --process-id <PID>

# 3. 检查SQL查询
SELECT 
    creation_time, 
    cpu_time,
    total_elapsed_time,
    SUBSTRING(qt.text, 1, 200) as query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY total_elapsed_time DESC;
```

**解决方案**:
```csharp
// 添加索引
public class User
{
    [Index]
    public string Username { get; set; }
    
    [Index]
    public DateTime CreatedAt { get; set; }
}

// 使用分页查询
public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize)
{
    return await _users
        .OrderBy(u => u.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ProjectTo<UserDto>(_mapper.Configuration)
        .ToListAsync();
}

// 启用缓存
public async Task<UserDto> GetByIdAsync(Guid id)
{
    var cacheKey = $"user_{id}";
    if (_cache.TryGetValue(cacheKey, out UserDto cached))
        return cached;
        
    var user = await _repository.GetByIdAsync(id);
    var dto = _mapper.Map<UserDto>(user);
    
    _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(30));
    return dto;
}
```

#### 问题9: 内存泄漏
**症状**: 应用内存持续增长，最终崩溃

**快速诊断**:
```bash
# 监控内存使用
dotnet-counters monitor --process-id <PID> System.Runtime.Memory

# 检查GC信息
dotnet-counters monitor --process-id <PID> System.Runtime.GC
```

**解决方案**:
```csharp
// 使用using语句
public async Task<ServiceResult> ProcessFileAsync(Stream stream)
{
    using var reader = new StreamReader(stream);
    // 处理文件
} // 自动释放资源

// 释放事件处理器
public class EventPublisher : IDisposable
{
    private readonly List<IDisposable> _subscriptions = new();
    
    public void Subscribe<T>(Action<T> handler)
    {
        // 订阅逻辑
    }
    
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription?.Dispose();
        }
        _subscriptions.Clear();
    }
}
```

### 🔒 安全问题

#### 问题10: 用户权限控制不当
**症状**: 普通用户可以访问管理员功能

**快速诊断**:
```csharp
// 检查用户角色
var user = await _userRepository.GetByIdAsync(userId);
Console.WriteLine($"用户角色: {user.Role}");

// 检查权限验证
[Authorize(Roles = "Admin")]
public IActionResult GetAdminData()
{
    // 管理员功能
}
```

**解决方案**:
```csharp
// 角色权限检查
[Authorize(Roles = "Admin")]
public IActionResult DeleteUser(Guid id)
{
    // 管理员删除用户
}

// 策略权限检查
[Authorize(Policy = "CanManagePatients")]
public IActionResult CreatePatient(PatientCreateDto dto)
{
    // 创建患者
}

// 自定义策略
services.AddAuthorization(options =>
{
    options.AddPolicy("CanManagePatients", policy =>
    {
        policy.RequireRole("Doctor", "Admin");
    });
});
```

---

## 🛠️ 开发工具问题

### 11. Visual Studio调试问题

#### 问题11: 断点不触发
**症状**: 设置断点但调试时不停止

**解决方案**:
```csharp
// 确保使用Debug模式
#if DEBUG
    Console.WriteLine("调试模式已启用");
#endif

// 检查符号文件
// 项目 -> 属性 -> 生成 -> 高级 -> 调试信息 -> 完整
```

**调试配置**:
```json
// launchSettings.json
{
  "profiles": {
    "LYBT.WebAPI": {
      "commandName": "Project",
      "launchBrowser": true,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "applicationUrl": "https://localhost:5001"
    }
  }
}
```

### 12. 包管理问题

#### 问题12: NuGet包还原失败
**症状**: `dotnet restore` 失败

**解决方案**:
```bash
# 清理NuGet缓存
dotnet nuget locals all --list
dotnet nuget locals all --clear

# 设置NuGet源
dotnet nuget add source https://api.nuget.org/v3/index.json --name "NuGet Official"

# 强制还原
dotnet restore --force
```

**包源配置**:
```xml
<!-- NuGet.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

---

## 📱 客户端问题

### 13. WPF应用启动问题

#### 问题13: 客户端无法连接服务端
**症状**: 启动后显示连接错误

**快速诊断**:
```csharp
// 检查API基础URL
var baseUrl = configuration["ApiSettings:BaseUrl"];
Console.WriteLine($"API URL: {baseUrl}");

// 测试连接
using var client = new HttpClient();
var response = await client.GetAsync($"{baseUrl}/api/v1/auth/validate");
Console.WriteLine($"连接测试: {response.StatusCode}");
```

**解决方案**:
```json
// appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5001/",
    "TimeoutSeconds": 30
  }
}
```

**网络配置**:
```csharp
// 配置HttpClient
services.AddHttpClient<ApiService>(client =>
{
    client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]);
    client.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 30));
});
```

---

## 🔍 问题诊断检查清单

### 快速诊断流程

1. **检查服务状态**
   - [ ] 数据库服务运行正常
   - [ ] WebAPI服务启动成功
   - [ ] 客户端应用可以访问

2. **检查配置文件**
   - [ ] appsettings.json格式正确
   - [ ] 连接字符串配置正确
   - [ ] JWT密钥配置正确

3. **检查日志文件**
   - [ ] 查看应用启动日志
   - [ ] 查看错误日志
   - [ ] 查看调试日志

4. **检查网络连接**
   - [ ] 端口未被占用
   - [ ] 防火墙配置正确
   - [ ] 代理设置正确

5. **检查数据库状态**
   - [ ] 数据库文件存在
   - [ ] 表结构正确
   - [ ] 数据迁移完成

### 常用命令集合

```bash
# 环境检查
dotnet --version
node --version
npm --version

# 项目操作
dotnet restore
dotnet build
dotnet run
dotnet test

# 数据库操作
dotnet ef database update
dotnet ef migrations add <MigrationName>
dotnet ef migrations script

# 日志查看
tail -f logs/*.log
Get-Content logs/*.log -Tail 50
```

---

## 📞 求助资源

### 技术文档
- [API文档](../api-reference.md)
- [配置指南](../config-templates.md)
- [代码模式](../code-patterns.md)
- [开发清单](../development-checklist.md)

### 在线资源
- [Microsoft .NET文档](https://docs.microsoft.com/dotnet/)
- [Entity Framework Core文档](https://docs.microsoft.com/ef/core/)
- [ASP.NET Core文档](https://docs.microsoft.com/aspnet/core/)

### 团队支持
- 技术负责人：[联系方式]
- 项目经理：[联系方式]
- 开发团队：[联系方式]

---

**使用提示**: 按紧急程度查找问题，先执行快速诊断，然后按照解决方案步骤操作。如果问题仍然存在，请记录详细的错误信息和操作步骤。
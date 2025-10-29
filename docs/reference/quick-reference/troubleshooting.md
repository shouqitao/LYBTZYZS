# 故障排除指南

\*\*按紧急程度分类的快速问题解决指南\*\* - 解决日常开发中95%的常见问题

## 🚨 紧急问题 (P0 - 立即解决)

### 1. 编译错误

#### 问题：缺少依赖包
```
错误 CS0246: 找不到类型或命名空间名"XXX"
```

**解决方案：**
```bash
# 恢复NuGet包
dotnet restore LYBT.All.sln

# 清理并重建
dotnet clean LYBT.All.sln
dotnet build LYBT.All.sln -c Release --no-restore
```

**预防措施：**
- 每次pull代码后先执行`dotnet restore`
- 使用`LYBT.All.sln`统一解决方案文件
- 定期清理bin和obj目录

#### 问题：AutoMapper配置错误
```
AutoMapper.AutoMapperMappingException: Missing type map configuration
```

**解决方案：**
```csharp
// 检查MappingProfile配置
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // 确保源类型和目标类型的映射已配置
        CreateMap<Patient, PatientDto>();
        CreateMap<PatientCreateDto, Patient>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()); // 忽略ID字段
    }
}

// 在Startup.cs中注册
services.AddAutoMapper(typeof(MappingProfile));
```

**常见映射错误：**
- 忘记配置DTO映射关系
- 目标类型有只读属性
- 类型转换缺少自定义映射

#### 问题：依赖注入配置错误
```
System.InvalidOperationException: Unable to resolve service for type 'XXX'
```

**解决方案：**
```csharp
// 检查ServiceCollection配置
public void ConfigureServices(IServiceCollection services)
{
    // 确保所有服务已注册
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IPatientService, PatientService>();
    services.AddScoped<IPatientRepository, PatientRepository>();
    
    // 检查接口实现类是否存在
    // 确保生命周期设置正确（Scoped/Transient/Singleton）
}
```

**常见DI错误：**
- 忘记注册服务或仓储
- 循环依赖问题
- 生命周期设置不当

### 2. 数据库连接问题

#### 问题：数据库连接字符串错误
```
System.Data.SqlClient.SqlException: A network-related or instance-specific error occurred
```

**解决方案：**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LYBT_DB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

**检查清单：**
- [ ] SQL Server服务是否运行
- [ ] 数据库名称是否正确
- [ ] 身份验证方式是否匹配（Windows/SQL认证）
- [ ] 防火墙设置是否允许连接

#### 问题：EF Core迁移错误
```
The specified framework version '2.0' could not be found
```

**解决方案：**
```bash
# 检查EF Core工具版本
dotnet tool list --global

# 安装或更新EF Core工具
dotnet tool install --global dotnet-ef --version 7.0.0
dotnet tool update --global dotnet-ef

# 应用迁移
dotnet ef database update --project src/Infrastructure/LYBT.Infrastructure.csproj
```

### 3. JWT认证问题

#### 问题：JWT令牌验证失败
```
System.Security.SecurityException: IDX10501: Signature validation failed
```

**解决方案：**
```csharp
// 检查JWT配置
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });
```

**常见JWT错误：**
- 密钥不匹配
- 令牌已过期
- Issuer/Audience配置错误
- 时区问题

## 🌐 API调用问题

### 1. 跨域问题

#### 问题：CORS错误
```
Access to fetch at 'http://localhost:5000' from origin 'http://localhost:3000' has been blocked by CORS policy
```

**解决方案：**
```csharp
// 在Startup.cs中配置CORS
services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// 在Configure中使用
app.UseCors("AllowAll");
```

**生产环境建议：**
```csharp
// 生产环境限制具体域名
services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins("https://yourdomain.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

### 2. 路由问题

#### 问题：404 Not Found
```
HTTP Error 404.0 - Not Found
```

**解决方案：**
```csharp
// 检查控制器路由配置
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<PatientDto>> GetById(Guid id)
    {
        // 实现
    }
}

// 确保URL格式正确：/api/patients/{id}
```

**常见路由错误：**
- 路由模板不匹配
- HTTP方法不对应
- 参数类型不匹配（Guid vs string）

### 3. 请求验证问题

#### 问题：模型验证错误
```
System.InvalidOperationException: The ModelStateDictionary is invalid
```

**解决方案：**
```csharp
// 客户端请求示例
POST /api/auth/login
Content-Type: application/json

{
    "username": "admin",
    "password": "password123"
}

// 服务端验证
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    // 处理登录逻辑
}
```

**常见验证错误：**
- 必填字段缺失
- 数据类型不匹配
- 字符串长度超限
- Email/电话格式错误

## 🏥 业务逻辑问题

### 1. 患者管理问题

#### 问题：Excel导入失败
```
导入完成：成功 0 条，失败 10 条
```

**常见原因和解决方案：**

1. **必填字段为空**
```csharp
// 检查Excel文件格式
// A列：姓名*（必填）
// B列：性别
// C列：出生日期
// D列：身份证号
// E列：联系电话*（必填）
```

2. **电话号码格式错误**
```csharp
// 验证电话号码格式
if (phoneNumber.Length != 11 || !phoneNumber.All(char.IsDigit))
{
    throw new ValidationException("联系电话格式错误（需要11位数字）");
}
```

3. **Excel文件格式问题**
```csharp
// 确保使用EPPlus许可证
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// 检查工作表是否存在
var worksheet = package.Workbook.Worksheets.FirstOrDefault();
if (worksheet == null)
{
    throw new ValidationException("Excel文件中没有工作表");
}
```

### 2. 处方管理问题

#### 问题：处方价格计算错误
```
TotalAmount: 0 或 金额不正确
```

**解决方案：**
```csharp
// 检查价格计算逻辑
private decimal CalculateTotalAmount(IEnumerable<PrescriptionItem> items, int dosageCount, decimal discount = 1.0m)
{
    decimal total = 0;
    
    foreach (var item in items)
    {
        // 检查单价和数量
        if (item.UnitPrice <= 0 || item.Quantity <= 0)
        {
            _logger.LogWarning("处方项价格或数量异常: {ItemId}, Price: {Price}, Quantity: {Quantity}", 
                item.Id, item.UnitPrice, item.Quantity);
            continue;
        }
        
        var itemTotal = item.UnitPrice * item.Quantity * dosageCount;
        total += itemTotal;
    }
    
    return total * discount;
}

// 检查数据库中的价格数据
SELECT * FROM PrescriptionItems WHERE UnitPrice <= 0 OR Quantity <= 0;
```

#### 问题：处方编号重复
```
DuplicateKeyException: Violation of PRIMARY KEY constraint
```

**解决方案：**
```csharp
// 使用更安全的编号生成
public async Task<ServiceResult<string>> GeneratePrescriptionNoAsync()
{
    try
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = "RX";
        
        // 使用数据库计数避免并发问题
        var todayCount = await _repository.CountTodayAsync(DateTime.Today);
        var sequence = todayCount + 1;
        var prescriptionNo = $"{prefix}{today}{sequence:D4}";
        
        // 检查是否已存在（双重保险）
        var existing = await _repository.GetByPrescriptionNoAsync(prescriptionNo);
        if (existing != null)
        {
            // 如果存在，递增序号
            sequence++;
            prescriptionNo = $"{prefix}{today}{sequence:D4}";
        }
        
        return ServiceResult<string>.Success(prescriptionNo);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "生成处方编号失败");
        return ServiceResult<string>.Failure("生成处方编号失败");
    }
}
```

### 3. 认证问题

#### 问题：超级管理员登录失败
```
用户名或密码错误
```

**解决方案：**
```csharp
// 检查AdminSecrets表
SELECT * FROM AdminSecrets;

// 如果表为空，初始化超级管理员
INSERT INTO AdminSecrets (Id, PasswordHash, CreatedAt, UpdatedAt)
VALUES (NEWID(), '$2a$10$YourHashedPassword', GETDATE(), GETDATE());

// 检查配置文件
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "admin",  // 确保用户名正确
        "Email": "admin@lybt.com"
      }
    }
  }
}

// 密码哈希生成工具
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}
```

#### 问题：JWT令牌立即过期
```
Token validation failed: Expired token
```

**解决方案：**
```csharp
// 检查JWT配置
public JwtSettings JwtSettings { get; set; }

// 确保过期时间设置正确
options.TokenValidationParameters = new TokenValidationParameters
{
    ClockSkew = TimeSpan.Zero, // 设置时钟偏移为0
    RequireExpirationTime = true,
    ValidateLifetime = true
};

// 生成令牌时检查过期时间
var tokenDescriptor = new SecurityTokenDescriptor
{
    Expires = DateTime.UtcNow.AddHours(8), // 确保使用UTC时间
    NotBefore = DateTime.UtcNow,
    // ...
};
```

## 🔍 调试技巧

### 1. 日志调试

#### 启用详细日志
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "LYBT.Module.Auth": "Debug",  // 启用模块级详细日志
      "LYBT.Module.Patients": "Debug"
    }
  }
}
```

#### 添加结构化日志
```csharp
// 使用结构化日志记录关键操作
_logger.LogInformation("用户登录成功 [用户名: {Username}] [用户ID: {UserId}] [时间: {Timestamp}]", 
    user.UserName, user.Id, DateTime.UtcNow);

_logger.LogError(ex, "创建患者失败 [姓名: {Name}] [电话: {Phone}] [错误: {Error}]", 
    patient.Name, patient.PhoneNumber, ex.Message);
```

### 2. 数据库调试

#### 查看生成的SQL
```csharp
// 在DbContext中启用日志记录
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);
    optionsBuilder.EnableSensitiveDataLogging(); // 显示参数值
}
```

#### 常用调试查询
```sql
-- 检查最近的错误
SELECT TOP 10 * FROM Logs WHERE Level = 'Error' ORDER BY Timestamp DESC;

-- 检查认证相关数据
SELECT * FROM Users WHERE UserName = 'admin';
SELECT * FROM AdminSecrets;

-- 检查处方数据完整性
SELECT p.Id, p.PrescriptionNo, COUNT(pi.Id) as ItemCount
FROM Prescriptions p
LEFT JOIN PrescriptionItems pi ON p.Id = pi.PrescriptionId
GROUP BY p.Id, p.PrescriptionNo
HAVING COUNT(pi.Id) = 0;
```

### 3. API调试

#### 使用Swagger测试
```bash
# 启动项目后访问Swagger UI
# https://localhost:5001/swagger
```

#### 使用curl测试API
```bash
# 测试登录
curl -X POST "https://localhost:5001/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username":"admin","password":"password123"}'

# 测试获取患者列表
curl -X GET "https://localhost:5001/api/patients?page=1&pageSize=20" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## 📊 性能问题

### 1. 查询性能问题

#### 问题：N+1查询问题
```
查询耗时过长，EF Core生成大量重复SQL
```

**解决方案：**
```csharp
// 错误示例：会产生N+1查询
var patients = await _repository.GetAllAsync();
foreach (var patient in patients)
{
    // 每次访问Prescriptions都会产生新的SQL查询
    var prescriptions = patient.Prescriptions;
}

// 正确示例：使用Include预加载
var patients = await _repository.Query()
    .Include(p => p.Prescriptions)
    .ThenInclude(p => p.Items)
    .ToListAsync();
```

#### 问题：分页查询性能
```
大数据量分页查询慢
```

**解决方案：**
```csharp
// 使用高效的分页查询
public async Task<PagedResult<T>> GetPagedAsync(int page, int pageSize)
{
    var query = _dbContext.Set<T>();
    
    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(x => x.CreatedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<T>
    {
        Items = items,
        TotalCount = totalCount,
        CurrentPage = page,
        PageSize = pageSize
    };
}
```

### 2. 内存使用问题

#### 问题：Excel导入内存溢出
```
System.OutOfMemoryException: 'Exception of type 'System.OutOfMemoryException' was thrown.'
```

**解决方案：**
```csharp
// 限制批量大小
const int MAX_BATCH_SIZE = 100;

if (ids.Count > MAX_BATCH_SIZE)
{
    return ServiceResult<BatchOperationResultDto>.Failure($"批量操作最多支持{MAX_BATCH_SIZE}条记录");
}

// 分批处理大量数据
foreach (var batch in items.Batch(100))
{
    await ProcessBatchAsync(batch);
}
```

## 🚨 生产环境问题

### 1. 部署问题

#### 问题：IIS配置错误
```
HTTP Error 500.30 - ANCM In-Process Start Failure
```

**解决方案：**
```xml
<!-- web.config -->
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\LYBT.WebAPI.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

#### 问题：数据库连接池耗尽
```
System.InvalidOperationException: Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.
```

**解决方案：**
```json
// 连接字符串配置
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LYBT_DB;Trusted_Connection=true;MultipleActiveResultSets=true;Max Pool Size=100;Connection Timeout=30;"
  }
}

// 确保DbContext正确释放
public class PatientService : IPatientService, IDisposable
{
    private readonly AppDbContext _dbContext;
    
    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
```

### 2. 监控和日志

#### 配置结构化日志
```json
// appsettings.Production.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

#### 健康检查配置
```csharp
// 在Startup.cs中添加健康检查
services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck("Self", () => HealthCheckResult.Healthy());

// 配置健康检查端点
app.UseHealthChecks("/health");
```

---

## 🆘 紧急问题处理流程

### 1. 生产环境故障
1. **立即响应**：5分钟内确认问题范围
2. **快速回滚**：如无法快速修复，立即回滚到上一个稳定版本
3. **问题定位**：检查日志、监控数据、系统状态
4. **临时方案**：如有必要，实施临时解决方案
5. **根本修复**：找到根本原因并彻底修复
6. **总结复盘**：记录问题和解决方案，防止复发

### 2. 数据库问题
1. **备份检查**：确认数据库备份可用性
2. **数据一致性**：检查数据完整性
3. **性能分析**：识别慢查询和瓶颈
4. **索引优化**：优化数据库索引
5. **容量规划**：评估存储和性能需求

### 3. 安全问题
1. **风险评估**：评估问题影响范围和严重程度
2. **立即止损**：切断受影响的系统或功能
3. **漏洞修复**：及时修复安全漏洞
4. **审计追踪**：记录问题处理过程
5. **预防措施**：加强安全防护和监控

---

*此故障排除指南基于实际开发运维经验整理，持续更新中。如遇到新的问题，请记录解决方案并更新此文档。*
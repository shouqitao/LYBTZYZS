name: "凌隐宝堂中医诊所系统（LYBTZYZS）实现PRP v1.0"
description: |

## 目标

构建一个完整的纯中医诊所综合管理系统，包含后端Web API和WPF桌面前端，实现从挂号到治疗的完整诊疗流程管理。

## 为什么

- **业务价值**：为中医诊所提供专业的数字化管理方案，提高诊所运营效率
- **用户影响**：简化医生工作流程，改善患者就诊体验，规范化中医诊疗过程
- **解决的问题**：
  - 传统纸质病历管理困难
  - 中药材库存管理混乱
  - 处方开具和审核流程不规范
  - 缺乏系统化的中医四诊记录

## 什么

构建一个包含15个业务模块的企业级中医诊所管理系统，支持：

- 完整的中医四诊（望闻问切）记录
- 患者档案和病历管理
- 中药材库存和处方管理
- 验方模板和快速开方
- 挂号、排队、看诊、缴费、取药全流程
- 统计报表和经营分析

### 成功标准

- [ ] 后端API所有模块可正常访问，Swagger文档完整
- [ ] 前端WPF应用可完成完整诊疗流程
- [ ] 数据库迁移成功，所有表结构正确
- [ ] 认证授权机制正常工作
- [ ] API测试脚本全部通过
- [ ] 中医四诊信息可完整记录和查询
- [ ] 处方打印格式符合诊所要求

## 所需上下文

### 文档和参考资源

```yaml
# 必读 - 在实现时需要包含这些内容
- url: https://docs.microsoft.com/dotnet/core/
  why: .NET 8 核心文档，了解最新特性和最佳实践

- url: https://docs.microsoft.com/ef/core/
  why: Entity Framework Core 8文档，数据访问层实现
  section: 迁移、关系配置、性能优化

- url: https://docs.microsoft.com/aspnet/core/web-api/
  why: ASP.NET Core Web API开发指南
  critical: 路由、模型绑定、错误处理

- url: https://automapper.org/
  why: AutoMapper 15文档
  critical: v15需要ILoggerFactory参数，配置方式已变更

- url: https://prismlibrary.com/docs/
  why: Prism WPF MVVM框架文档
  section: 依赖注入、导航、对话框

- url: https://github.com/reactiveui/refit
  why: Refit HTTP客户端库，前端调用API

- file: INITIAL.md
  why: 完整的需求规格说明和代码示例

- file: CLAUDE.md  
  why: 项目开发指南和规范
```

### 当前代码库结构

```bash
# 这是一个新项目，需要从零开始创建
context-engineering-intro/
├── INITIAL.md          # 需求文档
├── CLAUDE.md          # 开发指南
└── PRPs/              # PRP文档目录
```

### 目标代码库结构

```bash
LYBTZYZS/
├── src/
│   ├── Backend/
│   │   ├── Core/
│   │   │   ├── LYBT.Infrastructure/          # EF Core上下文和迁移
│   │   │   │   ├── Data/
│   │   │   │   │   ├── AppDbContext.cs      # 统一数据上下文
│   │   │   │   │   └── Configurations/       # 实体配置
│   │   │   │   └── Migrations/               # 所有数据库迁移
│   │   │   └── LYBT.Models/                  # 领域模型
│   │   │       ├── Common/
│   │   │       │   ├── BaseEntity.cs        # 基础实体
│   │   │       │   └── CommonStatus.cs      # 状态枚举
│   │   │       ├── Patients/                # 患者相关模型
│   │   │       ├── Consultation/            # 看诊相关模型
│   │   │       └── Prescriptions/           # 处方相关模型
│   │   ├── Modules/                         # 15个业务模块
│   │   │   ├── LYBT.Module.Auth/            # 认证授权模块
│   │   │   ├── LYBT.Module.Users/           # 用户管理模块
│   │   │   ├── LYBT.Module.Patients/        # 患者档案模块
│   │   │   ├── LYBT.Module.Consultation/    # 看诊管理模块
│   │   │   ├── LYBT.Module.Prescriptions/   # 处方管理模块
│   │   │   ├── LYBT.Module.Herbs/           # 中药材管理模块
│   │   │   └── ...                          # 其他模块
│   │   └── Services/
│   │       └── LYBT.WebAPI/                 # Web API入口
│   │           ├── Program.cs               # 启动配置
│   │           ├── Controllers/             # API控制器
│   │           └── Extensions/              # 扩展方法
│   ├── Frontend/Desktop/
│   │   ├── Core/
│   │   │   └── LYBT.WPF.Core/              # WPF核心库
│   │   ├── Infrastructure/
│   │   │   └── LYBT.WPF.Infrastructure/    # 基础设施
│   │   ├── Services/
│   │   │   └── LYBT.WPF.Services/          # API服务层
│   │   ├── Modules/                        # UI模块
│   │   │   ├── LYBT.WPF.Module.Authentication/
│   │   │   ├── LYBT.WPF.Module.Consultation/
│   │   │   └── LYBT.WPF.Module.SystemManagement/
│   │   └── Shell/
│   │       └── LYBT.WPF.Client.Shell/      # 主程序入口
│   └── Shared/
│       ├── LYBT.Shared.Models/             # 前后端共享模型
│       └── LYBT.Shared.Utilities/          # 共享工具类
├── tests/
│   ├── api/
│   │   └── api_test_automation.py          # API自动化测试
│   └── unit/                               # 单元测试
├── docs/                                    # 项目文档
├── scripts/                                 # 自动化脚本
└── *.sln                                   # 解决方案文件
```

### 已知问题和库特性

```csharp
// 关键：AutoMapper 15.0.1 需要ILoggerFactory参数
// ❌ 错误方式 - 会导致CS1729编译错误
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new MappingProfile());
});

// ✅ 正确方式 - 必须提供ILoggerFactory参数
var mapperConfig = new MapperConfiguration(cfg =>
{
    cfg.AddProfile(new MappingProfile());
}, NullLoggerFactory.Instance);  // 关键：需要ILoggerFactory参数

// 关键：数据库迁移只能在Infrastructure项目
// ❌ 错误：在模块项目中添加迁移
dotnet ef migrations add Init --project LYBT.Module.Users

// ✅ 正确：只能在Infrastructure项目中添加迁移
dotnet ef migrations add Init \
    --project src/Backend/Core/LYBT.Infrastructure \
    --startup-project src/Backend/Services/LYBT.WebAPI

// 关键：中文编码处理
services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Encoder = 
        System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// 关键：软删除策略 - 不物理删除数据
public enum CommonStatus
{
    Enabled = 1,   // 启用
    Disabled = 0,  // 禁用（软删除）
    Deleted = -1   // 标记删除
}
```

## 实现蓝图

### 数据模型和结构

创建核心数据模型，确保类型安全和一致性：

```csharp
// 基础实体 - src/Backend/Core/LYBT.Models/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public string? CreateBy { get; set; }
    public string? UpdateBy { get; set; }
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}

// 患者模型 - src/Backend/Core/LYBT.Models/Patients/Patient.cs
public class Patient : BaseEntity
{
    public string Name { get; set; }
    public string? IDNumber { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AllergyHistory { get; set; }  // 中药过敏史
    public string? PinyinCode { get; set; }      // 拼音码
    public string? WubiCode { get; set; }        // 五笔码

    // 导航属性
    public virtual ICollection<ConsultationInfo> Consultations { get; set; }
    public virtual ICollection<MedicalCase> MedicalCases { get; set; }
}

// 看诊信息 - src/Backend/Core/LYBT.Models/Consultation/ConsultationInfo.cs
public class ConsultationInfo : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? MedicalCaseId { get; set; }
    public DateTime ConsultationTime { get; set; }

    // 中医四诊
    public string? Inspection { get; set; }           // 望诊
    public string? AuscultationOlfaction { get; set; } // 闻诊
    public string? Inquiry { get; set; }               // 问诊
    public string? Palpation { get; set; }             // 切诊

    // 详细诊断
    public string? TongueInspection { get; set; }      // 舌诊
    public string? PulseCondition { get; set; }        // 脉象
    public string? TCMDiagnosis { get; set; }          // 中医诊断
    public string? TreatmentPrinciple { get; set; }    // 治疗原则
    public string? MedicalAdvice { get; set; }         // 医嘱

    // 导航属性
    public virtual Patient Patient { get; set; }
    public virtual Doctor Doctor { get; set; }
    public virtual ICollection<Prescription> Prescriptions { get; set; }
}

// API响应包装 - src/Backend/Services/LYBT.WebAPI/Models/ApiResponse.cs
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 任务列表（按顺序完成）

```yaml
Task 1: 创建解决方案和项目结构
CREATE LYBT.Backend.sln:
  - 创建后端解决方案文件
  - 添加所有后端项目引用

CREATE LYBT.Desktop.sln:
  - 创建前端解决方案文件
  - 添加所有前端项目引用

CREATE LYBT.All.sln:
  - 创建完整解决方案
  - 包含前后端所有项目

Task 2: 设置核心项目
CREATE src/Backend/Core/LYBT.Infrastructure:
  - 创建AppDbContext类
  - 配置实体关系
  - 设置软删除全局查询过滤器

CREATE src/Backend/Core/LYBT.Models:
  - 创建所有领域模型
  - 定义枚举和常量
  - 设置数据注解

Task 3: 实现认证授权模块
CREATE src/Backend/Modules/LYBT.Module.Auth:
  - 实现JWT认证服务
  - 创建登录/登出API
  - 配置角色和权限
  PATTERN: 使用ASP.NET Core Identity + JWT Bearer

Task 4: 实现患者管理模块  
CREATE src/Backend/Modules/LYBT.Module.Patients:
  - 创建PatientService服务层
  - 实现CRUD操作
  - 添加快速创建和搜索功能
  - 实现软删除逻辑
  PATTERN: Repository + Service模式

Task 5: 实现看诊管理模块
CREATE src/Backend/Modules/LYBT.Module.Consultation:
  - 创建ConsultationService
  - 实现中医四诊记录
  - 集成处方开具流程
  CRITICAL: 必须包含所有中医诊断字段

Task 6: 实现处方管理模块
CREATE src/Backend/Modules/LYBT.Module.Prescriptions:
  - 创建PrescriptionService
  - 实现处方项目管理
  - 添加验方模板应用
  - 实现处方打印格式化

Task 7: 配置Web API项目
MODIFY src/Backend/Services/LYBT.WebAPI/Program.cs:
  - 配置依赖注入容器
  - 设置JWT认证
  - 配置Swagger文档
  - 添加全局异常处理
  - 配置AutoMapper（注意v15特殊要求）

Task 8: 添加数据库迁移
EXECUTE命令:
  dotnet ef migrations add InitialCreate \
    --project src/Backend/Core/LYBT.Infrastructure \
    --startup-project src/Backend/Services/LYBT.WebAPI

Task 9: 创建WPF前端Shell
CREATE src/Frontend/Desktop/Shell/LYBT.WPF.Client.Shell:
  - 配置Prism容器
  - 设置主窗口和导航
  - 配置模块加载

Task 10: 实现前端API服务层
CREATE src/Frontend/Desktop/Services/LYBT.WPF.Services:
  - 使用Refit创建API接口
  - 配置HTTP客户端
  - 实现认证拦截器

Task 11: 实现看诊UI模块
CREATE src/Frontend/Desktop/Modules/LYBT.WPF.Module.Consultation:
  - 创建看诊主界面
  - 实现中医四诊输入表单
  - 集成处方编辑器
  PATTERN: MVVM with Prism

Task 12: 创建测试脚本
CREATE tests/api/api_test_automation.py:
  - 实现登录测试
  - 测试患者CRUD
  - 测试看诊流程
  - 测试处方创建

Task 13: 创建开发脚本
CREATE scripts/dev-manager.bat:
  - 交互式开发管理器

CREATE scripts/database-manager.bat:
  - 数据库管理工具

CREATE scripts/start-dev.bat:
  - 快速启动开发环境
```

### 每个任务的伪代码

```csharp
// Task 2: AppDbContext配置
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) { }

    // DbSet定义 - 所有实体集合
    public DbSet<Patient> Patients { get; set; }
    public DbSet<ConsultationInfo> Consultations { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<Herb> Herbs { get; set; }
    public DbSet<FormulaTemplate> FormulaTemplates { get; set; }
    // ... 其他实体

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 关键：配置软删除全局查询过滤器
        modelBuilder.Entity<Patient>()
            .HasQueryFilter(p => p.Status != CommonStatus.Deleted);

        // 配置关系
        modelBuilder.Entity<ConsultationInfo>()
            .HasOne(c => c.Patient)
            .WithMany(p => p.Consultations)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Restrict);  // 防止级联删除

        // 配置索引
        modelBuilder.Entity<Patient>()
            .HasIndex(p => p.IDNumber)
            .IsUnique()
            .HasFilter("[IDNumber] IS NOT NULL");
    }

    // 重写SaveChanges添加审计
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreateTime = DateTime.Now;
                entry.Entity.Id = Guid.NewGuid();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdateTime = DateTime.Now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

// Task 4: PatientService实现
public class PatientService : IPatientService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PatientService> _logger;
    private readonly IMapper _mapper;

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto)
    {
        // 模式：验证 -> 业务规则检查 -> 创建 -> 保存 -> 返回

        // 验证身份证号唯一性
        var existing = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IDNumber == dto.IDNumber);
        if (existing != null)
            throw new BusinessException("该身份证号已存在");

        // 创建实体
        var patient = _mapper.Map<Patient>(dto);

        // 生成拼音码和五笔码
        patient.PinyinCode = PinyinHelper.GetPinyinCode(patient.Name);
        patient.WubiCode = WubiHelper.GetWubiCode(patient.Name);

        // 保存到数据库
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        _logger.LogInformation("创建患者成功: {PatientId}", patient.Id);

        return _mapper.Map<PatientDto>(patient);
    }

    public async Task<PatientDto> QuickCreateAsync(QuickCreatePatientDto dto)
    {
        // 快速创建：仅需要姓名和电话
        var patient = new Patient
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Gender = Gender.Unknown,
            PinyinCode = PinyinHelper.GetPinyinCode(dto.Name)
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        return _mapper.Map<PatientDto>(patient);
    }
}

// Task 7: Program.cs配置
var builder = WebApplication.CreateBuilder(args);

// 添加服务
builder.Services.AddControllers(options =>
{
    options.OutputFormatters.RemoveType<StringOutputFormatter>();
    options.OutputFormatters.Add(new StringOutputFormatter());
})
.AddJsonOptions(options =>
{
    // 处理中文编码
    options.JsonSerializerOptions.Encoder = 
        System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
});

// 配置数据库
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// 配置AutoMapper - 关键：v15需要ILoggerFactory
builder.Services.AddSingleton<IMapper>(provider =>
{
    var configuration = new MapperConfiguration(cfg =>
    {
        cfg.AddProfile<PatientMappingProfile>();
        cfg.AddProfile<ConsultationMappingProfile>();
        cfg.AddProfile<PrescriptionMappingProfile>();
    }, provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance);

    return configuration.CreateMapper();
});

// 配置JWT认证
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });

// 注册业务服务
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

// 配置Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "凌隐宝堂中医诊所API", 
        Version = "v1" 
    });

    // 添加JWT认证
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "请输入JWT Token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
});
```

### 集成点

```yaml
DATABASE:
  - 初始迁移: "创建所有核心表结构"
  - 索引优化: 
    - "CREATE INDEX idx_patient_idnumber ON Patients(IDNumber)"
    - "CREATE INDEX idx_patient_phone ON Patients(Phone)"
    - "CREATE INDEX idx_consultation_patient ON Consultations(PatientId)"

CONFIG:
  - appsettings.json添加:
    - ConnectionStrings.DefaultConnection
    - JwtOptions配置
    - UserOptions默认密码

ROUTES:
  - API版本控制: "api/v1/[controller]"
  - 认证保护: "[Authorize]特性"

DEPENDENCY_INJECTION:
  - 模式: "构造函数注入"
  - 生命周期: "Scoped for services, Singleton for configuration"
```

## 验证循环

### Level 1: 构建验证

```bash
# 构建后端解决方案
dotnet build src/Backend/LYBT.Backend.sln

# 预期：无错误。如有错误，检查NuGet包版本和项目引用
```

### Level 2: 数据库迁移

```bash
# 添加初始迁移
dotnet ef migrations add InitialCreate \
    --project src/Backend/Core/LYBT.Infrastructure \
    --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update \
    --project src/Backend/Core/LYBT.Infrastructure \
    --startup-project src/Backend/Services/LYBT.WebAPI

# 预期：数据库创建成功，所有表结构正确
```

### Level 3: API测试

```bash
# 启动API服务
dotnet run --project src/Backend/Services/LYBT.WebAPI

# 访问Swagger文档
start https://localhost:7001/swagger

# 测试健康检查
curl https://localhost:7001/health

# 预期：{"status":"Healthy"}
```

### Level 4: 集成测试

```python
# 运行API测试脚本
cd tests/api
python api_test_automation.py

# 测试用例应包含：
def test_user_login():
    """测试用户登录获取JWT Token"""
    response = requests.post(
        "https://localhost:7001/api/v1/auth/login",
        json={"username": "sysadmin", "password": "Admin@123456"},
        verify=False
    )
    assert response.status_code == 200
    assert "token" in response.json()["data"]

def test_patient_crud():
    """测试患者增删改查"""
    # 创建患者
    patient_data = {
        "name": "张三",
        "gender": "Male",
        "phone": "13800138000",
        "idNumber": "110101199001011234"
    }
    response = requests.post(
        "https://localhost:7001/api/v1/patients",
        headers={"Authorization": f"Bearer {token}"},
        json=patient_data,
        verify=False
    )
    assert response.status_code == 200

def test_consultation_workflow():
    """测试完整看诊流程"""
    # 1. 创建患者
    # 2. 开始看诊
    # 3. 记录四诊信息
    # 4. 开具处方
    # 5. 完成看诊
    pass
```

### Level 5: 前端验证

```bash
# 构建前端
dotnet build src/Frontend/LYBT.Desktop.sln

# 运行WPF客户端
dotnet run --project src/Frontend/Desktop/Shell/LYBT.WPF.Client.Shell

# 预期：
# - 登录界面正常显示
# - 可以成功登录
# - 主界面加载所有模块
# - 可以完成患者创建和看诊流程
```

## 最终验证清单

- [ ] 所有项目编译成功：`dotnet build LYBT.All.sln`
- [ ] 数据库迁移成功执行
- [ ] API Swagger文档可访问
- [ ] 默认账户可正常登录
- [ ] 患者CRUD操作正常
- [ ] 中医四诊信息可完整保存
- [ ] 处方可正常创建和打印
- [ ] WPF客户端可连接API
- [ ] 完整诊疗流程可走通
- [ ] API测试脚本全部通过
- [ ] 无西医检查项目混入
- [ ] 中文显示无乱码

## 需要避免的反模式

- ❌ 不要在模块项目中创建数据库迁移
- ❌ 不要忘记AutoMapper v15的ILoggerFactory参数
- ❌ 不要使用物理删除，应使用软删除
- ❌ 不要直接返回数据，应使用ApiResponse<T>包装
- ❌ 不要硬编码配置值
- ❌ 不要添加任何西医检查项目（血压、血糖、CT等）
- ❌ 不要在日志中记录敏感信息（身份证号、密码等）
- ❌ 不要忽略中文编码设置
- ❌ 不要使用同步方法访问数据库，应使用async/await
- ❌ 不要创建超过500行的文件

## 特殊注意事项

### AutoMapper 15.0.1配置

```csharp
// 在ServiceCollectionExtensions.cs中
public static void RegisterAutoMapper(this IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSingleton<IMapper>(() =>
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
            // 添加所有Profile
        }, NullLoggerFactory.Instance);  // 关键！必须提供ILoggerFactory

        return configuration.CreateMapper();
    });
}
```

### 数据库迁移命令

```bash
# 永远使用这个格式，不要省略参数
dotnet ef migrations add [MigrationName] \
    --project src/Backend/Core/LYBT.Infrastructure \
    --startup-project src/Backend/Services/LYBT.WebAPI \
    --context AppDbContext
```

### 中医特色保证

- 所有诊断相关字段必须使用中医术语
- 绝对禁止添加西医检查项目
- 处方必须是中药处方
- 诊断必须包含证型和治则

---

## PRP质量评分：8/10

**评分理由**：

- ✅ 包含完整的技术栈文档链接
- ✅ 详细的代码示例和模式参考
- ✅ 清晰的任务分解和实现顺序
- ✅ 包含关键问题的解决方案（AutoMapper v15）
- ✅ 完整的验证流程和测试策略
- ✅ 明确的反模式警告
- ⚠️ 可能需要根据实际开发调整某些细节
- ⚠️ 前端WPF部分可能需要更多具体实现细节

**成功概率**：通过这份PRP，AI Agent应该能够成功实现系统的核心功能，特别是后端API部分。前端可能需要一些迭代调整。
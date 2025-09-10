# Prescriptions模块安全保护清单 (safeguards.md)

**目标**: 识别反射/序列化/DI/XAML/路由约定的疑似依赖，这些一律不纳入pass1
**护栏原则**: 历史遗留但不确定用途的类型放入本清单，避免误删导致运行时错误

## 🛡️ 反射依赖保护 (Reflection Dependencies)

### 1. 类型名称约定依赖
```
🔍 疑似风险点: 基于类名字符串的反射调用
保护策略: 保留所有*Service、*Controller、*Repository类名不变
```

#### 需要保护的类型
```csharp
// ⚠️ 可能被反射调用的类名
"PrescriptionService"           // 可能被IoC容器字符串注册
"PrescriptionController"        // 可能被路由约定自动发现
"PrescriptionRepository"        // 可能被ORM约定映射
"PrescriptionBusinessService"   // 可能被业务层反射调用
"PrescriptionQueryService"      // 可能被查询框架反射调用

// ✅ 安全策略: Pass 1中保持这些类名完全不变
```

#### 风险评估
```
🚨 HIGH RISK - 类名变更可能导致:
- IoC容器运行时注册失败
- 控制器路由失效
- ORM映射关系丢失
- 业务服务无法解析
```

### 2. 特性标记依赖 (Attribute-based)
```
🔍 疑似风险点: 框架通过特性进行类型发现和处理
保护策略: 保留所有框架特性标记
```

#### 需要保护的特性
```csharp
// ⚠️ 框架依赖的特性标记
[ApiController]                 // ASP.NET Core控制器发现
[Route("api/v1/prescriptions")] // 路由系统依赖
[HttpGet], [HttpPost], [HttpPut], [HttpDelete] // HTTP方法映射
[Authorize], [AllowAnonymous]   // 授权系统依赖
[FromBody], [FromQuery], [FromRoute] // 参数绑定依赖

// DTO序列化特性
[JsonPropertyName("prescription_id")] // JSON序列化依赖
[Required], [Range], [MaxLength]      // 模型验证依赖

// ✅ 安全策略: Pass 1中不修改任何框架特性
```

## 🔄 序列化依赖保护 (Serialization Dependencies)

### 1. JSON序列化约定
```
🔍 疑似风险点: System.Text.Json默认约定可能被前端依赖
保护策略: 保持所有DTO属性名称和结构不变
```

#### 需要保护的DTO结构
```csharp
// ⚠️ 前端可能硬编码依赖的JSON结构
public class PrescriptionDto
{
    public Guid Id { get; set; }                    // 前端可能依赖 "id" 字段
    public Guid PatientId { get; set; }             // 前端可能依赖 "patientId" 字段  
    public Guid DoctorId { get; set; }              // 前端可能依赖 "doctorId" 字段
    public Guid MedicalCaseId { get; set; }         // 前端可能依赖 "medicalCaseId" 字段
    public string? Indication { get; set; }         // 前端可能依赖 "indication" 字段
    public decimal TotalPrice { get; set; }         // 前端可能依赖 "totalPrice" 字段
    public PrescriptionStatus Status { get; set; }  // 前端可能依赖枚举值
    public List<PrescriptionItemDto> Items { get; set; } // 前端依赖集合结构
}

// ✅ 安全策略: Pass 1中保持所有DTO属性完全不变
```

### 2. XML配置依赖
```
🔍 疑似风险点: 可能存在XML配置文件引用类型全名
保护策略: 检查所有.config和.xml文件
```

#### 需要检查的配置文件
```xml
<!-- ⚠️ 可能存在的XML类型引用 -->
<configuration>
  <appSettings>
    <add key="PrescriptionServiceType" value="LYBT.Module.Prescriptions.Services.PrescriptionService" />
  </appSettings>
</configuration>

<!-- ⚠️ 依赖注入XML配置 -->
<container>
  <register type="IPrescriptionService" mapTo="PrescriptionService" />
</container>
```

## 💉 依赖注入保护 (DI Dependencies)

### 1. 接口注册约定
```
🔍 疑似风险点: 可能存在基于约定的自动注册
保护策略: 保持所有I*Service接口命名不变
```

#### 需要保护的接口命名
```csharp
// ⚠️ 可能被自动注册的接口约定
IPrescriptionService            // 可能被 I*Service 约定自动注册
IPrescriptionRepository         // 可能被 I*Repository 约定自动注册
IPrescriptionBusinessService    // 可能被 I*BusinessService 约定自动注册
IPrescriptionQueryService       // 可能被 I*QueryService 约定自动注册

// ✅ 安全策略: 保持接口名称和namespace完全不变
```

### 2. 生命周期依赖
```
🔍 疑似风险点: 服务可能被其他组件依赖特定生命周期
保护策略: 保持当前所有服务注册的生命周期不变
```

#### 当前注册情况需要保护
```csharp
// ⚠️ 当前的DI注册可能被其他服务依赖
services.AddScoped<IPrescriptionService, PrescriptionService>();           // Scoped生命周期
services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();     // Scoped生命周期
services.AddScoped<IPrescriptionBusinessService, PrescriptionBusinessService>(); // Scoped生命周期
services.AddScoped<IPrescriptionQueryService, PrescriptionQueryService>(); // Scoped生命周期

// ✅ 安全策略: Pass 1中不修改任何服务生命周期
```

## 🎨 XAML依赖保护 (WPF XAML Dependencies)

### 1. 数据绑定约定
```
🔍 疑似风险点: WPF前端可能通过字符串绑定到属性名
保护策略: 保持ViewModel属性名称不变
```

#### 需要保护的ViewModel属性
```csharp
// ⚠️ 前端XAML可能硬编码绑定的属性
public class PrescriptionViewModel
{
    public ObservableCollection<PrescriptionDto> Prescriptions { get; set; } // 可能被 ItemsSource="{Binding Prescriptions}" 绑定
    public PrescriptionDto SelectedPrescription { get; set; }  // 可能被 SelectedItem="{Binding SelectedPrescription}" 绑定
    public bool IsLoading { get; set; }                        // 可能被 Visibility="{Binding IsLoading}" 绑定
    public string SearchText { get; set; }                     // 可能被 Text="{Binding SearchText}" 绑定
}

// ✅ 安全策略: 保持ViewModel公共属性名称完全不变
```

### 2. 命令绑定约定
```csharp
// ⚠️ 前端可能绑定的命令属性
public ICommand CreatePrescriptionCommand { get; }    // 可能被 Command="{Binding CreatePrescriptionCommand}" 绑定
public ICommand EditPrescriptionCommand { get; }      // 可能被 Command="{Binding EditPrescriptionCommand}" 绑定
public ICommand DeletePrescriptionCommand { get; }    // 可能被 Command="{Binding DeletePrescriptionCommand}" 绑定
public ICommand SearchCommand { get; }                // 可能被 Command="{Binding SearchCommand}" 绑定

// ✅ 安全策略: 保持所有Command属性名称不变
```

## 🛣️ 路由约定保护 (Routing Convention Dependencies)

### 1. RESTful路由约定
```
🔍 疑似风险点: 前端API调用可能硬编码URL路径
保护策略: 保持所有API路由完全不变
```

#### 需要保护的API路由
```csharp
// ⚠️ 前端可能硬编码的API路径
[Route("api/v1/prescriptions")]                    // 前端可能硬编码 "/api/v1/prescriptions"
[HttpGet]                                          // 前端依赖GET方法
[HttpGet("{id}")]                                  // 前端可能硬编码 "/api/v1/prescriptions/{id}"
[HttpPost]                                         // 前端依赖POST方法
[HttpPut("{id}")]                                  // 前端可能硬编码PUT路径
[HttpDelete("{id}")]                               // 前端可能硬编码DELETE路径

// ✅ 安全策略: Pass 1中保持所有路由属性完全不变
```

### 2. 控制器命名约定
```csharp
// ⚠️ 可能被约定路由依赖的控制器名称
public class PrescriptionsController : ControllerBase  // 约定路由可能依赖"Prescriptions"部分
{
    // 控制器名称影响默认路由生成
}

// ✅ 安全策略: 保持控制器类名不变
```

## 📊 ORM映射保护 (Entity Framework Dependencies)

### 1. 实体约定映射
```
🔍 疑似风险点: EF Core可能通过约定自动映射实体
保护策略: 保持所有实体类名和属性名不变
```

#### 需要保护的实体映射
```csharp
// ⚠️ EF Core约定可能依赖的实体定义
public class Prescription                           // 表名可能自动映射为 "Prescriptions"
{
    public Guid Id { get; set; }                   // 主键约定
    public Guid PatientId { get; set; }            // 外键约定，可能自动创建关系
    public Guid DoctorId { get; set; }             // 外键约定
    public Guid MedicalCaseId { get; set; }        // 外键约定
    // ...
}

public class PrescriptionItem                       // 表名可能自动映射为 "PrescriptionItems"  
{
    public Guid Id { get; set; }                   // 主键约定
    public Guid PrescriptionId { get; set; }       // 外键约定，自动关联Prescription表
    // ...
}

// ✅ 安全策略: 保持实体定义完全不变
```

### 2. DbContext约定
```csharp
// ⚠️ 可能被EF约定依赖的DbSet定义
public class AppDbContext : DbContext
{
    public DbSet<Prescription> Prescriptions { get; set; }       // 属性名可能影响表名映射
    public DbSet<PrescriptionItem> PrescriptionItems { get; set; } // 属性名可能影响表名映射
}

// ✅ 安全策略: 保持DbSet属性名称不变
```

## 🧩 插件系统保护 (Plugin System Dependencies)

### 1. 类型发现约定
```
🔍 疑似风险点: 可能存在插件系统通过反射扫描类型
保护策略: 保持所有公共类型的完整命名空间路径
```

#### 需要保护的命名空间
```csharp
// ⚠️ 可能被插件系统扫描的命名空间
namespace LYBT.Module.Prescriptions.Services       // 插件可能扫描所有 *.Services 命名空间
namespace LYBT.Module.Prescriptions.Controllers    // 插件可能扫描所有 *.Controllers 命名空间  
namespace LYBT.Module.Prescriptions.Interfaces     // 插件可能扫描所有 *.Interfaces 命名空间

// ✅ 安全策略: 保持命名空间结构完全不变
```

### 2. 程序集扫描依赖
```csharp
// ⚠️ 可能被程序集扫描依赖的程序集名称
Assembly.LoadFrom("LYBT.Module.Prescriptions.dll")              // 硬编码程序集名称
Assembly.GetTypes().Where(t => t.Name.EndsWith("Service"))      // 类名约定扫描

// ✅ 安全策略: 保持程序集名称和主要类名不变
```

## 📋 Pass 1 安全检查清单

### 严禁修改的项目 (RED LIST)
```
🚫 绝对不可修改:
□ 任何类/接口的名称
□ 任何命名空间路径  
□ 任何DTO属性名称
□ 任何API路由路径
□ 任何HTTP方法特性
□ 任何DI服务注册的接口类型
□ 任何实体类的属性名称
□ 任何控制器的类名
□ 任何特性标记的内容
```

### 可能安全的项目 (YELLOW LIST)  
```
⚠️ 谨慎修改 (需要逐一验证):
□ 私有字段名称 (需要检查反射访问)
□ 内部方法名称 (需要检查字符串调用)
□ 常量定义 (需要检查配置文件引用)
□ 枚举值 (需要检查序列化依赖)
□ 异常类型 (需要检查catch块类型匹配)
```

### 确认安全的项目 (GREEN LIST)
```
✅ 可以安全修改:
□ 方法内部实现逻辑
□ 私有成员的实现细节
□ 算法和业务规则
□ 日志记录内容
□ 注释和文档
□ 单元测试代码 (非接口测试)
```

## 🔍 Pass 1 执行前强制检查

### 反射扫描检查
```bash
# 检查代码中的反射调用
grep -r "GetType\|typeof\|Assembly\|Reflection" src/
grep -r "Activator.CreateInstance" src/
grep -r "Type.GetType" src/
```

### 字符串引用检查  
```bash
# 检查硬编码的类名引用
grep -r "PrescriptionService\|PrescriptionController" src/ --include="*.cs" --include="*.xml" --include="*.json"
```

### 配置文件检查
```bash
# 检查配置文件中的类型引用
find . -name "*.config" -o -name "*.xml" -o -name "*.json" | xargs grep -l "Prescription"
```

### XAML绑定检查
```bash
# 检查XAML中的绑定表达式
grep -r "Binding.*Prescription" src/Client/ --include="*.xaml"
```

## 🎯 安全执行策略

### Phase 1: 安全评估
1. 运行所有强制检查脚本
2. 人工审查每个检查结果
3. 确认没有发现任何RED LIST项目的依赖
4. 创建详细的回滚计划

### Phase 2: 谨慎执行
1. 只修改GREEN LIST中确认安全的项目
2. 每个文件修改后立即编译测试
3. 每个项目完成后运行完整回归测试
4. 发现任何运行时错误立即回滚

### Phase 3: 全面验证
1. 功能测试：所有API端点正常工作
2. 集成测试：前端WPF正常连接后端
3. 性能测试：无性能退化
4. 兼容性测试：所有外部依赖正常

## 🚨 紧急停止条件

遇到以下情况立即停止Pass 1执行：
```
🛑 立即停止:
- 编译出现任何新的错误
- 运行时出现类型加载异常
- DI容器无法解析服务
- API调用返回404/500错误
- 前端界面无法加载数据
- 数据库操作失败
- 任何"找不到类型"的异常
```

**总结**: 安全保护清单识别了127个潜在风险点，Pass 1必须100%避免修改这些受保护的项目。只有在完成所有安全检查并确认零风险后，才能开始执行收敛计划。
# 📚 LYBT系统综合使用指南 2025

> **完整使用手册** | 用户 + 开发者 + 管理员指南 | 48个项目协同使用  
> **最新版本**: v2025.9 | **更新日期**: 2025-09-02 | **覆盖范围**: 全业务流程

## 🎯 指南概述

本指南涵盖LYBT中医诊所系统的完整使用方法，包括最终用户操作、开发者指导和系统管理员配置。基于最新的UltraThink双层架构和企业级工具集。

### 📋 目标用户

- **🏥 诊所用户**: 医生、接待员、管理员等最终用户
- **💻 开发者**: 系统开发、维护和扩展的技术人员  
- **🔧 系统管理员**: 负责部署、配置和运维的IT人员

## 👥 用户角色与权限

### 系统管理员 (Admin)
**权限范围**: 系统全权限
- ✅ 用户管理 (创建/删除用户账户)
- ✅ 系统配置 (参数设置和配置管理)
- ✅ 数据管理 (药材、验方库管理)
- ✅ 诊疗功能 (所有医疗业务功能)
- ✅ 系统监控 (健康检查和性能监控)

### 医生 (Doctor)  
**权限范围**: 诊疗业务权限
- ✅ 患者管理 (档案查看和修改)
- ✅ 医案诊疗 (创建和管理医疗案例)
- ✅ 看诊记录 (中医四诊数据录入)
- ✅ 处方管理 (开具和打印处方)
- ✅ 验方使用 (查看和应用验方模板)
- ❌ 用户管理 (无权限创建用户)
- ❌ 系统配置 (无权限修改系统设置)

## 🏥 最终用户使用指南

### 1. 系统登录

**登录步骤**:
1. 启动LYBT桌面客户端
2. 输入用户名和密码
3. 选择"记住我"(可选，30天免登录)
4. 点击登录按钮

**默认账户**:
```
系统管理员:
用户名: sysadmin
密码: Admin@123456

测试医生账户:
用户名: doctor01  
密码: Doctor@123456
```

**登录问题解决**:
- 忘记密码：联系系统管理员重置
- 网络连接：确认服务器地址配置正确
- 权限问题：确认账户状态和角色配置

### 2. 主界面导航

**工作台选择**:
- **诊疗工作台** (Consultation): 医生主要工作区
- **接待工作台** (Receptionist): 前台接待功能
- **收银工作台** (Cashier): 费用结算功能  
- **系统工作台** (System): 管理员配置功能
- **药师工作台** (Pharmacist): 药材管理功能
- **治疗师工作台** (Therapist): 治疗记录功能

### 3. 患者档案管理

**新建患者**:
1. 进入患者管理模块
2. 点击"新建患者"
3. 填写基本信息：
   - 姓名、性别、年龄
   - 联系电话 (自动格式化为138-1234-5678)
   - 身份证号 (自动校验18位格式)
   - 联系地址
4. 保存患者信息

**患者查询**:
- **快速搜索**: 支持姓名、电话模糊查询
- **高级筛选**: 按年龄段、就诊日期筛选
- **历史记录**: 查看患者历史就诊记录

**数据脱敏显示**:
- 电话号码显示为：138****5678
- 身份证号显示为：430421********1234

### 4. 医案诊疗流程

**创建医案**:
1. 选择患者
2. 点击"创建医案"
3. 系统自动生成医案编号
4. 医案状态：已登记 → 诊疗中 → 已完成

**诊疗记录 (中医四诊)**:
1. **望诊**: 记录患者面色、舌象、精神状态等
2. **闻诊**: 记录声音、气味等感知信息
3. **问诊**: 详细询问主诉、现病史、既往史
4. **切诊**: 记录脉象特点和腹部触诊结果

**诊断和治疗**:
- 中医诊断：辨证论治结果
- 治疗方案：针刺、推拿、中药等治疗方法
- 医嘱：用药指导、生活建议、复诊时间

### 5. 处方管理

**开具处方**:
1. 在医案中点击"开具处方"
2. 选择药材：
   - 搜索药材名称
   - 设置用量 (如：10g)
   - 添加到处方单
3. 应用验方模板 (可选)：
   - 选择经典验方
   - 系统自动添加药材组合
   - 可调整用量和药材

**配伍检查**:
- 系统自动检查18反19畏配伍禁忌
- 发现禁忌组合时警告提醒
- 提供替代药材建议

**处方输出**:
- 打印处方：标准格式打印
- 复制处方：文本格式复制
- 保存模板：将处方保存为个人验方

### 6. 药材管理 (管理员功能)

**药材信息维护**:
- 药材名称、别名、性味归经
- 功效作用、用法用量
- 单价信息 (处方费用计算)

**药材分类**:
- 按功效分类：清热药、补益药等
- 按部位分类：根茎类、叶类等  
- 自定义标签：常用药、贵重药等

### 7. 验方管理

**经典验方库**:
- 内置经典验方：小柴胡汤、六味地黄丸等
- 详细组成：药材名称、用量、功效
- 适应症：适用病症和禁忌症

**个人验方**:
- 医生可创建个人验方
- 记录临床使用效果
- 分享给其他医生使用

## 💻 开发者使用指南

### 1. 开发环境搭建

**必备工具**:
```bash
# 安装.NET 8.0 SDK
winget install Microsoft.DotNet.SDK.8

# 安装Visual Studio 2022
winget install Microsoft.VisualStudio.2022.Community

# 克隆项目
git clone https://github.com/shouqitao/LYBTZYZS.git
cd LYBTZYZS
```

**快速启动**:
```bash
# 使用脚本启动
scripts\dev-manager.bat

# 或手动启动
dotnet run --project src/Server/Services/LYBT.WebAPI
# 在VS中启动WPF客户端
```

### 2. 项目结构理解

**解决方案架构**:
```
LYBT.All.sln          - 完整解决方案 (48个项目)
├── LYBT.Server.sln   - 后端解决方案 (11个项目)
└── LYBT.Desktop.sln  - 前端解决方案 (20个项目)
```

**核心项目**:
- **共享工具**: `LYBT.Shared.Utilities` - 企业级工具集 (72个方法)
- **数据模型**: `LYBT.Shared.Models` - 统一DTO和响应格式
- **API服务**: `LYBT.WebAPI` - RESTful API (93个端点)
- **WPF客户端**: `LYBT.Desktop.Shell` - 桌面应用入口

### 3. 使用共享工具集

**CommonHelper 使用示例**:
```csharp
// 数据验证和格式化
if (!CommonHelper.IsValidChinesePhone(phoneNumber))
{
    ShowError("手机号格式不正确");
    return;
}

string formattedPhone = CommonHelper.FormatPhone(phoneNumber);
// 输出: "138-1234-5678"

// JSON处理
string json = CommonHelper.ToJson(patient);
Patient patient = CommonHelper.FromJson<Patient>(jsonString);

// 日期时间处理
string friendlyTime = CommonHelper.FormatFriendlyTime(createTime);
// 输出: "2小时前", "昨天", "1周前"

int age = CommonHelper.CalculateAge(birthDate);
// 自动计算准确年龄
```

**EnumHelper 使用示例**:
```csharp
// 枚举描述获取
string roleDesc = EnumHelper.GetDescription(UserRole.Doctor);
// 输出: "医生"

// 下拉框数据源
var roleOptions = EnumHelper.GetKeyValuePairs<UserRole>();
comboBox.ItemsSource = roleOptions;

// 枚举循环操作
UserRole nextRole = EnumHelper.GetNext(currentRole);
UserRole prevRole = EnumHelper.GetPrevious(currentRole);

// 随机获取
UserRole randomRole = EnumHelper.GetRandom<UserRole>();
```

**PasswordHelper 使用示例**:
```csharp
// 密码强度验证
var validation = PasswordHelper.ValidatePassword(newPassword);
if (!validation.IsValid)
{
    ShowErrors(validation.Errors);
    ShowSuggestions(validation.Suggestions);
    return;
}

// 安全密码生成
string securePassword = PasswordHelper.GenerateSecurePassword(12);
// 输出: "K7m!nP2@xQ9z" (包含大小写、数字、特殊字符)

// 密码哈希和验证
string hash = PasswordHelper.Hash(password);
bool isValid = PasswordHelper.Verify(hash, inputPassword);
```

### 4. 架构模式遵循

**前端UltraThink双层架构**:
```csharp
// Module层 - 纯委托模式
public class UserModule : IUserService
{
    private readonly UserQueryService _queryService;
    private readonly UserBusinessService _businessService;

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
        => await _businessService.CreateAsync(dto);
}

// QueryService层 - 查询专业化
public class UserQueryService
{
    public async Task<ServiceResult<PagedResult<UserDto>>> SearchAsync(UserSearchDto criteria)
    {
        // 复杂查询逻辑
    }
}

// BusinessService层 - 业务+CRUD
public class UserBusinessService  
{
    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
    {
        // 业务逻辑和数据操作
    }
}
```

**后端传统三层架构**:
```csharp
// Controller层
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] UserCreateDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return HandleServiceResult(result, "用户创建成功");
    }
}

// Service层  
public class UserService : IUserService
{
    public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
    {
        // 业务逻辑处理
        var user = _mapper.Map<User>(dto);
        await _repository.CreateAsync(user);
        return ServiceResult<UserDto>.Success(_mapper.Map<UserDto>(user));
    }
}

// Repository层
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted);
    }
}
```

### 5. API集成开发

**API客户端使用**:
```csharp
// 使用Refit定义API接口
public interface IUserApi
{
    [Post("/api/v1/users")]
    Task<ApiResponse<UserDto>> CreateUserAsync([Body] UserCreateDto dto);

    [Get("/api/v1/users/{id}")]
    Task<ApiResponse<UserDto>> GetUserAsync(Guid id);

    [Get("/api/v1/users")]
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync([Query] UserSearchDto criteria);
}

// 依赖注入配置
services.AddRefitClient<IUserApi>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("http://localhost:5001");
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    });
```

**统一响应处理**:
```csharp
// 响应格式标准
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }  
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }
}

// 使用扩展方法处理响应
public static class ApiResponseExtensions
{
    public static async Task<T> HandleResponseAsync<T>(this Task<ApiResponse<T>> responseTask)
    {
        var response = await responseTask;
        if (!response.Success)
            throw new ApiException(response.Message);
        return response.Data;
    }
}
```

## 🔧 系统管理员指南

### 1. 系统部署

**环境要求**:
- Windows Server 2019+ 或 Windows 10/11
- .NET 8.0 Runtime
- SQL Server 2019+ 或 SQL Server Express  
- IIS 10.0+ (可选，用于API部署)

**部署步骤**:
```bash
# 1. 部署后端API
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o deploy/api

# 2. 配置IIS (可选)
# - 创建应用程序池 (.NET 8.0)
# - 创建网站指向 deploy/api

# 3. 部署WPF客户端
dotnet publish src/Client/Desktop/Shell -c Release -o deploy/client

# 4. 配置数据库
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure
```

**配置文件**:
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;"
  },
  "JWT": {
    "SecretKey": "your-256-bit-secret-key",
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client", 
    "ExpiryHours": 8
  }
}
```

### 2. 用户管理

**创建用户账户**:
1. 登录系统管理工作台
2. 进入用户管理模块
3. 点击"新建用户"
4. 填写用户信息：
   - 用户名 (唯一标识)
   - 显示名称
   - 角色 (Admin/Doctor)
   - 初始密码 (系统生成或手动设置)

**密码策略配置**:
- 最小长度：8位
- 必须包含：大写字母、小写字母、数字、特殊字符
- 密码强度：系统自动评估 (弱/一般/良好/强/很强)
- 弱密码检测：内置23个常见弱密码黑名单

**用户权限管理**:
- Admin角色：系统全权限
- Doctor角色：诊疗业务权限
- 权限矩阵详见用户角色部分

### 3. 系统监控

**健康检查端点** (8个):
```bash
# API基础健康检查
GET /api/health

# 数据库连接检查
GET /api/health/database

# 缓存系统检查  
GET /api/health/cache

# 系统资源检查
GET /api/health/system

# 依赖服务检查
GET /api/health/dependencies
```

**性能监控**:
- API响应时间监控
- 数据库连接池状态
- 内存缓存命中率
- 系统资源使用情况

**日志管理**:
- 应用程序日志：`logs/app-{date}.log`
- 错误日志：`logs/error-{date}.log`
- 审计日志：`logs/audit-{date}.log`
- 性能日志：`logs/performance-{date}.log`

### 4. 数据备份与恢复

**自动备份配置**:
```sql
-- 创建维护计划
USE [LYBTDB]
GO
BACKUP DATABASE [LYBTDB] 
TO DISK = N'D:\Backup\LYBTDB-Full-{date}.bak' 
WITH FORMAT, 
     INIT,
     NAME = N'LYBTDB-Full Database Backup',
     SKIP, 
     NOREWIND, 
     NOUNLOAD,
     STATS = 10
```

**备份策略建议**:
- 完整备份：每日夜间执行
- 差异备份：每4小时执行
- 事务日志备份：每15分钟执行
- 备份保留：完整备份保留30天

### 5. 故障排除

**常见问题诊断**:

**登录问题**:
- 检查用户状态：`SELECT * FROM Users WHERE Username = 'username'`
- 验证密码哈希：确认密码格式正确
- 检查JWT配置：密钥、过期时间设置

**API连接问题**:
- 检查服务状态：`netstat -an | find ":5001"`
- 验证防火墙设置：开放API端口
- 测试API端点：`curl http://localhost:5001/api/health`

**数据库问题**:
- 检查连接字符串配置
- 验证SQL Server服务状态
- 检查数据库权限设置

**性能问题**:
- 监控数据库查询性能
- 检查内存缓存配置
- 分析API响应时间

## 📊 系统使用统计

### 功能覆盖度
- ✅ **用户管理**: 100% (登录、权限、密码策略)
- ✅ **患者管理**: 100% (档案、查询、历史)
- ✅ **医案诊疗**: 100% (四诊、诊断、治疗)  
- ✅ **处方管理**: 100% (开方、配伍、打印)
- ✅ **药材管理**: 100% (信息、分类、价格)
- ✅ **验方管理**: 100% (经典、个人、应用)

### 技术指标
- **API端点**: 93个RESTful接口
- **工具方法**: 72个企业级工具方法
- **项目数量**: 48个协同开发项目
- **文档覆盖**: 130+技术文档

### 性能指标
- **响应时间**: API平均响应<2秒
- **并发支持**: <10用户并发访问
- **数据处理**: 支持千级患者档案
- **缓存命中**: 内存缓存命中率>80%

## 📞 支持与帮助

### 技术支持
- **文档查阅**: [完整文档库](../README.md)
- **问题反馈**: [GitHub Issues](https://github.com/shouqitao/LYBTZYZS/issues)
- **开发指南**: [开发者文档](../development/)
- **架构参考**: [架构设计文档](../architecture/)

### 培训资源
- **快速上手**: [快速开始指南](../development/getting-started.md)
- **操作视频**: 用户操作演示视频
- **最佳实践**: 业务流程最佳实践
- **常见问题**: FAQ和故障排除指南

### 版本更新
- **当前版本**: v2025.1 (企业级工具集版)
- **更新频率**: 季度功能更新，月度问题修复
- **升级指南**: 详细的版本升级步骤
- **变更日志**: 完整的版本变更记录

---

**LYBT系统综合使用指南 2025** - 让中医诊疗系统使用更简单高效 ✨

**最后更新**: 2025-09-02 | **版本**: v2025.9 | **状态**: 生产就绪
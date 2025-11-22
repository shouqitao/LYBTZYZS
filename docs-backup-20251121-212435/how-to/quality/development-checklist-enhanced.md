# 开发检查清单（增强版）

**基于凌隐宝堂中医诊所实际开发流程的完整质量保证检查清单**  
**创建时间**: 2025-10-16  
**适用范围**: 所有开发活动和代码变更  
**检查频率**: 每次功能开发前、提交前、发布前  

---

## 🚀 开发前准备检查

### ✅ 环境状态检查

#### Git环境检查
**目的**: 确保代码仓库状态正常，避免冲突
**检查工具**: Git命令行 / Git GUI

**执行步骤**:
```bash
# 1. 检查当前分支
git branch --show-current

# 2. 检查是否有未提交的更改
git status --porcelain

# 3. 拉取最新代码
git fetch origin
git pull origin master

# 4. 检查是否有冲突
git status | grep "both modified"

# 5. 检查分支是否为最新
git log origin/master..HEAD --oneline
```

**验证标准**:
- [ ] 当前分支为master或feature分支
- [ ] 无未提交的本地更改
- [ ] 远程代码已更新到本地
- [ ] 无合并冲突
- [ ] 提交历史清晰，无过多未推送的提交

#### 项目构建检查
**目的**: 确保项目能够正常编译和运行
**检查工具**: .NET CLI, Visual Studio

**执行步骤**:
```bash
# 1. 清理构建缓存
dotnet clean LYBT.All.sln

# 2. 恢复NuGet包
dotnet restore LYBT.All.sln --no-cache

# 3. 编译项目
dotnet build LYBT.All.sln -c Release --no-restore --verbosity normal

# 4. 检查编译结果
echo $?
```

**验证标准**:
- [ ] 恢复成功，无包冲突
- [ ] 编译成功，无编译错误
- [ ] 无严重警告（可暂时忽略）
- [ ] 输出路径正确
- [ ] 依赖项版本一致

#### 测试基线检查
**目的**: 确保测试套件能够正常运行
**检查工具**: .NET Test, Visual Studio Test Explorer

**执行步骤**:
```bash
# 1. 运行所有测试
dotnet test LYBT.All.sln -c Release --logger "console;verbosity=normal" --no-build

# 2. 检查测试覆盖率（可选）
dotnet test LYBT.All.sln -c Release --collect:"XPlat Code Coverage"

# 3. 检查特定测试类别
dotnet test src/Tests/UnitTests --logger "console;verbosity=detailed"
```

**验证标准**:
- [ ] 测试环境配置正确
- [ ] 所有单元测试通过
- [ ] 集成测试基线通过
- [ ] 测试数据库连接正常
- [ ] 测试失败项已记录原因

#### 数据库状态检查
**目的**: 确保数据库环境正常
**检查工具**: SQL Server Management Studio, sqlcmd

**执行步骤**:
```bash
# 1. 检查SQL Server服务状态
sc query MSSQLSERVER

# 2. 测试数据库连接
sqlcmd -S localhost -E -Q "SELECT DB_NAME() AS CurrentDatabase"

# 3. 检查数据库文件
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name='LYBTDB'"

# 4. 检查表结构
sqlcmd -S localhost -E -d LYBTDB -Q "SELECT COUNT(*) FROM Users"
```

**验证标准**:
- [ ] SQL Server服务运行正常
- [ ] LYBTDB数据库存在
- [ ] 连接字符串配置正确
- [ ] 核心表结构完整
- [ ] 数据迁移状态正常

### ✅ Issue和需求确认

#### Issue状态检查
**目的**: 确保有对应的开发任务
**检查工具**: GitHub Issues, JIRA, 项目管理系统

**执行步骤**:
```bash
# 1. 检查是否有相关Issue
gh issue list --state open --assignee @your-username

# 2. 检查Issue状态
gh issue view <ISSUE_NUMBER>

# 3. 确认Issue描述清晰
# 检查Issue标题、描述、验收标准
```

**验证标准**:
- [ ] 存在对应的GitHub Issue
- [ ] Issue状态为open或in-progress
- [ ] Issue描述包含清晰的验收标准
- [ ] Issue分配给当前开发者
- [ ] 优先级设置合理

---

## 💻 功能实现检查

### ✅ 代码开发检查

#### 架构合规检查
**目的**: 确保代码符合项目架构标准
**检查工具**: Code Review, 静态分析

**执行步骤**:
```csharp
// 检查清单示例

// 1. 检查Controller层
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : BaseApiController
{
    // ✅ 继承BaseApiController
    // ✅ 使用路由版本控制
    // ✅ 使用构造函数注入
}

// 2. 检查Service层
public interface IUserService
{
    // ✅ 接口和实现分离
    // ✅ 方法返回ServiceResult
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
}

// 3. 检查Repository层
public interface IUserRepository
{
    // ✅ Repository接口定义
    // ✅ 异步方法
    Task<User> GetByIdAsync(Guid id);
}
```

**验证标准**:
- [ ] Controller继承BaseApiController
- [ ] 使用正确的路由模式
- [ ] Service层接口与实现分离
- [ ] Repository层异步模式
- [ ] 依赖注入使用构造函数注入
- [ ] 不存在禁止的依赖项

#### 业务逻辑检查
**目的**: 确保业务逻辑正确实现
**检查工具**: 单元测试，Code Review

**执行步骤**:
```csharp
// 业务逻辑检查要点

// 1. 数据验证
public async Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto)
{
    // ✅ 输入参数验证
    if (string.IsNullOrWhiteSpace(dto.Username))
        return ServiceResult<UserDto>.Failure("用户名不能为空");
    
    // ✅ 业务规则验证
    if (await _userRepository.ExistsByUsernameAsync(dto.Username))
        return ServiceResult<UserDto>.Failure("用户名已存在");
    
    // ✅ 事务处理
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 业务逻辑
        var user = _mapper.Map<User>(dto);
        var result = await _repository.AddAsync(user);
        await transaction.CommitAsync();
        
        return ServiceResult<UserDto>.Success(_mapper.Map<UserDto>(result));
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**验证标准**:
- [ ] 输入参数验证完整
- [ ] 业务规则实现正确
- [ ] 事务处理正确
- [ ] 错误处理完善
- [ ] 日志记录适当
- [ ] 性能考虑合理

### ✅ 代码质量检查

#### 代码规范检查
**目的**: 确保代码符合团队规范
**检查工具**: StyleCop, ReSharper, Code Analysis

**执行步骤**:
```bash
# 1. 运行代码分析
dotnet build /p:StyleCopEnabled=true /p:RunAnalyzersDuringBuild=true

# 2. 检查命名规范
# 确保类名、方法名、变量名符合PascalCase/camelCase

# 3. 检查注释规范
// 确保类和方法有XML注释
/// <summary>
/// 用户服务类
/// </summary>
public class UserService { }
```

**验证标准**:
- [ ] 命名规范符合C#标准
- [ ] XML注释完整
- [ ] 代码格式正确
- [ ] 无编译警告（除特定情况）
- [ ] 代码复杂度合理
- [ ] 方法长度适中

#### 性能考虑检查
**目的**: 确保代码性能合理
**检查工具**: Performance Profiler, Code Review

**执行步骤**:
```csharp
// 性能检查要点

// 1. 数据库查询优化
public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize)
{
    // ✅ 使用分页查询
    // ✅ 只查询需要的字段
    return await _users
        .OrderBy(u => u.CreatedAt)
        .Select(u => new UserDto { /* 只映射需要的字段 */ })
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}

// 2. 异步操作
public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
{
    // ✅ 使用异步方法
    // ✅ 配置取消令牌
    return await _repository.GetByIdAsync(id, cancellationToken);
}

// 3. 缓存使用
public async Task<UserDto> GetByIdWithCacheAsync(Guid id)
{
    // ✅ 检查缓存
    if (_cache.TryGetValue($"user_{id}", out UserDto cached))
        return cached;
    
    // ✅ 设置缓存
    var user = await _repository.GetByIdAsync(id);
    var dto = _mapper.Map<UserDto>(user);
    _cache.Set($"user_{id}", dto, TimeSpan.FromMinutes(30));
    
    return dto;
}
```

**验证标准**:
- [ ] 数据库查询优化
- [ ] 使用异步操作
- [ ] 合理使用缓存
- [ ] 避免N+1查询问题
- [ ] 内存使用合理
- [ ] 并发处理正确

---

## 🧪 测试验证检查

### ✅ 单元测试检查

#### 测试覆盖率检查
**目的**: 确保测试覆盖率达标
**检查工具**: dotCover, Coverage Gutters

**执行步骤**:
```bash
# 1. 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults/Coverage

# 2. 查看覆盖率报告
# 打开 TestResults/Coverage/index.html

# 3. 检查覆盖率指标
dotnet test --collect:"XPlat Code Coverage" --logger "console;verbosity=detailed"
```

**验证标准**:
- [ ] 单元测试覆盖率 > 70%
- [ ] 核心业务逻辑100%覆盖
- [ ] 边界条件测试完整
- [ ] 异常情况测试覆盖
- [ ] 测试用例描述清晰

#### 测试质量检查
**目的**: 确保测试质量符合要求
**检查工具**: 单元测试框架, Code Review

**执行步骤**:
```csharp
// 测试质量示例

// 1. 测试命名规范
public class UserServiceTests
{
    // ✅ 测试类命名：[类名]Tests
    
    [Fact]
    public async Task CreateAsync_ValidUser_ReturnsSuccess()
    {
        // ✅ 测试方法命名：[方法名]_[条件]_[期望结果]
        // Arrange
        var service = new UserService(_repository, _mapper, _logger);
        var dto = new UserCreateDto { Username = "testuser" };
        
        // Act
        var result = await service.CreateAsync(dto);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("testuser", result.Data.Username);
    }
    
    // 2. 测试数据准备
    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange - 准备测试数据
        var existingUser = CreateTestUser();
        
        // Act
        var result = await _service.GetByIdAsync(existingUser.Id);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser.Username, result.Data.Username);
    }
}
```

**验证标准**:
- [ ] 测试命名规范正确
- [ ] AAA模式（Arrange-Act-Assert）结构清晰
- [ ] 测试数据准备完整
- [ ] 断言明确具体
- [ ] 测试用例独立性
- [ ] 模拟对象使用正确

### ✅ 集成测试检查

#### API测试检查
**目的**: 确保API接口功能正常
**检查工具**: Postman, Swagger UI, Integration Tests

**执行步骤**:
```csharp
// API集成测试示例

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            username = "admin",
            password = "password123"
        };
        
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        Assert.True(loginResponse.Success);
        Assert.NotNull(loginResponse.Data.Token);
    }
}
```

**验证标准**:
- [ ] 所有API端点有集成测试
- [ ] HTTP状态码正确
- [ ] 请求响应格式正确
- [ ] 认证授权测试完整
- [ ] 错误处理测试覆盖
- [ ] 数据验证测试完整

---

## 📝 文档同步检查

### ✅ 技术文档检查

#### API文档更新检查
**目的**: 确保API文档与代码同步
**检查工具**: Swagger UI, 文档对比工具

**执行步骤**:
```bash
# 1. 启动API项目
dotnet run --project src/Server/Services/LYBT.WebAPI

# 2. 访问Swagger文档
# 打开 http://localhost:5001/swagger

# 3. 检查文档完整性
# - 所有Controller都在Swagger中显示
# - 参数说明正确
# - 响应示例完整
# - 认证配置正确
```

**验证标准**:
- [ ] Swagger UI可正常访问
- [ ] 所有API端点都有文档
- [ ] 参数描述准确完整
- [ ] 响应示例格式正确
- [ ] 认证授权说明清晰
- [ ] 错误码说明完整

#### 代码注释检查
**目的**: 确保代码注释完整准确
**检查工具**: IDE, 代码审查

**执行步骤**:
```csharp
// 检查注释质量

/// <summary>
/// 用户服务类
/// 负责处理用户相关的业务逻辑，包括创建、更新、删除和查询用户信息
/// </summary>
/// <param name="repository">用户仓储接口</param>
/// <param name="mapper">对象映射器</param name="logger">日志记录器</param>
/// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
/// <example>
/// <code>
/// var service = new UserService(repository, mapper, logger);
/// var result = await service.GetByIdAsync(userId);
/// </code>
/// </example>
public class UserService
{
    /// <summary>
    /// 根据ID获取用户信息
    /// </summary>
    /// <param name="id">用户唯一标识符</param>
    /// <returns>用户信息或null</returns>
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(id));
            
        // 实现逻辑...
    }
}
```

**验证标准**:
- [ ] 所有公共类都有XML注释
- [ ] 方法参数和返回值有注释
- [ ] 异常情况有说明
- [ ] 包含使用示例
- [ ] 注释内容准确完整
- [ ] 更新及时

### ✅ 项目文档检查

#### README文档更新
**目的**: 确保项目文档信息最新
**检查工具**: 文档审查工具

**执行步骤**:
```markdown
# 检查README.md内容

## 项目概述
✅ 项目描述准确
✅ 技术栈信息完整
✅ 功能特性列表正确

## 快速开始
✅ 环境要求清晰
✅ 安装步骤详细
✅ 配置说明准确

## 开发指南
✅ 开发环境设置
✅ 构建命令正确
✅ 调试配置说明
```

**验证标准**:
- [ ] README.md信息准确完整
- [ ] 安装说明步骤正确
- [ ] 开发环境指南详细
- [ ] 构建命令可执行
- [ ] 联系信息有效
- [ ] 更新日期及时

---

## 🚀 部署前检查

### ✅ 生产环境准备检查

#### 配置文件检查
**目的**: 确保生产环境配置正确
**检查工具**: 配置验证脚本

**执行步骤**:
```bash
# 1. 检查生产配置文件
ls -la appsettings.Production.json

# 2. 验证配置格式
python -m json.tool appsettings.Production.json

# 3. 检查环境变量设置
echo $DATABASE_CONNECTION_STRING
echo $JWT_SECRET_KEY
echo $ALLOWED_HOSTS

# 4. 验证敏感信息
# 确保无硬编码的密码或密钥
```

**验证标准**:
- [ ] 生产配置文件存在
- [ ] JSON格式正确
- [ ] 敏感信息使用环境变量
- [ ] 连接字符串配置正确
- [ ] 日志级别设置合理
- [ ] 性能参数优化

#### 数据库迁移检查
**目的**: 确保数据库迁移状态正确
**检查工具**: Entity Framework CLI

**执行步骤**:
```bash
# 1. 检查迁移状态
dotnet ef migrations list

# 2. 检查待执行的迁移
dotnet ef database update --dry-run

# 3. 验证数据库结构
dotnet ef dbcontext info
```

**验证标准**:
- [ ] 所有迁移都已创建
- [ ] 迁移脚本验证通过
- [ ] 数据库结构最新
- [ ] 种子数据准备完成
- [ ] 备份策略已制定
- [ ] 回滚方案已准备

#### 安全配置检查
**目的**: 确保安全配置符合要求
**检查工具**: 安全扫描工具

**执行步骤**:
```bash
# 1. 检查JWT配置
grep -n "SecretKey" appsettings.Production.json

# 2. 验证HTTPS配置
# 确保使用HTTPS协议
# 检查SSL证书配置

# 3. 检查CORS配置
# 确保跨域配置安全
```

**验证标准**:
- [ ] JWT密钥强度足够
- [ ] HTTPS配置正确
- [ ] CORS配置安全
- [ ] 敏感信息保护
- [ ] 访问控制正确
- [ ] 审计日志启用

---

## 📊 检查记录模板

### 检查记录表

| 检查类别 | 检查项目 | 状态 | 检查人 | 检查时间 | 备注 |
|----------|----------|------|--------|----------|------|
| 环境准备 | Git状态检查 | ✅ | | 2025-10-16 | |
| 环境准备 | 项目构建检查 | ✅ | | 2025-10-16 | |
| 环境准备 | 测试基线检查 | ✅ | | 2025-10-16 | |
| 代码开发 | 架构合规检查 | ✅ | | 2025-10-16 | |
| 代码开发 | 业务逻辑检查 | ✅ | | 2025-10-16 | |
| 测试验证 | 单元测试检查 | ✅ | | 2025-10-16 | |
| 测试验证 | API测试检查 | ✅ | | 2025-10-16 | |
| 文档同步 | API文档检查 | ✅ | | 2025-10-16 | |
| 文档同步 | 代码注释检查 | ✅ | | 2025-10-16 | |
| 部署准备 | 配置文件检查 | ✅ | | 2025-10-16 | |

### 问题记录模板

```markdown
## 问题记录

### 日期：2025-10-16

#### 问题1：数据库连接字符串配置错误
- **状态**: 🔴 待解决
- **影响**: 无法连接数据库
- **解决方案**: 更新appsettings.json中的连接字符串
- **负责人**: [开发者姓名]
- **预计解决时间**: 2小时

#### 问题2：测试覆盖率不足
- **状态**: 🟡 进行中
- **影响**: 单元测试覆盖率只有60%
- **解决方案**: 补充测试用例
- **负责人**: [测试工程师]
- **预计完成时间**: 明天
```

---

## 🎯 质量标准

### 检查完成度目标
- **开发前检查**: 100%完成
- **代码开发检查**: 95%以上完成
- **测试验证检查**: 90%以上完成
- **文档同步检查**: 85%以上完成
- **部署准备检查**: 100%完成

### 持续改进措施
1. **定期更新检查清单**：根据项目发展调整检查项
2. **自动化检查**：将部分检查项集成到CI/CD流程
3. **团队培训**：定期进行开发规范培训
4. **质量监控**：建立代码质量监控体系
5. **经验总结**：收集和分享最佳实践经验

---

**使用说明**: 请在开发过程中严格按照此清单进行检查，确保代码质量和项目规范性。发现问题及时记录并跟踪解决。
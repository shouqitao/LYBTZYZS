# LYBTZYZS 项目AI Agent开发规则

## 项目概述
凌隐宝堂中医诊所管理系统 - .NET 8 Web API + WPF桌面客户端

## 强制性规则

### 数据库迁移
- **必须** 在 `src/Backend/Core/LYBT.Infrastructure` 项目中添加所有迁移
- **禁止** 在任何Module项目中执行 `dotnet ef migrations add`
- **必须** 使用命令：`dotnet ef migrations add [Name] --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI`

### API响应格式
- **必须** 使用 `ApiResponse<T>` 包装所有API响应
- **必须** 使用BaseController的Success/BusinessFail/NotFound方法
- **禁止** 直接返回Ok()或BadRequest()

### 文件大小限制
- **禁止** 创建超过500行的文件
- **必须** 接近限制时立即重构为多个文件

## Project Architecture

### Module Organization

**8个核心业务模块 - 必须保持一致性**
- Auth - 身份认证和授权
- Users - 用户管理
- Patients - 患者档案（包含基础挂号功能）
- Consultation - 看诊管理（核心模块，支持中医四诊）
- MedicalCase - 医疗案例（统一管理整个诊疗流程）
- Prescriptions - 处方管理
- Herbs - 中药材管理（仅处方用药，不涉及库存管理）
- Formula - 验方管理（经典验方模板）

**每个模块必须包含以下结构**
- `{ModuleName}Module.cs` - 模块注册类
- `Interfaces/I{Entity}Service.cs` - 服务接口
- `Interfaces/I{Entity}Repository.cs` - 仓储接口
- `Services/{Entity}Service.cs` - 服务实现
- `Repositories/{Entity}Repository.cs` - 仓储实现
- `Mapping/{Entity}MappingProfile.cs` - AutoMapper配置

### 文件路径规则

**后端模块路径**
- 模块位置：`src/Backend/Modules/LYBT.Module.{ModuleName}/`
- 核心基础设施：`src/Backend/Core/LYBT.Infrastructure/`
- 领域模型：`src/Backend/Core/LYBT.Models/`
- Web API入口：`src/Backend/Services/LYBT.WebAPI/`

**前端模块路径**
- WPF Shell：`src/Frontend/Desktop/Shell/`
- 服务层：`src/Frontend/Desktop/Services/`
- 资源文件：`src/Frontend/Desktop/Assets/`
- 主题样式：`src/Frontend/Desktop/Themes/`

**共享模型路径**
- DTOs和共享模型：`src/Shared/LYBT.Shared.Models/`
- 工具类：`src/Shared/LYBT.Shared.Utilities/`

## Code Implementation Standards

### 数据库迁移规则

**只能在Infrastructure项目添加迁移**
```bash
dotnet ef migrations add {MigrationName} --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

**禁止在其他项目添加迁移** - 所有模块共享AppDbContext

### API响应格式

**POST方法必须返回创建的对象**
```csharp
return Ok(createdObject); // 正确
return Ok(new { message = "Created" }); // 错误
```

**PUT/DELETE方法返回操作结果消息**
```csharp
return Ok(new { message = "操作成功" }); // 正确
return Ok(updatedObject); // 错误
```

**错误响应使用ProblemDetails**
```csharp
return Problem("错误描述", statusCode: 400); // 正确
throw new Exception("错误"); // 错误
```

### 依赖注入配置

**模块服务注册位置**
- 在`{ModuleName}Module.cs`的`RegisterServices`方法中注册
- 使用`services.AddScoped<Interface, Implementation>()`

**禁止使用Singleton注册业务服务** - 只用于缓存、配置等

### AutoMapper配置

**必须提供ILoggerFactory参数**
```csharp
new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile()), NullLoggerFactory.Instance);
```

**禁止省略第二参数** - AutoMapper 15.0.1要求

## Workflow Standards

### 诊疗流程

**核心流程顺序 - 不可跳过**
1. 患者接待(Patients) - 创建或查询患者档案
2. 看诊(Consultation) - 执行中医四诊
3. 开方(Prescriptions) - 结合Formula和Herbs开具处方
4. MedicalCase贯穿全程 - 作为诊疗聚合根

**模块间依赖关系**
- Prescriptions依赖Formula和Herbs
- Consultation依赖Patients
- MedicalCase聚合所有诊疗信息

### 文件创建工作流

**创建新文件时必须检查**
1. 文件名必须英文kebab-case
2. 文档必须放在docs/对应子目录
3. 报告必须包含日期YYYYMMDD
4. 资源文件必须放在Assets/对应子目录

**禁止在根目录创建文档** - 违反文件组织规范

## Key File Interaction Standards

### 多文件同步修改规则

**修改模型时必须同时更新**
- `LYBT.Models/{Entity}Model.cs` - 数据库模型
- `LYBT.Shared.Models/{Entity}Dto.cs` - DTO模型
- `{Module}/Mapping/{Entity}MappingProfile.cs` - 映射配置

**修改API时必须同时更新**
- `Controllers/{Entity}Controller.cs` - 控制器
- `Services/{Entity}Service.cs` - 前端服务
- API文档（如果存在）

**添加新模块时必须**
1. 创建Module项目在`src/Backend/Modules/`
2. 在`LYBT.Backend.sln`添加项目引用
3. 在`Program.cs`注册模块
4. 更新`docs/MODULE_LIST.md`

## AI Decision-making Standards

### 命名决策树

**遇到命名冲突时**
1. 检查现有模块的命名模式
2. 使用相同的前缀/后缀约定
3. 保持与模块内其他类的一致性

**选择DTO名称时**
- 创建操作：`{Entity}CreateDto`
- 更新操作：`{Entity}UpdateDto`
- 查询结果：`{Entity}Dto`
- 详情查询：`{Entity}DetailDto`
- 分页查询：`{Entity}PagedQueryDto`

### 错误处理决策

**遇到异常时**
1. 业务逻辑错误 → 抛出BusinessException
2. 资源未找到 → 抛出NotFoundException
3. 验证失败 → 抛出ValidationException
4. 其他错误 → 记录日志并返回500

**选择HTTP状态码**
- 200: 成功
- 201: 创建成功（很少使用，通常返回200）
- 400: 请求参数错误
- 401: 未认证
- 403: 无权限
- 404: 资源不存在
- 500: 服务器内部错误

## Prohibited Actions

### 严格禁止的操作

**禁止创建超过500行的文件** - 必须拆分

**禁止使用中文文件名** - 会导致编码问题

**禁止在根目录创建文档** - 违反组织规范

**禁止跳过模块结构** - 必须包含Interface/Service/Repository

**禁止直接操作数据库** - 必须通过Repository

**禁止在Controller写业务逻辑** - 必须在Service层

**禁止使用同步数据库操作** - 必须使用async/await

**禁止硬编码配置** - 必须使用appsettings.json

**禁止省略XML注释** - 所有public成员必须有注释

**禁止使用var定义公共API返回值** - 必须明确类型

### 数据库操作禁忌

**禁止使用TRUNCATE** - 使用DELETE

**禁止删除已完成的任务记录** - 保持数据完整性

**禁止直接修改AdminSecrets表** - 使用专门的管理接口

**禁止在生产环境使用DROP DATABASE** - 需要特殊权限

## Framework/Plugin Usage Standards

### Entity Framework Core使用规则

**必须使用Code First** - 不使用Database First

**必须使用FluentAPI配置** - 不依赖数据注解

**必须包含审计字段** - CreatedAt, UpdatedAt, CreatedBy, UpdatedBy

### Prism.DryIoc使用规则

**模块必须继承IModule** - 实现Initialize和RegisterTypes

**视图必须在RegionManager注册** - 使用region导航

### JWT认证规则

**Token过期时间**
- 默认: 8小时
- Remember Me: 30天

**必须在请求头包含**
```
Authorization: Bearer {token}
```

## UltraThink Methodology

### UltraThink重构原则

**使用UltraThink时必须**
1. 进行10步深度思考分析
2. 创建分析报告在`docs/ultrathink/`
3. 文件名格式：`{feature}-ultrathink-report.md`
4. 包含问题分析、解决方案、实施步骤

**UltraThink适用场景**
- 大规模重构
- 性能优化
- 架构调整
- 复杂问题解决

## Environment Specific Rules

### 开发环境配置

**数据库连接**
- Server: localhost
- Database: LYBTDB
- 认证: Windows认证

**API端口**
- HTTPS: 7001
- HTTP: 5001

**默认登录凭据**
- 用户名: sysadmin
- 密码: Admin@123456

### 调试配置

**启动项目顺序**
1. 先启动WebAPI项目
2. 等待数据库初始化完成
3. 再启动WPF客户端

**日志文件位置**
- API日志: `src/Backend/Services/LYBT.WebAPI/server.log`
- 客户端日志: `%APPDATA%/LYBT/logs/`

## Testing Standards

### 单元测试规则

**测试文件命名**
- `{ClassName}Tests.cs`
- 位于`tests/Backend/LYBT.Module.{ModuleName}.Tests/`

**测试方法命名**
- `{MethodName}_Should{ExpectedBehavior}_When{Condition}`

**必须测试的场景**
- 正常流程
- 边界条件
- 异常情况

### 集成测试规则

**测试脚本位置**
- Python脚本: `tests/api/`
- 运行命令: `python api_test_automation.py`

## Documentation Standards

### 文档更新时机

**必须更新文档的情况**
- 添加新模块
- 修改API接口
- 更改业务流程
- 调整架构

**文档位置规则**
- 架构文档: `docs/architecture/`
- API文档: `docs/api/`
- 开发指南: `docs/development/`
- 测试文档: `docs/testing/`

### 注释规范

**必须包含的注释**
```csharp
/// <summary>
/// 功能描述
/// </summary>
/// <param name="参数名">参数说明</param>
/// <returns>返回值说明</returns>
```

**复杂逻辑必须注释原因**
```csharp
// Reason: 解释为什么这样实现
```
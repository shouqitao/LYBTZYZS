# 凌隐宝堂系统架构基准规范 (Canon)

**版本**: v1.0  
**生效日期**: 2025-01-09  
**适用范围**: LYBTZYZS 全项目  
**规则来源**: PRD-Consistency-Unification + .editorconfig + Directory.Build.props

---

## 🎯 唯一规则源声明

本文档是凌隐宝堂系统的**唯一架构基准**，所有开发活动必须严格遵循。当存在冲突时，以本文档为准。

---

## ✅ 必须遵循 (MUST)

### 1. 命名约定 (Naming Conventions)

#### ✅ Username 统一标准
- **代码中一律使用**: `Username` (PascalCase)
- **数据库遗留列**: `UserName` (通过 EF Core `.HasColumnName("UserName")` 映射)
- **DTO/ViewModel**: 统一使用 `Username` 属性名
- **API 参数**: 统一使用 `username` (camelCase)

**示例**:
```csharp
// ✅ 正确
public class User 
{
    [Column("UserName")]  // DB 遗留列名
    public string Username { get; set; }  // 代码统一属性名
}

public class LoginDto
{
    public string Username { get; set; }  // DTO 统一
}

// ❌ 错误
public string UserName { get; set; }  // 禁止在代码中使用
```

#### ✅ 通用命名规范
- **接口**: `I` 前缀，如 `IUserService`
- **私有字段**: `_camelCase`，如 `_userService`
- **常量**: `UPPER_CASE`，如 `DEFAULT_TIMEOUT`
- **枚举**: `PascalCase`，如 `UserRole.Doctor`

### 2. API 路由标准 (API Routing)

#### ✅ 固定路由格式
- **基础路径**: `/api/v1/[controller]`
- **禁止动态版本**: 不使用 `api/v{version:apiVersion}`
- **HTTP 动词**: 严格遵循 RESTful 语义

**示例**:
```csharp
// ✅ 正确
[Route("api/v1/[controller]")]
public class UsersController : BaseApiController

// ❌ 错误  
[Route("api/v{version:apiVersion}/[controller]")]  // 禁止动态版本
[Route("api/users")]  // 禁止硬编码路径
```

### 3. 空引用检查 (#nullable)

#### ✅ 全局启用
- **项目级别**: `#nullable enable` 在所有 C# 文件顶部
- **严格模式**: 启用 CS8618、CS8625 警告
- **空值处理**: 使用 `?` 操作符和空值合并

**示例**:
```csharp
#nullable enable

public class UserService
{
    public async Task<User?> GetUserAsync(string? username)
    {
        return username?.Length > 0 ? await FindUser(username) : null;
    }
}
```

### 4. 语言版本 (LangVersion)

#### ✅ 使用最新稳定版
- **设置**: `<LangVersion>latest</LangVersion>`
- **特性**: 积极使用 C# 12 特性（主构造函数、集合表达式等）
- **现代化**: 优先使用新语言特性替代旧模式

### 5. 中央包管理 (Central Package Management)

#### ✅ 强制启用
- **全局配置**: `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
- **版本定义**: 仅在 `Directory.Packages.props` 中定义版本
- **项目引用**: `.csproj` 中只声明包名，不包含版本号

**示例**:
```xml
<!-- ✅ 正确 - Directory.Packages.props -->
<PackageVersion Include="AutoMapper" Version="14.0.0" />

<!-- ✅ 正确 - Project.csproj -->
<PackageReference Include="AutoMapper" />

<!-- ❌ 错误 - Project.csproj -->
<PackageReference Include="AutoMapper" Version="14.0.0" />
```

### 6. 分层架构边界 (Layer Boundaries)

#### ✅ 严格分层
- **UI 层**: 仅依赖 Application 接口，禁止直接访问 Domain/Infrastructure
- **Application 层**: 可依赖 Domain，禁止依赖 UI/Infrastructure 实现
- **Domain 层**: 完全独立，不依赖其他层
- **Infrastructure 层**: 实现 Domain 接口，可依赖外部框架

**依赖方向**:
```
UI → Application → Domain ← Infrastructure
```

### 7. 日志与异常模板 (Logging & Exception Templates)

#### ✅ 统一模板
- **日志框架**: Microsoft.Extensions.Logging
- **异常处理**: 使用 `BaseApiController.HandleException<T>()`
- **日志级别**: Information(业务) > Warning(异常) > Error(系统错误)

**模板示例**:
```csharp
// ✅ 日志模板
_logger.LogInformation("用户 {Username} 执行 {Action} 操作", username, "Login");

// ✅ 异常处理模板
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    return HandleException<ApiResponse<User>>(ex, "获取用户信息", userId);
}
```

---

## ❌ 禁止行为 (MUST NOT)

### 1. 禁止的命名模式
- ❌ 代码中使用 `UserName` 属性名
- ❌ API 路由使用动态版本 `v{version:apiVersion}`  
- ❌ 硬编码魔法字符串，必须定义常量
- ❌ 不一致的大小写风格混用

### 2. 禁止的架构违规
- ❌ 跨层直接调用（如 UI 直接访问 Infrastructure）
- ❌ 循环依赖
- ❌ 在 Domain 层引用框架特定类型
- ❌ 重复接口定义（IModule 与 IService 重复问题）

### 3. 禁止的包管理模式
- ❌ 项目文件中定义包版本号
- ❌ 不同项目使用不同版本的相同包
- ❌ 引用未在中央包管理中定义的包

### 4. 禁止的 API 设计
- ❌ 非 RESTful 路由设计
- ❌ 不一致的响应格式
- ❌ 缺少统一异常处理
- ❌ 硬编码的 HTTP 状态码

---

## 📋 代码检查清单

### 提交前检查 (Pre-commit)
- [ ] ✅ 所有文件顶部包含 `#nullable enable`
- [ ] ✅ API 控制器使用 `/api/v1/[controller]` 路由
- [ ] ✅ Username 属性名统一，DB 映射正确
- [ ] ✅ 包引用无版本号，仅在中央包管理定义
- [ ] ✅ 无跨层依赖调用

### 代码审查检查 (Code Review)
- [ ] ✅ 遵循分层架构原则
- [ ] ✅ 异常处理使用统一模板
- [ ] ✅ 日志记录格式统一
- [ ] ✅ 命名约定符合规范
- [ ] ✅ 无重复接口定义

---

## 🔧 工具集成

### 自动化检查工具
- **EditorConfig**: 代码格式自动化
- **StyleCop.Analyzers**: 代码风格检查
- **NetArchTest**: 架构层次验证（规划中）
- **MSBuild**: 编译时规则验证

### IDE 配置要求
- **Visual Studio**: 启用 EditorConfig，安装 StyleCop 扩展
- **代码分析**: 启用全部 CA 规则，TreatWarningsAsErrors=false（渐进式）
- **格式化**: 保存时自动格式化，遵循 EditorConfig 设置

---

## 📚 相关文档

- [PRD 一致性统一重构](..\.claude\prds\PRD-Consistency-Unification.md)
- [EditorConfig 配置](..\.editorconfig)
- [构建配置](.\Directory.Build.props)
- [包管理配置](.\Directory.Packages.props)

---

## 🚨 违规处理

### 违规等级
- **P0 (阻塞)**: 架构违规、安全漏洞 → 立即修复
- **P1 (高)**: 命名不一致、API 设计问题 → 24小时内修复  
- **P2 (中)**: 代码风格、注释缺失 → 一周内修复
- **P3 (低)**: 优化建议 → 下次迭代处理

### 执行机制
- **自动检查**: CI/CD 流水线集成规则验证
- **人工审核**: Code Review 强制执行核心规则
- **培训支持**: 提供规范培训和最佳实践指导

---

**最后更新**: 2025-01-09  
**维护责任**: 架构团队  
**反馈渠道**: GitHub Issues

---

> 💡 **记住**: 一致性胜过完美性。宁可全项目使用次优但统一的方案，也不要混合使用多种"最佳"方案。
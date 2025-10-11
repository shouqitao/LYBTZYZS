# 架构测试指南

## 概述

本文档描述了LYBT项目的架构测试体系，包括Server端和Desktop端的架构约束规则、测试运行方法和维护指南。

**最后更新**: 2025-10-09  
**Issue追踪**: #1078 - 架构测试修复与Server端质量保证体系优化

## 测试体系结构

```
tests/Architecture/
├── Server/
│   ├── LYBT.Server.ArchTests.csproj    # Server专用架构测试
│   └── ServerArchTests.cs              # 15个Server架构规则
├── LYBT.ArchTests.csproj               # 全局架构测试
├── ArchTests.cs                        # 通用架构规则
└── DesktopLayerArchTests.cs            # Desktop专用架构规则
```

### 解决方案分配

| 解决方案 | 包含的架构测试 | 说明 |
|---------|---------------|------|
| LYBT.Server.sln | Server/LYBT.Server.ArchTests | Server端专用架构约束 |
| LYBT.All.sln | LYBT.ArchTests | 全局+Desktop架构约束 |
| LYBT.Desktop.sln | 无架构测试 | Desktop开发不运行架构测试 |

## Server端架构规则 (15条)

### 1. API版本控制约束
**规则**: `ApiVersionTests_Controllers_Should_Use_V1_Routes_Only`  
**目的**: 确保所有Controller使用统一的API版本控制策略  
**要求**:
- 所有Controller必须有Route属性
- 路由模板必须使用 `api/v1/` 前缀或 `api/v{version:apiVersion}` 格式
- 例外: 健康检查控制器(`health`)、基础控制器类

### 2. Controller位置约束
**规则**: `Controllers_Should_Be_In_Controllers_Namespace`  
**目的**: 保持清晰的代码组织结构  
**要求**:
- 所有Controller必须在 `Controllers` 命名空间结尾
- 例外: BaseApiController、BaseSystemController等基础设施类

### 3. 服务命名约定
**规则**: `Services_Should_Have_Service_Suffix`  
**目的**: 统一服务类命名，提高代码可读性  
**要求**:
- Service命名空间中的类必须以下列后缀结尾:
  - Service、Manager、Provider、Summary、Rules
- 接口不受此约束

### 4. 禁用MediatR框架
**规则**: `Server_Should_Not_Use_MediatR`  
**目的**: 避免过度架构，保持简洁的三层架构  
**要求**:
- Server端不得引用或使用MediatR库
- 违反将导致构建失败

### 5. 禁用CQRS模式
**规则**: `Server_Should_Not_Use_CQRS_Pattern`  
**目的**: 遵循MVP原则，避免复杂模式  
**要求**:
- 不得使用CommandHandler、QueryHandler等CQRS组件
- 不得有Command、Query结尾的类（业务实体除外）

### 6. 禁用Redis
**规则**: `Server_Should_Not_Use_Redis`  
**目的**: 简化部署，使用内存缓存足够MVP需求  
**要求**:
- 不得引用StackExchange.Redis或Microsoft.Extensions.Caching.Redis
- 使用IMemoryCache进行缓存

### 7. ORM限制
**规则**: `Server_Should_Only_Use_Entity_Framework`  
**目的**: 统一数据访问技术栈  
**要求**:
- 仅允许使用Entity Framework Core
- 禁止Dapper、NHibernate等其他ORM

### 8. 依赖方向约束 - Entities层
**规则**: `Entities_Should_Not_Depend_On_Business_Layers`  
**目的**: 保持实体层纯净  
**要求**:
- Entities层不得依赖Infrastructure、WebAPI、Module等业务层
- 可以依赖Shared.Models（枚举）

### 9. 依赖方向约束 - Infrastructure层
**规则**: `Infrastructure_Should_Not_Depend_On_WebAPI`  
**目的**: 防止循环依赖  
**要求**:
- Infrastructure层不得依赖WebAPI层
- 保持单向依赖: WebAPI → Infrastructure → Entities

### 10. DTO命名约束
**规则**: `DTOs_Should_Have_Dto_Suffix`  
**目的**: 明确数据传输对象  
**要求**:
- Dto/DTO命名空间中的类必须以Dto或DTO结尾
- 确保DTO与Entity区分明确

### 11. 异步方法约定
**规则**: `Service_IO_Methods_Should_Be_Async`  
**目的**: 防止I/O阻塞，提高性能  
**要求**:
- Service中涉及I/O操作的方法必须使用async/await
- 方法名包含create/update/delete/get/find/save时需要异步
- 例外: 配置方法、密码生成方法、缓存服务

### 12. 配置类位置约束
**规则**: `Configuration_Classes_Should_Be_In_Correct_Location`  
**目的**: 统一配置管理  
**要求**:
- Configuration类必须在以下命名空间之一:
  - Configuration
  - Data.Configurations
  - Extensions

### 13. 模块依赖约束
**规则**: `Modules_Should_Not_Have_Circular_Dependencies`  
**目的**: 保持模块独立性  
**要求**:
- Module之间不得相互依赖
- 例外: 共享的AuthService、Infrastructure组件

### 14. 安全授权约束
**规则**: `Controllers_Should_Have_Authorization_Attributes`  
**目的**: 确保API安全  
**要求**:
- 所有Controller必须有Authorize或AllowAnonymous属性
- 可以在类级别或方法级别设置
- 例外: BaseController、RootHealthController

### 15. P2基础设施强化
**规则**: `P2_Infrastructure_Hardening_Rules`  
**目的**: 确保生产就绪的基础设施  
**要求**:
- 必须存在日志配置类（包含Log和Configuration）
- 必须存在安全配置类（包含Security/Auth和Configuration）
- 必须存在数据库配置类（包含Database/DbContext）

## 运行架构测试

### Server端架构测试
```powershell
# 运行Server架构测试
dotnet test tests/Architecture/Server/LYBT.Server.ArchTests.csproj -c Release

# 在Server解决方案中运行所有测试（包含架构测试）
dotnet test LYBT.Server.sln -c Release
```

### 全局架构测试
```powershell
# 运行全局架构测试（包含Desktop）
dotnet test tests/Architecture/LYBT.ArchTests.csproj -c Release

# 在All解决方案中运行
dotnet test LYBT.All.sln --filter "FullyQualifiedName~ArchTests" -c Release
```

## 测试维护指南

### 添加新的架构规则

1. **确定规则范围**
   - Server专用 → 添加到 `ServerArchTests.cs`
   - Desktop专用 → 添加到 `DesktopLayerArchTests.cs`
   - 全局通用 → 添加到 `ArchTests.cs`

2. **编写测试方法**
```csharp
[Fact]
public void Your_Architecture_Rule_Name()
{
    var result = Types.InAssemblies(ServerAssemblies)
        .That()
        .HaveNameEndingWith("YourPattern")
        .Should()
        .FollowYourRule()
        .GetResult();

    Assert.True(result.IsSuccessful,
        $"架构违规: {string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? [])}");
}
```

3. **更新文档**
   - 在本文档中添加规则说明
   - 更新规则计数

### 处理测试失败

1. **分析失败原因**
```powershell
# 获取详细失败信息
dotnet test tests/Architecture/Server/LYBT.Server.ArchTests.csproj -c Release --logger "console;verbosity=detailed"
```

2. **决策流程**
   - 代码违反了合理的架构约束 → 修复代码
   - 架构规则过于严格 → 调整规则，添加合理例外
   - 特殊情况需要豁免 → 在测试中添加特定排除

3. **常见豁免模式**
```csharp
// 排除特定类名
.And()
.DoNotHaveName("SpecialCaseController")

// 排除包含特定字符串的类
var filteredTypes = types.Where(t => 
    !t.Name.Contains("Legacy") &&
    !t.Name.Contains("Migration"))
```

## 故障排除

### 常见问题

#### 1. 编译错误: 找不到类型或命名空间
**原因**: 项目引用不完整  
**解决**: 检查.csproj文件的ProjectReference配置

#### 2. 测试发现0个测试
**原因**: 测试项目配置错误  
**解决**: 
- 确保 `<IsTestProject>true</IsTestProject>`
- 验证xUnit包引用正确

#### 3. 程序集加载失败
**原因**: 构建输出路径不一致  
**解决**: 先构建整个解决方案再运行测试

#### 4. 意外的架构违规
**原因**: 新代码未遵循约定  
**解决**: 
- 查看失败消息了解具体违规类型
- 参考本文档相应规则章节
- 必要时咨询架构师

## 最佳实践

1. **持续集成**
   - 在CI/CD管道中包含架构测试
   - PR合并前必须通过所有架构测试

2. **定期审查**
   - 每季度审查架构规则的合理性
   - 根据项目发展调整规则

3. **团队教育**
   - 新成员入职时介绍架构约束
   - 在代码审查中强调架构规则

4. **渐进式改进**
   - 不要一次性添加过多规则
   - 先修复现有违规，再添加新规则

## 相关文档

- [Server模块设计标准](../server-module-design-standard.md)
- [开发标准](../../development/standards.md)
- [测试指南](../../development/testing-guide.md)

## 更新历史

| 日期 | 版本 | 变更内容 | Issue |
|------|------|---------|-------|
| 2025-10-09 | 1.0 | 初始版本，Server架构测试100%通过 | #1078 |
| 2025-10-09 | 1.1 | 分离Server和Desktop架构测试 | #1078 |
# LYBT 后端编程规范

## 1. 总体原则

### 1.1 架构原则
- 采用整洁架构（Clean Architecture）分层设计
- 基础设施层提供单一 `AppDbContext` 管理所有数据
- 业务模块独立开发但共享基础设施
- 严格遵循依赖注入原则

### 1.2 命名约定
- **控制器**：`{实体名}Controller`
- **服务接口**：`I{实体名}Service`
- **服务实现**：`{实体名}Service`
- **仓储接口**：`I{实体名}Repository`
- **仓储实现**：`{实体名}Repository`

## 2. 基础数据管理规范

### 2.1 数据删除策略
所有基础数据（用户、草药、患者、医生、模板等）采用软删除策略：
- **不提供物理删除功能**
- 使用 `IsActive` 或 `IsEnabled` 字段控制数据状态
- 删除操作转换为禁用操作
- 保持数据完整性和可追溯性

### 2.2 API 设计规范
每个基础数据控制器应提供以下标准接口：

```csharp
// 禁用单个记录
[HttpPatch("{id}/disable")]
public async Task<IActionResult> Disable(Guid id)

// 启用单个记录
[HttpPatch("{id}/enable")]
public async Task<IActionResult> Enable(Guid id)

// 切换状态（推荐）
[HttpPatch("{id}/toggle-status")]
public async Task<IActionResult> ToggleStatus(Guid id)

// 批量禁用
[HttpPatch("batch-disable")]
public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto)

// 批量启用
[HttpPatch("batch-enable")]
public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto)

// 不提供 DELETE 端点
// [HttpDelete("{id}")] - 禁止使用
```

### 2.3 服务层实现
服务层必须实现对应的软删除方法：

```csharp
public interface IBaseService<TEntity>
{
    Task<bool> DisableAsync(Guid id, Guid operatorId, string operatorName);
    Task<bool> EnableAsync(Guid id, Guid operatorId, string operatorName);
    Task<int> BatchDisableAsync(List<Guid> ids, Guid operatorId, string operatorName);
    Task<int> BatchEnableAsync(List<Guid> ids, Guid operatorId, string operatorName);
}
```

### 2.4 数据模型要求
所有支持软删除的实体必须包含以下字段：

```csharp
public class BaseEntity
{
    public bool IsActive { get; set; } = true;  // 或 IsEnabled
    public DateTime? DisabledTime { get; set; }  // 禁用时间
    public Guid? DisabledBy { get; set; }       // 禁用操作者
}
```

## 3. API 响应规范

### 3.1 统一响应格式
所有 API 响应必须使用 `ApiResponse<T>` 包装：

```csharp
return Ok(ApiResponse<object>.Success("操作成功"));
return BadRequest(ApiResponse<object>.Fail("操作失败", 400));
return NotFound(ApiResponse<object>.Fail("资源不存在", 404));
```

### 3.2 错误处理
- 业务异常返回 400 Bad Request
- 资源不存在返回 404 Not Found
- 系统异常返回 500 Internal Server Error
- 未授权返回 401 Unauthorized
- 权限不足返回 403 Forbidden

## 4. 安全规范

### 4.1 身份验证
- 所有控制器必须添加 `[Authorize]` 特性
- 公开接口需明确标注 `[AllowAnonymous]`

### 4.2 操作审计
所有数据修改操作必须记录：
- 操作者 ID (`operatorId`)
- 操作者名称 (`operatorName`)
- 操作时间
- 操作类型

### 4.3 权限控制
- 使用基于角色的授权 (RBAC)
- 敏感操作需要额外权限验证
- 禁用的数据仅管理员可查询

## 5. 性能优化

### 5.1 查询优化
- 使用分页避免大数据量查询
- 合理使用 Include 避免 N+1 问题
- 对热点数据使用缓存

### 5.2 缓存策略
```csharp
// 清除相关缓存
_cache.Remove("list_cache_key");
_cache.Remove($"detail_cache_key_{id}");
```

## 6. 日志规范

### 6.1 日志级别
- `Information`：正常业务操作
- `Warning`：潜在问题但不影响功能
- `Error`：错误但系统可恢复
- `Critical`：严重错误需立即处理

### 6.2 日志内容
```csharp
_logger.LogInformation("操作成功，实体ID: {EntityId}，操作者: {OperatorName}({OperatorId})", 
    id, operatorName, operatorId);
```

## 7. 异步编程

### 7.1 异步方法命名
所有异步方法必须以 `Async` 结尾：
```csharp
public async Task<bool> DisableAsync(Guid id)
```

### 7.2 异步最佳实践
- 避免 `async void`，除非是事件处理器
- 使用 `ConfigureAwait(false)` 在库代码中
- 避免阻塞异步代码

## 8. 依赖注入

### 8.1 服务注册
在各模块的 `Module.cs` 中注册服务：
```csharp
public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
```

### 8.2 生命周期
- `Scoped`：每个请求一个实例（推荐）
- `Transient`：每次注入新实例
- `Singleton`：全局单例（慎用）

## 9. 测试规范

### 9.1 单元测试
- 服务层方法必须有对应单元测试
- 测试覆盖率目标 > 80%
- 使用 xUnit + Moq 框架

### 9.2 集成测试
- 关键 API 端点需要集成测试
- 使用 TestServer 进行测试
- 测试数据隔离，避免污染

## 10. 代码审查清单

- [ ] 是否遵循软删除策略？
- [ ] API 响应是否使用 ApiResponse 包装？
- [ ] 是否记录操作审计信息？
- [ ] 是否处理所有异常情况？
- [ ] 是否添加适当的日志？
- [ ] 是否清理相关缓存？
- [ ] 是否使用异步方法？
- [ ] 是否添加权限验证？

---

**最后更新时间**：2025-08-03

**注意**：本规范是活文档，会根据项目发展持续更新。所有开发人员必须遵循这些规范以确保代码质量和一致性。
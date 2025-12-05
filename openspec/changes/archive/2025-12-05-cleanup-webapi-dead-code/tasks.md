# Tasks: cleanup-webapi-dead-code

## 1. 删除死代码文件

### 1.1 删除UnifiedConfigurationOptions.cs
- [x] 1.1.1 删除 `src/Server/Services/LYBT.WebAPI/Configuration/UnifiedConfigurationOptions.cs`

## 2. 清理重复代码

### 2.1 清理UnifiedMiddlewareConfiguration.cs中的重复类
- [x] 2.1.1 删除 `Extensions/UnifiedMiddlewareConfiguration.cs` 中的 `SecurityHeadersMiddleware` 静态类（行131-173）
- [x] 2.1.2 删除相关的空行和不再需要的using语句

## 3. 验证

### 3.1 编译验证
- [x] 3.1.1 运行 `dotnet build LYBT.All.sln`
- [x] 3.1.2 确认0错误0警告

### 3.2 测试验证
- [x] 3.2.1 运行WebAPI相关测试
- [x] 3.2.2 确认所有测试通过（50/50）

### 3.3 功能验证
- [x] 3.3.1 编译验证确保Swagger配置未受影响
- [x] 3.3.2 SecurityHeadersMiddleware（Middleware/）保留完整，安全响应头功能不受影响

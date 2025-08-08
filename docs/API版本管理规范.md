# API版本管理规范

## 概述

本文档定义了凌隐宝堂中医诊所诊疗系统的API版本管理规范，确保API的向后兼容性和平滑升级。

## 版本策略

### 版本格式

我们采用语义化版本（Semantic Versioning）：

- **主版本（Major）**: 包含不兼容的API更改
- **次版本（Minor）**: 向后兼容的功能性新增
- **修订版本（Patch）**: 向后兼容的问题修正

当前API版本：`v1.0`

### 版本控制方式

系统支持三种API版本指定方式：

1. **URL路径版本控制**（推荐）
   ```
   GET https://api.example.com/api/v1/users
   ```

2. **查询字符串版本控制**
   ```
   GET https://api.example.com/api/users?version=1.0
   ```

3. **请求头版本控制**
   ```
   GET https://api.example.com/api/users
   Header: X-Version: 1.0
   ```

## 实现规范

### 控制器配置

所有API控制器必须包含版本标记：

```csharp
using Asp.Versioning;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UsersController : BaseController
{
    // 控制器实现
}
```

### 多版本支持

当需要支持多个版本时：

```csharp
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PatientsController : BaseController
{
    [HttpGet]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetV1()
    {
        // v1.0 实现
    }
    
    [HttpGet]
    [MapToApiVersion("2.0")]
    public async Task<IActionResult> GetV2()
    {
        // v2.0 实现
    }
}
```

### 版本弃用

标记即将弃用的API版本：

```csharp
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DeprecatedController : BaseController
{
    // 控制器实现
}
```

## 版本迁移指南

### 新增版本检查清单

1. **评估变更影响**
   - [ ] 是否有破坏性变更？
   - [ ] 是否需要数据迁移？
   - [ ] 是否影响客户端集成？

2. **实施步骤**
   - [ ] 添加新版本标记
   - [ ] 实现版本特定的方法
   - [ ] 更新API文档
   - [ ] 更新客户端SDK

3. **测试验证**
   - [ ] 单元测试覆盖
   - [ ] 集成测试验证
   - [ ] 回归测试确认

### 版本兼容性矩阵

| API版本 | 客户端版本 | 支持状态 | 弃用日期 | 终止日期 |
|---------|------------|----------|----------|----------|
| v1.0    | 1.x        | 当前支持 | -        | -        |
| v2.0    | 2.x        | 开发中   | -        | -        |

## Swagger文档配置

确保每个API版本都有对应的Swagger文档：

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "凌隐宝堂中医诊所诊疗系统 API",
        Version = "v1",
        Description = "API v1.0 - 当前稳定版本"
    });
    
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "凌隐宝堂中医诊所诊疗系统 API",
        Version = "v2",
        Description = "API v2.0 - 开发预览版本"
    });
});
```

## 客户端集成建议

### 版本选择策略

1. **默认版本**: 当未指定版本时，使用 v1.0
2. **版本锁定**: 生产环境应锁定特定版本
3. **版本探测**: 客户端启动时检测可用版本

### 错误处理

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.15",
  "title": "Unsupported API Version",
  "status": 400,
  "detail": "The requested API version '0.9' is not supported.",
  "instance": "/api/v0.9/users"
}
```

## 最佳实践

1. **向后兼容优先**: 尽量通过扩展而非修改来演进API
2. **弃用通知期**: 至少提供6个月的弃用通知期
3. **版本文档**: 每个版本都应有完整的变更日志
4. **自动化测试**: 确保所有版本的API都有完整的测试覆盖

## 版本发布流程

1. **Alpha阶段**: 内部测试，可能有重大变更
2. **Beta阶段**: 公开测试，API基本稳定
3. **RC阶段**: 发布候选，仅修复关键问题
4. **GA阶段**: 正式发布，提供长期支持

## 监控和分析

建议监控以下指标：

- 各版本API的使用率
- 弃用API的调用频率
- 版本迁移进度
- 客户端版本分布

## 相关文档

- [API响应标准](./API响应标准.md)
- [前后端契约规范](./前后端契约规范.md)
- [开发规范](./开发规范.md)
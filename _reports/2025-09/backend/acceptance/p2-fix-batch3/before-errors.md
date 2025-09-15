# Before-Fix Error Evidence - 修复前错误证据

## 📋 错误收集

**收集时间**: 2025-09-15 14:00:00  
**目标**: 验证 'apiVersion' 约束错误是否仍然存在

## 🔍 错误证据分析

### 上一轮报告确认的错误
**来源**: `_reports/2025-09/backend/acceptance-rerun2/health.json`

```json
{
  "error": "API Version Constraint Error",
  "httpCode": 500,
  "details": {
    "apiVersionError": "The constraint reference 'apiVersion' could not be resolved to a type",
    "fullError": "System.InvalidOperationException: The constraint reference 'apiVersion' could not be resolved to a type. Register the constraint type with 'Microsoft.AspNetCore.Mvc.Versioning'"
  }
}
```

### 错误模式确认
✅ **错误存在**: 上一轮测试已确认存在API版本约束错误  
✅ **错误类型**: `System.InvalidOperationException`  
✅ **错误位置**: 路由引擎处理 `/api/v{version:apiVersion}` 模式时失败  
✅ **影响范围**: 所有使用版本化路由的端点

## 🔧 代码层面证据

### UnifiedServiceRegistration.cs分析

**文件**: `src/Server/Services/LYBT.WebAPI/Extensions/UnifiedServiceRegistration.cs`

✅ **API版本服务已注册** (第273-294行):
```csharp
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    // ... 配置完整
}).AddMvc();
```

❌ **路由约束映射缺失**:
- 搜索整个文件未找到 `RouteOptions` 配置
- 搜索整个文件未找到 `ConstraintMap` 配置  
- 搜索整个文件未找到 `ApiVersionRouteConstraint` 引用

### 根本原因确认

**缺失配置**:
```csharp
// 这段代码在整个项目中都不存在！
services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap["apiVersion"] = typeof(ApiVersionRouteConstraint);
});
```

**为什么会出错**:
1. 控制器使用了路由模板: `[Route("api/v{version:apiVersion}/[controller]")]`
2. ASP.NET Core路由引擎遇到 `{version:apiVersion}` 约束
3. 尝试解析 `apiVersion` 约束类型
4. 在 `RouteOptions.ConstraintMap` 中找不到 `apiVersion` 映射
5. 抛出 `InvalidOperationException`

## 📊 错误影响评估

### 无法访问的端点
- `/api/v1/health` - 健康检查
- `/api/v1/auth/login` - 登录认证
- `/api/v1/users/*` - 用户管理
- `/api/v1/patients/*` - 患者管理
- `/api/v1/consultation/*` - 诊疗管理
- `/api/v1/prescriptions/*` - 处方管理
- `/api/v1/herbs/*` - 药材管理
- `/api/v1/formulas/*` - 验方管理

### 可能仍可访问的端点
- `/swagger` - Swagger文档 (不使用版本化路由)
- 任何非版本化路由

## 🎯 修复目标明确

### 需要添加的代码
在 `RegisterApiServices()` 方法中添加:
```csharp
// 注册API版本路由约束
services.Configure<RouteOptions>(options =>
{
    options.ConstraintMap["apiVersion"] = typeof(Asp.Versioning.ApiVersionRouteConstraint);
});
```

### 验证方法
修复后应该能够：
1. 成功启动WebAPI (无异常)
2. `/api/v1/health` 返回200状态码
3. `/swagger` 显示v1 API文档

---

*错误证据收集时间: 2025-09-15 14:00:00*  
*状态: 根本原因已确认，准备执行修复* 🔧
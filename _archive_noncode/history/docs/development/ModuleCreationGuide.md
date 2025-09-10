# UltraThink双层架构模块创建指南

> 🎯 **目标**: 30分钟内创建遵循UltraThink双层架构的完整新模块

## 📋 概述

本指南将指导您创建遵循凌隐宝堂中医诊所系统UltraThink双层架构标准的新模块。通过使用提供的Visual Studio代码片段和标准化流程，您可以在30分钟内完成一个功能完整的新模块。

### UltraThink双层架构简介

UltraThink双层架构是项目采用的标准化架构模式，包含以下层次：

```
主Module类 (纯委托层)
    ├── QueryService (查询专业层)
    └── BusinessService (业务逻辑层)
```

**关键特点**:
- **纯委托模式**: 主Module类只负责请求分发，不包含业务逻辑
- **职责分离**: QueryService专注查询，BusinessService专注业务操作
- **类型安全**: 所有操作都通过强类型API接口
- **统一响应**: 使用`ServiceResult<T>`统一响应格式

## 🚀 快速开始

### 先决条件

- [ ] Visual Studio 2022 或更新版本
- [ ] 已安装代码片段文件 `build/CodeSnippets/NewModule.snippet`
- [ ] 熟悉项目的基本结构和约定

### 准备工作

1. **安装代码片段**（仅需一次）：
   ```
   1. 复制 build/CodeSnippets/NewModule.snippet 到剪贴板
   2. 在Visual Studio中：工具 → 代码片段管理器
   3. 导入 → 选择代码片段文件
   4. 验证快捷键 "ultramodule" 可用
   ```

2. **确认项目结构**：
   ```
   src/Client/Desktop/Modules/
   ├── [现有模块示例]
   └── [您的新模块目录]
   ```

## 📝 创建步骤详解

### 第一阶段：项目结构设置 (5分钟)

#### 1.1 创建模块目录结构

```bash
# 假设创建 Orders 模块
src/Client/Desktop/Modules/Orders/
├── Interfaces/          # 服务接口定义
├── Services/           # 服务实现
├── ViewModels/         # 视图模型
├── Views/             # 用户界面
├── OrdersModule.cs    # Prism模块注册
└── README.md          # 模块文档
```

#### 1.2 创建项目文件

创建 `LYBT.Desktop.Orders.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Shell\LYBT.Desktop.Shell.csproj" />
    <ProjectReference Include="..\..\..\..\Shared\Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Prism.DryIoc" Version="9.0.537" />
    <PackageReference Include="Refit" Version="7.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 第二阶段：使用代码片段生成代码 (10分钟)

#### 2.1 生成核心服务代码

1. **创建空白.cs文件**：`Services/OrderModule.cs`
2. **输入代码片段快捷键**：`ultramodule` + Tab
3. **填写参数**：
   - `ModuleName`: `Orders`
   - `EntityName`: `Order` 
   - `modulename`: `orders`

#### 2.2 整理生成的代码

代码片段会生成完整的UltraThink架构代码，包括：

- **主服务接口** (`IOrdersService`)
- **QueryService接口和实现** (`IOrdersQueryService`, `OrdersQueryService`)
- **BusinessService接口和实现** (`IOrdersBusinessService`, `OrdersBusinessService`)
- **主Module实现** (`OrdersModule` - 纯委托模式)
- **Prism模块注册类**

#### 2.3 创建文件并分离代码

将生成的代码按以下结构分离到不同文件：

```
Interfaces/
├── IOrdersQueryService.cs
└── IOrdersBusinessService.cs

Services/
├── OrderModule.cs        # 主Module类
├── OrdersQueryService.cs
└── OrdersBusinessService.cs

OrdersModule.cs          # Prism注册类
```

### 第三阶段：API接口集成 (8分钟)

#### 3.1 创建API接口定义

在 `Shared.Interfaces.Api` 中创建 `IOrdersApi.cs`：

```csharp
using Refit;
using LYBT.Shared.Models.Api.Common;
using LYBT.Shared.Models.Contracts.Orders;

namespace LYBT.Shared.Interfaces.Api;

/// <summary>
/// Orders模块API接口 - Refit定义
/// </summary>
public interface IOrdersApi
{
    [Get("/api/v1/orders/{id}")]
    Task<ApiResponse<ApiResponse<OrderDto>>> GetByIdAsync(Guid id);

    [Get("/api/v1/orders")]
    Task<ApiResponse<ApiResponse<PagedResult<OrderDto>>>> GetPagedAsync(
        int page = 1, int pageSize = 20, string? keyword = null);

    [Get("/api/v1/orders/search")]
    Task<ApiResponse<ApiResponse<List<OrderDto>>>> SearchAsync(string keyword);

    [Post("/api/v1/orders")]
    Task<ApiResponse<ApiResponse<OrderDto>>> CreateAsync([Body] OrderMutationDto dto);

    [Put("/api/v1/orders/{id}")]
    Task<ApiResponse<ApiResponse<OrderDto>>> UpdateAsync(Guid id, [Body] OrderMutationDto dto);

    [Delete("/api/v1/orders/{id}")]
    Task<ApiResponse<ApiResponse<object>>> DeleteAsync(Guid id);

    [Patch("/api/v1/orders/{id}/enable")]
    Task<ApiResponse<ApiResponse<object>>> EnableAsync(Guid id);

    [Patch("/api/v1/orders/{id}/disable")]
    Task<ApiResponse<ApiResponse<object>>> DisableAsync(Guid id);
}
```

#### 3.2 注册API服务

在 `Shell/Extensions/ServiceCollectionExtensions.cs` 中添加：

```csharp
// Orders模块API
services.AddRefitClient<IOrdersApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthenticationHandler>();
```

### 第四阶段：数据模型定义 (5分钟)

#### 4.1 创建数据传输对象

在 `Shared.Models.Contracts.Orders` 中创建必要的DTO：

```csharp
// OrderDto.cs
public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public OrderStatus Status { get; set; }
    // ... 其他属性
}

// OrderMutationDto.cs
public class OrderMutationDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    // ... 其他可变属性
}

// OrderPagedQueryDto.cs
public class OrderPagedQueryDto : PagedQueryDto
{
    // 特定于Orders的查询参数
}
```

#### 4.2 添加到主服务接口

在 `Shared.Interfaces.Services` 中添加主服务接口到相应的命名空间。

### 第五阶段：集成和测试 (2分钟)

#### 5.1 更新解决方案

1. **添加项目引用**到主解决方案
2. **更新Shell项目**的模块加载配置
3. **编译并验证**无编译错误

#### 5.2 基础功能验证

1. **服务注册验证**: 确认依赖注入容器能正确解析服务
2. **API连接测试**: 验证Refit客户端配置正确
3. **基础UI集成**: 添加简单的导航入口点

## ✅ 完成检查清单

### 架构合规性
- [ ] 主Module类使用纯委托模式，无业务逻辑
- [ ] QueryService专注查询操作，无状态变更
- [ ] BusinessService处理所有CRUD和状态管理
- [ ] 所有方法使用`ServiceResult<T>`响应格式
- [ ] 所有异步操作正确使用async/await

### 代码质量
- [ ] 所有公共方法包含XML文档注释
- [ ] 使用C# 12现代语法（主构造函数等）
- [ ] 正确的异常处理和日志记录
- [ ] 参数null检查（ArgumentNullException.ThrowIfNull）
- [ ] 遵循项目命名约定

### 集成测试
- [ ] 编译无错误无警告
- [ ] 依赖注入服务正确注册
- [ ] API接口Refit配置正确
- [ ] 与现有模块架构一致

### 文档和维护性
- [ ] 创建模块README.md
- [ ] 添加必要的代码注释
- [ ] 更新相关架构文档

## 🛠️ 故障排除

### 常见问题

#### 1. 编译错误：找不到依赖类型

**解决方案**:
- 检查项目引用是否正确添加
- 确认using语句包含必要的命名空间
- 验证NuGet包版本与项目一致

#### 2. 依赖注入解析失败

**解决方案**:
- 确认所有服务在Prism模块中正确注册
- 检查接口和实现类型匹配
- 验证构造函数依赖项都已注册

#### 3. API调用失败

**解决方案**:
- 检查Refit接口定义和后端API一致
- 确认HttpClient配置和基础URL
- 验证认证处理程序正确添加

#### 4. 架构模式不符合UltraThink标准

**解决方案**:
- 参考现有模块（如Users, Patients）的实现
- 使用 `docs/templates/ModulePatternValidation.md` 进行验证
- 确保主Module类不包含业务逻辑

### 性能优化建议

1. **使用缓存**: 对频繁查询的数据考虑添加内存缓存
2. **批量操作**: BusinessService中实现批量操作方法
3. **异步最佳实践**: 避免async void，正确处理异步异常

## 📚 相关资源

### 文档链接
- [UltraThink双层架构概述](../architecture/ultrathink-architecture-overview.md)
- [API接口设计标准](../api/api-design-standards.md)
- [模块模式验证指南](../templates/ModulePatternValidation.md)

### 代码示例
- **参考实现**: `src/Client/Desktop/Modules/Users/`
- **模板代码**: `build/CodeSnippets/NewModule.snippet`
- **测试示例**: `tests/Desktop/Modules/Users/`

### 开发工具
- **Visual Studio代码片段**: `ultramodule`
- **项目模板**: 计划中的dotnet模板
- **验证工具**: 自动架构模式检查器

## 🎯 最佳实践

### 设计原则
1. **单一职责**: 每个服务类专注一个领域
2. **依赖倒置**: 始终注入接口，不依赖具体实现
3. **开闭原则**: 易于扩展，无需修改现有代码
4. **一致性**: 遵循项目既定的架构模式

### 命名约定
- **模块名**: PascalCase，复数形式 (Orders, Products)
- **实体名**: PascalCase，单数形式 (Order, Product)
- **服务类**: `{Module}QueryService`, `{Module}BusinessService`
- **接口名**: `I{ServiceName}`

### 代码组织
- **按功能分组**: 相关方法归类到#region块
- **保持简洁**: 每个方法单一职责，易于测试
- **文档齐全**: 所有公共成员包含XML注释

---

## 📞 支持

如遇到问题或需要支持，请：

1. **查阅文档**: 首先检查相关技术文档
2. **参考示例**: 查看现有模块的实现方式
3. **团队协作**: 与其他开发团队成员讨论
4. **更新文档**: 发现新问题时及时更新本指南

**成功创建模块后，请考虑为项目贡献您的经验和改进建议！**
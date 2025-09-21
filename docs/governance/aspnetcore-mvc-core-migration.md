# Microsoft.AspNetCore.Mvc.Core 包迁移说明

## 背景

在执行服务端硬化计划时，发现 `Microsoft.AspNetCore.Mvc.Core` 版本停留在 2.2.5，无法升级到 8.0.x 版本。

## 问题分析

### 1. 包的历史变更

- **ASP.NET Core 2.x 时代**：`Microsoft.AspNetCore.Mvc.Core` 作为独立的 NuGet 包存在
- **ASP.NET Core 3.0+ 变革**：Microsoft 重组了包结构，引入了框架引用（FrameworkReference）概念
- **.NET 5/6/7/8**：这些功能已经内置在 `Microsoft.AspNetCore.App` 框架中

### 2. 当前使用情况

Infrastructure 层只使用了 MVC Core 中的基础类型：
- `ControllerBase` - 控制器基类
- `ActionResult` - 操作结果类型
- MVC 特性和过滤器

## 解决方案

### 从 NuGet 包迁移到框架引用

**之前（使用 NuGet 包）：**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" />
</ItemGroup>
```

**现在（使用框架引用）：**
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

### 优势

1. **版本统一**：自动使用与 .NET 8 SDK 匹配的版本
2. **减少依赖**：无需管理单独的包版本
3. **性能优化**：框架引用在运行时有更好的优化
4. **简化维护**：减少包版本冲突的可能性

## 影响范围

- ✅ **编译**：无影响，所有类型继续可用
- ✅ **运行时**：无影响，功能保持不变
- ✅ **部署**：无影响，框架引用在目标环境自动解析
- ✅ **向后兼容**：完全兼容，API 未变更

## 最佳实践

对于 .NET Core 3.0+ 项目：

1. **Web 项目**：自动包含 `Microsoft.AspNetCore.App` 框架引用
2. **类库项目**：如需 ASP.NET Core 类型，显式添加框架引用
3. **避免混用**：不要同时使用框架引用和相关的 NuGet 包

## 验证结果

```bash
# 编译测试
dotnet build LYBT.Server.sln
# 结果：✅ 成功，0 错误

# 架构测试
dotnet test tests/Architecture/LYBT.ArchTests.csproj
# 结果：✅ 所有测试通过
```

## 总结

这次迁移是必要的现代化改进，符合 .NET 8 的最佳实践。通过使用框架引用替代过时的 NuGet 包，我们：

- 消除了版本管理的复杂性
- 确保了与 .NET 8 生态系统的完美兼容
- 为未来的 .NET 版本升级铺平了道路

---

*更新日期：2025-09-21*
*关联任务：Server硬化计划 P1 依赖版本治理*
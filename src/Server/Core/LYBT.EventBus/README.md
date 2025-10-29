# LYBT.EventBus - 事件总线核心库（⚠️ 已废弃）

## ⚠️ 重要提示

**此项目已废弃，请使用新版本 [LYBT.Core.EventBus](../LYBT.Core.EventBus/README.md)**

---

## 📦 项目定位

- **层级**:Server端
- **类型**:核心库(事件总线 + 模块管理)
- **状态**:⚠️ **已废弃** - 此项目已被 `LYBT.Core.EventBus` 取代
- **迁移日期**:2025年初（根据项目结构推测）

## 🔄 迁移说明

### 为什么废弃？

`LYBT.EventBus` 和 `LYBT.Core.EventBus` 拥有完全相同的代码结构（23个文件，相同的目录结构），但命名空间不同：

- **旧版本**:`LYBT.EventBus.*`（当前项目）
- **新版本**:`LYBT.Core.EventBus.*`

项目引用分析显示：
- ❌ **LYBT.EventBus**: 无任何项目引用（已废弃）
- ✅ **LYBT.Core.EventBus**: 被8个业务模块引用（当前使用）

### 如何迁移？

**如果你正在引用此项目**，请按以下步骤迁移：

1. **更新项目引用**（在.csproj中）：
```xml
<!-- 旧引用（删除） -->
<ProjectReference Include="..\..\Core\LYBT.EventBus\LYBT.EventBus.csproj" />

<!-- 新引用（添加） -->
<ProjectReference Include="..\..\Core\LYBT.Core.EventBus\LYBT.Core.EventBus.csproj" />
```

2. **更新命名空间**（在.cs文件中）：
```csharp
// 旧命名空间（替换）
using LYBT.EventBus.Abstractions;
using LYBT.EventBus.Events;
using LYBT.EventBus.Module;

// 新命名空间（使用）
using LYBT.Core.EventBus.Abstractions;
using LYBT.Core.EventBus.Events;
using LYBT.Core.EventBus.Module;
```

3. **验证编译**：
```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

4. **删除旧引用**：
   - 确认所有项目已迁移到 `LYBT.Core.EventBus`
   - 可考虑删除 `src/Server/Core/LYBT.EventBus/` 目录

## 📚 完整文档

**请查阅新版本文档**：[LYBT.Core.EventBus README](../LYBT.Core.EventBus/README.md)

新版本提供：
- 完整的代码结构说明（28个文件详细列表）
- 事件总线7个核心方法说明
- 模块管理器28个方法说明
- 5个完整集成示例（事件发布/订阅、模块生命周期管理）
- 依赖关系图和技术栈说明

---

## 📂 代码结构（简略）

此项目与 `LYBT.Core.EventBus` 结构完全相同，包含：

```
LYBT.EventBus/
├── Abstractions/
│   ├── IEventBus.cs                 # 事件总线接口(7个方法)
│   ├── IIntegrationEvent.cs
│   └── IIntegrationEventHandler.cs
├── Events/
│   └── IntegrationEventBase.cs
├── Implementation/
│   └── InMemoryEventBus.cs          # 进程内事件总线实现
├── Module/                          # 模块化架构支持
│   ├── IModuleManager.cs            # 模块管理器接口(28个方法)
│   ├── Events/                      # 5个模块事件
│   └── Communication/               # 模块间通信示例
├── Services/
│   └── EventBusHostedService.cs     # 后台服务
└── Extensions/
    └── ServiceCollectionExtensions.cs # 依赖注入扩展
```

**共23个文件**，与新版本完全一致。

---

## 🔗 依赖关系

### 依赖的项目
- **无内部项目依赖**

### 被依赖项目
- ❌ **无项目引用此废弃版本** - 所有模块已迁移到 `LYBT.Core.EventBus`

### NuGet包
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.AspNetCore.Http.Abstractions
- Microsoft.AspNetCore.Hosting.Abstractions
- Microsoft.Extensions.Configuration.Abstractions

---

## 🛠 技术栈

**与新版本一致**：
- .NET 8
- In-Memory Event Bus
- 模块化架构
- 异步编程
- 泛型编程
- 依赖分析

---

## ⏭ 下一步

**请直接使用新版本**：[LYBT.Core.EventBus](../LYBT.Core.EventBus/README.md)

**如需历史参考**：此项目代码保留在仓库中，可作为迁移前的历史记录查阅。

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组（⚠️ 此项目不再维护）
**迁移指导**:所有新功能开发请使用 LYBT.Core.EventBus

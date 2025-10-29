# LYBT.Shared.Interfaces

> **真正共享接口契约库** - 保留项目，暂无实现
> **模块状态**: ⚠️ **空项目** | 📦 **保留用于未来扩展** | **2025-10-12更新**

## 📦 项目定位

- **层级**: Shared层（跨端共享）
- **类型**: 接口定义库（Contract Definitions）
- **职责**: 定义真正跨平台共享的接口契约，作为Server/Desktop/Mobile多端共享的统一抽象层。本项目遵循"真正共享才放入"原则，仅包含多个层级或平台真正需要的接口定义，避免过度抽象和伪共享。当前为空项目，保留用于未来扩展。

## 🎯 项目概述

LYBT.Shared.Interfaces 项目当前为空，保留作为未来定义 Server/Desktop/Mobile 真正共享接口契约的容器。

**当前状态**: 空项目（无接口定义）
**架构原则**: 真正跨平台共享的接口才放入此项目
**技术栈**: .NET 8

### 核心理念

**"Shared" 必须名副其实**：
- ✅ 多端真正共享的接口才放入
- ❌ 单端专用接口不应放入（避免"伪共享"）
- ⚠️ 过早抽象是万恶之源

## 📂 代码结构

```
LYBT.Shared.Interfaces/
├── LYBT.Shared.Interfaces.csproj  # 项目文件
└── README.md                        # 本文档（你正在阅读）
```

**说明**:
- **空项目**: 当前无任何接口定义
- **保留目的**: 未来真正跨平台共享的接口定义
- **最小依赖**: 仅依赖 .NET 8 基础框架，无其他NuGet包

## 🔗 依赖关系

### 依赖的项目
**无依赖** - 本项目应保持最小依赖，仅依赖.NET 8基础框架

### 被依赖项目
**当前无项目依赖** - 空项目阶段无被依赖关系

**未来预期被依赖**（当有接口定义时）:
1. **LYBT.Server.Interfaces** - Server端接口可能引用共享接口
2. **LYBT.Desktop.Contracts** - Desktop端契约可能引用共享接口
3. **LYBT.Mobile.Contracts** - Mobile端契约可能引用共享接口（未来）
4. **业务模块** - 需要跨端共享接口的业务模块

### NuGet包
**无NuGet依赖** - 保持最小依赖，避免引入平台特定的包

## 🛠 技术栈

- **.NET 8**: 目标框架
- **C# 12**: 编程语言
- **接口定义**: 纯接口契约，无实现代码
- **跨平台兼容**: 确保Server/Desktop/Mobile多端可用

## 📜 迁移历史

### 2025-10-12 - Refit API 接口迁移（Issue #1204）

根据 ADR-002 架构决策，Refit HTTP 客户端接口从 `Shared.Interfaces` 迁移到 `Desktop.Contracts`：

**迁移的接口**（8个）:
- ❌ ~~`Api/IAuthApi.cs`~~ → ✅ `Desktop.Contracts/Api/IAuthApi.cs`
- ❌ ~~`Api/IUserApi.cs`~~ → ✅ `Desktop.Contracts/Api/IUserApi.cs`
- ❌ ~~`Api/IPatientApi.cs`~~ → ✅ `Desktop.Contracts/Api/IPatientApi.cs`
- ❌ ~~`Api/IMedicalCaseApi.cs`~~ → ✅ `Desktop.Contracts/Api/IMedicalCaseApi.cs`
- ❌ ~~`Api/IConsultationApi.cs`~~ → ✅ `Desktop.Contracts/Api/IConsultationApi.cs`
- ❌ ~~`Api/IPrescriptionApi.cs`~~ → ✅ `Desktop.Contracts/Api/IPrescriptionApi.cs`
- ❌ ~~`Api/IHerbApi.cs`~~ → ✅ `Desktop.Contracts/Api/IHerbApi.cs`
- ❌ ~~`Api/IFormulaApi.cs`~~ → ✅ `Desktop.Contracts/Api/IFormulaApi.cs`

**迁移原因**（"伪共享"问题）:
1. **单端使用**: 这些接口仅被 Desktop 客户端使用（13个项目），Server端不需要
2. **Refit特定**: Refit是HTTP客户端库，专用于Desktop调用WebAPI，不适合作为"共享接口"
3. **命名误导**: "Shared" 命名造成误导，不符合架构分层原则
4. **架构决策**: 符合 ADR-002 桌面端直接调用 WebAPI 的架构决策

**迁移后架构改进**:
- ✅ Desktop专用接口归位到 `Desktop.Contracts`
- ✅ Shared.Interfaces 回归"真正共享"的定位
- ✅ 架构分层清晰，职责明确

**相关文档**:
- Issue: [#1204 Refit 接口迁移到 Desktop.Contracts](https://github.com/shouqitao/LYBTZYZS/issues/1204)
- ADR: `docs/explanation/architecture/decisions/ADR-002-desktop-remove-service-layer.md`
- 新位置: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/`

## 🏗️ 架构决策背景（ADR-002）

### 为什么清空 Shared.Interfaces？

**问题**: 原有8个Refit接口不符合"真正共享"的定义

**决策**: ADR-002 桌面端直接调用 WebAPI
- Desktop端通过Refit直接调用WebAPI，无需Service层
- Refit接口是Desktop专用技术，不应放入Shared层
- Server端使用IService接口，不需要Refit接口

**结果**:
1. Shared.Interfaces 清空，回归保留状态
2. Desktop专用接口迁移到 Desktop.Contracts
3. 架构更清晰：Desktop.Contracts（Desktop专用）vs. Shared.Interfaces（真正共享）

## 🔮 未来用途

此项目保留用于以下场景：

### ✅ 适合放入此项目的接口

**场景A: 跨端共享的业务接口**
```csharp
// 示例：通知推送接口（Server/Desktop/Mobile都需要）
public interface INotificationService
{
    Task SendNotificationAsync(string userId, string message);
    Task<IEnumerable<Notification>> GetNotificationsAsync(string userId);
}
```

**场景B: 跨平台的数据访问抽象**
```csharp
// 示例：配置管理接口（Server/Desktop/Mobile都需要）
public interface IConfigurationProvider
{
    T GetValue<T>(string key);
    void SetValue<T>(string key, T value);
}
```

**场景C: 通用认证授权抽象**
```csharp
// 示例：认证上下文接口（Server/Desktop/Mobile都需要）
public interface IAuthContext
{
    Guid UserId { get; }
    string UserName { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
}
```

**共同特征**:
- ✅ **Server ↔ Desktop 双向共享**的接口定义
- ✅ **Desktop ↔ Mobile 跨客户端共享**的接口定义
- ✅ **Server ↔ Mobile 共享**的接口定义
- ✅ **三端（Server/Desktop/Mobile）共享**的通用接口

### ❌ 不适合放入此项目的接口

**反例A: Desktop专用接口**
```csharp
// ❌ Refit HTTP客户端接口（仅Desktop使用）
public interface IUserApi
{
    [Get("/api/v1/users")]
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync([Query] UserSearchDto searchDto);
}
// → 应放入: Desktop.Contracts/Api/IUserApi.cs
```

**反例B: Server专用接口**
```csharp
// ❌ 业务服务接口（仅Server使用）
public interface IUserService
{
    Task<ServiceResult<UserDto>> CreateAsync(UserCreateDto dto);
    Task<ServiceResult<PagedResult<UserDto>>> GetPagedAsync(UserSearchDto searchDto);
}
// → 应放入: Server.Interfaces/IUserService.cs
```

**反例C: Mobile专用接口**
```csharp
// ❌ 平台特定接口（仅Mobile使用）
public interface IMobileDeviceService
{
    Task<string> GetDeviceIdAsync();
    Task<LocationInfo> GetCurrentLocationAsync();
}
// → 应放入: Mobile.Contracts/（未来）
```

**判断规则**:
- ❌ Desktop 专用接口 → 应放入 `Desktop.Contracts`
- ❌ Server 专用接口 → 应放入 `Server.Interfaces`
- ❌ Mobile 专用接口 → 应放入 `Mobile.Contracts`（未来）
- ❌ 技术框架特定接口（Refit/Prism/Avalonia/EFCore等）→ 应放入对应层级

## 🎯 设计原则

### 1. 真正共享才放入
**要求**: 只有多个层级或多个平台真正需要的接口才放入此项目

**判断流程**:
```
接口定义 → 询问自己3个问题：
1. Server端需要吗？
2. Desktop端需要吗？
3. Mobile端需要吗？（未来）

如果≥2个答案为"是" → ✅ 适合放入 Shared.Interfaces
如果≤1个答案为"是" → ❌ 放入对应层级（Desktop.Contracts/Server.Interfaces/Mobile.Contracts）
```

**示例对比**:
- `INotificationService`: Server发送通知 + Desktop/Mobile接收通知 → ✅ 真正共享
- `IUserApi` (Refit): 仅Desktop使用 → ❌ Desktop.Contracts
- `IUserService`: 仅Server使用 → ❌ Server.Interfaces

### 2. 避免过度抽象
**要求**: 不要为了"可能的未来扩展"而过早抽象

**反模式**:
```csharp
// ❌ 过度抽象：为了"可能支持Mobile"而提前抽象
public interface IGenericHttpClient<TRequest, TResponse>
{
    Task<TResponse> SendAsync(TRequest request);
}
// 问题：当前只有Desktop使用Refit，过早抽象HTTP客户端无意义
```

**正确做法**:
```csharp
// ✅ 等待真正需要时再抽象
// 当前：Desktop.Contracts保留Refit接口（Desktop专用）
// 未来：如果Mobile也需要HTTP客户端，再评估是否提取共享接口
```

### 3. 明确所有权
**要求**: 每个接口应有明确的所有者和使用场景

**所有权声明**（未来接口添加时）:
```csharp
/// <summary>
/// 通知服务接口（跨端共享）
/// 所有者: Shared层
/// 使用场景: Server发送通知 + Desktop/Mobile接收通知
/// 实现位置: Server.Infrastructure + Desktop.Infrastructure + Mobile.Infrastructure
/// </summary>
public interface INotificationService
{
    // ...
}
```

### 4. 最小依赖
**要求**: 此项目应保持最小依赖，避免引入平台特定的包

**依赖约束**:
- ✅ 允许: .NET 8基础框架（System命名空间）
- ✅ 允许: 通用NuGet包（如Newtonsoft.Json，如果确实需要）
- ❌ 禁止: Refit（Desktop专用）
- ❌ 禁止: Prism（Desktop专用）
- ❌ 禁止: ASP.NET Core（Server专用）
- ❌ 禁止: Entity Framework Core（Server专用）

## 📋 接口归属决策指南

### 决策流程图

```
1. 询问：这个接口多个端都需要吗？
   ├── 是 → 继续问2
   └── 否 → 放入对应层级（Desktop.Contracts/Server.Interfaces/Mobile.Contracts）

2. 询问：这个接口依赖特定技术框架吗？（Refit/Prism/ASP.NET等）
   ├── 是 → 放入对应层级（技术框架所在层）
   └── 否 → 继续问3

3. 询问：这个接口是业务抽象还是技术抽象？
   ├── 业务抽象 → 考虑放入 Shared.Interfaces
   └── 技术抽象 → 放入对应层级的Infrastructure

4. 最终检查：删除这个接口，会影响多个端吗？
   ├── 是（≥2个端受影响）→ ✅ 放入 Shared.Interfaces
   └── 否（≤1个端受影响）→ ❌ 放入对应层级
```

### 决策示例

**案例1: INotificationService**
- Q1: 多个端需要吗？→ 是（Server发送 + Desktop/Mobile接收）
- Q2: 依赖特定框架吗？→ 否（纯接口定义）
- Q3: 业务抽象还是技术抽象？→ 业务抽象（通知推送业务）
- Q4: 删除会影响多个端吗？→ 是（Server/Desktop/Mobile都受影响）
- **结论**: ✅ 适合放入 Shared.Interfaces

**案例2: IUserApi（Refit）**
- Q1: 多个端需要吗？→ 否（仅Desktop使用）
- **结论**: ❌ 放入 Desktop.Contracts

**案例3: IUserService**
- Q1: 多个端需要吗？→ 否（仅Server使用）
- **结论**: ❌ 放入 Server.Interfaces

**案例4: IConfigurationProvider**
- Q1: 多个端需要吗？→ 是（Server/Desktop/Mobile都需要配置）
- Q2: 依赖特定框架吗？→ 否（纯接口定义）
- Q3: 业务抽象还是技术抽象？→ 技术抽象（配置管理基础设施）
- Q4: 删除会影响多个端吗？→ 是（所有端都受影响）
- **结论**: ✅ 适合放入 Shared.Interfaces（或考虑放入各端Infrastructure，取决于实现差异）

## 🚀 快速开始

此项目是一个类库，作为共享接口定义被其他项目引用。当前为空项目，无法独立运行。

```bash
# 构建此项目
dotnet build src/Shared/LYBT.Shared.Interfaces/LYBT.Shared.Interfaces.csproj
```

**未来集成说明**（当有接口定义时）:

### 1. 添加项目引用
```xml
<!-- 在需要引用共享接口的项目中 -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\LYBT.Shared.Interfaces\LYBT.Shared.Interfaces.csproj" />
</ItemGroup>
```

### 2. 实现共享接口
```csharp
// Server端实现
public class NotificationService : INotificationService
{
    public async Task SendNotificationAsync(string userId, string message)
    {
        // Server端实现：通过SignalR发送推送
    }
}

// Desktop端实现
public class NotificationService : INotificationService
{
    public async Task SendNotificationAsync(string userId, string message)
    {
        // Desktop端实现：显示桌面通知
    }
}
```

## 📚 相关文档

**架构设计**:
- [Server端架构指南](../../docs/explanation/architecture/server/README.md)
- [Desktop端架构指南](../../docs/explanation/architecture/client/README.md)
- [Shared层架构指南](../../docs/explanation/architecture/shared/README.md)

**设计决策**:
- [ADR-002 Desktop移除Service层](../../docs/explanation/architecture/decisions/ADR-002-desktop-remove-service-layer.md)

**开发指南**:
- [Desktop模块统一设计标准](../../docs/explanation/architecture/client/unified-design-standard.md)
- [Server模块设计标准](../../docs/explanation/architecture/server-module-design-standard.md)

**迁移历史**:
- [Issue #1204 Refit接口迁移](https://github.com/shouqitao/LYBTZYZS/issues/1204)

---

> 📌 **当前状态**: 空项目，保留用于未来真正跨平台共享的接口定义
> 🎯 **架构原则**: "Shared" 必须名副其实 - 真正共享才放入
> ⚠️ **避免过早抽象**: 等待真正需要时再添加接口定义

**最后更新**: 2025-10-29
**维护负责**: 架构组

# LYBT.Shared.Interfaces

> **真正共享接口契约库** - 保留项目，暂无实现
> **模块状态**: ⚠️ **空项目** | 📦 **保留用于未来扩展** | **2025-10-12更新**

## 🎯 项目概述

LYBT.Shared.Interfaces 项目当前为空，保留作为未来定义 Server/Desktop/Mobile 真正共享接口契约的容器。

**当前状态**: 空项目（无接口定义）
**架构原则**: 真正跨平台共享的接口才放入此项目
**技术栈**: .NET 8

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

**迁移原因**:
- 这些接口仅被 Desktop 客户端使用（13个项目）
- Server 端不需要这些接口
- "Shared" 命名造成误导，不符合架构分层原则
- 符合 ADR-002 桌面端直接调用 WebAPI 的架构决策

**相关文档**:
- Issue: [#1204 Refit 接口迁移到 Desktop.Contracts](https://github.com/shouqitao/LYBTZYZS/issues/1204)
- ADR: `docs/architecture/decisions/ADR-002-desktop-remove-service-layer.md`
- 新位置: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/`

## 🔮 未来用途

此项目保留用于以下场景：

### 适合放入此项目的接口
- ✅ **Server ↔ Desktop 双向共享**的接口定义
- ✅ **Desktop ↔ Mobile 跨客户端共享**的接口定义
- ✅ **Server ↔ Mobile 共享**的接口定义
- ✅ **三端（Server/Desktop/Mobile）共享**的通用接口

### 不适合放入此项目的接口
- ❌ Desktop 专用接口 → 应放入 `Desktop.Contracts`
- ❌ Server 专用接口 → 应放入 `Server.Interfaces`
- ❌ Mobile 专用接口 → 应放入 `Mobile.Contracts`（未来）

## 📦 项目结构

```
LYBT.Shared.Interfaces/
├── LYBT.Shared.Interfaces.csproj  # 项目文件
└── README.md                        # 本文档
```

## 🎯 设计原则

1. **真正共享才放入**: 只有多个层级或多个平台真正需要的接口才放入此项目
2. **避免过度抽象**: 不要为了"可能的未来扩展"而过早抽象
3. **明确所有权**: 每个接口应有明确的所有者和使用场景
4. **最小依赖**: 此项目应保持最小依赖，避免引入平台特定的包

## 📚 参考资料

- [Desktop 模块统一设计标准](../../docs/architecture/client/unified-design-standard.md)
- [Server 模块设计标准](../../docs/architecture/server-module-design-standard.md)
- [ADR-002 Desktop 移除 Service 层](../../docs/architecture/decisions/ADR-002-desktop-remove-service-layer.md)
- [Issue #1204 Refit 接口迁移](https://github.com/shouqitao/LYBTZYZS/issues/1204)

---

> 📌 **当前状态**: 空项目，保留用于未来真正跨平台共享的接口定义
> 🎯 **架构原则**: "Shared" 必须名副其实 - 真正共享才放入

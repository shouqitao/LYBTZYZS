# Phase 1 详细实施计划 - Desktop基础设施重组

> **文档版本**: 1.0
> **创建日期**: 2025-01-09
> **关联Issue**: #1114
> **ADR**: ADR-005-desktop-modular-architecture.md

---

## 执行摘要

本文档记录Phase 1（基础设施重组）的详细实施计划、已完成工作、遇到的问题及解决策略。Phase 1是Desktop模块化架构重构的第一阶段，目标是分离技术基础设施与UI基础设施。

---

## 当前进度（2025-01-09）

### ✅ 已完成

#### 1. 架构决策与文档（Phase 0）
- ✅ 完成25步UltraThink深度分析
- ✅ 更新Issue #1114验收标准（含4-Phase实施路径、ROI分析）
- ✅ 更新unified-design-standard.md至v2.0（移除Service层，新增模块化架构）
- ✅ 创建ADR-005架构决策记录（完整决策过程、替代方案、风险应对）

#### 2. 前置检查（Phase 1.1）
- ✅ git pull（已是最新）
- ✅ dotnet build LYBT.All.sln -c Release（成功，4个警告0错误）
- ✅ 基线编译状态正常

#### 3. Desktop.Foundation项目创建（Phase 1.2）
- ✅ 创建项目目录：`src/Client/Desktop/Core/LYBT.Desktop.Foundation/`
- ✅ 创建.csproj文件（配置HTTP、配置、缓存、安全等依赖包）
- ✅ 添加到Desktop.sln和LYBT.All.sln
- ✅ 创建13个空目录：
  - Caching/, Configuration/, Diagnostics/, ErrorHandling/
  - Http/, Performance/, Security/, Session/, Settings/
  - HealthCheck/, Modules/, Handlers/, Extensions/
- ✅ 创建README.md（项目概述、目录结构、使用示例）
- ✅ 编译验证通过（0错误0警告）

### 🔄 进行中

#### Phase 1.3：迁移技术基础设施（遇到阻塞）

**问题发现**：
1. **跨层依赖**：技术基础设施代码引用了：
   - `LYBT.Desktop.Services.Business`（业务层，待删除）
   - `LYBT.Desktop.Services.Repositories`（仓储层，待下沉到模块）
   - `LYBT.Desktop.Services.Notifications`（UI层，待迁移到Presentation）
   - `LYBT.Desktop.Services.Interfaces`（接口层）

2. **缺失依赖**：
   - `Prism.Core`（IModuleManager, IModuleCatalog）
   - `IApiHealthCheckService`, `ApiHealthStatus`
   - `INotificationService`, `ITokenStorageService`

3. **编译错误**：直接复制导致22个编译错误

**已采取行动**：
- ✅ 回滚Phase 1.3改动
- ✅ 保持Foundation项目为干净状态（空目录结构）
- ✅ Foundation项目能正常编译

### ⏸️ 暂停

由于Phase 1.3遇到跨层依赖问题，暂停自动化迁移，采用人工分析策略。

---

## Phase 1 完整实施路径（修订版）

### Phase 1.1：前置检查 ✅ 已完成
- [x] git pull
- [x] dotnet build LYBT.All.sln验证
- [x] 记录基线编译状态

### Phase 1.2：创建Desktop.Foundation项目 ✅ 已完成
- [x] 创建项目文件和目录结构
- [x] 添加到解决方案
- [x] 编译验证（空项目）
- [x] 创建README.md

### Phase 1.3：迁移技术基础设施（需分步执行）

#### Step 1：依赖分析（必需）
**目标**：识别哪些代码可以安全迁移，哪些需要解耦

**任务清单**：
- [ ] 列出Desktop.Services/下13个技术基础设施目录的所有文件
- [ ] 分析每个文件的依赖关系（using语句）
- [ ] 分类文件：
  - 无依赖（可直接迁移）
  - 依赖Shared/Foundation（可直接迁移）
  - 依赖Business/Repositories（需解耦或等Phase 2）
  - 依赖Notifications/Navigation（需等Phase 1.5）

**工具**：
```bash
# 分析using依赖
find src/Client/Desktop/Core/LYBT.Desktop.Services/{Caching,Configuration,Diagnostics,ErrorHandling,Http,Performance,Security,Session,Settings,HealthCheck,Modules,Handlers,Extensions} -name "*.cs" -exec grep -H "^using LYBT" {} \; > dependency-analysis.txt
```

#### Step 2：迁移无依赖代码
**目标**：迁移不依赖Business/Repositories/Notifications的代码

**预计文件**（需验证）：
- Extensions/PollyExtensions.cs
- Performance/StartupOptimizationService.cs（如无依赖）
- Configuration/ConfigurationService.cs
- Caching/CacheService.cs

**操作**：
```bash
# 逐文件复制并验证编译
cp src/Client/Desktop/Core/LYBT.Desktop.Services/Extensions/PollyExtensions.cs \
   src/Client/Desktop/Core/LYBT.Desktop.Foundation/Extensions/
# 更新命名空间
sed -i 's/namespace LYBT.Desktop.Services/namespace LYBT.Desktop.Foundation/g' \
   src/Client/Desktop/Core/LYBT.Desktop.Foundation/Extensions/PollyExtensions.cs
# 验证编译
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj
```

#### Step 3：添加Prism.Core依赖
**目标**：解决ModuleLoadingService的Prism依赖

**操作**：
```xml
<!-- LYBT.Desktop.Foundation.csproj -->
<ItemGroup Label="Framework">
  <PackageReference Include="Prism.Core" />
</ItemGroup>
```

#### Step 4：处理接口依赖
**目标**：决定接口的归属位置

**选项A**：将Interfaces/中的基础接口迁移到Desktop.Infrastructure
**选项B**：在Foundation/中定义接口

**建议**：选项A，Desktop.Infrastructure作为通用接口层

#### Step 5：解耦Business/Repositories依赖
**目标**：移除对待删除层的依赖

**策略**：
- ServiceCollectionExtensions.cs → 删除Business/Repositories注册代码
- AuthorizationMessageHandler.cs → 将ITokenStorageService移到Foundation或Infrastructure

#### Step 6：处理Notifications依赖
**目标**：临时保留或等Phase 1.5

**策略**：
- UnifiedErrorHandlingService.cs → 临时引用Desktop.Services.Notifications
- Phase 1.5完成后更新为Desktop.Presentation.Notifications

### Phase 1.4：创建Desktop.Presentation项目

#### Step 1：创建项目
```bash
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Presentation
```

#### Step 2：创建.csproj
**依赖包**：
- Prism.Core, Prism.Wpf
- Microsoft.Extensions.Logging
- 引用Desktop.Foundation

#### Step 3：创建目录
- Navigation/
- Notifications/
- Theming/
- UserExperience/
- Print/

#### Step 4：创建README.md

### Phase 1.5：迁移UI基础设施

**迁移目录**（从Desktop.Services）：
- Navigation/ → Desktop.Presentation/Navigation/
- Notifications/ → Desktop.Presentation/Notifications/
- Theming/ → Desktop.Presentation/Theming/
- UserExperience/ → Desktop.Presentation/UserExperience/
- Print/ → Desktop.Presentation/Print/

**依赖更新**：
- 更新命名空间
- 更新Foundation中对Notifications的引用

### Phase 1.6：编译验证与架构测试

#### Step 1：全量编译
```bash
dotnet build LYBT.Desktop.sln -c Release
dotnet build LYBT.All.sln -c Release
```

#### Step 2：架构测试
```bash
dotnet test tests/ArchitectureTests/LYBT.Desktop.ArchTests.csproj
```

#### Step 3：更新依赖注入
**文件**：`src/Client/Desktop/Shell/LYBT.Desktop.Shell/App.xaml.cs`

**改动**：
```csharp
// 旧：
services.AddDesktopServices(configuration);

// 新：
services.AddDesktopFoundation(configuration);
services.AddDesktopPresentation(configuration);
```

#### Step 4：验收
- [ ] Desktop.Foundation编译通过（0错误0警告）
- [ ] Desktop.Presentation编译通过（0错误0警告）
- [ ] LYBT.All.sln编译通过
- [ ] 架构测试通过
- [ ] 原有功能无回归（手动冒烟测试）

---

## 技术债务与已知问题

### 1. 跨层依赖
**问题**：技术基础设施代码依赖业务层和UI层

**影响**：无法一次性迁移全部代码

**解决方案**：
- 短期：分步迁移，临时保留Services引用
- 长期：Phase 2完成后彻底移除Services项目

### 2. 接口位置不明确
**问题**：部分接口定义在Services/Interfaces/，归属不清

**影响**：迁移时需决定接口放在Foundation还是Infrastructure

**解决方案**：
- 技术基础接口 → Desktop.Infrastructure
- 业务接口 → 各模块的Repositories/目录

### 3. Prism依赖
**问题**：ModuleLoadingService需要Prism.Core

**影响**：Foundation需添加Prism依赖

**解决方案**：已在.csproj中添加Prism.Core引用

---

## 风险评估

| 风险 | 等级 | 影响 | 应对措施 |
|------|------|------|---------|
| 跨层依赖导致无法编译 | 高 | Phase 1阻塞 | 分步迁移，先迁移无依赖代码 |
| 接口位置决策错误 | 中 | 后期需重构 | 遵循DDD原则，技术接口在Infrastructure |
| 迁移遗漏文件 | 中 | 功能缺失 | 使用文件清单比对，自动化验证 |
| 命名空间更新遗漏 | 低 | 编译错误 | 全局搜索替换 + 编译验证 |
| 原有功能回归 | 中 | 用户影响 | Phase 1.6冒烟测试 |

---

## 下一步行动

### 立即行动（下次会话）
1. **执行Phase 1.3 Step 1**：依赖分析
   - 运行依赖分析脚本
   - 生成文件分类清单
   - 识别可安全迁移的文件

2. **执行Phase 1.3 Step 2**：迁移无依赖代码
   - 逐文件复制并验证
   - 更新命名空间
   - 编译验证

3. **评估进度**：
   - 如无依赖文件≥50%，继续Phase 1.3
   - 如无依赖文件<50%，考虑调整策略

### 中期目标（本周）
- 完成Phase 1.3（迁移技术基础设施）
- 完成Phase 1.4（创建Presentation项目）
- 完成Phase 1.5（迁移UI基础设施）

### 长期目标（2周内）
- 完成Phase 1.6（编译验证与架构测试）
- 准备进入Phase 2（模块化改造）

---

## 文档更新计划

### 需提交的文档（当前会话完成）
- ✅ Issue #1114验收标准更新
- ✅ docs/architecture/client/unified-design-standard.md v2.0
- ✅ docs/architecture/adr/ADR-005-desktop-modular-architecture.md
- 🆕 docs/reports/phase1-implementation-plan.md（本文档）

### 需更新的文档（Phase 1完成后）
- docs/index.md（添加ADR-005链接）
- docs/reports/INDEX.md（登记本报告）
- src/Client/Desktop/Core/LYBT.Desktop.Foundation/README.md（补充迁移进度）
- src/Client/Desktop/Core/LYBT.Desktop.Presentation/README.md（创建）

---

## 参考资料

- [Desktop模块化架构决策深度分析](./desktop-modular-architecture-decision.md)
- [ADR-005: Desktop端模块化架构重构](../architecture/adr/ADR-005-desktop-modular-architecture.md)
- [Client端业务模块统一设计标准 v2.0](../architecture/client/unified-design-standard.md)
- [Issue #1114: Desktop架构模块化重构](https://github.com/shouqitao/LYBTZYZS/issues/1114)

---

## 附录：命令速查

### 依赖分析
```bash
# 分析技术基础设施目录的依赖
find src/Client/Desktop/Core/LYBT.Desktop.Services/{Caching,Configuration,Diagnostics,ErrorHandling,Http,Performance,Security,Session,Settings,HealthCheck,Modules,Handlers,Extensions} \
  -name "*.cs" -not -path "*/obj/*" \
  -exec grep -H "^using LYBT.Desktop.Services" {} \; \
  > dependency-analysis.txt

# 统计文件数量
find src/Client/Desktop/Core/LYBT.Desktop.Services/{Caching,Configuration,Diagnostics,ErrorHandling,Http,Performance,Security,Session,Settings,HealthCheck,Modules,Handlers,Extensions} \
  -name "*.cs" -not -path "*/obj/*" | wc -l
```

### 批量命名空间替换
```bash
# Foundation目录
find src/Client/Desktop/Core/LYBT.Desktop.Foundation \
  -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i 's/namespace LYBT\.Desktop\.Services/namespace LYBT.Desktop.Foundation/g' {} \;

find src/Client/Desktop/Core/LYBT.Desktop.Foundation \
  -name "*.cs" -not -path "*/obj/*" \
  -exec sed -i 's/using LYBT\.Desktop\.Services/using LYBT.Desktop.Foundation/g' {} \;
```

### 编译验证
```bash
# Foundation项目
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj -c Release

# 全量编译
dotnet build LYBT.Desktop.sln -c Release
dotnet build LYBT.All.sln -c Release

# 架构测试
dotnet test tests/ArchitectureTests/LYBT.Desktop.ArchTests.csproj
```

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

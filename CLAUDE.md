# CLAUDE.md - 凌隐宝堂中医诊所系统开发指南

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 🔄 项目感知与上下文

### 项目概述

**凌隐宝堂中医诊所诊疗系统 (LYBTZYZS)** - 基于 .NET 8 的企业级纯中医诊所管理系统，采用 Web API 后端 + WPF 桌面前端架构。

**项目状态**: ✅ UltraThink v2.0 全项目重构完成 | ✅ 0错误 0警告 | ✅ 生产就绪

### 🎯 核心业务模块 (8个)

1. **Auth** - 身份认证与授权 (JWT + RBAC权限)
2. **Users** - 用户管理 (医生/管理员角色)
3. **Patients** - 患者档案 (完整病历管理)
4. **MedicalCase** - 医疗案例 (看诊流程管理容器，1:1关联Consultation)
5. **Consultation** - 看诊诊断 (中医四诊：望闻问切，辨证论治)
6. **Prescriptions** - 处方管理 (智能配伍，验方组合)
7. **Herbs** - 中药材管理 (处方用药选择，不含库存)
8. **Formula** - 验方管理 (经典验方模板库)

### 🏗️ 技术架构

- **后端**: .NET 8 + ASP.NET Core Web API + EF Core 8.0.17 + JWT认证
- **前端**: WPF + Prism.DryIoc 9.0.537 + Refit (类型安全REST客户端)
- **数据库**: SQL Server + 统一AppDbContext (所有模块共享)
- **缓存**: IMemoryCache智能缓存系统 (适合小型部署)
- **监控**: 8个健康检查端点 (生产就绪)

### 开始新对话时必须

- **始终先阅读本文档** 了解项目架构、目标、风格和约束
- **检查 `docs/TODO-Latest-*.md`** 了解当前任务状态
- **查看 `CLAUDE.local.md`** 了解用户特定的开发环境配置
- **使用一致的命名约定、文件结构和架构模式**

## 🚀 Serena MCP Server 初始化

### 关于Serena

Serena是一个MCP (Model Context Protocol) 服务器，提供增强的开发工具支持。为了正确使用Serena的工具，Claude需要读取其指令文本。

### 初始化方法

#### 自动初始化（v1.0.52+）
从v1.0.52版本开始，Claude Code会自动读取MCP服务器的指令，无需手动操作。

#### 手动初始化（旧版本或自动失败时）
如果使用旧版本或自动读取失败，可以通过以下方式手动初始化：

1. **方法一：显式请求**
   ```
   请求："read Serena's initial instructions"
   或中文："读取Serena的初始化指令"
   ```

2. **方法二：运行命令**
   ```
   /mcp__serena__initial_instructions
   ```

### 配置要求

要启用initial_instructions工具，需要在配置文件中添加：

```json
{
  "included_optional_tools": [
    "initial_instructions"
  ]
}
```

### 重要时机

**必须重新读取Serena指令的情况**：
- 🔄 开始新对话时
- 🗜️ 执行压缩操作后（compacting operation）
- ❌ Serena工具使用异常时

### 使用验证

初始化成功后，可以通过以下方式验证：
- 检查Serena工具是否可用
- 尝试调用Serena的基础功能
- 查看工具列表中是否包含Serena相关工具

### 注意事项

> ⚠️ **重要**: 每次新会话开始时，建议主动执行初始化命令，确保Claude正确配置了Serena工具。这是保证Serena功能正常工作的关键步骤。

## 🧱 代码结构与模块化

### 文件大小限制

- **永远不要创建超过500行的文件**，接近限制时进行重构
- **组织代码为清晰分离的模块**，按功能或职责分组

### 模块化原则

- 每个业务模块独立但共享数据上下文 (AppDbContext)
- 严格分离关注点：Model、Service、Repository、Controller
- 使用依赖注入：构造函数注入模式
- 异步优先：所有数据库操作使用 async/await

## 📁 文件组织规范（重要）

### 强制性规则

创建或修改文件时，**必须严格遵守**以下规则：

1. **禁止在根目录创建文档文件** - 所有文档必须放在 `docs/` 相应子目录
2. **必须使用英文文件名** - 避免中文造成的编码和跨平台问题
3. **必须使用kebab-case命名** - 如 `user-guide.md`, `fix-report-20250131.md`
4. **报告文件必须包含日期** - 格式：`name-YYYYMMDD.md`
5. **临时文件放入temp/目录** - 并确保在 `.gitignore` 中忽略

### 目录结构规范

```
docs/
├── architecture/    # 架构设计文档
├── api/            # API文档和规范
├── development/    # 开发指南（包含FILE_ORGANIZATION.md）
├── deployment/     # 部署和运维文档
├── testing/        # 测试相关文档
├── reports/        # 项目报告（带日期）
├── guides/         # 用户指南
├── ultrathink/     # UltraThink方法论文档
├── design/         # UI/UX设计文档
├── fixes/          # 问题修复记录
├── progress/       # 进度跟踪
└── legacy/         # 归档文档

scripts/            # 开发和构建脚本
tools/              # 用户工具和启动器
temp/               # 临时文件（gitignored）
```

### 文件命名示例

❌ **错误示例**:
- `WPF登录问题修复报告.md`
- `系统使用说明.md`
- `创建桌面快捷方式.bat`

✅ **正确示例**:
- `docs/reports/wpf-login-fix-report-20250131.md`
- `docs/guides/system-user-guide.md`
- `tools/create-desktop-shortcut.bat`

### 快速参考

| 文件类型 | 位置 | 命名格式 | 示例 |
|---------|------|---------|------|
| 用户指南 | `docs/guides/` | `feature-guide.md` | `system-user-guide.md` |
| API文档 | `docs/api/` | `api-name.md` | `auth-api.md` |
| 项目报告 | `docs/reports/` | `name-YYYYMMDD.md` | `fix-report-20250131.md` |
| UltraThink | `docs/ultrathink/` | `analysis-name.md` | `ui-design-system-report.md` |
| 开发脚本 | `scripts/` | `action.bat` | `build-all.bat` |
| 用户工具 | `tools/` | `tool-name.bat` | `start-system.bat` |

> 📌 **重要**: 详细规范请查看 [文件组织标准](docs/development/FILE_ORGANIZATION.md)

## 🎨 资源管理规范（重要）

### 强制性资源规则

处理资源文件（图标、图片、字体等）时，**必须严格遵守**：

1. **资源文件统一放置** - 所有资源必须放在 `Assets/` 目录及其子目录
2. **遵循命名规范** - 图标: `icon-{purpose}-{size}.png`，图片: `img-{module}-{description}.jpg`
3. **使用Pack URI引用** - `pack://application:,,,/Assets/Icons/icon-name.png`
4. **Build Action设为Resource** - 所有图片资源的生成操作必须设为Resource
5. **使用ResourcePaths常量** - 通过ResourcePaths.cs类引用资源路径

### 资源目录结构

```
src/Frontend/Desktop/
├── Assets/           # 静态资源（图片、图标等）
│   ├── Images/      # 图片文件
│   ├── Icons/       # 图标文件
│   ├── Fonts/       # 字体文件
│   └── Audio/       # 音频文件
├── Themes/          # XAML样式资源
│   ├── Design/      # 设计系统基础
│   └── Controls/    # 控件模板
└── Resources/       # 资源字典
    └── Dictionaries/# 合并的资源字典
```

### 添加新资源步骤

1. 将文件放入正确的Assets子目录
2. 设置Build Action = Resource
3. 在相应的ResourceDictionary中定义资源
4. 更新ResourcePaths.cs添加路径常量
5. 测试资源加载是否正常

> 📌 **重要**: 详细规范请查看 [资源管理指南](src/Frontend/Desktop/Assets/RESOURCE_MANAGEMENT.md)

## 常用开发命令

### 快速启动

```bash
# 交互式开发管理器（推荐）
scripts\dev-manager.bat

# 快速启动开发服务器
scripts\start-dev.bat

# 手动启动（开发时通常使用 Visual Studio）
dotnet run --project src/Backend/Services/LYBT.WebAPI
```

### 构建命令

```bash
# 构建解决方案
dotnet build LYBT.Backend.sln    # 后端
dotnet build LYBT.Desktop.sln    # 前端
dotnet build LYBT.All.sln        # 完整方案

# 发布生产版本
scripts\publish-production.bat
```

### 数据库管理

```bash
# 交互式数据库管理器
scripts\database-manager.bat

# 添加迁移 - 必须使用 Infrastructure 项目
dotnet ef migrations add [迁移名称] --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update --project src/Backend/Core/LYBT.Infrastructure --startup-project src/Backend/Services/LYBT.WebAPI
```

### 测试

```bash
# 运行所有测试
dotnet test

# API 自动化测试
cd tests/api
python api_test_automation.py
```

## 🧪 测试与可靠性

### 测试要求

- **为新功能创建单元测试**（使用 xUnit）
- **更新逻辑后检查现有测试是否需要更新**
- **测试文件位于 `tests/` 文件夹**，镜像主应用结构
- 每个功能至少包含：
  - 1个正常使用测试
  - 1个边缘情况测试  
  - 1个失败情况测试

### 测试完成状态（2025-08-08）

**Repository层测试**（完成）：
- 97个测试用例全部通过
- UserRepository: 31个测试
- PatientRepository: 38个测试 
- HerbRepository: 28个测试

**Service层测试**（进行中）：
- UserService: 68个测试用例（完成）
- PatientService: 88个测试用例（完成）
- 总计156个Service层测试已完成
- 下一步：HerbService、AuthService单元测试

**代码覆盖率**：从2.30%提升至2.76%，目标60%

### API测试

- 使用 Python 脚本进行 API 自动化测试
- 测试脚本位于 `tests/api/` 目录
- 运行命令：`python api_test_automation.py`

## 高层架构

### 整体技术栈

- **后端**: .NET 8, ASP.NET Core Web API, Entity Framework Core 8.0.17, SQL Server
- **前端**: WPF (.NET 8), Prism.DryIoc 9.0.537, Refit
- **认证**: JWT Bearer Token (类型安全UserRole枚举)
- **数据访问**: EF Core LINQ查询 + ExecuteUpdate批量操作 (零SQL注入)
- **缓存**: IMemoryCache智能缓存系统 (统计和性能监控)
- **监控**: 全面健康检查体系 (8个端点覆盖数据库/缓存/系统资源)
- **连接池**: 优化配置(Max=20, Min=2)适合小型部署
- **API文档**: Swagger/Swashbuckle 9.0.1

### 项目结构

```
src/
├── Backend/
│   ├── Core/
│   │   ├── LYBT.Infrastructure/     # 统一 AppDbContext，所有迁移在此
│   │   └── LYBT.Models/            # 领域模型
│   ├── Modules/                    # 15个业务模块
│   └── Services/LYBT.WebAPI/       # Web API 入口
├── Frontend/Desktop/               # WPF 客户端
└── Shared/                        # 前后端共享模型
```

### 关键架构特点

1. **🎆 UltraThink四层架构** (2025-08-17重构完成): 严格的Layer 1(BaseModel)→Layer 2(EntityModel)→Layer 3(Dto)→Layer 4(Info)分层体系
   - Desktop层(Layer 4)完全使用Info模型，不直接引用Contracts(Layer 3)
   - 通过AutoMapper实现DTO↔Info自动转换，消除手工转换代码
   - 16个Info模型+46个AutoMapper映射规则+22个ViewModels重构完成
2. **🔒 安全数据访问** (Phase 2完成): 零SQL注入风险的Repository层
   - 所有Repository使用LINQ和参数化查询
   - EF Core 7.0 ExecuteUpdate批量操作优化
   - 类型安全的JWT认证系统(UserRole枚举)
3. **⚡ 性能优化** (Phase 2完成): 适合小型部署的高性能配置
   - 数据库连接池(Max=20, Min=2)适合<20用户规模
   - 批量操作使用EF Core ExecuteUpdate避免内存加载
   - 智能内存缓存系统提升响应速度
4. **📊 全面监控** (Phase 2完成): 生产就绪的健康检查体系
   - 8个健康检查端点涵盖数据库、缓存、系统资源
   - Kubernetes就绪探针支持容器化部署
   - 实时性能指标和系统状态监控
5. **统一数据访问**: 所有模块共享 `AppDbContext`（在 Infrastructure 中）
6. **模块化设计**: 每个业务模块独立但共享数据上下文
7. **整洁架构**: 严格分离关注点
8. **API 响应包装**: 所有响应包装在 `ApiResponse<T>` 中
9. **依赖注入**: 构造函数注入模式，所有ViewModels注入IMapper
10. **异步优先**: 数据库操作使用 async/await

### 业务模块详细说明（实际存在的8个核心模块）

#### 核心认证与用户模块
1. **Auth** - 身份认证和授权
   - JWT Bearer Token认证 (8小时有效期，Remember Me: 30天)
   - RBAC角色权限控制 (Admin/Doctor角色)
   - 登录会话管理和安全审计

2. **Users** - 用户管理  
   - 医生和管理员账户管理
   - 角色权限分配和状态控制
   - 密码安全策略 (Hash+盐值加密)

3. **Patients** - 患者档案
   - 完整患者基础信息管理
   - 就诊历史跟踪和联系方式
   - 整合了基础患者接待功能

#### 核心诊疗流程模块 (v1.0)
4. **MedicalCase** - 医疗案例
   - **诊疗流程容器**: 看诊会话管理 (1:1关联Consultation)
   - **状态跟踪**: Registered → InProgress → Completed
   - **聚合功能**: 统一管理整个诊疗过程，包含原Records功能

5. **Consultation** - 看诊诊断
   - **中医四诊**: 望闻问切完整记录系统
   - **辨证论治**: 症状分析和中医治疗方案
   - **诊断记录**: 病情分析和医嘱建议

6. **Prescriptions** - 处方管理
   - **智能配伍**: 验方组合和药材智能合并
   - **安全检查**: 配伍禁忌检查 (十八反、十九畏检测)
   - **处方输出**: 标准格式打印和历史管理

#### 药材与验方模块
7. **Herbs** - 中药材管理
   - **药材信息**: 名称、单价、用法信息维护
   - **仅处方用药**: 不涉及库存管理，专注处方选择
   - **标准化管理**: 统一药材标准和规格

8. **Formula** - 验方管理
   - **经典验方库**: 传统验方模板收录
   - **个人验方**: 医生临床经验积累
   - **智能组合**: 可被Prescriptions引用和组合应用

#### 核心业务关系 (2025-08-23最新)
- **1:1关系**: MedicalCase ↔ Consultation (一个医案对应一次诊断，无复诊概念)
- **诊疗流程**: 创建医案 → 进行诊断 → [可选]开具处方 → 完成医案  
- **复诊处理**: 每次患者就诊都创建全新的MedicalCase，通过PatientId关联历史记录
- **v1.0范围**: 诊断+处方核心功能，挂号和收费模块计划v2.0开发
- **模块协作**: Formula → Prescriptions → Consultation → MedicalCase → Patients

## 📊 项目完成状态总结 (2025-08-23最新)

### ✅ UltraThink v2.0 重构成果

**架构完成度**: 🟢 **100%完成** - 全项目架构重构完成
- ✅ **编译状态**: 0错误 0警告 (前后端解决方案)
- ✅ **架构统一**: UltraThink三层模块化标准完全实施
- ✅ **API标准化**: 所有端点遵循RESTful小写命名规范
- ✅ **安全强化**: 零SQL注入风险，JWT认证体系完善
- ✅ **性能优化**: 智能缓存系统，适合小型诊所部署

**模块完整性**: 🟢 **8核心模块全部就绪**
- ✅ **前端**: WPF桌面客户端，Prism.DryIoc模块化架构
- ✅ **后端**: ASP.NET Core Web API，统一AppDbContext
- ✅ **共享**: Shared.Models统一DTO系统

**业务功能**: 🟢 **诊疗核心流程完整**
- ✅ **完整诊疗链**: 患者档案 → 医案创建 → 四诊记录 → 处方开具
- ✅ **智能配伍**: 验方组合，配伍禁忌检查
- ✅ **打印输出**: 标准处方格式，法规合规

**质量保证**: 🟡 **测试覆盖率持续提升**
- ✅ **Repository层**: 97个测试用例全部通过
- ✅ **Service层**: 156个测试用例完成 (UserService + PatientService)
- 🔄 **目标**: 代码覆盖率从2.76%提升至60% (HerbService、AuthService待完成)

### 🚀 生产就绪状态

**部署就绪**: 🟢 **Ready for Production**
- ✅ **健康检查**: 8个端点覆盖数据库/缓存/系统资源
- ✅ **监控体系**: 统一异常处理，完整日志记录
- ✅ **配置管理**: 环境变量，连接字符串标准化
- ✅ **安全审计**: JWT认证，RBAC权限，操作日志

**性能指标**: 🟢 **小型诊所优化完成**
- ✅ **并发支持**: <10用户并发，<20人员规模
- ✅ **响应时间**: API响应<2秒，缓存命中优化
- ✅ **数据库**: 连接池(Max=20, Min=2)，批量操作优化
- ✅ **内存缓存**: 智能过期策略，统计监控

## 🏥 实用化架构要求 (2025-08-17)

### 项目规模定位

**适用场景**: 20人以下用户的中小型诊所
- 👨‍⚕️ **医生**: 2-5人
- 👩‍💼 **接待员**: 1-2人  
- 👨‍💻 **管理员**: 1人
- 📊 **并发用户**: <10人
- 📈 **日访问量**: <1000次

### 核心业务需求

1. **✅ 异地组网**: 多个诊所分点统一管理
2. **✅ 数据同步**: 患者档案、药材库存共享
3. **✅ 简单维护**: 技术人员有限，要求系统稳定
4. **✅ 成本控制**: 避免过度复杂的基础设施

### 实用化设计原则

#### ✅ 保持简单有效
- **单一AppDbContext**: 所有模块共享，管理简单
- **内存缓存优先**: MemoryCache足够，避免Redis等复杂方案
- **传统部署**: IIS + Windows Server，成熟稳定
- **统一错误处理**: 现有BaseController体系已经很好

#### ✅ 避免过度设计
- ❌ **微服务架构** - 20人以下系统完全不需要
- ❌ **事件溯源** - 增加复杂性，收益有限  
- ❌ **CQRS** - 读写量都不大，过度设计
- ❌ **容器化** - 传统部署就够用
- ❌ **分布式缓存** - 内存缓存完全够用
- ❌ **消息队列** - 同步调用就够了

#### 🖥️ 客户端架构简化 (UltraThink Phase 4 - 2025-08-20)

**问题识别**：
- ❌ **过度复杂的服务** - 认证服务481行代码，职责混合
- ❌ **冗余接口实现** - 同时实现多个不必要的接口
- ❌ **复杂的IoC注册** - 多层包装和工厂方法
- ❌ **不必要的抽象层** - 通用API服务等过度抽象

**简化原则**：
1. **单一职责** - 每个服务只做一件事
2. **依赖最少** - 减少不必要的依赖注入
3. **代码精简** - 移除冗余功能和方法
4. **接口统一** - 避免混合实现多个接口

**实施效果**：
- ✅ **认证服务精简** - 481行 → 135行 (72%减少)
- ✅ **IoC异常修复** - 注册IAuthApi依赖
- ✅ **服务注册简化** - 单行注册替代复杂工厂
- ✅ **职责清晰** - 每个服务单一接口实现

**开发指导**：
```csharp
// ✅ 好的做法 - 简化服务
public class SimplifiedAuthenticationService : IAuthenticationService
{
    // 最少依赖，清晰职责
    private readonly IAuthApi _authApi;
    private readonly ITokenManager _tokenManager;
}

// ❌ 避免 - 过度复杂
public class AuthenticationService : IAuthenticationService, ISharedAuthService
{
    // 过多依赖，职责混合
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly SemaphoreSlim _authSemaphore;
}
```

#### 🎯 重构优先级与完成状态

**Phase 1: 安全基础** ✅ **已完成** (2025-08-17)
```csharp
// ✅ 修复SQL注入风险，使用LINQ替代原生SQL
return await _context.Users
    .Where(u => ids.Contains(u.Id))
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(u => u.Status, status)
        .SetProperty(u => u.UpdateTime, DateTime.Now));
```
- ✅ 所有Repository使用LINQ和参数化查询
- ✅ EF Core 7.0 ExecuteUpdate批量操作优化
- ✅ 类型安全JWT认证(UserRole枚举)

**Phase 2: 基础架构优化** ✅ **已完成** (2025-08-17)  
```csharp
// ✅ 智能缓存和全面健康检查
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiry)
{
    if (_cache.TryGetValue(key, out T value)) return value;
    value = await factory();
    _cache.Set(key, value, expiry);
    return value;
}
```
- ✅ 数据库连接池优化(Max=20, Min=2)
- ✅ 智能内存缓存系统(统计和性能监控)
- ✅ 全面健康检查体系(8个端点)
- ✅ Kubernetes就绪探针支持

**Phase 3: 异地组网** 🟡 **待定** (暂时搁置)
```csharp
// 🔄 多租户支持：简单的ClinicId软隔离 (按需实现)
public class BaseEntity
{
    public Guid Id { get; set; }
    public string ClinicId { get; set; } // 诊所标识
    public DateTime CreateTime { get; set; }
}
```
- 🔄 根据用户实际需求决定是否实施
- 🔄 目前专注单诊所部署，以交付为优先

### 技术栈选择 (实用版)

- 🌐 **部署**: IIS + Windows Server (成熟稳定)
- 💾 **数据库**: SQL Server Express (免费，够用) 
- 📡 **实时通信**: SignalR (微软官方，简单)
- 🗄️ **缓存**: MemoryCache (内置，零配置)
- 📊 **监控**: 简单的健康检查页面
- 🔄 **备份**: 数据库自动备份脚本

### 异地组网架构

```
总部诊所 (主节点)
    ├── 数据库主节点
    ├── 文件服务器  
    └── 备份服务
    
分点诊所A (从节点)
    ├── 本地缓存
    ├── 离线支持
    └── 数据同步
    
分点诊所B (从节点)  
    ├── 本地缓存
    ├── 离线支持
    └── 数据同步
```

### 开发约束

**✅ 必须遵循**:
1. **Repository层安全化** - 消除SQL注入风险
2. **多租户数据隔离** - ClinicId自动过滤
3. **简单缓存策略** - 常用数据10分钟内存缓存
4. **基础健康监控** - 数据库连接、磁盘空间检查

**❌ 禁止引入**:
1. **复杂的分布式技术** - 保持单体架构
2. **过度的抽象层** - Domain层等复杂设计模式
3. **高级缓存方案** - Redis、分布式缓存
4. **容器化部署** - 增加运维复杂度

### 相关文档

- [后台架构实用化建议](docs/ultrathink/backend-architecture-practical-recommendations-20250817.md) - 实用化重构方案
- [后台架构深度分析](docs/ultrathink/backend-architecture-analysis-20250817.md) - 完整架构评估 (参考)

## 核心工作流

### 诊疗流程（简化版）

```
患者接待(Patients) → 看诊(Consultation) → 开方(Prescriptions)
         ↑                    ↓
      医疗案例(MedicalCase)贯穿全程
```

- **MedicalCase** 贯穿整个流程，统一管理患者的诊疗案例和病历记录
- **Consultation** 是核心，支持中医四诊（望闻问切）
- **Patients** 模块处理患者信息管理和基础接待功能
- **Prescriptions** 结合Formula（验方）和Herbs（中药材）完成处方开具

## 🎯 UltraThink控制器架构标准（2025-08-17）

### 三层控制器体系

系统采用UltraThink统一控制器架构，所有控制器必须遵循以下三层体系：

```
BaseControllerCore (核心基础层)
    ├── BaseApiController (业务API层) - 8个核心业务模块
    │   ├── AuthController, UsersController, PatientsController
    │   ├── ConsultationController, MedicalCaseController
    │   ├── PrescriptionsController, HerbsController, FormulasController
    │   └── HerbImportExportController
    └── BaseSystemController (系统管理层) - 5个系统管理模块  
        ├── HealthController, MonitoringController
        ├── SecurityController, CacheController
        └── PerformanceController
```

### 控制器分类规则

#### 1. 业务API控制器 (继承BaseApiController)
- **用途**: 所有面向前端的业务功能API
- **响应格式**: 统一的 `ApiResponse<T>` 格式
- **异常处理**: 使用 `HandleException<T>()` 方法
- **服务结果**: 自动处理 `ServiceResult<T>` 转换

**标准模板**:
```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ExampleController : BaseApiController
{
    public ExampleController(IExampleService service, ILogger<ExampleController> logger, IMemoryCache cache)
        : base(logger, cache) { }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ExampleDto>>> GetById(Guid id)
    {
        try
        {
            var validation = ValidateGuid<ExampleDto>(id, "资源ID");
            if (validation != null) return validation;

            var result = await _service.GetByIdAsync(id);
            return HandleServiceResult(result, "查询成功");
        }
        catch (Exception ex)
        {
            return HandleException<ExampleDto>(ex, "获取资源详情", id);
        }
    }
}
```

#### 2. 系统管理控制器 (继承BaseSystemController)
- **用途**: 健康检查、监控、性能、安全等系统级功能
- **响应格式**: 简化的系统响应格式
- **异常处理**: 使用 `HandleSystemException()` 方法
- **权限**: 通常需要Admin权限

**标准模板**:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class ExampleSystemController : BaseSystemController
{
    public ExampleSystemController(ILogger<ExampleSystemController> logger)
        : base(logger) { }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var status = await GetSystemInfo();
            return SystemOk(status, "系统状态正常");
        }
        catch (Exception ex)
        {
            return HandleSystemException(ex, "获取系统状态");
        }
    }
}
```

### 响应格式标准

#### 业务API响应 (ApiResponse<T>)
```json
{
    "success": true,
    "message": "操作成功",
    "data": { "id": "123", "name": "示例" },
    "timestamp": "2025-08-17T10:30:00Z",
    "requestId": "req-123456"
}
```

#### 系统管理响应
```json
{
    "success": true,
    "message": "系统正常",
    "data": { "status": "healthy" },
    "timestamp": 1692261000,
    "requestId": "req-123456"
}
```

### 强制性开发规则

#### ✅ 必须遵循
- **基类继承**: 业务API继承BaseApiController，系统管理继承BaseSystemController
- **异常处理**: 所有public方法必须有try-catch异常处理
- **参数验证**: 使用基类提供的验证方法 (`ValidateGuid`, `ValidateModel`)
- **日志记录**: 重要操作使用 `LogOperation()` 记录日志
- **响应统一**: 使用基类提供的响应方法 (`Success`, `HandleServiceResult`, `SystemOk`)

#### ❌ 禁止行为
- 直接继承 `ControllerBase`
- 混合使用不同的响应格式
- 忽略异常处理
- 在业务API中使用系统响应格式，反之亦然

### 创建新控制器检查清单

新建业务API控制器时：
- [ ] 继承 `BaseApiController` 
- [ ] 添加正确的路由和版本配置
- [ ] 使用 `HandleServiceResult` 处理服务结果
- [ ] 使用 `HandleException<T>` 处理异常
- [ ] 添加适当的授权配置
- [ ] 记录关键操作日志

新建系统管理控制器时：
- [ ] 继承 `BaseSystemController`
- [ ] 使用 `SystemOk/SystemError` 响应方法
- [ ] 使用 `HandleSystemException` 处理异常
- [ ] 添加管理员权限检查 `[Authorize(Roles = "Admin")]`
- [ ] 返回类型使用 `IActionResult`

### 相关文档

- [控制器设计模式详解](docs/architecture/ultrathink-controller-design-patterns-20250817.md)
- [API响应标准规范](docs/architecture/ultrathink-api-response-standards-20250817.md)
- [控制器开发模板](docs/templates/controller-templates-20250817.md)

## ✅ 任务完成

### 任务管理

- **立即在 `docs/TODO-Latest-*.md` 中标记完成的任务**
- **添加发现的新任务到"发现的问题"部分**
- **每个任务完成后创建详细的 commit**

### Git 提交规范

```bash
# 提交格式
<type>: <subject>

# type 类型：
- feat: 新功能
- fix: 修复bug
- docs: 文档更新
- refactor: 重构
- test: 测试相关
- chore: 构建/工具相关
```

## 📎 风格与约定

### C# 编码规范

- **遵循 .NET 编码约定**
- **使用 PascalCase** 用于类名、方法名、属性
- **使用 camelCase** 用于参数、局部变量
- **私有字段使用 _camelCase**
- **接口以 I 开头**

### 代码注释规范

```csharp
/// <summary>
/// 方法功能简述
/// </summary>
/// <param name="参数名">参数说明</param>
/// <returns>返回值说明</returns>
public async Task<Result> MethodName(Type param)
{
    // 关键逻辑注释
    // Reason: 解释为什么这样做
}
```

## 开发约定

### 必须遵循的规则

1. **🎯 UltraThink四层架构** (最高优先级): 
   - Desktop层(Layer 4)严禁直接使用Contracts(Layer 3)的DTOs
   - 必须创建Info模型进行UI数据绑定
   - 必须使用AutoMapper实现DTO↔Info转换
   - ViewModels必须注入IMapper依赖
2. **数据库迁移**: 只能在 `LYBT.Infrastructure` 项目中添加
3. **数据访问**: 使用统一的 `AppDbContext`
4. **API 响应格式**: 遵循 [API响应标准](docs/API响应标准.md)
   - POST 方法返回 `Ok(createdObject)`
   - PUT/DELETE 方法返回 `Ok(new { message = "xxx" })`
   - 错误响应使用 `ProblemDetails`
5. **对象映射**: 使用 AutoMapper，配置在MappingProfile.cs
6. **模块模式**: 新模块遵循现有模块结构（Interfaces/Services/Repositories/Mapping）

### 环境配置

- **数据库**: SQL Server (localhost/LYBTDB)
- **API端口**: https://localhost:7001
- **默认登录**: sysadmin / Admin@123456
- **JWT过期**: 8小时（Remember Me: 30天）
- **默认密码配置**:
  - 普通用户: `ChangeMe123` (UserOptions.DefaultUserPassword)
  - 管理员: `Admin@123456` (SysAdminOptions.DefaultPassword)
  - 详见: [默认密码文档](docs/development/DEFAULT_PASSWORDS.md)

### 开发流程

1. 使用 Visual Studio 手动运行项目（根据 CLAUDE.local.md）
2. API 文档访问: https://localhost:7001/swagger
3. 使用 scripts/ 目录的批处理文件执行常见任务
4. 数据库在首次运行时自动初始化

## 📚 文档与可解释性

### 文档更新要求

- **添加新功能时更新 `README.md`**
- **更改依赖时更新 `docs/开发规范.md`**
- **修改设置时更新本文档**
- **注释非显而易见的代码**，确保中级开发者能理解
- **编写复杂逻辑时，添加 `# Reason:` 注释**解释原因

## 🧠 AI 行为规则

### 必须遵守

- **永远不要假设缺失的上下文，不确定时询问**
- **永远不要虚构库或函数**，只使用已验证的包
- **始终确认文件路径和模块名存在**再引用
- **除非明确指示，否则不要删除或覆盖现有代码**

### 中文支持

- **所有显示和回答使用中文**
- **注意处理中文字符编码问题**
- **API 响应消息使用中文**
- **错误提示使用中文**

## 术语说明

- **Prescriptions**: 处方（医生开具的用药指导）
- **Formula**: 验方（经典处方模板）
- **Consultation**: 看诊（中医四诊过程）
- **Herbs**: 中药材（仅用于处方，不含库存管理）
- **TCM**: 中医（Traditional Chinese Medicine）
- **MedicalCase**: 医疗案例（诊疗流程聚合根，包含完整病历）
- **Patients**: 患者（包含档案和基础接待功能）

## 项目特定指令

- 显示和回答都用中文
- 本项目数据库为 SQL Server（不是 LocalDB）
- 开发时手动用 VS 执行运行操作

## 文档管理

- 文档同时保留一份中文版，一份英文版。

## 开发规范文档

- [开发规范](docs/开发规范.md) - 完整的开发规范指南
- [前后端契约规范](docs/前后端契约规范.md) - 前后端接口约定
- [API响应标准](docs/API响应标准.md) - API 响应格式规范

### 🎆 UltraThink四层架构文档 (2025-08-17)

- [UltraThink四层架构重构完成报告](docs/ultrathink/ultrathink-four-layer-refactoring-complete-20250817.md) - 重构总结
- [Desktop层DTO违规分析](docs/reports/desktop-dto-architecture-violation-analysis-20250817.md) - 架构问题分析
- [UltraThink API响应标准](docs/architecture/ultrathink-api-response-standards-20250817.md) - API设计标准
- [UltraThink控制器设计模式](docs/architecture/ultrathink-controller-design-patterns-20250817.md) - 控制器架构

## 🔧 特殊配置说明

### AutoMapper 配置（重要）

- 使用 AutoMapper 15.0.1
- **必须提供 ILoggerFactory 参数**
- 示例配置：
  
  ```csharp
  var mapperConfig = new MapperConfiguration(cfg =>
  {
    cfg.AddProfile(new MappingProfile());
  }, NullLoggerFactory.Instance);  // 关键：需要ILoggerFactory参数
  ```

**测试中的AutoMapper配置**：
```csharp
// 在单元测试中的正确配置方式
var config = new MapperConfiguration(cfg => 
    cfg.AddProfile(new UserMappingProfile()), 
    NullLoggerFactory.Instance);  // 必须的第二参数
var mapper = config.CreateMapper();
```

### 依赖注入配置

- 使用 Prism.DryIoc 9.0.537
- 服务注册在 ServiceCollectionExtensions.cs
- 所有服务使用构造函数注入

## 脚本管理

- 脚本都用Python脚本

## 📋 常见问题快速解决

### 编译错误

1. 检查 NuGet 包版本
2. 清理解决方案：`dotnet clean`
3. 重新生成：`dotnet build`

### 依赖注入错误

1. 检查服务是否已注册
2. 确认接口和实现匹配
3. 检查构造函数参数

### 数据库连接问题

1. 确认 SQL Server 服务运行
2. 检查连接字符串
3. 验证数据库权限

## 🎯 项目质量目标

- **代码覆盖率目标：60%**（当前：2.76%）
- 响应时间 < 2秒
- 零关键bug
- 完整的错误处理
- 用户友好的中文提示

### 质量改进计划

1. **高优先级**：完成核心Service层测试（HerbService、AuthService）
2. **中优先级**：添加Controller层测试，提升覆盖率到60%
3. **低优先级**：实现缓存机制、API版本管理

## 中文编码

- 使用 UTF-8 编码处理所有中文字符
- 建议使用 Unicode 标准处理中文文本
- 在文件头添加 BOM 头以确保正确识别编码
- 注意跨平台兼容性和编码一致性
- windows 10 中文版 开发环境
# 架构治理基线 - Record-Only 模式

**版本**: v1.0  
**生效日期**: 2025-09-12  
**适用范围**: 凌隐宝堂中医诊所系统 (LYBTZYZS)
**治理级别**: 强制执行 (ArchTests + CI门禁)

## 🏗️ Record-Only 功能模式

### 核心设计原则

系统采用**Record-Only功能模式**，专注于数据记录和历史查询，严格禁止复杂业务逻辑：

```
功能范围: 仅CRUD操作 + 历史查询
数据处理: 记录存储、状态变更、搜索筛选
禁止功能: 智能推荐、配伍检查、复杂规则引擎、工作流、流水线、会话管理、复杂状态机
```

### 允许功能清单

```
✅ Create: 创建新记录 (患者、医案、处方等)
✅ Read: 数据查询 (GetById, GetPaged, Search)
✅ Update: 字段更新 (基础信息修改、状态切换)
✅ Delete: 记录删除 (软删除、状态管理)
✅ History: 历史查询 (就诊记录、处方历史)
✅ Search: 条件搜索 (姓名、时间、状态筛选)
✅ Calculate: 基础计算 (价格计算、数量统计)
```

### 严格禁止功能

```
❌ 智能推荐系统 (药材推荐、验方推荐)
❌ 配伍安全检查 (超出基础安全验证)
❌ 复杂业务规则引擎
❌ 工作流引擎 (自动化流程管理)
❌ 数据流水线 (复杂数据处理管道)
❌ 会话管理 (超出基础用户登录会话)
❌ 复杂状态机 (多状态自动转换)
❌ 事件驱动架构
❌ 预测性分析和机器学习
```

## 📐 分层架构定义

### 统一四层架构

系统采用统一的分层架构，所有层次严格遵循依赖方向：

```
Layer 1: UI层 (Desktop.ViewModels + LYBT.WebAPI.Controllers)
├── 职责: 用户交互、HTTP请求处理、数据展示、参数验证
├── 依赖: Application层接口
└── 禁止: 直接访问Domain层、Infrastructure层

Layer 2: Application层 (Desktop.Modules + Modules.Services)
├── 职责: 应用服务、业务编排、DTO转换、权限检查
├── 依赖: Domain层接口、Infrastructure层接口
└── 禁止: UI框架依赖、具体数据库实现

Layer 3: Domain层 (Entities + 领域服务)
├── 职责: 实体定义、领域逻辑、业务规则、实体关系
├── 依赖: 仅依赖.NET BCL
└── 禁止: 基础设施关注点、UI关注点

Layer 4: Infrastructure层 (LYBT.Infrastructure)
├── 职责: 数据访问、外部服务、技术实现
├── 依赖: Domain层接口、第三方库、数据库
└── 禁止: 业务逻辑、UI相关代码
```

## 🚫 层间依赖禁止规则

### 严格禁止的依赖关系

```
❌ UI层 ↛ Infrastructure层
❌ UI层 ↛ Entities层  
❌ Controllers ↛ Infrastructure实现类
❌ ViewModels ↛ 数据库上下文
❌ 任何层 ↛ 具体实现（必须通过接口）
```

### 允许的依赖关系

```
✅ 上层 → 下层（通过接口）
✅ 任何层 → Shared层
✅ 服务层 → Infrastructure接口
✅ Infrastructure → Entities
✅ 控制器 → 服务接口
```

## 🌐 API约束与响应标准

### API版本约束

**严格API版本控制**: 系统仅允许API v1版本，禁止版本扩散

```json
{
  "允许路由": "/api/v1/*",
  "禁止路由": ["/api/v2/*", "/api/v3/*", "/v2/*", "/v3/*", "/api/*"]
}
```

### 控制器驻留约束

**集中控制器管理**: 所有Web API控制器必须驻留在LYBT.WebAPI项目

```
✅ 允许位置: src/Server/Services/LYBT.WebAPI/Controllers/
❌ 禁止位置: 其他任何项目中的Controllers文件夹
❌ 禁止分散: 模块项目中的独立API控制器
```

### 统一响应格式

**标准化API响应**: 所有API必须使用统一的响应包装器

```csharp
// ✅ 成功响应格式
ApiResponse<T>.Success(data, "操作成功")

// ✅ 失败响应格式  
ApiResponse<T>.Fail("错误消息", ErrorCode.ValidationError)
```

### Username命名约束

**用户标识规范**: 系统中用户相关字段统一使用Username命名

```json
{
  "标准命名": "Username",
  "禁止命名": ["UserName", "user_name", "userName", "loginName"]
}
```

## ⚡ 事务管理约束

### EF Core隐式事务优先

**事务策略**: 优先使用EF Core SaveChanges的隐式事务机制

```csharp
// ✅ 首选方式: EF Core隐式事务
await _context.SaveChangesAsync();

// ✅ 必要时: 最小显式事务
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // 最少必要操作
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 禁止业务层步骤框架

**事务边界约束**: 严格禁止引入业务层步骤框架和补偿事务模式

```
❌ 禁止框架: Saga Pattern, Workflow Engine, Step Functions
❌ 禁止模式: 补偿事务、分布式事务协调器
❌ 禁止库: MediatR事务行为、Hangfire事务作业
```

## 📦 依赖管理约束

### 中央包管理强制

**统一包管理**: 所有NuGet包版本必须在Directory.Packages.props中集中管理

```xml
<!-- ✅ 标准方式: 中央包管理 -->
<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.17" />
<PackageVersion Include="AutoMapper" Version="15.0.1" />

<!-- ❌ 禁止方式: 项目独立包引用 -->
<PackageReference Include="SomePackage" Version="1.0.0" />
```

### 禁止重依赖引入

**依赖约束**: 严格禁止引入与Record-Only基线无关的重型框架

```json
{
  "禁止框架类别": [
    "工作流引擎 (Workflow Foundation, Elsa)",
    "规则引擎 (Rules Engine, Decision Tables)", 
    "事件总线 (MediatR, NServiceBus)",
    "状态机 (Stateless, Automatonymous)",
    "流水线框架 (Pipeline Patterns)",
    "会话引擎 (Session State Providers)",
    "AI/ML框架 (ML.NET, TensorFlow.NET)"
  ]
}
```

## 📊 质量门禁标准

### 编译质量强制要求

**零容忍编译问题**: 所有代码提交必须通过以下质量检查

```bash
# 强制执行的质量检查命令
dotnet format --verbosity minimal
dotnet build --no-restore --verbosity minimal
dotnet test --no-build --verbosity minimal
```

```
✅ 编译错误: 0个 (阻塞性)
✅ 编译警告: 0个 (阻塞性)
✅ 格式检查: 通过dotnet format (阻塞性)
✅ 单元测试: 全部通过 (阻塞性)
✅ 架构测试: 全部通过 (阻塞性)
```

### 架构合规性检查

**自动化架构验证**: ArchTests强制执行以下架构约束

```
✅ 层间依赖合规: UI层不得直接依赖Infrastructure层
✅ API版本合规: 禁止/api/v2等非v1路由
✅ 控制器位置合规: 仅允许LYBT.WebAPI项目包含Controllers
✅ 命名规范合规: 禁止Pipeline/Workflow等命名
✅ 框架使用合规: 禁止引入工作流/规则引擎等重型框架
✅ 事务模式合规: 禁止复杂事务协调框架
```

## 🚨 CI强制门禁

### 三级门禁体系

**门禁级别**: 所有检查失败立即阻塞PR合并

```yaml
# Level 1: 代码质量门禁
- dotnet format --verify-no-changes
- dotnet build --configuration Release
- 编译错误/警告 = 0

# Level 2: 测试质量门禁  
- dotnet test --configuration Release
- 单元测试通过率 = 100%
- 架构测试通过率 = 100%

# Level 3: 架构合规门禁
- ArchTests.LayerDependencyTests
- ArchTests.ApiVersionTests  
- ArchTests.ControllerLocationTests
- ArchTests.NamingConventionTests
- ArchTests.ForbiddenFrameworkTests
```

### CI失败处理流程

**零容忍违规策略**: 任何门禁失败立即阻塞

```
1. CI检测到违规 → 自动阻塞PR
2. 开发者必须修复所有违规 → 重新提交
3. 所有门禁通过 → 允许合并
4. 紧急情况例外 → 架构师手动批准（需记录ADR）
```

## 🔍 违规检测与处理

### 自动检测机制

**全方位监控**: 多层次自动化检测违规行为

```
1. 静态分析: 代码结构、命名、依赖关系分析
2. 编译检查: 框架引用、API路由、层间依赖
3. 运行时检查: 事务模式、响应格式验证
4. 架构测试: NetArchTest规则定义和执行
```

### 违规分类与处理

```json
{
  "阻塞性违规": {
    "层间依赖违规": "立即CI失败",
    "API版本违规": "立即CI失败", 
    "禁止框架引入": "立即CI失败",
    "控制器位置违规": "立即CI失败"
  },
  "警告性违规": {
    "命名不规范": "CI警告，建议修复",
    "注释缺失": "CI警告，建议完善"
  }
}
```

## 📋 例外管理

### 严格例外控制

**最小例外原则**: 例外情况严格控制，必须有充分理由

```
✅ 允许例外情况:
  - 遗留系统集成必需的复杂逻辑
  - .NET框架内置Pipeline（如中间件）
  - 第三方SDK要求的特定模式

❌ 不允许例外:
  - 业务逻辑复杂化为理由
  - 开发效率为理由引入重型框架
  - 个人偏好的技术选型
```

### 例外申请与追踪

```
1. 提交架构例外申请 (包含影响分析和风险评估)
2. 架构师review和决策 (必须在ADR中记录)  
3. 临时豁免配置 (设定明确的截止时间)
4. 定期review例外合理性 (季度review，自动到期)
```

## 📚 治理文档体系

### 机器可读规则

- **[.ai/rules.json](../.ai/rules.json)** - 完整的机器可读治理规则配置
- **[tests/Architecture/ArchTests.cs]** - NetArchTest架构约束测试套件
- **[.github/workflows/ci.yml]** - CI/CD强制门禁配置

### 人工可读文档

- **[CONTRIBUTING.md]** - 贡献指南和开发流程
- **[.github/PULL_REQUEST_TEMPLATE.md]** - PR检查清单模板
- **[CLAUDE.md](../CLAUDE.md)** - AI助手开发约定

---

**文档版本**: v1.0  
**生效日期**: 2025-09-12  
**强制执行**: ArchTests + CI门禁  
**下次review**: 2025-12-12
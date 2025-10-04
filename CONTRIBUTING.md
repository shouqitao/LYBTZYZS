# 贡献指南 - Pass 7 治理基线

欢迎为凌隐宝堂中医诊所系统 (LYBTZYZS) 做出贡献！本项目已建立 **Pass 7 治理基线**，所有贡献必须严格遵循 Record-Only 架构约束和质量标准。

## 🚨 治理基线概述

本项目实施**强制性治理基线**，通过 ArchTests + CI 门禁自动执行：

- **功能模式**: Record-Only (仅CRUD + 历史查询)
- **架构模式**: 统一四层架构 (UI → Application → Domain → Infrastructure)  
- **API约束**: 仅允许 /api/v1/* 路由
- **事务管理**: EF Core 隐式事务优先，最小显式事务
- **质量标准**: 零编译错误零警告，100%测试通过率

## 📋 开发前准备

### 1. 环境要求

```bash
- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- SQL Server (开发环境)
- Git 客户端
```

### 2. 克隆和设置

```bash
git clone <repository-url>
cd LYBTZYZS
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln
```

### 3. 运行架构测试验证

```bash
# 验证当前代码库符合治理基线
dotnet test tests/Architecture/LYBT.ArchTests.csproj --verbosity normal
```

## 🔄 开发工作流

### 1. 创建功能分支

```bash
git checkout -b feature/your-feature-name
# 或
git checkout -b fix/bug-description
# 或  
git checkout -b chore/task-description
```

### 2. 开发过程检查

在开发过程中，定期运行以下命令确保合规：

```bash
# Level 1: 代码质量检查
dotnet format LYBT.All.sln --verbosity minimal
dotnet build LYBT.All.sln --configuration Release --no-restore

# Level 2: 测试质量检查  
dotnet test --configuration Release --verbosity minimal

# Level 3: 架构合规检查
dotnet test tests/Architecture/LYBT.ArchTests.csproj --verbosity minimal
```

### 3. 提交规范

使用 [Conventional Commits](https://www.conventionalcommits.org/) 格式：

```
<type>(<scope>): <subject>

<body>

<footer>
```

**类型 (type)**:
- `feat`: 新功能 (仅限Record-Only范围内)
- `fix`: Bug修复
- `docs`: 文档更新
- `test`: 测试相关
- `refactor`: 重构 (不改变功能)
- `chore`: 构建/工具相关

**示例**:
```
feat(patients): add patient basic info CRUD operations

- Add CreatePatient, UpdatePatient, DeletePatient methods
- Add patient search and pagination
- Follow Record-Only baseline - no complex business logic

Closes #123
```

## 🚫 严格禁止事项

### 功能禁区 (违规将自动阻塞PR)

❌ **禁止引入的功能**:
- 智能推荐系统 (药材推荐、验方推荐)
- 配伍安全检查 (超出基础安全验证)
- 复杂业务规则引擎
- 工作流引擎 (自动化流程管理)  
- 数据流水线 (复杂数据处理管道)
- 会话管理 (超出基础用户登录会话)
- 复杂状态机 (多状态自动转换)
- 事件驱动架构
- 预测性分析和机器学习

### 技术禁区

❌ **禁止的框架和库**:
```json
{
  "工作流引擎": ["Workflow Foundation", "Elsa"],
  "规则引擎": ["Rules Engine", "Decision Tables"], 
  "事件总线": ["MediatR", "NServiceBus", "MassTransit"],
  "状态机": ["Stateless", "Automatonymous"],
  "流水线": ["Pipeline Patterns"],
  "会话引擎": ["Session State Providers"],
  "AI/ML框架": ["ML.NET", "TensorFlow.NET"]
}
```

❌ **禁止的命名模式**:
- 类名包含: `Pipeline`, `Workflow`, `Bus`, `Engine`, `Saga`
- 命名空间包含: `*.Workflows.*`, `*.Pipelines.*`, `*.Events.*`

❌ **禁止的API路由**:
- `/api/v2/*`, `/api/v3/*`, `/v2/*`, `/v3/*`
- 仅允许: `/api/v1/*`

## ✅ 允许的功能范围

### Record-Only 操作类型

```
✅ Create: 创建新记录 (患者、医案、处方等)
✅ Read: 数据查询 (GetById, GetPaged, Search)
✅ Update: 字段更新 (基础信息修改、状态切换)
✅ Delete: 记录删除 (软删除、状态管理)
✅ History: 历史查询 (就诊记录、处方历史)
✅ Search: 条件搜索 (姓名、时间、状态筛选)
✅ Calculate: 基础计算 (价格计算、数量统计)
✅ Validate: 基础数据验证
```

### 8个核心业务模块

1. **Auth** - 身份认证记录 (登录、登出、会话管理)
2. **Users** - 用户信息记录 (用户CRUD、角色分配)  
3. **Patients** - 患者档案记录 (患者信息管理、历史查询)
4. **MedicalCase** - 医疗案例记录 (案例创建、状态更新、查询)
5. **Consultation** - 看诊记录 (四诊数据记录、历史回顾)
6. **Prescriptions** - 处方记录 (处方开具、价格计算、打印)
7. **Herbs** - 中药材记录 (药材信息管理、库存记录)
8. **Formula** - 验方记录 (验方模板管理、历史查询)

## 🏗️ 架构约束

### 统一四层架构

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

### API设计约束

- **版本控制**: 仅允许 `/api/v1/*` 路由
- **控制器位置**: 所有控制器必须在 `LYBT.WebAPI` 项目
- **响应格式**: 统一使用 `ApiResponse<T>` 格式
- **命名规范**: 用户字段统一使用 `Username`

### 事务管理约束

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

## 🧪 测试要求

### 测试覆盖率要求

- **单元测试**: 新功能必须有对应测试
- **架构测试**: 必须通过所有 12 个架构约束测试
- **集成测试**: 涉及数据库操作的功能需要集成测试

### 运行测试

```bash
# 运行所有单元测试
dotnet test tests/Backend/ --configuration Release

# 运行架构测试 (必须全部通过)
dotnet test tests/Architecture/LYBT.ArchTests.csproj

# 运行特定架构测试
dotnet test tests/Architecture/LYBT.ArchTests.csproj --filter "FullyQualifiedName~LayerDependencyTests"
```

## 📝 文档要求

### 必须更新的文档

1. **API变更**: 更新相关API文档
2. **架构变更**: 更新架构设计文档  
3. **功能变更**: 更新用户指南
4. **配置变更**: 更新部署文档

### 文档位置

```
docs/
├── architecture/    # 架构设计文档
├── api/            # API文档和规范
├── guides/         # 用户指南
└── development/    # 开发指南
```

## 🚨 CI/CD 门禁流程

### 三级门禁体系 (全部阻塞性)

**Level 1: 代码质量门禁**
```yaml
- dotnet format --verify-no-changes  # 格式检查
- dotnet build --configuration Release  # 编译检查
```

**Level 2: 测试质量门禁**
```yaml
- dotnet test --configuration Release  # 单元测试
- dotnet test tests/Architecture/  # 架构测试
```

**Level 3: 架构合规门禁**
```yaml
- LayerDependencyTests  # 层间依赖检查
- ApiVersionTests  # API版本检查
- ControllerLocationTests  # 控制器位置检查
- NamingConventionTests  # 命名规范检查
- ForbiddenFrameworkTests  # 禁止框架检查
- RecordOnlyTests  # Record-Only功能检查
```

### 门禁失败处理

如果任何门禁失败：

1. **PR自动阻塞** - 无法合并直到修复所有问题
2. **查看构建日志** - 识别具体失败原因
3. **本地修复** - 在本地环境修复所有问题
4. **重新提交** - 推送修复后的代码

## 📞 获取帮助

### 联系方式

- **GitHub Issues**: 报告Bug或请求功能
- **GitHub Discussions**: 讨论架构和设计决策

### 常见问题

**Q: 我的PR被CI阻塞了，怎么办？**
A: 查看CI构建日志，修复所有失败的检查项，然后重新提交。

**Q: 我想添加一个复杂的业务规则，但被架构测试阻塞？**
A: 检查该功能是否符合Record-Only基线。如果超出CRUD+历史查询范围，需要重新设计为简单的数据记录功能。

**Q: 我需要使用某个新的NuGet包，应该怎么做？**
A: 确保该包不在禁止框架列表中，然后在 `Directory.Packages.props` 中添加版本定义。

**Q: 架构测试失败了，但我认为这是合理的设计？**
A: 治理基线是强制性的。如果确实需要例外情况，请提交架构例外申请，包含影响分析和风险评估。

## 📋 检查清单模板

在提交PR前，请使用此检查清单：

### 开发完成检查
- [ ] 功能仅限Record-Only范围 (CRUD + 历史查询)
- [ ] 未引入禁止的框架或命名模式
- [ ] 遵循统一四层架构约束
- [ ] API使用 /api/v1/* 路由格式
- [ ] 事务管理使用EF Core隐式事务优先

### 质量检查
- [ ] `dotnet format --verify-no-changes` 通过
- [ ] `dotnet build --configuration Release` 零错误零警告
- [ ] `dotnet test --configuration Release` 全部通过
- [ ] `dotnet test tests/Architecture/` 全部通过

### 文档检查
- [ ] 已更新相关文档
- [ ] 提交信息符合Conventional Commits规范
- [ ] PR描述清晰，包含变更说明

---

**感谢您遵循Pass 7治理基线为项目做出高质量的贡献！** 🙏

严格的架构约束确保系统保持简洁、可维护，专注于小诊所的实际需求。
# 架构原则（Architecture Principles）

**创建日期**: 2025-10-25
**维护者**: 项目架构团队
**目的**: 定义项目的架构原则三级分类体系，指导所有架构决策和代码实现

---

## 📋 什么是架构原则？

架构原则是软件系统设计和实现过程中必须遵循的基本准则。本项目采用**三级分类体系**，明确区分强制性、推荐性和指导性原则。

**核心价值**：
- ✅ **清晰的优先级**：明确哪些原则必须遵守，哪些可以权衡
- ✅ **决策指导**：为架构决策提供明确依据
- ✅ **例外管理**：违反强制原则需要正式批准（ADR + 例外清单）
- ✅ **团队对齐**：统一团队对架构原则的理解

---

## 🎯 三级分类体系

### Level 0: 强制原则（Mandatory）⭐⭐⭐

**定义**：必须遵守的核心原则，违反需要正式批准（ADR + 例外清单）

**特征**：
- ❌ 不允许违反，除非经过ADR批准
- ⚠️ 违反会导致严重的架构问题或业务风险
- 🔒 所有违反情况必须记录在[架构例外清单](./exceptions.md)

---

### Level 1: 推荐原则（Recommended）⭐⭐

**定义**：强烈推荐遵守的原则，但允许权衡取舍

**特征**：
- ✅ 应该遵守，但可以根据具体场景权衡
- ⚠️ 违反不需要ADR，但需要在代码审查中说明理由
- 📝 重大违反建议记录在commit message或PR描述中

---

### Level 2: 指导原则（Guideline）⭐

**定义**：最佳实践指导，团队约定的编码风格和模式

**特征**：
- 💡 建议遵守，提高代码一致性
- ✅ 违反不需要说明，但建议遵循
- 📚 主要用于新成员学习和代码审查参考

---

## 📐 架构原则详细清单

### Level 0: 强制原则（10条）⭐⭐⭐

#### P0-1: 技术黑名单禁止使用

**原则描述**：严格禁止使用以下技术栈（MVP阶段）

**禁用技术清单**：
- ❌ Redis/Memcached（缓存）
- ❌ CQRS/Event Sourcing（复杂架构模式）
- ❌ MediatR/消息总线
- ❌ Docker/Kubernetes（容器化）
- ❌ GraphQL（API设计）
- ❌ RabbitMQ/Kafka（消息队列）
- ❌ Microservices（微服务架构）
- ❌ NoSQL数据库（MongoDB/Cassandra）

**批准要求**：
- 如需使用，必须创建ADR说明理由
- 必须在[架构例外清单](./exceptions.md)中记录
- 必须得到技术负责人批准

**参考资料**：
- `.spec-workflow/steering/constitution.md` - Constitution第2条
- `docs/business-rules.md` - 业务规则#13

---

#### P0-2: 依赖方向必须正确（三层架构）

**原则描述**：严格遵守三层架构的依赖方向

**Server端依赖方向**（自上而下）：
```
Presentation (WebAPI Controllers)
    ↓ 只能依赖
Application (Services)
    ↓ 只能依赖
Domain (Entities, Repositories Interfaces)

❌ 禁止：Application → Presentation
❌ 禁止：Domain → Application
❌ 禁止：Domain → Presentation
```

**Client端依赖方向**（自上而下）：
```
View (XAML)
    ↓ 只能依赖
ViewModel
    ↓ 只能依赖
Model / Repository / API Client

❌ 禁止：ViewModel → View
❌ 禁止：Model → ViewModel
```

**批准要求**：
- 违反必须创建ADR说明理由（如EXC-001: Desktop端违反三层架构）
- 必须在[架构例外清单](./exceptions.md)中记录

**参考资料**：
- `docs/architecture/server/README.md` - Server端三层架构
- `docs/architecture/client/README.md` - Client端MVVM架构
- [ADR-001: 三层对齐架构标准](./decisions/ADR-001-three-tier-alignment.md)（假设存在）

---

#### P0-3: 聚合根边界不可跨越

**原则描述**：DDD聚合根的子实体不允许独立操作

**Server端聚合根边界**：
```
MedicalCase（聚合根）
  ├─ Consultation（子实体）
  ├─ Prescription（子实体）
  └─ Diagnosis（子实体）

✅ 正确：通过MedicalCase操作子实体
await _medicalCaseRepository.CreatePrescriptionAsync(medicalCaseId, dto);

❌ 错误：直接操作子实体
await _prescriptionRepository.CreateAsync(dto); // 绕过聚合根
```

**批准要求**：
- 违反必须创建ADR说明理由
- 必须在[架构例外清单](./exceptions.md)中记录

**参考资料**：
- `docs/business-rules.md` - 业务规则#3（聚合根边界）
- [ADR-002: MedicalCase DDD聚合根模式](./decisions/ADR-002-ddd-aggregate-root.md)（假设存在）
- [ADR-003: Repository层简化](./decisions/ADR-003-repository-simplification.md)

---

#### P0-4: 所有代码变更必须Issue跟踪

**原则描述**：任何代码、文档、配置变更必须先创建GitHub Issue

**强制要求**：
- ✅ 所有代码变更：必须先有GitHub Issue
- ✅ 所有文档修正：必须先创建GitHub Issue
- ✅ 所有Bug修复：必须先创建GitHub Issue
- ✅ 所有重构优化：必须先创建GitHub Issue
- ❌ 禁止无Issue工作："顺手修改"、"临时调整"都必须先创建Issue

**Commit Message要求**：
```bash
git commit -m "fix(module): 修复XXX问题

Fixes #1234  # 自动关闭Issue

- 具体改动1
- 验证：功能已正常工作"
```

**参考资料**：
- `CLAUDE.md` - Section 2（Issue驱动工作流）
- `.claude/core/WORKFLOW.md`

---

#### P0-5: 编译质量标准（0 errors, 0 warnings）

**原则描述**：所有代码提交前必须通过编译认证

**强制要求**：
```bash
# 所有提交前必须执行
dotnet build LYBT.All.sln -c Release --no-restore

# 结果要求
✅ 0 errors
✅ 0 warnings
```

**警告处理策略**：
- ≤20个警告：直接修复后提交
- >20个警告：创建Issue跟踪，分批修复

**参考资料**：
- `CLAUDE.md` - Section 4.1（核心质量标准）
- `docs/development/shared/quality-standards.md`（计划中）

---

#### P0-6: 运行时验证强制要求

**原则描述**：所有功能完成必须运行时验证，不能只编译通过

**强制要求**：
- ✅ 启动应用（Client + Server）
- ✅ 执行真实操作场景
- ✅ 验证数据库状态（必要时）
- ✅ 从用户视角确认功能完整可用
- ❌ 禁止只编译通过就提交
- ❌ 禁止"看起来没问题"就关闭Issue

**验收标准**：
```
编译通过 + 运行时验证通过 + 功能完整可用 = 任务完成
```

**参考资料**：
- `CLAUDE.md` - Section 2.6（完成标准与文档更新）

---

#### P0-7: 文档与代码必须同步

**原则描述**：代码变更必须同步更新文档，不允许滞后

**强制要求**：
- ✅ 架构调整：必须先更新ADR和架构文档
- ✅ API变更：必须同步更新API文档和Swagger
- ✅ 模块重构：必须同步更新模块README
- ✅ 配置变更：必须同步更新配置文档

**工作流**：
```
架构调整前：
  Step 1: 创建ADR
  Step 2: 更新架构文档
  Step 3: 更新例外清单（如有违反）
  Step 4: 开始代码变更

功能开发中：
  代码变更 → 立即更新文档 → 提交时一起commit
```

**参考资料**：
- `CLAUDE.md` - Section 2.6（代码与文档并行开发要求）
- `docs/development/shared/documentation-guidelines.md`

---

#### P0-8: Constitution合规性检查

**原则描述**：所有新功能/重构前必须检查Constitution合规性

**强制检查项**：
- ✅ 是否违反技术黑名单
- ✅ 是否符合MVP优先原则（够用即好）
- ✅ 是否符合三层对齐架构规范
- ✅ 是否遵守聚合根边界

**检查时机**：
- 需求分析阶段（lybtzyzs-requirements-arch-guard Skill）
- 设计文档阶段（lybtzyzs-design-arch-validator Skill）
- 代码审查阶段（lybtzyzs-mvp-compliance Skill）

**参考资料**：
- `.spec-workflow/steering/constitution.md`
- `.claude/skills/lybtzyzs-mvp-compliance/SKILL.md`

---

#### P0-9: UTF-8 with BOM编码标准

**原则描述**：所有文本文件必须使用UTF-8 with BOM编码

**强制要求**：
- ✅ 所有.cs文件
- ✅ 所有.json文件
- ✅ 所有.xml文件
- ✅ 所有.md文件

**检查方法**：
```powershell
# PowerShell检查文件编码
Get-Content -Path "file.cs" -Encoding UTF8
```

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）

---

#### P0-10: 依赖注入仅用构造函数

**原则描述**：严格禁止ServiceLocator和Container.Resolve

**正确示例**：
```csharp
public class PrescriptionManagementViewModel
{
    private readonly IPrescriptionApi _api;
    private readonly IMedicalCaseRepository _repository;

    // ✅ 正确：构造函数注入
    public PrescriptionManagementViewModel(
        IPrescriptionApi api,
        IMedicalCaseRepository repository)
    {
        _api = api;
        _repository = repository;
    }
}
```

**错误示例**：
```csharp
public class PrescriptionManagementViewModel
{
    // ❌ 错误：ServiceLocator
    private readonly IPrescriptionApi _api =
        ServiceLocator.Current.GetInstance<IPrescriptionApi>();

    // ❌ 错误：Container.Resolve
    private readonly IPrescriptionApi _api =
        App.Container.Resolve<IPrescriptionApi>();
}
```

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）
- `docs/development/shared/dependency-injection-guidelines.md`（计划中）

---

### Level 1: 推荐原则（15条）⭐⭐

#### P1-1: MVVM模式严格遵守

**原则描述**：Desktop端严格遵守MVVM模式，View不应包含业务逻辑

**推荐实践**：
- ✅ View仅负责UI呈现和数据绑定
- ✅ ViewModel负责业务逻辑和数据管理
- ✅ Model负责数据结构和持久化
- ⚠️ View的Code-Behind仅允许UI逻辑（动画、焦点控制等）

**权衡场景**：
- 特殊UI交互（如复杂动画、手势处理）可以在Code-Behind实现
- 第三方控件初始化可以在Code-Behind实现
- 需要在PR中说明理由

**参考资料**：
- `docs/architecture/client/README.md` - MVVM架构
- [ADR-004: Component设计指南](./decisions/ADR-004-component-design-guidelines.md)

---

#### P1-2: 异步方法必须async/await

**原则描述**：涉及I/O操作必须使用async/await，避免阻塞

**推荐实践**：
```csharp
// ✅ 正确：异步方法
public async Task<Prescription> GetPrescriptionAsync(int id)
{
    var response = await _api.GetPrescriptionByIdAsync(id);
    return response.Data;
}

// ❌ 错误：同步阻塞
public Prescription GetPrescription(int id)
{
    var response = _api.GetPrescriptionByIdAsync(id).Result; // 阻塞
    return response.Data;
}
```

**权衡场景**：
- 构造函数中不能使用async（需要改为Initialize方法）
- 事件处理器中可以使用async void（但建议避免）

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）

---

#### P1-3: 单文件不超过500行

**原则描述**：单个文件建议不超过500行，复杂逻辑拆分模块

**推荐实践**：
- ✅ ViewModel >500行：拆分为多个ViewModel或提取Component
- ✅ Service >500行：拆分为多个Service或提取Helper
- ✅ Controller >500行：拆分为多个Controller或提取Service

**权衡场景**：
- 自动生成的代码（如Designer.cs）可以超过500行
- 包含大量DTO定义的文件可以超过500行
- 需要在代码审查中评估是否真的需要拆分

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）

---

#### P1-4: 命名规范遵守

**原则描述**：统一的命名规范提高代码可读性

**推荐实践**：
- ✅ 类型与公开成员：`PascalCase`
- ✅ 私有字段：`_camelCase`
- ✅ 常量：`UPPER_SNAKE_CASE`
- ✅ 异步方法：`Async` 结尾
- ✅ 接口：`I` 开头

**权衡场景**：
- 第三方库要求的命名（如Prism的`OnNavigatedTo`）
- 序列化DTO与外部系统对接时（如JSON属性名）

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）
- `docs/development/shared/naming-conventions.md`（计划中）

---

#### P1-5: 测试覆盖核心逻辑

**原则描述**：新增/修改核心逻辑需补充单元或集成测试

**推荐实践**：
- ✅ Repository层：每个CRUD方法有对应测试
- ✅ Service层：每个业务方法有对应测试
- ✅ ViewModel：核心Command有对应测试
- ✅ 使用AAA模式（Arrange-Act-Assert）

**权衡场景**：
- MVP阶段可以暂缓测试（但需要记录技术债务）
- UI测试可以优先级较低
- 简单的CRUD可以只测试Repository

**参考资料**：
- `.claude/modes/testing.md`
- `.claude/skills/lybtzyzs-test-generator/SKILL.md`

---

#### P1-6: Commit Message规范

**原则描述**：清晰的Commit Message提高可追溯性

**推荐实践**：
```bash
<type>(<scope>): <subject>

Fixes #1234  # 自动关闭Issue
Related to Epic #1234  # 关联Epic但不关闭

- 具体改动1
- 具体改动2
- 验证：功能已正常工作

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

**Type类型**：
- `feat`: 新功能
- `fix`: Bug修复
- `refactor`: 重构
- `docs`: 文档更新
- `test`: 测试相关
- `chore`: 构建/工具配置

**权衡场景**：
- 紧急修复可以简化Commit Message
- 批量格式化代码可以使用`chore: format code`

**参考资料**：
- `CLAUDE.md` - Section 4.4（提交规范）

---

#### P1-7: 代码注释使用中文

**原则描述**：统一使用中文注释，提高团队沟通效率

**推荐实践**：
```csharp
// ✅ 正确：中文注释
/// <summary>
/// 获取指定ID的处方信息
/// </summary>
/// <param name="id">处方ID</param>
/// <returns>处方详情</returns>
public async Task<Prescription> GetPrescriptionAsync(int id)
{
    // 调用API获取数据
    var response = await _api.GetPrescriptionByIdAsync(id);
    return response.Data;
}

// ❌ 不推荐：英文注释（除非必要）
/// <summary>
/// Get prescription by ID
/// </summary>
public async Task<Prescription> GetPrescriptionAsync(int id) { }
```

**权衡场景**：
- 技术术语可以保留英文（如"Repository"、"ViewModel"）
- 第三方库集成代码可以使用英文注释

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）

---

#### P1-8: Emoji禁止用于代码

**原则描述**：代码中禁用Emoji，文档中允许Emoji

**推荐实践**：
```csharp
// ❌ 错误：代码中使用Emoji
public class PrescriptionService
{
    // ❌ 错误
    public void Delete() => Console.WriteLine("❌ 删除失败");
}
```

```markdown
<!-- ✅ 正确：文档中使用Emoji -->
## ✅ 完成标准
- ✅ 编译通过
- ❌ 测试失败
```

**权衡场景**：
- 日志输出到控制台可以使用Emoji（开发环境）
- 用户界面显示的文本可以使用Emoji（需求明确要求）

**参考资料**：
- `CLAUDE.md` - Section 4.2（代码规范）

---

#### P1-9: 避免过度设计（YAGNI）

**原则描述**：够用即好，避免未来可能用不到的功能

**推荐实践**：
- ✅ 仅实现当前明确所需的功能
- ✅ 删除未使用的代码和依赖
- ✅ 避免"将来可能需要"的抽象

**错误示例**：
```csharp
// ❌ 错误：过度设计的Command Handler
public class PrescriptionCommandHandler
{
    // 仅封装1行代码，无业务价值
    public void Delete(int id) => _viewModel.DeleteCommand.Execute(id);
}

// ✅ 正确：直接在ViewModel实现
public class PrescriptionManagementViewModel
{
    public DelegateCommand<int> DeleteCommand { get; }
}
```

**参考资料**：
- `docs/business-rules.md` - 业务规则#12（够用即好）
- [ADR-004: Component设计指南](./decisions/ADR-004-component-design-guidelines.md)

---

#### P1-10: Component设计三原则

**原则描述**：Desktop端Component必须符合设计三原则

**推荐实践**：
1. **跨模块共享优先**：功能被2个及以上模块使用
2. **避免薄封装**：不封装1-2行代码
3. **职责清晰优先**：不与ViewModel职责重叠

**正确示例**：
```csharp
// ✅ 正确：跨模块通知服务
public class NotificationService : INotificationService
{
    public void ShowSuccess(string message) { }
    public void ShowError(string message) { }
}
```

**错误示例**：
```csharp
// ❌ 错误：单模块薄封装
public class PrescriptionDataManager
{
    public ObservableCollection<Prescription> Prescriptions { get; }
    // 与ViewModel职责重叠
}
```

**参考资料**：
- [ADR-004: Component设计指南](./decisions/ADR-004-component-design-guidelines.md)

---

#### P1-11: 并行执行优先

**原则描述**：Issue含多个独立子任务时，优先规划并行执行

**推荐实践**：
```bash
# ✅ 正确：并行执行独立任务
Task1: 修复Bug A（Module 1）
Task2: 修复Bug B（Module 2）
Task3: 修复Bug C（Module 3）

# 可以3个任务并行开发，最后一起提交
```

**权衡场景**：
- 任务之间有依赖关系时必须顺序执行
- 资源冲突（如同一个文件）时需要顺序执行

**参考资料**：
- `.claude/core/RULES.md` - 工具选择优先级

---

#### P1-12: 文件归档规范遵守

**原则描述**：按照文件组织规范归档文档和脚本

**推荐实践**：
- ✅ 文档归档到`docs/`对应分类目录
- ✅ 脚本归档到`scripts/`对应功能目录
- ✅ 输出文件归档到`docs/reports/`或`scripts/analysis/outputs/`
- ❌ 禁止在根目录创建临时文件

**权衡场景**：
- 临时测试文件可以暂时放根目录（但必须及时删除）
- Pre-commit hook会自动检查根目录文件规范

**参考资料**：
- `.claude/core/FILE-ORGANIZATION.md`
- `docs/development/shared/file-organization-guidelines.md`

---

#### P1-13: 文档层级遵守（Level 0-4）

**原则描述**：文档必须按照五级分类体系组织

**推荐实践**：
- **Level 0（导航中心）**：`docs/index.md`
- **Level 1（核心规则）**：`docs/business-rules.md`、`docs/quick-reference/`
- **Level 2（架构指南）**：`docs/architecture/{server|client|shared}/README.md`
- **Level 3（深度参考）**：`docs/modules/`、`docs/deep/`
- **Level 4（设计模式）**：`docs/architecture/patterns/`

**权衡场景**：
- 新类型文档需要在`docs/index.md`中明确分类

**参考资料**：
- `docs/architecture/shared/architecture-documentation-system-proposal.md`
- `docs/index.md`

---

#### P1-14: 验证优先于修复

**原则描述**：对于"问题报告"，先验证真实性再实施修复

**推荐实践**：
```
Step 1: 使用grep/Read/Bash对比契约、配置、依赖关系
Step 2: 生成验证报告
Step 3: 决策：
  - ✅ 问题确认存在 → 创建Issue修复
  - ✅ 问题不存在 → 标记"已验证无需执行"
  - ⚠️ 无法确定 → 标记"条件执行"
```

**工具链**：
```
sequential-thinking（深度分析）
  → grep/Read（对比验证）
  → 生成验证报告
```

**参考资料**：
- `CLAUDE.md` - Section 2.5（任务启动前置检查）
- `.claude/core/PRINCIPLES.md`

---

#### P1-15: MCP工具优先使用

**原则描述**：优先使用MCP第三方工具，提升效率和准确性

**推荐实践**：
```
第1优先级：MCP工具（跨平台、稳定）
  - filesystem: 文件读写
  - serena: 代码语义分析
  - github: GitHub API集成
  - context7: 技术文档查询

第2优先级：Claude Code内置工具
  - Read/Write/Edit
  - Glob/Grep
  - Bash

第3优先级：Shell命令
  - 仅在MCP工具无法满足需求时使用
```

**参考资料**：
- `.claude/core/MCP-TOOLS-ORCHESTRATION.md`
- `docs/development/mcp-tools-reference.md`

---

### Level 2: 指导原则（10条）⭐

#### P2-1: 优先使用Linq而非循环

**原则描述**：Linq表达式更简洁、可读性更高

**推荐实践**：
```csharp
// ✅ 推荐：Linq
var activePrescriptions = prescriptions
    .Where(p => p.Status == PrescriptionStatus.Active)
    .OrderBy(p => p.CreatedAt)
    .ToList();

// ⚠️ 不推荐：传统循环（但不禁止）
var activePrescriptions = new List<Prescription>();
foreach (var p in prescriptions)
{
    if (p.Status == PrescriptionStatus.Active)
        activePrescriptions.Add(p);
}
activePrescriptions.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
```

---

#### P2-2: 使用var关键字

**原则描述**：var简化代码，提高可读性

**推荐实践**：
```csharp
// ✅ 推荐
var prescription = await _api.GetPrescriptionByIdAsync(id);
var prescriptions = new List<Prescription>();

// ⚠️ 不推荐（但不禁止）
Prescription prescription = await _api.GetPrescriptionByIdAsync(id);
List<Prescription> prescriptions = new List<Prescription>();
```

---

#### P2-3: 字符串插值而非拼接

**原则描述**：字符串插值更简洁、可读性更高

**推荐实践**：
```csharp
// ✅ 推荐
var message = $"处方ID: {id}，状态: {status}";

// ⚠️ 不推荐
var message = "处方ID: " + id + "，状态: " + status;
```

---

#### P2-4: null条件运算符

**原则描述**：使用?.和??简化null检查

**推荐实践**：
```csharp
// ✅ 推荐
var name = prescription?.PatientName ?? "未知";

// ⚠️ 不推荐
var name = prescription != null ? prescription.PatientName : "未知";
```

---

#### P2-5: 表达式主体成员

**原则描述**：单行方法使用表达式主体

**推荐实践**：
```csharp
// ✅ 推荐
public int GetTotal() => Items.Sum(i => i.Quantity);

// ⚠️ 不推荐
public int GetTotal()
{
    return Items.Sum(i => i.Quantity);
}
```

---

#### P2-6: 使用集合初始化器

**原则描述**：集合初始化器更简洁

**推荐实践**：
```csharp
// ✅ 推荐
var statuses = new List<string> { "Active", "Pending", "Completed" };

// ⚠️ 不推荐
var statuses = new List<string>();
statuses.Add("Active");
statuses.Add("Pending");
statuses.Add("Completed");
```

---

#### P2-7: 避免嵌套过深

**原则描述**：控制嵌套深度≤3层

**推荐实践**：
```csharp
// ✅ 推荐：早返回
if (prescription == null) return;
if (prescription.Status != PrescriptionStatus.Active) return;

// 处理逻辑

// ⚠️ 不推荐：嵌套过深
if (prescription != null)
{
    if (prescription.Status == PrescriptionStatus.Active)
    {
        // 处理逻辑
    }
}
```

---

#### P2-8: 方法参数不超过4个

**原则描述**：参数过多时使用对象封装

**推荐实践**：
```csharp
// ✅ 推荐：使用DTO
public async Task CreatePrescriptionAsync(CreatePrescriptionDto dto) { }

// ⚠️ 不推荐：参数过多
public async Task CreatePrescriptionAsync(
    int medicalCaseId,
    string patientName,
    DateTime date,
    string diagnosis,
    string treatment) { }
```

---

#### P2-9: 使用命名参数提高可读性

**原则描述**：多个bool参数时使用命名参数

**推荐实践**：
```csharp
// ✅ 推荐
LoadData(includeArchived: true, sortDescending: false);

// ⚠️ 不推荐
LoadData(true, false); // 难以理解
```

---

#### P2-10: 优先使用readonly

**原则描述**：只读字段使用readonly

**推荐实践**：
```csharp
// ✅ 推荐
private readonly IPrescriptionApi _api;

// ⚠️ 不推荐（如果不需要修改）
private IPrescriptionApi _api;
```

---

## 📊 原则分类统计

| 级别 | 数量 | 违反后果 |
|------|------|---------|
| **Level 0（强制）** | 10条 | 必须ADR批准 + 例外清单记录 |
| **Level 1（推荐）** | 15条 | 代码审查说明理由 |
| **Level 2（指导）** | 10条 | 无需说明，建议遵循 |
| **总计** | 35条 | - |

---

## 🔗 相关资源

- **ADR索引**: [docs/architecture/decisions/README.md](./decisions/README.md)
- **架构例外清单**: [docs/architecture/exceptions.md](./exceptions.md)
- **架构文档提案**: [docs/architecture/shared/architecture-documentation-system-proposal.md](./shared/architecture-documentation-system-proposal.md)
- **业务规则文档**: [docs/business-rules.md](../business-rules.md)
- **Constitution**: `.spec-workflow/steering/constitution.md`

---

## 📅 更新日志

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-25 | v1.0 | 初始创建（三级分类体系） | Claude Code |

---

**最后更新**: 2025-10-25
**维护者**: 项目架构团队

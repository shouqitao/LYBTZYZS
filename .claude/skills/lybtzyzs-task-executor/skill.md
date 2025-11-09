---
name: lybtzyzs-task-executor
description: 为LYBTZYZS项目自动执行GitHub Issue任务，提供完整的"读取Issue→构建上下文→执行任务→验证→提交"闭环。深度集成三层架构、Constitution合规检查、代码规范。触发关键词：执行任务、执行Issue、自动实现、implement task、实施Issue、开发Issue
---

# LYBTZYZS 任务执行引擎

## 核心能力

1. **Issue读取与解析**：自动读取GitHub Issue的标题、描述、验收标准、依赖关系
2. **上下文构建**：汇总需求文档、设计文档、架构约束、相关代码文件
3. **任务执行**：生成/修改代码文件，遵循项目规范（命名、DI、异步）
4. **自动验证**：编译检查 + 单元测试 + MVP合规 + 架构合规
5. **智能提交**：自动git commit（标准格式，关联Issue）+ 更新Issue状态
6. **错误处理**：验证失败时自动修复或提示问题
7. **进度追踪**：实时更新任务状态（pending → in_progress → completed）

## 何时使用

- GitHub Issue已创建并明确了验收标准
- 设计文档和架构验证已完成
- 需要快速实现简单到中等复杂度的任务（Repository/Service/Controller/ViewModel）
- 希望自动化重复性编码工作（CRUD、API端点、DTO映射）
- 需要确保代码符合项目规范和Constitution约束

## 工作流程

```
输入：GitHub Issue编号（如 #1601）
  ↓
Step 1: 读取Issue信息
  → mcp__github__issue_read(#1601)
  → 提取标题、描述、验收标准、依赖关系、labels
  ↓
Step 2: 构建执行上下文
  → 读取关联的需求文档（从Issue描述或labels提取）
  → 读取关联的设计文档
  → 提取Constitution约束（.spec-workflow/steering/constitution.md）
  → 分析相关代码文件（mcp__serena__find_symbol）
  ↓
Step 3: 执行任务（生成/修改代码）
  → 根据任务类型选择实现策略
  → Repository: 创建接口+实现，遵循BaseRepository模式
  → Service: 实现业务逻辑，遵循DI规范
  → Controller: 创建API端点，遵循RESTful规范
  → ViewModel: 实现MVVM模式，Command+属性绑定
  → View: 创建XAML UI，数据绑定
  ↓
Step 4: 自动验证
  → dotnet build LYBT.All.sln --no-restore
  → dotnet test（如果有单元测试）
  → lybtzyzs-mvp-compliance（技术黑名单检查）
  → lybtzyzs-arch-compliance（三层架构验证）
  ↓
Step 5: 处理验证结果
  → 如果通过：进入Step 6
  → 如果失败：分析错误 → 自动修复（简单错误）或提示用户
  ↓
Step 6: 自动提交代码
  → git add {修改的文件}
  → git commit -m "feat(module): 实现XXX功能\n\nFixes #1601\n\n- 具体改动1\n- 具体改动2\n\n🤖 Generated with Claude Code"
  → 更新GitHub Issue状态（添加"completed"label）
  ↓
输出：执行报告、提交SHA、验证结果
```

## 输入要求

**必需**：
- GitHub Issue编号（如 `#1601`）
- Issue必须包含清晰的验收标准（Checklist）

**可选**：
- 关联的设计文档路径（如果Issue描述中未指定）
- 执行模式：`auto`（自动提交）或 `manual`（仅生成代码，不提交）
- 验证级别：`full`（完整验证）或 `quick`（仅编译检查）

## 输出格式

### 1. 执行报告（实时输出）

```markdown
🚀 开始执行任务：Issue #1601

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 1/6: 读取Issue信息
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ Issue标题: feat(auth): 实现RefreshToken撤销功能
✓ 任务类型: Feature（Service层）
✓ 验收标准: 4项
✓ 依赖任务: Issue #1599（已完成）
✓ Epic关联: #1861（Token认证安全重构）

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 2/6: 构建执行上下文
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ 读取需求文档: docs/explanation/token-security-requirements-discussion.md
✓ 读取设计文档: docs/explanation/token-security-design.md
✓ 读取Constitution: .spec-workflow/steering/constitution.md
✓ 分析相关文件:
  - src/Server/Modules/LYBT.Module.Auth/Services/IAuthService.cs
  - src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs
  - src/Server/Infrastructure/LYBT.Infrastructure/Data/AppDbContext.cs

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 3/6: 执行任务
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 创建文件:
  ✓ src/Server/Modules/LYBT.Module.Auth/Services/ITokenRevocationService.cs
  ✓ src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs

📝 修改文件:
  ✓ src/Server/Modules/LYBT.Module.Auth/AuthModule.cs（添加DI注册）
  ✓ src/Server/Infrastructure/LYBT.Infrastructure/Data/AppDbContext.cs（添加DbSet）

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 4/6: 自动验证
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ 编译检查: Build succeeded (0 errors, 0 warnings)
✓ 单元测试: All tests passed (5/5)
✓ MVP合规检查: Passed（未使用技术黑名单）
✓ 架构合规检查: Passed（符合三层架构）

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 5/6: 验证通过，准备提交
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ 所有验收标准已满足
✓ 代码符合项目规范
✓ Constitution检查通过

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 6/6: 自动提交代码
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✓ git add（4个文件）
✓ git commit: feat(auth): 实现RefreshToken撤销功能
  Commit SHA: a1b2c3d4
✓ GitHub Issue更新: 添加label "completed"

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ 任务执行完成！
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 执行统计:
- 总耗时: 3分45秒
- 文件创建: 2个
- 文件修改: 2个
- 代码行数: +156 / -0
- Commit SHA: a1b2c3d4
- Issue状态: Open → Completed

💡 下一步建议:
1. 审查代码变更: git diff HEAD~1
2. 运行集成测试（如果有）
3. 关闭Issue #1601（如果验证通过）
```

### 2. 失败报告（如果验证未通过）

```markdown
❌ 任务执行失败：Issue #1601

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Step 4/6: 验证失败
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

❌ 编译错误（2个）:
  1. TokenRevocationService.cs(45,28): error CS0246: The type or namespace name 'RefreshToken' could not be found
  2. AuthModule.cs(23,12): error CS0246: The type or namespace name 'ITokenRevocationService' could not be found

📋 错误分析:
- 原因: 缺少RefreshToken实体类
- 建议: 先完成依赖任务 Issue #1599（创建RefreshToken实体）

🔄 自动修复尝试:
  → 检测到依赖任务未完成
  → 无法自动修复（需要等待Issue #1599完成）

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
💡 下一步操作:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

1. 完成依赖任务: Issue #1599（创建RefreshToken实体）
2. 重新执行本任务: 执行Issue #1601
3. 或手动修复编译错误后再执行验证
```

## 任务执行策略

### 1. 任务类型识别

根据Issue标题/描述/labels自动识别任务类型：

| 任务类型 | 识别关键词 | 实现策略 |
|---------|-----------|---------|
| **Repository** | Repository, 数据访问, 仓储 | 创建接口+实现，继承BaseRepository |
| **Service** | Service, 业务逻辑, 服务 | 创建接口+实现，DI注入Repository |
| **Controller** | Controller, API, 端点 | 创建Controller类，注入Service |
| **ViewModel** | ViewModel, MVVM, 视图模型 | 创建ViewModel类，Command+属性 |
| **View** | View, XAML, UI, 界面 | 创建XAML+Code-behind |
| **DTO** | DTO, 数据传输对象, 模型 | 创建DTO类+AutoMapper配置 |
| **Entity** | Entity, 实体, 数据模型 | 创建Entity类+DbContext配置 |
| **Test** | Test, 测试, 单元测试 | 创建xUnit测试类（AAA模式） |

### 2. Repository层实现策略

**模板**：
```csharp
// 接口定义（Interfaces项目）
public interface I{Module}Repository : IBaseRepository<{Entity}>
{
    Task<List<{Entity}>> Get{Module}ByConditionAsync({Params});
}

// 实现类（Infrastructure项目）
public class {Module}Repository : BaseRepository<{Entity}>, I{Module}Repository
{
    public {Module}Repository(AppDbContext dbContext, ILogger<{Module}Repository> logger)
        : base(dbContext, logger)
    {
    }

    public async Task<List<{Entity}>> Get{Module}ByConditionAsync({Params})
    {
        return await _dbContext.{Entities}
            .Where(x => x.{Condition})
            .ToListAsync();
    }
}
```

**关键要点**：
- 继承BaseRepository<T>
- DI注入：AppDbContext + ILogger
- 异步方法：所有I/O操作必须async/await
- LINQ查询：使用EF Core LINQ
- 日志记录：关键操作记录日志

### 3. Service层实现策略

**模板**：
```csharp
// 接口定义（Interfaces项目）
public interface I{Module}Service
{
    Task<List<{Dto}>> Get{Module}sAsync({Params});
    Task<{Dto}> Create{Module}Async(Create{Module}Request request);
}

// 实现类（Module项目）
public class {Module}Service : I{Module}Service
{
    private readonly I{Module}Repository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<{Module}Service> _logger;

    public {Module}Service(
        I{Module}Repository repository,
        IMapper mapper,
        ILogger<{Module}Service> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<{Dto}>> Get{Module}sAsync({Params})
    {
        var entities = await _repository.Get{Module}ByConditionAsync({Params});
        return _mapper.Map<List<{Dto}>>(entities);
    }

    public async Task<{Dto}> Create{Module}Async(Create{Module}Request request)
    {
        // 业务验证
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // 创建实体
        var entity = _mapper.Map<{Entity}>(request);
        var created = await _repository.CreateAsync(entity);

        _logger.LogInformation("{Module}创建成功，ID: {Id}", created.Id);

        return _mapper.Map<{Dto}>(created);
    }
}
```

**关键要点**：
- DI注入：Repository + Mapper + Logger
- Null检查：所有参数必须null检查
- 业务验证：先验证再执行
- AutoMapper：Entity ↔ DTO转换
- 日志记录：成功/失败都记录

### 4. Controller层实现策略

**模板**：
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class {Module}Controller : ControllerBase
{
    private readonly I{Module}Service _service;
    private readonly ILogger<{Module}Controller> _logger;

    public {Module}Controller(
        I{Module}Service service,
        ILogger<{Module}Controller> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取{Module}列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<{Dto}>>> Get{Module}sAsync([FromQuery] {Params})
    {
        try
        {
            var result = await _service.Get{Module}sAsync({Params});
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取{Module}列表失败");
            return StatusCode(500, "服务器内部错误");
        }
    }

    /// <summary>
    /// 创建{Module}
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<{Dto}>> Create{Module}Async([FromBody] Create{Module}Request request)
    {
        try
        {
            var result = await _service.Create{Module}Async(request);
            return CreatedAtAction(nameof(Get{Module}ById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "参数验证失败");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建{Module}失败");
            return StatusCode(500, "服务器内部错误");
        }
    }
}
```

**关键要点**：
- RESTful规范：GET/POST/PUT/DELETE
- DI注入：Service + Logger
- 异常处理：try-catch捕获异常
- HTTP状态码：200 OK, 201 Created, 400 Bad Request, 500 Internal Error
- XML注释：Swagger文档生成

### 5. ViewModel层实现策略（Client端）

**模板**：
```csharp
public class {Module}ViewModel : BindableBase
{
    private readonly I{Module}ApiRepository _apiRepository;
    private readonly ILogger<{Module}ViewModel> _logger;

    private ObservableCollection<{Dto}> _{modules};
    public ObservableCollection<{Dto}> {Modules}
    {
        get => _{modules};
        set => SetProperty(ref _{modules}, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading}, value);
    }

    public DelegateCommand Load{Modules}Command { get; }

    public {Module}ViewModel(
        I{Module}ApiRepository apiRepository,
        ILogger<{Module}ViewModel> logger)
    {
        _apiRepository = apiRepository ?? throw new ArgumentNullException(nameof(apiRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Load{Modules}Command = new DelegateCommand(async () => await Load{Modules}Async());
    }

    private async Task Load{Modules}Async()
    {
        try
        {
            IsLoading = true;
            var result = await _apiRepository.Get{Modules}Async();
            {Modules} = new ObservableCollection<{Dto}>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载{Module}列表失败");
            // TODO: 显示错误消息
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

**关键要点**：
- 继承BindableBase（Prism）
- DI注入：ApiRepository + Logger
- 属性绑定：使用SetProperty通知
- Command实现：DelegateCommand
- 异步操作：async/await + IsLoading状态
- 异常处理：try-catch-finally

### 6. View层实现策略（Client端）

**模板（XAML）**：
```xaml
<UserControl x:Class="LYBT.Desktop.{Module}.Views.{Module}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:prism="http://prismlibrary.com/">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- 工具栏 -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Button Content="加载"
                    Command="{Binding Load{Modules}Command}"
                    Width="100" Height="30"/>
        </StackPanel>

        <!-- 数据列表 -->
        <DataGrid Grid.Row="1"
                  ItemsSource="{Binding {Modules}}"
                  AutoGenerateColumns="False"
                  Margin="10">
            <DataGrid.Columns>
                <DataGridTextColumn Header="ID" Binding="{Binding Id}" Width="100"/>
                <DataGridTextColumn Header="名称" Binding="{Binding Name}" Width="*"/>
            </DataGrid.Columns>
        </DataGrid>

        <!-- 加载指示器 -->
        <ProgressBar Grid.Row="1"
                     IsIndeterminate="True"
                     Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibilityConverter}}"
                     VerticalAlignment="Top"/>
    </Grid>
</UserControl>
```

**关键要点**：
- Prism命名空间引用
- 数据绑定：Command, ItemsSource, Visibility
- 响应式布局：Grid.RowDefinitions
- 加载指示器：ProgressBar + IsLoading绑定

## 上下文构建策略

### 1. 需求文档提取

**优先级规则**：
1. Issue描述中明确指定（如 `需求文档: docs/explanation/xxx-requirements-discussion.md`）
2. 从Epic关联提取（Epic描述中的需求文档链接）
3. 从labels提取（如 `epic:token-security` → 匹配 `docs/explanation/token-security-*-requirements-discussion.md`）
4. 文件名匹配（Issue标题关键词 → 模糊匹配需求文档）

### 2. 设计文档提取

**优先级规则**：
1. Issue描述中明确指定
2. 从Epic关联提取
3. 从Task文档提取（`docs/tasks/xxx-tasks.md` → `docs/explanation/xxx-design.md`）
4. 文件名匹配

### 3. Constitution约束提取

**固定路径**：`.spec-workflow/steering/constitution.md`

**提取内容**：
- 技术黑名单（MVP阶段禁止的技术）
- 架构约束（三层架构规则）
- 代码规范（命名、DI、异步）

### 4. 相关代码文件分析

**策略**：
1. 从Issue描述中提取文件路径（如 `文件范围: src/Server/.../XXX.cs`）
2. 使用`mcp__serena__find_symbol`查找相关类/接口
3. 分析依赖关系（Repository → Service → Controller）
4. 读取相关文件内容（mcp__serena__read_file）

## 验证策略

### 1. 编译验证（必须通过）

```bash
dotnet build LYBT.All.sln -c Release --no-restore
```

**成功标准**：
- 0 errors
- 0 warnings

### 2. 单元测试验证（如果存在测试）

```bash
dotnet test LYBT.All.sln -c Release --no-build
```

**成功标准**：
- 所有测试通过（100%）

### 3. MVP合规检查（lybtzyzs-mvp-compliance）

**检查内容**：
- 技术黑名单：Redis, RabbitMQ, MediatR, CQRS等
- 过度设计模式：多层抽象接口、过度工厂模式

### 4. 架构合规检查（lybtzyzs-arch-compliance）

**检查内容**：
- 三层架构：依赖方向正确（Repository ← Service ← Controller）
- DDD边界：聚合根边界清晰
- Repository模式：继承BaseRepository

## 自动提交规范

### Commit Message格式

```
{type}({module}): {简短描述}

Fixes #{issue_number}

- 具体改动1
- 具体改动2
- 验证：编译通过、测试通过

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

**Type类型**：
- `feat`: 新功能
- `fix`: Bug修复
- `refactor`: 重构
- `test`: 测试
- `docs`: 文档

### Issue状态更新

**自动操作**：
1. 添加label：`completed`
2. 添加Comment：任务执行报告（包含验证结果、Commit SHA）

**不自动关闭Issue**：由用户或CI/CD流程决定何时关闭

## 错误处理与自动修复

### 1. 编译错误自动修复

**可修复类型**：
- 缺少using语句 → 自动添加
- 拼写错误（常见类型名） → 自动纠正
- 简单的语法错误 → 自动修复

**不可修复类型**：
- 依赖任务未完成（缺少依赖的类）
- 复杂的业务逻辑错误
- 架构设计问题

### 2. 测试失败处理

**策略**：
- 分析失败原因
- 如果是简单断言错误 → 调整代码
- 如果是业务逻辑问题 → 提示用户review

### 3. 合规检查失败处理

**MVP合规失败**：
- 提示违反的Constitution规则
- 建议替代方案
- 不自动修复（需要设计调整）

**架构合规失败**：
- 提示违反的架构规则
- 建议重构方向
- 不自动修复（需要设计调整）

## 技术实现

### 使用的MCP工具链

1. **mcp__github__issue_read**：读取Issue信息
2. **Read**：读取需求/设计文档
3. **Grep**：搜索关键词和模式
4. **mcp__serena__find_symbol**：分析代码结构
5. **mcp__serena__read_file**：读取相关代码文件
6. **mcp__serena__replace_symbol_body**：替换代码（编辑模式）
7. **mcp__serena__insert_after_symbol**：插入代码
8. **mcp__serena__create_text_file**：创建新文件
9. **Write**：创建文档/配置文件
10. **Bash**：执行编译/测试命令
11. **mcp__sequential-thinking**：深度分析（复杂任务）
12. **mcp__github__add_issue_comment**：更新Issue
13. **mcp__github__issue_write**：更新Issue状态

### 实现逻辑

```python
def execute_task(issue_number):
    """
    执行GitHub Issue任务
    """
    # Step 1: 读取Issue信息
    issue = github.issue_read(issue_number)
    task_type = identify_task_type(issue.title, issue.labels)

    # Step 2: 构建上下文
    context = {
        'requirements': extract_requirements_doc(issue),
        'design': extract_design_doc(issue),
        'constitution': read_constitution(),
        'related_files': analyze_related_files(issue, task_type)
    }

    # Step 3: 执行任务
    if task_type == "Repository":
        code_changes = implement_repository(issue, context)
    elif task_type == "Service":
        code_changes = implement_service(issue, context)
    elif task_type == "Controller":
        code_changes = implement_controller(issue, context)
    elif task_type == "ViewModel":
        code_changes = implement_viewmodel(issue, context)
    elif task_type == "View":
        code_changes = implement_view(issue, context)
    else:
        raise ValueError(f"未知任务类型: {task_type}")

    # Step 4: 验证
    validation_result = validate_changes(code_changes)
    if not validation_result.success:
        # 尝试自动修复
        if validation_result.fixable:
            code_changes = auto_fix(code_changes, validation_result.errors)
            validation_result = validate_changes(code_changes)

    if not validation_result.success:
        return failure_report(validation_result)

    # Step 5: 提交
    commit_sha = git_commit(code_changes, issue_number)
    github.add_issue_comment(issue_number, execution_report(validation_result, commit_sha))
    github.issue_write(issue_number, labels=["completed"])

    return success_report(commit_sha, validation_result)

def identify_task_type(title, labels):
    """
    识别任务类型
    """
    keywords = {
        "Repository": ["Repository", "仓储", "数据访问"],
        "Service": ["Service", "业务逻辑", "服务"],
        "Controller": ["Controller", "API", "端点"],
        "ViewModel": ["ViewModel", "MVVM", "视图模型"],
        "View": ["View", "XAML", "UI", "界面"],
    }

    for task_type, kws in keywords.items():
        if any(kw.lower() in title.lower() for kw in kws):
            return task_type

    # 从labels推断
    for label in labels:
        if "repository" in label.lower():
            return "Repository"
        if "service" in label.lower():
            return "Service"
        # ...

    # 默认
    return "Service"

def validate_changes(code_changes):
    """
    验证代码变更
    """
    results = []

    # 1. 编译检查
    build_result = bash("dotnet build LYBT.All.sln -c Release --no-restore")
    results.append(("Build", build_result.success, build_result.output))

    # 2. 测试检查（如果有）
    if has_tests(code_changes):
        test_result = bash("dotnet test LYBT.All.sln -c Release --no-build")
        results.append(("Test", test_result.success, test_result.output))

    # 3. MVP合规检查
    mvp_result = skill("lybtzyzs-mvp-compliance", code_changes)
    results.append(("MVP Compliance", mvp_result.success, mvp_result.output))

    # 4. 架构合规检查
    arch_result = skill("lybtzyzs-arch-compliance", code_changes)
    results.append(("Arch Compliance", arch_result.success, arch_result.output))

    return ValidationResult(results)
```

## 示例

### 示例1：执行Repository任务

**输入**：
```
"执行任务: Issue #1601"
或
"实施Issue #1601"
或
"开发#1601"
```

**Issue #1601内容**：
```markdown
## 标题
feat(auth): 创建RefreshTokenRepository

## 描述
创建RefreshToken的Repository接口和实现，用于管理RefreshToken的持久化操作。

**需求文档**: docs/explanation/token-security-requirements-discussion.md
**设计文档**: docs/explanation/token-security-design.md

## 验收标准
- [ ] 创建IRefreshTokenRepository接口（src/Server/Interfaces/...）
- [ ] 创建RefreshTokenRepository实现（src/Server/Infrastructure/...）
- [ ] 继承BaseRepository<RefreshToken>
- [ ] 实现GetByTokenAsync方法
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 单元测试通过（Mock DbContext）

## 文件范围
- src/Server/Interfaces/LYBT.Server.Interfaces/Repositories/IRefreshTokenRepository.cs
- src/Server/Infrastructure/LYBT.Infrastructure/Repositories/RefreshTokenRepository.cs

## 技术要点
- 异步I/O操作
- 使用EF Core LINQ查询
- 日志记录关键操作
```

**Skill执行过程**：
```
🚀 开始执行任务：Issue #1601

Step 1/6: 读取Issue信息
✓ Issue标题: feat(auth): 创建RefreshTokenRepository
✓ 任务类型: Repository
✓ 验收标准: 6项
✓ 文件范围: 2个文件

Step 2/6: 构建执行上下文
✓ 读取需求文档: docs/explanation/token-security-requirements-discussion.md
✓ 读取设计文档: docs/explanation/token-security-design.md
✓ 读取Constitution: .spec-workflow/steering/constitution.md
✓ 分析RefreshToken Entity定义

Step 3/6: 执行任务
✓ 创建IRefreshTokenRepository.cs
✓ 创建RefreshTokenRepository.cs
✓ 实现GetByTokenAsync方法
✓ 配置DI注册（如果需要）

Step 4/6: 自动验证
✓ 编译检查: Build succeeded
✓ MVP合规检查: Passed
✓ 架构合规检查: Passed

Step 5/6: 验证通过
✓ 所有验收标准已满足

Step 6/6: 自动提交代码
✓ git commit: feat(auth): 创建RefreshTokenRepository
✓ Commit SHA: a1b2c3d4
✓ GitHub Issue更新: 添加label "completed"

✅ 任务执行完成！
```

**生成的代码**：

`IRefreshTokenRepository.cs`:
```csharp
using LYBT.Shared.Models.Entities.Auth;

namespace LYBT.Server.Interfaces.Repositories
{
    /// <summary>
    /// RefreshToken仓储接口
    /// </summary>
    public interface IRefreshTokenRepository : IBaseRepository<RefreshToken>
    {
        /// <summary>
        /// 根据Token值获取RefreshToken
        /// </summary>
        /// <param name="token">Token值</param>
        /// <returns>RefreshToken实体，如果不存在则返回null</returns>
        Task<RefreshToken?> GetByTokenAsync(string token);
    }
}
```

`RefreshTokenRepository.cs`:
```csharp
using LYBT.Infrastructure.Data;
using LYBT.Server.Interfaces.Repositories;
using LYBT.Shared.Models.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Repositories
{
    /// <summary>
    /// RefreshToken仓储实现
    /// </summary>
    public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(
            AppDbContext dbContext,
            ILogger<RefreshTokenRepository> logger)
            : base(dbContext, logger)
        {
        }

        /// <summary>
        /// 根据Token值获取RefreshToken
        /// </summary>
        /// <param name="token">Token值</param>
        /// <returns>RefreshToken实体，如果不存在则返回null</returns>
        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("GetByTokenAsync调用时token参数为空");
                return null;
            }

            try
            {
                return await _dbContext.RefreshTokens
                    .FirstOrDefaultAsync(t => t.Token == token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据Token获取RefreshToken失败: {Token}", token);
                throw;
            }
        }
    }
}
```

### 示例2：执行Service任务

**输入**：
```
"执行Issue #1602"
```

**Issue #1602内容**：
```markdown
## 标题
feat(auth): 实现TokenRevocationService业务逻辑

## 描述
实现Token撤销服务，提供撤销单个Token和撤销用户所有Token的功能。

**依赖**: Issue #1601（已完成）
**设计文档**: docs/explanation/token-security-design.md

## 验收标准
- [ ] 创建ITokenRevocationService接口
- [ ] 创建TokenRevocationService实现
- [ ] 实现RevokeTokenAsync方法
- [ ] 实现RevokeAllUserTokensAsync方法
- [ ] 编译通过：0 errors, 0 warnings
- [ ] 单元测试通过（Mock Repository）

## 文件范围
- src/Server/Modules/LYBT.Module.Auth/Services/ITokenRevocationService.cs
- src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs
- src/Server/Modules/LYBT.Module.Auth/AuthModule.cs（DI注册）
```

**Skill执行过程**：
```
🚀 开始执行任务：Issue #1602

Step 1/6: 读取Issue信息
✓ 任务类型: Service
✓ 依赖检查: Issue #1601已完成

Step 2/6: 构建执行上下文
✓ 读取设计文档
✓ 分析IRefreshTokenRepository接口（来自Issue #1601）

Step 3/6: 执行任务
✓ 创建ITokenRevocationService.cs
✓ 创建TokenRevocationService.cs
✓ 更新AuthModule.cs（添加DI注册）

Step 4/6: 自动验证
✓ 编译检查: Build succeeded
✓ MVP合规检查: Passed
✓ 架构合规检查: Passed

Step 6/6: 自动提交
✓ Commit SHA: b2c3d4e5

✅ 任务执行完成！
```

## 限制条件

1. **依赖Issue质量**：Issue必须包含清晰的验收标准、文件范围、技术要点
2. **任务复杂度限制**：适合简单到中等复杂度的任务（<200行代码），复杂任务建议手动实现
3. **需要设计文档**：最佳实践是基于设计文档执行，缺少设计文档可能导致实现偏差
4. **不执行重构**：不处理大规模代码重构，仅实现新功能或修复Bug
5. **不处理UI设计**：View层（XAML）仅实现基本结构，不处理复杂UI设计
6. **依赖验证环境**：需要本地dotnet环境、数据库连接、测试环境可用

## 最佳实践

1. **先创建Issue**：确保Issue包含完整的验收标准和文件范围
2. **完善设计文档**：复杂功能先完成设计文档，再执行任务
3. **检查依赖任务**：确保依赖的Issue已完成（如Repository → Service）
4. **分解大任务**：大于4小时的任务拆分成更小的子任务
5. **Review生成代码**：自动生成的代码需要人工review，确保业务逻辑正确
6. **手动测试**：自动验证只是基础检查，需要手动测试业务逻辑
7. **渐进式执行**：先执行简单任务（Repository/DTO），再执行复杂任务（Service/Controller）

## 与其他Skill的协同

### Skill工作流

```
设计阶段：lybtzyzs-design-arch-validator
  ↓ 设计文档验证通过
任务分解：lybtzyzs-task-breakdown
  ↓ 生成task文档
Issue创建：lybtzyzs-issue-template
  ↓ 批量生成GitHub Issues
任务执行：lybtzyzs-task-executor（本Skill）
  ↓ 自动实现Issue
状态追踪：lybtzyzs-task-tracker
  ↓ 更新任务状态
任务反思：lybtzyzs-task-reflector
  ↓ 生成改进建议
```

## 性能指标

- Issue读取: <2秒
- 上下文构建: <5秒
- 代码生成: <10秒（简单任务），<30秒（复杂任务）
- 编译验证: <30秒
- 完整流程: <1分钟（简单任务），<3分钟（复杂任务）

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-11-07 | 初始版本，支持Repository/Service/Controller/ViewModel/View任务执行 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-11-07

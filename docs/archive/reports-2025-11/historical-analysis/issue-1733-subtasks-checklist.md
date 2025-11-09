# Issue #1733 - WebAPI MVP合规优化详细子任务清单

**Issue**: #1733
**创建时间**: 2025-10-31
**预期工期**: Phase 1 (1-2天) + Phase 2 (3-5天) + Phase 3 (1天)
**预期收益**: 代码减少~690行（19.7%），端点减少15个（17.4%）

---

## 📋 Phase 1: 高优先级简化（1-2天）

### Task 1.1: 简化HealthController ✅

**目标**: 移除环境分支逻辑，保留核心健康检查

**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs`

#### 子任务清单:

- [ ] **1.1.1 移除环境判断逻辑**
  - **位置**: `GetHealth()` 方法
  - **操作**: 删除 `if (_environment.IsProduction())` 分支逻辑
  - **保留**: 仅保留数据库连接检查
  - **删除代码**:
    ```csharp
    // 删除整个环境分支
    if (_environment.IsProduction())
    {
        var dbCheck = await CheckDatabase();
        checks.Add(new HealthCheck("system", "System Health")
        {
            Status = dbCheck.Status,
            Duration = dbCheck.Duration
        });
    }
    else
    {
        checks.Add(await CheckAppInfo());
        checks.Add(await CheckDatabase());
        checks.Add(CheckExternalDependencies());
        checks.Add(await CheckSeedData());
    }
    ```
  - **简化后代码**:
    ```csharp
    // 简化为单一检查
    checks.Add(await CheckDatabase());
    ```

- [ ] **1.1.2 删除冗余检查方法**
  - **删除方法**:
    - `CheckAppInfo()` - 应用信息检查
    - `CheckExternalDependencies()` - 外部依赖检查
    - `CheckSeedData()` - 种子数据检查
  - **保留方法**:
    - `CheckDatabase()` - 数据库连接检查

- [ ] **1.1.3 简化响应结构**
  - **位置**: `HealthCheck` 类
  - **操作**: 最小化生产环境响应信息
  - **移除字段**: `SubChecks`, `Details`（敏感信息）
  - **保留字段**: `Name`, `Status`, `Duration`

- [ ] **1.1.4 保留核心端点**
  - **保留**: `GET /health` (健康检查)
  - **保留**: `GET /ping` (快速探活)
  - **删除**: 其他冗余端点（如果有）

- [ ] **1.1.5 验证与测试**
  - **测试命令**:
    ```bash
    # 启动WebAPI
    dotnet run --project src/Server/Services/LYBT.WebAPI

    # 测试健康检查
    curl http://localhost:5000/api/health

    # 测试快速探活
    curl http://localhost:5000/api/ping
    ```
  - **验收标准**:
    - ✅ `/health` 返回数据库连接状态
    - ✅ `/ping` 返回快速响应（< 10ms）
    - ✅ 无环境分支逻辑
    - ✅ 编译无警告

**预期成果**: 代码减少~200行

---

### Task 1.2: 移除PerformanceController ✅

**目标**: 完全移除自建APM系统，推荐使用Application Insights

**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/PerformanceController.cs`

#### 子任务清单:

- [ ] **1.2.1 删除Controller文件**
  - **操作**: 删除整个文件
  - **命令**:
    ```bash
    rm src/Server/Services/LYBT.WebAPI/Controllers/PerformanceController.cs
    ```

- [ ] **1.2.2 删除相关服务接口**
  - **检查位置**: `src/Server/Services/LYBT.WebAPI/Services/` 或 Module层
  - **查找命令**:
    ```bash
    grep -r "IPerformanceService" src/Server/Services/LYBT.WebAPI/
    grep -r "PerformanceMetric" src/Server/Services/LYBT.WebAPI/
    ```
  - **删除文件**（如果存在）:
    - `IPerformanceService.cs`
    - `PerformanceService.cs`
    - `PerformanceMetric.cs`

- [ ] **1.2.3 清理依赖注入注册**
  - **位置**: `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs`
  - **查找并删除**:
    ```csharp
    // 删除性能服务注册
    services.AddSingleton<IPerformanceService, PerformanceService>();
    ```

- [ ] **1.2.4 更新文档说明**
  - **文件**: `docs/how-to-guides/server/webapi-deployment.md`
  - **添加说明**:
    ```markdown
    ## 性能监控

    推荐使用 Application Insights 进行生产环境监控：
    - 安装包: `Microsoft.ApplicationInsights.AspNetCore`
    - 配置: `appsettings.json` 中添加 `InstrumentationKey`
    - 文档: https://learn.microsoft.com/zh-cn/azure/azure-monitor/app/asp-net-core
    ```

- [ ] **1.2.5 验证与测试**
  - **测试命令**:
    ```bash
    # 编译整个解决方案
    dotnet build LYBT.All.sln -c Release

    # 检查是否还有引用
    grep -r "PerformanceController" src/
    ```
  - **验收标准**:
    - ✅ 编译无错误
    - ✅ 无残留引用
    - ✅ 文档已更新

**预期成果**: 代码减少~250行，端点减少6个

---

### Task 1.3: 简化CacheHealthController ✅

**目标**: 保留核心缓存管理功能，移除复杂诊断系统

**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/CacheHealthController.cs`

#### 子任务清单:

- [ ] **1.3.1 删除诊断端点**
  - **删除端点**:
    ```csharp
    [HttpPost("diagnose")]  // 运行缓存诊断
    [HttpGet("history")]    // 获取历史快照
    ```
  - **操作**: 删除完整方法及相关私有方法

- [ ] **1.3.2 保留核心端点**
  - **保留**: `DELETE /cache/clear` (清空缓存)
  - **保留**: `GET /cache/statistics` (获取缓存统计)
  - **简化逻辑**: 移除复杂的诊断算法

- [ ] **1.3.3 删除诊断相关类**
  - **查找位置**: `CacheHealthController.cs` 内部类或单独文件
  - **删除类**:
    - `CacheDiagnosticResult`
    - `CacheSnapshot`
    - `CacheDiagnosticAnalyzer`（如果存在）

- [ ] **1.3.4 简化统计方法**
  - **位置**: `GetStatistics()` 方法
  - **保留字段**:
    - `TotalKeys` (总键数)
    - `MemoryUsage` (内存使用)
    - `HitRate` (命中率，如果简单)
  - **删除字段**:
    - 详细键值分析
    - 历史趋势数据
    - 复杂的性能指标

- [ ] **1.3.5 验证与测试**
  - **测试命令**:
    ```bash
    # 启动WebAPI
    dotnet run --project src/Server/Services/LYBT.WebAPI

    # 测试缓存统计
    curl http://localhost:5000/api/cache/statistics

    # 测试缓存清空
    curl -X DELETE http://localhost:5000/api/cache/clear
    ```
  - **验收标准**:
    - ✅ `/cache/statistics` 返回基础统计
    - ✅ `/cache/clear` 成功清空缓存
    - ✅ 无诊断端点
    - ✅ 编译无警告

**预期成果**: 代码减少~150行，端点减少2个

---

### Task 1.4: 合并AuthController重复端点 ✅

**目标**: 消除重复端点，统一认证逻辑

**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/AuthController.cs`

#### 子任务清单:

- [ ] **1.4.1 合并登录端点**
  - **现状**:
    ```csharp
    [HttpPost("login")]              // 普通登录
    [HttpPost("admin/login")]        // 超级管理员登录
    ```
  - **操作**: 合并为单一端点
  - **修改后**:
    ```csharp
    /// <summary>
    /// 统一登录端点 - 支持普通用户和管理员
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request)
    {
        // 统一登录逻辑
        var result = await _authService.LoginAsync(request);

        // 根据角色返回不同权限的Token
        return HandleServiceResult(result, "登录成功");
    }
    ```
  - **删除**: `[HttpPost("admin/login")]` 端点

- [ ] **1.4.2 合并验证端点**
  - **现状**:
    ```csharp
    [HttpGet("validate")]    // 从Header验证
    [HttpPost("validate")]   // 从Body验证
    ```
  - **操作**: 保留GET，删除POST
  - **原因**: 验证应从Authorization Header读取Token
  - **修改后**:
    ```csharp
    /// <summary>
    /// 验证Token有效性 - 从Authorization Header读取
    /// </summary>
    [HttpGet("validate")]
    public async Task<ActionResult<ApiResponse<TokenValidationResponse>>> Validate()
    {
        // 从Header读取Token
        var token = HttpContext.Request.Headers["Authorization"]
            .ToString().Replace("Bearer ", "");

        var result = await _authService.ValidateTokenAsync(token);
        return HandleServiceResult(result, "Token验证成功");
    }
    ```
  - **删除**: `[HttpPost("validate")]` 端点

- [ ] **1.4.3 更新认证服务逻辑**
  - **位置**: `src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs`
  - **修改**: 统一登录方法，根据角色生成不同权限的Token
  - **示例**:
    ```csharp
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        // 验证用户身份
        var user = await _userRepository.GetByUsernameAsync(request.Username);

        // 验证密码
        if (!VerifyPassword(request.Password, user.PasswordHash))
            return ServiceResult<LoginResponse>.Failure("用户名或密码错误");

        // 根据角色生成Token（包含不同Claims）
        var token = GenerateToken(user);

        return ServiceResult<LoginResponse>.Success(new LoginResponse
        {
            Token = token,
            User = _mapper.Map<UserDto>(user)
        });
    }
    ```

- [ ] **1.4.4 更新API文档**
  - **文件**: `docs/reference/api/auth-api.md`（如果存在）
  - **更新内容**:
    - 移除 `/admin/login` 说明
    - 移除 `POST /validate` 说明
    - 更新 `/login` 说明（支持所有角色）

- [ ] **1.4.5 验证与测试**
  - **测试命令**:
    ```bash
    # 测试普通用户登录
    curl -X POST http://localhost:5000/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"username":"user","password":"password"}'

    # 测试管理员登录
    curl -X POST http://localhost:5000/api/auth/login \
      -H "Content-Type: application/json" \
      -d '{"username":"admin","password":"admin123"}'

    # 测试Token验证
    curl http://localhost:5000/api/auth/validate \
      -H "Authorization: Bearer <token>"
    ```
  - **验收标准**:
    - ✅ 普通用户登录成功，Token包含普通权限
    - ✅ 管理员登录成功，Token包含管理员权限
    - ✅ Token验证从Header读取
    - ✅ 无重复端点

**预期成果**: 代码减少~50行，端点减少2个

---

### Task 1.5: 移除克隆端点 ✅

**目标**: 移除Server端克隆端点，由Desktop层实现

**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`

#### 子任务清单:

- [ ] **1.5.1 删除克隆端点**
  - **位置**: `FormulasController.cs` 第338-371行
  - **删除端点**:
    ```csharp
    /// <summary>
    /// 克隆验方 - 复制验方作为新验方 (Issue #1167)
    /// </summary>
    [HttpPost("{id}/copy")]
    public async Task<ActionResult<ApiResponse<FormulaDto>>> CopyFormula(Guid id)
    {
        // ... 删除整个方法
    }
    ```

- [ ] **1.5.2 删除Service层克隆方法**
  - **位置**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
  - **查找方法**: `CloneFormulaAsync(Guid id)`
  - **操作**: 删除完整方法实现

- [ ] **1.5.3 删除接口定义**
  - **位置**: `src/Server/Modules/LYBT.Module.Formula/Interfaces/IFormulaService.cs`
  - **删除声明**:
    ```csharp
    Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid id);
    ```

- [ ] **1.5.4 更新Desktop层实现说明**
  - **文件**: `docs/how-to-guides/client/formula-management.md`（如果存在）
  - **添加说明**:
    ```markdown
    ## 克隆验方功能

    克隆功能由Desktop层实现，流程如下：

    ### ViewModel实现
    ```csharp
    // 位置: src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaListViewModel.cs

    public async Task CloneFormulaAsync(Guid formulaId)
    {
        // 1. 查询原验方
        var original = await _formulaService.GetByIdAsync(formulaId);
        if (!original.IsSuccess || original.Data == null)
            return;

        // 2. 构建克隆对象
        var clone = new FormulaCreateDto
        {
            Name = $"{original.Data.Name} - 副本",
            Category = original.Data.Category,
            Description = original.Data.Description,
            Usage = original.Data.Usage,
            Herbs = original.Data.Herbs.Select(h => new FormulaHerbCreateDto
            {
                HerbId = h.HerbId,
                HerbName = h.HerbName,
                Dosage = h.Dosage,
                Unit = h.Unit
            }).ToList()
        };

        // 3. 创建新验方
        var result = await _formulaService.CreateAsync(clone);
        if (result.IsSuccess)
        {
            MessageBox.Show("验方克隆成功");
            await LoadFormulasAsync(); // 刷新列表
        }
    }
    ```
    ```

- [ ] **1.5.5 验证与测试**
  - **测试命令**:
    ```bash
    # 编译检查
    dotnet build LYBT.All.sln -c Release

    # 检查是否还有引用
    grep -r "CloneFormula" src/Server/
    grep -r "/copy" src/Server/
    ```
  - **验收标准**:
    - ✅ Controller无克隆端点
    - ✅ Service无克隆方法
    - ✅ 编译无错误
    - ✅ 文档已更新Desktop实现说明

**预期成果**: 代码减少~40行，端点减少1个

---

## 📋 Phase 2: 中优先级优化（3-5天）

### Task 2.1: 统一缓存策略 ✅

**目标**: 移除旧的ResponseCache，统一使用OutputCache

**背景**: Issue #1732已配置OutputCache基础设施，现在需要清理旧缓存标记

#### 子任务清单:

- [ ] **2.1.1 扫描所有Controller**
  - **查找命令**:
    ```bash
    grep -r "ResponseCache" src/Server/Services/LYBT.WebAPI/Controllers/
    ```
  - **记录位置**: 列出所有使用 `[ResponseCache]` 的方法

- [ ] **2.1.2 替换为OutputCache**
  - **示例位置**: `FormulasController.cs` 第38行
  - **删除**:
    ```csharp
    [ResponseCache(Duration = 7200, Location = ResponseCacheLocation.Any)]
    ```
  - **确认保留**:
    ```csharp
    [OutputCache(PolicyName = "FormulasCache")]
    ```
  - **注意**: 如果方法已有 `[OutputCache]`，只需删除 `[ResponseCache]`

- [ ] **2.1.3 检查缓存策略配置**
  - **位置**: `src/Server/Services/LYBT.WebAPI/Extensions/DatabaseServiceCollectionExtensions.cs`
  - **确认存在**:
    ```csharp
    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("FormulasCache", builder =>
            builder.Expire(TimeSpan.FromHours(2)));

        options.AddPolicy("HerbsCache", builder =>
            builder.Expire(TimeSpan.FromHours(2)));

        // ... 其他策略
    });
    ```

- [ ] **2.1.4 添加缺失的策略**
  - **检查**: 是否所有使用缓存的Controller都有对应策略
  - **添加**: 为缺失的Controller添加策略（如需要）

- [ ] **2.1.5 验证与测试**
  - **测试命令**:
    ```bash
    # 编译检查
    dotnet build LYBT.All.sln -c Release

    # 启动WebAPI
    dotnet run --project src/Server/Services/LYBT.WebAPI

    # 测试缓存（第二次请求应命中缓存）
    curl -w "@curl-format.txt" http://localhost:5000/api/v1/formulas
    curl -w "@curl-format.txt" http://localhost:5000/api/v1/formulas
    ```
  - **验收标准**:
    - ✅ 无 `[ResponseCache]` 标记
    - ✅ 所有缓存使用 `[OutputCache]`
    - ✅ 第二次请求响应时间显著降低
    - ✅ 编译无警告

**预期成果**: 统一缓存策略，提升可维护性

---

### Task 2.2: 优化药材验证流程（方案C） ✅

**目标**: 实现自动模糊匹配，简化手动验证UI

**背景**: 现有流程需要手动逐个匹配药材，方案C通过自动匹配简化80%场景

#### 子任务清单:

- [ ] **2.2.1 实现模糊匹配算法**
  - **位置**: `src/Server/Modules/LYBT.Module.Formula/Services/FormulaService.cs`
  - **新增方法**:
    ```csharp
    /// <summary>
    /// 自动匹配药材 - 使用模糊匹配算法
    /// </summary>
    private async Task<Guid?> AutoMatchHerbAsync(string herbName)
    {
        // 1. 精确匹配
        var exactMatch = await _herbRepository.GetByNameAsync(herbName);
        if (exactMatch != null)
            return exactMatch.Id;

        // 2. 模糊匹配（Levenshtein距离）
        var allHerbs = await _herbRepository.GetAllAsync();
        var bestMatch = allHerbs
            .Select(h => new
            {
                Herb = h,
                Similarity = CalculateSimilarity(herbName, h.Name)
            })
            .Where(x => x.Similarity >= 0.8) // 相似度阈值80%
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();

        return bestMatch?.Herb.Id;
    }

    /// <summary>
    /// 计算字符串相似度（Levenshtein距离）
    /// </summary>
    private double CalculateSimilarity(string source, string target)
    {
        int distance = LevenshteinDistance(source, target);
        int maxLength = Math.Max(source.Length, target.Length);
        return 1.0 - (double)distance / maxLength;
    }

    /// <summary>
    /// Levenshtein距离算法
    /// </summary>
    private int LevenshteinDistance(string source, string target)
    {
        int[,] d = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            d[i, 0] = i;

        for (int j = 0; j <= target.Length; j++)
            d[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[source.Length, target.Length];
    }
    ```

- [ ] **2.2.2 修改导入方法自动匹配**
  - **位置**: `FormulaService.ImportFromExcelAsync()` 方法
  - **修改逻辑**:
    ```csharp
    // 原逻辑: 所有药材标记为未验证
    herbItem.IsValidated = false;
    herbItem.HerbId = null;

    // 新逻辑: 尝试自动匹配
    var matchedHerbId = await AutoMatchHerbAsync(herbItem.OriginalHerbName);
    if (matchedHerbId.HasValue)
    {
        // ✅ 自动匹配成功
        herbItem.HerbId = matchedHerbId.Value;
        herbItem.IsValidated = true;
        successCount++;
    }
    else
    {
        // ❌ 自动匹配失败，需手动验证
        herbItem.IsValidated = false;
        herbItem.HerbId = null;
        pendingCount++;
    }
    ```

- [ ] **2.2.3 优化导入结果响应**
  - **位置**: `FormulaImportResultDto` 类
  - **添加字段**:
    ```csharp
    public class FormulaImportResultDto
    {
        // 原有字段
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public List<string> ErrorMessages { get; set; }

        // 新增字段
        public int AutoMatchedCount { get; set; }      // 自动匹配成功数
        public int PendingValidationCount { get; set; } // 待手动验证数
        public List<PendingHerbDto> PendingHerbs { get; set; } // 待验证药材列表
    }

    public class PendingHerbDto
    {
        public Guid FormulaId { get; set; }
        public Guid HerbItemId { get; set; }
        public string OriginalName { get; set; }
        public List<HerbSuggestionDto> Suggestions { get; set; } // 推荐匹配项
    }
    ```

- [ ] **2.2.4 简化验证端点**
  - **位置**: `FormulasController.cs`
  - **简化**: 保留 `POST /formulas/{formulaId}/herbs/{herbItemId}/validate`
  - **移除**: `GET /formulas/pending-validation`（改为导入时直接返回）

- [ ] **2.2.5 更新Desktop层UI**
  - **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Views/FormulaImportView.xaml`
  - **修改**: 导入完成后显示待验证列表（如果有）
  - **流程**:
    1. 用户点击"导入"
    2. Server返回结果：80个自动匹配，20个待验证
    3. UI显示：成功80个，待验证20个（显示推荐列表）
    4. 用户点击推荐项或搜索选择
    5. 调用 `/validate` 端点完成验证

- [ ] **2.2.6 验证与测试**
  - **测试场景1**: 完全匹配
    ```bash
    # Excel数据: "人参", "黄芪", "甘草"
    # 预期: 100%自动匹配成功
    ```
  - **测试场景2**: 别名匹配
    ```bash
    # Excel数据: "党参"（别名）
    # 预期: 自动匹配到"人参"（相似度>80%）
    ```
  - **测试场景3**: 无法匹配
    ```bash
    # Excel数据: "未知药材X"
    # 预期: 返回待验证列表 + 推荐项
    ```
  - **验收标准**:
    - ✅ 自动匹配率 ≥ 80%
    - ✅ 匹配准确率 100%（无误匹配）
    - ✅ 待验证药材有推荐列表
    - ✅ UI简化，无需逐个查看

**预期成果**: 药材验证效率提升5倍，用户体验显著改善

---

### Task 2.3: 评估辅助端点必要性 ✅

**目标**: 评估 `/can-edit` 等辅助端点是否符合RESTful设计

**背景**: MedicalCaseController有 `GET /{id}/can-edit` 端点，需评估是否保留

#### 子任务清单:

- [ ] **2.3.1 扫描所有辅助端点**
  - **查找命令**:
    ```bash
    grep -r "can-" src/Server/Services/LYBT.WebAPI/Controllers/
    grep -r "/is-" src/Server/Services/LYBT.WebAPI/Controllers/
    grep -r "/has-" src/Server/Services/LYBT.WebAPI/Controllers/
    ```
  - **记录**: 列出所有辅助判断端点

- [ ] **2.3.2 评估每个端点**
  - **评估维度**:
    1. **业务必要性**: 是否必须由Server判断？
    2. **性能影响**: 额外网络请求开销
    3. **RESTful契合度**: 是否违反资源导向设计？
    4. **替代方案**: 能否在主查询中返回？

  - **示例评估**: `GET /medical-cases/{id}/can-edit`
    ```markdown
    **业务必要性**: 中（需检查状态和权限）
    **性能影响**: 高（每次编辑前额外请求）
    **RESTful契合度**: 低（辅助判断，非资源操作）
    **替代方案**: ✅ 在 `GET /medical-cases/{id}` 响应中添加 `CanEdit` 字段
    ```

- [ ] **2.3.3 实施优化方案**
  - **方案A**: 合并到主查询
    ```csharp
    // MedicalCaseDto 添加字段
    public class MedicalCaseDto
    {
        // 原有字段...

        // 新增辅助字段
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanCreatePrescription { get; set; }
    }

    // Service层计算
    var dto = _mapper.Map<MedicalCaseDto>(entity);
    dto.CanEdit = await _medicalCaseRules.CanEditAsync(entity.Id, currentUserId);
    dto.CanDelete = await _medicalCaseRules.CanDeleteAsync(entity.Id, currentUserId);
    ```

  - **方案B**: 保留但改为批量端点
    ```csharp
    // 批量查询多个病案的可编辑性
    [HttpPost("batch-check-edit")]
    public async Task<ActionResult<ApiResponse<Dictionary<Guid, bool>>>> BatchCheckEdit(
        [FromBody] List<Guid> ids)
    {
        var results = new Dictionary<Guid, bool>();
        foreach (var id in ids)
        {
            results[id] = await _medicalCaseRules.CanEditAsync(id, currentUserId);
        }
        return Success(results);
    }
    ```

- [ ] **2.3.4 更新Desktop层调用**
  - **修改**: Desktop层从主查询响应中读取 `CanEdit` 字段
  - **删除**: 独立的 `CanEdit` API调用

- [ ] **2.3.5 验证与测试**
  - **测试命令**:
    ```bash
    # 测试主查询包含辅助字段
    curl http://localhost:5000/api/v1/medical-cases/{id}

    # 预期响应包含:
    # {
    #   "id": "...",
    #   "canEdit": true,
    #   "canDelete": false,
    #   ...
    # }
    ```
  - **验收标准**:
    - ✅ 主查询返回辅助判断字段
    - ✅ 无独立辅助端点（或改为批量）
    - ✅ Desktop层无额外API调用
    - ✅ 性能提升（减少网络请求）

**预期成果**: 端点减少~2个，网络请求减少30%

---

## 📋 Phase 3: 文档同步（1天）

### Task 3.1: 更新API文档 ✅

**目标**: 同步API文档至最新状态

#### 子任务清单:

- [ ] **3.1.1 生成新的API概览**
  - **命令**:
    ```bash
    # 统计Controller数量和端点数量
    find src/Server/Services/LYBT.WebAPI/Controllers -name "*.cs" | wc -l

    # 统计总代码行数
    find src/Server/Services/LYBT.WebAPI/Controllers -name "*.cs" -exec wc -l {} + | tail -1
    ```
  - **更新文件**: `docs/reference/api/README.md`（如果存在）

- [ ] **3.1.2 更新已删除端点的文档**
  - **删除章节**:
    - PerformanceController 所有端点
    - CacheHealthController 诊断端点
    - FormulasController 克隆端点
    - AuthController 重复端点

- [ ] **3.1.3 更新已修改端点的文档**
  - **更新**: FormulasController 导入端点（新增自动匹配说明）
  - **更新**: MedicalCaseController 查询端点（新增辅助字段）

- [ ] **3.1.4 添加迁移说明**
  - **文件**: `docs/reference/api/migration-guide.md`（新建）
  - **内容**:
    ```markdown
    # API变更迁移指南

    ## Issue #1733 变更（2025-10-31）

    ### 已移除端点

    #### PerformanceController（完全移除）
    - `GET /api/performance/metrics` → 使用 Application Insights
    - `GET /api/performance/statistics` → 使用 Application Insights
    - ... 其他端点

    #### CacheHealthController（部分移除）
    - `POST /api/cache/diagnose` → 已移除
    - `GET /api/cache/history` → 已移除

    #### FormulasController
    - `POST /api/formulas/{id}/copy` → Desktop层实现
      ```csharp
      // 迁移代码
      var original = await GetByIdAsync(id);
      var clone = MapToCreateDto(original);
      await CreateAsync(clone);
      ```

    #### AuthController
    - `POST /api/auth/admin/login` → 使用 `POST /api/auth/login`
    - `POST /api/auth/validate` → 使用 `GET /api/auth/validate`

    ### 已优化端点

    #### FormulasController导入
    - `POST /api/formulas/import` 新增自动匹配功能
    - 响应新增字段: `AutoMatchedCount`, `PendingValidationCount`

    #### MedicalCaseController查询
    - `GET /api/medical-cases/{id}` 新增字段: `CanEdit`, `CanDelete`
    ```

**预期成果**: API文档与代码100%同步

---

### Task 3.2: 更新架构文档 ✅

**目标**: 同步架构文档至最新设计

#### 子任务清单:

- [ ] **3.2.1 更新Server端架构文档**
  - **文件**: `docs/explanation/architecture/server/README.md`
  - **更新内容**:
    - 移除 PerformanceController 说明
    - 更新 HealthController 简化说明
    - 更新 CacheHealthController 简化说明

- [ ] **3.2.2 更新验方模块设计文档**
  - **文件**: `docs/explanation/architecture/server/formula-design.md`（如果存在）
  - **更新内容**:
    - 克隆功能移至Desktop层说明
    - 药材验证优化说明（自动匹配）

- [ ] **3.2.3 更新Desktop端架构文档**
  - **文件**: `docs/explanation/architecture/client/README.md`
  - **更新内容**:
    - 新增验方克隆功能说明（Desktop实现）
    - 新增药材验证UI优化说明

- [ ] **3.2.4 更新MVPphilosophy文档**
  - **文件**: `.spec-workflow/steering/constitution.md`
  - **记录**: Issue #1733决策，证明MVP原则有效执行

**预期成果**: 架构文档反映最新设计决策

---

### Task 3.3: 更新快速参考 ✅

**目标**: 同步快速参考文档，方便开发者查阅

#### 子任务清单:

- [ ] **3.3.1 更新Controller清单**
  - **文件**: `docs/reference/quick-reference/controllers-list.md`（如果存在）
  - **更新**: 移除PerformanceController，更新其他Controller说明

- [ ] **3.3.2 更新常用端点清单**
  - **文件**: `docs/reference/quick-reference/common-endpoints.md`（如果存在）
  - **更新**: 移除已删除端点，更新已修改端点

- [ ] **3.3.3 更新故障排查指南**
  - **文件**: `docs/how-to-guides/troubleshooting.md`
  - **更新**: 性能监控相关问题改为推荐Application Insights

- [ ] **3.3.4 更新docs/index.md**
  - **文件**: `docs/index.md`
  - **更新**: 添加Issue #1733变更说明链接

**预期成果**: 开发者快速查阅最新API信息

---

## 📊 验收标准总览

### Phase 1验收（编译+运行时）

```bash
# 1. 编译检查
dotnet build LYBT.All.sln -c Release --no-restore
# 预期: 0 errors, 0 warnings

# 2. 启动WebAPI
dotnet run --project src/Server/Services/LYBT.WebAPI

# 3. 健康检查
curl http://localhost:5000/api/health
# 预期: {"status":"healthy","database":"connected"}

curl http://localhost:5000/api/ping
# 预期: "pong"

# 4. 缓存管理
curl http://localhost:5000/api/cache/statistics
# 预期: {"totalKeys":10,"memoryUsage":"2.5MB"}

curl -X DELETE http://localhost:5000/api/cache/clear
# 预期: {"success":true}

# 5. 统一认证
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
# 预期: {"token":"...","user":{...}}

# 6. 验证已删除端点返回404
curl http://localhost:5000/api/performance/metrics
# 预期: 404 Not Found

curl -X POST http://localhost:5000/api/cache/diagnose
# 预期: 404 Not Found

curl -X POST http://localhost:5000/api/formulas/xxx/copy
# 预期: 404 Not Found
```

### Phase 2验收（功能+性能）

```bash
# 1. 验方导入自动匹配
curl -X POST http://localhost:5000/api/formulas/import \
  -F "file=@test-formulas.xlsx"
# 预期响应包含:
# {
#   "totalCount": 100,
#   "successCount": 100,
#   "autoMatchedCount": 85,
#   "pendingValidationCount": 15,
#   "pendingHerbs": [...]
# }

# 2. 缓存策略统一
curl -w "@curl-format.txt" http://localhost:5000/api/v1/formulas
# 第一次: 响应时间 ~200ms
curl -w "@curl-format.txt" http://localhost:5000/api/v1/formulas
# 第二次: 响应时间 ~20ms（缓存命中）

# 3. 辅助字段包含在主查询
curl http://localhost:5000/api/v1/medical-cases/{id}
# 预期响应包含:
# {
#   "id": "...",
#   "canEdit": true,
#   "canDelete": false,
#   "canCreatePrescription": true,
#   ...
# }
```

### Phase 3验收（文档完整性）

```bash
# 1. 检查文档链接有效性
grep -r "PerformanceController" docs/ --include="*.md"
# 预期: 无结果（或仅在归档文档）

grep -r "/copy" docs/ --include="*.md"
# 预期: 仅在Desktop实现说明中

# 2. 验证API文档同步
grep -r "/api/formulas/import" docs/reference/api/
# 预期: 找到更新后的导入端点说明

# 3. 验证迁移指南存在
ls docs/reference/api/migration-guide.md
# 预期: 文件存在
```

---

## 📈 预期成果统计

| 指标 | Phase 1 | Phase 2 | Phase 3 | 总计 |
|------|---------|---------|---------|------|
| 代码减少（行） | ~640 | ~50 | 0 | **~690** |
| 端点减少（个） | 11 | 4 | 0 | **15** |
| 性能提升 | +5% | +15% | - | **+20%** |
| 文档更新（篇） | 0 | 0 | 8 | **8** |

### 详细分解

- **Task 1.1**: 代码 -200行
- **Task 1.2**: 代码 -250行，端点 -6个
- **Task 1.3**: 代码 -150行，端点 -2个
- **Task 1.4**: 代码 -50行，端点 -2个
- **Task 1.5**: 代码 -40行，端点 -1个
- **Task 2.2**: 药材验证效率 +500%
- **Task 2.3**: 端点 -2个，网络请求 -30%

---

## 🔄 后续优化建议

### Phase 4（可选）: 深度优化

- [ ] **批量操作端点统一设计**
  - 统一命名: `POST /api/{resource}/batch`
  - 统一响应: `BatchOperationResultDto`

- [ ] **分页参数标准化**
  - 统一参数: `page`, `pageSize`, `sortBy`, `sortOrder`
  - 统一响应: `PagedResult<T>`

- [ ] **错误响应标准化**
  - 统一错误码: `ApiErrorCodes`
  - 统一响应格式: `ApiResponse<T>`

---

**文档创建时间**: 2025-10-31
**关联Issue**: #1733
**预计完成时间**: Phase 1 (1-2天) + Phase 2 (3-5天) + Phase 3 (1天) = **5-8天**
**维护者**: Claude Code

---

## ✅ 执行追踪

- [ ] Phase 1.1 完成
- [ ] Phase 1.2 完成
- [ ] Phase 1.3 完成
- [ ] Phase 1.4 完成
- [ ] Phase 1.5 完成
- [ ] Phase 2.1 完成
- [ ] Phase 2.2 完成
- [ ] Phase 2.3 完成
- [ ] Phase 3.1 完成
- [ ] Phase 3.2 完成
- [ ] Phase 3.3 完成
- [ ] 最终验收通过

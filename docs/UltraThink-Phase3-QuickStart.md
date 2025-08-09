# 🚀 UltraThink Phase 3 快速开始指南

## 立即可执行的任务

### 🔥 Stage 4: 测试覆盖率提升 - 今日开始

#### 第1天任务清单
```bash
# 1. 创建测试基础设施
- [ ] 创建 tests/UltraThink/ 目录结构
- [ ] 创建 TestDataBuilder 基类
- [ ] 创建 MockFactory 工厂类
- [ ] 配置测试覆盖率工具

# 2. 开始HerbService测试
- [ ] HerbService.GetAllHerbsAsync 测试
- [ ] HerbService.GetHerbByIdAsync 测试
- [ ] HerbService.CreateHerbAsync 测试
- [ ] HerbService.UpdateHerbAsync 测试
- [ ] HerbService.DeleteHerbAsync 测试
```

#### 测试模板示例
```csharp
[Fact]
public async Task GetHerbById_WhenHerbExists_ReturnsHerb()
{
    // Arrange
    var herbId = Guid.NewGuid();
    var expectedHerb = new TestDataBuilder()
        .WithId(herbId)
        .WithName("麻黄")
        .Build();
    
    _mockRepository.Setup(x => x.GetByIdAsync(herbId))
        .ReturnsAsync(expectedHerb);
    
    // Act
    var result = await _herbService.GetHerbByIdAsync(herbId);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedHerb.Name, result.Name);
}
```

### 🔨 Stage 8: 继续大文件重构 - 可并行执行

#### 待重构文件列表（按优先级）
1. **ConsultationService.cs** (~900行)
   - 拆分为: ConsultationManager, DiagnosisService, TreatmentService
   
2. **UserService.cs** (~800行)
   - 拆分为: UserManager, RoleService, PermissionService
   
3. **PatientService.cs** (~750行)
   - 拆分为: PatientManager, PatientSearchService, PatientStatisticsService

### 🛡️ Stage 7: 安全快速修复 - 高优先级

#### 立即修复清单
```csharp
// 1. SQL注入防护 - 使用参数化查询
❌ 错误: $"SELECT * FROM Users WHERE Name = '{userName}'"
✅ 正确: "SELECT * FROM Users WHERE Name = @userName"

// 2. 敏感数据加密
❌ 错误: Password = userInput
✅ 正确: Password = BCrypt.HashPassword(userInput)

// 3. XSS防护
❌ 错误: return Html.Raw(userContent)
✅ 正确: return Html.Encode(userContent)
```

## 📋 每日检查清单

### 代码质量检查
- [ ] 新代码行数 < 500行/文件
- [ ] 测试覆盖率增加 > 2%
- [ ] 0个新的编译警告
- [ ] 代码复杂度 < 10

### UltraThink原则检查
- [ ] **职责单一**: 每个类只有一个改变的理由
- [ ] **代码干净**: 命名清晰，注释适当
- [ ] **性能出色**: 查询优化，异步处理

## 🎯 本周目标（Week 1）

### 必须完成
1. ✅ 创建测试基础设施
2. ✅ 完成至少3个Service的单元测试
3. ✅ 测试覆盖率提升至10%

### 尽量完成
1. ⭕ 重构1个大文件
2. ⭕ 修复3个安全问题
3. ⭕ 创建性能基准测试

## 📊 进度追踪

### 测试覆盖率进度
```
当前: 2.76% [■□□□□□□□□□] 目标: 60%
Day 1: +2%  [■■□□□□□□□□]
Day 2: +3%  [■■■□□□□□□□]
Day 3: +2%  [■■■■□□□□□□]
...
```

### 文件重构进度
```
大文件数量: 45个
已重构: 3个 [■■■□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□□]
本周目标: 5个
```

## 🚦 快速命令

### 运行测试
```bash
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test tests/Backend/LYBT.Module.Herbs.Tests

# 生成覆盖率报告
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### 代码分析
```bash
# 查找大文件
powershell -Command "Get-ChildItem -Path 'src' -Filter '*.cs' -Recurse | Where-Object { (Get-Content $_.FullName).Count -gt 500 } | Select-Object FullName"

# 运行代码分析
dotnet build /p:RunAnalyzers=true

# 检查代码复杂度
# 需要安装: dotnet tool install -g dotnet-code-metrics
dotnet code-metrics -p LYBTZYZS.sln
```

## 💡 专家建议

### 测试优先
> "先写测试，再写代码。这不仅能提高代码质量，还能帮助你更好地理解需求。"

### 小步重构
> "每次只重构一小部分，确保测试通过后再继续。宁可慢，不可错。"

### 持续集成
> "每次提交都应该触发自动化测试。破坏构建的代码不应该合并。"

## 🎉 激励语

**今日格言**: "优秀的代码是重构出来的，而不是一次写成的。"

**本周挑战**: 让测试覆盖率突破10%，你能做到！

---
*开始时间: 2025-01-08*
*使用 UltraThink 方法论*
*记住: 职责单一 · 代码干净 · 性能出色*
# 性能分析命令 (/analyze-perf)

深度分析代码性能问题，识别性能瓶颈并提供优化建议。

## 📋 执行流程

### 1️⃣ 性能问题识别

#### A. 数据库查询性能
使用`mcp__serena__search_for_pattern`搜索潜在问题：

**检查模式**：
```csharp
// ❌ N+1查询问题
foreach (var item in items) {
    var related = await _repository.GetByIdAsync(item.RelatedId);
}

// ❌ 过度查询
var all = await _repository.GetAllAsync();  // 获取全部数据
var filtered = all.Where(...).ToList();      // 客户端过滤

// ❌ 缺少分页
public async Task<List<T>> GetAllAsync()  // 无分页参数

// ❌ 笛卡尔积风险
.Include(x => x.Collection1)
.Include(x => x.Collection2)
.Include(x => x.Collection3)
```

#### B. 内存泄漏风险
```csharp
// ❌ 未释放资源
public void ProcessData() {
    var stream = new FileStream(...);
    // 缺少using或Dispose
}

// ❌ 事件订阅未取消
public void Subscribe() {
    EventAggregator.GetEvent<XXX>().Subscribe(Handler);
    // 未在Dispose中Unsubscribe
}

// ❌ 静态集合累积
private static List<T> _cache = new();  // 永不清理
```

#### C. 并发性能问题
```csharp
// ❌ 阻塞异步
var result = asyncMethod().Result;  // 死锁风险

// ❌ 同步IO
File.ReadAllText(path);  // 应使用异步

// ❌ 缺少并行处理
foreach (var item in largeCollection) {
    await ProcessAsync(item);  // 串行处理，应考虑并行
}
```

### 2️⃣ 性能测试

#### 执行基准测试
```bash
# 如果有BenchmarkDotNet测试
dotnet run -c Release --project tests/Performance/LYBT.Benchmarks.csproj

# 数据库查询性能测试
dotnet test tests/Performance/QueryPerformanceTests.cs -c Release
```

#### 分析测试结果
- 平均响应时间
- 内存分配量
- GC压力
- 数据库查询次数

### 3️⃣ 代码审查（使用Serena MCP）

#### 查找性能反模式
```bash
# 使用mcp__serena__find_symbol找到所有Repository方法
# 检查是否有GetAllAsync模式

# 使用mcp__serena__find_referencing_symbols
# 检查Repository方法的调用者是否正确使用
```

#### 重点检查模块
- **Service层**：是否有过度查询
- **Repository层**：是否有N+1问题
- **ViewModel层**（Desktop）：是否有内存泄漏

### 4️⃣ 生成性能报告

#### 报告模板
```markdown
# 🚀 性能分析报告

**分析日期**：{当前日期}
**分析范围**：{模块/功能}
**分析方法**：代码审查 + 静态分析

---

## 🔴 P0 - 严重性能问题

### 问题1：{问题标题}
- **位置**：`src/path/to/file.cs:123`
- **问题描述**：{详细说明}
- **影响**：
  - 网络流量：+XX%
  - 内存占用：+XX MB
  - 响应时间：+XX ms
- **优化建议**：
  \`\`\`csharp
  // ✅ 推荐做法
  {代码示例}
  \`\`\`

---

## 🟡 P1 - 中等性能问题

### 问题2：{问题标题}
{同上格式}

---

## 🟢 优化建议（可选）

### 建议1：{建议标题}
{优化方向}

---

## 📊 优化收益预估

| 优化项 | 预期收益 | 实施难度 | 优先级 |
|--------|---------|---------|--------|
| 修复N+1查询 | 响应时间-80% | 中 | P0 |
| 添加分页 | 内存-90% | 低 | P0 |
| 异步优化 | 吞吐量+50% | 低 | P1 |

---

## 🎯 实施建议

**Phase 1（本周）**：修复P0问题
**Phase 2（下周）**：修复P1问题
**Phase 3（可选）**：性能优化
```

### 5️⃣ 创建跟踪Issue

如果发现严重性能问题，自动创建Issue：
```bash
gh issue create --title "perf: {问题描述}" \
  --label "type:performance,priority:p0" \
  --body "详见性能分析报告：{报告路径}"
```

## 🎯 使用场景

- 发现系统响应变慢
- 准备上线前的性能检查
- 重构后的性能回归测试
- 定期性能健康检查

## ⚡ 快速使用

### 分析整个模块
```
/analyze-perf Module.Patients
```

### 分析特定类
```
/analyze-perf PatientService
```

### 分析当前PR的性能影响
```
/analyze-perf --current-pr
```

## 📚 性能优化最佳实践

### 数据库查询
- ✅ 使用分页（PagedResult<T>）
- ✅ 投影（Select只需要的字段）
- ✅ 避免N+1（使用Include或批量查询）
- ✅ 使用异步方法（GetPagedAsync）

### 内存管理
- ✅ 使用using释放资源
- ✅ 事件订阅必须取消
- ✅ 避免静态集合累积
- ✅ 大集合使用yield return

### 异步并发
- ✅ 所有IO操作使用async/await
- ✅ 避免.Result和.Wait()
- ✅ 考虑Task.WhenAll并行处理
- ✅ 使用ConfigureAwait(false)（库代码）

## 🔧 工具集成

- `mcp__serena__search_for_pattern` - 搜索性能反模式
- `mcp__serena__find_symbol` - 查找性能敏感方法
- `mcp__sequential-thinking` - 深度分析性能瓶颈
- `git diff` - 分析PR的性能影响

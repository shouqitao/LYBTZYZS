---
name: lybtzyzs-code-review
description: 为LYBTZYZS项目执行代码规范自动审查，检查命名规范、MVVM模式、DI使用、异步模式、中文注释等。生成详细的审查报告（严重问题/警告/通过项）。触发关键词：代码审查、检查代码规范、代码质量检查、review、code review、check code、review this code
---

# LYBTZYZS 代码规范审查器

## 核心能力

1. **命名规范检查** - PascalCase/\_camelCase/UPPER_SNAKE_CASE验证
2. **MVVM模式验证** - ViewModel不操作UI、Command使用、属性通知
3. **DI规范检查** - 构造函数注入、禁止ServiceLocator/Container.Resolve
4. **异步模式检查** - I/O必须async、避免.Wait()/.Result阻塞
5. **中文注释检查** - 公开API必须有中文注释
6. **Architecture合规** - 三层对齐架构、依赖方向验证
7. **生成详细报告** - 严重问题（必须修复）/警告（建议优化）/通过检查

## 何时使用

- PR提交前执行代码质量检查
- 重构后验证代码规范合规性
- 新成员代码需要规范指导
- 定期代码质量审计
- 怀疑代码存在规范问题时

## 工作流程

1. 确定检查范围（默认：当前git diff，可指定目录/文件）
2. 选择检查严格程度（严格/标准/宽松）
3. 执行多维度检查（命名/MVVM/DI/异步/注释/架构）
4. 生成分级报告（🔴严重问题/🟡警告/🟢通过）
5. 计算总体评分（0-10分）
6. 提供修复建议和示例

## 输入要求

**必需**：
- 无（默认检查当前git diff）

**可选**：
- `scope` - 检查范围：
  - `diff` - 当前git diff（默认）
  - `staged` - 已暂存文件（git add后）
  - `directory:{path}` - 指定目录（如 `directory:src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase`）
  - `files:{paths}` - 指定文件列表（逗号分隔）
- `strictness` - 严格程度：
  - `strict` - 严格模式（所有规则）
  - `standard` - 标准模式（默认）
  - `relaxed` - 宽松模式（仅严重问题）

## 输出格式

```markdown
## 🔍 代码审查报告

**检查范围**: src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
**文件数**: 8个
**代码行数**: 2,345行
**严格程度**: 标准

---

## 🔴 严重问题（必须修复）- 2个

### 1. ServiceLocator反模式
**文件**: `ViewModels/PatientSelectionViewModel.cs:45`
**问题**: 使用了禁止的ServiceLocator模式
```csharp
// ❌ 错误
private IPatientRepository _repository = ServiceLocator.Current.GetInstance<IPatientRepository>();
```
**修复建议**:
```csharp
// ✅ 正确：使用构造函数注入
private readonly IPatientRepository _repository;

public PatientSelectionViewModel(IPatientRepository repository)
{
    _repository = repository;
}
```

### 2. 阻塞调用 .Wait()
**文件**: `Services/MedicalCaseService.cs:123`
**问题**: 使用了阻塞调用，可能导致死锁
```csharp
// ❌ 错误
var result = _repository.GetByIdAsync(id).Wait();
```
**修复建议**:
```csharp
// ✅ 正确：使用async/await
var result = await _repository.GetByIdAsync(id);
```

---

## 🟡 警告（建议优化）- 3个

### 1. 私有字段命名不规范
**文件**: `ViewModels/MedicalCaseFlowViewModel.cs:20`
**问题**: 私有字段未使用\_前缀
```csharp
// ⚠️ 不推荐
private IRegionManager regionManager;
```
**建议**:
```csharp
// ✅ 推荐
private readonly IRegionManager _regionManager;
```

### 2. 公开方法缺少中文注释
**文件**: `Services/PatientService.cs:56`
**问题**: 公开方法缺少中文注释
```csharp
// ⚠️ 缺少注释
public async Task<PatientDto> GetByIdAsync(int id)
```
**建议**:
```csharp
/// <summary>
/// 根据ID获取患者信息
/// </summary>
public async Task<PatientDto> GetByIdAsync(int id)
```

### 3. 异步方法未使用Async后缀
**文件**: `Repositories/MedicalCaseRepository.cs:78`
**问题**: 异步方法名称应以Async结尾
```csharp
// ⚠️ 不推荐
public async Task<MedicalCase> GetById(int id)
```
**建议**:
```csharp
// ✅ 推荐
public async Task<MedicalCase> GetByIdAsync(int id)
```

---

## 🟢 通过检查

- ✅ **MVVM架构清晰** - ViewModel未直接操作UI元素
- ✅ **Command使用正确** - 所有操作使用ICommand绑定
- ✅ **依赖注入规范** - 除2处问题外，其余均使用构造函数注入
- ✅ **属性通知实现** - ViewModel正确实现INotifyPropertyChanged
- ✅ **三层架构对齐** - View→ViewModel→Repository→ApiClient层次清晰

---

## 📊 总体评分

**9.2/10（优秀）**

- 命名规范: 9/10（1处警告）
- MVVM模式: 10/10（完全合规）
- DI规范: 8/10（1处严重问题）
- 异步模式: 8/10（1处严重问题）
- 中文注释: 9/10（1处警告）
- 架构合规: 10/10（完全对齐）

**评级标准**:
- 9-10分: 优秀（可直接合并）
- 7-8分: 良好（修复严重问题后合并）
- 5-6分: 一般（需要重点优化）
- <5分: 不合格（需要大幅改进）

---

## 💡 改进建议

1. **立即修复**（阻塞PR）:
   - 移除ServiceLocator，改用构造函数注入
   - 移除.Wait()阻塞调用，改用async/await

2. **建议优化**（可选）:
   - 统一私有字段命名（添加\_前缀）
   - 补充公开API中文注释
   - 异步方法名称添加Async后缀

3. **持续改进**:
   - 建议启用.editorconfig自动格式化
   - 考虑集成SonarLint实时检查
   - 定期执行代码审查（每周1次）
```

## 检查维度详解

### 1. 命名规范检查

**检查项**:
- ✅ 类型名称：PascalCase（`PatientService`, `MedicalCaseDto`）
- ✅ 公开成员：PascalCase（`GetByIdAsync`, `PatientName`）
- ✅ 私有字段：\_camelCase（`_repository`, `_logger`）
- ✅ 常量：UPPER_SNAKE_CASE（`MAX_RETRY_COUNT`）
- ✅ 异步方法：Async后缀（`CreateAsync`, `GetPagedAsync`）

**实现方式**:
```
Grep模式匹配：
- 类型定义: class|interface|enum|struct
- 公开成员: public.*\s+\w+
- 私有字段: private.*\s+[^_]\w+\s+[a-z]
- 异步方法无Async后缀: async Task.*\s+(?!.*Async)
```

### 2. MVVM模式检查

**检查项**:
- ✅ ViewModel不直接操作UI：搜索`MessageBox.Show`、`Visibility.`、`this.FindName`
- ✅ Command使用：验证操作绑定到`ICommand`而非事件
- ✅ 属性通知：检查`INotifyPropertyChanged`实现
- ✅ ViewModel无UI引用：搜索`using System.Windows;`、`using System.Windows.Controls;`

**实现方式**:
```
serena代码分析：
- find_symbol: 查找ViewModel类
- 检查依赖：是否引用System.Windows命名空间
- 检查方法：是否直接操作UI元素
```

### 3. DI规范检查

**检查项**:
- ✅ 构造函数注入：验证依赖通过构造函数传入
- ❌ 禁止ServiceLocator：搜索`ServiceLocator.`、`Container.Resolve`、`ServiceProvider.GetService`
- ❌ 禁止属性注入：搜索`[Inject]`特性

**实现方式**:
```
Grep快速扫描：
- ServiceLocator反模式: ServiceLocator\.|Container\.Resolve|ServiceProvider\.GetService
- 属性注入: \[Inject\]
```

### 4. 异步模式检查

**检查项**:
- ✅ I/O操作必须async：数据库查询、HTTP请求、文件操作
- ❌ 禁止阻塞调用：搜索`.Wait()`、`.Result`、`Task.WaitAll`
- ✅ async方法正确使用await：避免`async void`（除事件处理器）

**实现方式**:
```
Grep快速扫描：
- 阻塞调用: \.Wait\(|\.Result[^a-zA-Z]|Task\.WaitAll
- async void: async\s+void\s+(?!On[A-Z])
```

### 5. 中文注释检查

**检查项**:
- ✅ 公开类必须有注释：`public class`需`/// <summary>`
- ✅ 公开方法必须有注释：`public`方法需中文描述
- ✅ 接口方法必须有注释：`interface`成员需说明

**实现方式**:
```
serena代码分析：
- find_symbol: 查找public类型和方法
- 检查是否有/// <summary>注释
- 验证注释是否包含中文字符（[\u4e00-\u9fa5]）
```

### 6. 架构合规检查

**检查项**:
- ✅ 三层对齐：Client(View→ViewModel→Repository→ApiClient)
- ✅ 依赖方向：上层依赖下层，不能反向
- ✅ 禁止跨层调用：View不能直接调用Repository

**实现方式**:
```
serena代码分析：
- find_referencing_symbols: 检查依赖关系
- 验证依赖方向：Presentation→Application→Domain
```

## 技术实现

**使用的MCP工具链**:
1. **Bash (git)** - 获取git diff/staged文件列表
2. **Grep** - 快速模式匹配（ServiceLocator、.Wait()、命名规范）
3. **mcp__serena** - 深度代码分析（MVVM模式、依赖关系、注释检查）
4. **sequential-thinking** - 复杂逻辑判断（架构合规性、综合评分）
5. **Read** - 读取Constitution获取规范标准

**实现逻辑**:
```
1. 确定检查范围 → git diff/指定目录/文件列表
2. 快速扫描（Grep）→ ServiceLocator/阻塞调用/命名规范（<5秒）
3. 深度分析（serena）→ MVVM模式/依赖关系/注释完整性（<10秒）
4. 架构验证（serena + sequential-thinking）→ 三层对齐/依赖方向（<5秒）
5. 生成报告 → 分级问题列表 + 修复建议 + 总体评分
6. 输出结果 → Markdown格式报告
```

## 限制条件

- 仅支持.NET/C#代码（.cs文件）
- XAML文件暂不支持深度检查（仅基础语法）
- 命名规范检查基于正则表达式，可能有误报
- MVVM模式检查需要项目使用Prism/标准MVVM框架
- 性能：<100个文件 <20秒，>100个文件可能超时
- 需要git仓库（检查git diff）

## 最佳实践

1. **PR前必检** - 每次提交PR前执行代码审查
2. **增量检查** - 优先检查git diff（避免全量扫描）
3. **严格程度选择** - 新代码用strict，重构代码用standard，紧急修复用relaxed
4. **立即修复严重问题** - 🔴严重问题必须在PR合并前修复
5. **渐进优化警告** - 🟡警告可以创建后续Issue逐步优化
6. **定期全量检查** - 每月执行一次全量代码审查（检测技术债务）

## 性能指标

- **快速扫描**（Grep）：<5秒（<100个文件）
- **深度分析**（serena）：<10秒（<50个文件）
- **架构验证**（sequential-thinking）：<5秒
- **报告生成**：<2秒
- **端到端完成**：<20秒（标准场景）

**性能优化策略**:
- 优先使用Grep快速扫描（80%问题）
- serena仅用于复杂分析（MVVM/架构）
- 并行执行独立检查项
- 缓存Constitution规范标准

## 版本历史

| 版本 | 日期 | 变更说明 |
|------|------|----------|
| v1.0 | 2025-10-22 | 初始版本，支持6个检查维度 |

---

**维护者**：Claude Code
**反馈渠道**：GitHub Issues
**最后更新**：2025-10-22

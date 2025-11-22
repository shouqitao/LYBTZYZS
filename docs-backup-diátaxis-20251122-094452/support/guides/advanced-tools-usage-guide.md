# LYBTZYZS高级工具使用指南

> **文档版本**: v1.0
> **更新日期**: 2025-11-21
> **适用场景**: 复杂任务执行、架构设计、技术调研、代码分析

## 📚 文档目的

本指南详细说明LYBTZYZS项目中三个新引入的高级MCP工具的使用方法：
1. **Sequential-thinking** - 结构化深度推理工具
2. **Tavily-mcp** - 实时Web信息检索工具
3. **NetContext-server** - .NET代码库语义分析工具

## 🎯 工具总览

### 工具能力对比

| 工具 | 核心功能 | 主要价值 | 使用频率 |
|-----|---------|---------|---------|
| **Sequential-thinking** | 8-15步结构化推理 | 避免跳跃式思考，确保决策质量 | 高（复杂任务必用） |
| **Tavily-mcp** | 实时Web搜索 | 获取最新技术文档和最佳实践 | 中（按需使用） |
| **NetContext-server** | .NET代码语义搜索 | 精准定位代码，分析架构依赖 | 高（代码任务必用） |

### 工具之间的关系

```
┌────────────────────────────────────────┐
│     Sequential-thinking（编排层）        │
│         结构化推理 + 工具调度            │
└─────────┬──────────────────┬───────────┘
          │                  │
          ↓                  ↓
┌─────────────────┐  ┌──────────────────┐
│   Tavily-mcp    │  │ NetContext-server│
│  实时信息检索    │  │   代码库分析      │
└─────────────────┘  └──────────────────┘
```

**关系说明**：
- Sequential-thinking是"大脑"，负责整体推理和决策
- Tavily和NetContext是"传感器"，负责获取外部信息
- Sequential可以在推理过程中调用Tavily和NetContext补充信息
- Tavily和NetContext也可以独立使用

---

## 1️⃣ Sequential-thinking 详细指南

### 1.1 工具特性

**核心能力**：
- 提供8-15步的结构化推理链
- 支持在任意思考步骤中调用其他MCP工具
- 支持修正式推理（is_revision: true）
- 支持分支推理（branch_from_thought）
- 可动态调整total_thoughts

**关键参数**：
```typescript
{
  thought: string,              // 当前思考内容
  thoughtNumber: number,        // 当前步骤编号（从1开始）
  totalThoughts: number,        // 预计总步骤数
  nextThoughtNeeded: boolean,   // 是否需要继续思考
  isRevision?: boolean,         // 是否修正前面的推理
  revisesThought?: number,      // 修正哪一步
  branchFromThought?: number,   // 从哪一步分支
  branchId?: string            // 分支标识
}
```

### 1.2 使用场景

#### ✅ 适合使用Sequential-thinking的场景

1. **架构设计**
   ```bash
   场景：设计新模块的架构方案
   步骤数：8-12步
   推理链：需求分析 → 技术选型 → 架构设计 → 依赖分析 → 风险评估 → 方案输出
   ```

2. **Bug根因分析**
   ```bash
   场景：复杂Bug的根本原因诊断
   步骤数：6-10步
   推理链：现象描述 → 代码定位 → 调用链分析 → 数据流追踪 → 根因确定 → 修复方案
   ```

3. **技术方案评估**
   ```bash
   场景：在多个技术方案中选择最优方案
   步骤数：10-15步
   推理链：方案收集 → 标准制定 → 逐个评估 → 对比分析 → 风险评估 → 最终决策
   ```

4. **性能问题诊断**
   ```bash
   场景：系统性能瓶颈分析
   步骤数：8-12步
   推理链：性能数据分析 → 瓶颈定位 → 代码审查 → 优化方案 → 收益评估 → 实施建议
   ```

#### ❌ 不适合使用Sequential-thinking的场景

1. **简单查询**（如：查API文档）→ 直接用Tavily
2. **代码定位**（如：找某个类）→ 直接用NetContext
3. **明确的单步任务**（如：修改一个变量名）→ 直接编辑

### 1.3 最佳实践

#### 实践1：合理规划推理步骤

```bash
# ❌ 错误：步骤过少，思考不充分
totalThoughts: 3
Thought 1: 分析问题
Thought 2: 找解决方案
Thought 3: 输出结论

# ✅ 正确：步骤合理，逻辑严密
totalThoughts: 8
Thought 1: 理解问题背景和约束
Thought 2: 收集相关信息（可能调用Graphiti）
Thought 3: 分析问题的多个维度
Thought 4: 调用tavily查询业界方案
Thought 5: 调用netcontext分析现有代码
Thought 6: 综合评估各种方案
Thought 7: 识别风险和依赖
Thought 8: 输出最终决策和建议
```

#### 实践2：在推理过程中穿插工具调用

```bash
# 示例：架构设计任务
Thought 1: 分析业务需求和技术约束
  → 纯推理，不调用外部工具

Thought 2: 查询类似功能的实现方案
  → 调用 mcp__tavily-mcp__tavily-search
  → query: "WPF MVVM prescription management best practices"

Thought 3: 分析检索结果，提取关键模式
  → 基于tavily返回的结果继续推理

Thought 4: 分析现有代码中的相关模块
  → 调用 mcp__netcontext-server__semantic_search
  → query: "Prescription ViewModel Repository"

Thought 5: 基于现有架构设计新模块
  → 综合前面的信息进行推理

Thought 6-8: 详细设计、风险分析、输出方案
  → 纯推理
```

#### 实践3：使用修正式推理

```bash
# 场景：发现前面的推理有误
Thought 5:
  content: "发现Thought 3中对性能的估算有误，实际查询复杂度应该是O(n²)而非O(n)"
  isRevision: true
  revisesThought: 3

# 重新推理
Thought 6: 基于修正后的复杂度重新评估方案...
```

#### 实践4：动态调整总步骤数

```bash
# 初始估计
totalThoughts: 8

# Thought 5时发现问题比预期复杂
Thought 5:
  content: "发现需要分析的维度比预期多，需要增加推理步骤"
  totalThoughts: 12  # 动态调整
  needsMoreThoughts: true
```

### 1.4 实战案例

#### 案例1：Epic #2175性能优化方案设计

```bash
任务：设计处方搜索的性能优化方案

Thought 1/10: 理解当前性能问题
- 当前实现：每次输入都遍历全量数据
- 性能数据：3000条数据耗时200ms
- 目标：优化到50ms以内

Thought 2/10: 查询业界的搜索优化方案
→ 调用 mcp__tavily-mcp__tavily-search
→ query: "WPF search optimization fuzzy matching performance"
→ 结果：找到分级过滤、缓存预计算、索引构建等方案

Thought 3/10: 分析现有代码的过滤逻辑
→ 调用 mcp__netcontext-server__semantic_search
→ query: "PrescriptionItemViewModel FilterPrescriptions"
→ 结果：定位到当前的LINQ过滤实现

Thought 4/10: 评估各种优化方案的适用性
- 分级过滤：适合，可实现7级拼音匹配
- 缓存预计算：适合，拼音码可预计算
- 索引构建：不适合，数据量不大

Thought 5/10: 设计7级分级过滤算法
100分（完全匹配）→ 90分（拼音码前缀）→ ... → 30分（包含关系）

Thought 6/10: 设计Dictionary缓存优化方案
预计算小写字符串，避免~2000次ToLower()调用

Thought 7/10: 调用Graphiti查询历史类似优化
→ 调用 mcp__graphiti-memory__search_memory_facts
→ query: "性能优化 LINQ 缓存"
→ 结果：找到之前的ValueTuple优化经验

Thought 8/10: 综合设计完整方案
1. 7级分级过滤（核心算法）
2. Dictionary缓存（性能保证）
3. ValueTuple替代匿名类型（减少GC压力）

Thought 9/10: 风险评估
- 内存开销：Dictionary额外占用约100KB，可接受
- 维护复杂度：分级算法需要文档化
- 兼容性：无破坏性变更

Thought 10/10: 输出实施方案
【详细的实施步骤和验收标准】

最终结论：采用"7级分级过滤 + Dictionary缓存"方案
```

---

## 2️⃣ Tavily-mcp 详细指南

### 2.1 工具特性

**核心能力**：
- 实时Web搜索（brave_web_search、tavily-search）
- 支持多种搜索深度（basic / advanced）
- 可控制结果数量（max_results: 5-20）
- 支持国家/语言筛选
- 支持时间范围筛选（freshness参数）

**主要工具**：
```bash
mcp__tavily-mcp__tavily-search       # 主搜索工具
mcp__tavily-mcp__tavily-extract      # 网页内容提取
mcp__tavily-mcp__tavily-crawl        # 网站爬取
mcp__tavily-mcp__brave-web-search    # Brave搜索引擎
```

### 2.2 使用场景

#### ✅ 适合使用Tavily的场景

1. **查询最新技术文档**
   ```bash
   场景：查找.NET 8新特性
   query: ".NET 8 performance improvements new features"
   max_results: 10
   ```

2. **查找最佳实践**
   ```bash
   场景：MVVM架构最佳实践
   query: "WPF MVVM architecture best practices 2025"
   search_depth: "advanced"
   ```

3. **错误信息解决方案**
   ```bash
   场景：解决Entity Framework错误
   query: "EF Core Include ThenInclude N+1 query solution"
   max_results: 5
   ```

4. **开源项目示例**
   ```bash
   场景：查找相似功能的实现
   query: "GitHub WPF prescription management open source"
   max_results: 10
   ```

#### ❌ 不适合使用Tavily的场景

1. **项目内部代码查询** → 用NetContext
2. **历史经验查询** → 用Graphiti
3. **需要深度推理** → 用Sequential-thinking

### 2.3 最佳实践

#### 实践1：精准的查询关键词

```bash
# ❌ 查询词过于宽泛
query: "performance optimization"

# ✅ 查询词精确，包含技术栈
query: "WPF LINQ performance optimization ToLower() caching"
```

#### 实践2：适当控制结果数量

```bash
# 快速查询：5个结果足够
max_results: 5
query: "C# string.Contains case insensitive"

# 深度调研：10-20个结果
max_results: 15
query: "WPF data virtualization best practices"
```

#### 实践3：利用时间筛选获取最新信息

```bash
# 查询最新技术（近1年）
mcp__tavily-mcp__tavily-search
  query: ".NET 8 performance benchmarks"
  freshness: "py"  # past year

# 查询最近新闻（近1周）
mcp__tavily-mcp__tavily-search
  query: "Entity Framework Core 9.0 release"
  freshness: "pw"  # past week
```

### 2.4 独立使用示例

```bash
# 场景：快速查询API用法
任务：查找Entity Framework的Include用法

步骤：
1. 直接调用tavily（无需sequential-thinking）
   mcp__tavily-mcp__tavily-search
   query: "Entity Framework Core Include ThenInclude example"
   max_results: 5

2. 浏览结果，找到Microsoft官方文档

3. 应用到代码中
```

### 2.5 与Sequential-thinking组合使用

```bash
# 场景：技术方案评估
任务：评估WPF中的数据虚拟化方案

Sequential-thinking流程：
Thought 1: 理解数据虚拟化的需求
Thought 2: 调用tavily查询方案
  → mcp__tavily-mcp__tavily-search
  → query: "WPF data virtualization VirtualizingPanel performance"
Thought 3: 分析检索到的3个方案（VirtualizingStackPanel / Custom Virtualization / Third-party控件）
Thought 4: 评估各方案的优缺点
Thought 5: 结合项目约束做出选择
```

---

## 3️⃣ NetContext-server 详细指南

### 3.1 工具特性

**核心能力**：
- .NET代码库语义搜索（semantic_search）
- 项目和解决方案扫描（list_projects、list_solutions）
- 源文件列举（list_source_files）
- 文件内容读取（open_file）
- 代码覆盖率分析（coverage_summary、coverage_analysis）

**主要工具**：
```bash
mcp__netcontext-server__semantic_search      # 语义搜索（核心）
mcp__netcontext-server__list_projects        # 列举.csproj文件
mcp__netcontext-server__list_source_files    # 列举源代码文件
mcp__netcontext-server__open_file           # 读取文件内容
mcp__netcontext-server__search_code         # 文本搜索
```

### 3.2 使用场景

#### ✅ 适合使用NetContext的场景

1. **语义级代码搜索**
   ```bash
   场景：查找"处方管理相关的ViewModel"
   query: "Prescription management ViewModel"
   # 返回：PrescriptionItemViewModel, PrescriptionEditorViewModel等
   ```

2. **架构分析**
   ```bash
   场景：分析某个功能的实现架构
   query: "MedicalCase consultation workflow"
   # 返回：相关的Service、Repository、ViewModel
   ```

3. **代码定位**
   ```bash
   场景：查找特定功能的实现位置
   query: "pinyin code filtering algorithm"
   # 精准定位到PrescriptionItemViewModel的Filter方法
   ```

4. **依赖分析**
   ```bash
   场景：查找某个类的所有引用
   先用semantic_search找到类
   再用serena的find_referencing_symbols分析依赖
   ```

#### ❌ 不适合使用NetContext的场景

1. **简单的文本搜索**（如grep "string"）→ 用serena的search_for_pattern更快
2. **需要最新技术文档** → 用Tavily
3. **需要推理分析** → 用Sequential-thinking

### 3.3 最佳实践

#### 实践1：使用自然语言查询

```bash
# ❌ 机械的关键词堆砌
query: "ViewModel class Prescription"

# ✅ 自然语言描述
query: "ViewModel that handles prescription item display and filtering"
```

#### 实践2：结合topK参数控制结果数量

```bash
# 精准查询：返回Top 3
mcp__netcontext-server__semantic_search
  query: "Main application startup entry point"
  topK: 3

# 广泛查询：返回Top 10
mcp__netcontext-server__semantic_search
  query: "All ViewModels in the application"
  topK: 10
```

#### 实践3：与serena工具配合使用

```bash
# Step 1: 用netcontext-server进行语义搜索
mcp__netcontext-server__semantic_search
  query: "Prescription filtering logic"
→ 结果：找到PrescriptionItemViewModel.cs

# Step 2: 用serena进行精确的符号级操作
mcp__serena__find_symbol
  name_path: "PrescriptionItemViewModel/FilterPrescriptions"
  relative_path: "src/Client/Desktop/Modules/LYBT.Desktop.Prescriptions/ViewModels/PrescriptionItemViewModel.cs"
  include_body: true
```

### 3.4 与Sequential-thinking组合使用

```bash
# 场景：代码重构方案设计
任务：重构处方模块的数据访问层

Sequential-thinking流程：
Thought 1: 理解当前架构
Thought 2: 用netcontext搜索相关代码
  → mcp__netcontext-server__semantic_search
  → query: "Prescription data access repository pattern"
  → 结果：找到PrescriptionRepository, PrescriptionService
Thought 3: 分析现有代码的问题
Thought 4: 设计重构方案
Thought 5: 用netcontext查找依赖
  → mcp__netcontext-server__semantic_search
  → query: "Code that uses PrescriptionRepository"
Thought 6: 评估重构影响范围
Thought 7: 输出分步重构计划
```

### 3.5 NetContext vs Serena 选择指南

| 任务类型 | 推荐工具 | 原因 |
|---------|---------|------|
| 语义级搜索（如："处方相关功能"） | NetContext | 基于AI理解，更智能 |
| 符号级操作（如：重命名方法） | Serena | 精确的符号操作 |
| 代码定位（不知道确切名称） | NetContext | 自然语言查询 |
| 代码定位（知道确切名称） | Serena | 更快更准确 |
| 批量文本替换 | Serena | 正则表达式支持 |
| 架构分析 | NetContext | 语义理解能力强 |
| 依赖关系分析 | Serena | 精确的引用追踪 |

---

## 4️⃣ 工具组合实战案例

### 案例1：新模块架构设计（三工具协同）

**任务**：设计"库存管理"新模块的架构

#### 工作流程

```bash
Phase 1: 启动Sequential-thinking深度推理（总指挥）

Thought 1/12: 分析库存管理的业务需求
- 需求：药材入库、出库、盘点、预警
- 约束：必须符合项目的MVVM + Repository架构
- 目标：与现有8大模块保持一致性

Thought 2/12: 查询业界的库存管理最佳实践
→ 调用 mcp__tavily-mcp__tavily-search
→ query: "inventory management system architecture .NET best practices"
→ 结果：找到FIFO/LIFO策略、批次管理、预警机制等模式

Thought 3/12: 分析检索结果，提取适用模式
- FIFO先进先出：适合中药材管理
- 批次管理：必需，药材有效期管理
- 多级预警：库存预警 + 效期预警

Thought 4/12: 分析现有模块的架构模式
→ 调用 mcp__netcontext-server__semantic_search
→ query: "Herbs module architecture Repository Service ViewModel"
→ 结果：Herbs模块采用标准三层架构

Thought 5/12: 查看Herbs模块的具体实现
→ 调用 mcp__serena__find_symbol
→ name_path: "HerbsRepository"
→ depth: 1
→ 结果：获取HerbsRepository的方法列表

Thought 6/12: 查询Graphiti历史经验
→ 调用 mcp__graphiti-memory__search_memory_facts
→ query: "Herbs module 架构设计 Repository模式"
→ 结果：找到Epic #1773的组件化经验

Thought 7/12: 设计库存模块的数据模型
基于FIFO + 批次管理设计：
- Inventory（库存主表）
- InventoryBatch（批次表）
- InventoryTransaction（流水表）

Thought 8/12: 设计Repository层
参考HerbsRepository，设计：
- IInventoryRepository
- GetWithBatchesAsync() - ThenInclude批次数据
- GetLowStockItemsAsync() - 预警查询

Thought 9/12: 设计Service层
- InventoryService
  - InboundAsync() - 入库
  - OutboundAsync() - 出库（FIFO逻辑）
  - StockTakingAsync() - 盘点

Thought 10/12: 设计ViewModel层
参考Epic #1773组件化模式：
- InventoryListViewModel + DataManager
- InventoryEditorViewModel + Validator
- InventoryCommandHandler

Thought 11/12: 风险评估
- 数据迁移：新增3张表，无兼容性问题
- 性能风险：批次查询需要优化（类似Prescription的Include优化）
- 集成风险：需要与Herbs模块联动

Thought 12/12: 输出完整架构方案
【详细的类图、时序图、实施步骤】
```

#### 关键点

1. **Sequential-thinking作为总指挥**：统筹整个推理过程
2. **Tavily补充外部知识**：获取业界最佳实践
3. **NetContext分析现有代码**：确保架构一致性
4. **Graphiti提供历史经验**：避免重复踩坑

### 案例2：Bug诊断与修复（Sequential + NetContext）

**任务**：诊断并修复"处方搜索性能问题"

```bash
Thought 1/8: 理解性能问题的现象
- 症状：输入拼音码后，UI卡顿200ms
- 数据量：3000条处方数据
- 期望：优化到50ms以内

Thought 2/8: 定位性能瓶颈代码
→ 调用 mcp__netcontext-server__semantic_search
→ query: "Prescription search filter performance"
→ 结果：定位到PrescriptionItemViewModel.FilterPrescriptions方法

Thought 3/8: 分析当前实现的问题
→ 调用 mcp__serena__find_symbol
→ name_path: "PrescriptionItemViewModel/FilterPrescriptions"
→ include_body: true
→ 发现问题：每次都遍历全量数据，且多次调用ToLower()

Thought 4/8: 查询性能优化方案
→ 调用 mcp__tavily-mcp__tavily-search
→ query: "C# LINQ performance optimization ToLower caching"
→ 结果：找到预计算、缓存、分级过滤等方案

Thought 5/8: 设计优化方案
1. 7级分级过滤：快速剔除不匹配项
2. Dictionary缓存：预计算小写字符串
3. ValueTuple优化：减少GC压力

Thought 6/8: 评估优化效果
- 时间复杂度：O(n) → O(n)（不变，但常数项优化）
- 空间复杂度：增加约100KB（可接受）
- 预期收益：200ms → 30ms

Thought 7/8: 查询历史类似优化经验
→ 调用 mcp__graphiti-memory__search_memory_facts
→ query: "性能优化 Dictionary缓存 ValueTuple"
→ 结果：发现之前类似优化的成功案例

Thought 8/8: 输出完整修复方案
【详细的代码实现、测试计划、验收标准】
```

### 案例3：技术调研（Sequential + Tavily）

**任务**：调研WPF数据虚拟化技术方案

```bash
Thought 1/10: 理解数据虚拟化的需求
- 场景：处方列表有10000+条数据
- 问题：全量加载导致内存占用高、UI卡顿
- 目标：只渲染可见行，按需加载数据

Thought 2/10: 查询WPF虚拟化的原理和方案
→ 调用 mcp__tavily-mcp__tavily-search
→ query: "WPF VirtualizingStackPanel data virtualization large dataset"
→ max_results: 10

Thought 3/10: 分析检索到的方案
方案1：VirtualizingStackPanel（内置）
方案2：Custom VirtualizingPanel
方案3：Third-party控件（如DevExpress）

Thought 4/10: 查询各方案的性能对比
→ 调用 mcp__tavily-mcp__tavily-search
→ query: "VirtualizingStackPanel performance benchmark comparison"

Thought 5/10: 评估方案1：VirtualizingStackPanel
优点：
- 内置支持，无需引入第三方库
- 配置简单，XAML属性设置即可
缺点：
- 功能有限，不支持复杂场景
- 水平虚拟化支持较弱

Thought 6/10: 评估方案2：Custom VirtualizingPanel
优点：
- 完全可控，可定制复杂逻辑
- 性能最优
缺点：
- 开发成本高
- 维护复杂

Thought 7/10: 评估方案3：Third-party控件
优点：
- 功能强大，成熟稳定
- 开发效率高
缺点：
- 违反项目MVP原则（禁止引入新控件库）
- 许可证成本

Thought 8/10: 结合项目约束筛选方案
- 排除方案3：违反Constitution约束
- 方案1 vs 方案2：权衡开发成本和需求复杂度

Thought 9/10: 查询方案1的实际应用效果
→ 调用 mcp__tavily-mcp__tavily-search
→ query: "VirtualizingStackPanel real world performance 10000 items"

Thought 10/10: 输出技术选型结论
推荐方案：VirtualizingStackPanel
理由：
1. 满足10000条数据的性能要求（实测50ms渲染时间）
2. 符合MVP原则，无需引入新库
3. 开发成本低，1天即可完成
4. 可后续升级到Custom Panel（如需要）
```

---

## 5️⃣ 常见问题与解决方案

### Q1: Sequential-thinking何时该停止推理？

**判断标准**：
1. ✅ 问题已被充分分析
2. ✅ 方案已明确且可执行
3. ✅ 风险已识别且有应对措施
4. ✅ 决策依据充分且逻辑严密

**常见错误**：
- ❌ 过早停止：5步就结束，分析不充分
- ❌ 过度推理：15步还在重复论证，效率低下

**建议**：
- 架构设计：8-12步
- Bug诊断：6-10步
- 方案评估：10-15步

### Q2: Tavily搜索结果太多，如何筛选？

**方法1：精确查询词**
```bash
# 宽泛查询
query: "performance optimization"
max_results: 20  # 结果质量参差不齐

# 精确查询
query: "WPF LINQ ToLower() performance optimization caching"
max_results: 5   # 精准命中
```

**方法2：结合Sequential-thinking评估**
```bash
Thought 1: 调用tavily查询
Thought 2: 评估检索结果的相关性
Thought 3: 筛选出Top 3最相关的方案
Thought 4: 详细分析这3个方案
```

### Q3: NetContext和Serena应该用哪个？

**选择矩阵**：

| 场景 | 推荐工具 | 原因 |
|-----|---------|------|
| 不知道类名，用自然语言描述 | NetContext | 语义搜索能力强 |
| 知道类名，需要精确定位 | Serena | 更快更准确 |
| 需要重命名/替换操作 | Serena | 符号级编辑 |
| 需要分析架构依赖 | NetContext → Serena | 先语义搜索，再符号分析 |
| 需要批量文本替换 | Serena | 正则表达式支持 |

**组合使用**：
```bash
# Step 1: 用NetContext进行语义搜索
netcontext: "Find ViewModels that handle prescription"
→ 找到10个相关ViewModel

# Step 2: 用Serena进行精确操作
serena.find_symbol("PrescriptionItemViewModel/FilterPrescriptions")
→ 获取方法详细信息和代码
```

### Q4: 三个工具都需要调用吗？

**不是！根据任务复杂度决定**：

**简单任务**（<3步骤）：
- 查API → 只用Tavily
- 定位代码 → 只用NetContext
- 快速修改 → 直接编辑

**中等任务**（3-5步骤）：
- Sequential + Tavily（技术调研）
- Sequential + NetContext（代码重构）

**复杂任务**（≥6步骤）：
- Sequential + Tavily + NetContext + Graphiti（大型架构设计）

### Q5: 如何保存Sequential-thinking的推理结果？

**方法**：保存到Graphiti记忆

```bash
# 完成推理后
mcp__graphiti-memory__add_memory
  name: "库存模块-架构设计-Sequential推理链-2025-01-21"
  episode_body: """
  ## 任务：库存管理模块架构设计

  ## 推理过程（12步）
  1. 分析业务需求：FIFO + 批次管理 + 多级预警
  2. Tavily查询：找到业界最佳实践（FIFO、批次、预警机制）
  3. 分析现有架构：Herbs模块采用标准三层
  ...
  12. 输出方案：【详细架构设计】

  ## 关键决策
  - 采用FIFO策略（理由：符合中药材先进先出原则）
  - 参考Herbs模块架构（理由：保持一致性）
  - 使用ThenInclude预加载（理由：避免N+1查询）

  ## 工具调用记录
  - Tavily: 2次（查询最佳实践、查询性能优化）
  - NetContext: 1次（分析Herbs模块）
  - Graphiti: 1次（查询历史经验）

  ## 输出
  【完整的架构设计文档】
  """
```

---

## 6️⃣ 工具使用检查清单

### 任务开始前

- [ ] 任务是否需要深度推理？（≥6步骤）→ 使用Sequential-thinking
- [ ] 是否需要最新技术文档？→ 准备使用Tavily
- [ ] 是否需要分析现有代码？→ 准备使用NetContext
- [ ] 是否需要历史经验？→ 准备查询Graphiti

### Sequential-thinking使用中

- [ ] 推理步骤是否合理？（不少于6步，不多于15步）
- [ ] 是否在合适的时机调用其他工具？
- [ ] 是否避免了跳跃式思考？
- [ ] 是否考虑了多个维度的分析？

### Tavily使用中

- [ ] 查询词是否精确？（包含技术栈、关键词）
- [ ] 结果数量是否合理？（5-10个）
- [ ] 是否筛选了时间范围？（如需要最新信息）
- [ ] 是否与Sequential-thinking结合使用？（如需要）

### NetContext使用中

- [ ] 是否使用自然语言查询？（而非机械关键词）
- [ ] 结果数量是否合理？（topK: 3-10）
- [ ] 是否与Serena配合使用？（语义搜索 → 符号操作）
- [ ] 是否保存了重要的代码分析结果？

### 任务完成后

- [ ] 推理结果是否保存到Graphiti？
- [ ] 是否更新了相关文档？
- [ ] 是否总结了工具使用的经验教训？
- [ ] 是否验证了方案的可行性？

---

## 7️⃣ 附录

### 附录A：工具命令速查表

#### Sequential-thinking
```bash
mcp__sequential-thinking__sequentialthinking
  thought: "当前思考内容"
  thoughtNumber: 1
  totalThoughts: 10
  nextThoughtNeeded: true
```

#### Tavily
```bash
# 基础搜索
mcp__tavily-mcp__tavily-search
  query: "技术关键词"
  max_results: 5
  search_depth: "basic"

# 高级搜索
mcp__tavily-mcp__tavily-search
  query: "技术关键词"
  max_results: 10
  search_depth: "advanced"
  freshness: "pm"  # past month
```

#### NetContext
```bash
# 语义搜索
mcp__netcontext-server__semantic_search
  query: "自然语言描述"
  topK: 5

# 列举项目
mcp__netcontext-server__list_projects

# 列举源文件
mcp__netcontext-server__list_source_files
  projectDir: "/path/to/project"
```

### 附录B：常用查询模板

#### Tavily查询模板
```bash
# 最佳实践查询
"{技术栈} {功能描述} best practices 2025"

# 性能优化查询
"{具体问题} performance optimization solution"

# 错误解决查询
"{错误信息} {技术栈} solution"

# 开源项目查询
"GitHub {功能描述} {技术栈} open source example"
```

#### NetContext查询模板
```bash
# 功能定位查询
"{功能描述} implementation in the codebase"

# 架构分析查询
"{模块名} architecture Repository Service ViewModel"

# 依赖分析查询
"Code that uses {类名} or {方法名}"
```

### 附录C：更多学习资源

**官方文档**：
- Sequential-thinking: [Anthropic MCP官方文档](https://github.com/modelcontextprotocol/servers)
- Tavily: [Tavily MCP Server文档](https://docs.tavily.com/)
- NetContext: [.NET Code Context MCP Server](https://github.com/netcontext-server)

**项目内部文档**：
- `CLAUDE.md` - 工作流程总览
- `docs/guides/requirement-driven-workflow.md` - 需求驱动工作流
- `docs/reference/mvp-constraints.md` - MVP约束

**Graphiti记忆**：
- 检索关键词: "工具使用最佳实践"
- 检索关键词: "Sequential-thinking实战案例"
- 检索关键词: "性能优化经验总结"

---

## 📝 文档维护

**更新策略**：
- 每引入新工具时更新本文档
- 每发现新的使用模式时补充案例
- 每完成大型任务后总结经验教训

**反馈渠道**：
- 通过Graphiti记忆系统记录使用体验
- 在任务完成后的reflect阶段总结工具使用效果

**版本历史**：
- v1.0 (2025-11-21): 初始版本，包含三个工具的详细使用指南

---

> 📌 **注意**：本文档是活文档，会随着项目实践不断更新和完善。建议定期查阅以获取最新的使用指南和最佳实践。

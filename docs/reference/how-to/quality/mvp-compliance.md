---
name: lybtzyzs-mvp-compliance
description: 检查LYBTZYZS项目代码是否符合MVP原则和Constitution约束，自动检测技术黑名单违规和过度设计模式
version: v1.0
last_updated: 2025-10-21
---

# LYBTZYZS MVP合规检查

## 变更记录
- v1.0 (2025-10-21): 初始版本

---

## 检查目标

本Skill用于自动检测违反项目Constitution的代码设计，确保遵循MVP原则和技术约束。

**检查范围**：
- 技术黑名单违规（自动检测）
- 过度设计模式（分析 + 建议）
- 依赖注入规范（自动检测）

**参考文档**：
- Constitution: `.spec-workflow/steering/constitution.md`
- 技术决策: `docs/architecture/shared/technical-decisions.md`

---

## 检查流程

### 第一步：自动检测技术黑名单

使用`grep`工具扫描代码中的禁用技术关键字：

**禁用技术列表**：
- Redis / IDistributedCache / StackExchange.Redis
- CQRS / ICommand / IQuery / CommandHandler
- MediatR / IMediator / IRequest
- Docker / Dockerfile / docker-compose
- GraphQL / HotChocolate / GraphQL.NET
- RabbitMQ / Kafka / IMessageBus
- 微服务架构模式（Ocelot / Consul / ServiceDiscovery）

**检测命令**：
```bash
grep -r "IDistributedCache\|MediatR\|IMediator\|GraphQL\|RabbitMQ\|Kafka" --include="*.cs" src/
```

**如发现违规**：
- 直接报告违规文件和行号
- 说明违反的Constitution条款
- 提供符合MVP的替代方案

**示例报告**：
```
❌ 违规检测：技术黑名单

文件：src/Server/Services/CacheService.cs:15
代码：private readonly IDistributedCache _cache;
违规：使用Redis分布式缓存（Constitution 1.3禁用）
说明：MVP阶段暂无分布式缓存需求

建议替代方案：
1. 使用内存缓存（MemoryCache）
2. 简单的静态字典（适用于少量配置数据）
```

---

### 第二步：检测依赖注入违规

使用`grep`工具检测ServiceLocator模式：

**违规模式**：
- Container.Resolve
- ServiceProvider.GetService
- [Inject] 属性注入

**检测命令**：
```bash
grep -r "Container\.Resolve\|ServiceProvider\.GetService\|\[Inject\]" --include="*.cs" src/
```

**如发现违规**：
- 直接报告违规位置
- 说明必须使用构造函数注入
- 提供重构示例

---

### 第三步：过度设计分析（需人工确认）

使用`serena`工具分析代码复杂度，使用`sequential-thinking`工具评估设计合理性。

**检测场景**：
1. **简单CRUD + 复杂模式**
   - 检测：简单的用户管理功能使用Event Sourcing
   - 分析：功能需求 vs 架构复杂度
   - 建议：简化为Repository模式

2. **不必要的抽象层**
   - 检测：仅1-2个实现的接口层
   - 分析：抽象的收益 vs 维护成本
   - 建议：评估是否真正需要

3. **过度工厂模式**
   - 检测：简单对象创建使用复杂工厂
   - 分析：对象创建复杂度
   - 建议：直接new或简化工厂

**分析流程**：
1. 使用`serena`工具分析代码结构
2. 使用`sequential-thinking`深度推理设计合理性
3. 生成分析报告（当前设计 vs 简化方案）
4. 列出优缺点对比
5. **等待用户确认**是否需要简化

**报告格式**：
```
⚠️ 可疑设计检测：过度抽象

文件：src/Server/Services/UserService.cs
分析：用户CRUD功能使用Event Sourcing模式

当前设计：
- Event Store存储所有用户操作事件
- Event Handler处理事件并更新读模型
- 3个额外的抽象层

简化方案：
- 使用Repository模式直接操作数据库
- 1个Service层 + 1个Repository层

对比分析：
优势：
- 当前设计：完整的事件溯源，可回放历史
- 简化方案：代码简洁，易于维护

劣势：
- 当前设计：复杂度高，维护成本大（MVP阶段不需要）
- 简化方案：无法回溯历史操作（当前需求不需要）

建议：简化为Repository模式（节省60%代码）

❓ 请确认是否接受简化建议？
```

---

### 第四步：生成合规报告

汇总所有检查结果，生成完整的合规报告。

**报告结构**：
```markdown
# MVP合规检查报告

生成时间：[时间戳]
检查范围：[文件路径]

## ❌ 自动检测违规（需立即修复）

### 1. 技术黑名单违规
- 文件：xxx.cs:行号
- 违规：使用Redis
- 修复建议：[替代方案]

### 2. 依赖注入违规
- 文件：xxx.cs:行号
- 违规：使用ServiceLocator
- 修复建议：改为构造函数注入

## ⚠️ 建议确认项（需人工决策）

### 1. 过度设计
- 文件：xxx.cs
- 分析：[详细分析]
- 建议：[简化方案]
- 状态：等待确认

## ✅ 通过检查

- 无技术黑名单违规
- 无依赖注入违规
- 设计复杂度合理
```

---

## 工具协同

本Skill调用以下MCP工具：

1. **grep** - 扫描技术黑名单关键字
2. **serena** - 分析代码结构和复杂度
3. **sequential-thinking** - 深度推理设计合理性
4. **filesystem** - 读取Constitution文档

**执行顺序**：
```
grep（黑名单扫描）→ serena（代码分析）→ sequential-thinking（设计评估）→ 生成报告
```

---

## 测试场景

### 场景1：检测Redis黑名单

**测试代码**：
```csharp
using Microsoft.Extensions.Caching.Distributed;

public class CacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache cache)
    {
        _cache = cache;
    }
}
```

**预期输出**：
```
❌ 违规检测：技术黑名单

文件：测试代码
代码：IDistributedCache
违规：使用Redis分布式缓存（Constitution 1.3禁用）
建议：使用MemoryCache替代
```

---

### 场景2：检测过度设计

**测试代码**：
```csharp
// 简单的用户列表查询功能，却使用了复杂的CQRS模式

public class GetUsersQuery : IRequest<List<User>> { }

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, List<User>>
{
    private readonly IMediator _mediator;
    // ... 复杂的查询处理逻辑
}
```

**预期输出**：
```
⚠️ 可疑设计检测：过度设计

分析：简单用户列表查询使用CQRS + MediatR
违规：
1. 使用MediatR（技术黑名单）
2. 过度设计（简单查询不需要CQRS）

建议简化方案：
public class UserService : IUserService
{
    public async Task<List<User>> GetUsersAsync()
    {
        return await _repository.GetAllAsync();
    }
}

❓ 请确认是否接受简化建议？
```

---

## 使用指南

### 触发时机

当用户提出以下请求时，自动触发本Skill：
- "检查代码是否符合MVP原则"
- "验证是否使用了禁用技术"
- "分析设计是否过度复杂"
- "Constitution合规性检查"

### 执行步骤

1. **明确检查范围**：询问用户要检查哪些文件/目录
2. **执行自动检测**：扫描技术黑名单和依赖注入违规
3. **分析复杂度**：评估设计合理性（可选）
4. **生成报告**：汇总所有发现并提供修复建议
5. **等待确认**：对于模糊设计，等待用户决策

### 注意事项

- 技术黑名单违规 → 直接报告，无需确认
- 过度设计判断 → 提供分析，等待确认
- 报告中包含具体文件位置和修复建议
- 优先修复明确违规，再处理建议项

---

## 限制和免责

- 本Skill仅检测明显的违规模式，无法覆盖所有边界情况
- 过度设计判断依赖启发式分析，可能存在误判
- 最终决策权在用户，本Skill仅提供建议
- 建议定期更新Skill以适应Constitution变化

---

## 相关资源

- Constitution文档：`.spec-workflow/steering/constitution.md`
- MVP原则：`docs/architecture/shared/mvp-principles.md`
- 技术决策：`docs/architecture/shared/technical-decisions.md`

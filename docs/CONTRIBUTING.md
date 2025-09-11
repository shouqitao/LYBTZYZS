# 贡献指南

## Record-Only系统原则

本项目遵循**Record-Only系统架构原则**，所有开发必须严格遵守以下核心约束：

### 🎯 Record-Only核心理念

- **简化优于复杂** - 专注小诊所实际需求，避免企业级过度设计
- **记录优于流程** - 系统主要功能是记录医疗数据，而非复杂业务流程管理
- **实用优于理论** - 基于2-5人小诊所实际使用场景设计功能

### 🚫 严禁引入的模式和技术

#### 复杂业务模式
- ❌ **工作流引擎** (Workflow, Pipeline, Engine)
- ❌ **事件驱动架构** (Event Sourcing, CQRS)
- ❌ **消息队列** (Message Bus, Service Bus)
- ❌ **状态机** (State Machine, Saga)
- ❌ **微服务架构** (过度复杂，不适合小规模部署)

#### 过度抽象
- ❌ **复杂设计模式** (如过度使用工厂模式、策略模式)
- ❌ **多层抽象** (Domain层等过度架构分层)
- ❌ **AOP切面编程** (增加复杂性)
- ❌ **自定义验证框架** (使用标准DataAnnotations即可)

#### 企业级功能
- ❌ **复杂权限体系** (仅支持Doctor/Admin两种角色)
- ❌ **多租户架构** (单诊所部署为主)
- ❌ **分布式缓存** (内存缓存足够)
- ❌ **容器化部署** (传统部署更适合)

### ✅ 推荐的技术栈和模式

#### 后端架构
- ✅ **传统三层架构** (Controller-Service-Repository)
- ✅ **统一AppDbContext** (所有模块共享数据上下文)
- ✅ **LINQ查询** (类型安全，防SQL注入)
- ✅ **内存缓存** (IMemoryCache，简单有效)
- ✅ **JWT认证** (标准化，成熟可靠)

#### 前端架构
- ✅ **UltraThink双层架构** (QueryService + BusinessService + 纯委托)
- ✅ **MVVM模式** (Prism.DryIoc框架)
- ✅ **依赖注入** (构造函数注入)
- ✅ **AutoMapper** (对象映射)
- ✅ **Refit** (类型安全HTTP客户端)

## 开发规范

### 代码提交要求

1. **架构一致性检查**
   - 确保不违反Record-Only原则
   - 通过所有ArchTests架构测试
   - 遵循UltraThink双层架构标准

2. **代码质量要求**
   - 所有编译警告已解决
   - 通过代码格式化检查 (`dotnet format`)
   - 新增功能必须包含单元测试

3. **命名规范**
   - API路由使用小写 (`/api/v1/users`)
   - 控制器以Controller结尾
   - 服务接口以I开头 (`IUserService`)
   - 禁止使用Workflow、Pipeline、Bus等命名

### 测试要求

- **单元测试覆盖率** ≥60% (核心模块≥80%)
- **架构测试** 必须通过全部11个规则
- **集成测试** 关键API端点必须测试
- **性能测试** 响应时间≤2秒

### 文档要求

- **API变更** 必须更新Swagger文档
- **架构决策** 记录在决策文档中
- **Breaking Changes** 必须在PR中明确说明

## 提交流程

### 1. 开发前检查

```bash
# 检查当前分支状态
git status

# 确保基于最新代码
git pull origin master
```

### 2. 开发过程

```bash
# 运行格式化
dotnet format

# 本地构建测试
dotnet build
dotnet test
```

### 3. 提交审查

- 填写完整的PR模板信息
- 确认Record-Only原则合规性
- 通过所有自动化检查
- 至少一名维护者审核通过

## 架构决策参考

详细架构原则参见：
- [CLAUDE.md](../CLAUDE.md) - 项目开发指南
- [架构测试](../tests/Architecture/ArchTests.cs) - 强制架构约束
- [README.md](../README.md) - 项目概览

## 获得帮助

- 提交Issue描述问题
- 参考现有代码实现模式
- 查阅项目文档和注释
- 遵循"简单优于复杂"的设计原则

---

**记住**: 这是一个为小诊所设计的Record-Only系统，保持简单实用是我们的核心目标。
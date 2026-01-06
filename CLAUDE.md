<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# LYBTZYZS项目配置

**项目**: 凌隐宝堂中医诊所管理系统
**版本**: v1.0.0
**阶段**: 架构功能完善期 (Architecture & Feature Completion)
**技术栈**: .NET 8 + WPF + Prism + EF Core + SQL Server
**仓库**: https://github.com/shouqitao/LYBTZYZS

---

## 当前阶段: 架构功能完善期

**阶段目标**: 完善系统架构设计，补全功能实现，确保代码质量和可维护性

### 四大核心方向

| 方向 | 英文术语 | 具体要求 |
|------|----------|----------|
| **架构完善** | Architecture Improvement | 优化系统架构、统一设计模式、消除架构债务 |
| **功能完善** | Feature Completion | 补全缺失功能、完善业务逻辑、增强用户体验 |
| **质量提升** | Quality Enhancement | 编写单元测试、集成测试、提高代码覆盖率 |
| **规范统一** | Standardization | 统一代码风格、命名规范、API契约、文档标准 |

### 开发准则

1. **Architecture First**: 架构完善优先，所有功能开发必须符合既定架构模式
2. **Root Cause Analysis**: 修复问题必须定位根因，禁止表面修补(Workaround)
3. **Test Coverage**: 新功能必须编写对应测试，保证代码质量
4. **Documentation**: 重要架构决策和API变更必须更新文档

### 代码变更标准

```
[ALLOW]  架构优化 | 设计模式统一 | 代码重构 | 技术债务清理
[ALLOW]  功能完善 | 业务逻辑补全 | 用户体验优化
[ALLOW]  单元测试 | 集成测试 | 性能测试编写
[ALLOW]  文档更新 | API文档 | 架构说明
[ALLOW]  Bug修复 | 绑定错误修正 | 异常处理完善
[REVIEW] 跨模块架构调整 - 说明影响范围后执行
[REVIEW] 新增外部依赖 - 需评估必要性
```

**架构变更分级**:
- **局部优化**: 单模块内的模式调整、代码组织优化 -> 直接执行
- **跨模块优化**: 影响2-3个模块的接口调整 -> 说明影响范围后执行
- **架构重构**: 涉及核心架构变更 -> 需用户确认方案后执行
- **技术栈变更**: 引入新框架或替换现有技术 -> **必须用户审批**

---

## 修改前必查(铁律)

**出方案或修改代码前，必须完成以下步骤:**

1. **查记忆**: 用Serena记忆功能查已有解决方案
   ```
   mcp__serena__list_memories()           # 列出所有记忆
   mcp__serena__read_memory("记忆名")     # 读取特定记忆
   ```
2. **查文档**: 用context7/microsoft_docs_mcp查官方文档和最佳实践
3. **查案例**: 用tavily-search/brave-search查业界优秀实现
4. **问用户**: 方案确认后再执行，不确定必问

**禁止**: 未经调研直接编码 | 猜测方案 | 跳过用户确认 | 兼容模式(发现问题一律优化为最优模式)

---

## 记忆系统: Serena Memory

**工具集**:
- `list_memories` - 列出所有可用记忆
- `read_memory` - 读取记忆内容
- `write_memory` - 写入新记忆
- `edit_memory` - 编辑现有记忆
- `delete_memory` - 删除记忆

**使用场景**:
- 设计决策记录
- 架构约束备忘
- 术语规范定义
- 问题解决方案存档

**命名规范**: `<主题>-<日期>.md` 或 `<模块>-<功能>.md`

---

## UltraThink四阶段

**THINK(深度思考)** -> **PLAN(任务规划)** -> **EXECUTE(渐进执行)** -> **REFLECT(总结归档)**

---

## 核心约束

**关键规则**:
- TodoWrite必用 - 复杂任务必须创建任务列表
- Serena记忆优先 - 重要决策存入记忆系统
- Issue自动关闭(满足4标准) - 代码实现+测试通过+文档更新+PR合并
- Consultation仅指诊断部分 - 术语规范

---

## 死代码识别模式

**识别方法**:
1. `Grep` 搜索类型引用，仅自身和README = 死代码
2. 服务已注册但从未被ViewModel注入 = 死代码
3. 预规划功能无Repository/Service支持 = 未激活代码

**清理流程**:
1. 创建新文件 -> 更新引用 -> 删除原文件
2. Shell/Extensions中的Logger注册需同步清理
3. 空壳模块暂保留维持系统加载兼容性

**模块状态**:
- `Prescriptions` - 已完全移除（2026-01-05），功能迁移到MedicalCase

---

## 架构索引

查看Serena记忆获取架构信息:
```
mcp__serena__list_memories()  # 查看可用的架构记忆文件
```

**主要架构**:
- 前端: WPF + Prism + MVVM
- 后端: ASP.NET Core + 三层架构
- 数据: EF Core + SQL Server
- 设计: DDD聚合根模式

---

最后更新: 2026-01-05 08:12
文档版本: v3.6-dead-code-patterns

# 凌隐宝堂中医诊所项目 (LYBTZYZS)

> **核心理念**: "多问（问Graphiti）好比靠猜"  
> **执行流程**: THINK（深度思考）→ PLAN（任务规划）→ EXECUTE（渐进执行）→ REFLECT（总结归档）

## 🎯 UltraThink四阶段（详细查询Graphiti）

### 阶段1: THINK（深度思考）
- sequential-thinking深度推理（复杂任务必用）
- 查询Graphiti记忆（search_memory_facts + search_nodes + get_episodes）
- 实时信息检索（tavily / microsoft_docs）
- 代码定位（netcontext-server / serena）
- **详细指南**: `search_memory_facts("LYBTZYZS-UltraThink详细执行指南")`

### 阶段2: PLAN（任务规划）
- 大需求: requirements-generator → design-generator → task-breakdown
- 小需求: task-executor直接执行
- **TodoWrite必用**: ≥3步骤或≥30分钟
- **详细指南**: `search_memory_facts("LYBTZYZS-TodoWrite使用规范")`

### 阶段3: EXECUTE（渐进执行）
- 单一职责 + 小步快跑（≤2小时）
- 每完成子任务立即保存记忆到Graphiti
- **详细指南**: `search_memory_facts("LYBTZYZS-工具组合模式")`

### 阶段4: REFLECT（总结归档）
- 调用lybtzyzs-task-reflector生成总结
- 保存完整记忆到Graphiti（13部分模板）
- 满足4标准自动关闭Issue（验收+验证+文档+记忆）
- **详细指南**: `search_memory_facts("LYBTZYZS-Graphiti记忆管理详细模板")`

## 🧠 Graphiti优先原则

### 查询流程
1. **启动前（RETRIEVE）**: 先查Graphiti记忆，避免重复劳动
2. **执行中（RECORD）**: 每完成子任务立即保存记忆
3. **结束后（ARCHIVE）**: 保存完整任务总结（13部分）

### 效率优化
- 文件路径查询: 95%+ token节省
- 文档搜索: 95%+ token节省  
- 代码定位: 95%+ token节省
- **整体效果**: 上下文使用量减少80%+

## 🚨 核心约束（严格执行）

- **TodoWrite必用**: 复杂任务必须使用TodoWrite跟踪
- **Graphiti第一大脑**: 所有决策、经验、路径必须存储
- **Issue自动关闭**: 满足4标准立即关闭，无需询问
- **术语规范**: Consultation仅指"诊断部分"，看诊用MedicalCase
- **问题清单**: 所有问题保存到Graphiti，一次提一个
- **详细约束**: `search_memory_facts("LYBTZYZS-核心约束")`

## 📦 项目配置

- **项目**: 凌隐宝堂中医诊所管理系统 (LYBTZYZS)
- **技术栈**: .NET 8 + WPF + Prism + EF Core + SQL Server
- **架构**: 前端MVVM | 后端三层 | 统一IService接口
- **仓库**: https://github.com/shouqitao/LYBTZYZS

---
**最后更新**: 2025-11-23  
**文档版本**: v3.0-ultra-minimal

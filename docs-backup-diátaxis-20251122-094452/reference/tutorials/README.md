# Tutorial（教程）总览

**目标**：通过循序渐进的引导式教程，帮助新手快速掌握凌隐宝堂中医诊所系统的开发

**创建日期**：2025-10-29
**维护者**：项目团队

---

## 🎓 Tutorial vs 其他文档类型

### Tutorial（本目录）
- ✅ **学习导向** - 引导式、逐步教学
- ✅ **实践导向** - 边学边做，快速上手
- ✅ **明确目标** - 完成教程后能独立开发
- ⚠️ **不追求全面** - 只覆盖最核心的工作流

### 其他文档类型
- **How-to Guides**（操作指南）- 解决特定问题，假设有基础知识
- **Reference**（参考手册）- 查阅信息，精确描述
- **Explanation**（解释说明）- 深入理解架构和概念

> **💡 提示**：如果你已经有开发经验，可能更适合直接查阅[Reference](../reference/quick-reference/)或[Explanation](../explanation/architecture/)文档。Tutorial适合完全新手或需要系统学习的开发者。

---

## 📚 教程列表

### 🚀 新手入门

#### 1. **[5分钟快速开始](quick-start.md)** ⭐ 完全新手必读
**目标**：让完全新手在5分钟内启动系统并完成首次操作

**你将学到**：
- 环境搭建（.NET 8、SQL Server）
- 启动Server端（WebAPI）
- 启动Client端（Desktop WPF）
- 首次登录和基本操作

**预计时间**：5分钟
**难度**：⭐（入门）

---

#### 2. **[开发第一个功能](first-feature.md)** ⭐ 开发者必读
**目标**：通过完整的端到端示例，掌握系统的开发流程

**你将学到**：
- Server端三层架构开发（Entity → DTO → Service → Controller）
- Client端MVVM架构开发（Model → ViewModel → View）
- 完整的测试和提交流程

**预计时间**：1小时
**难度**：⭐⭐（初级）

---

### 🎯 进阶教程（待补充）

以下教程正在规划中，将根据社区反馈优先补充：

3. **环境搭建完整指南** - 详细的开发环境配置和常见问题解决
4. **调试技巧教程** - VS2022/Rider调试技巧和性能分析
5. **数据库迁移教程** - Entity Framework Core迁移管理和数据初始化

> **📢 贡献提示**：如果你希望看到特定主题的教程，请在[GitHub Issues](https://github.com/shouqitao/凌隐宝堂中医诊所/issues)提出建议！

---

## 🗺️ 学习路径推荐

### 路径1：完全新手 → 独立开发者

```
步骤1: 快速启动
[5分钟快速开始](quick-start.md) (5分钟)
  ├─ 验证环境
  ├─ 启动系统
  └─ 基本操作

步骤2: 理解架构
[架构总览](../explanation/architecture/README.md) (20分钟)
  ├─ 三层对齐架构
  ├─ Server端设计
  └─ Client端设计

步骤3: 实战开发
[开发第一个功能](first-feature.md) (1小时)
  ├─ Server端开发
  ├─ Client端开发
  └─ 测试提交

步骤4: 深入学习
[Server端开发指南](../how-to-guides/server/README.md) (参考)
[Client端开发指南](../how-to-guides/client/README.md) (参考)
  ├─ 代码规范
  ├─ 最佳实践
  └─ 常见模式

总耗时：约2小时 + 按需深入
```

---

### 路径2：有经验开发者 → 快速上手

```
步骤1: 快速启动
[5分钟快速开始](quick-start.md) (5分钟)

步骤2: 架构理解
[架构总览](../explanation/architecture/README.md) (15分钟)
  ├─ 三层对齐原理
  └─ 核心设计决策

步骤3: 参考文档
[API快速参考](../reference/quick-reference/api-reference.md)
[代码模式](../reference/quick-reference/code-patterns.md)
  └─ 按需查阅

总耗时：约20分钟 + 按需查阅
```

---

## 💡 使用建议

### 学习Tutorial的最佳方式

1. ✅ **跟随教程逐步操作**，不要跳步
   - 每完成一步都验证结果
   - 确保理解每步的目的

2. ✅ **实际操作代码**，不要只看不做
   - 手动输入代码，不要复制粘贴（加深记忆）
   - 尝试修改参数，观察效果

3. ✅ **遇到问题先自查**
   - 查看教程的"常见问题"章节
   - 对比你的代码和示例代码的差异
   - 检查错误信息，尝试理解原因

4. ✅ **完成后举一反三**
   - 尝试在教程基础上扩展功能
   - 应用到自己的实际需求中

---

### Tutorial的局限性

Tutorial设计为引导式学习，有以下局限性：

- ❌ **不覆盖所有功能** - 只展示核心流程
  - → 需要全面了解请查阅[Reference](../reference/quick-reference/)

- ❌ **不深入解释原理** - 侧重"怎么做"而非"为什么"
  - → 需要理解设计请查阅[Explanation](../explanation/architecture/)

- ❌ **不解决特定问题** - 是通用学习路径
  - → 需要解决具体问题请查阅[How-to Guides](../how-to-guides/)

---

## 📊 学习进度跟踪

建议在学习过程中跟踪进度：

- [ ] ✅ 完成"5分钟快速开始"
- [ ] ✅ 成功启动Server和Client
- [ ] ✅ 完成首次登录和操作
- [ ] ✅ 理解三层对齐架构
- [ ] ✅ 完成"开发第一个功能"
- [ ] ✅ 独立完成Server端开发
- [ ] ✅ 独立完成Client端开发
- [ ] ✅ 掌握测试和提交流程

**目标**：完成所有核心Tutorial后，你应该能够独立开发凌隐宝堂中医诊所系统的新功能。

---

## 📞 获取帮助

如果在学习过程中遇到问题：

1. **查看文档**
   - [常见问题解决](../reference/quick-reference/troubleshooting.md) - 80%的问题都能找到答案
   - [API快速参考](../reference/quick-reference/api-reference.md) - 查阅接口用法

2. **搜索Issues**
   - [GitHub Issues](https://github.com/shouqitao/凌隐宝堂中医诊所/issues) - 搜索类似问题
   - 如未找到，创建新Issue并详细描述问题

3. **联系团队**
   - 通过项目Issue系统提交问题
   - 包含完整的错误信息和复现步骤

---

## 🔄 贡献指南

欢迎贡献新的Tutorial！优秀的Tutorial应该符合以下标准：

### 内容标准
- ✅ **明确的学习目标** - 开头说明"你将学到什么"
- ✅ **循序渐进的步骤** - 每步都有清晰的操作指令
- ✅ **丰富的代码示例** - 提供完整可运行的代码
- ✅ **成功标志** - 每步都有验证方式
- ✅ **预计时间** - 标注完成所需时间
- ✅ **常见问题** - 预判可能遇到的问题

### 格式标准
- ✅ 使用Markdown格式
- ✅ 代码块标注语言（```csharp、```bash等）
- ✅ 适当使用Emoji增强可读性
- ✅ 包含截图或命令输出示例（可选）

### 提交流程
1. Fork项目仓库
2. 在`docs/tutorials/`下创建新教程
3. 更新本README.md的教程列表
4. 提交Pull Request
5. 等待团队审核和反馈

---

## 📚 相关资源

**其他文档类型**：
- [How-to Guides](../how-to-guides/) - 操作指南
- [Reference](../reference/quick-reference/) - 参考手册
- [Explanation](../explanation/architecture/) - 解释说明

**项目资源**：
- [项目README](../../README.md) - 项目总览
- [文档导航中心](../index.md) - 完整文档索引
- [GitHub仓库](https://github.com/shouqitao/凌隐宝堂中医诊所) - 代码和Issues

---

**最后更新**：2025-10-29
**文档版本**：v1.0（Diátaxis框架重构版）

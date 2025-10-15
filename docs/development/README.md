# 开发文档索引

- **维护人**：Claude Code
- **最后更新**：2025-10-15
- **版本**：v3.0（Server/Client对齐架构）

本目录收录开发阶段的规范、专项方案与实现总结。阅读建议：先浏览规范与指南，再查阅专项计划或完成总结。

---

## 🏗️ 对齐开发架构

### 🖥️ 端特定开发
- **[Server端开发](./server/)** - 服务端开发专项指南、测试架构
- **[Client端开发](./client/)** - 桌面端开发指南、MVVM实践

### 🔄 共享开发标准
- **[共享开发指南](./shared/)** - 跨端开发标准、工具配置、最佳实践

---

## 📁 详细目录

### 🖥️ Server端开发 (`server/`)
```
server/
├── README.md                                      # 本导航文档
├── server-testing-architecture-completion-report.md  # 测试架构完成报告 ⭐
└── server-tests-coverage-epic-summary.md          # 测试覆盖率总结
```

### 🖥️ Client端开发 (`client/`)
```
client/
├── README.md                                      # Client端开发导航
└── [待完善]                                       # MVVM实践、UI开发指南
```

### 🔄 共享开发指南 (`shared/`)
```
shared/
├── README.md                                      # 共享开发导航
├── documentation-guidelines.md                    # 文档编写规范 ⭐
├── standards.md                                   # 编码标准 ⭐
├── github-workflow-guide.md                       # GitHub工作流指南
├── module-template-guide.md                       # 模块化开发指南
├── ENUM_CENTRALIZATION_GUIDE.md                   # 枚举集中化指南
├── null-safety-guidelines.md                      # 空安全编程指南
├── repository-dependency-injection-guide.md       # 依赖注入指南
├── stylecop-version-evaluation.md                 # StyleCop版本评估
├── TECH_DEBT_BACKLOG.md                           # 技术债务管理
├── test-architecture-standard.md                  # 测试架构标准
├── testing-guide.md                               # 测试实施指南
├── testing-training-materials.md                  # 测试培训材料
└── phase2-session-management-completion-report.md # Session管理完成报告
```

---

## 🎯 快速导航

### 📋 新成员必读
1. **[编码标准](./shared/standards.md)** - 统一编码规范
2. **[文档指南](./shared/documentation-guidelines.md)** - 文档编写标准
3. **[GitHub工作流](./shared/github-workflow-guide.md)** - 开发流程

### 🛠️ 开发实践
- **[模块化开发](./shared/module-template-guide.md)** - 模块开发标准
- **[依赖注入](./shared/repository-dependency-injection-guide.md)** - DI配置指南
- **[空安全编程](./shared/null-safety-guidelines.md)** - 防御性编程

### 🧪 测试与质量
- **[测试架构标准](./shared/test-architecture-standard.md)** - 测试设计原则
- **[测试实施指南](./shared/testing-guide.md)** - 测试最佳实践
- **[Server端测试](./server/server-testing-architecture-completion-report.md)** - 服务端测试实践

### 📊 项目管理
- **[技术债务管理](./shared/TECH_DEBT_BACKLOG.md)** - 技术债务跟踪
- **[枚举集中化](./shared/ENUM_CENTRALIZATION_GUIDE.md)** - 数据字典管理

---

## 📚 使用指南

### 👨‍💻 开发角色导航

#### 新手开发者
1. 阅读 [`standards.md`](./shared/standards.md) 了解编码规范
2. 学习 [`github-workflow-guide.md`](./shared/github-workflow-guide.md) 掌握开发流程
3. 参考 [`module-template-guide.md`](./shared/module-template-guide.md) 开始模块开发

#### Server端开发者
1. 掌握共享开发标准（阅读shared目录核心文档）
2. 专注Server端架构（参考 [`../architecture/server/`](../architecture/server/)）
3. 实施测试架构（学习 [`server-testing-architecture-completion-report.md`](./server/server-testing-architecture-completion-report.md)）

#### Client端开发者  
1. 理解MVVM架构（参考 [`../architecture/client/`](../architecture/client/)）
2. 学习依赖注入配置（参考 [`repository-dependency-injection-guide.md`](./shared/repository-dependency-injection-guide.md)）
3. 实践UI开发模式（Client端开发指南待完善）

#### 架构师/技术负责人
1. 理解技术决策（阅读ADR文档）
2. 管理技术债务（维护 [`TECH_DEBT_BACKLOG.md`](./shared/TECH_DEBT_BACKLOG.md)）
3. 制定开发标准（更新design-standard文档）

### 🔗 文档维护
- **更新原则**：开发实践变更时同步更新文档
- **版本管理**：重大流程变更时更新版本号
- **责任人**：Claude Code维护，团队贡献
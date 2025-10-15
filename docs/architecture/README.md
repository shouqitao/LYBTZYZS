# 架构文档索引

- **维护人**：Claude Code
- **最后更新**：2025-10-15
- **版本**：v3.0（Server/Client对齐架构）

本目录收录LYBT项目的架构设计、决策记录（ADR）、专题分析与实施指南。

---

## 🏗️ 对齐架构概览

### 🖥️ 端特定架构
- **[Server端架构](./server/)** - 三层架构、模块化设计、服务层标准
- **[Client端架构](./client/)** - MVVM模式、依赖注入、UI组件架构

### 🔄 共享架构
- **[共享架构](./shared/)** - 跨端架构决策、设计模式、技术标准

---

## 📁 详细目录

### 🖥️ Server端架构 (`server/`)
```
server/
├── README.md                           # 本导航文档
├── design-standard.md                  # Server端三层架构设计标准 ⭐
├── ADR-003-server-module-unified-design.md  # Server模块统一设计
└── module-template/                    # Server模块开发模板
    └── [待完善]                        # 模板和脚手架
```

### 🖥️ Client端架构 (`client/`)
```
client/
├── README.md                           # Client端架构导航
├── unified-design-standard.md          # MVVM统一设计标准 ⭐
└── module-template/                    # Client模块开发模板
    ├── README.md                       # 模板使用指南
    ├── module-checklist.md             # 模块开发检查清单
    └── [代码模板]                       # XAML/C#模板文件
```

### 🔄 共享架构 (`shared/`)
```
shared/
├── README.md                           # 共享架构导航
├── adr/                                # 架构决策记录 (ADR)
│   ├── ADR-001-cqrs-mediatr-rejection.md
│   ├── ADR-002-technology-roadmap-suggestion.md
│   └── ADR-005-desktop-modular-architecture.md
├── decisions/                          # 技术决策文档
│   ├── ADR-001-reject-overengineering.md
│   ├── ADR-002-desktop-services-removal.md
│   └── ADR-004-service-interface-unified-design-standard.md
└── testing/                            # 跨端测试标准
    └── architecture-testing-guide.md
```

---

## 🎯 快速导航

### 📋 必读文档
1. **[Server端设计标准](./server/design-standard.md)** - 三层架构规范
2. **[Client端设计标准](./client/unified-design-standard.md)** - MVVM架构规范
3. **[共享架构决策](./shared/adr/)** - 技术选型背景

### 🛠️ 开发模板
- **[Client模块模板](./client/module-template/)** - WPF组件开发脚手架
- **[Server模块模板](./server/module-template/)** - 服务模块开发脚手架

### 🧪 测试标准
- **[架构测试指南](./shared/testing/architecture-testing-guide.md)** - 架构合规性测试

---

## 📚 使用指南

### 👨‍💻 新成员入门
1. 选择端架构：Server → [`design-standard.md`](./server/design-standard.md) | Client → [`unified-design-standard.md`](./client/unified-design-standard.md)
2. 了解技术决策：阅读 [`shared/adr/`](./shared/adr/) 中的相关ADR
3. 使用开发模板：参考对应端的 `module-template/`

### 🏗️ 架构决策
- 新的技术选型需先创建ADR文档
- 重要的架构调整需更新design-standard文档
- 跨端影响的设计需放入shared目录

### 🔗 文档维护
- **更新频率**：架构变更时同步更新
- **版本管理**：重大架构调整时更新版本号
- **责任人**：Claude Code维护，团队review
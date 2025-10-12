# 贡献指南 - Pass 7 治理基线

欢迎为凌隐宝堂中医诊所系统做出贡献！本项目实施 **Pass 7 治理基线**，所有贡献必须严格遵循 Record-Only 架构约束和质量标准。

## 🚨 核心约束

- **功能模式**: Record-Only (仅CRUD + 历史查询)
- **架构模式**: 统一四层架构 (UI → Application → Domain → Infrastructure)
- **API约束**: 仅允许 `/api/v1/*` 路由
- **质量标准**: 零编译错误零警告，100%测试通过率

详细架构约束请参阅 **[开发标准 §1](docs/development/standards.md#1-架构约束pass-7-治理基线)**

## 📋 开发前准备

### 环境要求

```bash
- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- SQL Server (开发环境)
- Git 客户端
```

### 快速开始

```bash
# 1. 克隆和设置
git clone <repository-url>
cd LYBTZYZS
dotnet restore LYBT.All.sln
dotnet build LYBT.All.sln

# 2. 验证架构合规
dotnet test tests/Architecture/LYBT.ArchTests.csproj --verbosity normal
```

## 🔄 开发工作流

### 1. 创建功能分支

```bash
git checkout -b feature/your-feature-name  # 新功能
git checkout -b fix/bug-description        # Bug修复
git checkout -b chore/task-description     # 构建/工具相关
```

### 2. 开发过程检查

定期运行以下命令确保合规：

```bash
# Level 1: 代码质量
dotnet format LYBT.All.sln --verbosity minimal
dotnet build LYBT.All.sln --configuration Release

# Level 2: 测试质量
dotnet test --configuration Release --verbosity minimal

# Level 3: 架构合规
dotnet test tests/Architecture/LYBT.ArchTests.csproj
```

详细质量门禁说明请参阅 **[开发标准 §14](docs/development/standards.md#14-质量门禁cicd)**

### 3. 提交代码

使用 [Conventional Commits](https://www.conventionalcommits.org/) 格式：

```
<type>(<scope>): <subject>

<body>

Closes #<issue-number>
```

提交规范详情请参阅 **[开发标准 §13](docs/development/standards.md#13-版本管理标准)**

## 📚 核心标准文档

### 必读文档

- **[开发标准](docs/development/standards.md)** ⭐ 唯一权威开发规范（SSOT）
  - [§1 架构约束（Pass 7 治理基线）](docs/development/standards.md#1-架构约束pass-7-治理基线)
  - [§4 编码规范](docs/development/standards.md#4-编码规范)
  - [§5 分层实现规约](docs/development/standards.md#5-分层实现规约)
  - [§10 测试标准](docs/development/standards.md#10-测试标准)
  - [附录A：检查清单](docs/development/standards.md#附录a检查清单)

### 架构文档

- [Server端模块设计标准](docs/architecture/server-module-design-standard.md)
- [Desktop端统一设计标准](docs/architecture/client/unified-design-standard.md)
- [项目状态和技术决策](docs/PROJECT-STATUS-2025-09-27.md)

### 开发指南

- [最小实践指南](docs/development/minimal-practice.md) - Issue→清单→PR工作法
- [文档规范](docs/development/documentation-guidelines.md)
- [GitHub工作流程指南](docs/development/github-workflow-guide.md) - Issue/PR/标签/自动化

## ❓ 常见问题

**Q: 我的PR被CI阻塞了，怎么办？**
A: 查看CI构建日志，修复所有失败的检查项，然后重新提交。参考 [开发标准 §14](docs/development/standards.md#14-质量门禁cicd)

**Q: 我想添加一个复杂的业务规则，但被架构测试阻塞？**
A: 检查该功能是否符合Record-Only基线。如果超出CRUD+历史查询范围，需要重新设计为简单的数据记录功能。参考 [开发标准 §1.1](docs/development/standards.md#11-record-only-功能模式)

**Q: 我需要使用某个新的NuGet包，应该怎么做？**
A: 确保该包不在禁止框架列表中，然后在 `Directory.Packages.props` 中添加版本定义。参考 [开发标准 §3.2](docs/development/standards.md#32-核心技术栈)

**Q: 架构测试失败了，但我认为这是合理的设计？**
A: 治理基线是强制性的。如果确实需要例外情况，请提交架构例外申请，包含影响分析和风险评估。

## ✅ 提交前检查清单

在提交PR前，请确认：

### 开发完成
- [ ] 功能仅限Record-Only范围 (CRUD + 历史查询)
- [ ] 未引入禁止的框架或命名模式
- [ ] 遵循统一四层架构约束
- [ ] API使用 `/api/v1/*` 路由格式

### 质量检查
- [ ] `dotnet format --verify-no-changes` 通过
- [ ] `dotnet build --configuration Release` 零错误零警告
- [ ] `dotnet test --configuration Release` 全部通过
- [ ] `dotnet test tests/Architecture/` 全部通过

### 文档检查
- [ ] 已更新相关文档
- [ ] 提交信息符合Conventional Commits规范
- [ ] PR描述清晰，包含变更说明

完整检查清单请参阅 **[开发标准 附录A](docs/development/standards.md#附录a检查清单)**

## 📞 获取帮助

- **GitHub Issues**: 报告Bug或请求功能
- **GitHub Discussions**: 讨论架构和设计决策
- **文档索引**: [docs/index.md](docs/index.md) - 完整文档导航

---

**感谢您遵循Pass 7治理基线为项目做出高质量的贡献！** 🙏

严格的架构约束确保系统保持简洁、可维护，专注于小诊所的实际需求。

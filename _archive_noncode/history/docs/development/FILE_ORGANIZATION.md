# File Organization Standards
# 文件组织规范

## 1. Directory Structure / 目录结构

```
LYBTZYZS/
├── src/                        # Source code / 源代码
│   ├── Backend/               # Backend services / 后端服务
│   ├── Frontend/              # Frontend applications / 前端应用
│   └── Shared/                # Shared models / 共享模型
├── docs/                      # Documentation / 文档
│   ├── architecture/          # Architecture design / 架构设计
│   ├── api/                   # API documentation / API文档
│   ├── development/           # Development guides / 开发指南
│   ├── deployment/            # Deployment guides / 部署指南
│   ├── testing/               # Testing documentation / 测试文档
│   ├── reports/               # Project reports / 项目报告
│   ├── guides/                # User guides / 用户指南
│   ├── ultrathink/            # UltraThink methodology / UltraThink方法论
│   ├── design/                # UI/UX design / UI/UX设计
│   ├── fixes/                 # Fix documentation / 修复文档
│   ├── progress/              # Progress tracking / 进度跟踪
│   └── legacy/                # Archived documents / 存档文档
├── scripts/                   # Build and dev scripts / 构建和开发脚本
├── tools/                     # User tools and launchers / 用户工具和启动器
├── tests/                     # Test scripts and data / 测试脚本和数据
├── temp/                      # Temporary files (gitignored) / 临时文件（忽略）
├── README.md                  # Project overview / 项目概述
├── CLAUDE.md                  # AI assistant guidelines / AI助手指南
└── .gitignore                 # Git ignore rules / Git忽略规则
```

## 2. File Naming Conventions / 文件命名规范

### 2.1 General Rules / 通用规则
- **Use English names only** / 仅使用英文命名
- **Use kebab-case** (hyphen-separated) / 使用连字符分隔
- **Be descriptive and clear** / 描述清晰明确
- **Avoid special characters** / 避免特殊字符

### 2.2 Specific Formats / 具体格式
- Documents: `feature-name.md`
- Reports: `report-name-YYYYMMDD.md`
- Scripts: `action-description.bat` or `.ps1`
- Test files: `test-feature-name.py`

### 2.3 Examples / 示例
❌ **Wrong / 错误**:
- `WPF登录问题修复报告.md`
- `系统使用说明.md`
- `创建桌面快捷方式.bat`

✅ **Correct / 正确**:
- `wpf-login-fix-report.md`
- `system-user-guide.md`
- `create-desktop-shortcut.bat`

## 3. Document Categories / 文档分类

### 3.1 Architecture (`docs/architecture/`)
- System architecture diagrams / 系统架构图
- Technical design documents / 技术设计文档
- Module structure docs / 模块结构文档

### 3.2 API (`docs/api/`)
- API specifications / API规范
- Endpoint documentation / 端点文档
- Integration guides / 集成指南

### 3.3 Development (`docs/development/`)
- Coding standards / 编码规范
- Development workflows / 开发流程
- Configuration guides / 配置指南
- **FILE_ORGANIZATION.md** (this document)

### 3.4 Deployment (`docs/deployment/`)
- Installation guides / 安装指南
- Environment setup / 环境配置
- Release procedures / 发布流程

### 3.5 Testing (`docs/testing/`)
- Test plans / 测试计划
- Test cases / 测试用例
- Test reports / 测试报告

### 3.6 Reports (`docs/reports/`)
- Progress reports / 进度报告
- Fix reports / 修复报告
- Performance reports / 性能报告
- **Date format**: `report-YYYYMMDD.md`

### 3.7 Guides (`docs/guides/`)
- User manuals / 用户手册
- Quick start guides / 快速入门
- Tutorial documents / 教程文档

### 3.8 UltraThink (`docs/ultrathink/`)
- Methodology documents / 方法论文档
- Analysis reports / 分析报告
- Implementation guides / 实施指南

### 3.9 Design (`docs/design/`)
- UI specifications / UI规范
- UX guidelines / UX指南
- Visual design docs / 视觉设计文档

## 4. Script Organization / 脚本组织

### 4.1 Development Scripts (`scripts/`)
- Build scripts / 构建脚本
- Database scripts / 数据库脚本
- Development utilities / 开发工具
- CI/CD scripts / 持续集成脚本

### 4.2 User Tools (`tools/`)
- Application launchers / 应用启动器
- Installation helpers / 安装助手
- Desktop shortcuts / 桌面快捷方式
- Maintenance tools / 维护工具

## 5. Version Control Rules / 版本控制规则

### 5.1 Git Operations
- Use `git mv` to move files (preserves history) / 使用`git mv`移动文件（保留历史）
- Commit file reorganization separately / 单独提交文件重组
- Write clear commit messages / 编写清晰的提交信息

### 5.2 .gitignore Rules
```gitignore
# Temporary files
temp/
*.tmp
*.temp

# Local configuration
*.local.md
*.private.*

# IDE files
.vs/
.vscode/
.idea/
```

## 6. Mandatory Rules for AI Assistants / AI助手强制规则

When creating or modifying files, AI assistants MUST:

1. **NEVER create documentation files in the root directory**
2. **ALWAYS use English file names**
3. **ALWAYS follow the directory structure above**
4. **ALWAYS include dates in report filenames (YYYYMMDD format)**
5. **ALWAYS place temporary files in `temp/` directory**
6. **ALWAYS use kebab-case for file naming**
7. **NEVER use Chinese characters in file names**
8. **ALWAYS organize files by their purpose and type**

## 7. Migration Checklist / 迁移清单

When reorganizing existing files:

- [ ] Create directory structure
- [ ] Use `git mv` for all file moves
- [ ] Rename Chinese files to English
- [ ] Update internal references
- [ ] Update CLAUDE.md with standards
- [ ] Update .gitignore if needed
- [ ] Commit changes with clear message
- [ ] Update documentation index

## 8. Quick Reference / 快速参考

| File Type | Location | Naming Pattern | Example |
|-----------|----------|----------------|---------|
| User Guide | `docs/guides/` | `feature-guide.md` | `system-user-guide.md` |
| API Doc | `docs/api/` | `api-name.md` | `auth-api.md` |
| Report | `docs/reports/` | `name-YYYYMMDD.md` | `fix-report-20250131.md` |
| Test Doc | `docs/testing/` | `test-feature.md` | `login-test-guide.md` |
| UltraThink | `docs/ultrathink/` | `analysis-name.md` | `ui-design-system-report.md` |
| Dev Script | `scripts/` | `action.bat` | `build-all.bat` |
| User Tool | `tools/` | `tool-name.bat` | `start-system.bat` |

## 9. Document Index / 文档索引

For easy navigation, maintain a `docs/INDEX.md` file that lists all documents with their descriptions and locations.

---

**Last Updated / 最后更新**: 2025-01-31  
**Version / 版本**: 1.0.0  
**Author / 作者**: LYBT Development Team

> 📌 **Note**: This document is mandatory reading for all developers and AI assistants working on this project.  
> 📌 **注意**: 本文档是所有开发人员和AI助手的必读文件。